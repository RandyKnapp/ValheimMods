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
        Helheim
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
        public List<List<ItemAmount>> Helheim;
    }

    [Serializable]
    public class EnchantingFeatureValues
    {
        public List<float[]> Sacrifice;
        public List<float[]> ConvertMaterials;
        public List<float[]> Enchant;
        public List<float[]> Augment;
        public List<float[]> Disenchant;
        public List<float[]> Helheim;
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

        public static string GetFeatureName(EnchantingFeature feature)
        {
            var featureNames = new []
            {
                "$mod_epicloot_sacrifice",
                "$mod_epicloot_convertmaterials",
                "$mod_epicloot_enchant",
                "$mod_epicloot_augment",
                "$mod_epicloot_disenchant",
                "$mod_epicloot_helheim",
            };
            return featureNames[(int)feature];
        }

        public static string GetFeatureDescription(EnchantingFeature feature)
        {
            var featureDescriptions = new []
            {
                "$mod_epicloot_featureinfo_sacrifice",
                "$mod_epicloot_featureinfo_convertmaterials",
                "$mod_epicloot_featureinfo_enchant",
                "$mod_epicloot_featureinfo_augment",
                "$mod_epicloot_featureinfo_disenchant",
                "$mod_epicloot_featureinfo_helheim",
            };
            return featureDescriptions[(int)feature];
        }

        public static string GetFeatureUpgradeLevelDescription(EnchantingTable table, EnchantingFeature feature, int level)
        {
            var featureUpgradeDescriptions = new []
            {
                "$mod_epicloot_featureupgrade_sacrifice",
                "$mod_epicloot_featureupgrade_convertmaterials",
                "$mod_epicloot_featureupgrade_enchant",
                "$mod_epicloot_featureupgrade_augment",
                "$mod_epicloot_featureupgrade_disenchant",
                "$mod_epicloot_featureupgrade_helheim",
            };

            var values = table.GetFeatureValue(feature, level);
            return Localization.instance.Localize(featureUpgradeDescriptions[(int)feature], values.Item1.ToString("0.#"), values.Item2.ToString("0.#"));
        }

        public static int GetFeatureMaxLevel(EnchantingFeature feature)
        {
            return Config.MaximumFeatureLevels.TryGetValue(feature, out var maxLevel) ? maxLevel : 1;
        }

        public static List<InventoryItemListElement> GetUpgradeCost(EnchantingFeature feature, int level)
        {
            var result = new List<InventoryItemListElement>();

            var upgradeCosts = feature switch
            {
                EnchantingFeature.Sacrifice => Config.UpgradeCosts.Sacrifice,
                EnchantingFeature.ConvertMaterials => Config.UpgradeCosts.ConvertMaterials,
                EnchantingFeature.Enchant => Config.UpgradeCosts.Enchant,
                EnchantingFeature.Augment => Config.UpgradeCosts.Augment,
                EnchantingFeature.Disenchant => Config.UpgradeCosts.Disenchant,
                EnchantingFeature.Helheim => Config.UpgradeCosts.Helheim,
                _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null)
            };

            if (upgradeCosts == null)
                return result;

            if (level < 0 || level >= upgradeCosts.Count)
            {
                Debug.LogWarning($"[EpicLoot] Warning: tried to get enchanting feature upgrade cost for level that does not exist ({feature}, {level})");
                return result;
            }

            var costList = upgradeCosts[level];
            if (costList == null)
                return result;

            foreach (var itemAmountConfig in costList)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item);
                if (prefab == null)
                {
                    Debug.LogWarning($"[EpicLoot] Tried to add unknown item ({itemAmountConfig.Item}) to upgrade cost for feature ({feature}, {level})");
                    continue;
                }

                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    Debug.LogWarning($"[EpicLoot] Tried to add item without ItemDrop ({itemAmountConfig.Item}) to upgrade cost for feature ({feature}, {level})");
                    continue;
                }

                var costItem = itemDrop.m_itemData.Clone();
                costItem.m_dropPrefab = prefab;
                costItem.m_stack = itemAmountConfig.Amount;
                result.Add(new InventoryItemListElement() { Item = costItem });
            }

            return result;
        }
    }
}
