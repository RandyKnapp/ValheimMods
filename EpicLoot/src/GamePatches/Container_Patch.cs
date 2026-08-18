using EpicLoot.Config;
using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Container), nameof(Container.AddDefaultItems))]
    public static class Container_AddDefaultItems_Patch
    {
        public static void Postfix(Container __instance)
        {
            if (!PendingChestLoot.TryGetLootTables(__instance, out var containerName, out var lootTables))
            {
                return;
            }

            var zdo = __instance.m_nview == null ? null : __instance.m_nview.GetZDO();

            // CheckForChanges destroys an m_autoDestroyEmpty container as soon as it is owned, empty
            // and not in use, so one whose vanilla drop table happened to roll nothing would vanish
            // before anyone could reach it. Never defer those.
            if (!ELConfig.DeferChestLootRoll.Value || __instance.m_autoDestroyEmpty
                || zdo == null || !zdo.IsValid())
            {
                PendingChestLoot.Roll(__instance, containerName, lootTables);
                return;
            }

            // Vanilla only calls AddDefaultItems on the ZDO owner, so this write is safe.
            zdo.Set(PendingChestLoot.PendingKey, true);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    public static class Container_Awake_PendingLoot_Patch
    {
        public static void Postfix(Container __instance)
        {
            // Vanilla Awake calls AddDefaultItems, so a chest flagged moments ago is picked up here
            // as well — this is the only place the registry is populated, and it covers both a
            // freshly generated chest and one rehydrated from a ZDO in a later session.
            var zdo = __instance.m_nview == null ? null : __instance.m_nview.GetZDO();
            if (zdo != null && zdo.IsValid() && zdo.GetBool(PendingChestLoot.PendingKey))
            {
                PendingChestLoot.Register(__instance);
            }
        }
    }

    /// <summary>
    /// The universal "contents are being read" accessor — InventoryGui, Container.CanBeRemoved and
    /// quick-loot mods all route through it. Rolling in a prefix hands the caller an inventory that
    /// already holds the loot, so nothing needs a second pass to notice it.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.GetInventory))]
    public static class Container_GetInventory_Patch
    {
        public static void Prefix(Container __instance)
        {
            if (PendingChestLoot.AnyPending)
            {
                PendingChestLoot.TryRoll(__instance);
            }
        }
    }

    /// <summary>
    /// GetHoverText reads m_inventory directly, so it bypasses the GetInventory hook; without this a
    /// pending chest whose vanilla drop table rolled nothing would read "( empty )". Hovering is a
    /// short local raycast, so it is a fair "the player is at the chest" signal.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    public static class Container_GetHoverText_Patch
    {
        public static void Prefix(Container __instance)
        {
            if (PendingChestLoot.AnyPending)
            {
                PendingChestLoot.TryRoll(__instance);
            }
        }
    }

    /// <summary>
    /// DropAllItems reads m_inventory directly, so smashing a chest nobody ever opened would
    /// otherwise silently lose its EpicLoot contents.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
    public static class Container_OnDestroyed_Patch
    {
        public static void Prefix(Container __instance)
        {
            if (PendingChestLoot.AnyPending)
            {
                PendingChestLoot.TryRoll(__instance);
            }
        }
    }

    /// <summary>
    /// Take-all moves out of m_inventory directly. Unreachable for chests in vanilla (Interact bails
    /// on hold), but Container.TakeAll is public and quick-loot mods call it.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.RPC_TakeAllRespons))]
    public static class Container_RPC_TakeAllRespons_Patch
    {
        public static void Prefix(Container __instance, bool granted)
        {
            if (granted && PendingChestLoot.AnyPending)
            {
                PendingChestLoot.TryRoll(__instance);
            }
        }
    }
}
