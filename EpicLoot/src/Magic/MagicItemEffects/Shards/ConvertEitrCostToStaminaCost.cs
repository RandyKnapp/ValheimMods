using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Converts a portion of the eitr cost of using a skill into stamina cost instead.
    public static class ConvertEitrCostToStaminaCost {
        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class UseEitr_Patch {
            [UsedImplicitly]
            private static void Postfix(Player __instance, float v) {
                if (v <= 0f || __instance != Player.m_localPlayer) {
                    return;
                }

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.ConvertEitrCostToStaminaCost, 0.01f);
                if (fraction <= 0f) {
                    return;
                }

                var converted = v * fraction;
                __instance.AddEitr(converted);     // refund the converted portion of the eitr cost...
                __instance.UseStamina(converted);  // ...and pay it from stamina instead.
            }
        }
    }
}
