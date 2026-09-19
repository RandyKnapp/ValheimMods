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
                if (__instance.m_character is Player && WeaponHasBloodlust(__instance.m_weapon))
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

                if (__instance.m_character is Player && WeaponHasBloodlust(__instance.m_weapon))
                {
                    __instance.m_attackHealth = GetBloodlustHealth(__instance.m_attackHealth, __instance.m_attackStamina);
                }
            }

            public static void Postfix(Attack __instance, ref float __state)
            {
                __instance.m_attackHealth = __state;
            }
        }

        /// <summary>
        /// Bloodlust is a property of the WEAPON being swung -- it rewrites that attack's stamina cost into a
        /// health cost -- so it must be read off the weapon, the way Throwable and ChainLightning read theirs.
        /// <see cref="MagicEffectsHelper.HasActiveMagicEffectOnWeapon"/> does NOT do that despite its name and
        /// its weapon argument: it sums the effect across every equipped magic item plus active set bonuses
        /// and subtracts only the OFF-HAND weapon, so a single Bloodlust source anywhere in the loadout made
        /// every weapon cost health. Reading the weapon also realigns behaviour with the two places that
        /// already use per-item semantics -- the tooltip (MagicTooltipWeapon.AddAttackStaminaUse) and
        /// MagicItemEffectDefinition's ItemUsesHealthOnAttack requirement -- which otherwise disagreed with
        /// what the game actually charged.
        /// </summary>
        private static bool WeaponHasBloodlust(ItemDrop.ItemData weapon)
        {
            return weapon != null && weapon.IsMagic(out MagicItem magicItem) &&
                magicItem.HasEffect(MagicEffectType.Bloodlust, includeSocketed: true);
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
