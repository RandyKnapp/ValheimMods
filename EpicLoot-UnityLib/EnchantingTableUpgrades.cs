using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
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

    public class EnchantingTableUpgradeLedger
    {
        public const string LedgerIdentifier = "randyknapp.mods.epicloot.EnchantingUpgradeLedger";
        public const int FeatureUnavailableSentinel = -2;
        public const int FeatureLockedSentinel = -1;

        public event Action<EnchantingFeature, int> OnFeatureLevelChanged;
        public event Action OnAnyFeatureLevelChanged;

        private Dictionary<EnchantingFeature, int> _featureLevels = new();

        public void Load()
        {
            var globalKeys = ZoneSystem.instance.GetGlobalKeys();
            var ledgerGlobalKey = globalKeys.Find(x => x.StartsWith(LedgerIdentifier));
            var ledgerData = ledgerGlobalKey?.Substring(LedgerIdentifier.Length);

            if (string.IsNullOrEmpty(ledgerData))
            {
                Reset();
            }
            else
            {
                try
                {
                    _featureLevels = JsonConvert.DeserializeObject<Dictionary<EnchantingFeature, int>>(ledgerData);
                }
                catch (Exception)
                {
                    Debug.LogError("[EpicLoot] WARNING! Could not load enchanting ledger, enchanting upgrades will not work!");
                    Reset();
                }
            }
        }

        public void Reset()
        {
            _featureLevels.Clear();
            foreach (EnchantingFeature feature in Enum.GetValues(typeof(EnchantingFeature)))
            {
                var defaultLevel = EnchantingTableUpgrades.Config.DefaultFeatureLevels.TryGetValue(feature, out var level) ? level : FeatureUnavailableSentinel;
                _featureLevels[feature] = defaultLevel;
                OnFeatureLevelChanged?.Invoke(feature, defaultLevel);
            }

            OnAnyFeatureLevelChanged?.Invoke();
        }

        public void Save()
        {
            if (ZoneSystem.instance == null)
                return;

            ZoneSystem.instance.m_globalKeys.RemoveWhere(x => x.StartsWith(LedgerIdentifier));

            var ledgerData = JsonConvert.SerializeObject(_featureLevels, Formatting.None);
            ledgerData = LedgerIdentifier + ledgerData;
            ZoneSystem.instance.SetGlobalKey(ledgerData);
        }

        public void RefreshFromGlobalKeys()
        {
            var previousValues = new Dictionary<EnchantingFeature, int>(_featureLevels);
            Load();
            var anyFeatureChanged = false;
            foreach (var entry in _featureLevels)
            {
                if (entry.Value != (previousValues.TryGetValue(entry.Key, out var previousLevel) ? previousLevel : FeatureUnavailableSentinel))
                {
                    OnFeatureLevelChanged?.Invoke(entry.Key, entry.Value);
                    anyFeatureChanged = true;
                }
            }

            if (anyFeatureChanged)
                OnAnyFeatureLevelChanged?.Invoke();
        }

        public int GetLevel(EnchantingFeature feature)
        {
            return _featureLevels.TryGetValue(feature, out var level) ? level : -1;
        }

        public void SetLevel(EnchantingFeature feature, int level)
        {
            if (level > (EnchantingTableUpgrades.Config.MaximumFeatureLevels.TryGetValue(feature, out var maxLevel) ? maxLevel : 1))
            {
                Debug.LogWarning($"[EpicLoot] Tried to set enchanting feature ({feature}) higher than max level!");
                return;
            }

            _featureLevels[feature] = level;
        }
    }

    public class EnchantingFeatureUpgradeRequest
    {
        public EnchantingFeature Feature;
        public int ToLevel;
        public Action<bool> ResponseCallback;
    }

    public static class EnchantingTableUpgrades
    {
        public static readonly EnchantingTableUpgradeLedger Ledger = new();
        public static EnchantingUpgradesConfig Config;

        public static event Action<EnchantingFeature, int> OnFeatureLevelChanged;
        public static event Action OnAnyFeatureLevelChanged;

        private static bool _isServer;
        private static readonly List<EnchantingFeatureUpgradeRequest> _upgradeRequests = new();

        public static void InitializeConfig(EnchantingUpgradesConfig config)
        {
            Ledger.OnFeatureLevelChanged += (feature, i) => OnFeatureLevelChanged?.Invoke(feature, i);
            Ledger.OnAnyFeatureLevelChanged += () => OnAnyFeatureLevelChanged?.Invoke();
            Config = config;
        }

        public static void RegisterRPC(ZRoutedRpc routedRpc, bool isServer)
        {
            _isServer = isServer;
            if (_isServer)
                routedRpc.Register<int, int>("RequestEnchantingUpgrade", RPC_RequestEnchantingUpgrade);

            routedRpc.Register<int, int, bool>("EnchantingUpgradeResponse", RPC_EnchantingUpgradeResponse);
        }

        public static void RPC_RequestEnchantingUpgrade(long sender, int featureI, int toLevel)
        {
            if (!_isServer)
                return;
            
            var feature = (EnchantingFeature)featureI;
            if (IsFeatureAvailable(feature) && toLevel == GetFeatureLevel(feature) + 1)
            {
                Ledger.SetLevel(feature, toLevel);
                Ledger.Save();
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, "EnchantingUpgradeResponse", featureI, toLevel, true);
                OnFeatureLevelChanged?.Invoke(feature, toLevel);
                OnAnyFeatureLevelChanged?.Invoke();
            }
            else
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, "EnchantingUpgradeResponse", featureI, toLevel, false);
            }
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

        public static string GetFeatureUpgradeLevelDescription(EnchantingFeature feature, int level)
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

            var values = EnchantingTableUpgrades.GetFeatureValue(feature, level);
            return Localization.instance.Localize(featureUpgradeDescriptions[(int)feature], values.Item1.ToString("0.#"), values.Item2.ToString("0.#"));
        }

        private static void RPC_EnchantingUpgradeResponse(long sender, int featureI, int toLevel, bool success)
        {
            var feature = (EnchantingFeature)featureI;
            var listCopy = _upgradeRequests.ToList();
            foreach (var request in listCopy)
            {
                if (request.Feature == feature && request.ToLevel == toLevel)
                {
                    request.ResponseCallback.Invoke(success);
                    _upgradeRequests.Remove(request);

                    if (Player.m_localPlayer != null)
                    {
                        if (toLevel == 0)
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$mod_epicloot_unlockmessage", GetFeatureName(feature)));
                        else
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$mod_epicloot_upgrademessage", GetFeatureName(feature), toLevel.ToString()));
                    }
                }
            }
        }

        public static void RequestEnchantingUpgrade(EnchantingFeature feature, int toLevel, Action<bool> responseCallback)
        {
            _upgradeRequests.Add(new EnchantingFeatureUpgradeRequest()
            {
                Feature = feature,
                ToLevel = toLevel,
                ResponseCallback = responseCallback
            });
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RequestEnchantingUpgrade", (int)feature, toLevel);
        }

        public static void OnZNetStart()
        {
            Ledger.RefreshFromGlobalKeys();
        }

        public static void OnZNetDestroyed()
        {
            Ledger.Reset();
        }

        public static void OnWorldSave()
        {
            if (_isServer)
                Ledger.Save();
        }

        public static void NewGlobalKeys()
        {
            Ledger.RefreshFromGlobalKeys();
        }

        public static bool IsFeatureAvailable(EnchantingFeature feature)
        {
            return Ledger.GetLevel(feature) > EnchantingTableUpgradeLedger.FeatureUnavailableSentinel;
        }

        public static bool IsFeatureLocked(EnchantingFeature feature)
        {
            return Ledger.GetLevel(feature) == EnchantingTableUpgradeLedger.FeatureLockedSentinel;
        }

        public static bool IsFeatureUnlocked(EnchantingFeature feature)
        {
            return Ledger.GetLevel(feature) > EnchantingTableUpgradeLedger.FeatureLockedSentinel;
        }

        public static int GetFeatureLevel(EnchantingFeature feature)
        {
            return Ledger.GetLevel(feature);
        }

        public static int GetFeatureMaxLevel(EnchantingFeature feature)
        {
            return Config.MaximumFeatureLevels.TryGetValue(feature, out var maxLevel) ? maxLevel : 1;
        }

        public static bool IsFeatureMaxLevel(EnchantingFeature feature)
        {
            return GetFeatureLevel(feature) == GetFeatureMaxLevel(feature);
        }

        public static List<InventoryItemListElement> GetUnlockCost(EnchantingFeature feature)
        {
            if (IsFeatureUnlocked(feature))
                Debug.LogWarning($"[EpicLoot] Warning: tried to get unlock cost for a feature that is already unlocked! ({feature})");

            return GetUpgradeCostInternal(feature, 0);
        }

        public static List<InventoryItemListElement> GetUpgradeCost(EnchantingFeature feature)
        {
            if (IsFeatureLocked(feature) || !IsFeatureAvailable(feature))
                Debug.LogWarning($"[EpicLoot] Warning: tried to get enchanting feature unlock cost for a feature that is locked or unavailable! ({feature})");

            var currentLevel = GetFeatureLevel(feature);
            return GetUpgradeCostInternal(feature, currentLevel + 1);
        }

        private static List<InventoryItemListElement> GetUpgradeCostInternal(EnchantingFeature feature, int level)
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
                costItem.m_stack = itemAmountConfig.Amount;
                result.Add(new InventoryItemListElement() { Item = costItem });
            }

            return result;
        }

        public static Tuple<float, float> GetFeatureValue(EnchantingFeature feature, int level)
        {
            if (!IsFeatureAvailable(feature))
                return new Tuple<float, float>(float.NaN, float.NaN);

            if (level < 0 || level > GetFeatureMaxLevel(feature))
                return new Tuple<float, float>(float.NaN, float.NaN);

            var values = feature switch
            {
                EnchantingFeature.Sacrifice => Config.UpgradeValues.Sacrifice,
                EnchantingFeature.ConvertMaterials => Config.UpgradeValues.ConvertMaterials,
                EnchantingFeature.Enchant => Config.UpgradeValues.Enchant,
                EnchantingFeature.Augment => Config.UpgradeValues.Augment,
                EnchantingFeature.Disenchant => Config.UpgradeValues.Disenchant,
                EnchantingFeature.Helheim => Config.UpgradeValues.Helheim,
                _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null)
            };

            if (level >= values.Count)
                return new Tuple<float, float>(float.NaN, float.NaN);

            var levelValues = values[level];
            if (levelValues.Length == 1)
                return new Tuple<float, float>(levelValues[0], float.NaN);
            if (levelValues.Length >= 2)
                return new Tuple<float, float>(levelValues[0], levelValues[1]);
            return new Tuple<float, float>(float.NaN, float.NaN);
        }
    }
}
