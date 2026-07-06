namespace EpicLoot;

public partial class MagicTooltip
{
    private void AddBlockForceAndParry()
    {
        // TODO fix magic effects not applying to caluclation, like for damages
        // TODO: apply duelest values here, make a getter
        float deflectionForce = GetDeflectionForceValue(item, magicItem, qualityLevel, out bool hasModifiers);
        string magicBlockColor = hasModifiers ? magicColor : "orange";

        if (deflectionForce > 1f)
        {
            text.Append($"\n$item_blockforce: " +
                $"<color={magicBlockColor}>{deflectionForce:0.#}</color>");
        }

        if (item.m_shared.m_timedBlockBonus > 1f)
        {
            float parryBonus = GetParryBonusValue(item, magicItem, qualityLevel, out hasModifiers);
            magicBlockColor = hasModifiers ? magicColor : "orange";

            text.Append($"\n$item_parrybonus: <color={magicBlockColor}>{parryBonus:0.#}x</color>");
        }
    }

    private void AddBlockArmor()
    {
        float blockPower = GetBlockPowerValue(item, magicItem, qualityLevel, out bool hasModifiers);
        if (blockPower <= 1f)
        {
            return;
        }

        string magicBlockColor1 = hasModifiers ? magicColor : "orange";
        string magicBlockColor2 = hasModifiers ? magicColor : "yellow";

        float blockPowerTooltipValue = item.GetBlockPowerTooltip(qualityLevel);
        string blockPowerPercentageString = blockPowerTooltipValue.ToString("0");

        text.Append($"\n$item_blockarmor: <color={magicBlockColor1}>{blockPower:0.#}</color> " +
            $"<color={magicBlockColor2}>({blockPowerPercentageString})</color>");
    }

    private void AddParryAdrenaline()
    {
        if (item.m_shared.m_perfectBlockAdrenaline > 0.0)
        {
            text.Append($"\n$item_parryadrenaline: <color=orange>{item.m_shared.m_perfectBlockAdrenaline}</color>");
        }
    }
}