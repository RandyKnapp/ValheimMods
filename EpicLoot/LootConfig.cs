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
        public int[] Level;
        public int[][] Drops;
        public LootDrop[] Loot;
    }

    [Serializable]
    public class LootConfig
    {
        public LootTable[] LootTables;
    }
}
