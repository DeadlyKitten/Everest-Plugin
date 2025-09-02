using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Everest.Accessories;
using Everest.Api;
using Everest.Utilities;
using UnityEngine;
using Zorro.Core;

namespace Everest.Core
{
    public class Skeleton : MonoBehaviour
    {
        public static List<Skeleton> AllActiveSkeletons { get; private set; } = new List<Skeleton>();

        public Transform HeadBone => Bones[(int)SkeletonBodypart.Head];
        public string Nickname { get; private set; }
        public DateTime Timestamp { get; private set; }
        public Transform[] Bones { get; private set; }

        private List<SkeletonAccessory> _accessories = new List<SkeletonAccessory>();

        private void Awake()
        {
            Bones = transform.GetComponentsInChildren<Transform>()
                .Where(x => Enum.GetNames(typeof(SkeletonBodypart))
                .Contains(x.name))
                .ToArray();
        }

        private void OnEnable() => AllActiveSkeletons.Add(this);

        private void OnDisable() => AllActiveSkeletons.Remove(this);

        public async UniTask Initialize(SkeletonData data)
        {
            Nickname = data.nickname;
            gameObject.name = Nickname;
            Timestamp = DateTime.Parse(data.timestamp);
            TryAddAccessory(data.steam_id);
        }

        private void TryAddAccessory(string steamId)
        {
            if (string.IsNullOrEmpty(steamId)) return;

            if (AccessoryManager.TryGetAccessoryForSteamId(steamId, out var accessory))
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
