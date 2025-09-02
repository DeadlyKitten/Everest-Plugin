using System.IO;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

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

        private static ConfigEntry<bool> showSkeletonNametags;
        private static ConfigEntry<bool> showTimeSinceDeath;
        private static ConfigEntry<bool> showSecondsAlways;
        private static ConfigEntry<float> maxDistanceForVisibleNametag;
        private static ConfigEntry<float> minDistanceForVisibleNametag;
        private static ConfigEntry<float> maxAngleForVisibleNametag;
        private static ConfigEntry<float> maxNametagSize;
        private static ConfigEntry<float> minNametagSize;
        private static ConfigEntry<Color> nametagColor;
        private static ConfigEntry<float> nametagOutlineWidth;
        private static ConfigEntry<Color> nametagOutlineColor;

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
        public static bool ShowSkeletonNametags => showSkeletonNametags.Value;
        public static bool ShowTimeSinceDeath => showTimeSinceDeath.Value;
        public static bool ShowSecondsAlways => showSecondsAlways.Value;
        public static float MaxDistanceForVisibleNametag => maxDistanceForVisibleNametag.Value;
        public static float MinDistanceForVisibleNametag => minDistanceForVisibleNametag.Value;
        public static float MaxAngleForVisibleNametag => maxAngleForVisibleNametag.Value;
        public static float MaxNametagSize => maxNametagSize.Value;
        public static float MinNametagSize => minNametagSize.Value;
        public static Color NametagColor => nametagColor.Value;
        public static float NametagOutlineWidth => nametagOutlineWidth.Value;
        public static Color NametagOutlineColor => nametagOutlineColor.Value;


        public static void Initialize()
        {
            config = new ConfigFile(Path.Combine(Paths.ConfigPath, "Everest.cfg"), true);

            enabled = config.Bind("General", "Enabled", true, "Enable or disable the Everest plugin.");
            maxSkeletons = config.Bind("General", "MaxSkeletons", 100, "Number of skeletons to spawn.");
            allowUploads = config.Bind("General", "AllowUploads", true, "Allow uploading your own skeletons.");
            hideFloaters = config.Bind("General", "HideFloaters", true, "Attempt to hide skeletons that are floating in the air without a valid ground position.");
            excludeNearCrashSite = config.Bind("General", "ExcludeNearCrashSite", false, "Exclude skeletons that are near the crash site.");
            excludeNearCampfires = config.Bind("General", "ExcludeNearCampfires", false, "Exclude skeletons that are near campfires.");
            skeletonDrawDistance = config.Bind("Performance", "SkeletonDrawDistance", 150, "Maximum distance (in units) at which skeletons are drawn.");
            cullingUpdateFrequency = config.Bind("Performance", "CullingUpdateFrequency", 1.0f, "Frequency (in seconds) at which the culling system updates. Lower values may improve responsiveness decrease performance.");
            maxVisibleSkeletons = config.Bind("Performance", "MaxVisibleSkeletons", 100, "Maximum number of skeletons that can be visible at once.");
            showToasts = config.Bind("UI", "ShowToasts", true, "Enable or disable toast notifications in the UI.");
            showSkeletonNametags = config.Bind("UI", "ShowSkeletonNametags", true, "Enable or disable skeleton nametags.");
            showTimeSinceDeath = config.Bind("UI", "ShowTimeSinceDeath", true, "Enable or disable showing the time since death in skeleton nametags.");
            showSecondsAlways = config.Bind("UI", "ShowSecondsAlways", false, "Always show seconds in the time since death, even for recent deaths.");
            maxDistanceForVisibleNametag = config.Bind("UI", "MaxDistanceForVisibleNametag", 8f, "Maximum distance at which skeleton nametags are visible.");
            minDistanceForVisibleNametag = config.Bind("UI", "MinDistanceForVisibleNametag", 3f, "Minimum distance at which skeleton nametags are visible.");
            maxAngleForVisibleNametag = config.Bind("UI", "MaxAngleForVisibleNametag", 25f, "Maximum angle (in degrees) from the camera at which skeleton nametags are visible.");
            maxNametagSize = config.Bind("UI", "MaxNametagSize", 2.5f, "Maximum size of skeleton nametags.");
            minNametagSize = config.Bind("UI", "MinNametagSize", 1.2f, "Minimum size of skeleton nametags.");
            nametagColor = config.Bind("UI", "NametagColor", Color.white, "Color of skeleton nametags.");
            nametagOutlineWidth = config.Bind("UI", "NametagOutlineWidth", 0.05f, "Width of the outline around skeleton nametags.");
            nametagOutlineColor = config.Bind("UI", "NametagOutlineColor", Color.black, "Color of the outline around skeleton nametags.");

            EverestPlugin.LogInfo("Configuration loaded successfully.");
            EverestPlugin.LogInfo($"Enabled: {Enabled}");

            if (!Enabled) return;

            EverestPlugin.LogInfo($"Max Skeletons: {MaxSkeletons}");
            EverestPlugin.LogInfo($"Allow Uploads: {AllowUploads}");
            EverestPlugin.LogInfo($"Hide Floaters: {HideFloaters}");
            EverestPlugin.LogInfo($"Exclude Near Crash Site: {ExcludeNearCrashSite}");
            EverestPlugin.LogInfo($"Exclude Near Campfires: {ExcludeNearCampfires}");
            EverestPlugin.LogInfo($"Skeleton Draw Distance: {SkeletonDrawDistance}");
            EverestPlugin.LogInfo($"Culling Update Frequency: {CullingUpdateFrequency} seconds");
            EverestPlugin.LogInfo($"Max Visible Skeletons: {maxVisibleSkeletons.Value}");
            EverestPlugin.LogInfo($"Show Toasts: {ShowToasts}");
            EverestPlugin.LogInfo($"Show Skeleton Nametags: {ShowSkeletonNametags}");

            if (ShowSkeletonNametags)
            {
                EverestPlugin.LogInfo($"Show Time Since Death: {ShowTimeSinceDeath}");
                EverestPlugin.LogInfo($"Show Seconds Always: {ShowSecondsAlways}");
                EverestPlugin.LogInfo($"Max Distance For Visible Nametag: {MaxDistanceForVisibleNametag}");
                EverestPlugin.LogInfo($"Min Distance For Visible Nametag: {MinDistanceForVisibleNametag}");
                EverestPlugin.LogInfo($"Max Angle For Visible Nametag: {MaxAngleForVisibleNametag}");
                EverestPlugin.LogInfo($"Max Nametag Size: {MaxNametagSize}");
                EverestPlugin.LogInfo($"Min Nametag Size: {MinNametagSize}");
                EverestPlugin.LogInfo($"Nametag Color: {NametagColor}");
                EverestPlugin.LogInfo($"Nametag Outline Width: {NametagOutlineWidth}");
                EverestPlugin.LogInfo($"Nametag Outline Color: {NametagOutlineColor}");
            }
        }
    }
}
