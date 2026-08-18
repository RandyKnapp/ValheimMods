using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a stamina restoration effect based on fire damage taken, uses an accumulated threshold.
    public static class Kindling {
        // Fire damage taken per stamina trigger. Tunable; higher = a slower trickle.
        private const float FireDamagePerTrigger = 75f;

        // Tooltip: "Restore {0} Stamina per {1} Fire Damage Taken" -- {1} is the FireDamagePerTrigger const
        // so the shown threshold stays in sync with the code rather than a baked-in literal.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Kindling,
                value => new object[] { value, FireDamagePerTrigger });
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

                _accumulatedFireDamage += hit.m_damage.m_fire;
                if (_accumulatedFireDamage < FireDamagePerTrigger) {
                    return;
                }

                var triggers = (int)(_accumulatedFireDamage / FireDamagePerTrigger);
                _accumulatedFireDamage -= triggers * FireDamagePerTrigger;
                player.AddStamina(triggers * staminaPerTrigger);
            }
        }
    }
}
