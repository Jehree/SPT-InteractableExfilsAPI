using System;
using BepInEx.Configuration;
using EFT.UI;
using InteractableExfilsAPI.Singletons;

namespace InteractableExfilsAPI.Helpers
{
    internal class Settings
    {
        public static ConfigEntry<bool> ExtractAreaStartsEnabled;
        public static ConfigEntry<bool> InactiveExtractsDisplayUnavailable;
        public static ConfigEntry<bool> DebugMode;

        private static void OnSettingsChanged(object sender, EventArgs e)
        {
            Plugin.LogSource.LogInfo("BepInEx setting changed, refresh prompt");
            InteractableExfilsService.RefreshPrompt();
        }

        public static void Init(ConfigFile config)
        {
            ExtractAreaStartsEnabled = config.Bind(
                "Settings",
                "Auto-Extract",
                false,
                new ConfigDescription("Extract Timer Starts Automatically", null, new ConfigurationManagerAttributes { })
            );

            InactiveExtractsDisplayUnavailable = config.Bind(
                "Settings",
                "Show unavailable extracts",
                false,
                new ConfigDescription("Unavailable Extracts Display as Unavailable", null, new ConfigurationManagerAttributes { })
            );

            DebugMode = config.Bind(
                "Debug",
                "Enable Debug Actions",
                false,
                new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true })
            );

            config.SettingChanged += OnSettingsChanged;
        }
    }
}
