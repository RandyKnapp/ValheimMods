using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public static class Bloodlust
    {
        [HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
        public static class Attack_GetAttackStamina_Prefix_Patch_Bloodlust
        {
            public static bool Prefix(Attack __instance, ref float __result)
            {
                if (AttackHasBloodlust(__instance))
                {
                    __result = GetBloodlustStamina();
                    return false;
                }

                return true;
            }
        }

        [HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackHealth))]
        public class Bloodlust_Attack_GetAttackHealth_Patch
        {
            public static void Prefix(Attack __instance, ref float __state)
            {
                __state = __instance.m_attackHealth;

                if (AttackHasBloodlust(__instance))
                {
                    __instance.m_attackHealth = GetBloodlustHealth(__instance.m_attackHealth, __instance.m_attackStamina);
                }
            }

            public static void Postfix(Attack __instance, ref float __state)
            {
                __instance.m_attackHealth = __state;
            }
        }

        // Read off the weapon itself. MagicEffectsHelper.HasActiveMagicEffectOnWeapon, despite its name, sums the
        // whole loadout, so one Bloodlust anywhere would make every weapon cost health.
        private static bool WeaponHasBloodlust(ItemDrop.ItemData weapon)
        {
            return weapon != null && weapon.IsMagic(out MagicItem magicItem) &&
                magicItem.HasEffect(MagicEffectType.Bloodlust, includeSocketed: true);
        }

        private static bool AttackHasBloodlust(Attack attack)
        {
            return attack.m_character is Player player && HasBloodlust(player, attack.m_weapon, attack);
        }

        // Separate from the Attack so the compendium can ask it of a held weapon's primary attack, whose
        // template has no m_weapon until it is swung.
        internal static bool HasBloodlust(Player player, ItemDrop.ItemData weapon, Attack attack)
        {
            return WeaponHasBloodlust(weapon) || SetGrantsBloodlust(player, weapon, attack);
        }

        // A set bonus belongs to no weapon, so the limits the weapon version gets from its roll requirements are
        // checked here per attack. Staves are left out by skill: their secondary costs stamina alone.
        private static bool SetGrantsBloodlust(Player player, ItemDrop.ItemData weapon, Attack attack)
        {
            if (MagicEffectsHelper.GetTotalActiveSetEffectValue(player, MagicEffectType.Bloodlust) <= 0f)
            {
                return false;
            }

            if (weapon == null || attack.m_attackStamina <= 0f || attack.m_attackEitr > 0f)
            {
                return false;
            }

            Skills.SkillType skill = weapon.m_shared.m_skillType;
            if (skill == Skills.SkillType.Pickaxes || skill == Skills.SkillType.ElementalMagic ||
                skill == Skills.SkillType.BloodMagic)
            {
                return false;
            }

            ItemDrop.ItemData.ItemType type = weapon.m_shared.m_itemType;
            return type == ItemDrop.ItemData.ItemType.OneHandedWeapon ||
                type == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                type == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft;
        }

        public static float GetBloodlustStamina()
        {
            return 0f;
        }

        public static float GetBloodlustHealth(float attackHealth, float attackStamina)
        {
            return attackHealth + attackStamina;
        }
    }
}
