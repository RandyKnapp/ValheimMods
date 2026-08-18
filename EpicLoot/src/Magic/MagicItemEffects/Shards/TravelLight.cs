using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to move speed at the cost of carry weight
    public static class TravelLight {
        // Max carry weight removed per 1 point of shard value (i.e. per 1% of move speed gained).
        private const float CarryWeightPerValue = 10f;

        // Backstop so stacked sources can't push the carry cap to zero or below.
        private const float MinResultingCarryWeight = 50f;

        // Tooltip: "+{0}% Move Speed, -{1} Carry Weight" -- {1} is derived from the rolled value so the
        // shown cost stays in sync with the code rather than a baked-in literal.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.TravelLight,
                value => new object[] { value, value * CarryWeightPerValue });
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ApplyStatusEffectSpeedMods))]
        private static class ApplyStatusEffectSpeedMods_Patch {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float speed) {
                var player = Player.m_localPlayer;
                if (__instance.m_character != player) {
                    return;
                }

                var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.TravelLight, 0.01f);
                if (bonus > 0f) {
                    speed *= 1f + bonus;
                }
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyMaxCarryWeight))]
        private static class ModifyMaxCarryWeight_Patch {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float limit) {
                var player = Player.m_localPlayer;
                if (__instance.m_character != player) {
                    return;
                }

                var reduction = player.GetTotalActiveMagicEffectValue(
                    MagicEffectType.TravelLight, CarryWeightPerValue);
                if (reduction > 0f) {
                    limit = Mathf.Max(limit - reduction, MinResultingCarryWeight);
                }
            }
        }
    }
}
