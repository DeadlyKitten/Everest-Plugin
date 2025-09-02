using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Everest.Accessories;
using Everest.Api;
using Everest.Utilities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
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

        public async UniTaskVoid Initialize(SkeletonData data)
        {
            Nickname = data.nickname;
            gameObject.name = Nickname;
            Timestamp = DateTime.Parse(data.timestamp);
            TryAddAccessory(data.steam_id);

            transform.SetPositionAndRotation(data.global_position, Quaternion.Euler(data.global_rotation));

            var positions = data.bone_local_positions.ToNativeArray(Allocator.TempJob);
            var rotations = data.bone_local_rotations.ToNativeArray(Allocator.TempJob);

            var job = new PoseSkeletonJob
            {
                Positions = positions,
                Rotations = rotations
            };

            var transformAccessArray = new TransformAccessArray(Bones);

            var handle = job.ScheduleByRef(transformAccessArray);

            await UniTask.WaitUntil(() => handle.IsCompleted);

            positions.Dispose();
            rotations.Dispose();
            transformAccessArray.Dispose();
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

        private struct PoseSkeletonJob : IJobParallelForTransform
        {
            public NativeArray<Vector3> Positions;
            public NativeArray<Vector3> Rotations;

            public void Execute(int index, TransformAccess transform)
            {
                transform.SetLocalPositionAndRotation(
                    Positions[index], 
                    Quaternion.Euler(Rotations[index])
                );
            }
        }
    }
}
