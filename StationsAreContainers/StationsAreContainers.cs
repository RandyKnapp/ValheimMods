using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;

namespace StationsAreContainers
{
    [BepInPlugin(PluginId, DisplayName, Version)]
    [BepInDependency("randyknapp.mods.improvedbuildhud", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
    public class StationsAreContainers : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.stationcontainers";
        public const string DisplayName = "Stations Are Containers";
        public const string Version = "1.0.3";

        public class StationConfig
        {
            public ConfigEntry<bool> Enabled;
            public ConfigEntry<int>  Width;
            public ConfigEntry<int>  Height;
        }

        private static ConfigEntry<bool> _serverConfigLocked;
        private readonly ConfigSync _configSync = new ConfigSync(PluginId) { DisplayName = DisplayName, CurrentVersion = Version, MinimumRequiredVersion = Version };

        private static StationsAreContainers _instance;
        private Harmony _harmony;
        private static readonly Dictionary<string, StationConfig> Configs = new Dictionary<string, StationConfig>();

        [UsedImplicitly]
        private void Awake()
        {
            _instance = this;

            InitConfigForStation("$piece_workbench", "Workbench");
            InitConfigForStation("$piece_forge", "Forge");
            InitConfigForStation("$piece_cauldron", "Cauldron");
            InitConfigForStation("$piece_stonecutter", "Stonecutter");
            InitConfigForStation("$piece_artisanstation", "Artisan Table");
            InitConfigForStation("$piece_blackforge", "Black Forge");
            InitConfigForStation("$piece_magetable", "Galdr Table");

            _serverConfigLocked = SyncedConfig("Config Sync", "Lock Config", false, "[Server Only] The configuration is locked and may not be changed by clients once it has been synced from the server. Only valid for server config, will have no effect on clients.");
            _configSync.AddLockingConfigEntry(_serverConfigLocked);

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

        private ConfigEntry<T> SyncedConfig<T>(string group, string configName, T value, string description, bool synchronizedSetting = true)
        {
            var configEntry = Config.Bind(group, configName, value, new ConfigDescription(description));

            var syncedConfigEntry = _configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        public static StationConfig InitConfigForStation(string locID, string label)
        {
            var config = new StationConfig();

            config.Enabled  = _instance.Config.Bind(label, $"{label} Container Enabled", true);
            config.Width    = _instance.Config.Bind(label, $"{label} Container Width", 6);
            config.Height   = _instance.Config.Bind(label, $"{label} Container Height", 3);

            Configs.Add(locID, config);
            return config;
        }

        public static StationConfig GetConfigForStation(CraftingStation station)
        {
            if (Configs.TryGetValue(station.m_name, out var config))
            {
                return config;
            }
            else
            {
                var label = station.m_name.Replace("$", "");
                var newConfig = InitConfigForStation(station.m_name, label);
                return newConfig;
            }
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
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
                var config = StationsAreContainers.GetConfigForStation(craftingStation);
                return CreateConfiguredContainer(craftingStation,
                    config.Enabled.Value,
                    config.Width.Value,
                    config.Height.Value
                );
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

    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems), new[] { typeof(Recipe), typeof(bool), typeof(int) })]
    public static class Player_HaveRequirementItems_Patch
    {
        public static void Postfix(Player __instance, ref bool __result, Recipe piece, int qualityLevel)
        {
            if (__instance == null)
                return;

            var station = __instance.GetCurrentCraftingStation();
            if (!__result && station != null)
            {
                var container = station.GetComponent<Container>();
                if (container != null)
                {
                    foreach (var resource in piece.m_resources)
                    {
                        if (resource.m_resItem != null)
                        {
                            var itemName = resource.m_resItem.m_itemData.m_shared.m_name;
                            var amount = resource.GetAmount(qualityLevel);
                            var totalPlayerHas = __instance.GetInventory().CountItems(itemName) + container.GetInventory().CountItems(itemName);
                            if (totalPlayerHas < amount)
                                return;
                        }
                    }

                    __result = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new [] { typeof(ItemDrop.ItemData) })]
    public static class Inventory_RemoveItem_Patch
    {
        public static void Postfix(Inventory __instance, ref bool __result, ItemDrop.ItemData item)
        {
            if (!__result && __instance != null && Player.m_localPlayer != null && __instance == Player.m_localPlayer.m_inventory)
            {
                var station = Player.m_localPlayer.GetCurrentCraftingStation();
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

    [HarmonyPatch]
    public static class InventoryGui_SetupRequirement_Patch
    {
        public static int SettingUpRequirement;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirementList))]
        [HarmonyPrefix]
        public static void SetupRequirementsList_Prefix()
        {
            SettingUpRequirement++;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirementList))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void SetupRequirementsList_Postfix()
        {
            SettingUpRequirement--;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems))]
        [HarmonyPostfix]
        public static void InventoryGui_SetupRequirement_Postfix(Inventory __instance, ref int __result, string name, int quality = -1, bool matchWorldLevel = true)
        {
            if (SettingUpRequirement > 0 && __instance != null && Player.m_localPlayer != null && __instance == Player.m_localPlayer.m_inventory)
            {
                var station = Player.m_localPlayer.GetCurrentCraftingStation();
                if (station != null)
                {
                    var stationContainer = station.GetComponent<Container>();
                    if (stationContainer != null)
                    {
                        __result += stationContainer.GetInventory().CountItems(name, quality, matchWorldLevel);
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
}
