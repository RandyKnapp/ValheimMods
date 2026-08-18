using EpicLoot.LegendarySystem;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot;

/// <summary>
/// Generating magic items and extending the loot tables from another plugin. This is the supported
/// replacement for compiling against a publicized <c>EpicLoot.dll</c> and calling <c>LootRoller</c>
/// directly, which breaks on every internal refactor.
/// </summary>
public static partial class API
{
    /// <summary>
    /// Rolls a magic item of the given rarity and stamps it onto <paramref name="item"/>, reproducing
    /// the whole drop flow: effect selection, socket count, legendary/mythic assignment, randomized
    /// wear, and the display name.
    /// </summary>
    /// <param name="item">The item to enchant in place. Must satisfy <see cref="CanBeMagicItem"/>.</param>
    /// <param name="rarity">rarity ordinal</param>
    /// <param name="luck">Luck factor, as returned by <see cref="GetLuckFactor"/>. Pass 0 for none.</param>
    /// <param name="legendaryID">Optional legendary/mythic id to force. Ignored when empty; the roll
    /// fails rather than silently downgrading if the id is unknown or its requirements do not fit
    /// <paramref name="item"/>.</param>
    /// <returns>true if the item is now magic</returns>
    [PublicAPI]
    public static bool TryMakeMagicItem(ItemDrop.ItemData item, int rarity, float luck, string legendaryID)
    {
        if (!EpicLoot.CanBeMagicItem(item) || !TryToRarity(rarity, out ItemRarity value))
        {
            return false;
        }

        try
        {
            MagicItem magicItem = LootRoller.RollMagicItem(value, item, luck);
            if (magicItem == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(legendaryID))
            {
                if (!UniqueLegendaryHelper.TryGetLegendaryInfo(legendaryID, out LegendaryInfo info)
                    || !info.Requirements.CheckRequirements(item, magicItem))
                {
                    OnError?.Invoke($"TryMakeMagicItem: legendary '{legendaryID}' is unknown or does not fit {item.m_shared.m_name}");
                    return false;
                }

                magicItem.LegendaryID = legendaryID;
                magicItem.DisplayName = info.Name;
                magicItem.SetID = UniqueLegendaryHelper.GetSetForLegendaryItem(info);
            }

            WithChangeReason(ChangeReason.LootRoll, () => item.SaveMagicItem(magicItem));
            LootRoller.InitializeMagicItem(item);
            RaiseLootGenerated(item);
            return true;
        }
        catch (Exception ex)
        {
            EpicLoot.LogErrorForce($"[EpicLoot.API] TryMakeMagicItem failed for {item.m_shared?.m_name}: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Rolls a magic item without applying it, for callers that want to inspect or adjust it first.
    /// </summary>
    /// <param name="rarity">rarity ordinal</param>
    /// <param name="item">The item the roll is for; effect selection depends on it.</param>
    /// <param name="luck">Luck factor, 0 for none</param>
    /// <returns>JSON serialized MagicItem, or "" if the roll was not possible</returns>
    [PublicAPI]
    public static string RollMagicItemJson(int rarity, ItemDrop.ItemData item, float luck)
    {
        if (item == null || !TryToRarity(rarity, out ItemRarity value))
        {
            return "";
        }

        try
        {
            MagicItem magicItem = LootRoller.RollMagicItem(value, item, luck);
            return magicItem == null ? "" : JsonConvert.SerializeObject(magicItem);
        }
        catch (Exception ex)
        {
            EpicLoot.LogErrorForce($"[EpicLoot.API] RollMagicItemJson failed: {ex}");
            return "";
        }
    }

    /// <summary>
    /// Applies previously rolled or hand-authored magic data to an item.
    /// </summary>
    /// <param name="item">The item to write to</param>
    /// <param name="magicItemJson">JSON serialized MagicItem, as returned by
    /// <see cref="RollMagicItemJson"/> or <see cref="GetMagicItemJson"/></param>
    /// <returns>true if applied</returns>
    [PublicAPI]
    public static bool ApplyMagicItemJson(ItemDrop.ItemData item, string magicItemJson)
    {
        if (item == null || string.IsNullOrEmpty(magicItemJson))
        {
            return false;
        }

        try
        {
            MagicItem magicItem = JsonConvert.DeserializeObject<MagicItem>(magicItemJson);
            if (magicItem == null)
            {
                return false;
            }

            item.SaveMagicItem(magicItem);
            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"ApplyMagicItemJson: could not parse magic item json: {ex.Message}");
            return false;
        }
    }

    /// <param name="position">World position the loot is dropping at</param>
    /// <returns>The luck factor in effect there, from the world luck setting and nearby players' Luck
    /// magic effects.</returns>
    [PublicAPI]
    public static float GetLuckFactor(Vector3 position)
    {
        return LootRoller.GetLuckFactor(position);
    }

    /// <param name="rarity">rarity ordinal</param>
    /// <returns>How many effects a fresh roll of this rarity would get; 0 for an unknown rarity.</returns>
    [PublicAPI]
    public static int RollEffectCountForRarity(int rarity)
    {
        return TryToRarity(rarity, out ItemRarity value) ? LootRoller.RollEffectCountPerRarity(value) : 0;
    }

    /// <param name="rarity">rarity ordinal; only Legendary and Mythic have entries</param>
    /// <returns>Every registered legendary/mythic id at that rarity, including API-registered ones.</returns>
    [PublicAPI]
    public static List<string> GetLegendaryIDs(int rarity)
    {
        if (!TryToRarity(rarity, out ItemRarity value))
        {
            return new List<string>();
        }

        switch (value)
        {
            case ItemRarity.Legendary:
                return UniqueLegendaryHelper.LegendaryInfo.Keys.ToList();
            case ItemRarity.Mythic:
                return UniqueLegendaryHelper.MythicInfo.Keys.ToList();
            default:
                return new List<string>();
        }
    }

    /// <param name="legendaryID">A legendary or mythic id</param>
    /// <returns>JSON serialized LegendaryInfo, or "" if unknown</returns>
    [PublicAPI]
    public static string GetLegendaryInfoJson(string legendaryID)
    {
        if (string.IsNullOrEmpty(legendaryID)
            || !UniqueLegendaryHelper.TryGetLegendaryInfo(legendaryID, out LegendaryInfo info))
        {
            return "";
        }

        return JsonConvert.SerializeObject(info);
    }

    /// <summary>
    /// The effect types that could legally be rolled onto this item, honoring rarity, item type, skill,
    /// exclusivity and any requirement registered via <see cref="RegisterMagicEffectRequirement"/>.
    /// </summary>
    /// <param name="item">The candidate item</param>
    /// <param name="magicItemJson">The magic data the item would have; pass "" for an empty roll of
    /// <paramref name="rarity"/></param>
    /// <param name="rarity">rarity ordinal, used only when <paramref name="magicItemJson"/> is empty</param>
    /// <returns>The available effect type ids</returns>
    [PublicAPI]
    public static List<string> GetAvailableEffectTypes(ItemDrop.ItemData item, string magicItemJson, int rarity)
    {
        if (item == null)
        {
            return new List<string>();
        }

        try
        {
            MagicItem magicItem = string.IsNullOrEmpty(magicItemJson)
                ? null
                : JsonConvert.DeserializeObject<MagicItem>(magicItemJson);

            if (magicItem == null)
            {
                if (!TryToRarity(rarity, out ItemRarity value))
                {
                    return new List<string>();
                }

                magicItem = new MagicItem { Rarity = value };
            }

            return MagicItemEffectDefinitions.GetAvailableEffects(item, magicItem)
                .Select(x => x.Type)
                .ToList();
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"GetAvailableEffectTypes failed: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Adds a loot table at runtime. Registrations are cached and re-applied whenever loottables.json
    /// reloads or a dedicated server pushes its copy, so they survive both.
    /// </summary>
    /// <param name="json">JSON serialized <see cref="LootTable"/> or an array of them</param>
    /// <returns>unique key if added, else null</returns>
    [PublicAPI]
    public static string AddLootTables(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            List<LootTable> tables = json.TrimStart().StartsWith("[")
                ? JsonConvert.DeserializeObject<List<LootTable>>(json)
                : new List<LootTable> { JsonConvert.DeserializeObject<LootTable>(json) };

            tables?.RemoveAll(x => x == null);
            if (tables == null || tables.Count == 0)
            {
                return null;
            }

            ExternalLootTables.AddRange(tables);
            LootRoller.AddLootTables(tables);
            return RuntimeRegistry.Register(tables);
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Failed to parse loot table passed in through external plugin: {ex.Message}");
            return null;
        }
    }

    /// <param name="key">unique identifier returned by <see cref="AddLootTables"/></param>
    /// <param name="json">JSON serialized list of <see cref="LootTable"/></param>
    /// <returns>True if updated</returns>
    [PublicAPI]
    public static bool UpdateLootTables(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out List<LootTable> existing))
        {
            return false;
        }

        List<LootTable> tables = JsonConvert.DeserializeObject<List<LootTable>>(json);
        if (tables == null)
        {
            return false;
        }

        // AddLootTable appends, so the previous instances have to be dropped by reference first or both
        // versions would roll.
        LootRoller.RemoveLootTables(existing);
        ExternalLootTables.RemoveAll(existing);

        ExternalLootTables.AddRange(tables);
        existing.Clear();
        existing.AddRange(tables);

        LootRoller.AddLootTables(tables);
        return true;
    }

    private static readonly List<LootTable> ExternalLootTables = new();

    /// <summary>
    /// Re-applies cached external loot tables into <see cref="LootRoller.LootTables"/>. Subscribed to
    /// <see cref="EpicLoot.LootTableLoaded"/> in the API static constructor.
    /// </summary>
    private static void ReloadExternalLootTables()
    {
        if (ExternalLootTables.Count == 0)
        {
            return;
        }

        LootRoller.AddLootTables(ExternalLootTables);
        OnReload?.Invoke("Reloaded external loot tables");
    }
}
