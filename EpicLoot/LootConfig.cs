using System;

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
    public class LootTable
    {
        public string Object;
        public int[][] Drops;
        public int[][] Drops2;
        public int[][] Drops3;
        public LootDrop[] Loot;
        public LootDrop[] Loot2;
        public LootDrop[] Loot3;
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
    }
}
