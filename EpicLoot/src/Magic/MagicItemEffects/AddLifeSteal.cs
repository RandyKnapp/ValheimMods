using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects
{
    public static class AddLifeSteal
    {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void CheckAndDoLifeSteal(HitData hit, Character attacker)
        {
            if (attacker == null || attacker is not Player player)
            {
                return;
            }

            // A plain weapon still lifesteals from set bonuses (Hel's Court is armor only); the other hand's weapon is
            // left out of the total below either way.
            ItemDrop.ItemData weapon = MagicEffectsHelper.GetActiveWeapon(player);
            if (weapon == null)
            {
                return;
            }

            var lifeStealMultiplier = 0f;
            ModifyWithLowHealth.Apply(player, MagicEffectType.LifeSteal, effect =>
                lifeStealMultiplier += MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, weapon, effect, 0.01f));

            if (lifeStealMultiplier == 0)
            {
                return;
            }

            var healOn = hit.m_damage.EpicLootGetTotalDamage() * lifeStealMultiplier;

            attacker.Heal(healOn);
        }
    }
}