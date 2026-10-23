using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects
{
    // Heals the player a flat amount when a hit of theirs kills a creature.
    public static class LifeAfterKill
    {
        private static readonly KillCredit _credit = new KillCredit();

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction, attacker side).
        public static void OnDamageDealt(Character target, HitData hit, Character attacker)
        {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || target == player || attacker != player
                || target.IsPlayer() || target.IsTamed())
            {
                return;
            }

            // The memoized total first: against a target another peer owns, the kill check estimates the whole hit.
            // One heal per kill, however many hits land as the victim dies (KillCredit).
            if (player.GetTotalActiveMagicEffectValue(MagicEffectType.LifeAfterKill) <= 0f
                || !_credit.TryClaim(target, hit))
            {
                return;
            }

            ItemDrop.ItemData weapon = MagicEffectsHelper.GetActiveWeapon(player);
            if (weapon == null)
            {
                return;
            }

            float heal = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, weapon, MagicEffectType.LifeAfterKill);
            if (heal <= 0f)
            {
                return;
            }

            player.Heal(heal);
        }
    }
}
