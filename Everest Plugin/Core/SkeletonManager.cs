using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Everest.Api;
using Everest.UI;
using Everest.Utilities;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;

namespace Everest.Core
{
    public class SkeletonManager : MonoBehaviourPun
    {
        private const int MAX_ATTEMPTS_FOR_UUID = 20;
        private const string SERVER_RESPONSE_IDENTIFIER_KEY = "serverResponseIdentifier";
        private string _serverResponseIdentifier;

        private static GameObject _skeletonPrefab;

        private static ComputeShader _distanceCheckShader;
        private int _kernelIndex;
        private ComputeBuffer _countBuffer;
        private ComputeBuffer _positionsBuffer;
        private ComputeBuffer _resultsBuffer;
        private HashSet<uint> _objectsInRange = new HashSet<uint>();
        private HashSet<uint> _objectsInRangeLastIteration = new HashSet<uint>();
        private CullableSkeleton[] _skeletons;
        private IObjectPool<Skeleton> _skeletonPool;

        private float _elapsedTime = -2f;
        private bool _initialized = false;
        private bool _isCulling = false;
        private Stopwatch _cullLoopTimer = new Stopwatch();

        private int _totalSkeletonCount;

        private void Awake()
        {
            if (ConfigHandler.MaxSkeletons <= 0)
            {
                EverestPlugin.LogWarning("Number of skeletons is set to 0 in the configuration. Exiting...");
                DestroyImmediate(gameObject);
                return;
            }

            if (_skeletonPrefab == null)
            {
                EverestPlugin.LogError("Skeleton prefab not set. Not spawning skeletons.");
                DestroyImmediate(gameObject);
                return;
            }

            EverestPlugin.LogDebug("Skeleton Manager Initializing...");
        }

        private async void Start()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            EverestPlugin.LogInfo("Waiting to establish connection to room...");
            await UniTask.WaitUntil(() => PhotonNetwork.IsConnected && PhotonNetwork.InRoom);
            EverestPlugin.LogInfo($"Connection established as {(PhotonNetwork.IsMasterClient ? "Host" : "Client")}.");

            _skeletons = (await GetSkeletonDataAsync()).Select(Skeleton => new CullableSkeleton(Skeleton)).ToArray();

            if (_skeletons == null || _skeletons.Length == 0)
            {
                EverestPlugin.LogWarning("No skeleton data found for this map.");
                ToastController.Instance.Toast("No skeletons :(", Color.red, 5f, 3f);
                return;
            }
            EverestPlugin.LogInfo($"Received {_skeletons.Length} skeletons.");

            _totalSkeletonCount = Math.Min(ConfigHandler.MaxSkeletons, _skeletons.Length);
            
            PrepareBuffers();

            PrepareSkeletonPool();

            _initialized = true;

            stopwatch.Stop();
            ToastController.Instance.Toast($"{_totalSkeletonCount} skeletons have been summoned! Took {stopwatch.ElapsedMilliseconds} ms.", Color.green, 5f, 3f);
        }

        private void Update()
        {
            if (!_initialized || _isCulling) return;

            if (_elapsedTime < ConfigHandler.CullingUpdateFrequency)
            {
                _elapsedTime += Time.deltaTime;
                return;
            }
            _elapsedTime = 0;

            HandleDistanceCullingAsync().Forget();
        }

        private void OnDestroy()
        {
            _countBuffer?.Release();
            _positionsBuffer?.Release();
            _resultsBuffer?.Release();
        }

        private async UniTaskVoid HandleDistanceCullingAsync()
        {
            _isCulling = true;

            _distanceCheckShader.SetVector("_PlayerPosition", Camera.main.transform.position);

            _resultsBuffer.SetCounterValue(0);

            _distanceCheckShader.Dispatch(_kernelIndex, Mathf.CeilToInt(_totalSkeletonCount / 256f), 1, 1);

            var resultsCount = await GetResultCountAsync();

            if (resultsCount == 0)
            {
                _objectsInRange.Clear();
                await ProcessCullingResults();
                _isCulling = false;
                return;
            }

            var request = await AsyncGPUReadback.Request(_resultsBuffer, resultsCount * Marshal.SizeOf(typeof(DistanceCullingResult)), 0);

            if (request.hasError)
            {
                EverestPlugin.LogError("GPU Readback error");
                _isCulling = false;
                return;
            }

            var _results = request.GetData<DistanceCullingResult>().ToArray();
            _objectsInRange.Clear();

            Array.Sort(_results);

            var count = Math.Min(resultsCount, ConfigHandler.MaxVisibleSkeletons);

            for (int i = 0; i < count; i++)
            {
                _objectsInRange.Add(_results[i].index);
            }

            await ProcessCullingResults();

            _isCulling = false;
        }

