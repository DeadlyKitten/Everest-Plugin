using System;
using System.Linq;
using Everest.Api;
using Everest.Utilities;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace Everest.Patches
{
    [HarmonyPatch(typeof(Character), nameof(Character.RPCA_Die))]
    public class CharacterDeathPatch
    {
        private static void Prefix(Character __instance)
        {
            if (__instance != Character.localCharacter) return;

            EverestPlugin.LogInfo("Whoops, you died!");

            var steamId = GetSteamId();
            var authTicket = GetAuthTicket();
            var mapId = GetMapId();
            var mapSegment = GetMapSegment();

            var isNearCampfire = CheckForCampfire(__instance);
            var isNearCrashSite = CheckForBingBong(__instance);

            var globalPosition = __instance.transform.position;
            var globalRotation = __instance.transform.rotation.eulerAngles;

            var (boneLocalPositions, boneLocalRotations) = GenerateLocalTransformData(__instance);

            var requestPayload = new SubmissionRequest()
            {
                SteamId = steamId,
                AuthSessionTicket = authTicket,
                MapId = mapId,
                MapSegment = mapSegment,
                IsNearCampfire = isNearCampfire,
                IsNearCrashSite = isNearCrashSite,
                GlobalPosition = globalPosition,
                GlobalRotation = globalRotation,
                BoneLocalPositions = boneLocalPositions,
                BoneLocalRotations = boneLocalRotations
            };

            EverestClient.SubmitDeathAsync(requestPayload).Forget();
        }

        private static string GetSteamId() => SteamUser.GetSteamID().ToString();

        private static int GetMapId() => GameHandler.GetService<NextLevelService>().Data.Value.CurrentLevelIndex;

        private static int GetMapSegment() => (int)MapHandler.Instance.GetCurrentSegment();

        private static string GetAuthTicket() => SteamAuthTicketService.GetSteamAuthTicket().Item1;

        private static bool CheckForCampfire(Character character)
        {
            var campfires = GameObject.FindObjectsByType<Campfire>(FindObjectsSortMode.None)
                .Where(x => x.advanceToSegment != Segment.Beach);

            foreach (var campfire in campfires)
            {
                if (Vector3.Distance(character.Center, campfire.transform.position) < campfire.moraleBoostRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CheckForBingBong(Character character)
        {
            var bingBongSpawner = GameObject.FindObjectsByType<SingleItemSpawner>(FindObjectsSortMode.None)
                .Where(SingleItemSpawner => SingleItemSpawner.prefab.name == "BingBong")
                .FirstOrDefault();

            if (!bingBongSpawner) return false;

            const float CRASH_SITE_RADIUS = 50f;
            return Vector3.Distance(character.Center, bingBongSpawner.transform.position) < CRASH_SITE_RADIUS;
        }

        private static (Vector3[] localPositions, Vector3[] localRotations) GenerateLocalTransformData(Character character)
        {
            if (character == null) return (null, null);

            var bones = character.transform.GetComponentsInChildren<Transform>().Where(x => Enum.GetNames(typeof(SkeletonBodypart)).Contains(x.name)).ToList();

            var positions = bones.Select(bone => bone.localPosition).ToArray();
            var rotations = bones.Select(bone => bone.localRotation.eulerAngles).ToArray();

            return (positions, rotations);
        }
    }
}