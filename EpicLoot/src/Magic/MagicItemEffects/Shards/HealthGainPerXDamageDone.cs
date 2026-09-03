using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a heal to the player for every X damage dealt, cumulative across hits
    public static class HealthGainPerXDamageDone {
        // Threshold of damage dealt before the player is healed. Tunable as "DamagePerTrigger" in this
        // effect's Config block in config/shardstones.json.
        public const float DefaultDamagePerTrigger = 200f;

        private const string DamagePerTriggerKey = "DamagePerTrigger";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { DamagePerTriggerKey, DefaultDamagePerTrigger },
        };

        // Floored at 1 because the payout divides by this: a configured 0 would turn a single hit into an
        // unbounded number of triggers.
        private static float GetDamagePerTrigger() {
            return Mathf.Max(1f, EffectConfig.Get(MagicEffectType.HealthGainPerXDamageDone,
                DamagePerTriggerKey, DefaultDamagePerTrigger));
        }

        // Tooltip: "Heal {0} per {1} Damage Dealt" -- {1} is the configured threshold, so the shown number
        // follows a retune instead of staying at the baked-in default.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.HealthGainPerXDamageDone,
                value => new object[] { value, GetDamagePerTrigger() });
        }

        // Cumulative damage the local player has dealt with the effect active but not yet paid out as a
        // heal. Carries the sub-threshold remainder across hits.
        private static float _accumulatedDamage;

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(HitData hit, Character attacker) {
            if (!(attacker is Player player) || player != Player.m_localPlayer) {
                return;
            }

            // The shard is socketed into the attacking weapon, so read its per-weapon value.
            var weapon = MagicEffectsHelper.GetActiveWeapon(player);
            if (weapon == null || !weapon.IsMagic()) {
                return;
            }

            float healthPerTrigger = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, weapon, MagicEffectType.HealthGainPerXDamageDone);
            if (healthPerTrigger <= 0f) {
                return;
            }

            var damagePerTrigger = GetDamagePerTrigger();
            _accumulatedDamage += hit.m_damage.EpicLootGetTotalDamage();
            if (_accumulatedDamage < damagePerTrigger) {
                return;
            }

            int triggers = (int)(_accumulatedDamage / damagePerTrigger);
            _accumulatedDamage -= triggers * damagePerTrigger;
            player.Heal(triggers * healthPerTrigger);
        }
    }
}