        private async UniTask ProcessCullingResults()
        {
            _cullLoopTimer.Restart();

            if (_objectsInRange.SetEquals(_objectsInRangeLastIteration))
                return;

            _objectsInRangeLastIteration.Clear();
            foreach (var index in _objectsInRange)
            {
                _objectsInRangeLastIteration.Add(index);
            }

            for (uint i = 0; i < _totalSkeletonCount; i++)
            {
                bool isInRange = _objectsInRange.Contains(i);

                if (isInRange)
                {
                    if (_skeletons[i].Instance == null)
                    {
                        _skeletons[i].Instance = _skeletonPool.Get();
                        await PrepareSkeleton(_skeletons[i].Data, _skeletons[i].Instance);
                    }
                }
                else
                {
                    if (_skeletons[i].Instance != null)
                    {
                        _skeletonPool.Release(_skeletons[i].Instance);
                        _skeletons[i].Instance = null;
                    }
                }

                if (_cullLoopTimer.Elapsed.TotalMilliseconds >= 1)
                {
                    await UniTask.Yield();
                    _cullLoopTimer.Restart();
                }
            }

            _cullLoopTimer.Stop();
        }

        private async UniTask<int> GetResultCountAsync()
        {
            ComputeBuffer.CopyCount(_resultsBuffer, _countBuffer, 0);
            var request = await AsyncGPUReadback.Request(_countBuffer);

            if (request.hasError)
            {
                EverestPlugin.LogError("GPU count readback error.");
                return 0;
            }

            return request.GetData<int>()[0];
        }

        private void PrepareBuffers()
        {
            _kernelIndex = _distanceCheckShader.FindKernel("CSMain");

            _distanceCheckShader.SetFloat("_Threshold", ConfigHandler.SkeletonDrawDistance);
            _distanceCheckShader.SetInt("_TotalPositions", _totalSkeletonCount);

            _countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

            _resultsBuffer = new ComputeBuffer(_totalSkeletonCount, Marshal.SizeOf(typeof(DistanceCullingResult)), ComputeBufferType.Append);
            _distanceCheckShader.SetBuffer(_kernelIndex, "_Results", _resultsBuffer);

            _positionsBuffer = new ComputeBuffer(_skeletons.Length, Marshal.SizeOf(typeof(Vector3)));
            _distanceCheckShader.SetBuffer(_kernelIndex, "_Positions", _positionsBuffer);

            var positions = _skeletons.Select(skeleton => SkeletonTransformHelper.GetHipWorldPosition(
                skeleton.Data.global_position,
                skeleton.Data.global_rotation,
                skeleton.Data.bone_local_positions[0])
            ).ToArray();

            _positionsBuffer.SetData(positions);
        }

        public static async UniTask LoadComputeShaderAsync()
        {
            EverestPlugin.LogInfo("Loading compute shader...");

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Everest.Resources.computeshader.bundle");

            if (stream == null)
            {
                EverestPlugin.LogError("Compute Shader AssetBundle not found in resources.");
                return;
            }

            var assetBundle = await AssetBundle.LoadFromStreamAsync(stream);

            var asset = await assetBundle.LoadAssetAsync("Assets/Everest/distancethreshold.compute").ToUniTask();

            _distanceCheckShader = asset as ComputeShader;

            await assetBundle.UnloadAsync(false).ToUniTask();

            EverestPlugin.LogInfo($"Compute Shader prefab loaded: {_distanceCheckShader != null}");
        }

        public static async UniTask LoadSkeletonPrefabAsync()
        {
            EverestPlugin.LogInfo("Loading skeleton prefab...");

            var skeletonObject = await Resources.LoadAsync<GameObject>("Skeleton") as GameObject;

            if (skeletonObject == null)
            {
                EverestPlugin.LogError("Skeleton prefab not found in Resources.");
                ToastController.Instance.Toast("Skeleton prefab not found in Resources.", Color.red, 5f, 3f);
                return;
            }

            _skeletonPrefab = Instantiate(skeletonObject).AddComponent<Skeleton>();
            DontDestroyOnLoad(_skeletonPrefab);

            foreach (var sfx in _skeletonPrefab.GetComponentsInChildren<SFX_PlayOneShot>())
            {
                DestroyImmediate(sfx);
            }
        }

            _skeletonPrefab.gameObject.SetActive(false);

