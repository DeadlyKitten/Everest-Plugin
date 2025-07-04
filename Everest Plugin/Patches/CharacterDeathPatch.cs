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
        private static void Postfix(Character __instance)
        {
            if (!__instance == Character.localCharacter) return;

            var steamId = SteamUser.GetSteamID().ToString();
            var mapId = GameHandler.GetService<NextLevelService>().Data.Value.CurrentLevelIndex;
            var mapSegment = (int) MapHandler.Instance.GetCurrentSegment();

            var globalPosition = __instance.transform.position;
            var globalRotation = __instance.transform.rotation.eulerAngles;

            var (boneLocalPositions, boneLocalRotations) = GenerateLocalTransformData(__instance);

            var requestPayload = new SubmissionRequest(steamId, mapId, mapSegment, globalPosition, globalRotation, boneLocalPositions, boneLocalRotations);

            EverestClient.SubmitDeath(requestPayload).Forget();
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
