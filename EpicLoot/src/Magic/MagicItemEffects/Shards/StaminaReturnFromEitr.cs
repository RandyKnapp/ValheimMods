using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a return of stamina based on eitr used
    public static class StaminaReturnFromEitr {
        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class UseEitr_Patch {
            [UsedImplicitly]
            private static void Postfix(Player __instance, float v) {
                if (v <= 0f || __instance != Player.m_localPlayer) {
                    return;
                }

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.StaminaReturnFromEitr, 0.01f);
                if (fraction > 0f) {
                    __instance.AddStamina(v * fraction);
                }
            }
        }
    }
}
