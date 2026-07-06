using EpicLoot.LegendarySystem;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;

namespace EpicLoot;

public static partial class API
{
    [PublicAPI]
    public static string AddLegendaryItem(string type, string json)
    {
        try
        {
            if (!Enum.TryParse(type, true, out ItemRarity rarity))
            {
                return null;
            }

            LegendaryInfo config = JsonConvert.DeserializeObject<LegendaryInfo>(json);

            if (config == null)
            {
                return null;
            }

            switch (rarity)
            {
                case ItemRarity.Legendary:
                    UniqueLegendaryHelper.Config.LegendaryItems.Add(config);
                    UniqueLegendaryHelper.LegendaryInfo[config.ID] = config;
                    break;
                case ItemRarity.Mythic:
                    UniqueLegendaryHelper.Config.MythicItems.Add(config);
                    UniqueLegendaryHelper.MythicInfo[config.ID] = config;
                    break;
            }

            ExternalLegendaryItems.AddOrSet(rarity, config);
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

        LegendaryInfo config = JsonConvert.DeserializeObject<LegendaryInfo>(json);
        if (config == null)
        {
            return false;
        }

        legendaryInfo.CopyFieldsFrom(config);
        return true;
    }

    [PublicAPI]
    public static string AddLegendarySet(string type, string json)
    {
        try
        {
            if (!Enum.TryParse(type, true, out ItemRarity rarity))
            {
                return null;
            }

            LegendarySetInfo config = JsonConvert.DeserializeObject<LegendarySetInfo>(json);

            if (config == null)
            {
                return null;
            }

            switch (rarity)
            {
                case ItemRarity.Legendary:
                    UniqueLegendaryHelper.LegendarySets[config.ID] = config;
                    UniqueLegendaryHelper.Config.LegendarySets.Add(config);
                    foreach (var name in config.LegendaryIDs)
                    {
                        UniqueLegendaryHelper.LegendaryItemsToSetMap[name] = config;
                    }
                    break;
                case ItemRarity.Mythic:
                    UniqueLegendaryHelper.MythicSets[config.ID] = config;
                    UniqueLegendaryHelper.Config.MythicSets.Add(config);
                    foreach (var name in config.LegendaryIDs)
                    {
                        UniqueLegendaryHelper.MythicItemsToSetMap[name] = config;
                    }
                    break;
            }

            ExternalLegendarySets.AddOrSet(rarity, config);
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

        LegendarySetInfo config = JsonConvert.DeserializeObject<LegendarySetInfo>(json);
        if (config == null)
        {
            return false;
        }

        legendarySetInfo.CopyFieldsFrom(config);
        return true;
    }
}