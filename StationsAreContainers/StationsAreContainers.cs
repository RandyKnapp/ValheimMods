using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace StationsAreContainers
{
    [BepInPlugin(PluginId, DisplayName, Version)]
    [BepInDependency("randyknapp.mods.improvedbuildhud", BepInDependency.DependencyFlags.SoftDependency)]
    public class StationsAreContainers : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.stationcontainers";
        public const string DisplayName = "Stations Are Containers";
        public const string Version = "1.0.0";

        public static ConfigEntry<bool> Workbench_Enabled;
        public static ConfigEntry<int>  Workbench_Width;
        public static ConfigEntry<int>  Workbench_Height;
        public static ConfigEntry<bool> Forge_Enabled;
        public static ConfigEntry<int>  Forge_Width;
        public static ConfigEntry<int>  Forge_Height;
        public static ConfigEntry<bool> Cauldron_Enabled;
        public static ConfigEntry<int>  Cauldron_Width;
        public static ConfigEntry<int>  Cauldron_Height;
        public static ConfigEntry<bool> Stonecutter_Enabled;
        public static ConfigEntry<int>  Stonecutter_Width;
        public static ConfigEntry<int>  Stonecutter_Height;
        public static ConfigEntry<bool> ArtisanTable_Enabled;
        public static ConfigEntry<int>  ArtisanTable_Width;
        public static ConfigEntry<int>  ArtisanTable_Height;

        private Harmony _harmony;

        [UsedImplicitly]
        private void Awake()
        {
            Workbench_Enabled = Config.Bind("Workbench", "Workbench Container Enabled", true);
            Workbench_Width = Config.Bind("Workbench", "Workbench Container Width", 6);
            Workbench_Height = Config.Bind("Workbench", "Workbench Container Height", 3);
            Forge_Enabled = Config.Bind("Forge", "Forge Container Enabled", true);
            Forge_Width = Config.Bind("Forge", "Forge Container Width", 6);
            Forge_Height = Config.Bind("Forge", "Forge Container Height", 3);
            Cauldron_Enabled = Config.Bind("Cauldron", "Cauldron Container Enabled", true);
            Cauldron_Width = Config.Bind("Cauldron", "Cauldron Container Width", 6);
            Cauldron_Height = Config.Bind("Cauldron", "Cauldron Container Height", 3);
            Stonecutter_Enabled = Config.Bind("Stonecutter", "Stonecutter Container Enabled", true);
            Stonecutter_Width = Config.Bind("Stonecutter", "Stonecutter Container Width", 6);
            Stonecutter_Height = Config.Bind("Stonecutter", "Stonecutter Container Height", 3);
            ArtisanTable_Enabled = Config.Bind("Artisan Table", "Artisan Table Container Enabled", true);
            ArtisanTable_Width = Config.Bind("Artisan Table", "Artisan Table Container Width", 6);
            ArtisanTable_Height = Config.Bind("Artisan Table", "Artisan Table Container Height", 3);

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);

            var improvedBuildHudPlugin = GetComponent("ImprovedBuildHud");
            if (improvedBuildHudPlugin != null)
            {
                var improvedBuildHudType = improvedBuildHudPlugin.GetType();
                var getAvailableItemsMethod = AccessTools.Method(improvedBuildHudType, "GetAvailableItems");
                if (getAvailableItemsMethod != null)
                {
                    _harmony.Patch(getAvailableItemsMethod, null, new HarmonyMethod(typeof(StationsAreContainers), nameof(ImprovedBuildHud_GetAvailableItems_Patch)));
                }
            }
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            _harmony?.UnpatchAll(PluginId);
        }

        public static void ImprovedBuildHud_GetAvailableItems_Patch(ref int __result, string itemName)
        {
            if (Player.m_localPlayer != null)
            {
                var station = Player.m_localPlayer.GetCurrentCraftingStation();
                if (station != null)
                {
                    var container = station.GetComponent<Container>();
                    if (container != null)
                    {
                        __result += container.m_inventory.CountItems(itemName);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
    public static class CraftinStation_Interact_Patch
    {
        public static void Postfix(CraftingStation __instance)
        {
            if (InventoryGui.IsVisible())
            {
                var container = GetOrCreateContainer(__instance);
                if (container)
                {
                    InventoryGui.instance.m_currentContainer = container;
                }

                InventoryGui.instance.UpdateCraftingPanel();
            }
        }

        public static Container GetOrCreateContainer(CraftingStation craftingStation)
        {
            var container = craftingStation.gameObject.GetComponent<Container>();
            if (container == null)
            {
                switch (craftingStation.m_name)
                {
                    case "$piece_workbench":
                        return CreateConfiguredContainer(craftingStation,
                            StationsAreContainers.Workbench_Enabled.Value,
                            StationsAreContainers.Workbench_Width.Value,
                            StationsAreContainers.Workbench_Height.Value
                        );

                    case "$piece_forge":
                        return CreateConfiguredContainer(craftingStation,
                            StationsAreContainers.Forge_Enabled.Value,
                            StationsAreContainers.Forge_Width.Value,
                            StationsAreContainers.Forge_Height.Value
                        );

                    case "$piece_cauldron":
                        return CreateConfiguredContainer(craftingStation,
                            StationsAreContainers.Cauldron_Enabled.Value,
                            StationsAreContainers.Cauldron_Width.Value,
                            StationsAreContainers.Cauldron_Height.Value
                        );

                    case "$piece_stonecutter":
                        return CreateConfiguredContainer(craftingStation,
                            StationsAreContainers.Stonecutter_Enabled.Value,
                            StationsAreContainers.Stonecutter_Width.Value,
                            StationsAreContainers.Stonecutter_Height.Value
                        );

                    case "$piece_artisanstation":
                        return CreateConfiguredContainer(craftingStation,
                            StationsAreContainers.ArtisanTable_Enabled.Value,
                            StationsAreContainers.ArtisanTable_Width.Value,
                            StationsAreContainers.ArtisanTable_Height.Value
                        );
                }
            }

            return container;
        }

        public static Container CreateConfiguredContainer(CraftingStation craftingStation, bool enabled, int width, int height)
        {
            if (!enabled)
                return null;

            var container = craftingStation.gameObject.AddComponent<Container>();
            container.m_name = container.m_inventory.m_name = craftingStation.m_name;
            container.m_width = container.m_inventory.m_width = width;
            container.m_height = container.m_inventory.m_height = height;

            return container;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Piece.Requirement[]), typeof(bool), typeof(int) })]
    public static class Player_HaveRequirements_Patch
    {
        public static void Postfix(Player __instance, ref bool __result, Piece.Requirement[] resources, int qualityLevel)
        {
            var station = __instance.GetCurrentCraftingStation();
            if (!__result && station != null)
            {
                var container = station.GetComponent<Container>();
                if (container != null)
                {
                    foreach (var resource in resources)
                    {
                        if (resource.m_resItem != null)
                        {
                            var itemName = resource.m_resItem.m_itemData.m_shared.m_name;
                            var amount = resource.GetAmount(qualityLevel);
                            var totalPlayerHas = __instance.m_inventory.CountItems(itemName) + container.m_inventory.CountItems(itemName);
                            if (totalPlayerHas < amount)
                                return;
                        }
                    }

                    __result = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetAllItems), new Type[]{})]
    public static class Inventory_GetAllItems_Patch
    {
        public static void Postfix(Inventory __instance, ref List<ItemDrop.ItemData> __result)
        {
            if (__instance == Player.m_localPlayer?.m_inventory)
            {
                var station = Player.m_localPlayer?.GetCurrentCraftingStation();
                if (station != null)
                {
                    var stationContainer = station.GetComponent<Container>();
                    if (stationContainer != null)
                    {
                        __result = new List<ItemDrop.ItemData>(__result);
                        __result.AddRange(stationContainer.m_inventory.m_inventory);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new [] { typeof(ItemDrop.ItemData) })]
    public static class Inventory_RemoveItem_Patch
    {
        public static void Postfix(Inventory __instance, ref bool __result, ItemDrop.ItemData item)
        {
            if (!__result && __instance == Player.m_localPlayer?.m_inventory)
            {
                var station = Player.m_localPlayer?.GetCurrentCraftingStation();
                if (station != null)
                {
                    var stationContainer = station.GetComponent<Container>();
                    if (stationContainer != null)
                    {
                        __result = stationContainer.m_inventory.RemoveItem(item);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    public static class Player_ConsumeResources_Patch
    {
        public static bool Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel)
        {
            if (__instance == null)
                return true;

            var station = __instance.GetCurrentCraftingStation();
            if (station == null)
                return true;

            var stationContainer = station.GetComponent<Container>();
            if (stationContainer == null)
                return true;

            foreach (var requirement in requirements)
            {
                if (requirement.m_resItem != null)
                {
                    var itemName = requirement.m_resItem.m_itemData.m_shared.m_name;
                    var amount = requirement.GetAmount(qualityLevel);
                    if (amount > 0)
                    {
                        var has = __instance.m_inventory.CountItems(itemName);
                        var fromContainer = has >= amount ? 0 : amount - has;
                        if (fromContainer > 0)
                            stationContainer.m_inventory.RemoveItem(itemName, fromContainer);
                    }   
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    [HarmonyPriority(Priority.Low)]
    public static class InventoryGui_SetupRequirement_Patch
    {
        public static void Postfix(Transform elementRoot, Piece.Requirement req, Player player, int quality)
        {
            var amountText = elementRoot.transform.Find("res_amount").GetComponent<Text>();
            if (req.m_resItem != null)
            {
                if (!player.HaveRequirements(new [] { req }, false, quality))
                {
                    amountText.color = Mathf.Sin(Time.time * 10f) > 0.0 ? Color.red : Color.white;
                }
                else
                {
                    amountText.color = Color.white;
                }
            }
        }
    }
}
