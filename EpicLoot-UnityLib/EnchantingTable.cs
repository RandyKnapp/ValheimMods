using System;
using System.Collections.Generic;
using System.Linq;
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

        private static readonly List<EnchantingFeatureUpgradeRequest> _upgradeRequests = new();
        private ZNetView _nview;
        private Player _interactingPlayer;

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
            _nview = GetComponent<ZNetView>();
            
            if (_nview == null || _nview.GetZDO() == null)
                return;
            
            var wearTear = GetComponent<WearNTear>();
            if (wearTear != null)
            {
                wearTear.m_destroyedEffect.m_effectPrefabs = new EffectList.EffectData[]
                {
                    new() { m_prefab = ZNetScene.instance.GetPrefab("vfx_SawDust") },
                    new() { m_prefab = ZNetScene.instance.GetPrefab("sfx_wood_destroyed") }
                };
                wearTear.m_hitEffect.m_effectPrefabs = new EffectList.EffectData[1]
                {
                    new() { m_prefab = ZNetScene.instance.GetPrefab("vfx_SawDust") }
                };
            }
            
            _nview.Register<ZDOID, int, int>("el.TableUpgradeRequest", RPC_TableUpgradeRequest);
            _nview.Register<ZDOID, int, int, bool>("el.TableUpgradeResponse", RPC_TableUpgradeResponse);
            
            InitFeatureLevels();
        }

        //Function RequestTableUpgrade
        public void RequestTableUpgrade(EnchantingFeature feature, int toLevel, Action<bool> responseCallback)
        {
            var tableZDO = _nview.GetZDO().m_uid;
            _upgradeRequests.Add(new EnchantingFeatureUpgradeRequest()
            {
                TableZDO = tableZDO,
                Feature = feature,
                ToLevel = toLevel,
                ResponseCallback = responseCallback
            });
            _nview.InvokeRPC("el.TableUpgradeRequest",tableZDO, (int)feature, toLevel);

        }
           
        //Function for RPC_TableUpgradeRequest
        private void RPC_TableUpgradeRequest(long sender, ZDOID tableZDO, int featureI, int toLevel)
        {
            if (!_nview.IsOwner())
                return;
            
            var instance = ZNetScene.instance.FindInstance(tableZDO);
            if (instance == null)
                return;

            var table = instance.GetComponent<EnchantingTable>();
            if (table == null)
                return;

            var feature = (EnchantingFeature)featureI;
            if (table.IsFeatureAvailable(feature) && toLevel == table.GetFeatureLevel(feature) + 1)
            {
                table.SetFeatureLevel(feature, toLevel);
                _nview.InvokeRPC(sender,"el.TableUpgradeResponse",tableZDO, featureI, toLevel, true);
                OnFeatureLevelChanged?.Invoke(feature, toLevel);
                OnAnyFeatureLevelChanged?.Invoke();
            }
            else
            {
                _nview.InvokeRPC(sender,"el.TableUpgradeResponse",tableZDO, featureI, toLevel, false);
            }
        }
        
        //FUnction for RPC_TableUpgradeResponse
        private void RPC_TableUpgradeResponse(long sender, ZDOID tableZDO, int featureI, int toLevel, bool success)
        {
            //Only sent to Sender of Request.
            //Performs checks,
            //Calls OnTableUpgradeResponse
            var feature = (EnchantingFeature)featureI;
            var listCopy = _upgradeRequests.ToList();
            foreach (var request in listCopy)
            {
                if (request.TableZDO == tableZDO && request.Feature == feature && request.ToLevel == toLevel)
                {
                    request.ResponseCallback.Invoke(success);
                    _upgradeRequests.Remove(request);
                    if (Player.m_localPlayer != null)
                    {
                        if (toLevel == 0)
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$mod_epicloot_unlockmessage", EnchantingTableUpgrades.GetFeatureName(feature)));
                        else
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$mod_epicloot_upgrademessage", EnchantingTableUpgrades.GetFeatureName(feature), toLevel.ToString()));
                    }
                }
            }

        }

        public void Update()
        {
            if (_interactingPlayer != null && EnchantingTableUI.instance != null && EnchantingTableUI.instance.isActiveAndEnabled && !InUseDistance(_interactingPlayer))
            {
                EnchantingTableUI.Hide();
                _interactingPlayer = null;
            }
        }

        public void Refresh()
        {
            OnAnyFeatureLevelChanged?.Invoke();
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

        private string FormatFeatureName(string featureName)
        {
            return string.Format($"el.et.v1.{featureName}");
        }

        private void InitFeatureLevels()
        {
            const int uninitializedSentinel = -888;
            foreach (EnchantingFeature feature in Enum.GetValues(typeof(EnchantingFeature)))
            {
                var featureName = feature.ToString();
                if (_nview.GetZDO().GetInt(FormatFeatureName(featureName), uninitializedSentinel) == uninitializedSentinel)
                    _nview.GetZDO().Set(FormatFeatureName(featureName), GetDefaultFeatureLevel(feature)+1);
                //For those that travel here from afar, you might be asking yourself why I'm adding and subtracting 1 to the level.
                //It's because Iron Gate decided that 0 value ZDO's should be removed when world save occurs........
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
            if (_nview == null || _nview.GetZDO() == null)
                return FeatureUnavailableSentinel;

            
            if (!UpgradesActive(feature, out var featureActive))
            {
                return FeatureLevelOne;
            }
            
            if (!featureActive)
                return FeatureUnavailableSentinel;
            
            var featureName = feature.ToString();
            var level = _nview.GetZDO().GetInt(FormatFeatureName(featureName), FeatureUnavailableSentinel);
            //For those that travel here from afar, you might be asking yourself why I'm adding and subtracting 1 to the level.
            //It's because Iron Gate decided that 0 value ZDO's should be removed when world save occurs........
            var returncode = level == FeatureUnavailableSentinel ? FeatureUnavailableSentinel : level - 1;
            return level == FeatureUnavailableSentinel ? FeatureUnavailableSentinel : level - 1;
        }

        public void SetFeatureLevel(EnchantingFeature feature, int level)
        {
            if (_nview == null)
                return;

            if (!UpgradesActive(feature, out var featureActive))
            {
                level = FeatureLevelOne;
            }
            else
            {
                if (level > (EnchantingTableUpgrades.Config.MaximumFeatureLevels.TryGetValue(feature, out var maxLevel) ? maxLevel : 1))
                {
                    return;
                }
            }
            var featureName = feature.ToString();
            _nview.GetZDO().Set(FormatFeatureName(featureName), level+1);
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