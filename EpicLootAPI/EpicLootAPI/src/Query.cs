using JetBrains.Annotations;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace EpicLootAPI;

public static partial class EpicLoot
{
    private static readonly Method API_GetApiVersion = new("GetApiVersion");
    private static readonly Method API_GetPluginVersion = new("GetPluginVersion");
    private static readonly Method API_HasEndpoint = new("HasEndpoint");
    private static readonly Method API_IsMagicItem = new("IsMagicItem");
    private static readonly Method API_TryGetRarity = new("TryGetRarity");
    private static readonly Method API_GetRarityCount = new("GetRarityCount");
    private static readonly Method API_GetRarityColorByIndex = new("GetRarityColorByIndex");
    private static readonly Method API_GetRarityDisplayNameByIndex = new("GetRarityDisplayNameByIndex");
    private static readonly Method API_GetItemRarityColor = new("GetItemRarityColor");
    private static readonly Method API_IsEpicLootItem = new("IsEpicLootItem");
    private static readonly Method API_IsShardStone = new("IsShardStone");
    private static readonly Method API_IsRunestone = new("IsRunestone");
    private static readonly Method API_IsMagicCraftingMaterial = new("IsMagicCraftingMaterial");
    private static readonly Method API_IsUnidentified = new("IsUnidentified");
    private static readonly Method API_CanBeMagicItem = new("CanBeMagicItem");
    private static readonly Method API_ItemHasMagicEffect = new("ItemHasMagicEffect");
    private static readonly Method API_GetAllMagicEffectTypes = new("GetAllMagicEffectTypes");
    private static readonly Method API_GetEnchantCostsJson = new("GetEnchantCostsJson");
    private static readonly Method API_GetSacrificeProductsJson = new("GetSacrificeProductsJson");
    private static readonly Method API_GetItemDisplayName = new("GetItemDisplayName");
    private static readonly Method API_GetItemDecoratedName = new(
        "GetItemDecoratedName",
        typeof(ItemDrop.ItemData),
        typeof(string));

    /// <summary>
    /// The version of the API contract the installed Epic Loot exposes, or 0 if Epic Loot is not
    /// present. Check this before calling anything else.
    /// </summary>
    [PublicAPI]
    public static int GetApiVersion()
    {
        return (int)(API_GetApiVersion.Invoke()[0] ?? 0);
    }

    /// <returns>true if Epic Loot is loaded and its API resolved</returns>
    [PublicAPI]
    public static bool IsLoaded()
    {
        return GetApiVersion() > 0;
    }

    /// <returns>The installed Epic Loot plugin version, or "" if not present.</returns>
    [PublicAPI]
    public static string GetPluginVersion()
    {
        return (string)(API_GetPluginVersion.Invoke()[0] ?? "");
    }

    /// <summary>
    /// Whether a given endpoint exists on the installed Epic Loot. Use this when you rely on something
    /// newer than the oldest version you support.
    /// </summary>
    /// <param name="name">exact, case-sensitive method name</param>
    [PublicAPI]
    public static bool HasEndpoint(string name)
    {
        return (bool)(API_HasEndpoint.Invoke(name)[0] ?? false);
    }

    /// <returns>true if the item carries Epic Loot magic data</returns>
    [PublicAPI]
    public static bool IsMagicItem(this ItemDrop.ItemData item)
    {
        return (bool)(API_IsMagicItem.Invoke(item)[0] ?? false);
    }

    /// <summary>The rarity of a magic item, magic crafting material, or runestone.</summary>
    /// <param name="rarity">receives the rarity; unchanged when this returns false</param>
    /// <returns>true if the item has a rarity at all</returns>
    [PublicAPI]
    public static bool TryGetRarity(this ItemDrop.ItemData item, out ItemRarity rarity)
    {
        rarity = ItemRarity.Magic;
        int value = 0;
        object[] result = API_TryGetRarity.Invoke(item, value);
        if ((bool)(result[0] ?? false))
        {
            rarity = (ItemRarity)(int)(result[2] ?? 0);
            return true;
        }

        return false;
    }

    /// <summary>
    /// The name Epic Loot shows for an item -- the generated magic name ("Sturdy Rag Trousers of the
    /// Bear") where it has one, the vanilla <c>m_shared.m_name</c> otherwise. Still needs localizing.
    /// </summary>
    [PublicAPI]
    public static string GetDisplayName(this ItemDrop.ItemData item)
    {
        return (string)(API_GetItemDisplayName.Invoke(item)[0] ?? "");
    }

    /// <summary>
    /// <see cref="GetDisplayName"/> wrapped in a color tag, the way Epic Loot's own UI renders item
    /// names. The tag overrides whatever color your text component is set to, so pass
    /// <paramref name="colorOverride"/> when you need the name dimmed.
    /// </summary>
    /// <param name="colorOverride">A color replacing the rarity color -- hex ("#808080ff") or a name
    /// ("white"). Null keeps the rarity color.</param>
    [PublicAPI]
    public static string GetDecoratedName(this ItemDrop.ItemData item, string colorOverride = null)
    {
        return (string)(API_GetItemDecoratedName.Invoke(item, colorOverride)[0] ?? "");
    }

