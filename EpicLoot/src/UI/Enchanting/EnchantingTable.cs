using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.CraftingV2;
using UnityEngine;

namespace EpicLoot_UnityLib
{
    public class EnchantingTable : MonoBehaviour, Hoverable, Interactable
    {
        public const string PrefabName = "piece_enchantingtable";
        public const float UseDistance = 2.7f;
        public const string DisplayNameLocID = "mod_epicloot_assets_enchantingtable";
        public const int FeatureUnavailableSentinel = -2;
        public const int FeatureLockedSentinel = -1;
        public const int FeatureLevelOne = 1;

        public GameObject EnchantingUIPrefab;

        public EnchantingTableStash Stash { get; private set; }

        public event Action<EnchantingFeature, int> OnFeatureLevelChanged;
        public event Action OnAnyFeatureLevelChanged;
        

        // An upgrade the table's owner has not answered yet, with the materials already paid. Nothing used
        // to end the wait, so an owner leaving before replying kept the payment for good; after
        // UpgradeResponseTimeout the request is settled from what the table shows (ExpireUpgradeRequests).
        private class PendingUpgrade
        {
            public EnchantingFeatureUpgradeRequest Request;
            public Player Requester;
            public float SentTime;
        }

        // Far beyond a normal round trip, so an answer that is merely slow is not refunded as well as granted.
        private const float UpgradeResponseTimeout = 20f;

        private static readonly List<PendingUpgrade> _upgradeRequests = new();
        private static readonly EnchantingFeature[] Features =
            (EnchantingFeature[])Enum.GetValues(typeof(EnchantingFeature));

        private ZNetView _nview;
        private Player _interactingPlayer;
        private readonly int[] _knownLevels = new int[Features.Length];
        private uint _knownDataRevision;

        public bool Interact(Humanoid user, bool repeat, bool alt)
        {
            if (repeat || user != Player.m_localPlayer || !InUseDistance(user))
            {
                return false;
            }

            EnchantingTableUI.Show(this);
            _interactingPlayer = Player.m_localPlayer;
            return false;
        }

        public void Awake()
        {
        }

        public void Start()
        {
            _nview = GetComponent<ZNetView>();

            if (_nview == null || !_nview.IsValid())
            {
                return;
            }

            _nview.Register<ZDOID, int, int>("el.TableUpgradeRequest", RPC_TableUpgradeRequest);
            _nview.Register<ZDOID, int, int, bool>("el.TableUpgradeResponse", RPC_TableUpgradeResponse);
            Stash = new EnchantingTableStash(this, _nview);

            InitFeatureLevels();
            SyncFeatureLevels(raiseEvents: false);

            Refresh();
        }

        public void RequestTableUpgrade(EnchantingFeature feature, int toLevel, Action<bool> responseCallback)
        {
            ZDOID tableZDO = _nview.GetZDO().m_uid;
            _upgradeRequests.Add(new PendingUpgrade()
            {
                Request = new EnchantingFeatureUpgradeRequest()
                {
                    TableZDO = tableZDO,
                    Feature = feature,
                    ToLevel = toLevel,
                    ResponseCallback = responseCallback
                },
                Requester = Player.m_localPlayer,
                SentTime = Time.time
            });

            _nview.InvokeRPC("el.TableUpgradeRequest",tableZDO, (int)feature, toLevel);
        }

