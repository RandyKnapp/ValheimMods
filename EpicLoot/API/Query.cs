using EpicLoot.Crafting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot;

/// <summary>
/// Read-only questions an external plugin can ask about an item. Everything here is non-throwing and
/// null-safe: the underlying extension methods are not (<see cref="ItemDataExtensions.GetRarity"/> throws
/// for a non-magic item, <see cref="EpicLoot.GetRarityIconIndex"/> throws for an out-of-range rarity), so
/// this layer wraps rather than exposes that behavior.
///
/// Rarity crosses this boundary as an <see cref="int"/>, never the <see cref="ItemRarity"/> enum -- see
/// the signature rule in docs/API.md.
/// </summary>
public static partial class API
{
    private static readonly int RarityCount = Enum.GetValues(typeof(ItemRarity)).Length;

    /// <summary>
    /// Converts a boundary int to an <see cref="ItemRarity"/>.
    /// </summary>
    /// <returns>false if the value names no rarity, in which case nothing should be assumed about
    /// <paramref name="result"/></returns>
    private static bool TryToRarity(int rarity, out ItemRarity result)
    {
        result = default;
        if (rarity < 0 || rarity >= RarityCount)
        {
            return false;
        }

        result = (ItemRarity)rarity;
        return true;
    }

    /// <param name="item">may be null</param>
    /// <returns>true if the item carries Epic Loot magic data</returns>
    [PublicAPI]
    public static bool IsMagicItem(ItemDrop.ItemData item)
    {
        return item != null && item.IsMagic();
    }

    /// <summary>
    /// The rarity of a magic item, magic crafting material, or runestone.
    /// </summary>
    /// <param name="item">may be null</param>
    /// <param name="rarity">receives the rarity ordinal; unchanged when this returns false</param>
    /// <returns>true if the item has a rarity at all</returns>
    [PublicAPI]
    public static bool TryGetRarity(ItemDrop.ItemData item, ref int rarity)
    {
        if (item == null || !item.HasRarity())
        {
            return false;
        }

        rarity = (int)item.GetRarity();
        return true;
    }

    /// <summary>
    /// The name Epic Loot shows for an item -- the generated magic name ("Sturdy Rag Trousers of the
    /// Bear") where it has one, the vanilla <c>m_shared.m_name</c> otherwise. Any parenthesised suffix
    /// on the vanilla name, such as a variant's stat text, is carried over.
    /// </summary>
    /// <param name="item">may be null</param>
    /// <returns>The unlocalized display name, or "" for a null item</returns>
    [PublicAPI]
    public static string GetItemDisplayName(ItemDrop.ItemData item)
    {
        return item == null ? "" : item.GetDisplayName();
    }

    /// <summary>
    /// <see cref="GetItemDisplayName"/> wrapped in a color tag, which is how Epic Loot's own UI renders
    /// item names. Note the tag overrides whatever color the text component is set to.
    /// </summary>
    /// <param name="item">may be null</param>
    /// <param name="colorOverride">A color to use instead of the item's rarity color, either a hex
    /// string ("#808080ff") or a name ("white") -- pass this to dim a name that would otherwise ignore
    /// your own tinting. May be null.</param>
    /// <returns>The decorated display name, or "" for a null item</returns>
    [PublicAPI]
    public static string GetItemDecoratedName(ItemDrop.ItemData item, string colorOverride = null)
    {
        return item == null ? "" : item.GetDecoratedName(colorOverride);
    }

    /// <returns>The number of rarity tiers, so a consumer can enumerate 0..n-1 without hard-coding 5.</returns>
    [PublicAPI]
    public static int GetRarityCount()
    {
        return RarityCount;
    }

    /// <param name="rarity">rarity ordinal</param>
    /// <returns>The configured hex color, e.g. "#AA55FF". White for an unknown rarity.</returns>
    [PublicAPI]
    public static string GetRarityColorByIndex(int rarity)
    {
        return TryToRarity(rarity, out ItemRarity value) ? EpicLoot.GetRarityColor(value) : "#FFFFFF";
    }

    /// <param name="rarity">rarity ordinal</param>
    /// <returns>The unlocalized display token, e.g. "$mod_epicloot_Epic".</returns>
    [PublicAPI]
    public static string GetRarityDisplayNameByIndex(int rarity)
    {
        return TryToRarity(rarity, out ItemRarity value) ? EpicLoot.GetRarityDisplayName(value) : "";
    }

