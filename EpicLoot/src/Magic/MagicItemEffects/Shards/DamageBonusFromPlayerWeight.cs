using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to weapon damage based on the player's weight.
    public static class DamageBonusFromPlayerWeight {
        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result) {
            var player = Player.m_localPlayer;
            if (player == null || !player.IsItemEquiped(__instance)) {
                return;
            }

            var pct = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, __instance, MagicEffectType.DamageBonusFromPlayerWeight, 0.01f);
            if (pct == 0f) {
                return;
            }

            var bonus = pct * PenaltyScaling.WeightFactor(player);
            if (bonus != 0f) {
                __result.Modify(1f + bonus);
            }
        }
    }
}
