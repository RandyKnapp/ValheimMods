using System.Collections.Generic;
using HarmonyLib;

namespace EpicLoot;

public static partial class TerminalManager
{
    internal static readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>();

    // Vanilla only ever completes the *second* token of the input: both Terminal.tabCycle and
    // Terminal.updateSearch are handed input.Split(' ')[1] as the word and the command's single
    // cached option list, no matter how much has been typed. These two patches re-point both at the
    // token the caret is actually in and ask the command for that argument's options, so completion
    // works for every argument rather than only the first.

    /// <summary>
    /// Resolves the argument the caret is sitting in and the options available for it. Returns false
    /// when the input does not name an Epic Loot command, leaving vanilla completion untouched.
    /// </summary>
    private static bool TryGetCaretArgument(Terminal terminal, out string word, out List<string> options)
    {
        word = string.Empty;
        options = [];

        var input = terminal == null ? null : terminal.m_input;
        if (input == null) return false;

        string text = input.text;
        string[] tokens = text.Split(' ');
        if (tokens.Length < 2) return false;

        string name = terminal.m_tabPrefix == char.MinValue ? tokens[0] : tokens[0].Substring(1);
        if (name.Length == 0 || !_commands.TryGetValue(name, out Command command)) return false;

        int caret = input.caretPosition;
        if (caret < 0) caret = 0;
        if (caret > text.Length) caret = text.Length;

        // Walk the tokens until the caret falls at or before a token's last character. A caret sitting
        // on a separating space belongs to the token that follows it, which is what makes completion
        // work on a trailing space ("magicitem rare |").
        int argIndex = tokens.Length - 1;
        int tokenStart = 0;
        for (int i = 0; i < tokens.Length; ++i)
        {
            if (caret <= tokenStart + tokens[i].Length)
            {
                argIndex = i;
                break;
            }

            tokenStart += tokens[i].Length + 1;
        }

        // argIndex 0 means the caret is back on the command name; vanilla would "complete" it by
        // splicing an argument over the top of it, so hand back an empty list to suppress that.
        word = argIndex == 0 ? string.Empty : tokens[argIndex];
        options = command.GetTabOptions(tokens, argIndex);
        return true;
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.tabCycle))]
    private static class Terminal_TabCycle_Patch
    {
        // word drives both the prefix filter and m_tabLength, which is how tabCycle works out how much
        // of the existing text to splice over - so it has to describe the caret's token, not token 1.
        private static void Prefix(Terminal __instance, bool usePrefix, ref string word, ref List<string> options)
        {
            if (usePrefix) return;
            if (!TryGetCaretArgument(__instance, out string caretWord, out List<string> caretOptions)) return;

            word = caretWord;
            options = caretOptions;
        }
    }

    [HarmonyPatch(typeof(Terminal), nameof(Terminal.updateSearch))]
    private static class Terminal_UpdateSearch_Patch
    {
        // The suggestion strip above the input needs the same treatment, otherwise it keeps offering
        // the first argument's values. It also backs tabCycle's no-prefix-match fallback, which would
        // otherwise splice a rarity into an item slot.
        private static void Prefix(Terminal __instance, bool usePrefix, ref string word, ref List<string> options)
        {
            if (usePrefix) return;
            if (!TryGetCaretArgument(__instance, out string caretWord, out List<string> caretOptions)) return;

            word = caretWord;
            options = caretOptions;
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
            _ = new Command("magicshards", "spawn all shardstones: [rarity] (random valid rarity per shard if omitted)", SpawnMagicShards, GetMagicShardOptions, isCheat: true);
            _ = new Command("alwaysdrop", "toggle always drop", ToggleAlwaysDrop, isCheat: true);
            _ = new Command("cheatgating", "toggle cheat gating", ToggleCheatGating, isCheat: true);
            _ = new Command("cheatsockets", "forces the provided number of sockets to always roll onto drops", CheatSockets, isCheat: true);
            _ = new Command("testtreasuremap", "spawns treasure chests and adds to adventure map", TestTreasureMap,  isCheat: true, alternates: "testtm");
            _ = new Command("resettreasuremap", "removes all active treasure maps", ResetTreasureMap,  isCheat: true, alternates: "resettm");
            _ = new Command("debugtreasuremap", "toggle treasure map debug mode", DebugTreasureMap,  isCheat: true, alternates: "debugtm");
            _ = new Command("resetbounties", "removes active bounties", ResetBounties,  isCheat: true);
            _ = new Command("testbountynames", "print randomly generated bounty names: [amount]", TestBountyNames,  isCheat: true);
            _ = new Command("resetadventure", "clear player adventure data", ResetAdventure,  isCheat: true);
            _ = new Command("adventureindex", "print or rebuild the world biome index: [rebuild]", AdventureIndexInfo,  isCheat: true);
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


