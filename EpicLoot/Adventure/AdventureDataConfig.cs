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
    public class TreasureMapBiomeInfoConfig
    {
        public Heightmap.Biome Biome;
        public int Cost;
        public float MinRadius;
        public float MaxRadius;
    }

    [Serializable]
    public class TreasureMapConfig
    {
        public List<TreasureMapBiomeInfoConfig> BiomeInfo = new List<TreasureMapBiomeInfoConfig>();
        public float StartRadiusMin = 0;
        public float StartRadiusMax = 500;
        public int IncreaseRadiusCount = 3;
        public float RadiusInterval = 500;
        public float MinimapAreaRadius = 100;
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
