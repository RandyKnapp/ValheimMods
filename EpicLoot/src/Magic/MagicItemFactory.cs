using System.Collections.Generic;
using System.Linq;
using EpicLoot.GatedItemType;
using EpicLoot.LegendarySystem;
using UnityEngine;

namespace EpicLoot;

// Fields only Newtonsoft fills in are never assigned as far as the compiler can see.
#pragma warning disable CS0649

// What API.CreateMagicItem is asked for, read from the caller's JSON. The shim's MagicItemRequest
// (EpicLootAPI/src/CreateItem.cs) mirrors it field for field: add fields, never rename one.
internal sealed class MagicItemRequest
{
    public string Prefab;
    // An etched runestone of Rarity instead of a named prefab, so callers never build its name.
    public bool Runestone;
    public int Rarity;
    public List<RequestedEffect> Effects;
    public string LegendaryID;
    public string SetID;
    public string DisplayName;
    public int? SocketCount;
}

internal sealed class RequestedEffect
{
    public string EffectType;
    // Null rolls a value from the item's range, as a loot roll would.
    public float? EffectValue;
}

#pragma warning restore CS0649

// Builds a new item carrying exactly the magic a MagicItemRequest describes. Nothing is checked against the
// rules a roll follows (item type, rarity, exclusivity, whether a unique fits the item): the caller asked for
// this item, so unknown effects and items that never roll magic are written as asked. Only a request that
// cannot be built at all is refused. The item is placed nowhere; the caller adds or drops it.
internal static class MagicItemFactory
{
    internal static ItemDrop.ItemData Create(MagicItemRequest request, out string error)
    {
        error = null;
        if (request == null)
        {
            error = "no request";
            return null;
        }

        if (request.Rarity < 0 || request.Rarity >= Rarities.Count)
        {
            error = $"no rarity {request.Rarity} (0 to {Rarities.Count - 1})";
            return null;
        }

        ItemRarity rarity = (ItemRarity)request.Rarity;

        if (ObjectDB.instance == null)
        {
            error = "ObjectDB is not ready yet";
            return null;
        }

        string prefabName = request.Runestone ? $"EtchedRunestone{rarity}" : request.Prefab;
        GameObject prefab = string.IsNullOrEmpty(prefabName) ? null : ObjectDB.instance.GetItemPrefab(prefabName);
        ItemDrop.ItemData item = prefab != null ? LootRoller.CreateLootItemData(prefab) : null;
        if (item == null)
        {
            error = string.IsNullOrEmpty(prefabName) ? "no Prefab named" : $"no item prefab named '{prefabName}'";
            return null;
        }

        MagicItem magicItem = new MagicItem
        {
            Rarity = rarity,
            LegendaryID = string.IsNullOrEmpty(request.LegendaryID) ? null : request.LegendaryID,
            SetID = string.IsNullOrEmpty(request.SetID) ? null : request.SetID
        };

        // Before the values are rolled: a unique's guaranteed effect values set the range they roll in. A set
        // rune's LegendaryID is only its Set Etch preference, so a rune keeps both IDs as given.
        string uniqueName = request.Runestone ? null : ResolveUniqueAndSet(item, magicItem);

        foreach (RequestedEffect requested in request.Effects ?? new List<RequestedEffect>())
        {
            if (string.IsNullOrEmpty(requested?.EffectType))
            {
                error = "an effect has no EffectType";
                return null;
            }

            float value;
            if (requested.EffectValue.HasValue)
            {
                value = requested.EffectValue.Value;
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    error = $"{requested.EffectType} has no finite value";
                    return null;
                }
            }
            else
            {
                value = RollValue(requested.EffectType, rarity, request.Runestone ? null : magicItem.LegendaryID);
            }

            magicItem.Effects.Add(new MagicItemEffect(requested.EffectType, value));
        }

        if (request.Runestone)
        {
            magicItem.SocketCount = 0;
        }
        else if (request.SocketCount.HasValue)
        {
            // The slot panel and Brokkr's Gift assume this range.
            magicItem.SocketCount = Mathf.Clamp(request.SocketCount.Value, 0, LootRoller.MaxSocketCount);
        }
        else
        {
            magicItem.SocketCount = GatedItemTypeHelper.IsSocketable(item) ? LootRoller.RollSocketCountPerRarity(rarity) : 0;
        }

        // A rune's name is built when it is shown, so it stores none unless asked. Generated names read the
        // effects, so this comes after them.
        if (!string.IsNullOrEmpty(request.DisplayName))
        {
            magicItem.DisplayName = request.DisplayName;
        }
        else if (!request.Runestone)
        {
            magicItem.DisplayName = uniqueName ?? MagicItemNames.GetNameForItem(item, magicItem);
        }

        API.WithChangeReason(API.ChangeReason.Create, () => item.SaveMagicItem(magicItem));
        return item;
    }

    // Fills in the set of a known unique, or the piece of a known set, and returns the unique's name. A set
    // gets the piece Set Etch would make of this item, or its first piece when none fits or the set does not
    // roll at this rarity, since nothing here is refused. Unknown IDs stay as given.
    private static string ResolveUniqueAndSet(ItemDrop.ItemData item, MagicItem magicItem)
    {
        if (!string.IsNullOrEmpty(magicItem.LegendaryID))
        {
            if (!UniqueLegendaryHelper.TryGetLegendaryInfo(magicItem.LegendaryID, out LegendaryInfo unique))
            {
                return null;
            }

            magicItem.SetID ??= UniqueLegendaryHelper.GetSetForLegendaryItem(unique);
            return unique.Name;
        }

        if (string.IsNullOrEmpty(magicItem.SetID) ||
            !UniqueLegendaryHelper.TryGetLegendarySetInfo(magicItem.SetID, out LegendarySetInfo set))
        {
            return null;
        }

        if (!UniqueLegendaryHelper.TryGetSetPieceForItem(set, item, magicItem.Rarity, null, out LegendaryInfo piece))
        {
            piece = UniqueLegendaryHelper.GetDefinedSetPieces(set).FirstOrDefault();
        }

        if (piece == null)
        {
            return null;
        }

        magicItem.LegendaryID = piece.ID;
        return piece.Name;
    }

    private static float RollValue(string effectType, ItemRarity rarity, string legendaryID)
    {
        if (!MagicItemEffectDefinitions.TryGet(effectType, out MagicItemEffectDefinition definition))
        {
            return MagicItemEffect.DefaultValue;
        }

        return LootRoller.RollEffect(definition, rarity,
            MagicItemEffectDefinitions.GetRollRange(definition, rarity, legendaryID)).EffectValue;
    }
}
