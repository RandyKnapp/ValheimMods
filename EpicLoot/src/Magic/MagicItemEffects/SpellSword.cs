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
                if (__instance.m_character == Player.m_localPlayer &&
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
                if (character == Player.m_localPlayer &&
                    MagicEffectsHelper.HasActiveMagicEffectOnWeapon(
                        Player.m_localPlayer, weapon, MagicEffectType.SpellSword, out float effectValue))
                {
                    __result += GetAdditionalSpellswordAttackEitr(__instance.m_attackStamina);
                }
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
