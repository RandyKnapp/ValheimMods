using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a small heal over time when the player spends Eitr. The heal is triggered every time the player spends a threshold amount of Eitr.
    public static class HealthOnEitrUse {
        // Threshold of Eitr spent before the player is healed.
        private const float EitrPerTrigger = 100f;

        // Tooltip: "Heal {0} per {1} Eitr Spent" -- {1} is the EitrPerTrigger const so the shown threshold
        // stays in sync with the code rather than a baked-in literal.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.HealthOnEitrUse,
                value => new object[] { value, EitrPerTrigger });
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

                _accumulatedEitr += v;
                if (_accumulatedEitr < EitrPerTrigger) {
                    return;
                }

                var triggers = (int)(_accumulatedEitr / EitrPerTrigger);
                _accumulatedEitr -= triggers * EitrPerTrigger;
                __instance.Heal(triggers * healthPerTrigger);
            }
        }
    }
}
