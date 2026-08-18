using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpicLoot;

public static partial class TerminalManager
{
    private const int LUCK_TEST_COL_1 = 140;
    private const int LUCK_TEST_COL_2 = 230;
    private const int LUCK_TEST_COL_3 = 320;
    private const int LUCK_TEST_COL_4 = 410;
    private const int LUCK_TEST_COL_5 = 500;
    private const int LUCK_TEST_COL_6 = 590;
    
    private static void TestLuck(Terminal.ConsoleEventArgs args)
    {
        string lootTable = args.GetString(1, "Greydwarf");
        float luckFactor = args.TryParameterFloat(2);
        
        KeyValuePair<string, List<LootTable>> loot_info = LootRoller.GetLootTableOrDefault(lootTable);
        LootDrop lootDrop = LootRoller.GetLootForLevel(loot_info.Value[0], 1)[0];
        lootDrop = LootRoller.ResolveLootDrop(lootDrop);
        if (lootDrop.Rarity == null)
        {
            lootDrop.Rarity = [100, 0, 0, 0, 0];
            args.Context.PrintError($"No rarity table was found for {loot_info.Value[0]} using default: [100, 0, 0, 0, 0]");
        }

        var rarityBase = LootRoller.GetRarityWeights(lootDrop.Rarity, 0);
        var rarityLuck = LootRoller.GetRarityWeights(lootDrop.Rarity, luckFactor);

        var sb = new StringBuilder();
        sb.Append($"> Luck Test: {loot_info.Key}, {luckFactor}\n");
        sb.Append("Rarity");
        sb.Append($"<pos={LUCK_TEST_COL_1}>Base");
        sb.Append($"<pos={LUCK_TEST_COL_2}>%");
        sb.Append($"<pos={LUCK_TEST_COL_3}>Luck");
        sb.Append($"<pos={LUCK_TEST_COL_4}>%");
        sb.Append($"<pos={LUCK_TEST_COL_5}>Diff");
        sb.Append($"<pos={LUCK_TEST_COL_6}>Factor\n");
        sb.Append("=========================================================================\n");

        var rarityBaseTotal = rarityBase.Sum(x => x.Value);
        var rarityLuckTotal = rarityLuck.Sum(x => x.Value);
        for (var index = 0; index < 5; index++)
        {
            var rarity = (ItemRarity)index;
            var color = EpicLoot.GetRarityColor(rarity);
            var baseWeight = rarityBase[rarity];
            var luckWeight = rarityLuck[rarity];

            var basePercent = baseWeight / rarityBaseTotal;
            var luckPercent = luckWeight / rarityLuckTotal;

            sb.Append($"<color={color}>{rarity}");
            sb.Append($"<pos={LUCK_TEST_COL_1}>{baseWeight:0.##}");
            sb.Append($"<pos={LUCK_TEST_COL_2}>{basePercent:0.##%}");
            sb.Append($"<pos={LUCK_TEST_COL_3}>{luckWeight:0.##}");
            sb.Append($"<pos={LUCK_TEST_COL_4}>{luckPercent:0.##%}");
            sb.Append($"<pos={LUCK_TEST_COL_5}>{luckPercent - basePercent:+0.##%;-0.##%;0%}");
            sb.Append($"<pos={LUCK_TEST_COL_6}>{(basePercent > 0 ? (luckPercent / basePercent).ToString("0.##") : "0")}\n");
            sb.Append("</color>");
        }
        
        args.Context.PrintInfo(sb.ToString());
    }

    private static List<string> GetTestLuckOptions(string[] args)
    {
        return args.Length switch
        {
            2 => GetCreatureNames(),
            _ => []
        };
    }
}