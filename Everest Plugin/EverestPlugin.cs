using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using Cysharp.Threading.Tasks;
using Everest.Accessories;
using Everest.Api;
using Everest.Core;
using Everest.UI;
using Everest.Utilities;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;

namespace Everest
{
    [BepInPlugin("com.steven.peak.everest", "Everest", "1.1.1")]
    public class EverestPlugin : BaseUnityPlugin
    {
        public static EverestPlugin Instance;

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
                Converters = new List<JsonConverter> { new Vector3Converter() },
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            if (!PlayerLoopHelper.IsInjectedUniTaskPlayerLoop())
            {
                var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
                PlayerLoopHelper.Initialize(ref playerLoop);
            }

            SceneManager.activeSceneChanged += OnActiveSceneChanged;

            if (ConfigHandler.ShowToasts) new GameObject("Everest UI Manager").AddComponent<ToastController>();

            SkeletonManager.LoadComputeShaderAsync().Forget();
            SkeletonManager.LoadSkeletonPrefabAsync().Forget();

            AccessoryManager.Initialize().Forget();
            TombstoneHandler.Initialize().Forget();

            LogInfo("Everest Initialized");
        }

#if DEBUG
        private void Update()
        {
            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                var currentScene = SceneManager.GetActiveScene().name.ToLower();
                if (currentScene.StartsWith("level_") || currentScene == "wilisland")
                {
                    Character.Die();
                }
            }
        }
#endif

        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            LogInfo(newScene.name);

            if (newScene.name.ToLower() == "airport")
                GetServerStatus().Forget();

            if (newScene.name.ToLower().StartsWith("level_") || newScene.name == "WilIsland")
                new GameObject("SkeletonManager").AddComponent<SkeletonManager>();
        }

        private async UniTaskVoid GetServerStatus()
        {
            LogWarning(Info.Metadata.Version.ToString());
            var serverStatus = await EverestClient.RetrieveServerStatusAsync(Info.Metadata.Version.ToString());

            var message = new StringBuilder();
            message.AppendLine($"Everest Server Status: {serverStatus?.status ?? "offline"}");
            if (!string.IsNullOrEmpty(serverStatus?.messageOfTheDay))
                message.AppendLine(serverStatus.messageOfTheDay);
            if (!string.IsNullOrEmpty(serverStatus?.updateInfo))
                message.AppendLine(serverStatus.updateInfo);

            var color = serverStatus.status == "online" ? string.IsNullOrEmpty(serverStatus.updateInfo) ? Color.green : Color.yellow : Color.red;

            ToastController.Instance.Toast(message.ToString(), color, 7f, 3f);
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
