using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.FixedUpdate))]
    public static class ModifyAttackSpeed_CharacterAnimEvent_FixedUpdate_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character ___m_character, ref Animator ___m_animator)
        {
            if (!___m_character.IsPlayer() || !___m_character.InAttack())
            {
                return;
            }

            // check if our marker bit is present and not within float epsilon
            var currentSpeedMarker = ___m_animator.speed * 1e7 % 100;
            if (currentSpeedMarker > 10 && currentSpeedMarker < 30)
            {
                return;
            }

            var player = (Player)___m_character;
            var currentAttack = player.m_currentAttack;
            if (currentAttack == null)
            {
                return;
            }

            var animationSpeedup = 0.0f;
            ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyAttackSpeed, effect =>
            {
                animationSpeedup += player.GetTotalActiveMagicEffectValue(effect, 0.01f);
            });

            if (___m_animator.speed > 0.001f)
            {
                ___m_animator.speed = ___m_animator.speed * (1 + animationSpeedup) + 19e-7f; // number with single bit in mantissa set
            }
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.Update))]
    public class ModifyAttackSpeed_Attack_Update_Patch
    {
        public static void Postfix(Attack __instance)
        {
            var animator = __instance.m_character.m_animator;
            var animSpeed = animator.speed.ToString("0.00");
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            var transitionInfo = animator.GetAnimatorTransitionInfo(0);
            var animSpeedMult = stateInfo.speedMultiplier.ToString("0.00");
            DebugText.SetText($"state={stateInfo.fullPathHash}, speed={animSpeed}, mult={animSpeedMult}, transition={transitionInfo.fullPathHash}");
        }
    }
}
