using System.Collections.Generic;
using System.Linq;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;

namespace EpicLoot.Compendium;

public class SetInfo(string topic, bool showSearchBar = true) : MagicTextInfo(topic, showSearchBar)
{
    public override void Build(MagicPages instance)
    {
        // One entry per set, whatever rarities it rolls at, lowest rarity first.
        foreach (LegendarySetInfo set in UniqueLegendaryHelper.AllSets.Values
                     .OrderBy(x => UniqueLegendaryHelper.GetRarities(x).Min()))
        {
            FormatSetInfo(instance, set);
        }
    }

    private static void FormatSetInfo(MagicPages instance, LegendarySetInfo set)
    {
        List<LegendaryInfo> infos = [];
        foreach (string item in set.LegendaryIDs.Distinct())
        {
            if (!UniqueLegendaryHelper.TryGetLegendaryInfo(item, out LegendaryInfo info))
            {
                continue;
            }
            infos.Add(info);
        }

        // A set none of whose pieces exist can never drop (a per-rarity copy whose pieces failed to load).
        if (infos.Count == 0)
        {
            return;
        }

        List<ItemRarity> rarities = UniqueLegendaryHelper.GetRarities(set);
        List<string> content = [];

        content.Add(SetDescriptions.FormatRarities(rarities));

        content.Add($"$mod_epicloot_set ({infos.Count}):");
        bool anyFurtherRestricted = false;
        foreach (LegendaryInfo item in infos)
        {
            string requirements = SetDescriptions.DescribePieceRequirements(item, out bool furtherRestricted);
            anyFurtherRestricted |= furtherRestricted;
            content.Add($" - {item.Name} <color=#c0c0c0ff>({requirements}{(furtherRestricted ? "*" : "")})</color>");
        }

        if (anyFurtherRestricted)
        {
            content.Add("<color=#c0c0c0ff>* $mod_epicloot_set_restricted</color>");
        }

        content.Add("$mod_epicloot_set_bonuses: ");
        foreach (SetBonusInfo bonus in set.SetBonuses.OrderBy(x => x.Count))
        {
            if (bonus.Effect == null ||
                !MagicItemEffectDefinitions.AllDefinitions.TryGetValue(bonus.Effect.Type, out MagicItemEffectDefinition definition))
            {
                continue;
            }

            content.Add($" - ({bonus.Count}) {SetDescriptions.DescribeSetBonus(definition, bonus, rarities)}");

            // The bonus line is kept short for tooltips; the full explanation is the description.
            string detail = SetDescriptions.DescribeSetBonusDetail(definition, bonus, rarities);
            if (detail.Length > 0)
            {
                content.Add($"    <color=#c0c0c0ff>{detail}</color>");
            }

            string activationKey = SetAbilityKeys.DescribeActivationKey(definition);
            if (activationKey.Length > 0)
            {
                content.Add($"    <color=#c0c0c0ff>{activationKey}</color>");
            }
        }

        instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>" +
                                        $"<color={EpicLoot.GetRarityColor(rarities.Max())}>" +
                                        $"{set.Name}</color></size>", content.ToArray());
    }
}
