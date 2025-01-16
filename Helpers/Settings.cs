using System;
using BepInEx.Configuration;
using InteractableExfilsAPI.Singletons;

namespace InteractableExfilsAPI.Helpers
{
    public class Settings
    {
        public static ConfigEntry<bool> AutoExtractEnabled;
        public static ConfigEntry<bool> InactiveExtractsDisplayUnavailable;
        public static ConfigEntry<bool> DebugMode;

        private static void OnSettingsChanged(object sender, EventArgs e)
        {
            InteractableExfilsService.RefreshPrompt();
        }

        public static void Init(ConfigFile config)
        {
            AutoExtractEnabled = config.Bind(
                "1: Settings",
                "Auto-Extract",
                false,
                new ConfigDescription("Extract Timer Starts Automatically", null, new ConfigurationManagerAttributes { })
            );

            InactiveExtractsDisplayUnavailable = config.Bind(
                "1: Settings",
                "Show unavailable extracts",
                false,
                new ConfigDescription("Unavailable Extracts Display as Unavailable", null, new ConfigurationManagerAttributes { })
            );

            DebugMode = config.Bind(
                "2: Debug",
                "Enable Debug Actions",
                false,
                new ConfigDescription("", null, new ConfigurationManagerAttributes { IsAdvanced = true })
            );

            config.SettingChanged += OnSettingsChanged;
        }
    }
}
