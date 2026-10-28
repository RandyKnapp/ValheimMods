using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EpicLoot.MagicItemEffects;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot;

// Where an item's magic lives. Vanilla gives every ItemDrop.ItemData a string dictionary, m_customData, that it
// saves, loads, clones and sends along with the item. Epic Loot keeps the MagicItem in it as one JSON entry under
// Key, and this class is the whole layer between that entry and the MagicItem object the rest of the mod reads:
//
// - One MagicItemComponent per ItemData instance holds the parsed MagicItem and remembers which string it was
//   parsed from. Get re-parses only when the entry under the key is no longer that string, so whatever replaces
//   an item's data (a ZDO load, a chest reload, an upgrade, another mod writing the key) is picked up on the next
//   read, with no load hooks at all. An item without the entry costs one dictionary lookup and nothing else.
// - SetMagicItem on the component is the one writer: it serializes, stores, and tells the rest of the mod
//   (Indestructible, the API's change event, the tooltip cache, the worn-item ZDO mirror).
// - Stacking. Vanilla merges stacks by name, quality and world level; two stacks with different magic must stay
//   apart or one's data would be lost. The patches below compare the entry on every merge path vanilla has: the
//   free-stack search behind AddItem and CanAddItem, a drop onto an occupied cell, and the auto-stacking of
//   ground drops. Two items with no custom data at all are answered without a lookup.
// - An upgrade at a workbench removes the item and adds a fresh one from its prefab; the data is carried across.
//
// Other mods read and write the magic through ItemDataExtensions (GetMagicItem, SaveMagicItem, RemoveMagicItem)
// or the API; there is no other surface.
public static class MagicItemData
{
    // Every save since 2023 stores the magic under this key, and other mods read it by this string.
    public const string Key = EpicLoot.PluginId + "#EpicLoot.MagicItemComponent";

    private static readonly ConditionalWeakTable<ItemDrop.ItemData, MagicItemComponent> Components = new();

    // The stored JSON, or null when the item carries none. An empty entry or a serialized null (older builds
    // wrote one onto every equippable item) counts as none.
    public static string RawOf(ItemDrop.ItemData item)
    {
        Dictionary<string, string> data = item?.m_customData;
        if (data == null || data.Count == 0 || !data.TryGetValue(Key, out string raw) ||
            string.IsNullOrEmpty(raw) || raw == "null")
        {
            return null;
        }

        return raw;
    }

    public static bool HasMagicData(ItemDrop.ItemData item) => RawOf(item) != null;

    // The item's component when it carries a MagicItem, else null.
    public static MagicItemComponent Get(ItemDrop.ItemData item)
    {
        MagicItemComponent component = Resolve(item, create: false);
        return component?.MagicItem != null ? component : null;
    }

    // The item's component, made empty when it has none, for the SetMagicItem that follows.
    public static MagicItemComponent GetOrCreate(ItemDrop.ItemData item) => Resolve(item, create: true);

    private static MagicItemComponent Resolve(ItemDrop.ItemData item, bool create)
    {
        if (item == null)
        {
            return null;
        }

        string raw = RawOf(item);
        if (Components.TryGetValue(item, out MagicItemComponent component))
        {
            // The entry was replaced under the component: follow it.
            if (!ReferenceEquals(component.Raw, raw))
            {
                component.Reload(raw);
            }

            return component;
        }

        if (raw == null && !create)
        {
            return null;
        }

        component = new MagicItemComponent(item);
        // Registered before it loads: loading runs Indestructible.Sync, which reads the item's magic back
        // through here and must find this component rather than build a second one.
        Components.Add(item, component);
        component.Reload(raw);
        return component;
    }

    // Drops the item's magic data. Returns whether there was an entry to drop.
    public static bool Remove(ItemDrop.ItemData item)
    {
        if (item?.m_customData == null)
        {
            return false;
        }

        if (Components.TryGetValue(item, out MagicItemComponent component))
        {
            component.Unload();
            Components.Remove(item);
        }
        else
        {
            Indestructible.Revert(item);
        }

        return item.m_customData.Remove(Key);
    }

    // Stores the JSON a component serialized. Only MagicItemComponent.SetMagicItem calls this.
    internal static void Store(ItemDrop.ItemData item, string json) => item.m_customData[Key] = json;

