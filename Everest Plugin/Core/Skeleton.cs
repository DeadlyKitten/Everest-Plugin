using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Everest.Accessories;
using UnityEngine;
using Zorro.Core;

namespace Everest.Core
{
    public class Skeleton : MonoBehaviour
    {
        private List<SkeletonAccessory> _accessories = new List<SkeletonAccessory>();

        public async UniTask TryAddAccessory(string steamId)
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
