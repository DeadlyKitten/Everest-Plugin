using System;
using System.Diagnostics;
using System.Linq;
using Cysharp.Threading.Tasks;
using Everest.Accessories;
using Everest.Api;
using Everest.Utilities;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Zorro.Core;

namespace Everest.Core
{
    public class SkeletonManager : MonoBehaviourPun
    {
        private const int MAX_ATTEMPTS_FOR_UUID = 20;
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
            EverestPlugin.LogDebug("Waiting to establish connection to room...");
            await UniTask.WaitUntil(() => PhotonNetwork.IsConnected && PhotonNetwork.InRoom);
            EverestPlugin.LogDebug($"Connection established as {(PhotonNetwork.IsMasterClient ? "Host" : "Client")}.");

            SkeletonData[] skeletonDatas = await GetSkeletonData();

            if (skeletonDatas == null || skeletonDatas.Length == 0)
            {
                EverestPlugin.LogWarning("No skeleton data found for this map.");
                UIHandler.Instance.Toast("No skeletons :(", Color.red, 5f, 3f);
                return;
            }
            EverestPlugin.LogDebug($"Received {skeletonDatas.Length} skeletons.");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var skeletons = await InstantiateSkeletons(skeletonDatas.Length, stopwatch);
            var numberOfSkeletons = skeletons.Length;

            for (int skeletonIndex = 0; skeletonIndex < numberOfSkeletons; skeletonIndex++)
            {
                var skeleton = skeletons[skeletonIndex];
                await PrepareSkeleton(skeletonDatas[skeletonIndex], skeleton);
            }

            stopwatch.Stop();
            EverestPlugin.LogDebug($"Summoned {numberOfSkeletons} skeletons in {stopwatch.ElapsedMilliseconds} ms.");
            UIHandler.Instance.Toast($"Skeletons spawned successfully! Took {stopwatch.ElapsedMilliseconds} ms.", Color.green, 5f, 5f);
        }

        private async UniTask<GameObject[]> InstantiateSkeletons(int skeletonDatasCount, Stopwatch stopwatch)
        {
            EverestPlugin.LogDebug("Instantiating skeletons...");
            var numberOfSkeletonsToSpawn = Math.Min(ConfigHandler.MaxSkeletons, skeletonDatasCount);
            var skeletons = await InstantiateAsync(skeletonPrefab, numberOfSkeletonsToSpawn, transform, Vector3.zero, Quaternion.identity);

            EverestPlugin.LogDebug($"Instantiated {numberOfSkeletonsToSpawn} in {stopwatch.ElapsedMilliseconds} ms");

            return skeletons;
        }

        private static async UniTask PrepareSkeleton(SkeletonData skeletonData, GameObject skeleton)
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
                        skeleton.SetActive(false);
                        return;
                    }
                }
            }

            var steamId = skeletonData.steam_id;
            await TryAddAccessory(skeleton, steamId);
        }

        private static async UniTask TryAddAccessory(GameObject skeleton, string steamId)
        {
            if (string.IsNullOrEmpty(steamId)) return;

            var accessoryResult = await AccessoryManager.TryGetAccessoryForSteamId(steamId);
            if (accessoryResult.success)
            {
                var accessory = accessoryResult.accessory;
                accessory.transform.SetParent(skeleton.transform.FindChildRecursive(accessory.bone));
                accessory.transform.SetLocalPositionAndRotation(accessory.localPosition, accessory.localRotation);
                accessory.transform.localScale = Vector3.one;
            }
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
                    serverResponseIdentifier = identifier as string;
                    break;
                }

                await UniTask.Delay(50);
            }

            if (string.IsNullOrEmpty(serverResponseIdentifier))
            {
                EverestPlugin.LogWarning("Failed to retrieve server response identifier from host after multiple attempts.");
                return await RetrieveSkeletonDatasAsHost();
            }

            EverestPlugin.LogDebug($"Received identifier UUID from host: {serverResponseIdentifier}");

            var serverResponse = await EverestClient.RetrieveAsync(serverResponseIdentifier);

            return serverResponse.data;
        }

        private void SyncServerResponseIdentifier(string identifier)
        {
            EverestPlugin.LogDebug($"Syncing server response identifier: {identifier}");
            serverResponseIdentifier = identifier;

            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { SERVER_RESPONSE_IDENTIFIER_KEY, identifier }
            });
        }
    }
}
