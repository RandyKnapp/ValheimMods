using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects
{
    public static class AddEitrLeech
    {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(HitData hit, Character attacker)
        {
            if (attacker == null || attacker is not Player player)
            {
                return;
            }

            var weapon = MagicEffectsHelper.GetActiveWeapon(player);
            if (weapon == null || !weapon.IsMagic())
            {
                return;
            }

            if (MagicEffectsHelper.HasActiveMagicEffectOnWeapon(
                player, weapon, MagicEffectType.EitrLeech, out float eitrleechMultiplier, 0.01f))
            {
                player.AddEitr(hit.m_damage.EpicLootGetTotalDamage() * eitrleechMultiplier);
            }
        }
    }
}