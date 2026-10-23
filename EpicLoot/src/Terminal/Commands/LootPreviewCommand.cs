using System.Collections.Generic;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void PrintLootPreview(Terminal.ConsoleEventArgs args)
    {
        string objectName = args.GetString(1, "Greydwarf");
        int maxLevel = args.TryParameterInt(2, 4);
        if (maxLevel < 1)
        {
            maxLevel = 1;
        }

        args.Context.PrintInfo($"> lootpreview: {objectName} levels 1-{maxLevel}");
        foreach (string line in LootPreview.Describe(objectName, maxLevel))
        {
            args.Context.PrintDebug(line);
        }
    }

    private static List<string> GetLootPreviewOptions(string[] args)
    {
        return args.Length switch
        {
            2 => GetCreatureNames(),
            _ => []
        };
    }
}
