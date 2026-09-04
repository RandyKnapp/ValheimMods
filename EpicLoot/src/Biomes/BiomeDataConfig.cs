using System;
using System.Collections.Generic;

namespace EpicLoot.Biomes
{
    /// <summary>
    /// One biome entry from biomedata.json. Vanilla biomes are identified by Name alone (ID optional);
    /// biomes added by other mods need the numeric Heightmap.Biome value that mod assigns them.
    /// </summary>
    [Serializable]
    public class BiomeEntryConfig
    {
        /// <summary>
        /// Letters and digits only. Doubles as the key other configs use ("Biome": "CursedMountain")
        /// and as the suffix of derived prefab and loot table names (TreasureMapChest_{Name},
        /// {Name}_{Rarity}_Unidentified, ShardStone_{Name}).
        /// </summary>
        public string Name;

        /// <summary>The Heightmap.Biome value. Optional for vanilla names, required for custom biomes.</summary>
        public int? ID;

        /// <summary>
        /// Progression order, lowest first. Entries without an Order sort last, in file order. The
        /// shipped file uses 10, 20, ... so a patch can slot a biome in between without renumbering.
        /// </summary>
        public int? Order;

        /// <summary>
        /// Global keys that must all be set for this biome's boss to count as defeated. An empty list
        /// means the biome is never gated.
        /// </summary>
        public List<string> BossDefeatedKeys = new List<string>();

        /// <summary>Rich-text color for the biome name in UI, any value ColorUtility.TryParseHtmlString accepts.</summary>
        public string Color;

        /// <summary>
        /// Optional display text ($token or literal). Defaults to the vanilla "$biome_(value)" token,
        /// which Expand World Data also registers for the biomes it adds.
        /// </summary>
        public string DisplayName;
    }

    [Serializable]
    public class BiomeDataConfig
    {
        public List<BiomeEntryConfig> Biomes = new List<BiomeEntryConfig>();
    }
}
