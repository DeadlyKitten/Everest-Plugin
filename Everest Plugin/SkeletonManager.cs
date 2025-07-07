using System;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Everest.Api;
using Everest.Utilities;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.LowLevel;
using System.Diagnostics;

namespace Everest
{
    public class SkeletonManager : MonoBehaviourPun
    {
        private const string SERVER_RESPONSE_IDENTIFIER_KEY = "serverResponseIdentifier";
        private string serverResponseIdentifier;

        private static GameObject skeletonPrefab;

        public void Awake()
        {
            if (ConfigHandler.MaxSkeletons <= 0)
            {
                EverestPlugin.LogInfo("Number of skeletons is set to 0 in the configuration. Exiting...");
                return;
            }

            if (skeletonPrefab == null)
            {
                EverestPlugin.LogDebug("Skeleton prefab not set. Not spawning skeletons.");
                return;
            }

            EverestPlugin.LogDebug("Skeleton Manager Initializing...");
            GenerateSkeletons().Forget();
        }

        public static async UniTaskVoid LoadSkeletonPrefab()
        {
            skeletonPrefab = await Resources.LoadAsync<GameObject>("Skeleton") as GameObject;

            if (skeletonPrefab == null)
            {
                EverestPlugin.LogError("Skeleton prefab not found in Resources.");
                UIHandler.Instance.Toast("Skeleton prefab not found in Resources.", Color.red, 5f, 3f);
            }
            
        }

        private async UniTaskVoid GenerateSkeletons()
        {
            if (!PlayerLoopHelper.IsInjectedUniTaskPlayerLoop())
            {
                var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
                PlayerLoopHelper.Initialize(ref playerLoop);
            }

            EverestPlugin.LogDebug("Waiting to establish connection to room...");
            await UniTask.WaitUntil(() => PhotonNetwork.IsConnected && PhotonNetwork.InRoom);
            EverestPlugin.LogDebug($"Connection established as {(PhotonNetwork.IsMasterClient ? "Host" : "Client")}.");

            SkeletonData[] skeletonDatas = await GetSkeletonData();

            if (skeletonDatas == null || skeletonDatas.Length == 0)
            {
                EverestPlugin.LogWarning("No skeleton data found for this map.");
                UIHandler.Instance.Toast("No skeletons :(", Color.yellow, 5f, 3f);
                return;
            }
            else
            {
                EverestPlugin.LogDebug($"Received {skeletonDatas.Length} skeletons.");
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            EverestPlugin.LogDebug("Instantiating skeletons...");
            var numberOfSkeletonsToSpawn = Math.Min(ConfigHandler.MaxSkeletons, skeletonDatas.Length);
            var skeletons = await InstantiateAsync(skeletonPrefab, numberOfSkeletonsToSpawn, transform, Vector3.zero, Quaternion.identity);

            for (int skeletonIndex = 0; skeletonIndex < numberOfSkeletonsToSpawn; skeletonIndex++)
            {
                var skeleton = skeletons[skeletonIndex];
                skeleton.transform.SetPositionAndRotation(skeletonDatas[skeletonIndex].global_position, Quaternion.Euler(skeletonDatas[skeletonIndex].global_rotation));

                var bones = skeleton.transform.GetComponentsInChildren<Transform>().Where(x => Enum.GetNames(typeof(SkeletonBodypart)).Contains(x.name)).ToList();
                for (int boneIndex = 0; boneIndex < 18; boneIndex++)
                {
                    bones[boneIndex].SetLocalPositionAndRotation(skeletonDatas[skeletonIndex].bone_local_positions[boneIndex], Quaternion.Euler(skeletonDatas[skeletonIndex].bone_local_rotations[boneIndex]));
                }

                if (skeletonIndex % 5 == 0) await UniTask.NextFrame();
            }

            stopwatch.Stop();
            EverestPlugin.LogDebug($"Spawned {skeletonDatas.Length} skeletons in {stopwatch.ElapsedMilliseconds} ms.");
            UIHandler.Instance.Toast($"Skeletons spawned successfully! Took {stopwatch.ElapsedMilliseconds} ms.", Color.green, 5f, 5f);
        }

        private async Task<SkeletonData[]> GetSkeletonData()
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

            while (string.IsNullOrEmpty(serverResponseIdentifier))
            {
                if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SERVER_RESPONSE_IDENTIFIER_KEY, out var identifier))
                {
                    serverResponseIdentifier = identifier as string;
                }

                await UniTask.Delay(50);
            }

            EverestPlugin.LogDebug($"Received identifier UUID from host: {serverResponseIdentifier}");

            var serverResponse = await EverestClient.RetrieveAsync(serverResponseIdentifier);

            return serverResponse.data;
        }

        private void SyncServerResponseIdentifier(string identifier)
        {
            EverestPlugin.LogDebug($"Syncing server response identifier: {identifier}");
            this.serverResponseIdentifier = identifier;

            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { SERVER_RESPONSE_IDENTIFIER_KEY, identifier }
            });
        }
    }
}
