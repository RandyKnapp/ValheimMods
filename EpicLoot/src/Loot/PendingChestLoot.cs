using System.Collections.Generic;
using System.Linq;

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

            // Container polls its ZDO once a second, so m_inventory can be a second stale.
            container.Load();

            // Clear before rolling. A failed roll then loses the loot rather than duplicating it,
            // the helper is reentrant-safe, and no peer can observe another peer's items without
            // also observing the cleared flag, since both live in this same ZDO.
            Pending.Remove(container);
            zdo.Set(PendingKey, false);

            Roll(container, containerName, lootTables);
        }
    }
}
