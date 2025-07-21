using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Everest.Api;
using Everest.Utilities;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
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
        private ComputeBuffer _positionsBuffer;
        private ComputeBuffer _resultsBuffer;
        private HashSet<uint> _objectsInRange = new HashSet<uint>();
        private HashSet<uint> _objectsInRangeLastIteration = new HashSet<uint>();
        private CullableSkeleton[] _skeletons;
        private IObjectPool<Skeleton> _skeletonPool;

        private float _elapsedTime = -2f;
        private bool _initialized = false;
        private bool _isCulling = false;

        private int _totalSkeletonCount;

        private void Awake()
        {
            if (ConfigHandler.MaxSkeletons <= 0)
            {
                EverestPlugin.LogInfo("Number of skeletons is set to 0 in the configuration. Exiting...");
                return;
            }

            if (_skeletonPrefab == null)
            {
                EverestPlugin.LogDebug("Skeleton prefab not set. Not spawning skeletons.");
                return;
            }

            EverestPlugin.LogDebug("Skeleton Manager Initializing...");
        }

        private async void Start()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            EverestPlugin.LogDebug("Waiting to establish connection to room...");
            await UniTask.WaitUntil(() => PhotonNetwork.IsConnected && PhotonNetwork.InRoom);
            EverestPlugin.LogDebug($"Connection established as {(PhotonNetwork.IsMasterClient ? "Host" : "Client")}.");

            _skeletons = (await GetSkeletonData()).Select(Skeleton => new CullableSkeleton(Skeleton)).ToArray();

            if (_skeletons == null || _skeletons.Length == 0)
            {
                EverestPlugin.LogWarning("No skeleton data found for this map.");
                UIHandler.Instance.Toast("No skeletons :(", Color.red, 5f, 3f);
                return;
            }
            EverestPlugin.LogDebug($"Received {_skeletons.Length} skeletons.");

            _totalSkeletonCount = Math.Min(ConfigHandler.MaxSkeletons, _skeletons.Length);
            
            PrepareBuffers();

            PrepareSkeletonPool();

            _initialized = true;

            stopwatch.Stop();
            UIHandler.Instance.Toast($"{_totalSkeletonCount} skeletons have been summoned! Took {stopwatch.ElapsedMilliseconds} ms.", Color.green, 5f, 3f);
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

            HandleDistanceCulling().Forget();
        }

        private void OnDestroy()
        {
            _positionsBuffer?.Release();
            _resultsBuffer?.Release();
        }

        private async UniTaskVoid HandleDistanceCulling()
        {
            _isCulling = true;

            _distanceCheckShader.SetVector("_PlayerPosition", Camera.main.transform.position);

            _resultsBuffer.SetCounterValue(0);

            _distanceCheckShader.Dispatch(_kernelIndex, Mathf.CeilToInt(_totalSkeletonCount / 256f), 1, 1);

            var resultsCount = GetResultCount();

            if (resultsCount == 0)
            {
                _objectsInRange.Clear();
                await ProcessCullingResults();
                _isCulling = false;
                return;
            }

            var request = AsyncGPUReadback.Request(_resultsBuffer, resultsCount * Marshal.SizeOf(typeof(DistanceCullingResult)), 0);
            await request;

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
                        _skeletons[i].Instance.gameObject.name = $"Skeleton_{i}";
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
            }
        }

        private int GetResultCount()
        {
            ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            ComputeBuffer.CopyCount(_resultsBuffer, countBuffer, 0);
            int[] countArray = { 0 };
            countBuffer.GetData(countArray);
            countBuffer.Release();
            return countArray[0];
        }

        private void PrepareBuffers()
        {
            _kernelIndex = _distanceCheckShader.FindKernel("CSMain");

            _distanceCheckShader.SetFloat("_Threshold", ConfigHandler.SkeletonDrawDistance);
            _distanceCheckShader.SetInt("_TotalPositions", _totalSkeletonCount);

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

        public static async UniTaskVoid LoadComputeShader()
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Everest.Resources.computeshader.bundle");

            if (stream == null)
            {
                EverestPlugin.LogError("Compute Shader AssetBundle not found in resources.");
                return;
            }

            var assetBundle = await AssetBundle.LoadFromStreamAsync(stream);

            var request = assetBundle.LoadAssetAsync("Assets/Everest/distancethreshold.compute");
            await UniTask.WaitUntil(() => request.isDone);

            _distanceCheckShader = request.asset as ComputeShader;

            EverestPlugin.LogDebug($"Compute Shader prefab loaded: {_distanceCheckShader != null}");

            await assetBundle.UnloadAsync(false).ToUniTask();
        }

        public static async UniTaskVoid LoadSkeletonPrefab()
        {
            _skeletonPrefab = await Resources.LoadAsync<GameObject>("Skeleton") as GameObject;

            if (_skeletonPrefab == null)
            {
                EverestPlugin.LogError("Skeleton prefab not found in Resources.");
                UIHandler.Instance.Toast("Skeleton prefab not found in Resources.", Color.red, 5f, 3f);
                return;
            }

            foreach (var sfx in _skeletonPrefab.GetComponentsInChildren<SFX_PlayOneShot>())
            {
                DestroyImmediate(sfx);
            }
        }

        private async UniTask PrepareSkeleton(SkeletonData skeletonData, Skeleton skeleton)
        {
            skeleton.transform.SetPositionAndRotation(skeletonData.global_position, Quaternion.Euler(skeletonData.global_rotation));

            var bones = skeleton.transform.GetComponentsInChildren<Transform>().Where(x => Enum.GetNames(typeof(SkeletonBodypart)).Contains(x.name)).ToList();
            for (int boneIndex = 0; boneIndex < 18; boneIndex++)
            {
                bones[boneIndex].SetLocalPositionAndRotation(skeletonData.bone_local_positions[boneIndex], Quaternion.Euler(skeletonData.bone_local_rotations[boneIndex]));

                if (boneIndex == 0)
                {
                    if (Vector3.Distance(bones[boneIndex].position, TombstoneHandler.TombstonePosition) < 1f)
                    {
                        EverestPlugin.LogWarning("Skeleton position is too close to tombstone, skipping skeleton.");
                        skeleton.gameObject.SetActive(false);
                        return;
                    }
                }
            }

            var steamId = skeletonData.steam_id;
            await skeleton.TryAddAccessory(steamId);
        }

        private async UniTask<SkeletonData[]> GetSkeletonData()
        {
            var skeletonDatas = new SkeletonData[0];

            if (PhotonNetwork.IsMasterClient)
                skeletonDatas = await RetrieveSkeletonDatasAsHost();
            else
                skeletonDatas = await RetrieveSkeletonDatasAsClient();
            return skeletonDatas;
        }

        private async UniTask<SkeletonData[]> RetrieveSkeletonDatasAsHost()
        {
            EverestPlugin.LogDebug("Retrieving skeleton data as host...");

            var mapId = GameHandler.GetService<NextLevelService>().Data.Value.CurrentLevelIndex;
            var serverResponse = await EverestClient.RetrieveAsync(mapId);
            SyncServerResponseIdentifier(serverResponse.identifier);
            return serverResponse.data;
        }

        private async UniTask<SkeletonData[]> RetrieveSkeletonDatasAsClient()
        {
            EverestPlugin.LogDebug("Retrieving skeleton data as client...");

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
                return await RetrieveSkeletonDatasAsHost();
            }

            EverestPlugin.LogDebug($"Received identifier UUID from host: {_serverResponseIdentifier}");

            var serverResponse = await EverestClient.RetrieveAsync(_serverResponseIdentifier);

            return serverResponse.data;
        }

        private void SyncServerResponseIdentifier(string identifier)
        {
            EverestPlugin.LogDebug($"Syncing server response identifier: {identifier}");
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
