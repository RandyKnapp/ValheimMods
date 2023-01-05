using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
    public static class DoubleMagicShot
    {
        public static void Prefix(Attack __instance)
        {
            if (__instance == null)
                return;

            if (__instance.GetWeapon() == null)
                return;

            var player = (Player)__instance.m_character;

            var weaponDamage = __instance.GetWeapon()?.GetDamage();
            var ItemType = __instance.GetWeapon()?.m_shared.m_itemType;
            var SkillType = __instance.GetWeapon()?.m_shared.m_skillType;

            if (player.HasActiveMagicEffect(MagicEffectType.DoubleMagicShot) && weaponDamage.HasValue && ItemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon && SkillType == Skills.SkillType.ElementalMagic)
            {
                __instance.m_projectiles = 2;
            }
        }
    }
}
