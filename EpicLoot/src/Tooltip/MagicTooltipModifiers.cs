using UnityEngine;

namespace EpicLoot;

public partial class MagicTooltip
{
    private void ModifierFormat(string name, bool hasModifier, float value, float totalValue)
    {
        string color = (hasModifier) ? magicColor : "orange";
        text.Append($"\n{name}: <color={color}>{value * 100f:+0;-0}%</color> " +
            $"($item_total:<color=yellow>{totalValue * 100f:+0;-0}%</color>)");
    }

    private void AddAllEquipmentModifiers()
    {
        // TODO: add health/stamina regen if needed
        AddEitrRegen();
        AddMovementModifier();
        AddBaseModifier();
        AddHeatModifier();
        AddJumpStaminaModifier();
        AddAttackStaminaModifier();
        AddBlockStaminaModifier();
        AddDodgeStaminaModifier();
        AddSwimStaminaModifier();
        AddSneakStaminaModifier();
        AddRunStaminaModifier();
        AddMaxAdrenaline();
    }

    private void AddEitrRegen()
    {
        float value = GetEitrModifierValue(item, magicItem, out bool hasModifiers);
        if (hasModifiers || item.m_shared.m_eitrRegenModifier > 0f)
        {
            if (hasModifiers || item.m_shared.m_eitrRegenModifier != 0f)
            {
                float playerTotal = localPlayer.GetEquipmentEitrRegenModifier();
                ModifierFormat("$item_eitrregen_modifier", hasModifiers, value, playerTotal);
            }
        }
    }

    private void AddMovementModifier()
    {
        float value = GetMovementModifierValue(item, magicItem, out bool hasModifiers);
        if (hasModifiers || item.m_shared.m_movementModifier != 0f)
        {
            float playerTotal = localPlayer.GetEquipmentMovementModifier();
            ModifierFormat("$item_movement_modifier", hasModifiers, value, playerTotal);
        }
    }

    private void AddBaseModifier()
    {
        bool hasModifiers = false;
        float value = item.m_shared.m_homeItemsStaminaModifier;
        if (hasModifiers || item.m_shared.m_homeItemsStaminaModifier != 0f)
        {
            float playerTotal = localPlayer.GetEquipmentHomeItemModifier();
            ModifierFormat("$base_item_modifier", hasModifiers, value, playerTotal);
        }
    }

    private void AddHeatModifier()
    {
        float value = GetHeatModifierValue(item, magicItem, out bool hasModifiers);
        if (hasModifiers || item.m_shared.m_heatResistanceModifier != 0f)
        {
            float playerTotal = localPlayer.GetEquipmentHeatResistanceModifier();
            ModifierFormat("$item_heat_modifier", hasModifiers, value, playerTotal);
        }
    }

    private void AddJumpStaminaModifier()
    {
        float value = GetJumpStaminaUsageModifierValue(item, magicItem, out bool hasModifiers);
        if (hasModifiers || item.m_shared.m_jumpStaminaModifier != 0f)
        {
            float playerTotal = localPlayer.GetEquipmentJumpStaminaModifier();
            localPlayer.m_seman.ModifyJumpStaminaUsage(1f, ref playerTotal, minZero: false);
            ModifierFormat("$se_jumpstamina", hasModifiers, value, playerTotal);
        }
    }

    private void AddAttackStaminaModifier()
    {
        float value = GetAttackStaminaModifierValue(item, magicItem, out bool hasModifiers);
        if (hasModifiers || item.m_shared.m_attackStaminaModifier != 0f)
        {
            float playerTotal = localPlayer.GetEquipmentAttackStaminaModifier();
            localPlayer.m_seman.ModifyAttackStaminaUsage(1f, ref playerTotal, minZero: false);
            ModifierFormat("$se_attackstamina", hasModifiers, value, playerTotal);
        }
    }

    private void AddBlockStaminaModifier()
    {
        float value = GetBlockStaminaModifierValue(item, magicItem, out bool hasModifiers);
        if (hasModifiers || item.m_shared.m_blockStaminaModifier != 0f)
        {
            float playerTotal = localPlayer.GetEquipmentBlockStaminaModifier();
            localPlayer.m_seman.ModifyBlockStaminaUsage(1f, ref playerTotal, minZero: false);
            ModifierFormat("$se_blockstamina", hasModifiers, value, playerTotal);
        }
    }

    private void AddDodgeStaminaModifier()
    {
        float value = GetDodgeStaminaModifierValue(item, magicItem, out bool hasModifiers);
        if (hasModifiers || item.m_shared.m_dodgeStaminaModifier != 0f)
        {
            float playerTotal = localPlayer.GetEquipmentDodgeStaminaModifier();
            localPlayer.m_seman.ModifyDodgeStaminaUsage(1f, ref playerTotal, minZero: false);
            ModifierFormat("$se_dodgestamina", hasModifiers, value, playerTotal);
        }
    }

    private void AddSwimStaminaModifier()
    {
        // TODO: add a magic effect for this?
        bool hasModifiers = false;
        float value = item.m_shared.m_swimStaminaModifier;
        if (hasModifiers || item.m_shared.m_swimStaminaModifier != 0f)
        {
            float playerTotal = localPlayer.GetEquipmentSwimStaminaModifier();
            localPlayer.m_seman.ModifySwimStaminaUsage(1f, ref playerTotal, minZero: false);
            ModifierFormat("$se_swimstamina", hasModifiers, value, playerTotal);
        }
    }

    private void AddSneakStaminaModifier()
    {
        // TODO: add a magic effect for this?
        bool hasModifiers = false;
        float value = item.m_shared.m_sneakStaminaModifier;
        if (hasModifiers || item.m_shared.m_sneakStaminaModifier != 0f)
        {
            float playerTotal = localPlayer.GetEquipmentSneakStaminaModifier();
            localPlayer.m_seman.ModifySneakStaminaUsage(1f, ref playerTotal, minZero: false);
            ModifierFormat("$se_sneakstamina", hasModifiers, value, playerTotal);
        }
    }

    private void AddRunStaminaModifier()
    {
        float value = GetRunStaminaModifierValue(item, magicItem, out bool hasModifiers);
        if (hasModifiers || item.m_shared.m_runStaminaModifier != 0f)
        {
            float playerTotal = localPlayer.GetEquipmentRunStaminaModifier();
            localPlayer.m_seman.ModifyRunStaminaDrain(1f, ref playerTotal, Vector3.zero, minZero: false);
            ModifierFormat("$se_runstamina", hasModifiers, value, playerTotal);
        }
    }

    private void AddMaxAdrenaline()
    {
        if (item.m_shared.m_maxAdrenaline > 0f)
        {
            text.AppendFormat("\n$item_maxadrenaline: <color=orange>{0}</color>", item.m_shared.m_maxAdrenaline);
        }
    }
}