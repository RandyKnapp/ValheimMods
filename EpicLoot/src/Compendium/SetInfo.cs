using System.Collections.Generic;
using EpicLoot.LegendarySystem;

namespace EpicLoot.Compendium;

public class SetInfo(string topic, bool showSearchBar = true) : MagicTextInfo(topic, showSearchBar)
{
    public override void Build(MagicPages instance)
    {
        foreach (LegendarySetInfo set in UniqueLegendaryHelper.LegendarySets.Values)
        {
            FormatSetInfo(instance, set, ItemRarity.Legendary);
        }

        foreach (LegendarySetInfo set in UniqueLegendaryHelper.MythicSets.Values)
        {
            FormatSetInfo(instance, set, ItemRarity.Mythic);
        }
    }
    
    private static void FormatSetInfo(MagicPages instance, LegendarySetInfo set, ItemRarity rarity)
    {
        List<LegendaryInfo> infos = [];
        foreach (string item in set.LegendaryIDs)
        {
            if (!UniqueLegendaryHelper.TryGetLegendaryInfo(item, out LegendaryInfo info))
            {
                continue;
            }
            infos.Add(info);
        }
        List<string> content = [];
        
        content.Add($"$mod_epicloot_set ({infos.Count}):");
        foreach (LegendaryInfo item in infos)
        {
            content.Add($" - {item.Name} <color=#c0c0c0ff>({string.Join(", ", item.Requirements.AllowedItemTypes)})</color>");
        }
            
        content.Add("$mod_epicloot_set_bonuses: ");
        foreach (SetBonusInfo bonus in set.SetBonuses)
        {
            if (!MagicItemEffectDefinitions.AllDefinitions.TryGetValue(bonus.Effect.Type, out MagicItemEffectDefinition definition))
            {
                continue;
            }
            
            content.Add($" - ({bonus.Count}) " + 
                        $"{string.Format(Localization.instance.Localize(definition.DisplayText), 
                            "<b><color=yellow>X</color></b>")}");
        }
        
        instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>" + 
                                        $"<color={EpicLoot.GetRarityColor(rarity)}>" + 
                                        $"{set.Name}</color></size>", content.ToArray());
    }
    
}