using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using Everest.Utilities;
using ExitGames.Client.Photon;
using HarmonyLib;
using Newtonsoft.Json;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Everest
{
    [BepInPlugin("com.steven.peak.everest", "Everest", "1.0.0")]
    public class EverestPlugin : BaseUnityPlugin
    {
        public static EverestPlugin Instance;

        private const byte INSTANTIATION_EVENT_CODE = 172;

        private void Awake()
        {
            Instance = this;

            var harmony = new Harmony("com.steven.peak.everest");
            harmony.PatchAll();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new Vector3Converter() }
            };

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            LogInfo(newScene.name);

            if (newScene.name.ToLower().StartsWith("level_") || newScene.name == "WilIsland")
            {
                new GameObject("SkeletonManager").AddComponent<SkeletonManager>();
            }
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
