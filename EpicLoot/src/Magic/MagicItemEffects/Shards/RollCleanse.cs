using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Applies a cleanse effect to the local player when they roll. The duration of the cleanse is determined by the total active value of the RollCleanse magic effect.
    public static class RollCleanse {
        // Rising-edge tracker for the local player's dodge animation, so the cleanse fires once when a
        // roll begins rather than every frame the dodge animation is playing.
        private static bool _wasInDodge;

        [HarmonyPatch(typeof(Player), nameof(Player.UpdateDodge))]
        private static class UpdateDodge_Patch {
            [UsedImplicitly]
            private static void Postfix(Player __instance) {
                if (__instance != Player.m_localPlayer) {
                    return;
                }

                var inDodge = __instance.m_inDodge;
                var rollStarted = inDodge && !_wasInDodge;
                _wasInDodge = inDodge;

                if (rollStarted) {
                    ApplyCleanse(__instance);
                }
            }
        }

        private static void ApplyCleanse(Player player) {
            var seconds = player.GetTotalActiveMagicEffectValue(MagicEffectType.RollCleanse);
            if (seconds <= 0f) {
                return;
            }

            foreach (var se in player.GetSEMan().GetStatusEffects()) {
                if ((se is SE_Poison || se is SE_Burning) && se.m_ttl > 0f) {
                    // Advance elapsed time toward the effect's TTL. This shortens the remaining duration
                    // (and drops the unspent DoT damage); SEMan removes it once m_time passes m_ttl.
                    se.m_time += seconds;
                }
            }
        }
    }
}
