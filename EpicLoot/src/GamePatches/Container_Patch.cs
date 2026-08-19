using EpicLoot.Config;
using HarmonyLib;
using UnityEngine;

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

            // CLLC's DropTable.GetDropListItems postfix multiplies a drop by appending the *same*
            // ItemDrop.ItemData reference N times rather than cloning it, and vanilla AddDefaultItems
            // feeds each of those to Inventory.AddItem. The result is one object occupying several
            // inventory slots, which every later pass over the inventory — ours included — then
            // treats as distinct items. Split them before EpicLoot touches this container.
            PendingChestLoot.DeAliasInventory(__instance, containerName);

            var zdo = __instance.m_nview == null ? null : __instance.m_nview.GetZDO();

            // CheckForChanges destroys an m_autoDestroyEmpty container as soon as it is owned, empty
            // and not in use, so one whose vanilla drop table happened to roll nothing would vanish
            // before anyone could reach it. Never defer those.
            if (!ELConfig.DeferChestLootRoll.Value || __instance.m_autoDestroyEmpty
                || zdo == null || !zdo.IsValid())
            {
                // ZNetView.m_useInitZDO is true for exactly as long as ZNetScene.CreateObject is
                // mid-Instantiate of this container, and this postfix runs inside Container.Awake,
                // inside that Instantiate. Rolling here would nest a second Instantiate inside the
                // first while ZNetScene still has m_initZDO checked out — so hand it to LateUpdate.
                if (ZNetView.m_useInitZDO)
                {
                    PendingChestLoot.RequestDirectRoll(__instance);
                }
                else
                {
                    PendingChestLoot.Roll(__instance, containerName, lootTables);
                }

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
    /// quick-loot mods all route through it. It is a plain getter, though: container-scanning mods
    /// (AzuCraftyBoxes' EpicLoot inventory provider among them, via our own API) call it over every
    /// container in range, several times per frame. Rolling inline here meant one crafting-station
    /// query force-rolled every unopened chest in the dungeon in a single frame. Queue instead; the
    /// loot lands on the next LateUpdate, which is still well before a player can act on it, since
    /// hovering a chest fires the trigger below first.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.GetInventory))]
    public static class Container_GetInventory_Patch
    {
        public static void Prefix(Container __instance)
        {
            if (PendingChestLoot.AnyPending)
            {
                PendingChestLoot.RequestRoll(__instance);
            }
        }
    }

    /// <summary>
    /// GetHoverText reads m_inventory directly, so it bypasses the GetInventory hook; without this a
    /// pending chest whose vanilla drop table rolled nothing would read "( empty )". Hovering is a
    /// short local raycast, so it is a fair "the player is at the chest" signal — but it also runs
    /// every frame the player looks at the chest, so it queues rather than rolling inline.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    public static class Container_GetHoverText_Patch
    {
        public static void Prefix(Container __instance)
        {
            if (PendingChestLoot.AnyPending)
            {
                PendingChestLoot.RequestRoll(__instance);
            }
        }
    }

    /// <summary>
    /// Opening a chest is a deliberate player action on one specific container, so it rolls
    /// synchronously — there is no bulk-call risk here, and the contents must be present by the time
    /// the open RPC resolves.
    /// </summary>
    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    public static class Container_Interact_Patch
    {
        public static void Prefix(Container __instance, bool hold)
        {
            if (!hold && PendingChestLoot.AnyPending)
            {
                PendingChestLoot.TryRoll(__instance);
            }
        }
    }

    /// <summary>
    /// DropAllItems reads m_inventory directly, so smashing a chest nobody ever opened would
    /// otherwise silently lose its EpicLoot contents. Synchronous: the inventory is emptied inside
    /// this same call, so a deferred roll would arrive after the loot had already been dropped.
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
    /// on hold), but Container.TakeAll is public and quick-loot mods call it. Synchronous for the
    /// same reason as OnDestroyed.
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
