using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot;

/// <summary>
/// Extension points that let another plugin widen what Epic Loot considers "available" without patching
/// it. Each family replaces a set of Harmony patches integrating mods write today -- see the migration
/// table in docs/API.md.
///
/// Every registered delegate is invoked inside a try/catch. A provider that throws is logged with its id
/// (unconditionally, since it is another plugin's bug) and treated as contributing nothing, so a broken
/// integration degrades to vanilla behavior rather than breaking the enchanting table.
/// </summary>
public static partial class API
{
    private class InventoryProvider
    {
        public string Id;
        public Func<List<ItemDrop.ItemData>> GetItems;
        public Func<string, int> CountItem;
        public Func<string, int, int> RemoveItem;
        public Func<ItemDrop.ItemData, int, int> RemoveExactItem;
    }

    private static readonly List<InventoryProvider> InventoryProviders = new();
    private static readonly Dictionary<string, Func<Player, List<ItemDrop.ItemData>>> EquipmentProviders = new();
    private static readonly Dictionary<string, Func<ItemDrop.ItemData, bool>> SacrificeFilters = new();

    // Checked before every provider dispatch. Registration is rare and dispatch is not, so the common
    // case must cost one bool read.
    internal static bool AnyInventoryProviders;
    internal static bool AnyEquipmentProviders;
    internal static bool AnySacrificeFilters;

    private static void LogProviderFailure(string family, string id, Exception ex)
    {
        // Force-logged: this is a fault in the registering plugin, and silently swallowing it would make
        // the resulting "my items don't count" report impossible to diagnose.
        EpicLoot.LogErrorForce($"[EpicLoot.API] {family} provider '{id}' threw and was skipped: {ex}");
    }

    #region Inventory providers

