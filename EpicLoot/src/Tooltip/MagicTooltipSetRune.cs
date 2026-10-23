using System.Collections.Generic;
using System.Linq;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;

namespace EpicLoot;

public partial class MagicTooltip
{
    // A set rune (Rune page, Set Extract) carries a set rather than effects. Instead of the magic-item
    // sections, which would all read empty, it says which set, what it can be etched onto and what the
    // set grants. No worn-piece count: the rune is not a piece, and "(0/4)" would read as progress.
    private void AddSetRuneTooltip()
    {
        string setColor = EpicLoot.GetSetItemColor();
        text.Append($"<color={setColor}>$mod_epicloot_setrune_label</color>\n");
        AddDescription();
        text.Append("\n");

        AddTeleportable();
        AddValuable();
        AddStackSizeAndWeight();

        if (!UniqueLegendaryHelper.TryGetLegendarySetInfo(magicItem.SetID, out LegendarySetInfo set))
        {
            text.Append($"\n\n<color={setColor}>$mod_epicloot_set: {magicItem.SetID}</color>");
            text.Append("\n<color=#ff5555ff>$mod_epicloot_setrune_unknownset</color>");
            return;
        }

        List<ItemRarity> rarities = UniqueLegendaryHelper.GetRarities(set);

        text.Append($"\n\n<color={setColor}>$mod_epicloot_set: {set.Name}</color>");
        text.Append($"\n$mod_epicloot_setrune_rarities: {SetDescriptions.FormatRarities(rarities)}");

        text.Append("\n$mod_epicloot_setrune_etchonto:");
        bool anyFurtherRestricted = false;
        foreach (LegendaryInfo piece in UniqueLegendaryHelper.GetDefinedSetPieces(set))
        {
            string requirements = SetDescriptions.DescribePieceRequirements(piece, out bool furtherRestricted);
            anyFurtherRestricted |= furtherRestricted;
            text.Append($"\n  {piece.Name} <color=#c0c0c0ff>({requirements}{(furtherRestricted ? "*" : "")})</color>");
        }

        text.Append("\n$mod_epicloot_set_bonuses:");
        // Shift adds each bonus's description, as on a set piece's tooltip.
        bool showDetails = MagicItem.ShowEffectDetails;
        foreach (SetBonusInfo bonus in set.SetBonuses.Where(x => x?.Effect != null).OrderBy(x => x.Count))
        {
            if (!MagicItemEffectDefinitions.TryGet(bonus.Effect.Type, out MagicItemEffectDefinition definition))
            {
                continue;
            }

            text.Append($"\n<color={setColor}>({bonus.Count}) ‣ " +
                $"{SetDescriptions.DescribeSetBonus(definition, bonus, rarities)}</color>");

            string detail = showDetails ? SetDescriptions.DescribeSetBonusDetail(definition, bonus, rarities) : "";
            if (detail.Length > 0)
            {
                text.Append($"\n<color=#c0c0c0ff>    {detail}</color>");
            }

            string activationKey = showDetails ? SetAbilityKeys.DescribeActivationKey(definition) : "";
            if (activationKey.Length > 0)
            {
                text.Append($"\n<color=#c0c0c0ff>    {activationKey}</color>");
            }
        }

        if (anyFurtherRestricted)
        {
            text.Append("\n<color=#c0c0c0ff>* $mod_epicloot_set_restricted</color>");
        }
    }
}
