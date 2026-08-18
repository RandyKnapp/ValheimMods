using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to Eitr when sacrificing health.
    public static class GainEitrWhenSacrificingHealth {
        [HarmonyPatch(typeof(Character), nameof(Character.UseHealth))]
        private static class UseHealth_Patch {
            [UsedImplicitly]
            private static void Postfix(Character __instance, float hp) {
                var player = Player.m_localPlayer;
                if (hp <= 0f || __instance != player || player.GetMaxEitr() <= 0f) {
                    return;
                }

                var fraction = player.GetTotalActiveMagicEffectValue(
                    MagicEffectType.GainEitrWhenSacrificingHealth, 0.01f);
                if (fraction > 0f) {
                    player.AddEitr(hp * fraction);
                }
            }
        }
    }
}
