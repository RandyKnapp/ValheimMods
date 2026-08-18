using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Reduces the Eitr cost of using abilities by a percent
    public static class ReduceEitrCost {
        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class Player_UseEitr_Patch {
            [UsedImplicitly]
            private static void Prefix(Player __instance, ref float v) {
                if (v <= 0f || __instance != Player.m_localPlayer) {
                    return;
                }

                var reduction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.ReduceEitrCost, 0.01f);
                if (reduction > 0f) {
                    v *= Mathf.Max(0f, 1f - reduction);
                }
            }
        }
    }
}
