using EpicLoot.MagicItemEffects.Shards;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager
{
    private const int FISH_LUCK_COL_1 = 200;
    private const int FISH_LUCK_COL_2 = 300;
    private const int FISH_LUCK_COL_3 = 420;

    // Samples the LuckWhileFishing treasure roll so the value threshold curve can be checked without
    // grinding catches. Entries that no longer appear at a given value have been dropped by the rising
    // floor (or sit above the reachable roll range); both are expected as the effect value climbs.
    private static void TestFishingLuck(Terminal.ConsoleEventArgs args)
    {
        float value = args.TryParameterFloat(1, 25f);
        int samples = Mathf.Max(1, args.TryParameterInt(2, 10000));

        List<KeyValuePair<string, float>> table = LuckWhileFishing.ResolveTable();
        if (table.Count == 0)
        {
            args.Context.PrintError("> No fishing treasure entries resolved (is ObjectDB loaded?)");
            return;
        }

        var hits = new Dictionary<string, int>();
        var totalAmount = new Dictionary<string, long>();
        int misses = 0;
        for (int i = 0; i < samples; i++)
        {
            if (LuckWhileFishing.TryRollTreasure(value, out string prefabName, out int amount))
            {
                hits.TryGetValue(prefabName, out int count);
                hits[prefabName] = count + 1;
                totalAmount.TryGetValue(prefabName, out long sum);
                totalAmount[prefabName] = sum + amount;
            }
            else
            {
                misses++;
            }
        }

        var sb = new StringBuilder();
        sb.Append($"> Fishing Luck Test: value {value:0.##}, {samples} samples, triple chance {LuckWhileFishing.GetTripleChance():0.##}%\n");
        sb.Append("Item");
        sb.Append($"<pos={FISH_LUCK_COL_1}>Threshold");
        sb.Append($"<pos={FISH_LUCK_COL_2}>Share");
        sb.Append($"<pos={FISH_LUCK_COL_3}>Avg Amount\n");
        sb.Append("=========================================================================\n");

        foreach (KeyValuePair<string, float> entry in table)
        {
            hits.TryGetValue(entry.Key, out int count);
            totalAmount.TryGetValue(entry.Key, out long sum);

            sb.Append(entry.Key);
            sb.Append($"<pos={FISH_LUCK_COL_1}>{entry.Value:0.##}");
            sb.Append($"<pos={FISH_LUCK_COL_2}>{(count / (float)samples):0.##%}");
            sb.Append($"<pos={FISH_LUCK_COL_3}>{(count > 0 ? (sum / (float)count).ToString("0.##") : "-")}");
            sb.Append(count == 0 ? "   (dropped)\n" : "\n");
        }

        if (misses > 0)
        {
            sb.Append($"\nNo treasure: {(misses / (float)samples):0.##%} (floor above every entry)\n");
        }

        args.Context.PrintInfo(sb.ToString());
    }
}
