using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [RequireComponent(typeof(Character))]
    public class BountyTarget : MonoBehaviour
    {
        public const string BountyTargetKey = "BountyTarget";
        public const string MonsterIDKey = "MonsterID";

        private Character _character;
        private Humanoid _humanoid;
        private BountyInfo _bountyInfo;
        private string _monsterID;

        public void Awake()
        {
            _character = GetComponent<Character>();
            _character.m_onDeath += OnDeath;

            _humanoid = GetComponent<Humanoid>();
        }

        public void OnDestroy()
        {
            if (_character != null)
            {
                _character.m_onDeath -= OnDeath;
            }
        }

        private void OnDeath()
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                var saveData = player.GetAdventureSaveData();
                if (saveData.GetBountyInfoByID(_bountyInfo.ID) != null && _bountyInfo.State == BountyState.InProgress)
                {
                    AdventureDataManager.SlayBountyTarget(_bountyInfo, _monsterID);
                }
            }
        }

        public void Setup(BountyInfo bounty, string monsterID)
        {
            _bountyInfo = bounty;
            _monsterID = monsterID;

            var zdo = _character.m_nview?.GetZDO();
            if (zdo != null && zdo.IsValid())
            {
                zdo.Set(BountyTargetKey, _bountyInfo.ID);
                zdo.Set(MonsterIDKey, monsterID);
            }

            _character.m_name = "Target: " + _character.name;
            _character.SetLevel(3); // TODO: Configurable
            _character.m_baseAI.SetPatrolPoint();
            _character.m_boss = true;
        }
    }

    [HarmonyPatch(typeof(Character), "Start")]
    public static class Character_Start_Patch
    {
        public static void Postfix(Character __instance)
        {
            var zdo = __instance.m_nview?.GetZDO();
            if (zdo != null && zdo.IsValid())
            {
                var bountyID = zdo.GetString(BountyTarget.BountyTargetKey);
                if (!string.IsNullOrEmpty(bountyID))
                {
                    var bountyInfo = Player.m_localPlayer?.GetAdventureSaveData().GetBountyInfoByID(bountyID);
                    if (bountyInfo != null)
                    {
                        var bountyTarget = __instance.gameObject.AddComponent<BountyTarget>();
                        var monsterID = zdo.GetString(BountyTarget.MonsterIDKey);
                        bountyTarget.Setup(bountyInfo, monsterID);
                    }
                }
            }
        }
    }
}
