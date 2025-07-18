using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace Everest.Core
{
    public static class ConfigHandler
    {
        private static ConfigFile config;

        private static ConfigEntry<bool> enabled;
        private static ConfigEntry<int> numSkeletons;
        private static ConfigEntry<bool> allowUploads;
        private static ConfigEntry<bool> showToasts;

        private static ConfigEntry<float> cullingDistance;

        public static bool Enabled => enabled.Value;
        public static int MaxSkeletons => numSkeletons.Value;
        public static bool AllowUploads => allowUploads.Value;
        public static bool ShowToasts => showToasts.Value;

        public static float CullingDistance => cullingDistance.Value;


        public static void Initialize()
        {
            config = new ConfigFile(Path.Combine(Paths.ConfigPath, "Everest.cfg"), true);

            enabled = config.Bind("General", "Enabled", true, "Enable or disable the Everest plugin.");
            numSkeletons = config.Bind("General", "MaxSkeletons", 100, "Number of skeletons to spawn.");
            allowUploads = config.Bind("General", "AllowUploads", true, "Allow uploading your own skeletons.");
            cullingDistance = config.Bind("General", "CullingDistance", 150.0f, "The distance the skeletons will render at");
            showToasts = config.Bind("UI", "ShowToasts", true, "Enable or disable toast notifications in the UI.");

            EverestPlugin.LogInfo("Configuration loaded successfully.");
            EverestPlugin.LogInfo($"Enabled: {Enabled}");
            EverestPlugin.LogInfo(message: $"Max Skeletons: {MaxSkeletons}");
            EverestPlugin.LogInfo($"Culling distance: {CullingDistance}");
            EverestPlugin.LogInfo($"Allow Uploads: {AllowUploads}");
            EverestPlugin.LogInfo($"Show Toasts: {ShowToasts}");

        }
    }
}
