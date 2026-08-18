using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace EpicLoot;

public static partial class TerminalManager
{
    internal static readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.tabCycle))]
    private static class Terminal_TabCycle_Patch
    {
        // override tab cycle to allow to cycle through different tab options depending on current input
        // instead of just the single string list
        private static void Prefix(Terminal __instance, string word, bool usePrefix, ref List<string> options)
        {
            if (usePrefix) return;
            
            string[] strArray = __instance.m_input.text.Split(' ');
            string inputCommand = strArray.First();
            if (!_commands.TryGetValue(inputCommand, out Command command)) return;

            options = command.GetTabOptions(strArray);
        }
    }
    
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class Register_Commands
    {
        public static void Postfix()
        {
            _ = new Command("magicitem", "spawn magic item: [rarity] [item] [amount] [effect count]", SpawnMagicItem, GetSpawnMagicItemOptions, isCheat: true, alternates: "mi");
            _ = new Command("magicitemwitheffect", "spawn magic item with effect: [effect] [item]", SpawnMagicItemWithEffect, GetSpawnMagicItemWithEffectOptions, isCheat: true, alternates: "mieffect");
            _ = new Command("magicitemlegendary", "spawn legendary item: [legendaryID] [item]", SpawnLegendaryMagicItem, GetLegendaryOptions, isCheat: true, alternates: "milegend");
            _ = new Command("magicitemmythic", "spawn mythic item: [mythicID] [item]", SpawnMythicMagicItem, GetMythicOptions, isCheat: true, alternates: "mimythic");
            _ = new Command("magicitemset", "spawn magic item set: [setID]", SpawnMagicItemSet, GetMagicItemSetOptions, isCheat: true, alternates: "miset");
            _ = new Command("checkstackquality", "show list of items that have a max stack size over 1 and max quality over 1", CheckStackQuality, isCheat: true);
            _ = new Command("magicmats", "spawn all magic materials with half stack", SpawnMagicCraftingMaterials, isCheat: true);
            _ = new Command("magicshards", "spawn all shardstones: [rarity] (random valid rarity per shard if omitted)", SpawnMagicShards, isCheat: true);
            _ = new Command("alwaysdrop", "toggle always drop", ToggleAlwaysDrop, isCheat: true);
            _ = new Command("cheatgating", "toggle cheat gating", ToggleCheatGating, isCheat: true);
            _ = new Command("cheatsockets", "forces the provided number of sockets to always roll onto drops", CheatSockets, isCheat: true);
            _ = new Command("testtreasuremap", "spawns treasure chests and adds to adventure map", TestTreasureMap,  isCheat: true, alternates: "testtm");
            _ = new Command("resettreasuremap", "removes all active treasure maps", ResetTreasureMap,  isCheat: true, alternates: "resettm");
            _ = new Command("debugtreasuremap", "toggle treasure map debug mode", DebugTreasureMap,  isCheat: true, alternates: "debugtm");
            _ = new Command("resetbounties", "removes active bounties", ResetBounties,  isCheat: true);
            _ = new Command("testbountynames", "print randomly generated bounty names: [amount]", TestBountyNames,  isCheat: true);
            _ = new Command("resetadventure", "clear player adventure data", ResetAdventure,  isCheat: true);
            _ = new Command("bounties", "print available bounties: [interval]", PrintAvailableBounties, isCheat: true);
            _ = new Command("playerbounties", "print player available bounties", PrintPlayerAvailableBounties,  isCheat: true);
            _ = new Command("gotomerchant", "teleport to merchant: [merchant]", GoToMerchant, GetGoToOptions, isCheat: true, alternates: "gotom");
            _ = new Command("globalkeys", "print active global keys", PrintGlobalKeys,  isCheat: true);
            _ = new Command("lootres", "print loot resolution: [creature] [level] [itemIndex]", PrintLootResolution, GetLootResolutionOptions, isCheat: true);
            _ = new Command("resetcooldowns", "reset ability cooldowns", ResetAbilityCooldowns,  isCheat: true);
            _ = new Command("debugluck", "print players luck factor in console", DebugLuck, isCheat: true);
            _ = new Command("tooltipdebug", "write inventory item tooltips to disk", DebugTooltip, isCheat: true);
            _ = new Command("tooltipdebugvanilla", "write inventory item tooltips to disk, without magic effects", DebugVanillaTooltip,  isCheat: true);
            _ = new Command("lucktest", "rolls an example loot table with the sepcified luck eg: lucktest Greydwarf 1.0", TestLuck, GetTestLuckOptions, isCheat: true);
            _ = new Command("fishlucktest", "samples the lucky fishing treasure roll: [effectValue] [samples]", TestFishingLuck, isCheat: true);
            _ = new Command("printconfig", "prints out the Epic Loot current configuration of the specified type", PrintConfig, GetPrintConfigOptions);
            _ = new Command("magicapi", "exercise the public API: version | query | providers | events | roll [rarity]", ApiDiagnostics, GetApiDiagnosticsOptions, isCheat: true);
            _ = new Command("el-help", "print available epic loot commands", Help, hideFromHelp: true);
        }
    }
}


