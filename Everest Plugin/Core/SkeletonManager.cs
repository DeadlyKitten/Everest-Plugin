using System;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using Everest.Api;
using Everest.Jobs;
using Everest.UI;
using Everest.Utilities;
using ExitGames.Client.Photon;
using Photon.Pun;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Everest.Core
{
    public class SkeletonManager : MonoBehaviourPun
    {
        private const int MAX_ATTEMPTS_FOR_UUID = 20;
        private const string SERVER_RESPONSE_IDENTIFIER_KEY = "serverResponseIdentifier";
        private string _serverResponseIdentifier;

        private Transform _playerCamera;

        private static Skeleton _skeletonPrefab;

        private CullingJobNative _cullingJob;

        private NativeHashSet<int> _objectsInRange;
        private NativeHashSet<int> _objectsInRangeLastIteration;
        private CullableSkeleton[] _skeletons;
        private NativeArray<float> _skeletonPositionsX;
        private NativeArray<float> _skeletonPositionsY;
        private NativeArray<float> _skeletonPositionsZ;
        private NativeArray<DistanceCullingResult> _cullingResults;
        private float distanceThresholdSquared;
        private SkeletonPool _skeletonPool;

        private float _elapsedTime = -2f;
        private bool _initialized = false;
        private bool _isCulling = false;

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
        }

        private async void Start()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _playerCamera = Camera.main.transform;

            EverestPlugin.LogInfo("Waiting to establish connection to room...");
            await UniTask.WaitUntil(() => PhotonNetwork.IsConnected && PhotonNetwork.InRoom);
            EverestPlugin.LogInfo($"Connection established as {(PhotonNetwork.IsMasterClient ? "Host" : "Client")}.");

            _skeletons = (await GetSkeletonDataAsync()).Select(Skeleton => new CullableSkeleton(Skeleton)).ToArray();

            if (_skeletons == null || _skeletons.Length == 0)
            {
                EverestPlugin.LogWarning("No skeleton data found for this map.");
                ToastController.Instance.Toast("No skeletons :(", Color.red, 5f, 3f);
                DestroyImmediate(this.gameObject);
                return;
            }
            _totalSkeletonCount = _skeletons.Length;

            EverestPlugin.LogInfo($"Received {_totalSkeletonCount} skeletons.");
            
            PrepareSkeletonPool();

            _skeletonPositionsX = new NativeArray<float>(_totalSkeletonCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _skeletonPositionsY = new NativeArray<float>(_totalSkeletonCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _skeletonPositionsZ = new NativeArray<float>(_totalSkeletonCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < _totalSkeletonCount; i++)
            {
                var skeleton = _skeletons[i];

                float3 pos = (float3)SkeletonTransformHelper.GetHipWorldPosition(
                    skeleton.Data.global_position,
                    skeleton.Data.global_rotation,
                    skeleton.Data.bone_local_positions[0]
                );

                _skeletonPositionsX[i] = pos.x;
                _skeletonPositionsY[i] = pos.y;
                _skeletonPositionsZ[i] = pos.z;
            }

            _cullingResults = new NativeArray<DistanceCullingResult>(_totalSkeletonCount, Allocator.Persistent);
            _objectsInRange = new NativeHashSet<int>(ConfigHandler.MaxVisibleSkeletons, Allocator.Persistent);
            _objectsInRangeLastIteration = new NativeHashSet<int>(ConfigHandler.MaxVisibleSkeletons, Allocator.Persistent);

            distanceThresholdSquared = Mathf.Pow(ConfigHandler.SkeletonDrawDistance, 2);

            unsafe
            {
                _cullingJob = new CullingJobNative()
                {
                    SquaredDrawDistance = distanceThresholdSquared,
                    SkeletonPositionsX = (float*)_skeletonPositionsX.GetUnsafeReadOnlyPtr(),
                    SkeletonPositionsY = (float*)_skeletonPositionsY.GetUnsafeReadOnlyPtr(),
                    SkeletonPositionsZ = (float*)_skeletonPositionsZ.GetUnsafeReadOnlyPtr(),
                    Results = (DistanceCullingResult*)_cullingResults.GetUnsafePtr()
                };
            }

            _initialized = true;

            stopwatch.Stop();
            ToastController.Instance.Toast($"{_totalSkeletonCount} skeletons have been summoned! Took {stopwatch.ElapsedMilliseconds} ms.", Color.green, 5f, 3f);
        }

        private void OnDestroy()
        {
            _skeletonPositionsX.Dispose();
            _skeletonPositionsY.Dispose();
            _skeletonPositionsZ.Dispose();
            _cullingResults.Dispose();
            _objectsInRange.Dispose();
            _objectsInRangeLastIteration.Dispose();
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

        private async UniTaskVoid HandleDistanceCullingAsync()
        {
            _isCulling = true;

            var cameraPosition = _playerCamera.position;

            if (Vector3.Distance(_cullingJob.CameraPosition, cameraPosition) < 0.1f)
            {
                _isCulling = false;
                return;
            }

            _cullingJob.CameraPosition = cameraPosition;
            var cullingJobHandle = _cullingJob.ScheduleNative(_totalSkeletonCount, 256);

            var sortingJobHandle = _cullingResults.SortJob().Schedule(cullingJobHandle);
            await sortingJobHandle;
            if (destroyCancellationToken.IsCancellationRequested) return;
            sortingJobHandle.Complete();

            var displayCount = 0;
            for (int i = 0; i < ConfigHandler.MaxVisibleSkeletons; i++)
            {
                if (_cullingResults[i].distance == float.MaxValue)
                    break;

                _objectsInRange.Add(_cullingResults[i].index);
                displayCount++;
            }

            ProcessCullingResults();

            _objectsInRange.Clear();
            _isCulling = false;
        }

        private void ProcessCullingResults()
        {
            if (SetEquals(_objectsInRange, _objectsInRangeLastIteration))
                return;

            foreach (var index in _objectsInRangeLastIteration)
            {
                if (!_objectsInRange.Contains(index) && _skeletons[index].Instance != null)
                {
                    _skeletonPool.Release(_skeletons[index].Instance);
                    _skeletons[index].Instance = null;
                }
            }

            foreach (var index in _objectsInRange)
            {
                if (!_objectsInRangeLastIteration.Contains(index) && _skeletons[index].Instance == null)
                {
                    _skeletons[index].Instance = _skeletonPool.Get();
                    PrepareSkeleton(_skeletons[index].Data, _skeletons[index].Instance);
                }
            }

            (_objectsInRange, _objectsInRangeLastIteration) = (_objectsInRangeLastIteration, _objectsInRange);
        }

        private bool SetEquals(NativeHashSet<int> a, NativeHashSet<int> b)
        {
            if (a.Count != b.Count) return false;

            foreach (var item in a)
            {
                if (!b.Contains(item)) return false;
            }

            return true;
        }

        public static async UniTask LoadSkeletonPrefabAsync()
        {
            EverestPlugin.LogInfo("Loading skeleton prefab...");

            var skeletonObject = await Resources.LoadAsync<GameObject>("Skeleton") as GameObject;

            if (skeletonObject == null)
            {
                EverestPlugin.LogError("Skeleton prefab not found in Resources.");
                return;
            }

            _skeletonPrefab = Instantiate(skeletonObject).AddComponent<Skeleton>();
            DontDestroyOnLoad(_skeletonPrefab);

            foreach (var sfx in _skeletonPrefab.GetComponentsInChildren<SFX_PlayOneShot>())
            {
                DestroyImmediate(sfx);
            }

            _skeletonPrefab.gameObject.SetActive(false);

            EverestPlugin.LogInfo($"Skeleton prefab loaded: {_skeletonPrefab != null}");
        }

        private void PrepareSkeleton(SkeletonData skeletonData, Skeleton skeleton)
        {
            var spawnPosition = SkeletonTransformHelper.GetHipWorldPosition(
                skeletonData.global_position,
                skeletonData.global_rotation,
                skeletonData.bone_local_positions[0]
            );

            if (Vector3.Distance(spawnPosition, TombstoneHandler.TombstonePosition) < 1f)
            {
                EverestPlugin.LogDebug("Skeleton position is too close to tombstone, skipping skeleton.");
                skeleton.gameObject.SetActive(false);
                return;
            }

            if (ConfigHandler.HideFloaters)
            {
                var layerMask = LayerMask.GetMask("Default", "Terrain", "Map");
                var colliders = Physics.OverlapSphere(spawnPosition, 1f, layerMask);

                if (!colliders.Any())
                {
                    EverestPlugin.LogDebug("Skeleton is in midair! Skipping skeleton.");
                    skeleton.gameObject.SetActive(false);
                    return;
                }
            }

            skeleton.Initialize(skeletonData);
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

        private void PrepareSkeletonPool() => _skeletonPool = new SkeletonPool(_skeletonPrefab, transform);

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
    }
}
