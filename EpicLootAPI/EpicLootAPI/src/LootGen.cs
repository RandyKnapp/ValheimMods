using JetBrains.Annotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLootAPI;

/// <summary>
/// Generating magic items and extending loot tables. The supported replacement for compiling against a
/// publicized EpicLoot.dll and calling <c>LootRoller</c> directly.
/// </summary>
public static partial class EpicLoot
{
    private static readonly Method API_TryMakeMagicItem = new("TryMakeMagicItem");
    private static readonly Method API_RollMagicItemJson = new("RollMagicItemJson");
    private static readonly Method API_ApplyMagicItemJson = new("ApplyMagicItemJson");
    private static readonly Method API_GetLuckFactor = new("GetLuckFactor");
    private static readonly Method API_RollEffectCountForRarity = new("RollEffectCountForRarity");
    private static readonly Method API_GetLegendaryIDs = new("GetLegendaryIDs");
    private static readonly Method API_GetLegendaryInfoJson = new("GetLegendaryInfoJson");
    private static readonly Method API_GetAvailableEffectTypes = new("GetAvailableEffectTypes");
    private static readonly Method API_AddLootTables = new("AddLootTables");
    private static readonly Method API_UpdateLootTables = new("UpdateLootTables");

    /// <summary>
    /// Rolls a magic item of the given rarity and stamps it onto <paramref name="item"/>, reproducing
    /// the whole drop flow: effect selection, socket count, legendary assignment, randomized wear, and
    /// the display name.
    /// </summary>
    /// <param name="item">The item to enchant in place. Must satisfy <see cref="CanBeMagicItem"/>.</param>
    /// <param name="rarity">Target rarity</param>
    /// <param name="luck">Luck factor, from <see cref="GetLuckFactor"/>. 0 for none.</param>
    /// <param name="legendaryID">Optional legendary/mythic id to force. The roll fails rather than
    /// silently downgrading if the id is unknown or does not fit the item.</param>
    /// <returns>true if the item is now magic</returns>
    [PublicAPI]
    public static bool TryMakeMagicItem(this ItemDrop.ItemData item, ItemRarity rarity, float luck = 0f,
        string? legendaryID = null)
    {
        object[] result = API_TryMakeMagicItem.Invoke(item, (int)rarity, luck, legendaryID ?? "");
        bool output = (bool)(result[0] ?? false);
        logger.LogDebug($"Made magic item: {item?.m_shared?.m_name}, {rarity}, {output}");
        return output;
    }

    /// <summary>
    /// Rolls a magic item without applying it, so you can inspect or adjust it first. Apply it with
    /// <see cref="ApplyMagicItem"/>.
    /// </summary>
    /// <param name="rarity">Target rarity</param>
    /// <param name="item">The item the roll is for; effect selection depends on it</param>
    /// <param name="luck">Luck factor, 0 for none</param>
    /// <returns>The rolled magic item, or null if the roll was not possible</returns>
    [PublicAPI]
    public static MagicItem? RollMagicItem(ItemRarity rarity, ItemDrop.ItemData item, float luck = 0f)
    {
        string json = (string)(API_RollMagicItemJson.Invoke((int)rarity, item, luck)[0] ?? "");
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<MagicItem>(json);
        }
        catch
        {
            logger.LogWarning("Failed to parse rolled magic item");
            return null;
        }
    }

    /// <summary>Applies rolled or hand-authored magic data to an item.</summary>
    /// <returns>true if applied</returns>
    [PublicAPI]
    public static bool ApplyMagicItem(this ItemDrop.ItemData item, MagicItem magicItem)
    {
        if (magicItem == null)
        {
            return false;
        }

        string json = JsonConvert.SerializeObject(magicItem);
        return (bool)(API_ApplyMagicItemJson.Invoke(item, json)[0] ?? false);
    }

    /// <param name="position">World position the loot is dropping at</param>
    /// <returns>The luck factor in effect there, from the world luck setting and nearby players' Luck
    /// magic effects.</returns>
    [PublicAPI]
    public static float GetLuckFactor(Vector3 position)
    {
        return (float)(API_GetLuckFactor.Invoke(position)[0] ?? 0f);
    }

    /// <returns>How many effects a fresh roll of this rarity would get.</returns>
    [PublicAPI]
    public static int RollEffectCountForRarity(ItemRarity rarity)
    {
        return (int)(API_RollEffectCountForRarity.Invoke((int)rarity)[0] ?? 0);
    }

    /// <param name="rarity">Only Legendary and Mythic have entries</param>
    /// <returns>Every registered legendary/mythic id at that rarity, including ones other plugins added.</returns>
    [PublicAPI]
    public static List<string> GetLegendaryIDs(ItemRarity rarity)
    {
        return (List<string>)(API_GetLegendaryIDs.Invoke((int)rarity)[0] ?? new List<string>());
    }

    /// <param name="legendaryID">A legendary or mythic id</param>
    /// <returns>The legendary definition, or null if unknown</returns>
    [PublicAPI]
    public static LegendaryInfo? GetLegendaryInfo(string legendaryID)
    {
        string json = (string)(API_GetLegendaryInfoJson.Invoke(legendaryID)[0] ?? "");
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<LegendaryInfo>(json);
        }
        catch
        {
            logger.LogWarning($"Failed to parse legendary info for {legendaryID}");
            return null;
        }
    }

    /// <summary>
    /// The effect types that could legally be rolled onto this item, honoring rarity, item type, skill,
    /// exclusivity and any registered external requirement.
    /// </summary>
    /// <param name="item">The candidate item</param>
    /// <param name="rarity">Rarity to evaluate against when <paramref name="magicItem"/> is null</param>
    /// <param name="magicItem">The magic data the item would have; null for an empty roll</param>
    [PublicAPI]
    public static List<string> GetAvailableEffectTypes(this ItemDrop.ItemData item, ItemRarity rarity,
        MagicItem? magicItem = null)
    {
        string json = magicItem == null ? "" : JsonConvert.SerializeObject(magicItem);
        return (List<string>)(API_GetAvailableEffectTypes.Invoke(item, json, (int)rarity)[0]
                              ?? new List<string>());
    }

    /// <summary>
    /// Adds loot tables at runtime. Registrations are cached by Epic Loot and re-applied whenever
    /// loottables.json reloads or a dedicated server pushes its copy, so they survive both.
    /// </summary>
    /// <param name="json">JSON serialized loot table, or an array of them</param>
    /// <returns>true if added</returns>
    [PublicAPI]
    public static bool AddLootTables(string json)
    {
        object[] result = API_AddLootTables.Invoke(json);
        if (result[0] is not string key)
        {
            return false;
        }

        RunTimeRegistry.Register(json, key);
        logger.LogDebug("Registered external loot tables");
        return true;
    }

    /// <summary>
    /// Replaces previously added loot tables. Pass the exact same <paramref name="originalJson"/> string
    /// instance you handed to <see cref="AddLootTables"/>.
    /// </summary>
    /// <returns>true if updated</returns>
    [PublicAPI]
    public static bool UpdateLootTables(string originalJson, string json)
    {
        if (!RunTimeRegistry.TryGetValue(originalJson, out string key))
        {
            return false;
        }

        return (bool)(API_UpdateLootTables.Invoke(key, json)[0] ?? false);
    }
}
