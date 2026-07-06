using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public class IncreaseMiningDrop : IncreaseDrop
{
    public static IncreaseMiningDrop Instance { get; private set; }

    static IncreaseMiningDrop()
    {
        Instance = new IncreaseMiningDrop()
        {
            MagicEffect = MagicEffectType.IncreaseMiningDrop,
            ZDOVar = "el-mining"
        };
    }

    // Reset ZDO variable on equipment change
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    public static class IncreaseMiningDrop_Player_EquipmentChange_Patches
    {
        public static void Postfix(Humanoid __instance)
        {
            if (__instance == Player.m_localPlayer && __instance.m_nview.IsValid())
            {
                if (__instance.m_nview.GetZDO().GetInt(Instance.ZDOVar) != 0)
                {
                    __instance.m_nview.GetZDO().Set(Instance.ZDOVar, 0);
                }
            }
        }
    }

    [HarmonyPatch(typeof(MineRock), nameof(MineRock.Damage))]
    public static class IncreaseMiningDrop_MineRock_Damage_Patch
    {
        private static void Prefix(MineRock __instance, HitData hit)
        {
            Instance.DoPrefix(hit);
        }
    }

    [HarmonyPatch(typeof(MineRock), nameof(MineRock.RPC_Hit))]
    public static class IncreaseMiningDrop_MineRock_RPC_Hit_Patch
    {
        private static void Postfix(MineRock __instance, HitData hit)
        {
            if (hit != null && __instance.m_nview == null)
            {
                Instance.TryDropExtraItems(hit.GetAttacker(), __instance.m_dropItems, __instance.transform.position);
            }
        }
    }

    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
    public static class IncreaseMiningDrop_MineRock5_Damage_Patch
    {
        private static void Prefix(MineRock5 __instance, HitData hit)
        {
            Instance.DoPrefix(hit);
        }
    }

    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.DamageArea))]
    public static class IncreaseMiningDrop_MineRock5_RPC_Hit_Patch
    {
        private static void Postfix(MineRock5 __instance, HitData hit, int hitAreaIndex, ref bool __result)
        {
            if (hit != null && __result)
            {
                var hitArea = __instance.GetHitArea(hitAreaIndex);
                Vector3 position = (__instance.m_hitEffectAreaCenter && hitArea.m_collider != null) ?
                    hitArea.m_collider.bounds.center : hit.m_point;
                Instance.TryDropExtraItems(hit.GetAttacker(), __instance.m_dropItems, position);
            }
        }
    }

    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
    public static class IncreaseMiningDrop_Destructible_Damage_Patch
    {
        private static void Prefix(Destructible __instance, HitData hit)
        {
            if (__instance.GetDestructibleType() == DestructibleType.Default &&
                __instance.m_damages.m_chop == HitData.DamageModifier.Immune &&
                __instance.m_damages.m_pickaxe != HitData.DamageModifier.Immune)
            {
                Instance.DoPrefix(hit);
            }
        }
    }

    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy))]
    public static class IncreaseMiningDrop_Destructible_Destroy_Patch
    {
        private static void Prefix(Destructible __instance, HitData hit)
        {
            if (hit != null && __instance.GetDestructibleType() == DestructibleType.Default &&
                __instance.m_damages.m_chop == HitData.DamageModifier.Immune &&
                __instance.m_damages.m_pickaxe != HitData.DamageModifier.Immune)
            {
                var dropList = __instance.gameObject.GetComponent<DropOnDestroyed>();
                if (dropList == null)
                {
                    return;
                }

                Instance.TryDropExtraItems(hit.GetAttacker(), dropList.m_dropWhenDestroyed, __instance.transform.position);
            }
        }
    }
}