    /// <summary>
    /// The color Epic Loot draws this specific item's name in -- magic rarity, crafting material rarity,
    /// or runestone rarity, whichever applies.
    /// </summary>
    /// <param name="item">may be null</param>
    /// <returns>hex color, "#FFFFFF" when the item has no rarity</returns>
    [PublicAPI]
    public static string GetItemRarityColor(ItemDrop.ItemData item)
    {
        if (item == null || !item.HasRarity())
        {
            return "#FFFFFF";
        }

        return GetRarityColorByIndex((int)item.GetRarity());
    }

    /// <summary>
    /// Whether this item was added by Epic Loot (shard stone, runestone, crafting material, bounty token,
    /// and so on). Supported replacement for testing <c>m_shared.m_name.StartsWith("$mod_epicloot")</c>.
    /// </summary>
    /// <param name="item">may be null</param>
    [PublicAPI]
    public static bool IsEpicLootItem(ItemDrop.ItemData item)
    {
        if (item?.m_shared == null)
        {
            return false;
        }

        return item.m_shared.m_name.StartsWith("$mod_epicloot", StringComparison.Ordinal)
               || item.IsShardStone()
               || item.IsRunestone()
               || item.IsMagicCraftingMaterial()
               || item.IsUnidentifiedMaterial();
    }

    /// <param name="item">may be null</param>
    [PublicAPI]
    public static bool IsShardStone(ItemDrop.ItemData item)
    {
        return item != null && item.IsShardStone();
    }

    /// <param name="item">may be null</param>
    [PublicAPI]
    public static bool IsRunestone(ItemDrop.ItemData item)
    {
        return item != null && item.IsRunestone();
    }

    /// <param name="item">may be null</param>
    [PublicAPI]
    public static bool IsMagicCraftingMaterial(ItemDrop.ItemData item)
    {
        return item != null && item.IsMagicCraftingMaterial();
    }

    /// <summary>
    /// An unidentified item is magic data that has not been revealed yet; it reports true from
    /// <see cref="IsMagicItem"/> but its effects should not be treated as known.
    /// </summary>
    /// <param name="item">may be null</param>
    [PublicAPI]
    public static bool IsUnidentified(ItemDrop.ItemData item)
    {
        return item != null && item.IsUnidentified();
    }

    /// <summary>
    /// Whether Epic Loot would ever allow this item to become magic (player item, non-stackable, not on
    /// the restricted list, an allowed item type). Check this before calling
    /// <see cref="TryMakeMagicItem"/>.
    /// </summary>
    /// <param name="item">may be null</param>
    [PublicAPI]
    public static bool CanBeMagicItem(ItemDrop.ItemData item)
    {
        return EpicLoot.CanBeMagicItem(item);
    }

    /// <param name="item">may be null</param>
    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    /// <param name="includeSocketed">count effects granted by socketed shards and runestones</param>
    [PublicAPI]
    public static bool ItemHasMagicEffect(ItemDrop.ItemData item, string effectType, bool includeSocketed)
    {
        return item != null && item.HasMagicEffect(effectType, includeSocketed);
    }

    /// <returns>Every registered magic effect type id, including any added through the API.</returns>
    [PublicAPI]
    public static List<string> GetAllMagicEffectTypes()
    {
        return MagicItemEffectDefinitions.AllDefinitions.Keys.ToList();
    }

    /// <summary>
    /// The material cost of enchanting this item to the given rarity. Supported replacement for
    /// reflecting the long-removed <c>EnchantTabController.GetEnchantCosts</c>.
    /// </summary>
    /// <param name="item">may be null</param>
    /// <param name="rarity">rarity ordinal</param>
    /// <returns>JSON list of <see cref="ItemAmountConfig"/> ({ "Item": prefabName, "Amount": n }), or
    /// "[]" when no cost is configured</returns>
    [PublicAPI]
    public static string GetEnchantCostsJson(ItemDrop.ItemData item, int rarity)
    {
        if (item == null || !TryToRarity(rarity, out ItemRarity value))
        {
            return "[]";
        }

        List<ItemAmountConfig> costs = EnchantCostsHelper.GetEnchantCost(item, value);
        return JsonConvert.SerializeObject(costs ?? new List<ItemAmountConfig>());
    }

    /// <summary>
    /// What sacrificing (disenchanting) this item yields. Honors any filter registered through
    /// <see cref="RegisterSacrificeFilter"/>, so an empty result means "not sacrificeable".
    /// </summary>
    /// <param name="item">may be null</param>
    /// <returns>JSON list of <see cref="ItemAmountConfig"/>, or "[]"</returns>
    [PublicAPI]
    public static string GetSacrificeProductsJson(ItemDrop.ItemData item)
    {
        if (item == null)
        {
            return "[]";
        }

        List<ItemAmountConfig> products = EnchantCostsHelper.GetSacrificeProducts(item);
        return JsonConvert.SerializeObject(products ?? new List<ItemAmountConfig>());
    }
}
