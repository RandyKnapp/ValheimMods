using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Consumes Eitr first for any health costs, then consumes health for the remainder.
    public static class ConsumeEitrFirstForBloodCosts {
        [HarmonyPatch(typeof(Character), nameof(Character.UseHealth))]
        private static class UseHealth_Patch {
            [UsedImplicitly]
            private static void Prefix(Character __instance, ref float hp) {
                if (hp <= 0f || __instance != Player.m_localPlayer) {
                    return;
                }

                var player = Player.m_localPlayer;
                var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.ConsumeEitrFirstForBloodCosts, 0.01f);
                if (fraction <= 0f) {
                    return;
                }

                var covered = Mathf.Min(hp * fraction, player.GetEitr());
                if (covered > 0f) {
                    player.UseEitr(covered);
                    hp -= covered;
                }
            }
        }
    }
}
