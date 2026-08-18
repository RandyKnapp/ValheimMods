using System.Collections.Generic;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void PrintLootResolution(Terminal.ConsoleEventArgs args)
    {
        string lootTable = args.GetString(1, "Greydwarf");
        int level = args.TryParameterInt(2, 1);
        int itemIndex = args.TryParameterInt(3, 0);
        
        LootTable table = LootRoller.GetLootTable(lootTable)[0];
        LootDrop[] tableForLevel = LootRoller.GetLootForLevel(table, level);
        if (tableForLevel.Length < itemIndex - 1)
        {
            args.Context.PrintError("> item index is out of range, using last index");
            itemIndex = tableForLevel.Length - 1;
        }
        
        args.Context.PrintInfo($"> lootres: {lootTable}:{level}:{itemIndex}");

        LootDrop lootDrop = tableForLevel[itemIndex];
        lootDrop = LootRoller.ResolveLootDrop(lootDrop);
        float[] rarity = lootDrop.Rarity;

        if (rarity.Length < 1)
        {
            args.Context.PrintError($"> loot resolution not defined for {lootTable}");
            return;
        }
        
        string rarityStr = "> rarity=[ ";
        for (int i = 0; i < rarity.Length - 1; i++)
        {
            rarityStr += $"{rarity[i]}, ";
        }

        rarityStr += $"{rarity[rarity.Length - 1]} ]";

        args.Context.PrintDebug(rarityStr);
    }

    private static List<string> GetLootResolutionOptions(string[] args)
    {
        return args.Length switch
        {
            2 => GetCreatureNames(),
            _ => []
        };
    }
}