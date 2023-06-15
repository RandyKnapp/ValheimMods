using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot_UnityLib;
using UnityEngine;

namespace EpicLoot.Crafting
{
    public static class EnchantCostsHelper
    {
        public static EnchantingCostsConfig Config;
        public static HashSet<string> DeprecatedMagicEffects = new HashSet<string>
        {
            MagicEffectType.WaterWalking,
            MagicEffectType.AddSpiritResistance,
            MagicEffectType.AddSpiritResistancePercentage,
            MagicEffectType.ReduceWeight,
            MagicEffectType.AddFireResistance,
            MagicEffectType.AddFrostResistance,
            MagicEffectType.AddLightningResistance,
            MagicEffectType.AddChoppingResistancePercentage
        };

        public static void Initialize(EnchantingCostsConfig config)
        {
            Config = config;
        }

        public static List<ItemAmountConfig> GetSacrificeProducts(ItemDrop.ItemData item)
        {
            var isMagic = item.IsMagic();
            var type = item.m_shared.m_itemType;
            var name = item.m_shared.m_name;

            var configEntry = Config.DisenchantProducts.Find(x => {
                if (x.IsMagic && !isMagic)
                {
                    return false;
                }

                if (isMagic && x.Rarity != item.GetRarity())
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
            var configEntry = Config.DisenchantProducts.Find(x => {
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
            var type = item.m_shared.m_itemType;

            var configEntry = Config.EnchantCosts.Find(x => {
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

        public static List<ItemAmountConfig> GetAugmentCost(ItemDrop.ItemData item, ItemRarity rarity, int recipeEffectIndex)
        {
            var type = item.m_shared.m_itemType;

            var configEntry = Config.AugmentCosts.Find(x => {
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

            if (EffectIsDeprecated(item, recipeEffectIndex))
            {
                return new List<ItemAmountConfig>();
            }

            if (configEntry != null && !item.GetMagicItem().IsEffectAugmented(recipeEffectIndex))
            {
                var cost = configEntry.Cost.ToList();
                var reaugmentCost = GetReAugmentCost(item, recipeEffectIndex);
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

            var magicItem = item.GetMagicItem();
            if (magicItem == null)
            {
                return null;
            }

            var totalAugments = magicItem.GetAugmentCount();
            if (totalAugments == 0)
            {
                return null;
            }

            var featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Augment);
            var reenchantCostReduction = float.IsNaN(featureValues.Item2) ? 0 : (featureValues.Item2 / 100.0f);

            var reaugmentCostIndex = Mathf.Clamp(totalAugments - 1, 0, Config.ReAugmentCosts.Count - 1);
            var baseCost = Config.ReAugmentCosts[reaugmentCostIndex];
            return new ItemAmountConfig()
            {
                Item = baseCost.Item,
                Amount = Mathf.CeilToInt(baseCost.Amount * (1.0f - Mathf.Clamp01(reenchantCostReduction)))
            };
        }

        public static bool EffectIsDeprecated(ItemDrop.ItemData item, int effectIndex)
        {
            var effects = item?.GetMagicItem()?.GetEffects();
            return (effects != null && effectIndex >= 0 && effectIndex < effects.Count && EffectIsDeprecated(effects[effectIndex].EffectType));
        }

        public static bool ItemHasDeprecatedEffect(ItemDrop.ItemData item)
        {
            var effects = item?.GetMagicItem()?.GetEffects();
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
