using EpicLoot.Biomes;
using EpicLoot.CraftingV2;
using System;
using System.Collections.Generic;
using UnityEngine;

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
                // Two-step lookup: GetItemPrefab returns null for an unknown name, so chaining
                // .GetComponent off it NRE'd before the guard could log (cf. AugmentHelper).
                GameObject prefabObject = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item);
                ItemDrop prefab = prefabObject != null ? prefabObject.GetComponent<ItemDrop>() : null;
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
                GameObject prefabObject = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item);
                ItemDrop prefab = prefabObject != null ? prefabObject.GetComponent<ItemDrop>() : null;
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
        /// Helper to get the biome from custom unidentified items with the format "{biome}_{rarity}_Unidentified".
        /// The prefix is a registry biome name, so biomes from biomedata.json resolve as well as vanilla ones.
        /// </summary>
        public static Heightmap.Biome GetBiomeFromUnidentifiedItem(ItemDrop.ItemData item)
        {
            string biomeString = item.m_dropPrefab.name.Split('_')[0];
            return BiomeDataManager.TryResolve(biomeString, out Heightmap.Biome biome) ? biome : Heightmap.Biome.None;
        }
    }
}
