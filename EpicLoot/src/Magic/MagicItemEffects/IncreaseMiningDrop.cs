using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public class IncreaseMiningDrop : IncreaseDrop
{
    public static IncreaseMiningDrop Instance { get; private set; }

    private static Character _rockAttacker;

    static IncreaseMiningDrop()
    {
        Instance = new IncreaseMiningDrop()
        {
            MagicEffect = MagicEffectType.IncreaseMiningDrop,
            ZDOVar = "el-mining",
            LegacyValueScale = 25f,
            FillerItem = "Stone"
        };
    }

    private static bool IsRock(Destructible destructible)
    {
        return destructible.GetDestructibleType() == DestructibleType.Default &&
            destructible.m_damages.m_chop == HitData.DamageModifier.Immune &&
            destructible.m_damages.m_pickaxe != HitData.DamageModifier.Immune;
    }

    [HarmonyPatch(typeof(MineRock), nameof(MineRock.RPC_Hit))]
    public static class IncreaseMiningDrop_MineRock_RPC_Hit_Patch
    {
        // Vanilla gives no "area destroyed" signal here: ZNetScene.Destroy only resets the ZDO and defers the
        // Unity destroy, so compare the area's health either side of the hit.
        private static void Prefix(MineRock __instance, int hitAreaIndex, ref float __state)
        {
            __state = -1f;
            if (__instance.m_nview != null && __instance.m_nview.IsValid())
            {
                __state = __instance.m_nview.GetZDO().GetFloat("Health" + hitAreaIndex, __instance.GetHealth());
            }
        }

        private static void Postfix(MineRock __instance, HitData hit, int hitAreaIndex, float __state)
        {
            if (hit == null || __state <= 0f)
            {
                return;
            }

            ZDO zdo = __instance.m_nview != null ? __instance.m_nview.GetZDO() : null;
            bool areaDiedThisHit = zdo == null || zdo.GetFloat("Health" + hitAreaIndex, 1f) <= 0f;
            if (areaDiedThisHit)
            {
                Instance.Grant(hit.GetAttacker(), __instance.m_dropItems, hit.m_point - hit.m_dir * 0.2f);
            }
        }
    }

    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.RPC_Damage))]
    public static class IncreaseMiningDrop_MineRock5_RPC_Damage_Patch
    {
        private static void Prefix(HitData hit)
        {
            _rockAttacker = hit.GetAttacker();
            Instance.BeginBatch(hit.m_point);
        }

        private static void Finalizer()
        {
            _rockAttacker = null;
            Instance.EndBatch();
        }
    }

    [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.DamageArea))]
    public static class IncreaseMiningDrop_MineRock5_DamageArea_Patch
    {
        // A destroyed chunk's collider is deactivated inside DamageArea (RPC_SetAreaHealth runs synchronously and
        // UpdateMesh turns it off), so its bounds are only meaningful before the call.
        private static void Prefix(MineRock5 __instance, HitData hit, int hitAreaIndex, ref Vector3 __state)
        {
            var hitArea = __instance.GetHitArea(hitAreaIndex);
            __state = (__instance.m_hitEffectAreaCenter && hitArea != null && hitArea.m_collider != null) ?
                hitArea.m_collider.bounds.center : hit.m_point;
        }

        private static void Postfix(MineRock5 __instance, HitData hit, bool __result, Vector3 __state)
        {
            if (hit != null && __result)
            {
                // CheckSupport destroys unsupported chunks with a HitData that has no attacker. It runs synchronously
                // inside RPC_Damage, so the chunk that started the collapse still names the player responsible.
                Character attacker = hit.m_hitType == HitData.HitType.Structural ? _rockAttacker : hit.GetAttacker();
                Instance.Grant(attacker, __instance.m_dropItems, __state);
            }
        }
    }

    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy))]
    public static class IncreaseMiningDrop_Destructible_Destroy_Patch
    {
        private static void Prefix(Destructible __instance, HitData hit)
        {
            if (hit == null || !IsRock(__instance))
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
