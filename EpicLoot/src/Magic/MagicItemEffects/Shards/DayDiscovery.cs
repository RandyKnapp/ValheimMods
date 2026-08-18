using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to the minimap exploration radius during the day
    public static class DayDiscovery {
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.Explore), typeof(Vector3), typeof(float))]
        private static class Explore_Patch {
            [UsedImplicitly]
            private static void Prefix(ref float radius) {
                if (Player.m_localPlayer == null || !EnvMan.IsDay()) {
                    return;
                }

                var bonus = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.DayDiscovery, 0.01f);
                if (bonus != 0f) {
                    radius *= 1f + bonus;
                }
            }
        }
    }
}
