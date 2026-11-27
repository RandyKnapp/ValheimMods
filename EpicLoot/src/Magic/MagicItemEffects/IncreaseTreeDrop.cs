using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public class IncreaseTreeDrop : IncreaseDrop
{
    public static IncreaseTreeDrop Instance { get; private set; }

    private static readonly int FelledBonusKey = "el-tree-felled".GetStableHashCode();

    private static float _fellingBonus;

    static IncreaseTreeDrop()
    {
        Instance = new IncreaseTreeDrop()
        {
            MagicEffect = MagicEffectType.IncreaseTreeDrop,
            ZDOVar = "el-tree",
            LegacyValueScale = 10f,
            FillerItem = "Wood",
            BetterItems = new HashSet<string>
            {
                "Blackwood", "ElderBark", "FineWood", "Frostwood", "RoundLog", "YggdrasilWood"
            }
        };
    }

    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.RPC_Damage))]
    public static class IncreaseTreeDrop_TreeBase_RPC_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            _fellingBonus = Instance.BonusOf(hit.GetAttacker());
        }

        private static void Postfix(TreeBase __instance, HitData hit)
        {
            // Vanilla's destroy path calls gameObject.SetActive(false) synchronously inside this very RPC, and only
            // that path does -- ZNetScene.Destroy defers the Unity destroy, so m_nview says nothing here.
            if (hit != null && !__instance.gameObject.activeSelf)
            {
                Vector3 position = __instance.transform.position + Vector3.up * __instance.m_spawnYOffset;
                Instance.Grant(hit.GetAttacker(), __instance.m_dropWhenDestroyed, position);
            }
        }

        private static void Finalizer()
        {
            _fellingBonus = 0f;
        }
    }

    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Destroy))]
    public static class IncreaseTreeDrop_TreeLog_Destroy_Patch
    {
        // Logs also break against the ground and each other, through ImpactEffect hits that have no attacker. The
        // logs of a felled tree carry the feller's bonus on their ZDO so those breaks still pay out.
        private static void Prefix(TreeLog __instance, HitData hitData, bool cheatedTool)
        {
            Character attacker = hitData?.GetAttacker();
            ZDO zdo = __instance.m_nview.GetZDO();
            float bonus = attacker != null ? Instance.BonusOf(attacker) : zdo?.GetFloat(FelledBonusKey) ?? 0f;

            Transform log = __instance.transform;
            Vector3 position = log.position + log.up * Random.Range(-__instance.m_spawnDistance, __instance.m_spawnDistance) +
                Vector3.up * 0.3f;
            Instance.Grant(bonus, cheatedTool, __instance.m_dropWhenDestroyed, position, hitData);
            _fellingBonus = bonus;
        }

        private static void Finalizer()
        {
            _fellingBonus = 0f;
        }
    }

    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Awake))]
    public static class IncreaseTreeDrop_TreeLog_Awake_Patch
    {
        private static void Postfix(TreeLog __instance)
        {
            if (_fellingBonus > 0f && __instance.m_nview != null && __instance.m_nview.GetZDO() is ZDO zdo)
            {
                zdo.Set(FelledBonusKey, _fellingBonus);
            }
        }
    }

    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy))]
    public static class IncreaseTreeDrop_Destructible_Destroy_Patch
    {
        private static void Prefix(Destructible __instance, HitData hit)
        {
            if (hit == null || __instance.GetDestructibleType() != DestructibleType.Tree)
            {
                return;
            }

            var dropper = __instance.gameObject.GetComponent<DropOnDestroyed>();
            if (dropper != null)
            {
                Instance.Grant(hit.GetAttacker(), dropper.m_dropWhenDestroyed, GroundedDropPosition(dropper));
            }
        }
    }
}