        private void RPC_TableUpgradeRequest(long sender, ZDOID tableZDO, int featureI, int toLevel)
        {
            if (!_nview.IsOwner())
            {
                return;
            }

            GameObject instance = ZNetScene.instance.FindInstance(tableZDO);
            if (instance == null)
            {
                return;
            }

            EnchantingTable table = instance.GetComponent<EnchantingTable>();
            if (table == null)
            {
                return;
            }

            EnchantingFeature feature = (EnchantingFeature)featureI;
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

        private void RPC_TableUpgradeResponse(long sender, ZDOID tableZDO, int featureI, int toLevel, bool success)
        {
            //Only sent to Sender of Request.
            //Performs checks,
            //Calls OnTableUpgradeResponse
            EnchantingFeature feature = (EnchantingFeature)featureI;
            List<PendingUpgrade> listCopy = _upgradeRequests.ToList();
            foreach (PendingUpgrade pending in listCopy)
            {
                EnchantingFeatureUpgradeRequest request = pending.Request;
                if (request.TableZDO == tableZDO && request.Feature == feature && request.ToLevel == toLevel)
                {
                    _upgradeRequests.Remove(pending);
                    SettleUpgradeRequest(request, success);
                }
            }
        }

        private static void SettleUpgradeRequest(EnchantingFeatureUpgradeRequest request, bool success)
        {
            request.ResponseCallback?.Invoke(success);
            if (Player.m_localPlayer == null)
            {
                return;
            }

            EnchantingFeature feature = request.Feature;
            if (!success)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$mod_epicloot_upgradefailedmessage",
                    EnchantingTableUpgrades.GetFeatureName(feature)));
            }
            else if (request.ToLevel == 0)
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$mod_epicloot_unlockmessage",
                    EnchantingTableUpgrades.GetFeatureName(feature)));
            }
            else
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize("$mod_epicloot_upgrademessage",
                    EnchantingTableUpgrades.GetFeatureName(feature), request.ToLevel.ToString()));
            }
        }

        // Settles the requests left unanswered past the timeout: as done when the table already shows the
        // level (only the answer was lost), otherwise as failed, which refunds the payment. An answer that
        // turns up later finds nothing to settle and is ignored.
        private static void ExpireUpgradeRequests()
        {
            if (_upgradeRequests.Count == 0)
            {
                return;
            }

            float now = Time.time;
            for (int i = _upgradeRequests.Count - 1; i >= 0; i--)
            {
                PendingUpgrade pending = _upgradeRequests[i];
                if (now - pending.SentTime < UpgradeResponseTimeout)
                {
                    continue;
                }

                _upgradeRequests.RemoveAt(i);

                // The payment came out of this character's inventory; after a logout or world change there is
                // nobody to hand it back to.
                if (pending.Requester == null || pending.Requester != Player.m_localPlayer)
                {
                    continue;
                }

                EnchantingFeatureUpgradeRequest request = pending.Request;
                SettleUpgradeRequest(request, ReadFeatureLevel(request.TableZDO, request.Feature) >= request.ToLevel);
            }
        }

        // The level the table's ZDO holds, whether or not the table is loaded here.
        private static int ReadFeatureLevel(ZDOID tableZDO, EnchantingFeature feature)
        {
            ZDO zdo = ZDOMan.instance?.GetZDO(tableZDO);
            if (zdo == null)
            {
                return FeatureUnavailableSentinel;
            }

            int level = zdo.GetInt(FormatFeatureName(feature.ToString()), FeatureUnavailableSentinel);
            return level == FeatureUnavailableSentinel ? FeatureUnavailableSentinel : level - 1;
        }

        public void Update()
        {
            ExpireUpgradeRequests();

            if (_nview != null && _nview.IsValid() && _nview.GetZDO().DataRevision != _knownDataRevision)
            {
                SyncFeatureLevels(raiseEvents: true);
            }

            Stash?.Update();

            if (_interactingPlayer != null && EnchantingTableUI.instance != null &&
                EnchantingTableUI.instance.isActiveAndEnabled && !InUseDistance(_interactingPlayer))
            {
                EnchantingTableUI.Hide();
                _interactingPlayer = null;
            }
        }

        // Only the owner's SetFeatureLevel raises the level events. Every other client sees an upgrade
        // arrive as a plain ZDO sync, so the levels are re-read whenever the ZDO's revision moves.
        private void SyncFeatureLevels(bool raiseEvents)
        {
            _knownDataRevision = _nview.GetZDO().DataRevision;

            bool anyChanged = false;
            foreach (EnchantingFeature feature in Features)
            {
                int level = GetFeatureLevel(feature);
                if (_knownLevels[(int)feature] == level)
                {
                    continue;
                }

                _knownLevels[(int)feature] = level;
                anyChanged = true;
                if (raiseEvents)
                {
                    OnFeatureLevelChanged?.Invoke(feature, level);
                }
            }

            if (anyChanged && raiseEvents)
            {
                OnAnyFeatureLevelChanged?.Invoke();
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
                ? Localization.instance.Localize("<color=#808080ff>$piece_toofar</color>")
                : Localization.instance.Localize($"${DisplayNameLocID}\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
        }

        public string GetHoverName()
        {
            return DisplayNameLocID;
        }

        public float GetHoverOffset()
        {
            // Hoverable gained this member in the Sept 2026 game update. All 52 vanilla
            // implementers return a serialized m_hoverOffset; zero keeps the enchanting
            // table's hover text exactly where it has always been.
            return 0f;
        }

        private static string FormatFeatureName(string featureName)
        {
            return string.Format($"el.et.v1.{featureName}");
        }

        private void InitFeatureLevels()
        {
            // Owner-only: a non-owner Set is not authoritative and bumps the data revision on a
            // ZDO someone else owns -- every client streaming the table in used to re-write the
            // disabled-feature sentinels on each load, letting clients disagree about levels.
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner())
            {
                return;
            }

            const int uninitializedSentinel = -888;
            foreach (EnchantingFeature feature in Enum.GetValues(typeof(EnchantingFeature)))
            {
                string featureName = feature.ToString();
                int featureValue = _nview.GetZDO().GetInt(FormatFeatureName(featureName), uninitializedSentinel);
                if (featureValue < 0)
                {
                    //For those that travel here from afar, you might be asking yourself why I'm adding and subtracting 1 to the level.
                    //It's because Iron Gate decided that 0 value ZDO's should be removed when world save occurs........
                    _nview.GetZDO().Set(FormatFeatureName(featureName), GetDefaultFeatureLevel(feature) + 1);
                }
            }
        }

        public static int GetDefaultFeatureLevel(EnchantingFeature feature)
        {
            if (!EnchantingUIController.UpgradesActive(feature, out bool featureActive))
            {
                return featureActive ? FeatureLevelOne : FeatureUnavailableSentinel;
            }

            if (!featureActive)
            {
                return FeatureUnavailableSentinel;
            }

            if (EnchantingTableUpgrades.Config.DefaultFeatureLevels.TryGetValue(feature, out int level))
            {
                return level;
            }

            // A server still on an enchantingupgrades.json from before Storage existed: locked, free to unlock.
            return feature == EnchantingFeature.Storage ? FeatureLockedSentinel : FeatureUnavailableSentinel;
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
            {
                return FeatureUnavailableSentinel;
            }

            if (!EnchantingUIController.UpgradesActive(feature, out bool featureActive))
            {
                return featureActive ? FeatureLevelOne : FeatureUnavailableSentinel;
            }

            if (!featureActive)
            {
                return FeatureUnavailableSentinel;
            }

            string featureName = feature.ToString();
            int level = _nview.GetZDO().GetInt(FormatFeatureName(featureName), FeatureUnavailableSentinel);
            //For those that travel here from afar, you might be asking yourself why I'm adding and subtracting 1 to the level.
            //It's because Iron Gate decided that 0 value ZDO's should be removed when world save occurs........
            return level == FeatureUnavailableSentinel ? FeatureUnavailableSentinel : level - 1;
        }

        public void SetFeatureLevel(EnchantingFeature feature, int level)
        {
            if (_nview == null)
            {
                return;
            }

            if (!EnchantingUIController.UpgradesActive(feature, out bool featureActive))
            {
                level = featureActive ? FeatureLevelOne : FeatureUnavailableSentinel;
            }
            else
            {
                if (level > (EnchantingTableUpgrades.Config.MaximumFeatureLevels.TryGetValue(
                    feature, out int maxLevel) ? maxLevel : 1))
                {
                    return;
                }
            }

            string featureName = feature.ToString();
            _nview.GetZDO().Set(FormatFeatureName(featureName), level+1);
            _knownLevels[(int)feature] = level;
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
            {
                Debug.LogWarning($"[EpicLoot] Warning: " +
                    $"tried to get unlock cost for a feature that is already unlocked! ({feature})");
            }

            return EnchantingTableUpgrades.GetUpgradeCost(feature, 0);
        }

        public List<InventoryItemListElement> GetFeatureUpgradeCost(EnchantingFeature feature)
        {
            if (IsFeatureLocked(feature) || !IsFeatureAvailable(feature))
            {
                Debug.LogWarning($"[EpicLoot] Warning: " +
                    $"tried to get enchanting feature unlock cost for a feature that is locked or unavailable! ({feature})");
            }

            int currentLevel = GetFeatureLevel(feature);
            return EnchantingTableUpgrades.GetUpgradeCost(feature, currentLevel + 1);
        }

        public Tuple<float, float> GetFeatureValue(EnchantingFeature feature, int level)
        {
            if (!IsFeatureAvailable(feature))
            {
                return new Tuple<float, float>(float.NaN, float.NaN);
            }

            if (level < 0 || level > EnchantingTableUpgrades.GetFeatureMaxLevel(feature))
            {
                return new Tuple<float, float>(float.NaN, float.NaN);
            }

            List<float[]> values = feature switch
            {
                EnchantingFeature.Sacrifice => EnchantingTableUpgrades.Config.UpgradeValues.Sacrifice,
                EnchantingFeature.ConvertMaterials => EnchantingTableUpgrades.Config.UpgradeValues.ConvertMaterials,
                EnchantingFeature.Enchant => EnchantingTableUpgrades.Config.UpgradeValues.Enchant,
                EnchantingFeature.Augment => EnchantingTableUpgrades.Config.UpgradeValues.Augment,
                EnchantingFeature.Disenchant => EnchantingTableUpgrades.Config.UpgradeValues.Disenchant,
                EnchantingFeature.Rune => EnchantingTableUpgrades.Config.UpgradeValues.Rune,
                EnchantingFeature.Storage => EnchantingTableUpgrades.Config.UpgradeValues.Storage,
                _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null)
            };

            if (values == null || level >= values.Count)
            {
                return new Tuple<float, float>(float.NaN, float.NaN);
            }

            float[] levelValues = values[level];
            if (levelValues.Length == 1)
            {
                return new Tuple<float, float>(levelValues[0], float.NaN);
            }

            if (levelValues.Length >= 2)
            {
                return new Tuple<float, float>(levelValues[0], levelValues[1]);
            }

            return new Tuple<float, float>(float.NaN, float.NaN);
        }

        public Tuple<float, float> GetFeatureCurrentValue(EnchantingFeature feature)
        {
            return GetFeatureValue(feature, GetFeatureLevel(feature));
        }
    }
}