    // A fresh instance of a prefab that carries magic data (a shardstone's cosmetic rarity, an unidentified
    // item, a Brokkr's Gift, a crafting material) takes the prefab's entry, unparsed, when it has none of its
    // own. The string is shared, which is what lets the first read of the instance parse it once.
    public static void CopyFromPrefab(ItemDrop.ItemData item, ItemDrop.ItemData prefabItem)
    {
        if (item?.m_customData == null || RawOf(item) != null)
        {
            return;
        }

        string raw = RawOf(prefabItem);
        if (raw != null)
        {
            item.m_customData[Key] = raw;
        }
    }

    // ---- Stacking ----

    // Whether `item` may join `existing`'s stack: both carry the same magic text, or neither carries any. Only
    // Epic Loot's own entry is judged, and an empty entry and none are the same thing. Stackables are
    // re-serialized once on load (MagicItemComponent.Reload), so text an older build wrote still compares
    // equal to a fresh drop's.
    internal static bool StacksWith(ItemDrop.ItemData existing, ItemDrop.ItemData item)
    {
        if (existing == item)
        {
            return true;
        }

        if ((existing.m_customData == null || existing.m_customData.Count == 0) &&
            (item.m_customData == null || item.m_customData.Count == 0))
        {
            return true;
        }

        string left = RawOf(existing);
        string right = RawOf(item);
        return left == null ? right == null : right != null && string.Equals(left, right, StringComparison.Ordinal);
    }

