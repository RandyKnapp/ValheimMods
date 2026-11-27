using System.Collections.Generic;
using System.Linq;
using EpicLoot.Biomes;
using EpicLoot.GatedItemType;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void BiomeKills(Terminal.ConsoleEventArgs args)
    {
        if (Player.m_localPlayer == null)
        {
            args.Context.PrintInfo("> No local player");
            return;
        }

        switch (args.GetString(1, "").ToLowerInvariant())
        {
            case "set":
                SetBiomeKills(args);
                return;
            case "roll":
                RollGatedItem(args);
                return;
        }

        foreach (string line in GradualUnlock.Describe())
        {
            args.Context.PrintInfo(line);
        }
    }

    private static void SetBiomeKills(Terminal.ConsoleEventArgs args)
    {
        string arg = args.GetString(2, "");
        if (!BiomeDataManager.TryResolve(arg, out Heightmap.Biome biome) || !BiomeDataManager.IsKnown(biome))
        {
            args.Context.PrintInfo($"> Unknown biome '{arg}'. Use 'biomes' to list them.");
            return;
        }

        int kills = args.TryParameterInt(3, 0);
        GradualUnlock.SetKills(biome, kills);
        args.Context.PrintInfo($"> {BiomeDataManager.GetName(biome)} kills set to {GradualUnlock.GetKills(biome)}");
    }

    // Resolves a loot entry the way a drop does, many times over, and counts what it became by tier.
    private static void RollGatedItem(Terminal.ConsoleEventArgs args)
    {
        string item = args.GetString(2, "");
        if (string.IsNullOrEmpty(item))
        {
            args.Context.PrintInfo("> biomekills roll <item|type> [count]");
            return;
        }

        int count = System.Math.Max(1, args.TryParameterInt(3, 200));
        GatedItemTypeMode mode = EpicLoot.GetGatedItemTypeMode();
        var results = new Dictionary<string, int>();
        for (int i = 0; i < count; i++)
        {
            string name = GatedItemTypeHelper.GetGatedItemNameFromItemOrType(item, mode) ?? "(nothing)";
            results.TryGetValue(name, out int seen);
            results[name] = seen + 1;
        }

        args.Context.PrintInfo($"> {item} x{count} under {mode}:");
        foreach (KeyValuePair<string, int> result in results.OrderByDescending(x => x.Value))
        {
            args.Context.PrintInfo($"    {result.Key} ({TierName(result.Key)}): {result.Value} ({(float)result.Value / count:0.#%})");
        }
    }

    private static string TierName(string itemName)
    {
        if (!GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(itemName, out GatedItemDetails details) ||
            details.RequiredBosses == null || details.RequiredBosses.Count == 0)
        {
            return "ungated";
        }

        Heightmap.Biome tier = BiomeDataManager.GetFirstBiomeForBossKey(details.RequiredBosses[0]);
        return tier == Heightmap.Biome.None ? "ungated" : BiomeDataManager.GetName(tier);
    }

    private static List<string> GetBiomeKillsOptions(string[] args)
    {
        return args.Length switch
        {
            2 => ["set", "roll"],
            3 when args[1] == "set" => BiomeDataManager.BiomesInOrder.Select(x => x.Name).ToList(),
            3 when args[1] == "roll" => GatedItemTypeHelper.ItemsByTypeAndBoss.Keys.ToList(),
            _ => []
        };
    }
}
