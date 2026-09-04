using System.Collections.Generic;
using EpicLoot_UnityLib;
using EpicLoot.Abilities;
using EpicLoot.Adventure;
using EpicLoot.Biomes;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using EpicLoot.GatedItemType;
using EpicLoot.LegendarySystem;
using EpicLoot.Magic;
using EpicLoot.ShardStones;
using Newtonsoft.Json;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static readonly List<string> ConfigNames =
    [
        "loottable", "abilities", "adventuredata", "biomedata", "enchantcosts",
        "enchantingupgrades", "iteminfo", "itemnames", "itemsorter", "legendaries",
        "magiceffects", "materialconversion", "recipes", "shardstones",
        "shardstoneconversions"
    ];

    private static List<string> GetPrintConfigOptions(string[] args)
    {
        return args.Length switch
        {
            2 => ConfigNames,
            _ => []
        };
    }

    private static void PrintConfig(Terminal.ConsoleEventArgs args)
    {
        string patchType = args.GetString(1, "loottable");
        
        args.Context.PrintInfo($"> printconfig {patchType}");

        switch (patchType)
        {
            case "loottable":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(LootRoller.Config, Formatting.Indented));
                break;
            case "abilities":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(AbilityDefinitions.Config, Formatting.Indented));
                break;
            case "adventuredata":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(AdventureDataManager.Config, Formatting.Indented));
                break;
            case "biomedata":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(BiomeDataManager.Config, Formatting.Indented));
                break;
            case "enchantcosts":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(EnchantCostsHelper.Config, Formatting.Indented));
                break;
            case "enchantingupgrades":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(EnchantingTableUpgrades.Config, Formatting.Indented));
                break;
            case "iteminfo":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(GatedItemTypeHelper.GatedConfig, Formatting.Indented));
                break;
            case "itemnames":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(MagicItemNames.Config, Formatting.Indented));
                break;
            case "legendaries":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(UniqueLegendaryHelper.Config, Formatting.Indented));
                break;
            case "magiceffects":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(MagicItemEffectDefinitions.AllDefinitions, Formatting.Indented));
                break;
            case "materialconversion":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(MaterialConversions.Config, Formatting.Indented));
                break;
            case "recipes":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(RecipesHelper.Config, Formatting.Indented));
                break;
            case "itemsorter":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(AutoAddEnchantableItems.Config, Formatting.Indented));
                break;
            case "shardstones":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(Shards.GetCFG(), Formatting.Indented));
                break;
            case "shardstoneconversions":
                EpicLoot.LogWarningForce(JsonConvert.SerializeObject(ShardStoneConversions.Config, Formatting.Indented));
                break;
        }
    }
}