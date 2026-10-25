using EpicLoot.MagicItemEffects;
using EpicLoot.MagicItemEffects.Shards;
using UnityEngine;

namespace EpicLoot;

// The stat lines magic can change. Each is added only when the tooltip would draw it, in drawing order, with
// Modified set when an effect touches it. Lines magic never changes (food, crafter, the swim and sneak
// modifiers, ...) are left to the renderer.
public partial class TooltipGatherer
{
    private void GatherStats()
    {
        GatherWeight();
        GatherDurability();

        switch (item.m_shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
            case ItemDrop.ItemData.ItemType.Bow:
            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
            case ItemDrop.ItemData.ItemType.Torch:
            case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                GatherDamages();
                GatherAttackStaminaUse();
                GatherEitrUse();
                GatherHealthUse();
                GatherHealthUsePercentage();
                GatherDrawStaminaUse();
                GatherBlockArmor();
                GatherBlockForceAndParry();
                GatherKnockback();
                GatherBackstab();
                break;
            case ItemDrop.ItemData.ItemType.Shield:
                GatherBlockArmor();
                GatherBlockForceAndParry();
                break;
            case ItemDrop.ItemData.ItemType.Helmet:
            case ItemDrop.ItemData.ItemType.Chest:
            case ItemDrop.ItemData.ItemType.Legs:
            case ItemDrop.ItemData.ItemType.Shoulder:
                GatherArmor();
                break;
            case ItemDrop.ItemData.ItemType.Ammo:
            case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                GatherDamages();
                GatherKnockback();
                break;
        }

        GatherEquipmentModifiers();
    }

    private TooltipStat AddStat(string id, float value, bool modified, string variant = null)
    {
        TooltipStat stat = new TooltipStat
        {
            Id = id,
            Variant = variant,
            Value = value,
            Modified = modified
        };
        data.Stats.Add(stat);
        return stat;
    }

    private void GatherWeight()
    {
        bool modified = magicItem.HasEffect(MagicEffectType.ReduceWeight) ||
            magicItem.HasEffect(MagicEffectType.Weightless);

        if (item.m_shared.m_maxStackSize > 1)
        {
            // The stack as it is, as vanilla's tooltip shows it, not a full one.
            AddStat("$item_weight", item.GetNonStackedWeight(), modified).Secondary = item.GetWeight();
        }
        else
        {
            AddStat("$item_weight", item.GetWeight(), modified);
        }
    }

    private void GatherDurability()
    {
        // Once Indestructible is live, m_useDurability reads false -- so check the effect first,
        // otherwise the "Indestructible" line only ever shows while the effect is NOT applied.
        bool isIndestructible = magicItem.HasEffect(MagicEffectType.Indestructible, false, true);

        // An indestructible item reports m_useDurability == false, so fall back to what it used
        // before the effect was applied -- items that never had durability still get no line.
        bool usesDurability = isIndestructible
            ? Indestructible.OriginallyUsesDurability(item)
            : item.m_shared.m_useDurability;

        if (!usesDurability)
        {
            return;
        }

        if (isIndestructible)
        {
            AddStat("$item_durability", 0f, true, TooltipStat.Indestructible);
            return;
        }

        TooltipStat stat = AddStat("$item_durability", item.GetDurabilityPercentage() * 100f,
            magicItem.HasEffect(MagicEffectType.ModifyDurability));
        stat.Min = item.m_durability;
        stat.Max = item.GetMaxDurability(qualityLevel);
    }

