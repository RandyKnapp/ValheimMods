using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.MagicItemEffects
{
    public class Spinny : MonoBehaviour
    {
        private const float RotationSpeed = 720;

        public void Awake()
        {
            transform.Rotate(-90, 270, 0);
        }

        public void Update()
        {
            transform.Rotate(0, -RotationSpeed * Time.deltaTime, 0);
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    public static class Throwable_Humanoid_StartAttack_Patch
    {
        public static bool Prefix(Humanoid __instance, ref bool __result, bool secondaryAttack)
        {
            if (!secondaryAttack)
            {
                return true;
            }

            __instance.ClearActionQueue();
            if (__instance.InAttack() && !__instance.HaveQueuedChain() || __instance.InDodge() ||
                !__instance.CanMove() || __instance.IsKnockedBack() || __instance.IsStaggering() || __instance.InMinorAction())
            {
                return true;
            }

            var currentWeapon = __instance.GetCurrentWeapon();
            if (currentWeapon == null || currentWeapon.m_dropPrefab == null)
            {
                return true;
            }

            if (!currentWeapon.IsMagic() || !currentWeapon.GetMagicItem().HasEffect(MagicEffectType.Throwable, includeSocketed: true) ||
                currentWeapon.m_shared.m_tamedOnly) // Temporary fix for all powerful butcher knifes
            {
                return true;
            }

            var spearPrefab = ObjectDB.instance?.GetItemPrefab("SpearFlint");
            if (spearPrefab == null)
            {
                return true;
            }

            if (__instance.m_currentAttack != null)
            {
                __instance.m_currentAttack.Stop();
                __instance.m_previousAttack = __instance.m_currentAttack;
                __instance.m_currentAttack = null;
            }

            var attack = spearPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_secondaryAttack.Clone();

            if (!attack.Start(__instance, __instance.m_body, __instance.m_zanim, __instance.m_animEvent,
                __instance.m_visEquipment, currentWeapon, __instance.m_previousAttack,
                __instance.m_timeSinceLastAttack, __instance.GetAttackDrawPercentage()))
            {
                return false;
            }

            __instance.m_currentAttack = attack;
            __instance.m_lastCombatTimer = 0.0f;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.ProjectileAttackTriggered))]
    public static class Throwable_Attack_ProjectileAttackTriggered_Patch
    {
        public static void Prefix(Attack __instance, ref EffectList __state)
        {
            if (__instance.m_weapon.IsMagic() && __instance.m_weapon.GetMagicItem().HasEffect(MagicEffectType.Throwable, includeSocketed: true))
            {
                __state = __instance.m_weapon.m_shared.m_triggerEffect;
                __instance.m_weapon.m_shared.m_triggerEffect = new EffectList();
            }
        }

        public static void Postfix(Attack __instance, EffectList __state)
        {
            if (__instance.m_weapon.IsMagic() && __instance.m_weapon.GetMagicItem().HasEffect(MagicEffectType.Throwable, includeSocketed: true))
            {
                if (__instance.m_weapon.m_lastProjectile.GetComponent<Projectile>() is Projectile projectile)
                {
                    projectile.m_spawnOnHitEffects = new EffectList { m_effectPrefabs =
                        projectile.m_spawnOnHitEffects.m_effectPrefabs.Concat(__state.m_effectPrefabs).ToArray() };
                    projectile.m_aoe = __instance.m_weapon.m_shared.m_attack.m_attackRayWidth;
                }

                __instance.m_weapon.m_shared.m_triggerEffect = __state;
            }
        }
    }

    [HarmonyPatch]
    public static class Throwable_Projectile_HitData_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Projectile), nameof(Projectile.OnHit));
            yield return AccessTools.Method(typeof(Projectile), nameof(Projectile.DoAOE));
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            ConstructorInfo constructor = AccessTools.Constructor(typeof(HitData));
            MethodInfo restoreToolTier = AccessTools.Method(typeof(Throwable_Projectile_HitData_Patch), nameof(RestoreToolTier));
            List<CodeInstruction> result = new List<CodeInstruction>();
            int matches = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                result.Add(instruction);
                if (instruction.opcode == OpCodes.Newobj && Equals(instruction.operand, constructor))
                {
                    result.Add(new CodeInstruction(OpCodes.Dup));
                    result.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    result.Add(new CodeInstruction(OpCodes.Call, restoreToolTier));
                    ++matches;
                }
            }

            // With no match nothing was inserted, so result is the method unchanged.
            if (matches == 0)
            {
                EpicLoot.LogWarningForce($"Throwable: could not find where Projectile.{original?.Name} creates its hit, " +
                    "so thrown weapons there will not keep their tool tier. Another mod may have changed that method.");
            }

            return result;
        }

        private static void RestoreToolTier(HitData hit, Projectile projectile)
        {
            HitData originalHit = projectile.m_originalHitData;
            if (originalHit == null || projectile.m_weapon == null ||
                !projectile.m_weapon.HasMagicEffect(MagicEffectType.Throwable, includeSocketed: true))
            {
                return;
            }

            // Projectile impacts create fresh hit data and omit the weapon's tool tier.
            // Keep the launch-time tier and world level for the normal resource checks.
            hit.m_toolTier = originalHit.m_toolTier;
            hit.m_itemWorldLevel = originalHit.m_itemWorldLevel;
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
    public static class Throwable_Attack_FireProjectileBurst_Patch
    {
        public static void Postfix(Attack __instance)
        {
            if (__instance.m_weapon.m_lastProjectile != null && __instance.m_weapon.IsMagic() &&
                __instance.m_weapon.GetMagicItem().HasEffect(MagicEffectType.Throwable, includeSocketed: true))
            {
                var existingMesh = __instance.m_weapon.m_lastProjectile.transform.Find("spear");
                if (existingMesh != null)
                {
                    ZNetScene.instance.Destroy(existingMesh.gameObject);
                }

                var weaponMesh = __instance.m_weapon.m_dropPrefab.transform.Find("attach");
                if (weaponMesh == null)
                {
                    return;
                }

                var newMesh = Object.Instantiate(weaponMesh.gameObject, __instance.m_weapon.m_lastProjectile.transform, false);
                newMesh.AddComponent<Spinny>();
            }
        }
    }
}
