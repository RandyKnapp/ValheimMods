using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot
{
    /// <summary>
    /// Defers a loot chest's EpicLoot roll from spawn time to the first time a player reads the
    /// chest's contents.
    ///
    /// A location's dungeon interior is instantiated in the same XZ sector as its surface location
    /// (Location.Awake places it 5000m straight up), and ZNetScene creates objects by sector with no
    /// height test — so every chest inside a crypt is created, and used to roll, the moment anyone
    /// entered the zone above it. Since gating is resolved during the roll and *downgrades* to a
    /// fallback item rather than re-rolling later, a crypt sailed past early in a playthrough kept
    /// meadows-tier loot forever. Rolling on first read instead means the roll sees the global boss
    /// keys, gating mode and nearby-player luck in effect when the chest is actually reached.
    /// </summary>
    public static class PendingChestLoot
    {
        public const string PendingKey = "EpicLoot.ChestLootPending";

        // Every trigger patch's fast path is AnyPending, which is false in essentially all play;
        // only a loaded, still-unrolled chest costs a lookup. That is the reason this is a registry
        // rather than a per-chest component (GetComponent per call) or a timer (distance checks over
        // every chest in range, which says nothing useful anyway — a dungeon chest sits 5000m from a
        // player standing above it, but an overworld ruin's chests are at ordinary distances).
        private static readonly HashSet<Container> Pending = new HashSet<Container>();

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

        // Rolling instantiates and destroys objects and touches ZNetView's global init flag. Nothing
        // in that sequence is safe to re-enter, and a chest's own Awake/Load postfixes can reach back
        // into any of the trigger patches, so a roll is never allowed to start inside another one.
        private static bool _rolling;

        // A single container sweep from a crafting-station mod can ask for every chest in range at
        // once. Spreading them keeps one frame from instantiating hundreds of loot objects.
        private const int MaxRollsPerFrame = 2;

        public static bool AnyPending => Pending.Count > 0;

        public static void Register(Container container)
        {
            // Chests leave the set when they roll, but a zone unload destroys them behind our back,
            // so sweep the corpses here. Registration happens once per chest instantiation, over a
            // set that is normally empty.
            Pending.RemoveWhere(x => x == null);
            Pending.Add(container);
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
            var items = LootRoller.RollLootTable(lootTables, 1, container.m_piece.name,
                container.transform.position);
            EpicLoot.Log($"Rolling on loot table: {containerName}, " +
                $"spawned {items.Count} items at drop point({container.transform.position.ToString("0")}).");
            foreach (var item in items)
            {
                container.m_inventory.AddItem(item);
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

            if (!zdo.GetBool(PendingKey))
            {
                Pending.Remove(container);
                return;
            }

            if (!TryGetLootTables(container, out var containerName, out var lootTables))
            {
                Pending.Remove(container);
                zdo.Set(PendingKey, false);
                return;
            }

            // We are about to write the flag and the inventory. Whoever is reading a chest's
            // contents is standing at it, so the server's ZDOMan.ReleaseZDOS would hand us this ZDO
            // within a couple of seconds regardless; claiming now just removes that window.
            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            // Clear before anything that can re-enter, not merely before Roll. container.Load() below
            // runs every mod's Container.Load postfix, and any of those that reads the contents back
            // through Container.GetInventory lands in this method again — with the container still
            // pending and the flag still set, that recursed without bound. Clearing first also means a
            // failed roll loses the loot rather than duplicating it, and no peer can observe another
            // peer's items without also observing the cleared flag, since both live in this same ZDO.
            Pending.Remove(container);
            zdo.Set(PendingKey, false);

            // Container polls its ZDO once a second, so m_inventory can be a second stale.
            container.Load();

            Roll(container, containerName, lootTables);
        }

        /// <summary>
        /// Splits duplicate ItemData references out of a container's inventory.
        ///
        /// CreatureLevelAndLootControl's DropTable.GetDropListItems postfix implements its loot
        /// multiplier by appending the same ItemDrop.ItemData reference N times instead of cloning it.
        /// Container.AddDefaultItems then hands each of those to Inventory.AddItem, so one object ends
        /// up in m_inventory several times: for a stackable item AddItem's own loop bound
        /// (index &lt; item.m_stack) is the field FindFreeStackItem then increments, and for a
        /// non-stackable one the entries simply pile up sharing a single m_gridPos. Everything that
        /// later walks the inventory — our custom-data layer keys an ItemInfo off the instance, so one
        /// backs every slot — sees items that are not actually separate, and a save/reload turns them
        /// into real duplicates.
        ///
        /// Cloning all but the first occurrence costs one pass over a list that was just built, and
        /// only ever does anything when another mod produced the aliasing.
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
        /// reads that other mods call over every container in range, many times per frame, and rolling
        /// inline there turned one crafting-station sweep into hundreds of instantiates in a frame.
        /// </summary>
        public static void RequestRoll(Container container)
        {
            if (container == null || !Queued.Add(container))
            {
                return;
            }

            DeferredRolls.Enqueue(container);
            PendingChestLootDriver.Ensure();
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
            RequestRoll(container);
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
    /// Drives <see cref="PendingChestLoot.PumpDeferredRolls"/>. LateUpdate rather than Update so the
    /// roll cannot land inside another component's Update — ZNetScene.Update in particular, which
    /// creates and destroys the very objects a roll would be instantiating.
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
        }
    }
}
