using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class TravelLight {
        // MinResultingCarryWeight stops stacked sources pushing the carry cap to zero or below.
        public const float DefaultCarryWeightPerValue = 10f;
        public const float DefaultMinResultingCarryWeight = 50f;

        private const string CarryWeightPerValueKey = "CarryWeightPerValue";
        private const string MinResultingCarryWeightKey = "MinResultingCarryWeight";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { CarryWeightPerValueKey, DefaultCarryWeightPerValue },
            { MinResultingCarryWeightKey, DefaultMinResultingCarryWeight },
        };

        // Floored at zero: a negative rate would hand out carry weight rather than trade it away.
        private static float GetCarryWeightPerValue() {
            return Mathf.Max(0f, EffectConfig.Get(MagicEffectType.TravelLight,
                CarryWeightPerValueKey, DefaultCarryWeightPerValue));
        }

        // Floored at 1: a cap of zero would leave the player permanently encumbered.
        private static float GetMinResultingCarryWeight() {
            return Mathf.Max(1f, EffectConfig.Get(MagicEffectType.TravelLight,
                MinResultingCarryWeightKey, DefaultMinResultingCarryWeight));
        }

        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.TravelLight,
                value => new object[] { value, value * GetCarryWeightPerValue() });
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

        // SharedSEManModifyMaxCarryWeightPatch runs this last: the MinResultingCarryWeight clamp measures
        // against the running total, so it has to see every bonus the other handlers added.
        public static void ModifyMaxCarryWeight(Player player, ref float limit) {
            // Memoized effect value before the config lookups: this hangs off GetMaxCarryWeight at 50Hz.
            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.TravelLight);
            if (value <= 0f) {
                return;
            }

            limit = Mathf.Max(limit - value * GetCarryWeightPerValue(), GetMinResultingCarryWeight());
        }
    }
}
