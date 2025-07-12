using System;
using System.Diagnostics;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using UnityEngine.SceneManagement;
using UnityEngine;
using Zorro.Core;

namespace Everest.Core
{
    public class TombstoneHandler
    {
        private static GameObject _tombstone;

        public static async UniTaskVoid Initialize()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Everest.Resources.tombstone.bundle");

            if (stream == null)
            {
                EverestPlugin.LogError("Tombstone AssetBundle not found in resources.");
                return;
            }

            var assetBundle = await AssetBundle.LoadFromStreamAsync(stream);

            var request = assetBundle.LoadAssetAsync("Assets/Everest/tombstone.prefab");
            await UniTask.WaitUntil(() => request.isDone);

            _tombstone = request.asset as GameObject;

            EverestPlugin.LogDebug($"Tombstone prefab loaded: {_tombstone != null}");

            await assetBundle.UnloadAsync(false).ToUniTask();

            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            stopwatch.Stop();
            EverestPlugin.LogDebug($"TombstoneManager initialized in {stopwatch.ElapsedMilliseconds} ms.");
        }

        public static void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            var tombstone = GameObject.Instantiate(_tombstone, new Vector3(7.7f, 3f, -372f), Quaternion.Euler(0f, 96f, 350f));
            tombstone.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            tombstone.SetLayerRecursivly(20);
        }
    }
}
