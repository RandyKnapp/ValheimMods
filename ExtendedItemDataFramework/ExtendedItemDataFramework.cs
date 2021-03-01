using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ExtendedItemDataFramework
{
    [BepInPlugin("randyknapp.mods.extendeditemdataframework", "Extended Item Data Framework", "1.0.0")]
    public class ExtendedItemDataFramework : BaseUnityPlugin
    {
        private static ConfigEntry<bool> _enabledConfig;

        public static bool Enabled => _enabledConfig != null && _enabledConfig.Value;

        private Harmony _harmony;

        private void Awake()
        {
            _enabledConfig = Config.Bind("General", "Enabled", true, "Turn off to disable this mod. When uninstalling, load and quit a game once with this mod disabled.");

            ExtendedItemData.NewExtendedItemData += UniqueItemData.OnNewExtendedItemData;
            ExtendedItemData.LoadExtendedItemData += UniqueItemData.OnLoadExtendedItemData;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        private void OnDestroy()
        {
            ExtendedItemData.NewExtendedItemData -= UniqueItemData.OnNewExtendedItemData;
            ExtendedItemData.LoadExtendedItemData -= UniqueItemData.OnLoadExtendedItemData;

            _harmony?.UnpatchAll();
        }

        
    }
}
