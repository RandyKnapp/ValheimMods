using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to sailing speed during the day
    public static class DaySailingSpeed {
        [HarmonyPatch(typeof(Ship), nameof(Ship.GetSailForce))]
        private static class Ship_GetSailForce_Patch {
            [UsedImplicitly]
            private static void Postfix(Ship __instance, ref Vector3 __result) {
                var player = Player.m_localPlayer;
                if (player == null || !EnvMan.IsDay() || !__instance.m_players.Contains(player)) {
                    return;
                }

                var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.DaySailingSpeed, 0.01f);
                if (bonus > 0f) {
                    __result *= 1f + bonus;
                }
            }
        }
    }
}
