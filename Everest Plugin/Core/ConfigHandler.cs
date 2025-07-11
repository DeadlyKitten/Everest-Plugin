using System;
using System.IO;
using System.Numerics;
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

        public static bool Enabled => enabled.Value;
        public static int MaxSkeletons => numSkeletons.Value;
        public static bool AllowUploads => allowUploads.Value;
        public static bool ShowToasts => showToasts.Value;


        public static void Initialize()
        {
            config = new ConfigFile(Path.Combine(Paths.ConfigPath, "Everest.cfg"), true);

            enabled = config.Bind("General", "Enabled", true, "Enable or disable the Everest plugin.");
            numSkeletons = config.Bind("General", "MaxSkeletons", 100, "Number of skeletons to spawn.");
            allowUploads = config.Bind("General", "AllowUploads", true, "Allow uploading your own skeletons.");
            showToasts = config.Bind("UI", "ShowToasts", true, "Enable or disable toast notifications in the UI.");

            EverestPlugin.LogInfo("Configuration loaded successfully.");
            EverestPlugin.LogInfo($"enabled: {Enabled}");
            EverestPlugin.LogInfo(message: $"numSkeletons: {MaxSkeletons}");
            EverestPlugin.LogInfo($"allowUploads: {AllowUploads}");
            EverestPlugin.LogInfo($"showToasts: {ShowToasts}");

        }
    }
}
