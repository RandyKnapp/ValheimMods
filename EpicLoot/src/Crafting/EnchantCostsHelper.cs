
using System;
﻿using EpicLoot_UnityLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EpicLoot.CraftingV2;

namespace EpicLoot.Crafting
{
    public static class EnchantCostsHelper
    {
        public static EnchantingCostsConfig Config;
        public static HashSet<string> DeprecatedMagicEffects = new HashSet<string>
        {
            MagicEffectType.AddSpiritResistancePercentage,
            MagicEffectType.AddChoppingResistancePercentage
        };
        #nullable enable
        public static event Action? OnSetupEnchantingCosts;
        #nullable disable
        public static void Initialize(EnchantingCostsConfig config)
        {
            Config = config;
            OnSetupEnchantingCosts?.Invoke();
        }

        public static EnchantingCostsConfig GetCFG()
        {
            return Config;
        }

        public static List<ItemAmountConfig> GetSacrificeProducts(ItemDrop.ItemData item)
        {
            bool isMagic = item.IsMagic();
            bool isUnidentified = item.IsUnidentified();
            ItemDrop.ItemData.ItemType type = item.m_shared.m_itemType;
            string name = item.m_shared.m_name;
            DisenchantProductsConfig configEntry = Config.DisenchantProducts.Find(x => {
                // Magic item check doesn't apply for unidentified items, since they are considered magic
                if (x.IsMagic != isMagic && isUnidentified == false)
                {
                    return false;
                }

                if (x.IsUnidentified != isUnidentified)
                {
                    return false;
                }

                if ((isUnidentified || isMagic) && x.Rarity != item.GetRarity())
                {
                    return false;
                }

                if (x.ItemTypes?.Count > 0 && !x.ItemTypes.Contains(type.ToString()))
                {
                    return false;
                }

                if (x.ItemNames?.Count > 0 && !x.ItemNames.Contains(name))
                {
                    return false;
                }

                return true;
            });

            return configEntry?.Products;
        }

        public static List<ItemAmountConfig> GetSacrificeProducts(bool isMagic, ItemDrop.ItemData.ItemType type, ItemRarity rarity )
        {
            DisenchantProductsConfig configEntry = Config.DisenchantProducts.Find(x => {
                if (x.IsMagic && !isMagic)
                {
                    return false;
                }

                if (isMagic && x.Rarity != rarity)
                {
                    return false;
                }

                if (x.ItemTypes?.Count > 0 && !x.ItemTypes.Contains(type.ToString()))
                {
                    return false;
                }

                return true;
            });

            return configEntry?.Products;
        }

        public static List<ItemAmountConfig> GetEnchantCost(ItemDrop.ItemData item, ItemRarity rarity)
        {
            ItemDrop.ItemData.ItemType type = item.m_shared.m_itemType;

            EnchantCostConfig configEntry = Config.EnchantCosts.Find(x => {
                if (x.Rarity != rarity)
                {
                    return false;
                }

                if (x.ItemTypes?.Count > 0 && !x.ItemTypes.Contains(type.ToString()))
                {
                    return false;
                }

                return true;
            });

            return configEntry?.Cost;
        }

        public static List<ItemAmountConfig> GetIdentifyCosts(string category, ItemRarity rarity, Heightmap.Biome biome)
        {
            List<ItemAmountConfig> totalCost = new List<ItemAmountConfig>();

            // Add biome-specific costs by rarity if configured
            if (Config.IdentifyCosts.TryGetValue(biome, out IdentifyCostConfig biomeConfig) &&
                biomeConfig.CostByRarity.TryGetValue(rarity, out List<ItemAmountConfig> rarityCosts))
            {
                totalCost.AddRange(rarityCosts);
            }
            else
            {
                EpicLoot.LogWarning($"No identify costs configured for biome {biome} and rarity {rarity}.");
            }

            // Add category-specific costs
            if (Config.IdentifyTypes.TryGetValue(category, out IdentifyTypeConfig typeConfig))
            {
                totalCost.AddRange(typeConfig.Costs);
            }
            else
            {
                EpicLoot.LogWarning($"No identify type configured for category {category}.");
            }

            return totalCost;
        }

        public static Dictionary<string, string> GetIdentificationCategories()
        {
            Dictionary<string, string> categories = new Dictionary<string, string>();
            foreach(KeyValuePair<string, IdentifyTypeConfig> identifyStyle in Config.IdentifyTypes)
            {
                categories.Add(identifyStyle.Key, identifyStyle.Value.Localization);
            }

            return categories;
        }

