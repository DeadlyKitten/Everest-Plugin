using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using Cysharp.Threading.Tasks;
using Everest.Accessories;
using Everest.Core;
using Everest.Utilities;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;

namespace Everest
{
    [BepInPlugin("com.steven.peak.everest", "Everest", "0.2.0")]
    public class EverestPlugin : BaseUnityPlugin
    {
        public static EverestPlugin Instance;

        private const byte INSTANTIATION_EVENT_CODE = 172;

        private void Awake()
        {
            Instance = this;

            ConfigHandler.Initialize();

            if (!ConfigHandler.Enabled)
            {
                LogInfo("Everest is disabled in the configuration. Exiting...");
                return;
            }

            if (ConfigHandler.AllowUploads)
            {
                var harmony = new Harmony("com.steven.peak.everest");
                harmony.PatchAll();
            }
            else LogInfo("Uploads are disabled in the configuration. Patching skipped.");

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Vector3Converter() }
            };

            if (!PlayerLoopHelper.IsInjectedUniTaskPlayerLoop())
            {
                var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
                PlayerLoopHelper.Initialize(ref playerLoop);
            }

            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            if (ConfigHandler.ShowToasts) new GameObject("Everest UI Manager").AddComponent<UIHandler>();

            SkeletonManager.LoadComputeShader().Forget();
            SkeletonManager.LoadSkeletonPrefab().Forget();

            AccessoryManager.Initialize().Forget();
            TombstoneHandler.Initialize().Forget();

            LogInfo("Everest Initialized");
        }



        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            LogInfo(newScene.name);

            if (newScene.name.ToLower() == "title") 
                UIHandler.Instance.Toast("Welcome to Everest!", Color.white, 7f, 3f);

            if (newScene.name.ToLower().StartsWith("level_") || newScene.name == "WilIsland")
                new GameObject("SkeletonManager").AddComponent<SkeletonManager>();
        }

        #region logging
        internal static void LogDebug(string message) => Instance.Log(message, LogLevel.Debug);
        internal static void LogInfo(string message) => Instance.Log(message, LogLevel.Info);
        internal static void LogWarning(string message) => Instance.Log(message, LogLevel.Warning);
        internal static void LogError(string message) => Instance.Log(message, LogLevel.Error);
        internal static void LogError(Exception ex) => Instance.Log($"{ex.Message}\n{ex.StackTrace}", LogLevel.Error);
        private void Log(string message, LogLevel logLevel) => Logger.Log(logLevel, message);
        #endregion
    }
}
