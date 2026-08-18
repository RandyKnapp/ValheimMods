using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a flat heal to the player when they hit an enemy with a weapon
    public static class LifeGainOnHit {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction, attacker side). Only fires for
        // hits on Characters -- destructibles (trees, rocks) never route through Character.Damage.
        public static void OnDamageDealt(HitData hit, Character attacker) {
            if (!(attacker is Player player) || player != Player.m_localPlayer) {
                return;
            }

            // The shard is socketed into the attacking weapon, so read its per-weapon value.
            var weapon = MagicEffectsHelper.GetActiveWeapon(player);
            if (weapon == null || !weapon.IsMagic()) {
                return;
            }

            var heal = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, weapon, MagicEffectType.LifeGainOnHit);
            if (heal <= 0f) {
                return;
            }

            // Don't pay out on whiffs -- a hit that landed for nothing (fully resisted, immune target)
            // shouldn't heal.
            if (hit.m_damage.EpicLootGetTotalDamage() <= 0f) {
                return;
            }

            player.Heal(heal);
        }
    }
}
