
using System;
﻿using EpicLoot_UnityLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EpicLoot.Biomes;
using EpicLoot.CraftingV2;
using EpicLoot.GatedItemType;

namespace EpicLoot.Crafting
{
    public static class EnchantCostsHelper
    {
        // Defaults to an empty config rather than null: every helper below dereferences this unguarded,
        // and other mods read it directly (VNEI's EpicLootCompat.IndexSacrifices does an
        // EnchantCostsHelper.Config.DisenchantProducts field access). An empty config degrades to
        // "no costs configured" instead of a NullReferenceException in someone else's code.
        public static EnchantingCostsConfig Config = new EnchantingCostsConfig();
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
            // A failed deserialize hands us null; keep the last good config rather than poisoning the
            // static for everything that reads it.
            if (config == null)
            {
                EpicLoot.LogWarning("Enchanting costs config was null, keeping the previous config.");
                return;
            }

            Config = config;
            OnSetupEnchantingCosts?.Invoke();
        }

        public static EnchantingCostsConfig GetCFG()
        {
            return Config;
        }

        public static List<ItemAmountConfig> GetSacrificeProducts(ItemDrop.ItemData item)
        {
            // An external filter (see API.RegisterSacrificeFilter) can veto an item -- typically one
            // equipped in a slot Epic Loot cannot see, which would otherwise be destroyed by accident.
            // No products means the Sacrifice tab does not offer it.
            if (!API.SacrificeAllowed(item))
            {
                return null;
            }

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

        /// <summary>
        /// Finds the entry keyed by a biome in a config dictionary whose keys are biome names. Keys are
        /// resolved through the registry, so "none", "Meadows" and a biomedata.json name all work.
        /// </summary>
        public static bool TryGetForBiome<T>(Dictionary<string, T> byBiome, Heightmap.Biome biome, out T value)
        {
            value = default;
            if (byBiome == null)
            {
                return false;
            }

            foreach (KeyValuePair<string, T> entry in byBiome)
            {
                if (BiomeDataManager.TryResolve(entry.Key, out Heightmap.Biome keyBiome) && keyBiome == biome)
                {
                    value = entry.Value;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Like <see cref="TryGetForBiome{T}"/>, but a biome without an entry of its own falls back to
        /// the nearest earlier biome in progression order that has one, ending at None. A custom biome
        /// therefore needs no entry of its own to identify at the right tier. A biome the registry does
        /// not know at all only falls back to None.
        /// </summary>
        public static bool TryGetForBiomeOrLower<T>(Dictionary<string, T> byBiome, Heightmap.Biome biome,
            out T value, out Heightmap.Biome resolvedBiome)
        {
            resolvedBiome = biome;
            if (TryGetForBiome(byBiome, biome, out value))
            {
                return true;
            }

            List<Heightmap.Biome> order = GatedItemTypeHelper.BiomesInOrder;
            int start = order.IndexOf(biome);
            if (start < 0)
            {
                start = Math.Min(1, order.Count);
            }

            for (int i = start - 1; i >= 0; i--)
            {
                if (TryGetForBiome(byBiome, order[i], out value))
                {
                    resolvedBiome = order[i];
                    return true;
                }
            }

            return false;
        }

        public static List<ItemAmountConfig> GetIdentifyCosts(string category, ItemRarity rarity, Heightmap.Biome biome)
        {
            List<ItemAmountConfig> totalCost = new List<ItemAmountConfig>();

            // Biome-specific costs by rarity. A biome without an entry of its own (a custom biome, say)
            // is charged as the nearest lower biome that has one.
            if (TryGetForBiomeOrLower(Config.IdentifyCosts, biome, out IdentifyCostConfig biomeConfig, out _) &&
                biomeConfig.CostByRarity.TryGetValue(rarity, out List<ItemAmountConfig> rarityCosts))
            {
                totalCost.AddRange(rarityCosts);
            }
            else
            {
                EpicLoot.LogWarning($"No identify costs configured for biome {BiomeDataManager.GetName(biome)} and rarity {rarity}.");
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
            // Only filter by item type when there is an item to read it from (the flag was never
            // set before, leaving every RuneCostConfig.ItemTypes list ignored).
            bool typecheck = item != null;
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
