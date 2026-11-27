using EpicLoot.General;
using EpicLoot.MagicItemEffects.Shards;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public static class ModifyDamage
{
    // The one GetDamage patch: the core magic modifiers, then the shard weapon modifiers, all gated on the
    // local player having the weapon equipped (RunGetDamagePatch), which also drives the weapon tooltip.
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
    private class ModifyDamage_ItemData_GetDamage_Patch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (RunGetDamagePatch(__instance))
            {
                ApplyEquippedWeaponModifiers(__instance, ref __result);
            }
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    private class ModifyDamage_Projectile_Setup_Patch
    {
        private static void Postfix(Projectile __instance, Character owner, HitData hitData, ItemDrop.ItemData item)
        {
            ApplyToSpawnedDamage(__instance, owner, hitData, item, ref __instance.m_damage);
        }
    }

    [HarmonyPatch(typeof(Aoe), nameof(Aoe.Setup))]
    private class ModifyDamage_Aoe_Setup_Patch
    {
        private static void Postfix(Aoe __instance, Character owner, HitData hitData, ItemDrop.ItemData item)
        {
            ApplyToSpawnedDamage(__instance, owner, hitData, item, ref __instance.m_damage);
        }
    }

    // A prefab that references itself (an Aoe chain's m_chainObj, a projectile that spawns itself on hit) points at
    // the live copy once instantiated, so the next spawn clones that copy, its already-scaled damage included. The
    // marker is cloned with it, so the clone is not scaled again.
    private class StaffDamageApplied : MonoBehaviour
    {
    }

    private static void ApplyToSpawnedDamage(MonoBehaviour spawned, Character owner, HitData hitData,
        ItemDrop.ItemData item, ref HitData.DamageTypes damage)
    {
        if (StaffRoles.CarriesOwnDamage(owner, hitData, item) && RunGetDamagePatch(item) &&
            spawned.GetComponent<StaffDamageApplied>() == null)
        {
            ApplyEquippedWeaponModifiers(item, ref damage);
            spawned.gameObject.AddComponent<StaffDamageApplied>();
        }
    }

    private const float PoisonPerPierce = 3f;
    private const float PoisonPerSlash = 2f;
    private const float LightningRollMax = 2f;

    // Valheim reads the weapon's damage once per target in a melee or area attack and once per projectile volley,
    // so a roll made while one of those runs lands on that hit alone. Anywhere else (tooltips, a staff's spawned
    // explosion) the imbue counts at its average.
    private static int _attacksRunning;

    private static float LightningRoll()
    {
        return _attacksRunning > 0 ? Random.Range(0f, LightningRollMax) : 1f;
    }

    [HarmonyPatch]
    private static class Attack_DamageRead_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Attack), nameof(Attack.DoMeleeAttack));
            yield return AccessTools.Method(typeof(Attack), nameof(Attack.DoAreaAttack));
            yield return AccessTools.Method(typeof(Attack), nameof(Attack.FireProjectileBurst));
        }

        private static void Prefix()
        {
            _attacksRunning++;
        }

        private static void Finalizer()
        {
            _attacksRunning--;
        }
    }

    private static void ApplyEquippedWeaponModifiers(ItemDrop.ItemData item, ref HitData.DamageTypes damage)
    {
        Player player = Player.m_localPlayer;
        ApplyMagicDamageModifiers(player, item, ref damage, CoinHoarder.GetDamageBonus(player));
        ApplyShardWeaponModifiers(item, ref damage);
    }

    private static void ApplyShardWeaponModifiers(ItemDrop.ItemData item, ref HitData.DamageTypes damage)
    {
        IncreaseDamageDuringDaytime.ModifyWeaponDamage(item, ref damage);
        IncreaseDamageDuringNighttime.ModifyWeaponDamage(item, ref damage);
        DamageBonusFromPlayerWeight.ModifyWeaponDamage(item, ref damage);
        DamageIncreaseFromMovementPenalty.ModifyWeaponDamage(item, ref damage);
        IncreaseHarvestDamage.ModifyWeaponDamage(item, ref damage);
        ConvertPhysicalDamageToLightning.ModifyWeaponDamage(item, ref damage);
        CoilingVenom.ModifyWeaponDamage(ref damage);
        IcyWeight.ModifyWeaponDamage(item, ref damage);
        ReduceArmorIncreaseDamage.ModifyWeaponDamage(item, ref damage);
        NecroticFire.ModifyWeaponDamage(item, ref damage);
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
            // Not equipped, so GetDamage left it alone. The coin bonus goes in too: it applies to whichever
            // weapon the player swings, so the tooltip shows what this one deals once equipped.
            ApplyMagicDamageModifiers(null, item, ref damage, CoinHoarder.GetDamageBonus(Player.m_localPlayer));
        }

        return damage;
    }

    /// <summary>
    /// Applys all magic effects that modify damage. Leave player blank to always calculate values for the item (for tooltip).
    /// <paramref name="coinHoarderBonus"/> is added to the all-damage multiplier, whether or not a player is given.
    /// </summary>
    private static void ApplyMagicDamageModifiers(Player player, ItemDrop.ItemData item, ref HitData.DamageTypes damages,
        float coinHoarderBonus)
    {
        float originalTotalDamage = damages.EpicLootGetTotalDamage();
        float modifyAll = 1f + coinHoarderBonus;

        if (player != null)
        {
            if (player.GetSEMan().HaveStatusEffect(EpicAssets.DodgeBuff_SE_Name.GetStableHashCode()) &&
                player.HasActiveMagicEffect(MagicEffectType.DodgeBuff, out float dodgeBuffValue, 0.01f))
            {
                modifyAll += dodgeBuffValue;
            }
        }
        
        GiantsBlood.ApplyPhysicalDamage(player, ref damages);

        if (!item.IsMagic())
        {
            damages.Modify(modifyAll);
            return;
        }

        float totalDamage = damages.EpicLootGetTotalDamage();
        Skills.SkillType magicItemskillType = item.m_shared.m_skillType;
        HitData.DamageTypes unenchanted = damages;
        float Value(string effectType) =>
            MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, item, effectType, 0.01f);

        // Each physical type rises only by its own amount; an axe's chop and a pickaxe's mining damage rise with it.
        float blunt = Value(MagicEffectType.AddBluntDamage);
        float slash = Value(MagicEffectType.AddSlashingDamage);
        float pierce = Value(MagicEffectType.AddPiercingDamage);
        damages.m_blunt += unenchanted.m_blunt * blunt;
        damages.m_slash += unenchanted.m_slash * slash;
        damages.m_pierce += unenchanted.m_pierce * pierce;
        if (magicItemskillType == Skills.SkillType.Axes)
        {
            damages.m_chop += unenchanted.m_chop * slash;
        }
        else if (magicItemskillType == Skills.SkillType.Pickaxes)
        {
            damages.m_pickaxe += unenchanted.m_pickaxe * pierce;
        }

        damages.m_fire += totalDamage * Value(MagicEffectType.AddFireDamage);
        damages.m_frost += totalDamage * Value(MagicEffectType.AddFrostDamage);
        damages.m_lightning += totalDamage * Value(MagicEffectType.AddLightningDamage) * LightningRoll();
        damages.m_poison += (PoisonPerPierce * unenchanted.m_pierce + PoisonPerSlash * unenchanted.m_slash +
            unenchanted.m_blunt) * Value(MagicEffectType.AddPoisonDamage);
        // Last, so it also feeds on what the imbues above added.
        damages.m_spirit += (damages.m_fire + damages.m_frost + damages.m_lightning + damages.m_poison) *
            Value(MagicEffectType.AddSpiritDamage);

        // Added damage too, so the multipliers below scale it.
        EitrInfusion.AddInfusedDamage(player, item, ref damages);

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

        if (Spellsword.IsSpellswordWeapon(item) && MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, item,
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

        damages.Modify(modifyAll);
    }

    internal static void ModifyPhysicalDamage(ref HitData.DamageTypes damages, float modifier)
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
