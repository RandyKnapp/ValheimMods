using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;

namespace EpicLoot.Compatibility;

/// <summary>
/// The quick slots of Equipment and Quick Slots, a second hotbar. They are cells in hidden rows at the bottom of
/// the player's inventory, so vanilla's <c>Inventory.GetBoundItems</c> (row 0 only) does not see them.
/// Bound on first use rather than in <c>Awake</c>: EAQS soft-depends on Epic Loot and so loads after it, and the
/// dependency cannot be declared this way round too, since BepInEx refuses a cycle. Nothing here throws, so an
/// EAQS without this endpoint leaves its quick slots treated as ordinary inventory.
/// </summary>
internal static class EaqsQuickSlots
{
    private const string PluginGuid = "randyknapp.mods.equipmentandquickslots";
    private const string ApiType = "EquipmentAndQuickSlots.API";
    private const string GetItemsMethod = "GetQuickSlotItems";

    private static bool _bound;
    private static Func<List<ItemDrop.ItemData>> _getItems;

    /// <summary>Adds the items in the local player's active quick slots to the list.</summary>
    public static void AppendItems(List<ItemDrop.ItemData> items)
    {
        if (!_bound)
        {
            _bound = true;
            _getItems = Bind();
        }

        if (_getItems == null)
        {
            return;
        }

        try
        {
            items.AddRange(_getItems());
        }
        catch (Exception e)
        {
            _getItems = null;
            EpicLoot.LogWarningForce($"Equipment and Quick Slots' quick slot query threw and is no longer used: {e.Message}");
        }
    }

    private static Func<List<ItemDrop.ItemData>> Bind()
    {
        if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out PluginInfo plugin) || plugin.Instance == null)
        {
            return null;
        }

        MethodInfo method = plugin.Instance.GetType().Assembly.GetType(ApiType, throwOnError: false)
            ?.GetMethod(GetItemsMethod, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
        var getItems = method == null ? null :
            Delegate.CreateDelegate(typeof(Func<List<ItemDrop.ItemData>>), method, throwOnBindFailure: false)
                as Func<List<ItemDrop.ItemData>>;

        if (getItems == null)
        {
            EpicLoot.LogWarningForce($"Equipment and Quick Slots {plugin.Metadata.Version} has no {GetItemsMethod}; " +
                "its quick slots are not treated as hotbar items.");
        }

        return getItems;
    }
}