    // The item vanilla is searching a stack for, during the calls that search. Each entry point saves what it
    // found and puts it back, so a nested search cannot answer for the wrong item.
    private static ItemDrop.ItemData _adding;

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
    private static class Inventory_AddItem_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop.ItemData item, out ItemDrop.ItemData __state)
        {
            __state = _adding;
            _adding = item;
        }

        [UsedImplicitly]
        private static void Finalizer(ItemDrop.ItemData __state) => _adding = __state;
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(Vector2i))]
    private static class Inventory_AddItemAtPosition_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop.ItemData item, out ItemDrop.ItemData __state)
        {
            __state = _adding;
            _adding = item;
        }

        [UsedImplicitly]
        private static void Finalizer(ItemDrop.ItemData __state) => _adding = __state;
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
    private static class Inventory_CanAddItem_Patch
    {
        [UsedImplicitly]
        private static void Prefix(ItemDrop.ItemData item, out ItemDrop.ItemData __state)
        {
            __state = _adding;
            _adding = item;
        }

        [UsedImplicitly]
        private static void Finalizer(ItemDrop.ItemData __state) => _adding = __state;
    }

    // Vanilla's own search (name, quality, room in the stack, world level) with the magic check added to it,
    // run in vanilla's place only while an item is being added. A prefix that already cancelled the search
    // keeps its answer.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindFreeStackItem))]
    private static class Inventory_FindFreeStackItem_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Inventory __instance, string name, int quality, float worldLevel,
            ref ItemDrop.ItemData __result, bool __runOriginal)
        {
            ItemDrop.ItemData adding = _adding;
            if (adding == null || !__runOriginal)
            {
                return __runOriginal;
            }

            foreach (ItemDrop.ItemData item in __instance.m_inventory)
            {
                if (item.m_shared.m_name == name && item.m_quality == quality &&
                    item.m_stack < item.m_shared.m_maxStackSize && (float)item.m_worldLevel == worldLevel &&
                    StacksWith(item, adding))
                {
                    __result = item;
                    return false;
                }
            }

            __result = null;
            return false;
        }
    }

    // The same for the room count CanAddItem sums: stacks the item may not join have no room for it.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindFreeStackSpace))]
    private static class Inventory_FindFreeStackSpace_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Inventory __instance, string name, float worldLevel, ref int __result,
            bool __runOriginal)
        {
            ItemDrop.ItemData adding = _adding;
            if (adding == null || !__runOriginal)
            {
                return __runOriginal;
            }

            int room = 0;
            foreach (ItemDrop.ItemData item in __instance.m_inventory)
            {
                if (item.m_shared.m_name == name && item.m_stack < item.m_shared.m_maxStackSize &&
                    (float)item.m_worldLevel == worldLevel && StacksWith(item, adding))
                {
                    room += item.m_shared.m_maxStackSize - item.m_stack;
                }
            }

            __result = room;
            return false;
        }
    }

    // A drop onto an occupied cell: vanilla merges onto the occupant when the types match, so the magic has to
    // agree too.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int),
        typeof(int), typeof(int), typeof(bool))]
    private static class Inventory_AddItemToCell_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Inventory __instance, ItemDrop.ItemData item, int x, int y, ref bool __result,
            bool __runOriginal)
        {
            if (!__runOriginal)
            {
                return false;
            }

            ItemDrop.ItemData occupant = __instance.GetItemAt(x, y);
            if (occupant == null || occupant == item || StacksWith(occupant, item))
            {
                return true;
            }

            __result = false;
            return false;
        }
    }

    // Ground drops: once a few hundred are loaded, vanilla merges nearby drops of this one's name and quality
    // into it and destroys them. Its loop with the magic check added, run in vanilla's place; keep it in step
    // with ItemDrop.AutoStackItems. Needed because another mod can make an etched rune or a Brokkr's Gift
    // stackable, and vanilla's m_autoStack defaults to true.
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.AutoStackItems))]
    private static class ItemDrop_AutoStackItems_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(ItemDrop __instance, bool __runOriginal)
        {
            if (!__runOriginal)
            {
                return false;
            }

            ItemDrop.ItemData itemData = __instance.m_itemData;
            if (itemData.m_shared.m_maxStackSize <= 1 || itemData.m_stack >= itemData.m_shared.m_maxStackSize ||
                __instance.m_haveAutoStacked)
            {
                return false;
            }

            __instance.m_haveAutoStacked = true;
            if (ItemDrop.s_itemMask == 0)
            {
                ItemDrop.s_itemMask = LayerMask.GetMask("item");
            }

            bool mergedAny = false;
            foreach (Collider collider in Physics.OverlapSphere(__instance.transform.position, 4f, ItemDrop.s_itemMask))
            {
                if (!collider.attachedRigidbody)
                {
                    continue;
                }

                ItemDrop other = collider.attachedRigidbody.GetComponent<ItemDrop>();
                if (other == null || other == __instance || !other.m_itemData.m_shared.m_autoStack ||
                    other.m_nview == null || !other.m_nview.IsValid() || !other.m_nview.IsOwner() ||
                    other.m_itemData.m_shared.m_name != itemData.m_shared.m_name ||
                    other.m_itemData.m_quality != itemData.m_quality)
                {
                    continue;
                }

                int room = itemData.m_shared.m_maxStackSize - itemData.m_stack;
                if (room == 0)
                {
                    break;
                }

                if (other.m_itemData.m_stack > room || !StacksWith(itemData, other.m_itemData))
                {
                    continue;
                }

                itemData.m_stack += other.m_itemData.m_stack;
                mergedAny = true;
                other.m_nview.Destroy();
            }

            if (mergedAny)
            {
                __instance.Save();
            }

            return false;
        }
    }

    // ---- Upgrades ----

    // Vanilla upgrades by removing the item and adding a fresh one from the prefab by name, so the upgraded
    // item would come back mundane. The item DoCrafting is upgrading is remembered for the duration, and the
    // item added under its name inherits its data. Only that item does: at the Forge of Potential a failed
    // attempt refunds ingredients through the same call while the upgrade is in flight (the first refunded Wood
    // once took the sword's enchantments), and the success and level-down outcomes both re-add the item by
    // its own name.
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    private static class InventoryGui_DoCrafting_Patch
    {
        internal static ItemDrop.ItemData Upgrading;

        [UsedImplicitly]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(InventoryGui __instance) => Upgrading = __instance.m_craftUpgradeItem;

        [UsedImplicitly]
        private static void Finalizer() => Upgrading = null;
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(int),
        typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool))]
    private static class Inventory_AddItemByName_Patch
    {
        [UsedImplicitly]
        private static void Postfix(ItemDrop.ItemData __result)
        {
            ItemDrop.ItemData source = InventoryGui_DoCrafting_Patch.Upgrading;
            if (source == null || __result == null || __result.m_shared?.m_name != source.m_shared?.m_name)
            {
                return;
            }

            InventoryGui_DoCrafting_Patch.Upgrading = null;
            if (source.m_customData != null && source.m_customData.Count > 0)
            {
                // A copy: the old item is gone from the inventory but may still be referenced this frame.
                __result.m_customData = new Dictionary<string, string>(source.m_customData);
            }
        }
    }
}
