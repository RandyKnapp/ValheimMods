using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Lightning damage done by the player restores eitr to the player, at damage thresholds.
    public static class Conduit {
        // Lightning damage dealt per eitr trigger; higher = a slower trickle. Tunable as
        // "DamagePerTrigger" in this effect's Config block in config/shardstones.json.
        public const float DefaultDamagePerTrigger = 200f;

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { DamagePerTriggerKey, DefaultDamagePerTrigger },
        };

        private const string DamagePerTriggerKey = "DamagePerTrigger";

        // Floored at 1 because the payout divides by this: a configured 0 would turn one hit into an
        // unbounded number of triggers.
        private static float GetDamagePerTrigger() {
            return Mathf.Max(1f,
                EffectConfig.Get(MagicEffectType.Conduit, DamagePerTriggerKey, DefaultDamagePerTrigger));
        }

        // Tooltip: "Restore {0} Eitr per {1} Lightning Damage Dealt" -- {1} is the configured threshold, so
        // the shown number follows a retune rather than staying at the baked-in default.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Conduit,
                value => new object[] { value, GetDamagePerTrigger() });
        }

        // Cumulative lightning damage the local player has dealt with the effect active but not yet paid out
        // as eitr. Carries the sub-threshold remainder across hits.
        private static float _accumulatedLightningDamage;

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || hit.m_damage.m_lightning <= 0f || attacker != player
                || __instance == player || __instance.IsPlayer() || __instance.IsTamed()) {
                return;
            }

            var eitrPerTrigger = player.GetTotalActiveMagicEffectValue(MagicEffectType.Conduit);
            if (eitrPerTrigger <= 0f) {
                return;
            }

            var damagePerTrigger = GetDamagePerTrigger();
            _accumulatedLightningDamage += hit.m_damage.m_lightning;
            if (_accumulatedLightningDamage < damagePerTrigger) {
                return;
            }

            var triggers = (int)(_accumulatedLightningDamage / damagePerTrigger);
            _accumulatedLightningDamage -= triggers * damagePerTrigger;
            player.AddEitr(triggers * eitrPerTrigger);
        }
    }
}
