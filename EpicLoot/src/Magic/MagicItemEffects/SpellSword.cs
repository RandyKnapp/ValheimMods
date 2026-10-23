using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public static class Spellsword
    {
        [HarmonyPriority(Priority.HigherThanNormal)]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
        public static class Attack_GetAttackStamina_Prefix_Patch_SpellSword
        {
            public static void Postfix(Attack __instance, ref float __result)
            {
                if (__instance.m_character == Player.m_localPlayer && IsSpellswordWeapon(__instance.m_weapon) &&
                    MagicEffectsHelper.HasActiveMagicEffectOnWeapon(
                        Player.m_localPlayer, __instance.m_weapon, MagicEffectType.SpellSword, out float effectValue))
                {
                    __result = GetSpellswordAttackStamina(__result);
                }
            }
        }

        // The Sept 2026 update split GetAttackEitr() into a thin wrapper over
        // GetAttackEitr(Character, ItemData), so an unqualified patch is now ambiguous and throws.
        // Patch the implementation rather than the wrapper: it is what every caller ends up in,
        // including Player.UpdateControllerTriggerFeedback, which calls it directly. Read the
        // arguments instead of m_character/m_weapon - that feedback path passes its own.
        [HarmonyPriority(Priority.HigherThanNormal)]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), typeof(Character), typeof(ItemDrop.ItemData))]
        public class Spellsword_Attack_GetAttackEitr_Patch
        {
            public static void Postfix(Attack __instance, Character character, ItemDrop.ItemData weapon, ref float __result)
            {
                // An Eitr Infusion has already paid the swing's eitr: it waives the extra cost while it lasts.
                if (character == Player.m_localPlayer && IsSpellswordWeapon(weapon) && !EitrInfusion.IsInfused &&
                    MagicEffectsHelper.HasActiveMagicEffectOnWeapon(
                        Player.m_localPlayer, weapon, MagicEffectType.SpellSword, out float effectValue))
                {
                    __result += GetAdditionalSpellswordAttackEitr(__instance.m_attackStamina);
                }
            }
        }

        // The weapons the effect can roll on: melee weapons swung with stamina. As a set bonus it is read from every
        // held weapon, so without this a bow, crossbow or staff would have its costs and damage changed too.
        internal static bool IsSpellswordWeapon(ItemDrop.ItemData weapon)
        {
            if (weapon == null || weapon.m_shared.m_attack.m_bowDraw)
            {
                return false;
            }

            switch (weapon.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                    break;
                default:
                    return false;
            }

            switch (weapon.m_shared.m_skillType)
            {
                case Skills.SkillType.BloodMagic:
                case Skills.SkillType.ElementalMagic:
                case Skills.SkillType.Pickaxes:
                case Skills.SkillType.Bows:
                case Skills.SkillType.Crossbows:
                case Skills.SkillType.Fishing:
                    return false;
                default:
                    return true;
            }
        }

        public static float GetAdditionalSpellswordAttackEitr(float attackStamina)
        {
            return attackStamina / 2;
        }

        public static float GetSpellswordAttackStamina(float attackStamina)
        {
            return attackStamina / 2;
        }
    }
}
