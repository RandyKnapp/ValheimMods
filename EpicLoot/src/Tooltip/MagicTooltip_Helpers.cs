using EpicLoot.MagicItemEffects;
using UnityEngine;

namespace EpicLoot;

public partial class MagicTooltip
{
    public static string DamageRange(float damage, float minFactor, float maxFactor,
        bool magic = false, string magicColor = "orange")
    {
        int num1 = Mathf.RoundToInt(damage * minFactor);
        int num2 = Mathf.RoundToInt(damage * maxFactor);
        string color1 = magic ? magicColor : "orange";
        string color2 = magic ? magicColor : "yellow";
        return $"<color={color1}>{Mathf.RoundToInt(damage)}</color> " +
            $"<color={color2}>({num1}-{num2}) </color>";
    }

    private static float GetEitrModifierValue(ItemDrop.ItemData item, MagicItem magicItem, out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyEitrRegen);
        return ModifyPlayerRegen.GetModifiedRegenValue(item, MagicEffectType.ModifyEitrRegen, item.m_shared.m_eitrRegenModifier);
    }

    public static float GetMovementModifierValue(ItemDrop.ItemData item, MagicItem magicItem, out bool hasModifiers)
    {
        bool hasMovementModifier = magicItem.HasEffect(MagicEffectType.ModifyMovementSpeed);
        bool hasRemoveSpeedPenaltyModifier = magicItem.HasEffect(MagicEffectType.RemoveSpeedPenalty);
        hasModifiers = hasMovementModifier || hasRemoveSpeedPenaltyModifier;

        return ModifyMovementSpeed.GetItemMovementModifier(item);
    }

    public static float GetHeatModifierValue(ItemDrop.ItemData item, MagicItem magicItem, out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.IncreaseHeatResistance);
        return IncreaseHeatResistance.GetHeatResistanceValue(item);
    }

    public static float GetJumpStaminaUsageModifierValue(ItemDrop.ItemData item, MagicItem magicItem, out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyJumpStaminaUse);
        return ModifyJumpStaminaUsage.GetJumpStaminaUsageValue(item);
    }

    public static float GetAttackStaminaModifierValue(ItemDrop.ItemData item, MagicItem magicItem, out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyAttackStaminaUse);
        // Subtract the raw effect fraction, not GetModifyAttackValue's (1 - x) MULTIPLIER: subtracting the
        // multiplier rendered a -13% enchant as "-87%". Mirrors GetBlockStaminaModifierValue below.
        return item.m_shared.m_attackStaminaModifier -
            magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackStaminaUse, 0.01f);
    }

    public static float GetBlockStaminaModifierValue(ItemDrop.ItemData item, MagicItem magicItem, out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyBlockStaminaUse);
        return ModifyBlockStaminaUse.GetModifyBlockStaminaValue(item);
    }

    public static float GetDodgeStaminaModifierValue(ItemDrop.ItemData item, MagicItem magicItem, out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyDodgeStaminaUse);
        return ModifyDodgeStamina.GetModifyDodgeStaminaValue(item);
    }

    public static float GetRunStaminaModifierValue(ItemDrop.ItemData item, MagicItem magicItem, out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifySprintStaminaUse);
        return ModifyRunStaminaDrain.GetModifySprintStaminaValue(item);
    }

    public static float GetDeflectionForceValue(ItemDrop.ItemData item, MagicItem magicItem, int quality, out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyBlockForce) ||
            magicItem.HasEffect(MagicEffectType.ModifyParry, true) ||
            magicItem.HasEffect(MagicEffectType.Duelist);

        float deflection = item.GetDeflectionForce(quality);
        if (!ModifyBlock.RunModifyBlockPatchs(item))
        {
            deflection = ModifyBlock.GetDeflectionForceValue(null, item, deflection, false);
        }

        return deflection;
    }

    public static float GetBlockPowerValue(ItemDrop.ItemData item, MagicItem magicItem, int quality, out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyBlockPower, true) ||
            magicItem.HasEffect(MagicEffectType.ModifyParry, true) ||
            magicItem.HasEffect(MagicEffectType.Duelist);

        float block = item.GetBaseBlockPower(quality);
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
    public static float GetParryBonusValue(ItemDrop.ItemData item, MagicItem magicItem, int quality, out bool hasModifiers)
    {
        hasModifiers = magicItem.HasEffect(MagicEffectType.ModifyParry) ||
            magicItem.HasEffect(MagicEffectType.ModifyParryLowHealth);

        // The same base block power vanilla's GetBaseBlockPower returns, before EpicLoot's postfixes.
        float baseBlock = item.m_shared.m_blockPower + Mathf.Max(0, quality - 1) * item.m_shared.m_blockPowerPerLevel;
        Player player = ModifyBlock.RunModifyBlockPatchs(item) ? Player.m_localPlayer : null;
        float normalBlock = ModifyBlock.GetBlockPowerValue(player, item, baseBlock, false);
        if (normalBlock <= 0f)
        {
            return item.m_shared.m_timedBlockBonus;
        }

        return item.m_shared.m_timedBlockBonus * ModifyBlock.GetBlockPowerValue(player, item, baseBlock, true) / normalBlock;
    }
}