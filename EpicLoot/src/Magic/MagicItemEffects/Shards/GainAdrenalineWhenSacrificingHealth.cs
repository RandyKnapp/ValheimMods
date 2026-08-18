using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to adrenaline based on the amount of health sacrificed.
    public static class GainAdrenalineWhenSacrificingHealth {
        [HarmonyPatch(typeof(Character), nameof(Character.UseHealth))]
        private static class UseHealth_Patch {
            [UsedImplicitly]
            private static void Postfix(Character __instance, float hp) {
                if (hp <= 0f || __instance != Player.m_localPlayer) {
                    return;
                }

                var fraction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.GainAdrenalineWhenSacrificingHealth, 0.01f);
                if (fraction > 0f) {
                    Player.m_localPlayer.AddAdrenaline(hp * fraction);
                }
            }
        }
    }
}