        public static List<ItemAmountConfig> GetRuneCost(ItemDrop.ItemData item, ItemRarity rarity, RuneActions operation)
        {
            bool typecheck = false;
            ItemDrop.ItemData.ItemType itemtype = ItemDrop.ItemData.ItemType.None;
            
            if (item != null)
            {
                itemtype = item.m_shared.m_itemType;
            }

            List<RuneCostConfig> cfg = new List<RuneCostConfig>();
            switch (operation)
            {
                case RuneActions.Extract:
                    cfg = Config.RuneExtractCosts;
                    break;
                case RuneActions.Etch:
                    cfg = Config.RuneEtchCosts;
                    break;
            }

            RuneCostConfig configEntry = cfg.Find(x =>
            {
                if (x.Rarity != rarity)
                {
                    return false;
                }

                if (x.ItemTypes?.Count > 0 && typecheck && !x.ItemTypes.Contains(itemtype.ToString()))
                {
                    return false;
                }

                return true;
            });

            if (configEntry == null)
            {
                EpicLoot.LogWarning($"Could not find rune cost data for {rarity} {operation}");
                return new List<ItemAmountConfig>();
            }
            return configEntry?.Cost;
        }

        public static List<ItemAmountConfig> GetAugmentCost(ItemDrop.ItemData item, ItemRarity rarity, int recipeEffectIndex)
        {
            if (EffectIsDeprecated(item, recipeEffectIndex))
            {
                return new List<ItemAmountConfig>();
            }

            ItemDrop.ItemData.ItemType type = item.m_shared.m_itemType;

            AugmentCostConfig configEntry = Config.AugmentCosts.Find(x => {
                if (x.Rarity != rarity)
                {
                    return false;
                }

                if (x.ItemTypes?.Count > 0 && !x.ItemTypes.Contains(type.ToString()))
                {
                    return false;
                }

                return true;
            });

            if (configEntry != null && !item.GetMagicItem().IsEffectAugmented(recipeEffectIndex))
            {
                List<ItemAmountConfig> cost = configEntry.Cost.ToList();
                ItemAmountConfig reaugmentCost = GetReAugmentCost(item, recipeEffectIndex);
                if (reaugmentCost != null)
                {
                    cost.Add(reaugmentCost);
                }
                return cost;
            }

            return configEntry?.Cost;
        }

        public static ItemAmountConfig GetReAugmentCost(ItemDrop.ItemData item, int indexToAugment)
        {
            if (EffectIsDeprecated(item, indexToAugment))
            {
                return null;
            }

            MagicItem magicItem = item.GetMagicItem();
            if (magicItem == null)
            {
                return null;
            }

            int totalAugments = magicItem.GetAugmentCount();
            if (totalAugments == 0)
            {
                return null;
            }

            Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Augment);
            float reenchantCostReduction = float.IsNaN(featureValues.Item2) ? 0 : (featureValues.Item2 / 100.0f);

            int reaugmentCostIndex = Mathf.Clamp(totalAugments - 1, 0, Config.ReAugmentCosts.Count - 1);
            ItemAmountConfig baseCost = Config.ReAugmentCosts[reaugmentCostIndex];
            return new ItemAmountConfig()
            {
                Item = baseCost.Item,
                Amount = Mathf.CeilToInt(baseCost.Amount * (1.0f - Mathf.Clamp01(reenchantCostReduction)))
            };
        }

        public static bool EffectIsDeprecated(ItemDrop.ItemData item, int effectIndex)
        {
            List<MagicItemEffect> effects = item?.GetMagicItem()?.GetEffects();
            return (effects != null && effectIndex >= 0 && effectIndex < effects.Count && EffectIsDeprecated(effects[effectIndex].EffectType));
        }

        public static bool ItemHasDeprecatedEffect(ItemDrop.ItemData item)
        {
            List<MagicItemEffect> effects = item?.GetMagicItem()?.GetEffects();
            if (effects != null)
            {
                for (int index = 0; index < effects.Count; index++)
                {
                    if (EffectIsDeprecated(effects[index].EffectType))
                        return true;
                }
            }

            return false;
        }

        public static bool EffectIsDeprecated(string effectType)
        {
            return DeprecatedMagicEffects.Contains(effectType);
        }

        public static bool EffectIsDeprecated(MagicItemEffectDefinition def)
        {
            return DeprecatedMagicEffects.Contains(def.Type);
        }
    }
}
