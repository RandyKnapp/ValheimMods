using EpicLoot.Config;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    /// <summary>
    /// The optional Elite Runestone Drops tables: one extra single-entry table on each elite template, so
    /// every elite creature has a flat chance to drop a blank runestone of its biome's rarity on top of its
    /// normal loot. They are built here rather than shipped in loottables.json so the switch and the chance
    /// are two server-synced Balance settings, which a dedicated server's admin can change from Quick
    /// Configure (JSON rows there are host only) and which reach the client that rolls a kill's loot.
    ///
    /// Creatures alias their template's table list (LootRoller.AddLootTable), so adding a table under the
    /// template's name reaches every creature filed under it, including the creature sorter's entries for
    /// other mods' creatures. Nothing here touches LootRoller.Config, so the tables never reach the file
    /// the equipment auto-add rewrites.
    /// </summary>
    public static class EliteRunestoneTables
    {
        // Each elite template and its biome's runestones: the rarity ladder the templates' own elite shard
        // sets climb (ShardT1 Magic in the Black Forest ... ShardT7/ShardT8 Mythic and Ancient in the Deep
        // North). Prefab names rather than the TierNRunestone item sets, which a player's loottables.json
        // may have dropped.
        private static readonly (string Template, string[] Runestones)[] EliteTiers =
        {
            ("Tier3EliteMob", new[] { "RunestoneMagic" }),                       // Black Forest
            ("Tier4EliteMob", new[] { "RunestoneRare" }),                        // Swamp
            ("Tier5EliteMob", new[] { "RunestoneEpic" }),                        // Mountain, Ocean
            ("Tier6EliteMob", new[] { "RunestoneLegendary" }),                   // Plains
            ("Tier7EliteMob", new[] { "RunestoneLegendary" }),                   // Mistlands
            ("Tier8EliteMob", new[] { "RunestoneMythic" }),                      // Ashlands
            ("Tier9EliteMob", new[] { "RunestoneMythic", "RunestoneAncient" }),  // Deep North
        };

        private static readonly List<LootTable> Added = new List<LootTable>();

        /// <summary>
        /// True for one of these tables. Their chance is the same at every star level, so a creature
        /// entry's own StarScaling must not apply to them (LootRoller.PlanRollAt).
        /// </summary>
        internal static bool IsEliteRunestoneTable(LootTable table)
        {
            return table != null && Added.Contains(table);
        }

        /// <summary>
        /// Called by LootRoller.Initialize right after the config's tables are loaded. Initialize has just
        /// emptied LootRoller.LootTables, so the previous tables are already gone.
        /// </summary>
        internal static void OnLootTablesLoaded()
        {
            Added.Clear();
            AddTables();
        }

        /// <summary>Re-applies the tables after either setting changes (a local edit or a server sync).</summary>
        public static void Refresh()
        {
            LootRoller.RemoveLootTables(Added);
            Added.Clear();
            AddTables();
        }

        private static void AddTables()
        {
            if (ELConfig.EliteRunestoneDrops == null || !ELConfig.EliteRunestoneDrops.Value)
            {
                return;
            }

            float chance = Mathf.Clamp01(ELConfig.EliteRunestoneDropChance.Value);
            if (chance <= 0f)
            {
                return;
            }

            foreach ((string template, string[] runestones) in EliteTiers)
            {
                // A template the loaded loottables.json lacks stays absent: no creature could reference
                // the new name, and AddLootTable would create it.
                if (!LootRoller.LootTables.ContainsKey(template))
                {
                    continue;
                }

                LootTable table = BuildTable(template, runestones, chance);
                LootRoller.AddLootTable(table);
                Added.Add(table);
            }

            EpicLoot.Log($"Elite Runestone Drops: {Added.Count} elite tables drop runestones at {chance:P0}.");
        }

        private static LootTable BuildTable(string template, string[] runestones, float chance)
        {
            LootDrop[] loot = new LootDrop[runestones.Length];
            for (int i = 0; i < runestones.Length; i++)
            {
                loot[i] = new LootDrop { Item = runestones[i], Weight = 1 };
            }

            LootTable table = LootTable.Simple(template,
                new[] { new[] { 0f, (1f - chance) * 100f }, new[] { 1f, chance * 100f } }, loot);

            // Explicit zeros, or the root DefaultStarScaling would add its per-star drop chance and extra
            // items, and the configured chance would only hold for a creature without stars.
            table.StarScaling = new LootScaling { DropChance = 0f, BonusDrops = 0f, RarityShift = 0f };
            return table;
        }
    }
}
