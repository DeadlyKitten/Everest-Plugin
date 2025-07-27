using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using Cysharp.Threading.Tasks;
using Everest.Accessories;
using Everest.Api;
using Everest.Core;
using Everest.Patches;
using Everest.Utilities;
using HarmonyLib;
using MessagePack;
using MessagePack.Formatters;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;
using Vector3Formatter = Everest.Formatters.Vector3Formatter;

namespace Everest
{
    [BepInPlugin("com.steven.peak.everest", "Everest", "0.2.0")]
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
                var originalMethod = typeof(Character).GetMethod(nameof(Character.RPCA_Die), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                var patchMethod = typeof(CharacterDeathPatch).GetMethod(nameof(CharacterDeathPatch.Prefix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

                var harmony = new Harmony("com.steven.peak.everest");
                harmony.Patch(original: originalMethod, prefix: new HarmonyMethod(patchMethod));
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

            SkeletonManager.LoadComputeShaderAsync().Forget();
            SkeletonManager.LoadSkeletonPrefabAsync().Forget();

            AccessoryManager.Initialize().Forget();
            TombstoneHandler.Initialize().Forget();

            PrepareMessagePackResolver();

            LogInfo("Everest Initialized");
        }

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
            var serverStatus = await EverestClient.RetrieveServerStatus(Info.Metadata.Version.ToString());

            var message = new StringBuilder();
            message.AppendLine($"Everest Server Status: {serverStatus?.status ?? "offline"}");
            if (!string.IsNullOrEmpty(serverStatus.updateInfo))
                message.AppendLine(serverStatus.updateInfo);
            if (!string.IsNullOrEmpty(serverStatus.message))
                message.AppendLine(serverStatus.message);

            var color = serverStatus.status == "online" ? string.IsNullOrEmpty(serverStatus.updateInfo) ? Color.green : Color.yellow : Color.red;

            UIHandler.Instance.Toast(message.ToString(), color, 7f, 3f);
        }

        private void PrepareMessagePackResolver()
        {
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                new IMessagePackFormatter[] { new Vector3Formatter() },
                new IFormatterResolver[] { MessagePack.Resolvers.StandardResolverAllowPrivate.Instance }
            );

            var options = MessagePack.MessagePackSerializerOptions.Standard.WithResolver(resolver);

            MessagePack.MessagePackSerializer.DefaultOptions = options;
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
