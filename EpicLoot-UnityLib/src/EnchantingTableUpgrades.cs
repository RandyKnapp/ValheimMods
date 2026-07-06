using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot_UnityLib
{
    public enum EnchantingFeature
    {
        Sacrifice,
        ConvertMaterials,
        Enchant,
        Augment,
        Disenchant,
        Rune
    }

    [Serializable]
    public class ItemAmount
    {
        public string Item = "";
        public int Amount = 1;
    }

    [Serializable]
    public class EnchantingUpgradeCosts
    {
        public List<List<ItemAmount>> Sacrifice;
        public List<List<ItemAmount>> ConvertMaterials;
        public List<List<ItemAmount>> Enchant;
        public List<List<ItemAmount>> Augment;
        public List<List<ItemAmount>> Disenchant;
        public List<List<ItemAmount>> Rune;
    }

    [Serializable]
    public class EnchantingFeatureValues
    {
        public List<float[]> Sacrifice;
        public List<float[]> ConvertMaterials;
        public List<float[]> Enchant;
        public List<float[]> Augment;
        public List<float[]> Disenchant;
        public List<float[]> Rune;
    }

    [Serializable]
    public class EnchantingUpgradesConfig
    {
        public Dictionary<EnchantingFeature, int> DefaultFeatureLevels;
        public Dictionary<EnchantingFeature, int> MaximumFeatureLevels;
        public EnchantingUpgradeCosts UpgradeCosts;
        public EnchantingFeatureValues UpgradeValues;
    }

    public class EnchantingFeatureUpgradeRequest
    {
        public ZDOID TableZDO;
        public EnchantingFeature Feature;
        public int ToLevel;
        public Action<bool> ResponseCallback;
    }

    public static class EnchantingTableUpgrades
    {
        public static EnchantingUpgradesConfig Config;


        public static void InitializeConfig(EnchantingUpgradesConfig config)
        {
            Config = config;
        }

        public static EnchantingUpgradesConfig GetCFG()
        {
            return Config;
        }

        public static string GetFeatureName(EnchantingFeature feature)
        {
            switch (feature)
            {
                case EnchantingFeature.Sacrifice:
                    return "$mod_epicloot_sacrifice";
                case EnchantingFeature.ConvertMaterials:
                    return "$mod_epicloot_convertmaterials";
                case EnchantingFeature.Enchant:
                    return "$mod_epicloot_enchant";
                case EnchantingFeature.Augment:
                    return "$mod_epicloot_augment";
                case EnchantingFeature.Disenchant:
                    return "$mod_epicloot_disenchant";
                case EnchantingFeature.Rune:
                    return "$mod_epicloot_runemanagement";
                default:
                    return "";
            }
        }

        public static string GetFeatureDescription(EnchantingFeature feature)
        {
            switch (feature)
            {
                case EnchantingFeature.Sacrifice:
                    return "$mod_epicloot_featureinfo_sacrifice";
                case EnchantingFeature.ConvertMaterials:
                    return "$mod_epicloot_featureinfo_convertmaterials";
                case EnchantingFeature.Enchant:
                    return "$mod_epicloot_featureinfo_enchant";
                case EnchantingFeature.Augment:
                    return "$mod_epicloot_featureinfo_augment";
                case EnchantingFeature.Disenchant:
                    return "$mod_epicloot_featureinfo_disenchant";
                case EnchantingFeature.Rune:
                    return "$mod_epicloot_featureinfo_runes";
                default:
                    return "";
            }
        }

        public static string GetFeatureUpgradeLevelDescription(EnchantingTable table,
            EnchantingFeature feature, int level)
        {
            string description;
            switch (feature)
            {
                case EnchantingFeature.Sacrifice:
                    description = "$mod_epicloot_featureupgrade_sacrifice";
                    break;
                case EnchantingFeature.ConvertMaterials:
                    description = "$mod_epicloot_featureupgrade_convertmaterials";
                    break;
                case EnchantingFeature.Enchant:
                    description = "$mod_epicloot_featureupgrade_enchant";
                    break;
                case EnchantingFeature.Augment:
                    description = "$mod_epicloot_featureupgrade_augment";
                    break;
                case EnchantingFeature.Disenchant:
                    description = "$mod_epicloot_featureupgrade_disenchant";
                    break;
                case EnchantingFeature.Rune:
                    description = "$mod_epicloot_featureupgrade_runes";
                    break;
                default:
                    description = "";
                    break;
            }

            Tuple<float, float> values = table.GetFeatureValue(feature, level);
            return Localization.instance.Localize(description,
                values.Item1.ToString(), values.Item2.ToString());
        }

        public static int GetFeatureMaxLevel(EnchantingFeature feature)
        {
            return Config.MaximumFeatureLevels.TryGetValue(feature, out int maxLevel) ? maxLevel : 1;
        }

        public static List<InventoryItemListElement> GetUpgradeCost(EnchantingFeature feature, int level)
        {
            List<InventoryItemListElement> result = new List<InventoryItemListElement>();

            List<List<ItemAmount>> upgradeCosts = feature switch
            {
                EnchantingFeature.Sacrifice => Config.UpgradeCosts.Sacrifice,
                EnchantingFeature.ConvertMaterials => Config.UpgradeCosts.ConvertMaterials,
                EnchantingFeature.Enchant => Config.UpgradeCosts.Enchant,
                EnchantingFeature.Augment => Config.UpgradeCosts.Augment,
                EnchantingFeature.Disenchant => Config.UpgradeCosts.Disenchant,
                EnchantingFeature.Rune => Config.UpgradeCosts.Rune,
                _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null)
            };

            if (upgradeCosts == null)
            {
                return result;
            }

            if (level < 0 || level >= upgradeCosts.Count)
            {
                Debug.LogWarning($"[EpicLoot] Warning: tried to get enchanting feature upgrade cost for " +
                    $"level that does not exist ({feature}, {level})");
                return result;
            }

            List<ItemAmount> costList = upgradeCosts[level];
            if (costList == null)
            {
                return result;
            }

            foreach (ItemAmount itemAmountConfig in costList)
            {
                GameObject prefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item);
                if (prefab == null)
                {
                    Debug.LogWarning($"[EpicLoot] Tried to add unknown item ({itemAmountConfig.Item}) " +
                        $"to upgrade cost for feature ({feature}, {level})");
                    continue;
                }

                ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    Debug.LogWarning($"[EpicLoot] Tried to add item without ItemDrop ({itemAmountConfig.Item}) " +
                        $"to upgrade cost for feature ({feature}, {level})");
                    continue;
                }

                ItemDrop.ItemData costItem = itemDrop.m_itemData.Clone();
                costItem.m_dropPrefab = prefab;
                costItem.m_stack = itemAmountConfig.Amount;
                result.Add(new InventoryItemListElement() { Item = costItem });
            }

            return result;
        }
    }
}
