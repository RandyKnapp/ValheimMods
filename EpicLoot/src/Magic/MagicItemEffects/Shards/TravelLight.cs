using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to move speed at the cost of carry weight
    public static class TravelLight {
        // Max carry weight removed per 1 point of shard value (i.e. per 1% of move speed gained), and the
        // backstop that stops stacked sources pushing the carry cap to zero or below. Tunable as
        // "CarryWeightPerValue" and "MinResultingCarryWeight" in this effect's Config block in
        // config/shardstones.json.
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

        // Tooltip: "+{0}% Move Speed, -{1} Carry Weight" -- {1} is derived from the rolled value and the
        // configured rate, so the shown cost follows a retune instead of the baked-in default.
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

        // ModifyMaxCarryWeight handler invoked by SharedSEManModifyMaxCarryWeightPatch, which runs it
        // last: the MinResultingCarryWeight clamp measures against the running total, so it has to see
        // every bonus the other handlers added.
        public static void ModifyMaxCarryWeight(Player player, ref float limit) {
            // Read the memoized effect value before touching config: this hangs off GetMaxCarryWeight,
            // which vanilla reaches at 50Hz via UpdateStats -> IsEncumbered, and most players carry no
            // TravelLight shard at all. The config lookups only happen once that check passes.
            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.TravelLight);
            if (value <= 0f) {
                return;
            }

            limit = Mathf.Max(limit - value * GetCarryWeightPerValue(), GetMinResultingCarryWeight());
        }
    }
}
