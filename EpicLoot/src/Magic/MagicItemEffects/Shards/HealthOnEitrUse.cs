using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a small heal over time when the player spends Eitr. The heal is triggered every time the player spends a threshold amount of Eitr.
    public static class HealthOnEitrUse {
        // Threshold of Eitr spent before the player is healed. Tunable as "EitrPerTrigger" in this
        // effect's Config block in config/shardstones.json.
        public const float DefaultEitrPerTrigger = 100f;

        private const string EitrPerTriggerKey = "EitrPerTrigger";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { EitrPerTriggerKey, DefaultEitrPerTrigger },
        };

        // Floored at 1 because the payout divides by this: a configured 0 would turn a single cast into an
        // unbounded number of triggers.
        private static float GetEitrPerTrigger() {
            return Mathf.Max(1f,
                EffectConfig.Get(MagicEffectType.HealthOnEitrUse, EitrPerTriggerKey, DefaultEitrPerTrigger));
        }

        // Tooltip: "Heal {0} per {1} Eitr Spent" -- {1} is the configured threshold, so the shown number
        // follows a retune instead of staying at the baked-in default.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.HealthOnEitrUse,
                value => new object[] { value, GetEitrPerTrigger() });
        }

        // Eitr the local player has spent with the effect active but not yet paid out as a heal. Carries
        // the sub-threshold remainder across casts.
        private static float _accumulatedEitr;

        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class UseEitr_Patch {
            [UsedImplicitly]
            private static void Postfix(Player __instance, float v) {
                if (v <= 0f || __instance != Player.m_localPlayer) {
                    return;
                }

                // The shard is socketed into the casting weapon, so read its per-weapon value.
                var weapon = MagicEffectsHelper.GetActiveWeapon(__instance);
                if (weapon == null || !weapon.IsMagic()) {
                    return;
                }

                var healthPerTrigger = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                    __instance, weapon, MagicEffectType.HealthOnEitrUse);
                if (healthPerTrigger <= 0f) {
                    return;
                }

                var eitrPerTrigger = GetEitrPerTrigger();
                _accumulatedEitr += v;
                if (_accumulatedEitr < eitrPerTrigger) {
                    return;
                }

                var triggers = (int)(_accumulatedEitr / eitrPerTrigger);
                _accumulatedEitr -= triggers * eitrPerTrigger;
                __instance.Heal(triggers * healthPerTrigger);
            }
        }
    }
}