            EverestPlugin.LogInfo($"Skeleton prefab loaded: {_skeletonPrefab != null}");
        }

            for (int boneIndex = 0; boneIndex < 18; boneIndex++)
            {
                skeleton.Bones[boneIndex].SetLocalPositionAndRotation(skeletonData.bone_local_positions[boneIndex], Quaternion.Euler(skeletonData.bone_local_rotations[boneIndex]));

                if (boneIndex == 0)
                {
                    if (Vector3.Distance(skeleton.Bones[boneIndex].position, TombstoneHandler.TombstonePosition) < 1f)
                    {
                EverestPlugin.LogDebug("Skeleton position is too close to tombstone, skipping skeleton.");
                        skeleton.gameObject.SetActive(false);
                        return;
                    }

                    if (ConfigHandler.HideFloaters)
                    {
                        var layerMask = LayerMask.GetMask("Default", "Terrain", "Map");
                        var colliders = Physics.OverlapSphere(skeleton.Bones[boneIndex].position, 1f, layerMask);

                        if (!colliders.Any())
                        {
                    EverestPlugin.LogDebug("Skeleton is in midair! Skipping skeleton.");
                            skeleton.gameObject.SetActive(false);
                            return;
                        }
                    }
                }
            }

            await skeleton.Initialize(skeletonData);
        }

        private async UniTask<SkeletonData[]> GetSkeletonDataAsync()
        {
            var skeletonDatas = new SkeletonData[0];

            if (PhotonNetwork.IsMasterClient)
                skeletonDatas = await RetrieveSkeletonDatasAsHostAsync();
            else
                skeletonDatas = await RetrieveSkeletonDatasAsClientAsync();
            return skeletonDatas;
        }

        private async UniTask<SkeletonData[]> RetrieveSkeletonDatasAsHostAsync()
        {
            EverestPlugin.LogInfo("Retrieving skeleton data as host...");

            var mapId = GameHandler.GetService<NextLevelService>().Data.Value.CurrentLevelIndex;
            var serverResponse = await EverestClient.RetrieveAsync(mapId);

            if (serverResponse == null || serverResponse.data == null)
            {
                EverestPlugin.LogWarning("No skeleton data found for this map.");
                ToastController.Instance.Toast("No skeletons :(", Color.red, 5f, 3f);
                return Array.Empty<SkeletonData>();
            }
            SyncServerResponseIdentifier(serverResponse.identifier);
            return serverResponse.data;
        }

        private async UniTask<SkeletonData[]> RetrieveSkeletonDatasAsClientAsync()
        {
            EverestPlugin.LogInfo("Retrieving skeleton data as client...");

            var attempts = 0;

            while (attempts++ < MAX_ATTEMPTS_FOR_UUID)
            {
                if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SERVER_RESPONSE_IDENTIFIER_KEY, out var identifier))
                {
                    _serverResponseIdentifier = identifier as string;
                    break;
                }

                await UniTask.Delay(50);
            }

            if (string.IsNullOrEmpty(_serverResponseIdentifier))
            {
                EverestPlugin.LogWarning("Failed to retrieve server response identifier from host after multiple attempts.");
                return await RetrieveSkeletonDatasAsHostAsync();
            }

            EverestPlugin.LogInfo($"Received identifier UUID from host: {_serverResponseIdentifier}");

            var serverResponse = await EverestClient.RetrieveAsync(_serverResponseIdentifier);

            return serverResponse.data;
        }

        private void SyncServerResponseIdentifier(string identifier)
        {
            EverestPlugin.LogInfo($"Syncing server response identifier: {identifier}");
            _serverResponseIdentifier = identifier;

            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { SERVER_RESPONSE_IDENTIFIER_KEY, identifier }
            });
        }

        private void PrepareSkeletonPool()
        {
            _skeletonPool = new ObjectPool<Skeleton>(CreateSkeleton, GetSkeleton, ReleaseSkeleton, DestroySkeleton, false, 10, 100);
        }

        private Skeleton CreateSkeleton()
        {
            var skeletonObject = Instantiate(_skeletonPrefab);
            skeletonObject.SetActive(false);
            skeletonObject.transform.SetParent(transform);
            return skeletonObject.AddComponent<Skeleton>();
        }

        private void GetSkeleton(Skeleton skeleton)
        {
            skeleton.gameObject.SetActive(true);
        }

        private void ReleaseSkeleton(Skeleton skeleton)
        {
            skeleton.RemoveAccessories();
            skeleton.gameObject.SetActive(false);
        }

        private void DestroySkeleton(Skeleton skeleton)
        {
            Destroy(skeleton.gameObject);
        }

        private struct CullableSkeleton
        {
            public Skeleton Instance;
            public SkeletonData Data;

            public CullableSkeleton(SkeletonData data)
            {
                Instance = null;
                Data = data;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DistanceCullingResult : IComparable<DistanceCullingResult>
        {
            public uint index;
            public float distance;

            public int CompareTo(DistanceCullingResult other)
            {
                return distance.CompareTo(other.distance);
            }
        }
    }
}
