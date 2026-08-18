using EpicLoot.General;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a lifesteal effect at the cost of a reduced max health pool.
    public static class BloodDrinker {
        // Max health removed, as a percent of the health pool, per 1 point of shard value. With the
        // shard's 3/6/9/12/15 values this yields -7.5%/-15%/-22.5%/-30%/-37.5% across Magic..Mythic.
        private const float MaxHealthPercentPerValue = 2.5f;

        // Floor on the amount removed. A pure percentage is negligible on a low-food character (37.5% of
        // the vanilla 25 base pool is ~9), so the cost never drops below this.
        private const float MinHealthReduction = 10f;

        // Absolute backstop so a degenerate pool can't be reduced to a zero/negative max health.
        private const float MinResultingMaxHealth = 1f;

        // Tooltip: "-{1}% Max Health, +{0}% Lifesteal" -- {1} is derived from the rolled value so the
        // shown cost stays in sync with the code rather than a baked-in literal.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.BloodDrinker,
                value => new object[] { value, value * MaxHealthPercentPerValue });
        }

        private static void ApplyMaxHealthReduction(Player player, ref float hp) {
            if (player != Player.m_localPlayer) {
                return;
            }

            // Clamped so stacked sources can't reach a >=100% reduction.
            var percent = Mathf.Clamp01(player.GetTotalActiveMagicEffectValue(
                MagicEffectType.BloodDrinker, MaxHealthPercentPerValue * 0.01f));
            if (percent <= 0f) {
                return;
            }

            var reduction = Mathf.Max(hp * percent, MinHealthReduction);
            hp = Mathf.Max(hp - reduction, MinResultingMaxHealth);
        }

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction, attacker side).
        public static void OnDamageDealt(HitData hit, Character attacker) {
            if (!(attacker is Player player) || player != Player.m_localPlayer) {
                return;
            }

            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.BloodDrinker, 0.01f);
            if (fraction <= 0f) {
                return;
            }

            var heal = hit.m_damage.EpicLootGetTotalDamage() * fraction;
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
