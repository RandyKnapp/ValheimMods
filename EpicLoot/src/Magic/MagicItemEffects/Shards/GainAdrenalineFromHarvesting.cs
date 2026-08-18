using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides adrenaline when harvesting trees and rocks.
    // TODO: consider supporting destructibles (small rocks/trees/bushes)
    public static class GainAdrenalineFromHarvesting {
        private static void OnHarvestHit(HitData hit) {
            if (hit == null || hit.GetAttacker() != Player.m_localPlayer) {
                return;
            }

            var value = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.GainAdrenalineFromHarvesting);
            if (value > 0f) {
                Player.m_localPlayer.AddAdrenaline(value);
            }
        }

        [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Damage))]
        private static class TreeBase_Damage_Patch {
            [UsedImplicitly]
            private static void Prefix(HitData hit) => OnHarvestHit(hit);
        }

        [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Damage))]
        private static class TreeLog_Damage_Patch {
            [UsedImplicitly]
            private static void Prefix(HitData hit) => OnHarvestHit(hit);
        }

        [HarmonyPatch(typeof(MineRock), nameof(MineRock.Damage))]
        private static class MineRock_Damage_Patch {
            [UsedImplicitly]
            private static void Prefix(HitData hit) => OnHarvestHit(hit);
        }

        [HarmonyPatch(typeof(MineRock5), nameof(MineRock5.Damage))]
        private static class MineRock5_Damage_Patch {
            [UsedImplicitly]
            private static void Prefix(HitData hit) => OnHarvestHit(hit);
        }
    }
}
