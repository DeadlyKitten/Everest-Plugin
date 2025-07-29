using System.IO;
using BepInEx;
using BepInEx.Configuration;

namespace Everest.Core
{
    public static class ConfigHandler
    {
        private static ConfigFile config;

        private static ConfigEntry<bool> enabled;
        private static ConfigEntry<int> maxSkeletons;
        private static ConfigEntry<bool> allowUploads;
        private static ConfigEntry<bool> hideFloaters;
        private static ConfigEntry<bool> excludeNearCrashSite;
        private static ConfigEntry<bool> excludeNearCampfires;
        private static ConfigEntry<bool> showToasts;
        private static ConfigEntry<int> skeletonDrawDistance;
        private static ConfigEntry<float> cullingUpdateFrequency;
        private static ConfigEntry<int> maxVisibleSkeletons;

        public static bool Enabled => enabled.Value;
        public static int MaxSkeletons => maxSkeletons.Value;
        public static bool AllowUploads => allowUploads.Value;
        public static bool HideFloaters => hideFloaters.Value;
        public static bool ExcludeNearCrashSite => excludeNearCrashSite.Value;
        public static bool ExcludeNearCampfires => excludeNearCampfires.Value;
        public static bool ShowToasts => showToasts.Value;
        public static int SkeletonDrawDistance => skeletonDrawDistance.Value;
        public static float CullingUpdateFrequency => cullingUpdateFrequency.Value;
        public static int MaxVisibleSkeletons => maxVisibleSkeletons.Value;


        public static void Initialize()
        {
            config = new ConfigFile(Path.Combine(Paths.ConfigPath, "Everest.cfg"), true);

            enabled = config.Bind("General", "Enabled", true, "Enable or disable the Everest plugin.");
            maxSkeletons = config.Bind("General", "MaxSkeletons", 100, "Number of skeletons to spawn.");
            allowUploads = config.Bind("General", "AllowUploads", true, "Allow uploading your own skeletons.");
            hideFloaters = config.Bind("General", "HideFloaters", true, "Attempt to hide skeletons that are floating in the air without a valid ground position.");
            excludeNearCrashSite = config.Bind("General", "ExcludeNearCrashSite", false, "Exclude skeletons that are near the crash site.");
            excludeNearCampfires = config.Bind("General", "ExcludeNearCampfires", false, "Exclude skeletons that are near campfires.");
            showToasts = config.Bind("UI", "ShowToasts", true, "Enable or disable toast notifications in the UI.");
            skeletonDrawDistance = config.Bind("Performance", "SkeletonDrawDistance", 150, "Maximum distance (in units) at which skeletons are drawn.");
            cullingUpdateFrequency = config.Bind("Performance", "CullingUpdateFrequency", 1.0f, "Frequency (in seconds) at which the culling system updates. Lower values may improve responsiveness decrease performance.");
            maxVisibleSkeletons = config.Bind("Performance", "MaxVisibleSkeletons", 100, "Maximum number of skeletons that can be visible at once.");

            EverestPlugin.LogInfo("Configuration loaded successfully.");
            EverestPlugin.LogInfo($"Enabled: {Enabled}");
            EverestPlugin.LogInfo(message: $"Max Skeletons: {MaxSkeletons}");
            EverestPlugin.LogInfo($"Allow Uploads: {AllowUploads}");
            EverestPlugin.LogInfo($"Hide Floaters: {HideFloaters}");
            EverestPlugin.LogInfo($"Show Toasts: {ShowToasts}");
            EverestPlugin.LogInfo($"Skeleton Draw Distance: {SkeletonDrawDistance}");
            EverestPlugin.LogInfo($"Culling Update Frequency: {CullingUpdateFrequency} seconds");
            EverestPlugin.LogInfo($"Max Visible Skeletons: {maxVisibleSkeletons.Value}");
        }
    }
}
