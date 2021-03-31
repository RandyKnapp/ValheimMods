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
        public int IronBountyTokenCost;
        public int GoldBountyTokenCost;

        public Currencies GetCost()
        {
            return new Currencies()
            {
                Coins = CoinsCost,
                ForestTokens = ForestTokenCost,
                IronBountyTokens = IronBountyTokenCost,
                GoldBountyTokens = GoldBountyTokenCost
            };
        }
    }

    [Serializable]
    public class SecretStashConfig
    {
        public int RefreshInterval;
        public List<SecretStashItemConfig> Materials = new List<SecretStashItemConfig>();
        public List<int> RollsPerRarity = new List<int> {1, 1, 1, 1};
        public List<SecretStashItemConfig> OtherItems = new List<SecretStashItemConfig>();
        public int OtherItemsRolls;
    }

    [Serializable]
    public class GambleConfig
    {
        public int RefreshInterval;
        public List<string> Gambles = new List<string>();
        public int GamblesCount;
        public int ForestTokenGamblesCount;
        public int IronBountyGamblesCount;
        public int GoldBountyGamblesCount;
        public int[] GambleRarityChance = new int[5];
        public int[][] GambleRarityChanceByRarity = new int[4][] { new int[5], new int[5], new int[5], new int[5] };
        public float ForestTokenGambleCoinsCost = 1.0f;
        public int ForestTokenGambleCostMin = 5;
        public int ForestTokenGambleCostMax = 10;
        public float IronBountyGambleCoinsCost = 1.5f;
        public int IronBountyGambleCost = 5;
        public float GoldBountyGambleCoinsCost = 1.5f;
        public int GoldBountyGambleCost = 3;
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
        public int RefreshInterval;
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
    public class BountyTargetNameConfig
    {
        public List<string> Prefixes;
        public List<string> Suffixes;
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
        public int RefreshInterval;
        public int IronMinLevel = 1;
        public int IronMaxLevel = 1;
        public float IronHealthMultiplier = 1.0f;
        public int GoldMinLevel = 1;
        public int GoldMaxLevel = 1;
        public float GoldHealthMultiplier = 1.0f;
        public int AddsMinLevel = 1;
        public int AddsMaxLevel = 1;
        public float AddsHealthMultiplier = 1.0f;
        public List<BountyTargetConfig> Targets = new List<BountyTargetConfig>();
        public BountyTargetNameConfig Names;
    }

    [Serializable]
    public class AdventureDataConfig
    {
        public SecretStashConfig SecretStash;
        public GambleConfig Gamble;
        public TreasureMapConfig TreasureMap;
        public BountiesConfig Bounties;
    }
}
