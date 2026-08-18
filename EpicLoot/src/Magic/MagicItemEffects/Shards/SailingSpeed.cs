using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to sailing speed
    public static class SailingSpeed {
        [HarmonyPatch(typeof(Ship), nameof(Ship.GetSailForce))]
        private static class Ship_GetSailForce_Patch {
            private static void Postfix(Ship __instance, ref Vector3 __result) {
                var player = Player.m_localPlayer;
                if (player == null || !__instance.m_players.Contains(player)) {
                    return;
                }

                float bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.SailingSpeed, 0.01f);
                if (bonus > 0f) {
                    __result *= 1f + bonus;
                }
            }
        }
    }
}
