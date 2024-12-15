using BepInEx.Configuration;

namespace InteractableExfilsAPI.Helpers
{
    internal class Settings
    {
        public static ConfigEntry<bool> ExtractAreaStartsEnabled;
        public static ConfigEntry<bool> InactiveExtractsDisplayUnavailable;
        public static ConfigEntry<bool> DebugMode;
        
        public static void Init (ConfigFile config)
        {
            ExtractAreaStartsEnabled = config.Bind(
                "1: Settings",
                "Extract Timer Starts Automatically",
                false
            );

            InactiveExtractsDisplayUnavailable = config.Bind(
                "1: Settings",
                "Unnavailable Extracts Display as Unavailable",
                false
            );

            DebugMode = config.Bind(
                "2: Debug",
                "Enable Debug Actions",
                false
            );
        }
    }
}
