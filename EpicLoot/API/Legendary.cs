using EpicLoot.LegendarySystem;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EpicLoot;

public static partial class API
{
    /// <param name="type">A rarity name (or ordinal). It is the rarity the unique rolls at when the json
    /// carries no "Rarities" list of its own.</param>
    /// <param name="json">Serialized LegendaryInfo</param>
    /// <returns>A key for <see cref="UpdateLegendaryItem"/>, or null on failure</returns>
    [PublicAPI]
    public static string AddLegendaryItem(string type, string json)
    {
        try
        {
            if (!TryParseRarity(type, out ItemRarity rarity))
            {
                OnError?.Invoke($"AddLegendaryItem: unknown rarity '{type}'.");
                return null;
            }

            LegendaryInfo config = JsonConvert.DeserializeObject<LegendaryInfo>(json);

            if (config == null)
            {
                return null;
            }

            UniqueLegendaryHelper.RegisterUnique(config, rarity);
            ExternalLegendaryItems.Add(config);
            return RuntimeRegistry.Register(config);
        }
        catch
        {
            OnError?.Invoke("Failed to parse legendary item from external plugin.");
            return null;
        }
    }

    [PublicAPI]
    public static bool UpdateLegendaryItem(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out LegendaryInfo legendaryInfo))
        {
            return false;
        }

        try
        {
            LegendaryInfo config = JsonConvert.DeserializeObject<LegendaryInfo>(json);
            if (config == null)
            {
                return false;
            }

            List<ItemRarity> rarities = legendaryInfo.Rarities;
            legendaryInfo.CopyFieldsFrom(config);
            // An update that does not mention rarities keeps the ones the unique was registered with.
            if (legendaryInfo.Rarities == null || legendaryInfo.Rarities.Count == 0)
            {
                legendaryInfo.Rarities = rarities;
            }

            UniqueLegendaryHelper.RefreshDerived();
            return true;
        }
        catch
        {
            OnError?.Invoke("Failed to parse legendary item from external plugin");
            return false;
        }
    }

    /// <param name="type">A rarity name (or ordinal). It is the rarity the set's pieces roll at when the
    /// json carries no "Rarities" list of its own.</param>
    /// <param name="json">Serialized LegendarySetInfo</param>
    /// <returns>A key for <see cref="UpdateLegendarySet"/>, or null on failure</returns>
    [PublicAPI]
    public static string AddLegendarySet(string type, string json)
    {
        try
        {
            if (!TryParseRarity(type, out ItemRarity rarity))
            {
                OnError?.Invoke($"AddLegendarySet: unknown rarity '{type}'.");
                return null;
            }

            LegendarySetInfo config = JsonConvert.DeserializeObject<LegendarySetInfo>(json);

            if (config == null)
            {
                return null;
            }

            UniqueLegendaryHelper.RegisterSet(config, rarity);
            ExternalLegendarySets.Add(config);
            return RuntimeRegistry.Register(config);
        }
        catch
        {
            OnError?.Invoke("Failed to parse legendary set from external plugin.");
            return null;
        }
    }

    [PublicAPI]
    public static bool UpdateLegendarySet(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out LegendarySetInfo legendarySetInfo))
        {
            return false;
        }

        try
        {
            LegendarySetInfo config = JsonConvert.DeserializeObject<LegendarySetInfo>(json);
            if (config == null)
            {
                return false;
            }

            List<ItemRarity> rarities = legendarySetInfo.Rarities;
            legendarySetInfo.CopyFieldsFrom(config);
            if (legendarySetInfo.Rarities == null || legendarySetInfo.Rarities.Count == 0)
            {
                legendarySetInfo.Rarities = rarities;
            }

            // Rebuilds the piece-to-set map, so a changed LegendaryIDs list takes effect immediately.
            UniqueLegendaryHelper.RefreshDerived();
            return true;
        }
        catch
        {
            OnError?.Invoke("Failed to parse legendary set from external plugin");
            return false;
        }
    }

    private static bool TryParseRarity(string type, out ItemRarity rarity)
    {
        return Enum.TryParse(type, true, out rarity) && Enum.IsDefined(typeof(ItemRarity), rarity);
    }
}