    /// <returns>How many rarity tiers exist, so you can enumerate without hard-coding 5.</returns>
    [PublicAPI]
    public static int GetRarityCount()
    {
        return (int)(API_GetRarityCount.Invoke()[0] ?? 0);
    }

    /// <returns>The configured hex color for a rarity, e.g. "#AA55FF".</returns>
    [PublicAPI]
    public static string GetRarityColor(ItemRarity rarity)
    {
        return (string)(API_GetRarityColorByIndex.Invoke((int)rarity)[0] ?? "#FFFFFF");
    }

    /// <returns>The unlocalized rarity token, e.g. "$mod_epicloot_Epic".</returns>
    [PublicAPI]
    public static string GetRarityDisplayName(ItemRarity rarity)
    {
        return (string)(API_GetRarityDisplayNameByIndex.Invoke((int)rarity)[0] ?? "");
    }

    /// <returns>The hex color Epic Loot draws this item's name in, "#FFFFFF" when it has no rarity.</returns>
    [PublicAPI]
    public static string GetItemRarityColor(this ItemDrop.ItemData item)
    {
        return (string)(API_GetItemRarityColor.Invoke(item)[0] ?? "#FFFFFF");
    }

    /// <returns>true if this item was added by Epic Loot (shard, runestone, material, bounty token, ...)</returns>
    [PublicAPI]
    public static bool IsEpicLootItem(this ItemDrop.ItemData item)
    {
        return (bool)(API_IsEpicLootItem.Invoke(item)[0] ?? false);
    }

    [PublicAPI]
    public static bool IsShardStone(this ItemDrop.ItemData item)
    {
        return (bool)(API_IsShardStone.Invoke(item)[0] ?? false);
    }

    [PublicAPI]
    public static bool IsRunestone(this ItemDrop.ItemData item)
    {
        return (bool)(API_IsRunestone.Invoke(item)[0] ?? false);
    }

    [PublicAPI]
    public static bool IsMagicCraftingMaterial(this ItemDrop.ItemData item)
    {
        return (bool)(API_IsMagicCraftingMaterial.Invoke(item)[0] ?? false);
    }

    /// <summary>
    /// An unidentified item reports as magic but its effects are not revealed yet.
    /// </summary>
    [PublicAPI]
    public static bool IsUnidentified(this ItemDrop.ItemData item)
    {
        return (bool)(API_IsUnidentified.Invoke(item)[0] ?? false);
    }

    /// <summary>
    /// Whether Epic Loot would ever allow this item to become magic. Check before
    /// <see cref="TryMakeMagicItem"/>.
    /// </summary>
    [PublicAPI]
    public static bool CanBeMagicItem(this ItemDrop.ItemData item)
    {
        return (bool)(API_CanBeMagicItem.Invoke(item)[0] ?? false);
    }

    /// <param name="effectType"><see cref="EffectType"/></param>
    /// <param name="includeSocketed">count effects granted by socketed shards and runestones</param>
    [PublicAPI]
    public static bool HasMagicEffect(this ItemDrop.ItemData item, string effectType, bool includeSocketed = true)
    {
        return (bool)(API_ItemHasMagicEffect.Invoke(item, effectType, includeSocketed)[0] ?? false);
    }

    /// <returns>Every registered magic effect type id, including ones added by other plugins.</returns>
    [PublicAPI]
    public static List<string> GetAllMagicEffectTypes()
    {
        return (List<string>)(API_GetAllMagicEffectTypes.Invoke()[0] ?? new List<string>());
    }

    /// <returns>The material cost of enchanting this item to the given rarity.</returns>
    [PublicAPI]
    public static List<ItemAmount> GetEnchantCosts(this ItemDrop.ItemData item, ItemRarity rarity)
    {
        string json = (string)(API_GetEnchantCostsJson.Invoke(item, (int)rarity)[0] ?? "[]");
        return DeserializeAmounts(json);
    }

    /// <summary>
    /// What sacrificing this item yields. An empty list means it is not sacrificeable -- either no cost
    /// is configured, or a registered sacrifice filter vetoed it.
    /// </summary>
    [PublicAPI]
    public static List<ItemAmount> GetSacrificeProducts(this ItemDrop.ItemData item)
    {
        string json = (string)(API_GetSacrificeProductsJson.Invoke(item)[0] ?? "[]");
        return DeserializeAmounts(json);
    }

    private static List<ItemAmount> DeserializeAmounts(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<List<ItemAmount>>(json) ?? new List<ItemAmount>();
        }
        catch
        {
            logger.LogWarning("Failed to parse item amount list");
            return new List<ItemAmount>();
        }
    }
}
