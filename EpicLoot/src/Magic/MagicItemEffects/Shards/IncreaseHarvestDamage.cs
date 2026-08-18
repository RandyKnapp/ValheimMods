using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to harvest damage (chop and pickaxe)
    public static class IncreaseHarvestDamage {
        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result) {
            var player = Player.m_localPlayer;
            if (player == null || !player.IsItemEquiped(__instance)) {
                return;
            }

            var bonus = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, __instance, MagicEffectType.IncreaseHarvestDamage, 0.01f);
            if (bonus != 0f) {
                __result.m_chop *= 1f + bonus;
                __result.m_pickaxe *= 1f + bonus;
            }
        }
    }
}
