using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to frost damage based on the player's movement penalty
    public static class IcyWeight {
        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result) {
            var player = Player.m_localPlayer;
            if (player == null || !player.IsItemEquiped(__instance)) {
                return;
            }

            var pct = player.GetTotalActiveMagicEffectValue(MagicEffectType.IcyWeight, 0.01f);
            if (pct <= 0f) {
                return;
            }

            var fraction = pct * PenaltyScaling.MovementPenaltyFactor(player);
            if (fraction <= 0f) {
                return;
            }

            var bonus = __result.EpicLootGetTotalDamage() * fraction;
            if (bonus > 0f) {
                __result.m_frost += bonus;
            }
        }
    }
}
