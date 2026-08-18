using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // +% movement speed while the player is on fire
    public static class BurningSpeed {
        private static bool IsBurning(SEMan seman) {
            var effects = seman.GetStatusEffects();
            for (var i = 0; i < effects.Count; i++) {
                if (effects[i] is SE_Burning) {
                    return true;
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ApplyStatusEffectSpeedMods))]
        private static class ApplyStatusEffectSpeedMods_Patch {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float speed) {
                if (__instance.m_character != Player.m_localPlayer || !IsBurning(__instance)) {
                    return;
                }

                var bonus = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.BurningSpeed, 0.01f);
                if (bonus != 0f) {
                    speed *= 1f + bonus;
                }
            }
        }
    }
}
