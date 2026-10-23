using System.Collections.Generic;
using System.Linq;
using EpicLoot.LegendarySystem;
using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager
{
    // magicitemset <setID> [rarity|random]: every piece of a set. Omitting the rarity uses the set's highest;
    // "random" gives each piece its own rarity from the set's list, for testing mixed-rarity tiers.
    private static void SpawnMagicItemSet(Terminal.ConsoleEventArgs args)
    {
        if (args.Length < 2)
        {
            args.Context.PrintWarning("> Specify Set ID, rarity (optional or random)");
            return;
        }

        string setID = args.GetString(1);
        string rarityArg = args.GetString(2);

        if (!UniqueLegendaryHelper.TryGetLegendarySetInfo(setID, out LegendarySetInfo setInfo))
        {
            args.Context.PrintError($"> Could not find set info for setID: ({setID})");
            return;
        }

        List<ItemRarity> enabled = UniqueLegendaryHelper.GetRarities(setInfo);
        bool randomPerPiece = !string.IsNullOrEmpty(rarityArg) && IsRandomArg(rarityArg);
        ItemRarity setRarity = enabled.Max();
        if (!string.IsNullOrEmpty(rarityArg) && !randomPerPiece &&
            !TryResolveSpawnRarity(args, setID, enabled, rarityArg, out setRarity))
        {
            return;
        }

        args.Context.PrintInfo($"magicitemset - setID:{setID} rarity:{(randomPerPiece ? "random" : setRarity.ToString())}");

        foreach (string legendaryID in setInfo.LegendaryIDs.Distinct())
        {
            ItemRarity rarity = randomPerPiece ? enabled[Random.Range(0, enabled.Count)] : setRarity;
            SpawnLegendaryHelper(args, legendaryID, rarity);
        }
    }

    private static List<string> GetMagicItemSetOptions(string[] args)
    {
        return args.Length switch
        {
            2 => UniqueLegendaryHelper.AllSets.Keys.OrderBy(x => x).ToList(),
            3 => GetRarityOptions(UniqueLegendaryHelper.TryGetLegendarySetInfo(args.GetString(1), out LegendarySetInfo set)
                ? UniqueLegendaryHelper.GetRarities(set)
                : null),
            _ => []
        };
    }
}
