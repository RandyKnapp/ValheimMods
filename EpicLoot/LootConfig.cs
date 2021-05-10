using System;
using System.Collections.Generic;

namespace EpicLoot
{
    [Serializable]
    public class LootDrop
    {
        public string Item;
        public int Weight = 1;
        public int[] Rarity;
    }

    [Serializable]
    public class LeveledLootDef
    {
        public int Level;
        public int[][] Drops;
        public LootDrop[] Loot;
    }

    [Serializable]
    public class LootTable
    {
        public string Object;
        public string RefObject;
        public int[][] Drops;
        public int[][] Drops2;
        public int[][] Drops3;
        public LootDrop[] Loot;
        public LootDrop[] Loot2;
        public LootDrop[] Loot3;
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
        public int[][] Magic;
        public int[][] Rare;
        public int[][] Epic;
        public int[][] Legendary;
    }

    [Serializable]
    public class LootConfig
    {
        public MagicEffectsCountConfig MagicEffectsCount;
        public LootItemSet[] ItemSets;
        public LootTable[] LootTables;
        public List<string> RestrictedItems = new List<string>();
    }
}
