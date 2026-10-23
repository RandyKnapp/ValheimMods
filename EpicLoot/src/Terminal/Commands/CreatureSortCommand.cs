using System.Collections.Generic;
using System.Linq;

namespace EpicLoot;

public static partial class TerminalManager
{
    // creaturesort       re-sorts and writes (server / single player only)
    // creaturesort dry   shows what the sorter would do, without writing anything
    // creaturesort all   the dry run including creatures that already have a table or are excluded
    private static void RunCreatureSort(Terminal.ConsoleEventArgs args)
    {
        string mode = args.GetString(1, "").ToLowerInvariant();
        bool write = mode == "";
        if (write && (ZNet.instance == null || !ZNet.instance.IsServer()))
        {
            args.Context.PrintError("> Only the server (or single player) applies the creature sorter; use 'creaturesort dry' to preview.");
            return;
        }

        var run = CreatureSorterRunner.Run("console", write);
        if (run == null)
        {
            args.Context.PrintError(write
                ? "> The creature sorter did not run: 'Auto Add Creatures To Loot Tables' is off, or no world is loaded."
                : "> The creature sorter needs a loaded world.");
            return;
        }

        var (decisions, merge) = run.Value;
        args.Context.PrintInfo($"> creaturesort{(write ? "" : " (dry run)")}: {merge.Added.Count} to add, " +
            $"{merge.Updated.Count} to update, {merge.Removed.Count} to remove, " +
            $"{decisions.Count(d => d.Disagrees)} explicit entries differ from the rules");
        IEnumerable<CreatureDecision> shown = mode == "all" ? decisions : decisions.Where(d => d.IsCandidate || d.Disagrees);
        foreach (string line in CreatureSorter.FormatReport(shown, null, "").Split('\n').Skip(2))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                args.Context.PrintDebug(line.TrimEnd('\r'));
            }
        }
    }

    private static List<string> GetCreatureSortOptions(string[] args)
    {
        return args.Length == 2 ? ["dry", "all"] : [];
    }
}
