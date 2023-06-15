using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot_UnityLib
{
    public class EnchantingTable : MonoBehaviour, Hoverable, Interactable
    {
        public const float UseDistance = 2.7f;
        public const string DisplayNameLocID = "mod_epicloot_assets_enchantingtable";
        public const int FeatureUnavailableSentinel = -2;
        public const int FeatureLockedSentinel = -1;
        public const int FeatureLevelOne = 1;

        public GameObject EnchantingUIPrefab;

        public event Action<EnchantingFeature, int> OnFeatureLevelChanged;
        public event Action OnAnyFeatureLevelChanged;
        
        public delegate bool UpgradesActiveDelegate(EnchantingFeature feature, out bool featureActive);
        public static UpgradesActiveDelegate UpgradesActive;


        private Player _interactingPlayer;
        private ZDO _zdo;

        public bool Interact(Humanoid user, bool repeat, bool alt)
        {
            if (repeat || user != Player.m_localPlayer || !InUseDistance(user))
                return false;

            EnchantingTableUI.Show(EnchantingUIPrefab, this);
            _interactingPlayer = Player.m_localPlayer;
            return false;
        }

        public void Awake()
        {
            var wearTear = GetComponent<WearNTear>();
            if (wearTear != null)
            {
                wearTear.m_destroyedEffect.m_effectPrefabs = new EffectList.EffectData[]
                {
                    new EffectList.EffectData() { m_prefab = ZNetScene.instance.GetPrefab("vfx_SawDust") },
                    new EffectList.EffectData() { m_prefab = ZNetScene.instance.GetPrefab("sfx_wood_destroyed") }
                };
                wearTear.m_hitEffect.m_effectPrefabs = new EffectList.EffectData[1]
                {
                    new EffectList.EffectData() { m_prefab = ZNetScene.instance.GetPrefab("vfx_SawDust") }
                };
            }

            InitFeatureLevels();
        }

        public void Update()
        {
            if (_interactingPlayer != null && EnchantingTableUI.instance != null && EnchantingTableUI.instance.isActiveAndEnabled && !InUseDistance(_interactingPlayer))
            {
                EnchantingTableUI.Hide();
                _interactingPlayer = null;
            }
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public bool InUseDistance(Humanoid human)
        {
            return Vector3.Distance(human.transform.position, transform.position) < UseDistance;
        }

        public string GetHoverText()
        {
            return !InUseDistance(Player.m_localPlayer)
                ? Localization.instance.Localize("<color=grey>$piece_toofar</color>")
                : Localization.instance.Localize($"${DisplayNameLocID}\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
        }

        public string GetHoverName()
        {
            return DisplayNameLocID;
        }

        private void InitFeatureLevels()
        {
            var nview = GetComponent<ZNetView>();
            if (nview == null)
                return;

            _zdo = nview.GetZDO();
            if (_zdo == null)
                return;

            const int uninitializedSentinel = -888;
            foreach (EnchantingFeature feature in Enum.GetValues(typeof(EnchantingFeature)))
            {
                var featureName = feature.ToString();
                if (_zdo.GetInt(featureName, uninitializedSentinel) == uninitializedSentinel)
                    _zdo.Set(featureName, GetDefaultFeatureLevel(feature));
            }
        }

        private static int GetDefaultFeatureLevel(EnchantingFeature feature)
        {
            if (!UpgradesActive(feature, out var featureActive))
                return FeatureLevelOne;
            
            if (!featureActive)
                return FeatureUnavailableSentinel;
            
            return EnchantingTableUpgrades.Config.DefaultFeatureLevels.TryGetValue(feature, out var level) ? level : FeatureUnavailableSentinel;
        }

        public void Reset()
        {
            foreach (EnchantingFeature feature in Enum.GetValues(typeof(EnchantingFeature)))
            {
                SetFeatureLevel(feature, GetDefaultFeatureLevel(feature));
            }
        }

        public int GetFeatureLevel(EnchantingFeature feature)
        {
            if (_zdo == null)
                return FeatureUnavailableSentinel;

            if (!UpgradesActive(feature, out var featureActive))
                return FeatureLevelOne;
            
            if (!featureActive)
                return FeatureUnavailableSentinel;
            
            var featureName = feature.ToString();
            return _zdo.GetInt(featureName, FeatureUnavailableSentinel);
        }

        public void SetFeatureLevel(EnchantingFeature feature, int level)
        {
            if (_zdo == null)
                return;

            if (!UpgradesActive(feature, out var featureActive))
            {
                level = FeatureLevelOne;
            }
            else
            {
                if (level > (EnchantingTableUpgrades.Config.MaximumFeatureLevels.TryGetValue(feature, out var maxLevel) ? maxLevel : 1))
                {
                    Debug.LogWarning($"[EpicLoot] Tried to set enchanting feature ({feature}) higher than max level!");
                    return;
                }
            }

            var featureName = feature.ToString();
            _zdo.Set(featureName, level);
            OnFeatureLevelChanged?.Invoke(feature, level);
            OnAnyFeatureLevelChanged?.Invoke();
        }

        public bool IsFeatureAvailable(EnchantingFeature feature)
        {
            return GetFeatureLevel(feature) > FeatureUnavailableSentinel;
        }

        public bool IsFeatureLocked(EnchantingFeature feature)
        {
            return GetFeatureLevel(feature) == FeatureLockedSentinel;
        }

        public bool IsFeatureUnlocked(EnchantingFeature feature)
        {
            return GetFeatureLevel(feature) > FeatureLockedSentinel;
        }

        public bool IsFeatureMaxLevel(EnchantingFeature feature)
        {
            return GetFeatureLevel(feature) == EnchantingTableUpgrades.GetFeatureMaxLevel(feature);
        }

        public List<InventoryItemListElement> GetFeatureUnlockCost(EnchantingFeature feature)
        {
            if (IsFeatureUnlocked(feature))
                Debug.LogWarning($"[EpicLoot] Warning: tried to get unlock cost for a feature that is already unlocked! ({feature})");

            return EnchantingTableUpgrades.GetUpgradeCost(feature, 0);
        }

        public List<InventoryItemListElement> GetFeatureUpgradeCost(EnchantingFeature feature)
        {
            if (IsFeatureLocked(feature) || !IsFeatureAvailable(feature))
                Debug.LogWarning($"[EpicLoot] Warning: tried to get enchanting feature unlock cost for a feature that is locked or unavailable! ({feature})");

            var currentLevel = GetFeatureLevel(feature);
            return EnchantingTableUpgrades.GetUpgradeCost(feature, currentLevel + 1);
        }

        public Tuple<float, float> GetFeatureValue(EnchantingFeature feature, int level)
        {
            if (!IsFeatureAvailable(feature))
                return new Tuple<float, float>(float.NaN, float.NaN);

            if (level < 0 || level > EnchantingTableUpgrades.GetFeatureMaxLevel(feature))
                return new Tuple<float, float>(float.NaN, float.NaN);

            var values = feature switch
            {
                EnchantingFeature.Sacrifice => EnchantingTableUpgrades.Config.UpgradeValues.Sacrifice,
                EnchantingFeature.ConvertMaterials => EnchantingTableUpgrades.Config.UpgradeValues.ConvertMaterials,
                EnchantingFeature.Enchant => EnchantingTableUpgrades.Config.UpgradeValues.Enchant,
                EnchantingFeature.Augment => EnchantingTableUpgrades.Config.UpgradeValues.Augment,
                EnchantingFeature.Disenchant => EnchantingTableUpgrades.Config.UpgradeValues.Disenchant,
                EnchantingFeature.Helheim => EnchantingTableUpgrades.Config.UpgradeValues.Helheim,
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

        public Tuple<float, float> GetFeatureCurrentValue(EnchantingFeature feature)
        {
            return GetFeatureValue(feature, GetFeatureLevel(feature));
        }
    }
}
