using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to max carry weight based on the player's comfort level when rested.
    public static class GainMaxCarryWeightFromRested {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyMaxCarryWeight))]
        private static class ModifyMaxCarryWeight_Patch {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float limit) {
                var player = Player.m_localPlayer;
                if (__instance.m_character != player || !__instance.HaveStatusEffect(SEMan.s_statusEffectRested)) {
                    return;
                }

                var comfortLevel = player.GetComfortLevel();
                if (comfortLevel <= 0) {
                    return;
                }

                limit += player.GetTotalActiveMagicEffectValue(MagicEffectType.GainMaxCarryWeightFromRested) * comfortLevel;
            }
        }
    }
}
