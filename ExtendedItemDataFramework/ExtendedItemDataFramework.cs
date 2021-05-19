using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace ExtendedItemDataFramework
{
    [BepInPlugin(PluginId, "Extended Item Data Framework", Version)]
    public class ExtendedItemDataFramework : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.extendeditemdataframework";
        public const string Version = "1.0.1";

        private static ConfigEntry<bool> _enabledConfig;
        private static ConfigEntry<bool> _loggingEnabled;
        public static ConfigEntry<bool> DisplayUniqueItemIDInTooltip;

        public static bool Enabled => _enabledConfig != null && _enabledConfig.Value;

        private static ExtendedItemDataFramework _instance;
        private Harmony _harmony;

        private void Awake()
        {
            _instance = this;
            _enabledConfig = Config.Bind("General", "Enabled", true, "Turn off to disable this mod. When uninstalling, load and quit a game once with this option set to false.");
            _loggingEnabled = Config.Bind("General", "Logging Enabled", false, "Enables log output from the mod.");
            DisplayUniqueItemIDInTooltip = Config.Bind("General", "Display UniqueItemID in Tooltip", false, "Displays the item's unique id in magenta text at the bottom of the tooltip.");

            ExtendedItemData.NewExtendedItemData += UniqueItemData.OnNewExtendedItemData;
            ExtendedItemData.LoadExtendedItemData += UniqueItemData.OnLoadExtendedItemData;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
        }

        private void OnDestroy()
        {
            ExtendedItemData.NewExtendedItemData -= UniqueItemData.OnNewExtendedItemData;
            ExtendedItemData.LoadExtendedItemData -= UniqueItemData.OnLoadExtendedItemData;

            _harmony?.UnpatchAll(PluginId);
            _instance = null;
        }

        public static void Log(string message)
        {
            if (_loggingEnabled.Value)
            {
                _instance.Logger.LogMessage(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (_loggingEnabled.Value)
            {
                _instance.Logger.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (_loggingEnabled.Value)
            {
                _instance.Logger.LogError(message);
            }
        }
    }
}
