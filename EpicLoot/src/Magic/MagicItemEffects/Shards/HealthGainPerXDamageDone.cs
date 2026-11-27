using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class HealthGainPerXDamageDone {
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

        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.HealthGainPerXDamageDone,
                value => new object[] { value, GetDamagePerTrigger() });
        }

        // Carries the sub-threshold remainder across hits.
        private static float _accumulatedDamage;

        // Postfix handler invoked by SharedCharacterDamagePatch (on-hit reaction).
        public static void OnDamageDealt(HitData hit, Character attacker, float healthBeforeHit) {
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
            _accumulatedDamage += MagicEffectsHelper.GetCountedDamage(hit, healthBeforeHit);
            if (_accumulatedDamage < damagePerTrigger) {
                return;
            }

            int triggers = (int)(_accumulatedDamage / damagePerTrigger);
            _accumulatedDamage -= triggers * damagePerTrigger;
            player.Heal(triggers * healthPerTrigger);
        }
    }
}
