using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch]
    public static class MultiShot
    {
        [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
        [HarmonyPrefix]
        public static void Attack_FireProjectileBurst_Prefix(Attack __instance)
        {
            if (__instance?.GetWeapon() == null || __instance.m_character == null || !__instance.m_character.IsPlayer())
                return;

            var weaponDamage = __instance.GetWeapon()?.GetDamage();
            if (!weaponDamage.HasValue)
                return;

            var player = (Player)__instance.m_character;
            var doubleShot = player.HasActiveMagicEffect(MagicEffectType.DoubleMagicShot);
            var tripleShot = player.HasActiveMagicEffect(MagicEffectType.TripleBowShot);
            if (!doubleShot && !tripleShot)
                return;

            var itemType = __instance.GetWeapon()?.m_shared.m_itemType;
            var skillType = __instance.GetWeapon()?.m_shared.m_skillType;

            if (doubleShot && itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon && skillType == Skills.SkillType.ElementalMagic)
            {
                // The accuracy on the fireball staff is 1, so the projectiles appear right on top of each other,
                // this forces them to appear distinct and still feels good (greater AOE in lieu of accuracy)
                if (__instance.m_projectileAccuracy < 2)
                    __instance.m_projectileAccuracy = 2;
                __instance.m_projectiles = 2;
            }
            else if (tripleShot && itemType == ItemDrop.ItemData.ItemType.Bow && skillType == Skills.SkillType.Bows)
            {
                __instance.m_projectiles = 3;
            }
        }
    }
}
