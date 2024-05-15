using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    //public HitData.DamageTypes GetDamage(int quality)
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
    public class ModifyDamage_ItemData_GetDamage_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (!__instance.IsMagic())
            {
                return;
            }

            float totalDamage = 0;
            totalDamage += __result.m_damage;
            totalDamage += __result.m_blunt;
            totalDamage += __result.m_slash;
            totalDamage += __result.m_pierce;
            totalDamage += __result.m_fire;
            totalDamage += __result.m_frost;
            totalDamage += __result.m_lightning;
            totalDamage += __result.m_poison;
            // not adding spirit, chop and pickaxe. Vanilla weapons get those on top of their tier appropriate values
            totalDamage *= 0.01f; //percentage of the total damage

            var magicItemskillType = __instance.m_shared.m_skillType;

            var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);

            // Add damages first
            __result.m_blunt        += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.AddBluntDamage);
            __result.m_slash        += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.AddSlashingDamage);
            __result.m_pierce       += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.AddPiercingDamage);
            __result.m_fire         += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.AddFireDamage);
            __result.m_frost        += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.AddFrostDamage);
            __result.m_lightning    += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.AddLightningDamage);
            __result.m_poison       += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.AddPoisonDamage);
            __result.m_spirit       += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.AddSpiritDamage);
            
            if (magicItemskillType == Skills.SkillType.Axes)
            {
                __result.m_chop += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.AddSlashingDamage);
            }
            else if (magicItemskillType == Skills.SkillType.Pickaxes)
            {
                __result.m_pickaxe += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.AddPiercingDamage);
            }

            // Then modify
            if (MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, __instance, MagicEffectType.ModifyPhysicalDamage))
            {
                var totalDamageMod = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.ModifyPhysicalDamage, 0.01f);
                var modifier = 1.0f + totalDamageMod;

                __result.m_blunt *= modifier;
                __result.m_slash *= modifier;
                __result.m_pierce *= modifier;
                __result.m_chop *= modifier;
                __result.m_pickaxe *= modifier;
            }

            if (MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, __instance, MagicEffectType.ModifyElementalDamage))
            {
                var totalDamageMod = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, MagicEffectType.ModifyElementalDamage, 0.01f);
                var modifier = 1.0f + totalDamageMod;

                __result.m_fire *= modifier;
                __result.m_frost *= modifier;
                __result.m_lightning *= modifier;
                __result.m_poison *= modifier;
                __result.m_spirit *= modifier;
            }

            var damageMod = 0f;
            ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyDamage, effect =>
            {
                if (MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, __instance, effect))
                {
                    damageMod += MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, __instance, effect, 0.01f);
                }
            });
            __result.Modify(1.0f + damageMod);

            if (player != null && player.GetSEMan().HaveStatusEffect("BerserkerStatusEffect".GetStableHashCode()))
            {
                var percentLife = player.GetHealthPercentage();
                var berserkerMod = Mathf.Lerp(2.0f, 0.5f, percentLife);
                __result.Modify(1.0f + berserkerMod);
            }
        }
    }
}
