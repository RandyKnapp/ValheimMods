using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects
{
    public static class AddEitrLeech
    {
        // On-hit reaction, called from SharedCharacterDamagePatch's postfix.
        public static void OnDamageDealt(HitData hit, Character attacker, float healthBeforeHit)
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
                player.AddEitr(MagicEffectsHelper.GetCountedDamage(hit, healthBeforeHit) * eitrleechMultiplier);
            }
        }
    }
}