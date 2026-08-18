using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void ToggleAlwaysDrop(Terminal.ConsoleEventArgs args)
    {
        EpicLoot.AlwaysDropCheat = !EpicLoot.AlwaysDropCheat;
        args.Context.PrintInfo($"> Always Drop: {EpicLoot.AlwaysDropCheat}");
    }

    private static void ToggleCheatGating(Terminal.ConsoleEventArgs args)
    {
        LootRoller.CheatDisableGating = !LootRoller.CheatDisableGating;
        args.Context.PrintInfo($"> Disable gating for magic item drops: {LootRoller.CheatDisableGating}");
    }
    
    private static void PrintGlobalKeys(Terminal.ConsoleEventArgs args)
    {
        if (ZoneSystem.instance != null)
        {
            args.Context.PrintInfo("> Print Global Keys:");
            foreach (string globalKey in ZoneSystem.instance.GetGlobalKeys())
            {
                args.Context.PrintInfo("> " + globalKey);
            }
        }
        else
        {
            args.Context.PrintError("> ZoneSystem is not available");
        }
    }

    private static void DebugLuck(Terminal.ConsoleEventArgs args)
    {
        var players = Player.s_players;
        if (players != null)
        {
            args.Context.PrintInfo($"> DebugLuckFactor ({players.Count} players)");
            var index = 0;
            foreach (var player in players)
            {
                args.Context.PrintInfo($"{index++}: {player?.GetPlayerName()}: {player?.m_nview?.GetZDO()?.GetInt("el-luk")}");
            }
        }
    }

    private static void DebugTooltip(Terminal.ConsoleEventArgs args)
    {
        GenerateTooltipTest.GenerateInventoryTooltips(false);
        args.Context.PrintInfo("> item tooltips written to disk at: .../BepInEx/config/EpicLoot/TooltipTest");
    }

    private static void DebugVanillaTooltip(Terminal.ConsoleEventArgs args)
    {
        GenerateTooltipTest.GenerateInventoryTooltips(true);
        args.Context.PrintInfo("> item tooltips written to disk at: .../BepInEx/config/EpicLoot/TooltipTest");
    }
    
    private static void CheckStackQuality(Terminal.ConsoleEventArgs args)
    {
        args.Context.PrintInfo("CheckStackQuality");
        if (ObjectDB.instance == null)
        {
            args.Context.PrintWarning("> ObjectDB is null");
            return;
        }

        int count = 0;
        for (var index = 0; index < ObjectDB.instance.m_items.Count; ++index)
        {
            GameObject itemPrefab = ObjectDB.instance.m_items[index];
            if (!itemPrefab.TryGetComponent(out ItemDrop itemDrop))
            {
                continue;
            }

            ItemDrop.ItemData itemData = itemDrop.m_itemData;

            if (itemData.m_shared.m_maxStackSize > 1 && itemData.m_shared.m_maxQuality > 1)
            {
                count++;
                args.Context.PrintInfo($"> {itemDrop.name}");
            }
        }

        if (count == 0)
        {
            args.Context.PrintError("> (none)");
        }
    }
    
    private static void Help(Terminal.ConsoleEventArgs args)
    {
        foreach (var kvp in _commands)
        {
            if (kvp.Value.hideFromHelp) continue;
            var str = $"<color={HEX_SoftBlue}>{kvp.Key} - {kvp.Value.Description}</color>";
            if (kvp.Value.OnlyAdmin) str += $" <color={HEX_Amber}>( Only Admin )</color>";
            args.Context.AddString(str);
        }
    }
}