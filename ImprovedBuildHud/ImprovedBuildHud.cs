using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ImprovedBuildHud
{
    public static class ImprovedBuildHudConfig
    {
        public static ConfigEntry<string> InventoryAmountFormat;
        public static ConfigEntry<string> InventoryAmountColor;
        public static ConfigEntry<string> CanBuildAmountFormat;
        public static ConfigEntry<string> CanBuildAmountColor;
    }

    [BepInPlugin("randyknapp.mods.improvedbuildhud", "Improved Build HUD", "1.0.1")]
    [BepInProcess("valheim.exe")]
    [BepInDependency("aedenthorn.CraftFromContainers", BepInDependency.DependencyFlags.SoftDependency)]
    public class ImprovedBuildHud : BaseUnityPlugin
    {
        private Harmony _harmony;

        //public static bool CraftFromContainersInstalledAndActive;

        private void Awake()
        {
            ImprovedBuildHudConfig.InventoryAmountFormat = Config.Bind("General", "Inventory Amount Format", "({0})", "Format for the amount of items in the player inventory to show after the required amount. Uses standard C# format rules. Leave empty to hide altogether.");
            ImprovedBuildHudConfig.InventoryAmountColor = Config.Bind("General", "Inventory Amount Color", "lightblue", "Color to set the inventory amount after the requirement amount. Leave empty to set no color. You can use the #XXXXXX hex color format.");
            ImprovedBuildHudConfig.CanBuildAmountFormat = Config.Bind("General", "Can Build Amount Color", "({0})", "Format for the amount of times you can build the currently selected item with your current inventory. Uses standard C# format rules. Leave empty to hide altogether.");
            ImprovedBuildHudConfig.CanBuildAmountColor = Config.Bind("General", "Can Build Amount Color", "white", "Color to set the can-build amount. Leave empty to set no color. You can use the #XXXXXX hex color format.");
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            /*CraftFromContainersInstalledAndActive = false;
            var bepInExManager = GameObject.Find("BepInEx_Manager");
            var plugins = bepInExManager.GetComponentsInChildren<BaseUnityPlugin>();
            foreach (var plugin in plugins)
            {
                if (plugin.Info.Metadata.GUID == "aedenthorn.CraftFromContainers")
                {
                    CraftFromContainersInstalledAndActive = CraftFromContainers.BepInExPlugin.modEnabled.Value;
                }
            }*/
        }

        private void OnDestroy()
        {
            //CraftFromContainersInstalledAndActive = false;
            _harmony.UnpatchAll();
        }
    }
}
