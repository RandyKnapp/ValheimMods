using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [RequireComponent(typeof(Character))]
    public class BountyTarget : MonoBehaviour
    {
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
            // TODO: save zdo data?
            if (_character != null)
            {
                _character.m_onDeath -= OnDeath;
            }
        }

        private bool HasBeenSetup()
        {
            var bountyID = _zdo.GetString(BountyTargetComponent.BountyIDKey);
            return !string.IsNullOrEmpty(bountyID);
        }

        private void OnDeath()
        {
            var pkg = new ZPackage();
            _bountyInfo.ToPackage(pkg);
            // Send death event to server
            EpicLoot.LogWarning($"SENDING -> RPC_SlayBountyTarget: {_monsterID} ({(_isAdd ? "minion" : "target")})");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SlayBountyTarget", pkg, _monsterID, _isAdd);
        }

        public void Initialize(BountyInfo bounty, string monsterID, bool isAdd)
        {
            _zdo.Set(BountyTargetComponent.BountyIDKey, bounty.ID);
            if (!ZNet.instance.IsDedicated())
            {
                var pkg = new ZPackage();
                bounty.ToPackage(pkg);
                pkg.SetPos(0);
                _zdo.Set(BountyTargetComponent.BountyDataKey, pkg.GetBase64());
            }

            _zdo.Set(BountyTargetComponent.MonsterIDKey, monsterID);
            _zdo.Set(BountyTargetComponent.IsAddKey, isAdd);
            _zdo.Set(BountyTargetComponent.BountyTargetNameKey, GetTargetName(_character.m_name, isAdd, bounty.TargetName));

            _character.SetLevel(GetTargetLevel(bounty, monsterID, isAdd));
            _character.SetMaxHealth(GetModifiedMaxHealth(_character, bounty, isAdd));
            _character.m_baseAI.SetPatrolPoint();

            Reinitialize();
        }

        /// <summary>
        /// Initialize custom data from the character zdo.
        /// </summary>
        public void Reinitialize()
        {
            if (!ZNet.instance.IsDedicated())
            {
                var pkgString = _zdo.GetString(BountyTargetComponent.BountyDataKey);
                var pkg = new ZPackage(pkgString);

                try
                {
                    _bountyInfo = BountyInfo.FromPackage(pkg);
                }
                catch (Exception)
                {
                    Debug.LogError($"[EpicLoot] Error loading bounty info on creature ({name})! " +
                        $"Possibly old or outdated bounty target, destroying creature." +
                        $"\nBountyData:\n{pkgString}");
                    DestroyInstance();
                    return;
                }

                // TODO: clean up abandoned bounties?
                // Currently does not track if abandoned on the zdo
                /*if (_bountyInfo.State == BountyState.Abandoned)
                {
                    // Destroy abandoned bounties
                    EpicLoot.Log("Destroying abandoned bounty!");
                    DestroyInstance();
                    return;
                }*/
            }

            _monsterID = _zdo.GetString(BountyTargetComponent.MonsterIDKey);
            _isAdd = _zdo.GetBool(BountyTargetComponent.IsAddKey);

            _character.m_name = _zdo.GetString(BountyTargetComponent.BountyTargetNameKey);
            _character.m_boss = !_zdo.GetBool(BountyTargetComponent.IsAddKey);
        }

        private void DestroyInstance()
        {
            _zdo.SetOwner(ZDOMan.GetSessionID());
            _character.m_nview.Destroy();
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

    static class BountyTargetComponent
    {
        public const string BountyIDKey = "BountyID";
        public const string BountyDataKey = "BountyData";
        public const string BountyTargetKey = "BountyTarget";
        public const string MonsterIDKey = "MonsterID";
        public const string IsAddKey = "IsAdd";
        public const string BountyTargetNameKey = "BountyTargetName";

        private static void StartBountyTarget(Character instance)
        {
            var zdo = instance.m_nview != null ? instance.m_nview.GetZDO() : null;

            if (zdo != null && zdo.IsValid())
            {
                if (!string.IsNullOrEmpty(zdo.GetString(BountyTargetKey)))
                {
                    EpicLoot.LogWarning($"Destroying old bounty target: {instance.name}");
                    zdo.SetOwner(ZDOMan.GetSessionID());
                    instance.m_nview.Destroy();
                    return;
                }

                if (!string.IsNullOrEmpty(zdo.GetString(BountyIDKey)))
                {
                    var bountyTarget = instance.gameObject.AddComponent<BountyTarget>();
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Start))]
        public static class Humanoid_Start_Patch
        {
            public static void Postfix(Humanoid __instance)
            {
                StartBountyTarget(__instance);
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Start))]
        public static class Character_Start_Patch
        {
            public static void Postfix(Character __instance)
            {
                StartBountyTarget(__instance);
            }
        }
    }
}
