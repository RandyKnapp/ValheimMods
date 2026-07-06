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

        [HarmonyPriority(Priority.HigherThanNormal)]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr))]
        public class Spellsword_Attack_GetAttackEitr_Patch
        {
            public static void Postfix(Attack __instance, ref float __result)
            {
                if (__instance.m_character == Player.m_localPlayer &&
                    MagicEffectsHelper.HasActiveMagicEffectOnWeapon(
                        Player.m_localPlayer, __instance.m_weapon, MagicEffectType.SpellSword, out float effectValue))
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
