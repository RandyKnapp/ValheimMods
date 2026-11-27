using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class Kindling {
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

        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Kindling,
                value => new object[] { value, GetDamagePerTrigger() });
        }

        // Carries the sub-threshold remainder across burn ticks.
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