    private void GatherDamages()
    {
        HitData.DamageTypes damages = ModifyDamage.GetDamageWithMagicEffects(item);

        // Eitr Imbue is applied to the hit itself (attacker-side, paid for with eitr) rather than in
        // GetDamage, so fold its bonus in here -- otherwise a purely physical weapon shows no spirit
        // damage at all despite dealing it on every affordable swing.
        float eitrImbueSpirit = EitrImbueAttack.GetSpiritBonus(magicItem, damages);
        damages.m_spirit += eitrImbueSpirit;

        player.GetSkills().GetRandomSkillRange(out float min, out float max, item.m_shared.m_skillType);

        bool allDamage = magicItem.HasEffect(MagicEffectType.ModifyDamage, true);
        bool physDamage = magicItem.HasEffect(MagicEffectType.ModifyPhysicalDamage);
        bool elemDamage = magicItem.HasEffect(MagicEffectType.ModifyElementalDamage);
        bool coinHoarderDamage = CoinHoarder.GetDamageBonus(player) > 0f;
        bool spellswordDamage = magicItem.HasEffect(MagicEffectType.SpellSword);
        bool allCheck = allDamage || coinHoarderDamage || spellswordDamage;

        AddDamage("$inventory_damage", damages.m_damage, min, max, allCheck);
        AddDamage("$inventory_blunt", damages.m_blunt, min, max,
            allCheck || physDamage || magicItem.HasEffect(MagicEffectType.AddBluntDamage));
        AddDamage("$inventory_slash", damages.m_slash, min, max,
            allCheck || physDamage || magicItem.HasEffect(MagicEffectType.AddSlashingDamage));
        AddDamage("$inventory_pierce", damages.m_pierce, min, max,
            allCheck || physDamage || magicItem.HasEffect(MagicEffectType.AddPiercingDamage));
        AddDamage("$inventory_fire", damages.m_fire, min, max,
            allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddFireDamage));
        AddDamage("$inventory_frost", damages.m_frost, min, max,
            allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddFrostDamage));
        AddDamage("$inventory_lightning", damages.m_lightning, min, max,
            allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddLightningDamage));
        AddDamage("$inventory_poison", damages.m_poison, min, max,
            allCheck || elemDamage || magicItem.HasEffect(MagicEffectType.AddPoisonDamage));
        AddDamage("$inventory_spirit", damages.m_spirit, min, max,
            allCheck || elemDamage || eitrImbueSpirit > 0f || magicItem.HasEffect(MagicEffectType.AddSpiritDamage));
    }

    private void AddDamage(string id, float damage, float minFactor, float maxFactor, bool modified)
    {
        if (damage == 0f)
        {
            return;
        }

        TooltipStat stat = AddStat(id, damage, modified);
        stat.Min = damage * minFactor;
        stat.Max = damage * maxFactor;
    }

    private void GatherAttackStaminaUse()
    {
        if (magicItem.HasEffect(MagicEffectType.Bloodlust))
        {
            AddStat("$item_staminause", Bloodlust.GetBloodlustStamina(), true, TooltipStat.Bloodlust);
            return;
        }

        if (item.m_shared.m_attack.m_attackStamina <= 0f)
        {
            return;
        }

        bool modified = magicItem.HasEffect(MagicEffectType.ModifyAttackStaminaUse) ||
            magicItem.HasEffect(MagicEffectType.ModifyBlockStaminaUse);
        float staminaUsePercentage = 1 - magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackStaminaUse, 0.01f);
        float totalStaminaUse = staminaUsePercentage * item.m_shared.m_attack.m_attackStamina;

        if (magicItem.HasEffect(MagicEffectType.SpellSword))
        {
            totalStaminaUse = Spellsword.GetSpellswordAttackStamina(totalStaminaUse);
        }

        AddStat("$item_staminause", totalStaminaUse, modified);
    }

    private void GatherEitrUse()
    {
        // Conjured Arrows (a set bonus) makes every shot of a bow cost eitr. The figure is the attack's own cost
        // call, so every modifier is already in it.
        if (ConjuredArrows.TryGetShotCost(item, out float conjuredCost))
        {
            AddStat("$item_eitruse", conjuredCost, true);
            return;
        }

        bool hasSpellSword = magicItem.HasEffect(MagicEffectType.SpellSword);

        // Spellsword gives a weapon an eitr cost it would not otherwise have.
        if (item.m_shared.m_attack.m_attackEitr <= 0f && !hasSpellSword)
        {
            return;
        }

        bool modified = magicItem.HasEffect(MagicEffectType.ModifyAttackEitrUse) ||
            magicItem.HasEffect(MagicEffectType.DoubleMagicShot) || hasSpellSword;

        // Same order as the game: Spellsword's extra eitr (half the base stamina cost) is added first,
        // then ModifyAttackEitrUse scales the whole cost.
        float baseEitrUse = item.m_shared.m_attack.m_attackEitr;
        if (hasSpellSword)
        {
            baseEitrUse += Spellsword.GetAdditionalSpellswordAttackEitr(item.m_shared.m_attack.m_attackStamina);
        }

        float eitrUsePercentage = 1 - magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackEitrUse, 0.01f);
        AddStat("$item_eitruse", eitrUsePercentage * baseEitrUse, modified);
    }

    private void GatherHealthUse()
    {
        bool hasBloodlust = magicItem.HasEffect(MagicEffectType.Bloodlust);
        if (item.m_shared.m_attack.m_attackHealth <= 0f && !hasBloodlust)
        {
            return;
        }

        float healthUsage = item.m_shared.m_attack.m_attackHealth;
        if (hasBloodlust)
        {
            healthUsage = Bloodlust.GetBloodlustHealth(healthUsage, item.m_shared.m_attack.m_attackStamina);
        }

        float effectValue = magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackHealthUse, 0.01f);
        AddStat("$item_healthuse", ModifyAttackCosts.GetEffectPercentage(effectValue) * healthUsage,
            magicItem.HasEffect(MagicEffectType.ModifyAttackHealthUse));
    }

    private void GatherHealthUsePercentage()
    {
        if (item.m_shared.m_attack.m_attackHealthPercentage <= 0f)
        {
            return;
        }

        // Already a percentage (40 = 40% of current health). Attack.GetAttackHealth folds this into the same
        // cost ModifyAttackHealthUse scales, so the reduction applies here too.
        float effectValue = magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackHealthUse, 0.01f);
        float healthUsePercentage = ModifyAttackCosts.GetEffectPercentage(effectValue) *
            item.m_shared.m_attack.m_attackHealthPercentage;
        AddStat("$item_healthuse", healthUsePercentage, magicItem.HasEffect(MagicEffectType.ModifyAttackHealthUse),
            TooltipStat.Percent);
    }

    private void GatherDrawStaminaUse()
    {
        if (item.m_shared.m_attack.m_drawStaminaDrain <= 0f)
        {
            return;
        }

        float attackDrawStaminaPercentage = 1 - magicItem.GetTotalEffectValue(MagicEffectType.ModifyDrawStaminaUse, 0.01f);
        AddStat("$item_staminahold", attackDrawStaminaPercentage * item.m_shared.m_attack.m_drawStaminaDrain,
            magicItem.HasEffect(MagicEffectType.ModifyDrawStaminaUse));
    }

    private void GatherBlockArmor()
    {
        float blockPower = GetBlockPowerValue(out bool hasModifiers);
        if (blockPower <= 1f)
        {
            return;
        }

        AddStat("$item_blockarmor", blockPower, hasModifiers).Secondary = item.GetBlockPowerTooltip(qualityLevel);
    }

    private void GatherBlockForceAndParry()
    {
        float deflectionForce = GetDeflectionForceValue(out bool hasModifiers);
        if (deflectionForce > 1f)
        {
            AddStat("$item_blockforce", deflectionForce, hasModifiers);
        }

        if (item.m_shared.m_timedBlockBonus > 1f)
        {
            float parryBonus = GetParryBonusValue(out hasModifiers);
            AddStat("$item_parrybonus", parryBonus, hasModifiers);
        }
    }

    private void GatherKnockback()
    {
        if (item.m_shared.m_attackForce > 0f)
        {
            AddStat("$item_knockback", CloseQuarter.GetKnockback(item, magicItem),
                magicItem.HasEffect(MagicEffectType.CloseQuarter, includeSocketed: true));
        }
    }

    private void GatherBackstab()
    {
        float totalBackstabBonusMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyBackstab, 0.01f);
        AddStat("$item_backstab", item.m_shared.m_backstabBonus * (1.0f + totalBackstabBonusMod),
            magicItem.HasEffect(MagicEffectType.ModifyBackstab));
    }

    private void GatherArmor()
    {
        AddStat("$item_armor", item.GetArmor(qualityLevel, Game.m_worldLevel),
            magicItem.HasEffect(MagicEffectType.ModifyArmor));
    }

    private void GatherEquipmentModifiers()
    {
        // The base-item, swim and sneak modifiers have no magic effect, so they stay with the renderer.
        bool hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyEitrRegen);
        if (hasModifiers || item.m_shared.m_eitrRegenModifier > 0f)
        {
            float value = ModifyPlayerRegen.GetModifiedRegenValue(item, MagicEffectType.ModifyEitrRegen,
                item.m_shared.m_eitrRegenModifier);
            AddModifier("$item_eitrregen_modifier", value, hasModifiers, player.GetEquipmentEitrRegenModifier());
        }

        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyMovementSpeed) ||
            magicItem.HasEffect(MagicEffectType.RemoveSpeedPenalty);
        if (hasModifiers || item.m_shared.m_movementModifier != 0f)
        {
            AddModifier("$item_movement_modifier", ModifyMovementSpeed.GetItemMovementModifier(item), hasModifiers,
                player.GetEquipmentMovementModifier());
        }

        hasModifiers = magicItem.HasEffect(MagicEffectType.IncreaseHeatResistance);
        if (hasModifiers || item.m_shared.m_heatResistanceModifier != 0f)
        {
            AddModifier("$item_heat_modifier", IncreaseHeatResistance.GetHeatResistanceValue(item), hasModifiers,
                player.GetEquipmentHeatResistanceModifier());
        }

        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyJumpStaminaUse);
        if (hasModifiers || item.m_shared.m_jumpStaminaModifier != 0f)
        {
            float playerTotal = player.GetEquipmentJumpStaminaModifier();
            player.m_seman.ModifyJumpStaminaUsage(1f, ref playerTotal, minZero: false);
            AddModifier("$se_jumpstamina", ModifyJumpStaminaUsage.GetJumpStaminaUsageValue(item), hasModifiers, playerTotal);
        }

        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyAttackStaminaUse);
        if (hasModifiers || item.m_shared.m_attackStaminaModifier != 0f)
        {
            float playerTotal = player.GetEquipmentAttackStaminaModifier();
            player.m_seman.ModifyAttackStaminaUsage(1f, ref playerTotal, minZero: false);
            // Subtract the raw effect fraction, not GetModifyAttackValue's (1 - x) MULTIPLIER: subtracting the
            // multiplier rendered a -13% enchant as "-87%". Mirrors the block stamina line below.
            float value = item.m_shared.m_attackStaminaModifier -
                magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackStaminaUse, 0.01f);
            AddModifier("$se_attackstamina", value, hasModifiers, playerTotal);
        }

        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyBlockStaminaUse);
        if (hasModifiers || item.m_shared.m_blockStaminaModifier != 0f)
        {
            float playerTotal = player.GetEquipmentBlockStaminaModifier();
            player.m_seman.ModifyBlockStaminaUsage(1f, ref playerTotal, minZero: false);
            AddModifier("$se_blockstamina", ModifyBlockStaminaUse.GetModifyBlockStaminaValue(item), hasModifiers, playerTotal);
        }

        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyDodgeStaminaUse);
        if (hasModifiers || item.m_shared.m_dodgeStaminaModifier != 0f)
        {
            float playerTotal = player.GetEquipmentDodgeStaminaModifier();
            player.m_seman.ModifyDodgeStaminaUsage(1f, ref playerTotal, minZero: false);
            AddModifier("$se_dodgestamina", ModifyDodgeStamina.GetModifyDodgeStaminaValue(item), hasModifiers, playerTotal);
        }

        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifySprintStaminaUse);
        if (hasModifiers || item.m_shared.m_runStaminaModifier != 0f)
        {
            float playerTotal = player.GetEquipmentRunStaminaModifier();
            player.m_seman.ModifyRunStaminaDrain(1f, ref playerTotal, Vector3.zero, minZero: false);
            AddModifier("$se_runstamina", ModifyRunStaminaDrain.GetModifySprintStaminaValue(item), hasModifiers, playerTotal);
        }
    }

    private void AddModifier(string id, float value, bool modified, float playerTotal)
    {
        AddStat(id, value, modified).Secondary = playerTotal;
    }

    private float GetDeflectionForceValue(out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyBlockForce) ||
            magicItem.HasEffect(MagicEffectType.ModifyParry, true) ||
            magicItem.HasEffect(MagicEffectType.Duelist);

        float deflection = item.GetDeflectionForce(qualityLevel);
        if (!ModifyBlock.RunModifyBlockPatchs(item))
        {
            deflection = ModifyBlock.GetDeflectionForceValue(null, item, deflection, false);
        }

        return deflection;
    }

    private float GetBlockPowerValue(out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyBlockPower, true) ||
            magicItem.HasEffect(MagicEffectType.ModifyParry, true) ||
            magicItem.HasEffect(MagicEffectType.Duelist);

        float block = item.GetBaseBlockPower(qualityLevel);
        if (!ModifyBlock.RunModifyBlockPatchs(item))
        {
            block = ModifyBlock.GetBlockPowerValue(null, item, block, false);
        }

        return block;
    }

    /// <summary>
    /// How much stronger a parry is than a normal block with this item. Vanilla multiplies the parry's block
    /// power by m_timedBlockBonus, and ModifyParry raises the base block power a parry starts from
    /// (ModifyBlock.GetBlockPowerValue), so the two multiply -- adding them understated the bonus.
    /// </summary>
    private float GetParryBonusValue(out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyParry) ||
            magicItem.HasEffect(MagicEffectType.ModifyParryLowHealth);

        // The same base block power vanilla's GetBaseBlockPower returns, before EpicLoot's postfixes.
        float baseBlock = item.m_shared.m_blockPower + Mathf.Max(0, qualityLevel - 1) * item.m_shared.m_blockPowerPerLevel;
        Player blockPlayer = ModifyBlock.RunModifyBlockPatchs(item) ? Player.m_localPlayer : null;
        float normalBlock = ModifyBlock.GetBlockPowerValue(blockPlayer, item, baseBlock, false);
        if (normalBlock <= 0f)
        {
            return item.m_shared.m_timedBlockBonus;
        }

        return item.m_shared.m_timedBlockBonus * ModifyBlock.GetBlockPowerValue(blockPlayer, item, baseBlock, true) / normalBlock;
    }
}
