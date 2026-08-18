using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides some adrenaline based on the amount of eitr used.
    public static class EitrUseGivesAdrenaline {
        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class UseEitr_Patch {
            [UsedImplicitly]
            private static void Postfix(Player __instance, float v) {
                if (v <= 0f || __instance != Player.m_localPlayer) {
                    return;
                }

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.EitrUseGivesAdrenaline, 0.01f);
                if (fraction > 0f) {
                    __instance.AddAdrenaline(v * fraction);
                }
            }
        }
    }
}
