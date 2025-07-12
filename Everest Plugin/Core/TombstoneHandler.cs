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
        public static Vector3 TombstonePosition => _tombstonePosition;

        private static GameObject _tombstone;

        private static readonly Vector3 _tombstonePosition = new Vector3(12f, 4.5f, -363f);
        private static readonly Quaternion _tombstoneRotation = Quaternion.Euler(0f, 160f, 4f);

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

        private static void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            var tombstone = GameObject.Instantiate(_tombstone, _tombstonePosition, _tombstoneRotation);
            tombstone.name = "Everest Tombstone";
            tombstone.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            tombstone.SetLayerRecursivly(20);
        }
    }
}
