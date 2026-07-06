namespace EpicLoot;

public partial class MagicTooltip
{
    private void AddArmor()
    {
        string hasArmorModifier = magicItem.HasEffect(MagicEffectType.ModifyArmor) ? magicColor : "orange";
        text.Append($"\n$item_armor: " +
            $"<color={hasArmorModifier}>{item.GetArmor(qualityLevel, Game.m_worldLevel):0.#}</color>");
    }

    private void AddDamageModifiers()
    {
        string modifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
        if (modifiersTooltipString.Length > 0)
        {
            text.Append(modifiersTooltipString);
        }
    }
}