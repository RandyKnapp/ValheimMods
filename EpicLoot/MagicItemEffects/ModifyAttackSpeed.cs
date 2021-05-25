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
            if ((currentSpeedMarker > 10 && currentSpeedMarker < 30) || ___m_animator.speed <= 0.001f)
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

            ___m_animator.speed = ___m_animator.speed * (1 + animationSpeedup) + 19e-7f; // number with single bit in mantissa set
        }
    }
}
