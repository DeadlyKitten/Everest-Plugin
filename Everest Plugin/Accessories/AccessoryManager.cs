using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using AssetBundle = UnityEngine.AssetBundle;
using GameObject = UnityEngine.GameObject;

namespace Everest.Accessories
{
    public class AccessoryManager
    {
        private static Dictionary<string, SkeletonAccessory> _accessories = new Dictionary<string, SkeletonAccessory>();

        public static async UniTaskVoid Initialize()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Everest.Resources.accessories.bundle");

            if (stream == null)
            {
                EverestPlugin.LogError("Accessories AssetBundle not found in resources.");
                return;
            }

            var assetBundle = await AssetBundle.LoadFromStreamAsync(stream);
            var request = assetBundle.LoadAllAssetsAsync();
            await UniTask.WaitUntil(() => request.isDone);

            foreach (var accessoryGO in request.allAssets.Where(x => x is GameObject))
            {
                var accessory = ((GameObject)accessoryGO).GetComponent<SkeletonAccessory>();

                if (!accessory)
                {
                    EverestPlugin.LogError($"Accessory component not found on {accessoryGO.name}");
                    continue;
                }

                _accessories.Add(accessory.SteamId, accessory);

                EverestPlugin.LogDebug($"Added accessory for Steam ID: {accessory.SteamId}");
            }

            await assetBundle.UnloadAsync(false);

            stopwatch.Stop();
            EverestPlugin.LogDebug($"AssetBundleManager initialized in {stopwatch.ElapsedMilliseconds} ms with {_accessories.Count} accessories loaded.");
        }

        public static async UniTask<(bool success, SkeletonAccessory accessory)> TryGetAccessoryForSteamId(string steamId)
        {
            if (_accessories.TryGetValue(steamId, out var prefab))
            {
                var instance = (await UnityEngine.Object.InstantiateAsync(prefab)).FirstOrDefault();
                if (instance == null)
                {
                    EverestPlugin.LogError($"Failed to instantiate accessory for Steam ID {steamId}.");
                    return (false, null);
                }
                EverestPlugin.LogDebug($"Successfully instantiated accessory for Steam ID {steamId}.");
                return (true, instance);
            }

            return (false, null);
        }
    }
}
