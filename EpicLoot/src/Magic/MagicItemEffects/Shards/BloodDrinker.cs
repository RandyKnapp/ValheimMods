using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class BloodDrinker {
        // Max health removed, as a percent of the health pool, per point of shard value.
        public const float DefaultMaxHealthPercentPerValue = 2.5f;

        // Floor on the amount removed: a pure percentage is negligible on a low-food character.
        public const float DefaultMinHealthReduction = 10f;

        // Absolute backstop so a degenerate pool can't be reduced to a zero/negative max health. Not
        // configurable: it exists to keep a misconfiguration from bricking the character, so exposing it
        // would defeat the purpose.
        private const float MinResultingMaxHealth = 1f;

        private const string MaxHealthPercentPerValueKey = "MaxHealthPercentPerValue";
        private const string MinHealthReductionKey = "MinHealthReduction";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { MaxHealthPercentPerValueKey, DefaultMaxHealthPercentPerValue },
            { MinHealthReductionKey, DefaultMinHealthReduction },
        };

        // Both floored at zero: a negative cost would hand out max health instead of trading it away.
        private static float GetMaxHealthPercentPerValue() {
            return Mathf.Max(0f, EffectConfig.Get(MagicEffectType.BloodDrinker,
                MaxHealthPercentPerValueKey, DefaultMaxHealthPercentPerValue));
        }

        private static float GetMinHealthReduction() {
            return Mathf.Max(0f, EffectConfig.Get(MagicEffectType.BloodDrinker,
                MinHealthReductionKey, DefaultMinHealthReduction));
        }

        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.BloodDrinker,
                value => new object[] { value, value * GetMaxHealthPercentPerValue() });
        }

        private static void ApplyMaxHealthReduction(Player player, ref float hp) {
            if (player != Player.m_localPlayer) {
                return;
            }

            // Memoized effect value before the config lookups: this hangs off the max-health pipeline.
            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.BloodDrinker);
            if (value <= 0f) {
                return;
            }

            // Clamped so stacked sources can't reach a >=100% reduction.
            var percent = Mathf.Clamp01(value * GetMaxHealthPercentPerValue() * 0.01f);
            if (percent <= 0f) {
                return;
            }

            var reduction = Mathf.Max(hp * percent, GetMinHealthReduction());
            hp = Mathf.Max(hp - reduction, MinResultingMaxHealth);
        }

        // Postfix handler invoked by SharedCharacterDamagePatch (on-hit reaction, attacker side).
        public static void OnDamageDealt(HitData hit, Character attacker, float healthBeforeHit) {
            if (!(attacker is Player player) || player != Player.m_localPlayer) {
                return;
            }

            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.BloodDrinker, 0.01f);
            if (fraction <= 0f) {
                return;
            }

            var heal = MagicEffectsHelper.GetCountedDamage(hit, healthBeforeHit) * fraction;
            if (heal > 0f) {
                player.Heal(heal);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
        public static class Player_GetTotalFoodValue_Patch {
            public static void Postfix(Player __instance, ref float hp) {
                ApplyMaxHealthReduction(__instance, ref hp);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetBaseFoodHP))]
        public static class Player_GetBaseFoodHP_Patch {
            public static void Postfix(Player __instance, ref float __result) {
                ApplyMaxHealthReduction(__instance, ref __result);
            }
        }
    }
}
