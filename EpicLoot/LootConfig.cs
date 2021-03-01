using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    [Serializable]
    public class LootDrop
    {
        public string Item;
        public float PercentDrop = 0.1f;
        public float PercentMagic = 0.6f;
        public float PercentRare = 0.3f;
        public float PercentEpic = 0.08f;
        public float PercentLegendary = 0.02f;
    }

    [Serializable]
    public class LootTable
    {
        public string Character;
        public List<LootDrop> Loot;
    }

    [Serializable]
    public class LootConfig
    {
        public List<LootTable> LootTables;
    }
}
