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

namespace Everest
{
    public class SkeletonManager : MonoBehaviourPun
    {
        private const string SERVER_RESPONSE_IDENTIFIER_KEY = "serverResponseIdentifier";
        private string serverResponseIdentifier;

        public void Awake()
        {
            GenerateSkeletons().Forget();
        }

        private async UniTaskVoid GenerateSkeletons()
        {
            if (!PlayerLoopHelper.IsInjectedUniTaskPlayerLoop())
            {
                var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
                PlayerLoopHelper.Initialize(ref playerLoop);
            }

            await UniTask.WaitUntil(() => PhotonNetwork.IsConnected && PhotonNetwork.InRoom);

            SkeletonData[] skeletonDatas = await GetSkeletonData();

            if (skeletonDatas == null || skeletonDatas.Length == 0)
            {
                EverestPlugin.LogWarning("No skeleton data found for this map.");
                return;
            }
            else
            {
                EverestPlugin.LogInfo($"Received {skeletonDatas.Length} skeletons.");
            }

            var skeletonPrefab = await Resources.LoadAsync<GameObject>("Skeleton") as GameObject;

            if (skeletonPrefab == null)
            {
                EverestPlugin.LogError("Skeleton prefab not found in Resources.");
                return;
            }

            EverestPlugin.LogInfo("Instantiating skeletons...");
            var skeletons = await InstantiateAsync(skeletonPrefab, skeletonDatas.Length);

            for (int skeletonIndex = 0; skeletonIndex < skeletonDatas.Length; skeletonIndex++)
            {
                var skeleton = skeletons[skeletonIndex];
                skeleton.transform.SetPositionAndRotation(skeletonDatas[skeletonIndex].global_position, Quaternion.Euler(skeletonDatas[skeletonIndex].global_rotation));

                var bones = skeleton.transform.GetComponentsInChildren<Transform>().Where(x => Enum.GetNames(typeof(SkeletonBodypart)).Contains(x.name)).ToList();
                for (int boneIndex = 0; boneIndex < 18; boneIndex++)
                {
                    bones[boneIndex].SetLocalPositionAndRotation(skeletonDatas[skeletonIndex].bone_local_positions[boneIndex], Quaternion.Euler(skeletonDatas[skeletonIndex].bone_local_rotations[boneIndex]));
                }
            }
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
            var mapId = GameHandler.GetService<NextLevelService>().Data.Value.CurrentLevelIndex;
            var serverResponse = await EverestClient.RetrieveAsync(mapId);
            SyncServerResponseIdentifier(serverResponse.identifier);
            return serverResponse.data;
        }

        private async UniTask<SkeletonData[]> RetrieveSkeletonDatasAsClient()
        {
            while (string.IsNullOrEmpty(serverResponseIdentifier))
            {
                if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(SERVER_RESPONSE_IDENTIFIER_KEY, out var identifier))
                {
                    serverResponseIdentifier = identifier as string;
                }

                EverestPlugin.LogInfo("Waiting...");
                await UniTask.Delay(50);
            }

            EverestPlugin.LogInfo(serverResponseIdentifier);

            var serverResponse = await EverestClient.RetrieveAsync(serverResponseIdentifier);

            EverestPlugin.LogInfo(serverResponse.identifier);

            return serverResponse.data;
        }

        private void SyncServerResponseIdentifier(string identifier)
        {
            EverestPlugin.LogInfo($"Syncing server response identifier: {identifier}");
            this.serverResponseIdentifier = identifier;

            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { SERVER_RESPONSE_IDENTIFIER_KEY, identifier }
            });
        }
    }
}
