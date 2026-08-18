using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to movement speed while in stormy weather
    public static class StormRider {
        // Vanilla EnvSetup names whose weather counts as a storm. Compared case-insensitively against the
        // current environment name (EnvSetup.m_name).
        private static readonly HashSet<string> StormEnvironments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ThunderStorm",
            "SnowStorm",
            "Twilight_SnowStorm",
        };

        public static bool IsStorm() {
            var env = EnvMan.instance?.GetCurrentEnvironment();
            return env != null && StormEnvironments.Contains(env.m_name);
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ApplyStatusEffectSpeedMods))]
        private static class ApplyStatusEffectSpeedMods_Patch {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float speed) {
                if (__instance.m_character != Player.m_localPlayer || !IsStorm()) {
                    return;
                }

                var bonus = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.StormRider, 0.01f);
                if (bonus != 0f) {
                    speed *= 1f + bonus;
                }
            }
        }
    }
}
