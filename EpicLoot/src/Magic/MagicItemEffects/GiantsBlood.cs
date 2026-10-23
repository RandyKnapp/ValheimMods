using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public static class GiantsBlood
    {
        public const float CarryWeightPctPerPoint = 1f;      // 1:1
        public const float HealthRegenPctPerPoint = 4f;      // 4:1
        public const float PhysicalDamagePctPerPoint = 0.5f; // 0.5:1

        private static readonly List<HitData.DamageModPair> FirePair = new List<HitData.DamageModPair> { default };

        public static float GiantsBloodValue(Player player) =>
            player == null ? 0f : player.GetTotalActiveMagicEffectValue(MagicEffectType.GiantsBlood);

        public static void ModifyMaxCarryWeight(Player player, float baseLimit, ref float limit)
        {
            var pct = player.GetTotalActiveMagicEffectValue(MagicEffectType.GiantsBlood, 0.01f);
            if (pct == 0f) return;

            // Of the base limit, as the other percentage carry effects do, not of the running total.
            limit += baseLimit * pct * CarryWeightPctPerPoint;
        }

        public static void ApplyHealthRegen(Player player, string magicEffect, ref float regenValue)
        {
            if (magicEffect != MagicEffectType.ModifyHealthRegen) return;

            regenValue += GiantsBloodValue(player) * HealthRegenPctPerPoint * 0.01f;
        }

        public static void ApplyPhysicalDamage(Player player, ref HitData.DamageTypes damages)
        {
            var value = GiantsBloodValue(player);
            if (value <= 0f) return;

            ModifyDamage.ModifyPhysicalDamage(ref damages, 1f + value * PhysicalDamagePctPerPoint * 0.01f);
        }

        public static bool TryGetFireModifier(Player player, out HitData.DamageModifier modifier)
        {
            // Strongest first: a full set holds all three, and the worst one is what it should suffer.
            if (player != null)
            {
                if (player.HasActiveMagicEffect(MagicEffectType.VeryWeakToFire))
                {
                    modifier = HitData.DamageModifier.VeryWeak;   // x2.0
                    return true;
                }

                if (player.HasActiveMagicEffect(MagicEffectType.WeakToFire))
                {
                    modifier = HitData.DamageModifier.Weak;       // x1.5
                    return true;
                }

                if (player.HasActiveMagicEffect(MagicEffectType.SlightlyWeakToFire))
                {
                    modifier = HitData.DamageModifier.SlightlyWeak;  // x1.25
                    return true;
                }
            }

            modifier = HitData.DamageModifier.Normal;
            return false;
        }
        [HarmonyPatch(typeof(Character), nameof(Character.GetDamageModifiers))]
        private static class Character_GetDamageModifiers_Patch
        {
            private static void Postfix(Character __instance, ref HitData.DamageModifiers __result)
            {
                var player = Player.m_localPlayer;
                if (player == null || __instance != player) return;

                if (!TryGetFireModifier(player, out var modifier)) return;

                FirePair[0] = new HitData.DamageModPair
                {
                    m_type = HitData.DamageType.Fire,
                    m_modifier = modifier,
                };
                __result.Apply(FirePair);
            }
        }

        private struct AttackRangeAndAoe
        {
            private bool _applied;
            private float _range;
            private float _height;
            private float _offset;
            private float _rayWidth;
            private float _rayWidthCharExtra;
            private float _heightChar1;
            private float _heightChar2;

            public static AttackRangeAndAoe Scale(Attack attack)
            {
                var state = new AttackRangeAndAoe();
                if (attack == null) return state;

                var scale = Transformation.GiantsBloodForm.Of(attack.m_character);
                if (Mathf.Approximately(scale, 1f)) return state;

                state._applied = true;
                state._range = attack.m_attackRange;
                state._height = attack.m_attackHeight;
                state._offset = attack.m_attackOffset;
                state._rayWidth = attack.m_attackRayWidth;
                state._rayWidthCharExtra = attack.m_attackRayWidthCharExtra;
                state._heightChar1 = attack.m_attackHeightChar1;
                state._heightChar2 = attack.m_attackHeightChar2;

                attack.m_attackRange *= scale;
                attack.m_attackHeight *= scale;
                attack.m_attackOffset *= scale;
                attack.m_attackRayWidth *= scale;
                attack.m_attackRayWidthCharExtra *= scale;

                attack.m_attackHeightChar1 *= scale;
                attack.m_attackHeightChar2 *= scale;

                return state;
            }

            public void Restore(Attack attack)
            {
                if (!_applied || attack == null) return;

                attack.m_attackRange = _range;
                attack.m_attackHeight = _height;
                attack.m_attackOffset = _offset;
                attack.m_attackRayWidth = _rayWidth;
                attack.m_attackRayWidthCharExtra = _rayWidthCharExtra;
                attack.m_attackHeightChar1 = _heightChar1;
                attack.m_attackHeightChar2 = _heightChar2;
            }
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        private static class Attack_DoMeleeAttack_Patch
        {
            private static void Prefix(Attack __instance, out AttackRangeAndAoe __state) =>
                __state = AttackRangeAndAoe.Scale(__instance);

            [HarmonyFinalizer]
            private static void Finalizer(Attack __instance, AttackRangeAndAoe __state) =>
                __state.Restore(__instance);
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.DoAreaAttack))]
        private static class Attack_DoAreaAttack_Patch
        {
            private static void Prefix(Attack __instance, out AttackRangeAndAoe __state) =>
                __state = AttackRangeAndAoe.Scale(__instance);

            [HarmonyFinalizer]
            private static void Finalizer(Attack __instance, AttackRangeAndAoe __state) =>
                __state.Restore(__instance);
        }

        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.GiantsBlood, value => new object[]
            {
                value,                              // {0} size %
                value * CarryWeightPctPerPoint,     // {1} carry weight %
                value * HealthRegenPctPerPoint,     // {2} health regen %
                value * PhysicalDamagePctPerPoint,  // {3} physical damage %
            });
        }
    }
}
