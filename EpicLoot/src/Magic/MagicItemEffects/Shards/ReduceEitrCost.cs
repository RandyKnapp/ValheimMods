using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class ReduceEitrCost {
        // What this effect leaves of an eitr cost. Other characters' costs are not reduced here.
        public static float GetCostShare(Character character) {
            return character == Player.m_localPlayer
                ? ModifyAttackCosts.GetEffectPercentage(
                    Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.ReduceEitrCost, 0.01f))
                : 1f;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class Player_UseEitr_Patch {
            [UsedImplicitly]
            private static void Prefix(Player __instance, ref float v) {
                if (v > 0f) {
                    v *= GetCostShare(__instance);
                }
            }
        }
    }
}
