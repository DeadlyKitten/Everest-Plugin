using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Everest.Accessories;
using Everest.Api;
using UnityEngine;
using Zorro.Core;

namespace Everest.Core
{
    public class Skeleton : MonoBehaviour
    {
        public static List<Skeleton> AllSkeletons { get; private set; } = new List<Skeleton>();
        public static List<Skeleton> AllActiveSkeletons { get; private set; } = new List<Skeleton>();

        public Transform HeadBone { get; set; }
        public string Nickname { get; private set; }
        public DateTime Timestamp { get; private set; }

        private SkinnedMeshRenderer _meshRenderer;
        private List<SkeletonAccessory> _accessories = new List<SkeletonAccessory>();

        private void Awake()
        {
            _meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            HeadBone = transform.FindChildRecursive("Head");

            AllSkeletons.Add(this);
        }

        private void OnDestroy() => AllSkeletons.Remove(this);

        private void OnEnable() => AllActiveSkeletons.Add(this);

        private void OnDisable() => AllActiveSkeletons.Remove(this);

        public async UniTask Initialize(SkeletonData data)
        {
            Nickname = data.nickname;
            gameObject.name = Nickname;
            Timestamp = DateTime.Parse(data.timestamp);
            await TryAddAccessory(data.steam_id);
        }

        private async UniTask TryAddAccessory(string steamId)
        {
            if (string.IsNullOrEmpty(steamId)) return;

            var accessoryResult = await AccessoryManager.TryGetAccessoryForSteamId(steamId);
            if (accessoryResult.success)
            {
                var accessory = accessoryResult.accessory;
                accessory.transform.SetParent(transform.FindChildRecursive(accessory.bone));
                accessory.transform.SetLocalPositionAndRotation(accessory.localPosition, accessory.localRotation);
                accessory.transform.localScale = Vector3.one;

                _accessories.Add(accessory);
            }
        }

        public void RemoveAccessories()
        {
            foreach (var accessory in _accessories)
            {
                if (accessory != null)
                {
                    Destroy(accessory.gameObject);
                }
            }

            _accessories.Clear();
        }
    }
}
