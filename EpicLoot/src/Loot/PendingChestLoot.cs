using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot
{
    /// <summary>
    /// Defers a loot chest's EpicLoot roll to the first time a player reads its contents. A dungeon
    /// interior sits in its surface location's XZ sector (5000m up), and ZNetScene creates objects by
    /// sector with no height test, so a spawn-time roll would gate a crypt's chests against whoever
    /// first entered the zone above it.
    /// </summary>
    public static class PendingChestLoot
    {
        public const string PendingKey = "EpicLoot.ChestLootPending";

        // Container.Awake reads the flag for every container that loads, player-built chests included.
        public static readonly int PendingKeyHash = PendingKey.GetStableHashCode();

        // A registry rather than a per-chest component, so a trigger patch costs AnyPending plus one set
        // lookup. AnyPending is true whenever any unopened loot chest is loaded, so the lookup is the
        // path that has to stay cheap.
        private static readonly HashSet<Container> Pending = new HashSet<Container>();

        // A zone unload destroys pending chests behind our back. The driver sweeps them on this period,
        // so AnyPending falls back to false once the last one is gone.
        private const float SweepInterval = 5f;
        private static float _nextSweepTime;

        // Chests whose roll was asked for from somewhere it must not run synchronously — a plain
        // accessor that other mods call in bulk, or inside ZNetScene's own object creation. Drained
        // from LateUpdate, a few per frame, by PendingChestLootDriver.
        private static readonly Queue<Container> DeferredRolls = new Queue<Container>();
        private static readonly HashSet<Container> Queued = new HashSet<Container>();

        // Chests queued by the *non-deferred* AddDefaultItems path, which never sets the ZDO flag and so
        // is invisible to TryRoll. Kept apart from the flag-gated queue on purpose: a chest reaches the
        // queue below from plain accessors too, and rolling those unconditionally would re-roll a chest
        // every time anything read it.
        private static readonly HashSet<Container> DirectRollRequests = new HashSet<Container>();

        // A chest's own Load postfixes can reach back into any of the trigger patches, and a roll is not
        // safe to re-enter, so one never starts inside another.
        private static bool _rolling;

        // A single container sweep from a crafting-station mod can ask for every chest in range at
        // once. Spreading them keeps one frame from rolling all of them.
        private const int MaxRollsPerFrame = 2;

        public static bool AnyPending => Pending.Count > 0;

        public static void Register(Container container)
        {
            Pending.Add(container);
            PendingChestLootDriver.Ensure();
        }

        /// <summary>
        /// Drops chests a zone unload destroyed. They leave the set when they roll, but nothing tells us
        /// when they are unloaded instead, and each one left behind keeps AnyPending true.
        /// </summary>
        internal static void SweepUnloaded()
        {
            if (Pending.Count == 0 || Time.unscaledTime < _nextSweepTime)
            {
                return;
            }

            _nextSweepTime = Time.unscaledTime + SweepInterval;
            Pending.RemoveWhere(x => x == null);
        }

        /// <summary>
        /// Resolves the loot table for a container, keyed on its Piece name. A container with no
        /// table (every player-built chest, and the loot_chest_stone that adventure-mode treasure
        /// map chests clone) is never flagged and never rolled.
        /// </summary>
        public static bool TryGetLootTables(Container container, out string containerName,
            out List<LootTable> lootTables)
        {
            containerName = null;
            lootTables = null;

            if (container == null || container.m_piece == null)
            {
                return false;
            }

            containerName = container.m_piece.name.Replace("(Clone)", "").Trim();
            lootTables = LootRoller.GetLootTable(containerName);
            return lootTables != null && lootTables.Count > 0;
        }

        /// <summary>
        /// Rolls a chest's EpicLoot contents into its inventory. The caller must own the ZDO —
        /// AddItem raises Inventory.Changed, which Container.OnContainerChanged only persists for
        /// the owner.
        /// </summary>
        public static void Roll(Container container, string containerName, List<LootTable> lootTables)
        {
            if (_rolling)
            {
                EpicLoot.LogWarning($"Re-entrant chest roll blocked for {containerName}; " +
                    "deferring it instead.");
                RequestDirectRoll(container);
                return;
            }

            _rolling = true;
            try
            {
                RollInternal(container, containerName, lootTables);
            }
            finally
            {
                _rolling = false;
            }
        }

        private static void RollInternal(Container container, string containerName,
            List<LootTable> lootTables)
        {
            // The clean prefab name: the roll looks the chest's own entry (Modifiers, StarScaling) up by it.
            var items = LootRoller.RollLootTable(lootTables, 1, containerName,
                container.transform.position);
            ContainerCapacity.AddItems(container, items);

            if (!EpicLoot.IsLogEnabled(LogLevel.Info))
            {
                return;
            }

            EpicLoot.Log($"Rolling on loot table: {containerName}, " +
                $"spawned {items.Count} items at drop point({container.transform.position.ToString("0")}).");
            foreach (var item in items)
            {
                EpicLoot.Log($"  - {item.m_shared.m_name}" + (item.IsMagic() ?
                    $": {string.Join(", ", item.GetMagicItem().Effects.Select(x => x.EffectType.ToString()))}" :
                    ""));
            }
        }

        /// <summary>
        /// Called from every path that reads a chest's contents. Rolls the chest if it is still
        /// flagged, so the items come back as part of that same read.
        /// </summary>
        public static void TryRoll(Container container)
        {
            if (container == null || !Pending.Contains(container))
            {
                return;
            }

            // Gating resolves against the local player — GatedItemTypeHelper.CheckIfItemNeedsGate
            // treats a missing one as "everything is gated" — so a dedicated server must never roll.
            // It would produce exactly the all-fallback loot this defers to avoid. Stay pending
            // until a real client reads the chest.
            if (Player.m_localPlayer == null || ZNetScene.instance == null || ObjectDB.instance == null)
            {
                return;
            }

            var nview = container.m_nview;
            var zdo = nview == null ? null : nview.GetZDO();
            if (zdo == null || !zdo.IsValid() || container.m_inventory == null)
            {
                return;
            }

            if (!zdo.GetBool(PendingKeyHash))
            {
                Pending.Remove(container);
                return;
            }

            // We are about to write the flag (and, below, the inventory). Whoever is reading a chest's
            // contents is standing at it, so the server's ZDOMan.ReleaseZDOS would hand us this ZDO
            // within a couple of seconds regardless; claiming now just removes that window.
            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            if (!TryGetLootTables(container, out var containerName, out var lootTables))
            {
                Pending.Remove(container);
                zdo.Set(PendingKeyHash, false);
                return;
            }

            // Clear before anything that can re-enter, not merely before Roll: container.Load() below
            // runs every mod's Container.Load postfix, and one that reads the contents back through
            // Container.GetInventory lands here again. Clearing first also means a failed roll loses the
            // loot rather than duplicating it.
            Pending.Remove(container);
            zdo.Set(PendingKeyHash, false);

            // Container polls its ZDO once a second, so m_inventory can be a second stale.
            container.Load();

            Roll(container, containerName, lootTables);
        }

        /// <summary>
        /// Splits duplicate ItemData references out of a container's inventory.
        /// CreatureLevelAndLootControl's DropTable.GetDropListItems postfix multiplies loot by appending
        /// the same ItemData reference N times, so one object fills several slots and a save/reload turns
        /// them into real duplicates. All but the first occurrence are cloned.
        /// </summary>
        public static void DeAliasInventory(Container container, string containerName)
        {
            var items = container == null || container.m_inventory == null
                ? null
                : container.m_inventory.m_inventory;
            if (items == null || items.Count < 2)
            {
                return;
            }

            var seen = new HashSet<ItemDrop.ItemData>();
            var cloned = 0;
            for (var i = 0; i < items.Count; ++i)
            {
                var item = items[i];
                if (item == null || seen.Add(item))
                {
                    continue;
                }

                var copy = item.Clone();
                copy.m_gridPos = item.m_gridPos;
                items[i] = copy;
                seen.Add(copy);
                ++cloned;
            }

            if (cloned > 0)
            {
                EpicLoot.LogWarning($"Container '{containerName}' was given {cloned} duplicate " +
                    "ItemData reference(s) by another mod's drop-table multiplier; cloned them so the " +
                    "slots are independent items.");
            }
        }

        /// <summary>
        /// Queues a chest to roll on the next LateUpdate instead of right now. Used by every trigger
        /// that is not a deliberate player action: Container.GetInventory and GetHoverText are plain
        /// reads that other mods call over every container in range, many times per frame.
        /// </summary>
        public static void RequestRoll(Container container)
        {
            if (container == null || !Pending.Contains(container))
            {
                return;
            }

            Enqueue(container);
        }

        /// <summary>
        /// Queues a chest that must roll exactly once without going through the ZDO pending flag — the
        /// non-deferred AddDefaultItems path, when it cannot roll inline because it is running inside
        /// ZNetScene object creation.
        /// </summary>
        public static void RequestDirectRoll(Container container)
        {
            if (container == null)
            {
                return;
            }

            DirectRollRequests.Add(container);
            Enqueue(container);
        }

        private static void Enqueue(Container container)
        {
            if (!Queued.Add(container))
            {
                return;
            }

            DeferredRolls.Enqueue(container);
            PendingChestLootDriver.Ensure();
        }

        /// <summary>
        /// Drains the deferred queue. Called from LateUpdate, so it always runs off whatever call
        /// stack asked for the roll — in particular outside ZNetScene's object creation, where a
        /// nested Instantiate would corrupt the ZDO it is in the middle of handing out.
        /// </summary>
        public static void PumpDeferredRolls()
        {
            for (var i = 0; i < MaxRollsPerFrame && DeferredRolls.Count > 0; ++i)
            {
                var container = DeferredRolls.Dequeue();
                Queued.Remove(container);
                var wasDirect = DirectRollRequests.Remove(container);

                // Unity fake-null: the zone can unload a chest between queueing and draining. Both sets
                // are cleared above rather than after this check so a destroyed chest cannot leak one.
                if (container == null)
                {
                    continue;
                }

                if (wasDirect)
                {
                    if (TryGetLootTables(container, out var containerName, out var lootTables))
                    {
                        Roll(container, containerName, lootTables);
                    }
                }
                else if (Pending.Contains(container))
                {
                    TryRoll(container);
                }
            }
        }
    }

    /// <summary>
    /// Drives <see cref="PendingChestLoot.PumpDeferredRolls"/> and the unloaded-chest sweep. LateUpdate
    /// rather than Update so the roll cannot land inside another component's Update — ZNetScene.Update
    /// in particular, which creates and destroys the very objects a roll would be instantiating.
    /// </summary>
    public class PendingChestLootDriver : MonoBehaviour
    {
        private static PendingChestLootDriver _instance;

        public static void Ensure()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("EpicLoot_PendingChestLootDriver");
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<PendingChestLootDriver>();
        }

        public void LateUpdate()
        {
            PendingChestLoot.PumpDeferredRolls();
            PendingChestLoot.SweepUnloaded();
        }
    }
}
