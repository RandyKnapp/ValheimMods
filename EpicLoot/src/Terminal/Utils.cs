using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager
{
    private const string HEX_Gray = "#B2BEB5";
    private const string HEX_SoftBlue = "#60A5FA";
    private const string HEX_Green = "#34D399";
    private const string HEX_Amber = "#FBBF24";
    private const string HEX_Red = "#F87171";
    private const float ITEM_NAME_REFRESH_INTERVAL = 60.0f;
    private const float CREATURE_NAME_REFRESH_INTERVAL = 600.0f;

    private static readonly List<string> _tempAllItemNames = [];
    private static float lastAllItemNameTime;
    private static readonly List<string> _tempAllCreatureNames = [];
    private static float lastCreatureNameTime;
    private static List<string> GetValidMagicItemNames()
    {
        if (ObjectDB.instance == null)
        {
            return _tempAllItemNames;
        }
        
        if (_tempAllItemNames.Count <= 0 || Time.time - lastAllItemNameTime > ITEM_NAME_REFRESH_INTERVAL)
        {
            _tempAllItemNames.Clear();
            _tempAllItemNames.AddRange(ObjectDB.instance.m_items
                .Where(x => EpicLoot.CanBeMagicItem(x.GetComponent<ItemDrop>().m_itemData))
                .Where(x => x.name != "HelmetDverger" && x.name != "BeltStrength" && x.name != "Wishbone")
                .Select(x => x.name));
            lastAllItemNameTime = Time.time;
        }
        return  _tempAllItemNames;
    }

    private static List<string> GetCreatureNames()
    {
        if (ZNetScene.instance == null) return _tempAllCreatureNames;
        if (_tempAllCreatureNames.Count <= 0 || Time.time - lastCreatureNameTime > CREATURE_NAME_REFRESH_INTERVAL)
        {
            _tempAllCreatureNames.Clear();
            _tempAllCreatureNames.AddRange(ZNetScene.instance.m_prefabs
                .Where(p => p.GetComponent<Character>())
                .Select(p => p.name));
            lastCreatureNameTime = Time.time;
        }
        return _tempAllCreatureNames;
    }
    
    private static void PrintInfo(this Terminal terminal, string msg) => terminal.Print(HEX_Green, msg);
    private static void PrintWarning(this Terminal terminal, string msg) => terminal.Print(HEX_Amber, msg);
    private static void PrintError(this Terminal terminal, string msg) => terminal.Print(HEX_Red, msg);
    private static void PrintDebug(this Terminal terminal, string msg) => terminal.Print(HEX_SoftBlue, msg);
    private static void Print(this Terminal terminal, string hex, string msg)
    {
        if (terminal == null) return;
        terminal.AddString($"<color={hex}>{msg}</color>");
    }

    private static string GetString(this Terminal.ConsoleEventArgs args, int index, string defaultValue = "") => args.Args.GetString(index, defaultValue);

    private static string GetString(this string[] args, int index, string defaultValue = "")
    {
        if (args.Length < index + 1) return defaultValue;
        return args[index];;
    }
    
    private static float GetFloat(this string[] args, int index, float defaultValue = 0f)
    {
        if (args.Length < index + 1) return defaultValue;
        string arg = args[index];
        return float.TryParse(arg, out float result) ? result : defaultValue;
    }
    
    private static int GetInt(this string[] args, int index, int defaultValue = 0)
    {
        if (args.Length < index + 1) return defaultValue;
        string arg = args[index];
        return int.TryParse(arg, out int result) ? result : defaultValue;
    }

    private static string GetStringFrom(this Terminal.ConsoleEventArgs args, int index, string defaultValue = "") => args.Args.GetStringFrom(index, defaultValue);

    private static string GetStringFrom(this string[] args, int index, string defaultValue = "")
    {
        if (args.Length < index + 1) return defaultValue;
        return string.Join(" ", args.Skip(index));
    }

    private static T GetEnum<T>(this Terminal.ConsoleEventArgs args, int index, T defaultValue) where T : struct, Enum =>
        args.Args.GetEnum(index, defaultValue);

    private static T GetEnum<T>(this string[] args, int index, T defaultValue) where T : struct, Enum
    {
        if (args.Length < index + 1) return defaultValue;
        var arg = args.GetString(index);
        return Enum.TryParse(arg, true, out T result) ? result : defaultValue;
    }
    
    private static float[] GetRarityTable(string rarityName)
    {
        return rarityName.ToLowerInvariant() switch
        {
            "magic" => [1, 0, 0, 0, 0],
            "rare" => [0, 1, 0, 0, 0],
            "epic" => [0, 0, 1, 0, 0],
            "legendary" => [0, 0, 0, 1, 0],
            "mythic" => [0, 0, 0, 0, 1],
            _ => [1, 1, 1, 1, 1]
        };
    }
}