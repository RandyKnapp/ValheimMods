using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public static class ModifyStaggerDuration
    {
        public const string ZdoKey = "el-sd";
    }

    [HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.FixedUpdate))]
    public static class ModifyStaggerDuration_CharacterAnimEvent_FixedUpdate_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character ___m_character, ref Animator ___m_animator)
        {
            if (___m_character.IsStaggering() && ___m_animator.speed > 0.001f)
            {
                if (!(___m_character.m_nview?.GetZDO() is ZDO zdo))
                {
                    return;
                }

                var speedupfactor = zdo.GetFloat(ModifyStaggerDuration.ZdoKey);

                if (speedupfactor > 1f)
                {
                    var animatorClipInfos = ___m_animator.GetCurrentAnimatorClipInfo(0);
                    if (animatorClipInfos.Length > 0)
                    {
                        var speed = animatorClipInfos[0].clip.length / speedupfactor * (animatorClipInfos[0].clip.name == "stagger2" ? 2 : 1);

                        ___m_animator.speed = speed;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    public static class RPC_TagCharacterOnHit_Character_RPC_Damage_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character __instance, HitData hit)
        {
            if (!__instance.IsStaggering() && hit.m_skill != Skills.SkillType.Bows && hit.m_skill != Skills.SkillType.None)
            {
                var staggerValue = 1f;
                if (hit.GetAttacker() is Player player)
                {
                    staggerValue += player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyStaggerDuration, 0.01f);
                }
                __instance.m_nview.GetZDO().Set(ModifyStaggerDuration.ZdoKey, staggerValue);
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    public static class TagCharacterOnBlock_Humanoid_BlockAttack_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Humanoid __instance, Character attacker)
        {
            if (!attacker.IsStaggering())
            {
                var staggerValue = 1f;
                if (__instance is Player player)
                {
                    staggerValue += player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyStaggerDuration, 0.01f);
                }
                attacker.m_nview.GetZDO().Set(ModifyStaggerDuration.ZdoKey, staggerValue);
            }
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
    public class StaggeringProjectileHit_Projectile_OnHit_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Projectile __instance, Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            var target = Projectile.FindHitObject(collider);
            if (target != null)
            {
                var character = target.GetComponent<Character>();
                if (character != null && __instance.m_nview?.GetZDO() is ZDO zdo)
                {
                    var staggerValue = zdo.GetFloat(ModifyStaggerDuration.ZdoKey, 1f);
                    character.m_nview.GetZDO().Set(ModifyStaggerDuration.ZdoKey, staggerValue);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
    public class StaggeringProjectileInstantiation_Attack_FireProjectileBurst_Patch
    {
        private static GameObject MarkAttackProjectile(GameObject attackProjectile, Attack attack)
        {
            if (attack != null && attackProjectile != null && attack.m_character == Player.m_localPlayer)
            {
                var znetView = attackProjectile.GetComponent<ZNetView>();
                if (znetView != null && znetView.GetZDO() != null)
                {
                    var staggerValue = 1f + Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyStaggerDuration, 0.01f);
                    znetView.GetZDO().Set(ModifyStaggerDuration.ZdoKey, staggerValue);
                }
            }

            return attackProjectile;
        }

        private static readonly MethodInfo AttackProjectileMarker = AccessTools.DeclaredMethod(typeof(StaggeringProjectileInstantiation_Attack_FireProjectileBurst_Patch), nameof(MarkAttackProjectile));
        private static readonly MethodInfo Instantiator = AccessTools.GetDeclaredMethods(typeof(Object)).Where(m => m.Name == "Instantiate" && m.GetGenericArguments().Length == 1).Select(m => m.MakeGenericMethod(typeof(GameObject))).First(m => m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(Vector3));

        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Call && instruction.OperandIs(Instantiator))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                    yield return new CodeInstruction(OpCodes.Call, AttackProjectileMarker);
                }
            }
        }
    }
}
