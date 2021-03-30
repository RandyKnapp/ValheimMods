using System;
using System.Collections.Generic;

namespace EpicLoot.Adventure
{
    [Serializable]
    public class SecretStashItemConfig
    {
        public string Item;
        public int CoinsCost;
        public int ForestTokenCost;
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
        public int ForestTokens = 0;
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
        public List<SecretStashItemConfig> SaleItems = new List<SecretStashItemConfig>();
    }

    [Serializable]
    public class BountyTargetAddConfig
    {
        public string ID;
        public int Count;
    }

    [Serializable]
    public class BountyTargetConfig
    {
        public Heightmap.Biome Biome;
        public string TargetID;
        public int RewardGold;
        public int RewardIron;
        public List<BountyTargetAddConfig> Adds = new List<BountyTargetAddConfig>();
    }

    [Serializable]
    public class BountiesConfig
    {
        public List<BountyTargetConfig> Targets = new List<BountyTargetConfig>();
    }

    [Serializable]
    public class AdventureDataConfig
    {
        public SecretStashConfig SecretStash;
        public TreasureMapConfig TreasureMap;
        public BountiesConfig Bounties;
    }
}
