using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public static class ModifyStaggerDuration
    {
        public const string ZdoKey = "el-sd";
        public const string RpcKey = "EL_TagStaggerDuration";

        // The tag must be checked on the client that owns the player wearing the effect (a remote
        // player's magic data is not replicated) and written by the client that owns the target's
        // ZDO (a non-owner Set is reverted on the owner's next sync). Same check-here/apply-there
        // pattern as Slow/Paralyze: owner writes directly, everyone else routes.
        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        public static class RegisterRpc_Character_Awake_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance)
            {
                __instance.m_nview?.Register<float>(RpcKey, (sender, value) => RPC_TagStaggerDuration(__instance, value));
            }
        }

        private static void RPC_TagStaggerDuration(Character character, float value)
        {
            if (character == null || character.m_nview == null ||
                !character.m_nview.IsValid() || !character.m_nview.IsOwner())
            {
                return;
            }

            character.m_nview.GetZDO().Set(ZdoKey, value);
        }

        public static void TagTarget(Character target, float staggerValue)
        {
            if (target == null || target.m_nview == null || !target.m_nview.IsValid())
            {
                return;
            }

            if (target.m_nview.IsOwner())
            {
                target.m_nview.GetZDO().Set(ZdoKey, staggerValue);
            }
            else
            {
                target.m_nview.InvokeRPC(ZNetView.Everybody, RpcKey, staggerValue);
            }
        }
    }

    // Stretches a stagger by slowing the animator while it plays. Vanilla starts every stagger at
    // speed 1 (Character.RPC_Stagger), and some stagger clips retime themselves with Speed animation
    // events (Fuling berserker, seekers). AnimationSpeedManager hands this handler each fresh vanilla
    // speed and marks the result, so the stagger lasts factor x its vanilla length on every creature,
    // without compounding frame to frame or flattening those events. The speed must stay relative:
    // deriving it from the clip length instead makes every stagger a flat length, which shortens it
    // on any creature whose stagger clip runs longer than that.
    [HarmonyPatch(typeof(Game), nameof(Game.Awake))]
    public static class ModifyStaggerDuration_AnimationHandler_Patch
    {
        private static bool _registered;

        public static double ModifyStaggerSpeed(Character character, double speed)
        {
            if (character == null || character.m_nview == null || !character.IsStaggering())
            {
                return speed;
            }

            ZDO zdo = character.m_nview.GetZDO();
            if (zdo == null)
            {
                return speed;
            }

            float factor = zdo.GetFloat(ModifyStaggerDuration.ZdoKey);
            return factor > 1f ? speed / factor : speed;
        }

        [UsedImplicitly]
        private static void Postfix()
        {
            // Game.Awake runs once per world load; registering again would divide by the factor twice.
            if (_registered)
            {
                return;
            }

            _registered = true;
            AnimationSpeedManager.Add(ModifyStaggerSpeed);
        }
    }

    public static class RPC_TagCharacterOnHit_Character_RPC_Damage_Patch
    {
        // Prefix handler invoked by SharedCharacterDamagePatch -- ATTACKER side. It used to live on
        // the RPC_Damage (victim-owner) dispatcher, where a remote attacking player's magic data
        // reads as empty, so the melee bonus never applied in multiplayer. The check runs here on
        // the attacker's client; the write is routed to the target's owner via TagTarget.
        public static void TagStaggerDuration(Character __instance, HitData hit, Character attacker)
        {
            if (__instance == null || __instance.m_nview == null || !__instance.m_nview.IsValid())
            {
                return;
            }

            if (!__instance.IsStaggering() && hit.m_skill != Skills.SkillType.Bows && hit.m_skill != Skills.SkillType.None)
            {
                var staggerValue = 1f;
                if (attacker is Player player && player == Player.m_localPlayer)
                {
                    staggerValue += player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyStaggerDuration, 0.01f);
                }
                ModifyStaggerDuration.TagTarget(__instance, staggerValue);
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    public static class TagCharacterOnBlock_Humanoid_BlockAttack_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Humanoid __instance, Character attacker)
        {
            if (attacker != null && !attacker.IsStaggering())
            {
                var staggerValue = 1f;
                if (__instance is Player player)
                {
                    staggerValue += player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyStaggerDuration, 0.01f);
                }
                // Routed: the blocker rarely owns the attacker's ZDO, and a non-owner Set is
                // reverted on the owner's next sync.
                ModifyStaggerDuration.TagTarget(attacker, staggerValue);
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
                if (character != null && __instance != null && __instance.m_nview != null && __instance.m_nview.GetZDO() is ZDO zdo)
                {
                    // Routed like the melee and block tags: the shooter rarely owns the target's ZDO.
                    var staggerValue = zdo.GetFloat(ModifyStaggerDuration.ZdoKey, 1f);
                    ModifyStaggerDuration.TagTarget(character, staggerValue);
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

        private static readonly MethodInfo AttackProjectileMarker = AccessTools.DeclaredMethod(
            typeof(StaggeringProjectileInstantiation_Attack_FireProjectileBurst_Patch), nameof(MarkAttackProjectile));
        private static readonly MethodInfo Instantiator = AccessTools
            .GetDeclaredMethods(typeof(Object)).Where(m => m.Name == "Instantiate" && m.GetGenericArguments().Length == 1)
            .Select(m => m.MakeGenericMethod(typeof(GameObject)))
            .First(m => m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(Vector3));

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
