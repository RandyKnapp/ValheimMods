using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
//using ServerSync;

namespace StationsAreContainers
{
    [BepInPlugin(PluginId, DisplayName, Version)]
    [BepInDependency("randyknapp.mods.improvedbuildhud", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
    public class StationsAreContainers : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.stationcontainers";
        public const string DisplayName = "Stations Are Containers";
        public const string Version = "1.0.8";

        public class StationConfig
        {
            public ConfigEntry<bool> Enabled;
            public ConfigEntry<int>  Width;
            public ConfigEntry<int>  Height;
        }

        //private static ConfigEntry<bool> _serverConfigLocked;
        //private readonly ConfigSync _configSync = new ConfigSync(PluginId) { DisplayName = DisplayName,
        //    CurrentVersion = Version, MinimumRequiredVersion = Version };

        private static StationsAreContainers _instance;
        internal static ManualLogSource Log => _instance.Logger;
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

            //_serverConfigLocked = SyncedConfig("Config Sync", "Lock Config", false,
            //    "[Server Only] The configuration is locked and may not be changed by clients once it has been synced from the server. " +
            //    "Only valid for server config, will have no effect on clients.");
            //_configSync.AddLockingConfigEntry(_serverConfigLocked);

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

        /*private ConfigEntry<T> SyncedConfig<T>(string group, string configName, T value, string description, bool synchronizedSetting = true)
        {
            var configEntry = Config.Bind(group, configName, value, new ConfigDescription(description));

            var syncedConfigEntry = _configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }*/

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

        // The storage of the station the player is crafting at, or null when it may not be used. Only the
        // client that owns the station's ZDO may count or drain it: vanilla Container.OnContainerChanged
        // saves only for the owner, so a non-owner's drain is never written, the next Container.Load puts
        // the items back, and the craft came free. A second player at a station is refused the storage by
        // the open handshake, and crafts from it are refused the same way.
        public static Container GetOwnedStationContainer(Player player)
        {
            if (player == null)
                return null;

            var station = player.GetCurrentCraftingStation();
            if (station == null)
                return null;

            var container = station.GetComponent<Container>();
            if (container == null || container.m_inventory == null || !container.IsOwner())
                return null;

            return container;
        }

        public static void ImprovedBuildHud_GetAvailableItems_Patch(ref int __result, string itemName)
        {
            // Inside SetupRequirementList the Inventory.CountItems postfix has already added the
            // container's items to the count GetAvailableItems just did on the player inventory --
            // adding them again here double-counted every requirement in the crafting panel.
            if (InventoryGui_SetupRequirement_Patch.SettingUpRequirement > 0)
                return;

            var container = GetOwnedStationContainer(Player.m_localPlayer);
            if (container != null)
            {
                __result += container.m_inventory.CountItems(itemName);
            }
        }
    }

    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
    public static class CraftinStation_Interact_Patch
    {
        // Valheim 1.0 renamed the container RPCs (pre-1.0 this was "RequestOpen"). An unregistered name
        // is dropped without an error, so the name is checked before it is relied on.
        private const string RequestOpenRpc = "RPC_RequestOpen";
        private static bool _warnedMissingRpc;

        public static void Postfix(CraftingStation __instance, bool repeat)
        {
            // Vanilla returns false even when it opened the station, so whether it did is read off the
            // player: a held Use repeats the call, and a refused one (no roof, too far) opens nothing.
            if (repeat || Player.m_localPlayer == null || Player.m_localPlayer.GetCurrentCraftingStation() != __instance)
                return;

            if (InventoryGui.IsVisible())
            {
                var container = GetOrCreateContainer(__instance);
                if (container && container.m_nview != null && container.m_nview.GetZDO() != null)
                {
                    if (container.m_nview.m_functions.ContainsKey(RequestOpenRpc.GetStableHashCode()))
                    {
                        // Go through vanilla's open handshake instead of assigning m_currentContainer
                        // directly: RPC_RequestOpen transfers ZDO ownership to this client (the grid only
                        // draws for the owner) and enforces the single-user lock. The answer is handled
                        // by Container_RPC_OpenResponse_Patch.
                        container.m_nview.InvokeRPC(RequestOpenRpc, Game.instance.GetPlayerProfile().GetPlayerID());
                    }
                    else
                    {
                        if (!_warnedMissingRpc)
                        {
                            _warnedMissingRpc = true;
                            StationsAreContainers.Log.LogWarning($"Container RPC '{RequestOpenRpc}' is not registered; " +
                                "opening station containers without the ownership handshake.");
                        }
                        AttachContainer(InventoryGui.instance, container);
                    }
                }
                else if (InventoryGui.instance.m_currentContainer != null &&
                         InventoryGui.instance.m_currentContainer.GetComponent<CraftingStation>() != null)
                {
                    // Station containers are disabled (or this one is invalid): don't leave a previous
                    // station's container pane attached.
                    InventoryGui.instance.m_currentContainer = null;
                }

                InventoryGui.instance.UpdateCraftingPanel();
            }
        }

        // Puts the station's storage pane under the crafting panel that is already open. Not through
        // InventoryGui.Show: that re-runs the whole open (tab switch, open sound) and, with Use still held
        // from the press that opened the station, arms the container hold-to-stack wait, during which
        // GameCamera.UpdateMouseCapture keeps the cursor hidden and locked.
        public static void AttachContainer(InventoryGui gui, Container container)
        {
            if (gui.m_currentContainer == container)
                return;

            gui.m_currentContainer = container;
            gui.m_firstContainerUpdate = true;
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
            // Container.Awake runs synchronously inside AddComponent and bails before creating
            // m_inventory when the station has no ZNetView/ZDO (ghost build previews, stripped
            // objects) -- dereferencing it here would NRE.
            if (container.m_inventory == null)
            {
                UnityEngine.Object.Destroy(container);
                return null;
            }
            container.m_name = container.m_inventory.m_name = craftingStation.m_name;
            container.m_width = container.m_inventory.m_width = width;
            container.m_height = container.m_inventory.m_height = height;

            return container;
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.RPC_OpenResponse))]
    public static class Container_RPC_OpenResponse_Patch
    {
        // The answer to a station's open request. Vanilla calls InventoryGui.Show(container) here, which
        // would reopen the inventory with the container even after the player has closed the station or
        // walked away, and arm the hold-to-stack wait (see AttachContainer). A station's pane is attached
        // only while that station's crafting panel is still open.
        public static bool Prefix(Container __instance, bool granted)
        {
            var station = __instance.GetComponent<CraftingStation>();
            if (station == null)
                return true;

            var player = Player.m_localPlayer;
            if (player == null)
                return false;

            if (!granted)
            {
                // Another player has this station's storage open; crafting still works without it.
                player.Message(MessageHud.MessageType.Center, "$msg_inuse");
            }
            else if (InventoryGui.IsVisible() && player.GetCurrentCraftingStation() == station)
            {
                CraftinStation_Interact_Patch.AttachContainer(InventoryGui.instance, __instance);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
    public static class InventoryGui_UpdateContainer_Patch
    {
        // Station storage counts toward crafts only once this client owns the station's ZDO (see
        // GetOwnedStationContainer). When another client held it, ownership arrives some frames after the
        // crafting panel was built, so the recipe list is rebuilt on the frame vanilla first draws the
        // pane for the owner (the frame it consumes m_firstContainerUpdate).
        public static void Prefix(InventoryGui __instance, out bool __state)
        {
            var container = __instance.m_currentContainer;
            __state = __instance.m_firstContainerUpdate && container != null && container.IsOwner() &&
                      container.GetComponent<CraftingStation>() != null;
        }

        // While m_waitForContainerStack is set, GameCamera.UpdateMouseCapture hides and locks the cursor
        // even though the inventory is open. Vanilla clears it only in UpdateContainer's owner branch, on
        // the first frame Use is up while the hold state is still >= 0. A station's pane can miss that for
        // good: the station's ZDO belongs to another player or the server, or the hold state was already
        // spent, and the player is left at the station with no cursor. Opening any chest re-arms and then
        // clears the flag, which is why chests kept working. Here it is cleared once Use is up, whenever
        // the pane is a station's (or no container is attached at all, where vanilla never clears it).
        public static void Postfix(InventoryGui __instance, bool __state)
        {
            if (__state && !__instance.m_firstContainerUpdate)
                __instance.UpdateCraftingPanel();

            if (!__instance.m_waitForContainerStack || InventoryGui.UseButtonHeld())
                return;

            var container = __instance.m_currentContainer;
            if (container != null && container.GetComponent<CraftingStation>() == null)
                return;

            __instance.m_waitForContainerStack = false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirementItems),
        new[] { typeof(Recipe), typeof(bool), typeof(int), typeof(int) })]
    public static class Player_HaveRequirementItems_Patch
    {
        public static void Postfix(Player __instance, ref bool __result, Recipe piece, bool discover, int qualityLevel, int amount)
        {
            // Discover mode checks m_knownMaterial, not inventory counts -- letting container
            // contents flip it would mark recipes "known" for materials the player never picked up.
            if (__instance == null || discover || __result)
                return;

            var container = StationsAreContainers.GetOwnedStationContainer(__instance);
            if (container == null)
                return;

            bool anySatisfied = false;
            foreach (var resource in piece.m_resources)
            {
                if (resource.m_resItem == null)
                    continue;

                var shared = resource.m_resItem.m_itemData.m_shared;
                var resAmount = resource.GetAmount(qualityLevel) * amount;
                // Mirror vanilla: the count that matters is the best single quality level, not the
                // sum across qualities.
                int best = 0;
                for (int quality = 1; quality < shared.m_maxQuality + 1; ++quality)
                {
                    int count = __instance.GetInventory().CountItems(shared.m_name, quality) +
                                container.GetInventory().CountItems(shared.m_name, quality);
                    if (count > best)
                        best = count;
                }

                if (piece.m_requireOnlyOneIngredient)
                {
                    if (best >= resAmount)
                    {
                        anySatisfied = true;
                        break;
                    }
                }
                else if (best < resAmount)
                {
                    return;
                }
            }

            if (piece.m_requireOnlyOneIngredient)
            {
                if (anySatisfied)
                    __result = true;
                return;
            }

            __result = true;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new [] { typeof(ItemDrop.ItemData) })]
    public static class Inventory_RemoveItem_Patch
    {
        public static void Postfix(Inventory __instance, ref bool __result, ItemDrop.ItemData item)
        {
            if (!__result && __instance != null && Player.m_localPlayer != null && __instance == Player.m_localPlayer.m_inventory)
            {
                var stationContainer = StationsAreContainers.GetOwnedStationContainer(Player.m_localPlayer);
                if (stationContainer != null)
                {
                    __result = stationContainer.m_inventory.RemoveItem(item);
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

        // Finalizer, not postfix: a throw inside SetupRequirementList (e.g. another mod's patch)
        // would skip a postfix, latch the counter above zero, and permanently inflate every later
        // Inventory.CountItems on the player inventory.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirementList))]
        [HarmonyFinalizer]
        public static void SetupRequirementsList_Finalizer()
        {
            if (SettingUpRequirement > 0)
                SettingUpRequirement--;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems))]
        [HarmonyPostfix]
        public static void InventoryGui_SetupRequirement_Postfix(Inventory __instance, ref int __result,
            string name, int quality = -1, bool matchWorldLevel = true)
        {
            if (SettingUpRequirement > 0 && __instance != null && Player.m_localPlayer != null && __instance == Player.m_localPlayer.m_inventory)
            {
                var stationContainer = StationsAreContainers.GetOwnedStationContainer(Player.m_localPlayer);
                if (stationContainer != null)
                {
                    __result += stationContainer.GetInventory().CountItems(name, quality, matchWorldLevel);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
    public static class Player_ConsumeResources_Patch
    {
        // Priority.Last + __runOriginal: the container must not be drained when an earlier prefix
        // already cancelled the consume (the items would vanish with nothing crafted).
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel,
            int itemQuality, int multiplier, bool __runOriginal)
        {
            if (!__runOriginal || __instance == null)
                return true;

            var stationContainer = StationsAreContainers.GetOwnedStationContainer(__instance);
            if (stationContainer == null)
                return true;

            foreach (var requirement in requirements)
            {
                if (requirement.m_resItem != null)
                {
                    var itemName = requirement.m_resItem.m_itemData.m_shared.m_name;
                    var amount = requirement.GetAmount(qualityLevel) * multiplier;
                    if (amount > 0)
                    {
                        // Vanilla removes with the same itemQuality filter -- count and drain with it
                        // too, or upgrades would under-drain the container and undercharge.
                        var has = __instance.m_inventory.CountItems(itemName, itemQuality);
                        var fromContainer = has >= amount ? 0 : amount - has;
                        if (fromContainer > 0)
                            stationContainer.m_inventory.RemoveItem(itemName, fromContainer, itemQuality);
                    }
                }
            }

            return true;
        }
    }
}
