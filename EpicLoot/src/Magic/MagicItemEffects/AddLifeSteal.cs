using EpicLoot.General;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public static class AddLifeSteal
    {
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        public static class AddLifeSteal_Character_Damage_Patch
        {
            public static void Postfix(HitData hit)
            {
                CheckAndDoLifeSteal(hit);
            }
        }
        
        public static void CheckAndDoLifeSteal(HitData hit)
        {
            var attacker = hit.GetAttacker();
            if (attacker == null || attacker is not Player player)
            {
                return;
            }

            ItemDrop.ItemData weapon;
            if (Attack_Patch.ActiveAttack != null && Attack_Patch.ActiveAttack.m_weapon != null)
            {
                weapon = Attack_Patch.ActiveAttack.m_weapon;
            }
            else
            {
                weapon = player.GetCurrentWeapon();
            }

            if (weapon == null || !weapon.IsMagic())
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