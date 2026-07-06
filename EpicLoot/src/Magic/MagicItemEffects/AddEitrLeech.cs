using EpicLoot.General;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public static class AddEitrLeech
    {
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        public static class AddEitrLeech_Character_Damage_Patch
        {
            public static void Postfix(HitData hit)
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

                if (MagicEffectsHelper.HasActiveMagicEffectOnWeapon(
                    player, weapon, MagicEffectType.EitrLeech, out float eitrleechMultiplier, 0.01f))
                {
                    player.AddEitr(hit.m_damage.EpicLootGetTotalDamage() * eitrleechMultiplier);
                }
            }
        }
    }
}