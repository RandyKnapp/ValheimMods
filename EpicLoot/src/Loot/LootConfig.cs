using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EpicLoot
{
    [Serializable]
    public class LootDrop
    {
        public string Item;
        public float Weight = 1;
        public float[] Rarity;

        // Per-rarity override of Item: the rarity rolled from Rarity[] picks the entry, and that name
        // replaces Item before anything looks the prefab up. A value may name a prefab, an ItemSet or a
        // "Object.Level" loot table reference -- LootRoller.ResolveLootDrop keeps resolving whatever it
        // substitutes. Item stays the default for a rarity the map does not cover.
        //
        // Left null rather than empty on purpose. Newtonsoft APPENDS to a pre-initialized collection, and
        // null is what tells ResolveLootDrop this entry does not vary by rarity at all. Ignoring nulls on
        // write keeps AutoAddEnchantableItems' rewritten loottables.json from sprouting "RarityItems": null
        // on every one of its ~2000 entries.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<ItemRarity, string> RarityItems;
    }

    [Serializable]
    public class LeveledLootDef
    {
        public int Level;
        public float[][] Drops;
        public LootDrop[] Loot;
    }

    [Serializable]
    public class LootTable
    {
        public string Object;
        public string RefObject;
        public float[][] Drops;
        public LootDrop[] Loot;
        public List<LeveledLootDef> LeveledLoot = new List<LeveledLootDef>();
    }

    [Serializable]
    public class LootItemSet
    {
        public string Name;
        public LootDrop[] Loot;
    }

    [Serializable]
    public class MagicEffectsCountConfig
    {
        public float[][] Magic;
        public float[][] Rare;
        public float[][] Epic;
        public float[][] Legendary;
        public float[][] Mythic;
    }

    [Serializable]
    public class SocketCountsConfig
    {
        public float[][] Magic;
        public float[][] Rare;
        public float[][] Epic;
        public float[][] Legendary;
        public float[][] Mythic;
    }

    [Serializable]
    public class LootConfig
    {
        public MagicEffectsCountConfig MagicEffectsCount;
        public SocketCountsConfig SocketCounts;
        public LootItemSet[] ItemSets;
        public LootTable[] LootTables;
        public List<string> RestrictedItems = new List<string>();
    }
}
