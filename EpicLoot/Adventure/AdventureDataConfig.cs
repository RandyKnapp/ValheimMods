using System;
using System.Collections.Generic;

namespace EpicLoot.Adventure
{
    [Serializable]
    public class SecretStashItemConfig
    {
        public string Item;
        public int CoinsCost;
    }

    [Serializable]
    public class SecretStashConfig
    {
        public List<SecretStashItemConfig> Materials = new List<SecretStashItemConfig>();
        public List<int> RollsPerRarity = new List<int> {1, 1, 1, 1};
        public List<SecretStashItemConfig> OtherItems = new List<SecretStashItemConfig>();
        public int OtherItemsRolls;
        public List<string> Gambles = new List<string>();
        public int GamblesCount;
        public int[] GambleRarityChance = new int[5];
        public List<SecretStashItemConfig> GambleCosts = new List<SecretStashItemConfig>();
    }

    [Serializable]
    public class TreasureMapConfig
    {
    }

    [Serializable]
    public class BountiesConfig
    {
    }

    [Serializable]
    public class AdventureDataConfig
    {
        public SecretStashConfig SecretStash;
        public TreasureMapConfig TreasureMap;
        public BountiesConfig Bounties;
    }
}
