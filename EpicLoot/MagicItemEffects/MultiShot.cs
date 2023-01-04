using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch]
    public static class MultiShot
    {
        [HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
        class FireProjectileBurstPatch
        {
            public static void Prefix(Attack __instance)
            {
                if (__instance == null)
                    return;
                
                var player = (Player)__instance.m_character;

                if (__instance.GetWeapon() == null)
                    return;

                var weaponDamage = __instance.GetWeapon()?.GetDamage();
                var ItemType = __instance.GetWeapon()?.m_shared.m_itemType;
                var SkillType = __instance.GetWeapon()?.m_shared.m_skillType;

                if (player.HasActiveMagicEffect(MagicEffectType.DoubleMagicShot) && weaponDamage.HasValue && ItemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon && SkillType == Skills.SkillType.ElementalMagic)
                {
                    __instance.m_projectiles = 2;
                }
                else if (player.HasActiveMagicEffect(MagicEffectType.TripleBowShot) && weaponDamage.HasValue && ItemType == ItemDrop.ItemData.ItemType.Bow && SkillType == Skills.SkillType.Bows)
                {
                    __instance.m_projectiles = 3;
                }
            }
        }
    }
}
