using System;
using System.Collections.Generic;
using System.Linq;
using Everest.Accessories;
using Everest.Api;
using Everest.UI;
using Everest.Utilities;
using Unity.Collections;
using UnityEngine;
using Zorro.Core;

namespace Everest.Core
{
    public class Skeleton : MonoBehaviour
    {
        public static int Count;
        public static Skeleton[] AllActiveSkeletons { get; private set; } = new Skeleton[ConfigHandler.MaxVisibleSkeletons];

        public static event Action<Skeleton> OnSkeletonSpawned;
        public static event Action<Skeleton> OnSkeletonDespawned;

        public Transform HeadBone => Bones[(int)SkeletonBodypart.Head];
        public Vector3 HeadBonePosition = Vector3.zero;
        public string Nickname { get; private set; }
        public DateTime Timestamp { get; private set; }
        public Transform[] Bones { get; private set; }

        public int ArrayIndex;
        public SkeletonNametag ActiveNametag;
        public int LastRenderedFrame;

        private List<SkeletonAccessory> _accessories = new List<SkeletonAccessory>();

        private void Awake()
        {
            Bones = transform.GetComponentsInChildren<Transform>()
                .Where(x => Enum.GetNames(typeof(SkeletonBodypart))
                .Contains(x.name))
                .ToArray();
        }

        private void OnEnable()
        {
            ArrayIndex = Count;
            AllActiveSkeletons[Count] = this;
            Count++;
        }

        private void OnDisable()
        {
            RemoveAccessories();
            Count--;
            var lastIndex = Count;
            var lastSkeleton = AllActiveSkeletons[lastIndex];

            lastSkeleton.ArrayIndex = ArrayIndex;
            AllActiveSkeletons[ArrayIndex] = lastSkeleton;

            OnSkeletonDespawned?.Invoke(this);
        }

        public void Initialize(SkeletonData data)
        {
            Nickname = data.nickname;
            gameObject.name = Nickname;
            Timestamp = data.timestamp;
            TryAddAccessory(data.steam_id);

            for (var i = 0; i < Bones.Length; i++)
            {
                Bones[i].SetLocalPositionAndRotation(
                    data.bone_local_positions[i],
                    data.bone_local_rotations[i]);
            }

            transform.SetPositionAndRotation(data.global_position, Quaternion.Euler(data.global_rotation));

            HeadBonePosition = HeadBone.position;
            OnSkeletonSpawned?.Invoke(this);
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
