using System;
using System.Collections.Generic;
using System.Linq;
using Everest.Accessories;
using Everest.Api;
using Everest.Utilities;
using Unity.Collections;
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

        private void OnDisable()
        {
            RemoveAccessories();
            AllActiveSkeletons.Remove(this);
        }

        public void Initialize(SkeletonData data)
        {
            Nickname = data.nickname;
            gameObject.name = Nickname;
            Timestamp = DateTime.Parse(data.timestamp);
            TryAddAccessory(data.steam_id);

            for (var i = 0; i < Bones.Length; i++)
            {
                Bones[i].SetLocalPositionAndRotation(
                    data.bone_local_positions[i],
                    Quaternion.Euler(data.bone_local_rotations[i]));
            }

            transform.SetPositionAndRotation(data.global_position, Quaternion.Euler(data.global_rotation));
        }

        private void TryAddAccessory(string steamId)
        {
            if (string.IsNullOrEmpty(steamId)) return;

            if (AccessoryManager.TryGetAccessoryForSteamId(steamId, out var accessory))
            {
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
