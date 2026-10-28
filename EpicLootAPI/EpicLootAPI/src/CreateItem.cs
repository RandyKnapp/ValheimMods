using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EpicLootAPI;

/// <summary>
/// What to create with <see cref="EpicLoot.CreateMagicItem(MagicItemRequest, out string)"/>. Mirrors Epic Loot's
/// own request (EpicLoot/src/Magic/MagicItemFactory.cs) field for field.
/// </summary>
[Serializable]
[PublicAPI]
public class MagicItemRequest
{
    /// <summary>The item prefab to create, e.g. "SwordIron". Ignored when <see cref="Runestone"/> is set.</summary>
    public string? Prefab;

    /// <summary>Create an etched runestone of <see cref="Rarity"/> instead of a named prefab.</summary>
    public bool Runestone;

    public ItemRarity Rarity;

    /// <summary>Written in this order. Unknown effect types are kept, and show as unknown in the tooltip.</summary>
    public List<RequestedEffect> Effects = new();

    /// <summary>A unique or set piece. A known one also sets <see cref="SetID"/> and the item's name.</summary>
    public string? LegendaryID;

    /// <summary>
    /// A set to join. With no <see cref="LegendaryID"/>, Epic Loot picks the piece this item fits, the way Set
    /// Etch does, or the set's first piece when none fits. On a runestone with no effects this makes a set rune.
    /// </summary>
    public string? SetID;

    /// <summary>Null for the unique's name, else a generated one. Runestones are named when shown.</summary>
    public string? DisplayName;

    /// <summary>Shard slots; null rolls them as loot would. Runestones always get none.</summary>
    public int? SocketCount;

    /// <summary>Adds an effect; a null <paramref name="value"/> is rolled from the item's range.</summary>
    /// <returns>this request, for chaining</returns>
    public MagicItemRequest AddEffect(string effectType, float? value = null)
    {
        Effects.Add(new RequestedEffect { EffectType = effectType, EffectValue = value });
        return this;
    }
}

[Serializable]
[PublicAPI]
public class RequestedEffect
{
    public string EffectType = "";

    /// <summary>Written as is; null rolls a value from the item's range, as a loot roll would.</summary>
    public float? EffectValue;
}

/// <summary>
/// Creating items that carry chosen magic: a reward with set effects, a quest item, a runestone with one
/// specific effect. Requires API 4; check <c>HasEndpoint("CreateMagicItem")</c> to support older Epic Loot.
/// </summary>
public static partial class EpicLoot
{
    private static readonly Method API_CreateMagicItem = new("CreateMagicItem");

    // Null fields stay out of the request, so a missing effect value reaches Epic Loot as "roll it".
    private static readonly JsonSerializerSettings RequestJsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    /// <summary>
    /// Creates a new item carrying exactly the effects, unique and set the request names. Nothing is checked
    /// against the rules a roll follows: unknown effects, effects the item could never roll and items that never
    /// roll magic are all written as asked. The item is in no inventory yet: add it to one with
    /// <c>Inventory.AddItem(item)</c>, or drop it with <c>ItemDrop.DropItem</c>.
    /// </summary>
    /// <param name="request">what to create</param>
    /// <param name="error">why nothing was created, or "" on success</param>
    /// <returns>The new item, or null when the request cannot be built (an unknown prefab, a non-finite
    /// value) or Epic Loot is missing or older than API 4.</returns>
    [PublicAPI]
    public static ItemDrop.ItemData? CreateMagicItem(MagicItemRequest request, out string error)
    {
        error = "";
        if (request == null)
        {
            error = "no request";
            return null;
        }

        string json = JsonConvert.SerializeObject(request, RequestJsonSettings);
        object[] result = API_CreateMagicItem.Invoke(json, "");
        if (result[0] is ItemDrop.ItemData item)
        {
            return item;
        }

        // Epic Loot fills in the reason; it stays empty only when the endpoint was never reached.
        error = result[2] as string ?? "";
        if (error.Length == 0)
        {
            error = "Epic Loot is not loaded or has no CreateMagicItem (API 4)";
        }

        return null;
    }

    /// <summary>An etched runestone carrying one effect, ready to socket or etch.</summary>
    /// <param name="rarity">the runestone's rarity</param>
    /// <param name="effectType"><see cref="EffectType"/>, or any effect another mod registered</param>
    /// <param name="effectValue">null rolls a value from the rarity's range</param>
    /// <returns>The runestone, or null (the reason is logged)</returns>
    [PublicAPI]
    public static ItemDrop.ItemData? CreateRunestone(ItemRarity rarity, string effectType, float? effectValue = null)
    {
        MagicItemRequest request = new MagicItemRequest { Runestone = true, Rarity = rarity }
            .AddEffect(effectType, effectValue);
        return CreateLogged(request);
    }

    /// <summary>A set rune: an etched runestone that Set Etch turns an item into a piece of the set with.</summary>
    /// <param name="rarity">the rune's rarity</param>
    /// <param name="setID">the set's ID</param>
    /// <param name="legendaryID">the piece Set Etch tries first, when the item fits it</param>
    /// <returns>The rune, or null (the reason is logged)</returns>
    [PublicAPI]
    public static ItemDrop.ItemData? CreateSetRune(ItemRarity rarity, string setID, string? legendaryID = null)
    {
        return CreateLogged(new MagicItemRequest
        {
            Runestone = true,
            Rarity = rarity,
            SetID = setID,
            LegendaryID = legendaryID
        });
    }

    private static ItemDrop.ItemData? CreateLogged(MagicItemRequest request)
    {
        ItemDrop.ItemData? item = CreateMagicItem(request, out string error);
        if (item == null)
        {
            logger.LogWarning($"Could not create {(request.Runestone ? "runestone" : request.Prefab)}: {error}");
        }

        return item;
    }
}
