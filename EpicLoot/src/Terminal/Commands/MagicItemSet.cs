using System.Collections.Generic;
using System.Linq;
using EpicLoot.LegendarySystem;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void SpawnMagicItemSet(Terminal.ConsoleEventArgs args)
    {
        if (args.Length < 2)
        {
            args.Context.PrintWarning("> Specify Set ID");
            return;
        }

        string setID = args.GetString(1);
        args.Context.PrintInfo($"magicitemset - setID:{setID}");

        if (!UniqueLegendaryHelper.TryGetLegendarySetInfo(setID,
                out LegendarySetInfo setInfo, out ItemRarity rarity))
        {
            args.Context.PrintError($"> Could not find set info for setID: ({setID})");
            return;
        }

        if (setInfo != null)
        {
            for (var i = 0; i < setInfo.LegendaryIDs.Count; ++i)
            {
                var legendaryID = setInfo.LegendaryIDs[i];
                SpawnLegendaryHelper(args, legendaryID, rarity);
            }
        }
        else
        {
            args.Context.PrintError($"> Could not find set info for setID: ({setID})");
        }
    }

    private static List<string> GetMagicItemSetOptions(string[] args)
    {
        return args.Length switch
        {
            2 => UniqueLegendaryHelper.LegendarySets.Keys.Union(UniqueLegendaryHelper.MythicSets.Keys).ToList(),
            _ => []
        };
    }
}