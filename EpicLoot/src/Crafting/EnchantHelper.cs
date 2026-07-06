using EpicLoot.CraftingV2;
using System;
using System.Collections.Generic;

namespace EpicLoot.Crafting
{
    public class EnchantHelper
    {
        public static List<KeyValuePair<ItemDrop, int>> GetEnchantCosts(ItemDrop.ItemData item, ItemRarity rarity)
        {
            List<KeyValuePair<ItemDrop, int>> costList = new List<KeyValuePair<ItemDrop, int>>();

            List<ItemAmountConfig> enchantCostDef = EnchantCostsHelper.GetEnchantCost(item, rarity);
            if (enchantCostDef == null)
            {
                return costList;
            }

            foreach (ItemAmountConfig itemAmountConfig in enchantCostDef)
            {
                ItemDrop prefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item).GetComponent<ItemDrop>();
                if (prefab == null)
                {
                    EpicLoot.LogWarning($"Tried to add unknown item ({itemAmountConfig.Item}) to enchant cost for item ({item.m_shared.m_name})");
                    continue;
                }

                costList.Add(new KeyValuePair<ItemDrop, int>(prefab, itemAmountConfig.Amount));
            }

            return costList;
        }

        public static List<KeyValuePair<ItemDrop, int>> GetRuneCost(ItemDrop.ItemData item, ItemRarity rarity, RuneActions operation)
        {
            List<KeyValuePair<ItemDrop, int>> costList = new List<KeyValuePair<ItemDrop, int>>();

            List<ItemAmountConfig> enchantCostDef = EnchantCostsHelper.GetRuneCost(item, rarity, operation);
            if (enchantCostDef == null)
            {
                return costList;
            }

            foreach (ItemAmountConfig itemAmountConfig in enchantCostDef)
            {
                ItemDrop prefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item).GetComponent<ItemDrop>();
                if (prefab == null)
                {
                    EpicLoot.LogWarning($"Tried to add unknown item ({itemAmountConfig.Item}) to rune cost for item ({item.m_shared.m_name})");
                    continue;
                }
                costList.Add(new KeyValuePair<ItemDrop, int>(prefab, itemAmountConfig.Amount));
            }

            return costList;
        }

        /// <summary>
        /// Helper to get the biome from custom unidentified items with the format "{biome}_{rarity}_Unidentified"
        /// </summary>
        public static Heightmap.Biome GetBiomeFromUnidentifiedItem(ItemDrop.ItemData item)
        {
            string biomeString = item.m_dropPrefab.name.Split('_')[0];
            if (!Enum.TryParse<Heightmap.Biome>(biomeString, out Heightmap.Biome biome))
            {
                biome = Heightmap.Biome.None;
            }

            return biome;
        }
    }
}
