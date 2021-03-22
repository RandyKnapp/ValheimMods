using System;
using System.Collections.Generic;

namespace EpicLoot.Crafting
{
    [Serializable]
    public class ItemAmountConfig
    {
        public string Item = "";
        public int Amount = 1;
    }

    [Serializable]
    public class DisenchantProductsConfig
    {
        public bool IsMagic;
        public ItemRarity Rarity;
        public List<string> ItemTypes = new List<string>();
        public List<string> ItemNames = new List<string>();
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
    public class EnchantingCostsConfig
    {
        public List<DisenchantProductsConfig> DisenchantProducts = new List<DisenchantProductsConfig>();
        public List<EnchantCostConfig> EnchantCosts = new List<EnchantCostConfig>();
        public List<AugmentCostConfig> AugmentCosts = new List<AugmentCostConfig>();
    }
}
