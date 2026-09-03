using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a stamina restoration effect based on fire damage taken, uses an accumulated threshold.
    public static class Kindling {
        // Fire damage taken per stamina trigger; higher = a slower trickle. Tunable as "DamagePerTrigger"
        // in this effect's Config block in config/shardstones.json.
        public const float DefaultDamagePerTrigger = 75f;

        private const string DamagePerTriggerKey = "DamagePerTrigger";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { DamagePerTriggerKey, DefaultDamagePerTrigger },
        };

        // Floored at 1 because the payout divides by this: a configured 0 would turn a single burn tick
        // into an unbounded number of triggers.
        private static float GetDamagePerTrigger() {
            return Mathf.Max(1f,
                EffectConfig.Get(MagicEffectType.Kindling, DamagePerTriggerKey, DefaultDamagePerTrigger));
        }

        // Tooltip: "Restore {0} Stamina per {1} Fire Damage Taken" -- {1} is the configured threshold, so
        // the shown number follows a retune instead of staying at the baked-in default.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Kindling,
                value => new object[] { value, GetDamagePerTrigger() });
        }

        // Cumulative fire damage the local player has taken with the effect active but not yet paid out as
        // stamina. Carries the sub-threshold remainder across burn ticks.
        private static float _accumulatedFireDamage;

        // Postfix rather than prefix so the read happens after vanilla applies Game.m_localDamgeTakenRate to
        // the hit in place -- we accumulate the fire damage the player actually suffered.
        [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
        private static class ApplyDamage_Patch {
            [UsedImplicitly]
            private static void Postfix(Character __instance, HitData hit) {
                var player = Player.m_localPlayer;
                if (__instance != player || player.IsDead() || hit == null || hit.m_damage.m_fire <= 0f) {
                    return;
                }

                var staminaPerTrigger = player.GetTotalActiveMagicEffectValue(MagicEffectType.Kindling);
                if (staminaPerTrigger <= 0f) {
                    return;
                }

                var damagePerTrigger = GetDamagePerTrigger();
                _accumulatedFireDamage += hit.m_damage.m_fire;
                if (_accumulatedFireDamage < damagePerTrigger) {
                    return;
                }

                var triggers = (int)(_accumulatedFireDamage / damagePerTrigger);
                _accumulatedFireDamage -= triggers * damagePerTrigger;
                player.AddStamina(triggers * staminaPerTrigger);
            }
        }
    }
}
