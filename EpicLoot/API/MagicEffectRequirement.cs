using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace EpicLoot;

public static partial class API
{
    private static readonly Dictionary<string, Func<ItemDrop.ItemData, object, string, bool, bool, bool, bool>> ExternalMagicEffectRequirements = new();

    /// <summary>
    /// Registers a magic effect requirement predicate for a Requirements.ExternalRequirements value.
    /// </summary>
    /// <param name="externalRequirement">The ExternalRequirements value used by magic effect JSON.</param>
    /// <param name="requirement">
    /// Predicate arguments are item data, magic item, magic effect type, check loot roll, check augment roll, and check rune roll.
    /// </param>
    /// <returns>true if the requirement was registered</returns>
    [PublicAPI]
    public static bool RegisterMagicEffectRequirement(string externalRequirement, Func<ItemDrop.ItemData, object, string, bool, bool, bool, bool> requirement)
    {
        if (string.IsNullOrWhiteSpace(externalRequirement))
        {
            OnError?.Invoke("Failed to register magic effect requirement: external requirement is empty");
            return false;
        }

        if (requirement == null)
        {
            OnError?.Invoke($"Failed to register magic effect requirement '{externalRequirement}': requirement is null");
            return false;
        }

        if (ExternalMagicEffectRequirements.ContainsKey(externalRequirement))
        {
            OnError?.Invoke($"Duplicate magic effect requirement: {externalRequirement}");
            return false;
        }

        ExternalMagicEffectRequirements[externalRequirement] = requirement;
        OnReload?.Invoke($"Registered magic effect requirement: {externalRequirement}");
        return true;
    }

    internal static bool CheckMagicEffectExternalRequirements(
        List<string> externalRequirements,
        ItemDrop.ItemData itemData,
        MagicItem magicItem,
        string magicEffectType,
        bool checklootroll,
        bool checkaugmentroll,
        bool checkruneroll)
    {
        if (externalRequirements == null || externalRequirements.Count == 0)
        {
            return true;
        }

        foreach (string externalRequirement in externalRequirements)
        {
            if (string.IsNullOrWhiteSpace(externalRequirement))
            {
                OnError?.Invoke($"Magic effect '{magicEffectType}' has an empty external requirement");
                return false;
            }

            if (!ExternalMagicEffectRequirements.TryGetValue(externalRequirement, out Func<ItemDrop.ItemData, object, string, bool, bool, bool, bool> requirement))
            {
                OnError?.Invoke($"Magic effect '{magicEffectType}' requires unregistered external requirement '{externalRequirement}'");
                return false;
            }

            try
            {
                if (!requirement(itemData, magicItem, magicEffectType, checklootroll, checkaugmentroll, checkruneroll))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Magic effect requirement '{externalRequirement}' failed for '{magicEffectType}': {ex.Message}");
                return false;
            }
        }

        return true;
    }
}
