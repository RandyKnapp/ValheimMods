using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to carry weight at night
    public static class NightCarryWeight {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyMaxCarryWeight))]
        private static class ModifyMaxCarryWeight_Patch {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, float baseLimit, ref float limit) {
                var player = Player.m_localPlayer;
                if (__instance.m_character != player || !EnvMan.IsNight()) {
                    return;
                }

                limit += baseLimit * player.GetTotalActiveMagicEffectValue(MagicEffectType.NightCarryWeight, 0.01f);
            }
        }
    }
}
