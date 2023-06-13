using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [RequireComponent(typeof(Character))]
    public class BountyTarget : MonoBehaviour
    {
        public const string BountyIDKey = "BountyID";
        public const string BountyDataKey = "BountyData";
        public const string MonsterIDKey = "MonsterID";
        public const string IsAddKey = "IsAdd";
        public const string BountyTargetNameKey = "BountyTargetName";

        private BountyInfo _bountyInfo;
        private string _monsterID;
        private bool _isAdd;

        private Character _character;
        private ZDO _zdo;

        [UsedImplicitly]
        public void Awake()
        {
            _character = GetComponent<Character>();
            _character.m_onDeath += OnDeath;
            _zdo = _character.m_nview.GetZDO();

            if (HasBeenSetup())
            {
                Reinitialize();
            }
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            if (_character != null)
            {
                _character.m_onDeath -= OnDeath;
            }
        }

        private bool HasBeenSetup()
        {
            var bountyID = _zdo.GetString(BountyIDKey);
            return !string.IsNullOrEmpty(bountyID);
        }

        private void OnDeath()
        {
            if (ZNet.instance.IsServer() || !ZNet.instance.IsServer() && !ZNet.instance.IsDedicated())
            {
                var pkg = new ZPackage();
                _bountyInfo.ToPackage(pkg);

                EpicLoot.LogWarning($"SENDING -> RPC_SlayBountyTarget: {_monsterID} ({(_isAdd ? "minion" : "target")})");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SlayBountyTarget", pkg, _monsterID, _isAdd);
            }
            else
            {
                var bountyID = _zdo.GetString(BountyIDKey);
                EpicLoot.LogWarning($"SENDING -> RPC_SlayBountyTargetFromBountyId: (bountyID={bountyID}) {_monsterID} ({(_isAdd ? "minion" : "target")})");
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SlayBountyIDTarget", _monsterID, _isAdd, bountyID);
            }
        }

        public void Initialize(BountyInfo bounty, string monsterID, bool isAdd)
        {
            _zdo.Set(BountyIDKey, bounty.ID);
            if (ZNet.instance.IsServer() || !ZNet.instance.IsServer() && !ZNet.instance.IsDedicated())
            {
                var pkg = new ZPackage();
                bounty.ToPackage(pkg);
                pkg.SetPos(0);
                _zdo.Set(BountyDataKey, pkg.GetBase64());
            }
            _zdo.Set(MonsterIDKey, monsterID);
            _zdo.Set(IsAddKey, isAdd);
            _zdo.Set(BountyTargetNameKey, GetTargetName(_character.m_name, isAdd, bounty.TargetName));
            
            _character.SetLevel(GetTargetLevel(bounty, monsterID, isAdd));
            _character.SetMaxHealth(GetModifiedMaxHealth(_character, bounty, isAdd));
            _character.m_baseAI.SetPatrolPoint();

            Reinitialize();
        }

        public void Reinitialize()
        {
            if (ZNet.instance.IsServer() || !ZNet.instance.IsServer() && !ZNet.instance.IsDedicated())
            { 
                var pkgString = _zdo.GetString(BountyDataKey);
                var pkg = new ZPackage(pkgString);
                try
                {
                    _bountyInfo = BountyInfo.FromPackage(pkg);
                }
                catch (Exception)
                {
                    Debug.LogError($"[EpicLoot] Error loading bounty info on creature ({name})! Possibly old or outdated bounty target, destroying creature.\nBountyData:\n{pkgString}");
                    _zdo.Set("BountyTarget", "");
                    _zdo.Set(BountyDataKey, "");
                    _character.m_nview.Destroy();
                    return;
                }
            }
            _monsterID = _zdo.GetString(MonsterIDKey);
            _isAdd = _zdo.GetBool(IsAddKey);

            _character.m_name = _zdo.GetString(BountyTargetNameKey);
            _character.m_boss = !_zdo.GetBool(IsAddKey);
        }

        private static float GetModifiedMaxHealth(Character character, BountyInfo bounty, bool isAdd)
        {
            if (isAdd)
            {
                return character.GetMaxHealth() * AdventureDataManager.Config.Bounties.AddsHealthMultiplier;
            }

            if (bounty.RewardGold > 0)
            {
                return character.GetMaxHealth() * AdventureDataManager.Config.Bounties.GoldHealthMultiplier;
            }

            return character.GetMaxHealth() * AdventureDataManager.Config.Bounties.IronHealthMultiplier;
        }

        private static string GetTargetName(string originalName, bool isAdd, string targetName)
        {
            return isAdd ? 
                Localization.instance.Localize("$mod_epicloot_bounties_minionname", originalName) 
                : (string.IsNullOrEmpty(targetName) ? originalName : targetName);
        }

        private static int GetTargetLevel(BountyInfo bounty, string monsterID, bool isAdd)
        {
            if (isAdd)
            {
                foreach (var targetInfo in bounty.Adds)
                {
                    if (targetInfo.MonsterID == monsterID)
                    {
                        return targetInfo.Level;
                    }
                }

                return 1;
            }

            return bounty.Target.Level;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Start))]
    [HarmonyPatch(typeof(Character), nameof(Character.Start))]
    public static class Character_Start_Patch
    {
        public static void Postfix(Character __instance)
        {
            var zdo = __instance.m_nview != null ? __instance.m_nview.GetZDO() : null;

            if (zdo != null && zdo.IsValid())
            {
                var old = !string.IsNullOrEmpty(zdo.GetString("BountyTarget"));

                if (old)
                {
                    EpicLoot.LogWarning($"Destroying old bounty target: {__instance.name}");
                    zdo.Set("BountyTarget", "");
                    __instance.m_nview.Destroy();
                    return;
                }

                if (!string.IsNullOrEmpty(zdo.GetString(BountyTarget.BountyIDKey)))
                {
                    var bountyTarget = __instance.gameObject.AddComponent<BountyTarget>();
                    bountyTarget.Reinitialize();
                }
            }
        }
    }
}