    /// <summary>
    /// Contributes additional items to the enchanting table's view of the player's inventory -- nearby
    /// containers, a backpack, a remote stash. Epic Loot always draws from the player's own inventory
    /// first and only falls through to providers for the shortfall.
    /// </summary>
    /// <param name="id">Unique id for this registration, normally your plugin GUID. Used for
    /// unregistration and in error messages.</param>
    /// <param name="getItems">Returns the extra items the table may see. Return the live instances, not
    /// copies -- Epic Loot needs instance identity to preserve magic data. May be null.</param>
    /// <param name="countItem">Given <c>m_shared.m_name</c>, returns how many you can supply. May be null.</param>
    /// <param name="removeItem">Given <c>m_shared.m_name</c> and a requested amount, consumes up to that
    /// amount and returns how many were actually removed. May be null.</param>
    /// <param name="removeExactItem">Consumes a specific item instance (match by reference, not by name --
    /// matching by name will destroy the wrong enchanted item) and returns how many were removed.
    /// May be null.</param>
    /// <returns>true if registered; false if <paramref name="id"/> is empty or already registered</returns>
    [PublicAPI]
    public static bool RegisterInventoryProvider(
        string id,
        Func<List<ItemDrop.ItemData>> getItems,
        Func<string, int> countItem,
        Func<string, int, int> removeItem,
        Func<ItemDrop.ItemData, int, int> removeExactItem)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            OnError?.Invoke("Failed to register inventory provider: id is empty");
            return false;
        }

        if (InventoryProviders.Exists(x => x.Id == id))
        {
            OnError?.Invoke($"Duplicate inventory provider: {id}");
            return false;
        }

        InventoryProviders.Add(new InventoryProvider
        {
            Id = id,
            GetItems = getItems,
            CountItem = countItem,
            RemoveItem = removeItem,
            RemoveExactItem = removeExactItem
        });
        AnyInventoryProviders = true;
        OnReload?.Invoke($"Registered inventory provider: {id}");
        return true;
    }

    /// <param name="id">The id passed to <see cref="RegisterInventoryProvider"/></param>
    /// <returns>true if a registration was removed</returns>
    [PublicAPI]
    public static bool UnregisterInventoryProvider(string id)
    {
        bool removed = InventoryProviders.RemoveAll(x => x.Id == id) > 0;
        AnyInventoryProviders = InventoryProviders.Count > 0;
        return removed;
    }

    /// <summary>
    /// Appends every provider-supplied item to <paramref name="into"/>. The caller must own that list --
    /// never pass a live <c>Inventory.m_inventory</c>.
    /// </summary>
    internal static void AppendProviderItems(List<ItemDrop.ItemData> into)
    {
        if (!AnyInventoryProviders || into == null)
        {
            return;
        }

        foreach (InventoryProvider provider in InventoryProviders)
        {
            if (provider.GetItems == null)
            {
                continue;
            }

            try
            {
                List<ItemDrop.ItemData> items = provider.GetItems();
                if (items == null)
                {
                    continue;
                }

                foreach (ItemDrop.ItemData item in items)
                {
                    // Guard against a provider handing back something the player already holds; the
                    // table would otherwise count it twice.
                    if (item != null && !into.Contains(item))
                    {
                        into.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                LogProviderFailure("Inventory", provider.Id, ex);
            }
        }
    }

    /// <param name="itemName"><c>m_shared.m_name</c></param>
    /// <returns>How many of <paramref name="itemName"/> all providers together can supply.</returns>
    internal static int CountProviderItems(string itemName)
    {
        if (!AnyInventoryProviders)
        {
            return 0;
        }

        int total = 0;
        foreach (InventoryProvider provider in InventoryProviders)
        {
            if (provider.CountItem == null)
            {
                continue;
            }

            try
            {
                total += Math.Max(0, provider.CountItem(itemName));
            }
            catch (Exception ex)
            {
                LogProviderFailure("Inventory", provider.Id, ex);
            }
        }

        return total;
    }

    /// <summary>Drains <paramref name="amount"/> across providers in registration order.</summary>
    /// <returns>How many were actually removed, which may be less than requested.</returns>
    internal static int RemoveProviderItems(string itemName, int amount)
    {
        if (!AnyInventoryProviders || amount <= 0)
        {
            return 0;
        }

        int remaining = amount;
        foreach (InventoryProvider provider in InventoryProviders)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (provider.RemoveItem == null)
            {
                continue;
            }

            try
            {
                // Clamp: a provider that over-reports would otherwise make the caller believe it
                // consumed more than it asked for.
                int removed = Mathf.Clamp(provider.RemoveItem(itemName, remaining), 0, remaining);
                remaining -= removed;
            }
            catch (Exception ex)
            {
                LogProviderFailure("Inventory", provider.Id, ex);
            }
        }

        return amount - remaining;
    }

    /// <summary>Drains a specific item instance across providers.</summary>
    /// <returns>How many were actually removed.</returns>
    internal static int RemoveExactProviderItem(ItemDrop.ItemData item, int amount)
    {
        if (!AnyInventoryProviders || item == null || amount <= 0)
        {
            return 0;
        }

        int remaining = amount;
        foreach (InventoryProvider provider in InventoryProviders)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (provider.RemoveExactItem == null)
            {
                continue;
            }

            try
            {
                int removed = Mathf.Clamp(provider.RemoveExactItem(item, remaining), 0, remaining);
                remaining -= removed;
            }
            catch (Exception ex)
            {
                LogProviderFailure("Inventory", provider.Id, ex);
            }
        }

        return amount - remaining;
    }

    #endregion

    #region Equipment providers

    /// <summary>
    /// Contributes additional equipped items for magic effect resolution -- extra equipment slots, quick
    /// slots, an equipped backpack. Feeds <c>PlayerExtensions.GetMagicEquipment</c>, so contributed items
    /// count toward effect totals, legendary set bonuses, tooltips, and shard socketing alike.
    /// </summary>
    /// <remarks>
    /// Results are memoized per player in <see cref="EquipmentEffectCache"/>, which only invalidates on
    /// vanilla <c>Humanoid.EquipItem</c>/<c>UnequipItem</c>. If your slots change outside those methods,
    /// call <see cref="InvalidatePlayerEffectCache"/> or the old values will keep being served.
    /// </remarks>
    /// <param name="id">Unique id, normally your plugin GUID</param>
    /// <param name="getExtraEquipped">Returns the items equipped in your slots for that player. Non-magic
    /// items are filtered out by Epic Loot, so returning everything is fine.</param>
    /// <returns>true if registered</returns>
    [PublicAPI]
    public static bool RegisterEquipmentProvider(string id, Func<Player, List<ItemDrop.ItemData>> getExtraEquipped)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            OnError?.Invoke("Failed to register equipment provider: id is empty");
            return false;
        }

        if (getExtraEquipped == null)
        {
            OnError?.Invoke($"Failed to register equipment provider '{id}': callback is null");
            return false;
        }

        if (EquipmentProviders.ContainsKey(id))
        {
            OnError?.Invoke($"Duplicate equipment provider: {id}");
            return false;
        }

        EquipmentProviders[id] = getExtraEquipped;
        AnyEquipmentProviders = true;
        OnReload?.Invoke($"Registered equipment provider: {id}");
        return true;
    }

    /// <param name="id">The id passed to <see cref="RegisterEquipmentProvider"/></param>
    /// <returns>true if a registration was removed</returns>
    [PublicAPI]
    public static bool UnregisterEquipmentProvider(string id)
    {
        bool removed = id != null && EquipmentProviders.Remove(id);
        AnyEquipmentProviders = EquipmentProviders.Count > 0;
        return removed;
    }

    /// <summary>
    /// Discards the memoized magic effect totals for a player, forcing the next read to recompute, and
    /// refreshes the equip effect visuals worn on the player. Call after changing the contents of slots
    /// you provide.
    /// </summary>
    /// <param name="player">may be null</param>
    [PublicAPI]
    public static void InvalidatePlayerEffectCache(Player player)
    {
        if (player != null)
        {
            EquipmentEffectCache.Reset(player);
            VisEquipment_Patch.RefreshPlayerFx(player);
        }
    }

    /// <summary>
    /// Appends provider-supplied equipped items to <paramref name="into"/>, keeping only magic ones and
    /// skipping anything already present.
    /// </summary>
    internal static void AppendProviderEquipment(Player player, List<ItemDrop.ItemData> into)
    {
        if (!AnyEquipmentProviders || player == null || into == null)
        {
            return;
        }

        foreach (KeyValuePair<string, Func<Player, List<ItemDrop.ItemData>>> entry in EquipmentProviders)
        {
            try
            {
                List<ItemDrop.ItemData> items = entry.Value(player);
                if (items == null)
                {
                    continue;
                }

                foreach (ItemDrop.ItemData item in items)
                {
                    if (item != null && item.IsMagic() && !into.Contains(item))
                    {
                        into.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                LogProviderFailure("Equipment", entry.Key, ex);
            }
        }
    }

    #endregion

    #region Sacrifice filters

    /// <summary>
    /// Vetoes sacrificing (disenchanting) specific items -- for instance to stop an item equipped in a
    /// slot Epic Loot cannot see from being destroyed. A filter returning false makes the item yield no
    /// sacrifice products, which hides it from the Sacrifice tab.
    /// </summary>
    /// <param name="id">Unique id, normally your plugin GUID</param>
    /// <param name="canSacrifice">Return false to veto. Called for every item the tab evaluates, so keep
    /// it cheap.</param>
    /// <returns>true if registered</returns>
    [PublicAPI]
    public static bool RegisterSacrificeFilter(string id, Func<ItemDrop.ItemData, bool> canSacrifice)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            OnError?.Invoke("Failed to register sacrifice filter: id is empty");
            return false;
        }

        if (canSacrifice == null)
        {
            OnError?.Invoke($"Failed to register sacrifice filter '{id}': callback is null");
            return false;
        }

        if (SacrificeFilters.ContainsKey(id))
        {
            OnError?.Invoke($"Duplicate sacrifice filter: {id}");
            return false;
        }

        SacrificeFilters[id] = canSacrifice;
        AnySacrificeFilters = true;
        OnReload?.Invoke($"Registered sacrifice filter: {id}");
        return true;
    }

    /// <param name="id">The id passed to <see cref="RegisterSacrificeFilter"/></param>
    /// <returns>true if a registration was removed</returns>
    [PublicAPI]
    public static bool UnregisterSacrificeFilter(string id)
    {
        bool removed = id != null && SacrificeFilters.Remove(id);
        AnySacrificeFilters = SacrificeFilters.Count > 0;
        return removed;
    }

    /// <returns>false if any filter vetoes sacrificing this item. A filter that throws does not veto.</returns>
    internal static bool SacrificeAllowed(ItemDrop.ItemData item)
    {
        if (!AnySacrificeFilters || item == null)
        {
            return true;
        }

        foreach (KeyValuePair<string, Func<ItemDrop.ItemData, bool>> entry in SacrificeFilters)
        {
            try
            {
                if (!entry.Value(item))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogProviderFailure("Sacrifice", entry.Key, ex);
            }
        }

        return true;
    }

    #endregion

    /// <returns>The ids of every registered provider, grouped by family, for diagnostics.</returns>
    [PublicAPI]
    public static Dictionary<string, List<string>> GetRegisteredProviders()
    {
        return new Dictionary<string, List<string>>
        {
            ["Inventory"] = InventoryProviders.ConvertAll(x => x.Id),
            ["Equipment"] = new List<string>(EquipmentProviders.Keys),
            ["Sacrifice"] = new List<string>(SacrificeFilters.Keys)
        };
    }
}
