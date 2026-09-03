using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.Crafting;

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
        // Left empty and backfilled in AdventureDataManager.Initialize: Newtonsoft APPENDS to
        // pre-initialized collections, so a hardcoded {1,1,1,1} turned the shipped [1,1,1,1,1]
        // into a 9-element list whose first four entries were the defaults.
        public List<int> RollsPerRarity = new List<int>();
        public List<SecretStashItemConfig> RandomItems = new List<SecretStashItemConfig>();
        public int RandomItemsCount = 0;
        public List<SecretStashItemConfig> OtherItems = new List<SecretStashItemConfig>();
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
        public float[] GambleRarityChance = new float[5];
        public float[][] GambleRarityChanceByRarity = { new float[5], new float[5], new float[5], new float[5], new float[5] };
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
        public int GoldTokens;
        public int IronTokens;
        public int Coins;
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
        [Obsolete] // TODO evaluate if should keep
        public int IncreaseRadiusCount = 3;
        [Obsolete] // TODO evaluate if should keep
        public float RadiusInterval = 500;
        public float MinimapAreaRadius = 100;
        /// <summary>
        /// How many times the adventure spawn search may push its sampling ring further out when
        /// everything inside the map circle is blocked (almost always by a ward). Each band steps out
        /// by one <see cref="MinimapAreaRadius"/>, which is the minimum that can escape a ward's
        /// veto, since a ward rejects points within its own radius + MinimapAreaRadius. Set to 0 to
        /// restore the old behaviour of never searching outside the circle.
        /// </summary>
        public int MaxSpawnSearchExpansions = 5;
        public List<SecretStashItemConfig> SaleItems = new List<SecretStashItemConfig>();
        
        [NonSerialized]
        private static Heightmap.Biome[] _biomeList;

        public Heightmap.Biome[] GetBiomeList()
        {
            if (_biomeList == null)
            {
                _biomeList = BiomeInfo.Select(item => item.Biome).ToArray();
            }

            return _biomeList;
        }

        public void UpdateBiomeList()
        {
            _biomeList = BiomeInfo.Select(item => item.Biome).ToArray();
        }
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
        public float ChanceForSpecialName;
        public List<string> SpecialNames;
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
        public int RewardCoins;
        public List<BountyTargetAddConfig> Adds = new List<BountyTargetAddConfig>();
    }

    [Serializable]
    public class BountyBossConfig
    {
        public Heightmap.Biome Biome;
        public string BossPrefab;
        public string BossDefeatedKey;
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
        public List<BountyBossConfig> Bosses = new List<BountyBossConfig>();
        public BountyTargetNameConfig Names;
    }

    [Serializable]
    public class TemperingConfig
    {
        // Left empty rather than seeded with the defaults: Newtonsoft APPENDS to pre-initialized
        // collections, and an absent rarity key has to fall through to TemperMan's hardcoded
        // default instead of merging with it. TemperMan.ApplyConfig owns the fallback.
        public Dictionary<ItemRarity, List<ItemAmountConfig>> CostsByRarity = new Dictionary<ItemRarity, List<ItemAmountConfig>>();
    }

    [Serializable]
    public class AdventureDataConfig
    {
        public float FulingCoinDropScale = 1;
        public SecretStashConfig SecretStash;
        public GambleConfig Gamble;
        public TreasureMapConfig TreasureMap;
        public BountiesConfig Bounties;
        public TemperingConfig Tempering;
    }
}
