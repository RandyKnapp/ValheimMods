using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EpicLoot.Crafting
{
    [Serializable]
    public class ItemAmountConfig
    {
        public string Item = "";
        public int Amount = 1;
    }

    [Serializable]
    public class DisenchantCostsConfig
    {
        public List<ItemAmountConfig> Magic = new List<ItemAmountConfig>();
        public List<ItemAmountConfig> Rare = new List<ItemAmountConfig>();
        public List<ItemAmountConfig> Epic = new List<ItemAmountConfig>();
        public List<ItemAmountConfig> Legendary = new List<ItemAmountConfig>();
        public List<ItemAmountConfig> Mythic = new List<ItemAmountConfig>();
    }

    [Serializable]
    public class DisenchantProductsConfig
    {
        [DefaultValue(false)]
        public bool IsUnidentified;
        [DefaultValue(false)]
        public bool IsMagic;
        public ItemRarity Rarity;
        public List<string> ItemTypes = new List<string>();
        public List<string> ItemNames = new List<string>();

        // Matches against the tail of m_shared.m_ammoType, the field Epic Loot uses as an identity channel for
        // items it creates (IsRunestone, IsMagicCraftingMaterial, IsShardSlotChisel all read it the same way).
        // "ShardStone" catches every (color, rarity) shardstone with one entry per rarity, and keeps catching a
        // color added later. The suffix can be as specific as the caller likes -- a full "Yagluth|Mythic|ShardStone"
        // singles out one stone -- which is how a per-color or per-boss yield would be expressed.
        //
        // An entry carrying one of these is treated as more specific than one without, and among those the
        // longest matching suffix wins, so a single-stone override beats the blanket entry no matter which
        // order they end up in: see EnchantCostsHelper.GetSacrificeProducts.
        public List<string> AmmoTypeSuffixes = new List<string>();
        public List<ItemAmountConfig> Products = new List<ItemAmountConfig>();
    }

    [Serializable]
    public class EnchantCostConfig
    {
        public ItemRarity Rarity;
        public List<string> ItemTypes = new List<string>();
        public List<ItemAmountConfig> Cost = new List<ItemAmountConfig>();
    }

    [Serializable]
    public class AugmentCostConfig
    {
        public ItemRarity Rarity;
        public List<string> ItemTypes = new List<string>();
        public List<ItemAmountConfig> Cost = new List<ItemAmountConfig>();
    }

    [Serializable]
    public class RuneCostConfig
    {
        public ItemRarity Rarity;
        public List<string> ItemTypes = new List<string>();
        public List<ItemAmountConfig> Cost = new List<ItemAmountConfig>();
    }

    [Serializable]
    public class IdentifyCostConfig
    {
        public string Biome;
        public Dictionary<ItemRarity, List<ItemAmountConfig>> CostByRarity = new Dictionary<ItemRarity, List<ItemAmountConfig>>();
    }

    [Serializable]
    public class IdentifyTypeConfig
    {
        public string Localization;
        // Keyed by biome name ("none", "Meadows", or a biomedata.json biome); resolved through the
        // registry on lookup so a custom biome cannot make the whole file fail to parse.
        public Dictionary<string, List<string>> BiomeLootLists = new Dictionary<string, List<string>>();
        public List<ItemAmountConfig> Costs = new List<ItemAmountConfig>();
    }

    [Serializable]
    public class EnchantingCostsConfig
    {
        public DisenchantCostsConfig DisenchantCosts = new DisenchantCostsConfig();
        public List<DisenchantProductsConfig> DisenchantProducts = new List<DisenchantProductsConfig>();
        public List<EnchantCostConfig> EnchantCosts = new List<EnchantCostConfig>();
        public List<AugmentCostConfig> AugmentCosts = new List<AugmentCostConfig>();
        public List<ItemAmountConfig> ReAugmentCosts = new List<ItemAmountConfig>();
        public List<RuneCostConfig> RuneExtractCosts = new List<RuneCostConfig>();
        public List<RuneCostConfig> RuneEtchCosts = new List<RuneCostConfig>();
        public Dictionary<string, IdentifyTypeConfig> IdentifyTypes = new Dictionary<string, IdentifyTypeConfig>();
        public Dictionary<string, IdentifyCostConfig> IdentifyCosts = new Dictionary<string, IdentifyCostConfig>();
    }
}
