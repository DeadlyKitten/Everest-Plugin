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
        private static ConfigEntry<int> skeletonDrawDistance;
        private static ConfigEntry<float> cullingUpdateFrequency;

        public static bool Enabled => enabled.Value;
        public static int MaxSkeletons => numSkeletons.Value;
        public static bool AllowUploads => allowUploads.Value;
        public static bool ShowToasts => showToasts.Value;
        public static int SkeletonDrawDistance => skeletonDrawDistance.Value;
        public static float CullingUpdateFrequency => cullingUpdateFrequency.Value;


        public static void Initialize()
        {
            config = new ConfigFile(Path.Combine(Paths.ConfigPath, "Everest.cfg"), true);

            enabled = config.Bind("General", "Enabled", true, "Enable or disable the Everest plugin.");
            numSkeletons = config.Bind("General", "MaxSkeletons", 100, "Number of skeletons to spawn.");
            allowUploads = config.Bind("General", "AllowUploads", true, "Allow uploading your own skeletons.");
            showToasts = config.Bind("UI", "ShowToasts", true, "Enable or disable toast notifications in the UI.");
            skeletonDrawDistance = config.Bind("Performance", "SkeletonDrawDistance", 100, "Maximum distance (in units) at which skeletons are drawn. Adjust this to improve performance if needed.");
            cullingUpdateFrequency = config.Bind("Performance", "CullingUpdateFrequency", 1.0f, "Frequency (in seconds) at which the culling system updates. Lower values may improve responsiveness decrease performance.");

            EverestPlugin.LogInfo("Configuration loaded successfully.");
            EverestPlugin.LogInfo($"Enabled: {Enabled}");
            EverestPlugin.LogInfo(message: $"Max Skeletons: {MaxSkeletons}");
            EverestPlugin.LogInfo($"Allow Uploads: {AllowUploads}");
            EverestPlugin.LogInfo($"Show Toasts: {ShowToasts}");
            EverestPlugin.LogInfo($"Skeleton Draw Distance: {SkeletonDrawDistance}");
            EverestPlugin.LogInfo($"Culling Update Frequency: {CullingUpdateFrequency} seconds");
        }
    }
}
