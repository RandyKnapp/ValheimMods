using EpicLoot.General;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public static class ModifyDamage
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
    private class ModifyDamage_ItemData_GetDamage_Patch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (RunGetDamagePatch(__instance))
            {
                ApplyMagicDamageModifiers(Player.m_localPlayer, __instance, ref __result);
            }
        }
    }

    /// <summary>
    /// Helper method to determine calculation for tooltip.
    /// </summary>
    public static bool RunGetDamagePatch(ItemDrop.ItemData item)
    {
        if (Player.m_localPlayer == null || !Player.m_localPlayer.IsItemEquiped(item))
        {
            return false;
        }

        return true;
    }

    public static HitData.DamageTypes GetDamageWithMagicEffects(ItemDrop.ItemData item)
    {
        HitData.DamageTypes damage = item.GetDamage();
        if (!RunGetDamagePatch(item))
        {
            ApplyMagicDamageModifiers(null, item, ref damage);
        }

        return damage;
    }

    /// <summary>
    /// Applys all magic effects that modify damage. Leave player blank to always calculate values for the item (for tooltip).
    /// </summary>
    private static void ApplyMagicDamageModifiers(Player player, ItemDrop.ItemData item, ref HitData.DamageTypes damages)
    {
        float originalTotalDamage = damages.EpicLootGetTotalDamage();
        float modifyAll = 1f;

        if (player != null)
        {
            if (player.HasActiveMagicEffect(MagicEffectType.CoinHoarder, out float coinHoarderEffectValue))
            {
                modifyAll += CoinHoarder.GetCoinHoarderValue(player, coinHoarderEffectValue);
            }

            if (player.GetSEMan().HaveStatusEffect(EpicAssets.DodgeBuff_SE_Name.GetStableHashCode()) &&
                player.HasActiveMagicEffect(MagicEffectType.DodgeBuff, out float dodgeBuffValue, 0.01f))
            {
                modifyAll += dodgeBuffValue;
            }
        }

        if (!item.IsMagic())
        {
            damages.Modify(modifyAll);
            return;
        }

        float totalDamage = damages.EpicLootGetTotalDamage();
        Skills.SkillType magicItemskillType = item.m_shared.m_skillType;

        // Add damages first
        damages.m_blunt += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
            player, item, MagicEffectType.AddBluntDamage, 0.01f);
        damages.m_slash += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
            player, item, MagicEffectType.AddSlashingDamage, 0.01f);
        damages.m_pierce += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
            player, item, MagicEffectType.AddPiercingDamage, 0.01f);
        damages.m_fire += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
            player, item, MagicEffectType.AddFireDamage, 0.01f);
        damages.m_frost += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
            player, item, MagicEffectType.AddFrostDamage, 0.01f);
        damages.m_lightning += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
            player, item, MagicEffectType.AddLightningDamage, 0.01f);
        damages.m_poison += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
            player, item, MagicEffectType.AddPoisonDamage, 0.01f);
        damages.m_spirit += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
            player, item, MagicEffectType.AddSpiritDamage, 0.01f);

        if (magicItemskillType == Skills.SkillType.Axes)
        {
            damages.m_chop += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, item, MagicEffectType.AddSlashingDamage, 0.01f);
        }
        else if (magicItemskillType == Skills.SkillType.Pickaxes)
        {
            damages.m_pickaxe += totalDamage * MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                player, item, MagicEffectType.AddPiercingDamage, 0.01f);
        }

        // Then modify

        if (MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, item,
            MagicEffectType.ModifyPhysicalDamage, out float physicalDamageEffectValue, 0.01f))
        {
            ModifyPhysicalDamage(ref damages, 1.0f + physicalDamageEffectValue);
        }

        if (MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, item,
            MagicEffectType.ModifyElementalDamage, out float elementalDamageEffectValue, 0.01f))
        {
            ModifyElementalDamage(ref damages, 1.0f + elementalDamageEffectValue);
        }

        if (MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, item,
            MagicEffectType.SpellSword, out float damageEffectValue, 0.01f))
        {
            modifyAll += damageEffectValue;
        }

        ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyDamage, effect =>
        {
            if (MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, item, effect, out float effectValue, 0.01f))
            {
                modifyAll += effectValue;
            }
        });

        if (player != null && player.GetSEMan().HaveStatusEffect("BerserkerStatusEffect".GetStableHashCode()))
        {
            modifyAll += Mathf.Lerp(2.0f, 0.5f, player.GetHealthPercentage());
        }

        // Modify all only once
        damages.Modify(modifyAll);
    }

    private static void ModifyPhysicalDamage(ref HitData.DamageTypes damages, float modifier)
    {
        damages.m_blunt *= modifier;
        damages.m_slash *= modifier;
        damages.m_pierce *= modifier;
        damages.m_chop *= modifier;
        damages.m_pickaxe *= modifier;
    }

    private static void ModifyElementalDamage(ref HitData.DamageTypes damages, float modifier)
    {
        damages.m_fire *= modifier;
        damages.m_frost *= modifier;
        damages.m_lightning *= modifier;
        damages.m_poison *= modifier;
        damages.m_spirit *= modifier;
    }
}
