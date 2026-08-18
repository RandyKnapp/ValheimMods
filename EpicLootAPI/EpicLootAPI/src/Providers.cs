using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace EpicLootAPI;

/// <summary>
/// Provider registration -- the supported replacement for Harmony-patching Epic Loot's inventory,
/// equipment, and sacrifice paths.
/// </summary>
public static partial class EpicLoot
{
    private static readonly Method API_RegisterInventoryProvider = new(
        "RegisterInventoryProvider",
        typeof(string),
        typeof(Func<List<ItemDrop.ItemData>>),
        typeof(Func<string, int>),
        typeof(Func<string, int, int>),
        typeof(Func<ItemDrop.ItemData, int, int>));

    private static readonly Method API_UnregisterInventoryProvider = new("UnregisterInventoryProvider");

    private static readonly Method API_RegisterEquipmentProvider = new(
        "RegisterEquipmentProvider",
        typeof(string),
        typeof(Func<Player, List<ItemDrop.ItemData>>));

    private static readonly Method API_UnregisterEquipmentProvider = new("UnregisterEquipmentProvider");

    private static readonly Method API_RegisterSacrificeFilter = new(
        "RegisterSacrificeFilter",
        typeof(string),
        typeof(Func<ItemDrop.ItemData, bool>));

    private static readonly Method API_UnregisterSacrificeFilter = new("UnregisterSacrificeFilter");
    private static readonly Method API_InvalidatePlayerEffectCache = new("InvalidatePlayerEffectCache");
    private static readonly Method API_GetRegisteredProviders = new("GetRegisteredProviders");

    /// <summary>
    /// Contributes extra items to the enchanting table's view of the player's inventory -- nearby
    /// containers, a backpack, a remote stash. Epic Loot spends the player's own inventory first and
    /// only charges the shortfall to providers.
    /// </summary>
    /// <param name="id">Unique id, normally your plugin GUID</param>
    /// <param name="getItems">The extra items the table may see. Return live instances, not copies:
    /// Epic Loot needs instance identity to preserve magic data. May be null.</param>
    /// <param name="countItem">Given <c>m_shared.m_name</c>, how many you can supply. May be null.</param>
    /// <param name="removeItem">Given <c>m_shared.m_name</c> and a requested amount, consume up to that
    /// much and return how many you actually removed. May be null.</param>
    /// <param name="removeExactItem">Consume a specific item instance -- match by reference, not by
    /// name, or you will destroy the wrong enchanted item. May be null.</param>
    /// <returns>true if registered</returns>
    [PublicAPI]
    public static bool RegisterInventoryProvider(
        string id,
        Func<List<ItemDrop.ItemData>> getItems,
        Func<string, int> countItem,
        Func<string, int, int> removeItem,
        Func<ItemDrop.ItemData, int, int> removeExactItem)
    {
        object[] result = API_RegisterInventoryProvider.Invoke(id, getItems, countItem, removeItem, removeExactItem);
        bool output = (bool)(result[0] ?? false);
        logger.LogDebug($"Registered inventory provider: {id}, {output}");
        return output;
    }

    /// <param name="id">The id passed to <see cref="RegisterInventoryProvider"/></param>
    [PublicAPI]
    public static bool UnregisterInventoryProvider(string id)
    {
        return (bool)(API_UnregisterInventoryProvider.Invoke(id)[0] ?? false);
    }

    /// <summary>
    /// Contributes extra equipped items for magic effect resolution -- extra equipment slots, quick
    /// slots, an equipped backpack. Contributed items count toward effect totals, legendary set
    /// bonuses, tooltips and shard socketing alike.
    /// </summary>
    /// <remarks>
    /// Contributed items also receive their equip effect visuals (auras worn on the player), reconciled
    /// after every equipment change -- you do not attach or remove those yourself.
    /// Epic Loot memoizes effect totals per player and only invalidates on vanilla
    /// <c>Humanoid.EquipItem</c>/<c>UnequipItem</c>. If your slots change outside those, call
    /// <see cref="InvalidatePlayerEffectCache"/> or stale values keep being served.
    /// </remarks>
    /// <param name="id">Unique id, normally your plugin GUID</param>
    /// <param name="getExtraEquipped">Items equipped in your slots. Non-magic items are filtered out on
    /// Epic Loot's side, so returning everything is fine.</param>
    /// <returns>true if registered</returns>
    [PublicAPI]
    public static bool RegisterEquipmentProvider(string id, Func<Player, List<ItemDrop.ItemData>> getExtraEquipped)
    {
        object[] result = API_RegisterEquipmentProvider.Invoke(id, getExtraEquipped);
        bool output = (bool)(result[0] ?? false);
        logger.LogDebug($"Registered equipment provider: {id}, {output}");
        return output;
    }

    /// <param name="id">The id passed to <see cref="RegisterEquipmentProvider"/></param>
    [PublicAPI]
    public static bool UnregisterEquipmentProvider(string id)
    {
        return (bool)(API_UnregisterEquipmentProvider.Invoke(id)[0] ?? false);
    }

    /// <summary>
    /// Discards the memoized magic effect totals for a player and refreshes the equip effect visuals
    /// worn on them. Call after changing the contents of slots you provide.
    /// </summary>
    [PublicAPI]
    public static void InvalidatePlayerEffectCache(this Player player)
    {
        API_InvalidatePlayerEffectCache.Invoke(player);
    }

    /// <summary>
    /// Vetoes sacrificing specific items -- typically one equipped in a slot Epic Loot cannot see, which
    /// would otherwise be destroyed by accident. A vetoed item yields no sacrifice products, which hides
    /// it from the Sacrifice tab.
    /// </summary>
    /// <param name="id">Unique id, normally your plugin GUID</param>
    /// <param name="canSacrifice">Return false to veto. Called for every item the tab evaluates, so keep
    /// it cheap.</param>
    /// <returns>true if registered</returns>
    [PublicAPI]
    public static bool RegisterSacrificeFilter(string id, Func<ItemDrop.ItemData, bool> canSacrifice)
    {
        object[] result = API_RegisterSacrificeFilter.Invoke(id, canSacrifice);
        bool output = (bool)(result[0] ?? false);
        logger.LogDebug($"Registered sacrifice filter: {id}, {output}");
        return output;
    }

    /// <param name="id">The id passed to <see cref="RegisterSacrificeFilter"/></param>
    [PublicAPI]
    public static bool UnregisterSacrificeFilter(string id)
    {
        return (bool)(API_UnregisterSacrificeFilter.Invoke(id)[0] ?? false);
    }

    /// <returns>Registered provider ids keyed by family ("Inventory", "Equipment", "Sacrifice").</returns>
    [PublicAPI]
    public static Dictionary<string, List<string>> GetRegisteredProviders()
    {
        return (Dictionary<string, List<string>>)(API_GetRegisteredProviders.Invoke()[0]
                                                  ?? new Dictionary<string, List<string>>());
    }
}
