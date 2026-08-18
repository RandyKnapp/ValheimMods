using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to weapon damage during nighttime
    public static class IncreaseDamageDuringNighttime {
        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result) {
            var player = Player.m_localPlayer;
            if (player == null || !EnvMan.IsNight() || !player.IsItemEquiped(__instance)) {
                return;
            }

            var bonus = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, __instance, MagicEffectType.IncreaseDamageDuringNighttime, 0.01f);
            if (bonus != 0f) {
                __result.Modify(1f + bonus);
            }
        }
    }
}
