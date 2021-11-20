using System;
using HarmonyLib;
using UnityEngine;

namespace ExtendedItemDataFramework
{
    //public void MoveAll(Inventory fromInventory)
    //public bool MoveItemToThis(Inventory fromInventory, ItemDrop.ItemData item, int amount, int x, int y)

    // Add from load:
    //public bool AddItem(string name, int stack, float durability, Vector2i pos, bool equiped, int quality, int variant, long crafterID, string crafterName)
    [HarmonyPatch(typeof(Inventory), "AddItem", new[] { typeof(string), typeof(int), typeof(float), typeof(Vector2i), typeof(bool), typeof(int), typeof(int), typeof(long), typeof(string) })]
    public static class Inventory_AddItemFromLoad_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result, string name, int stack, float durability, Vector2i pos, bool equiped, int quality, int variant, long crafterID, string crafterName)
        {
            __result = false;
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
            if (itemPrefab == null)
            {
                ZLog.Log("Failed to find item prefab " + name);
                return false;
            }
            ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
            if (component == null)
            {
                ZLog.Log("Missing itemdrop in " + name);
                return false;
            }
            var itemDataClone = component.m_itemData.Clone();
            itemDataClone.m_shared = (ItemDrop.ItemData.SharedData)AccessTools.Method(typeof(ItemDrop.ItemData), "MemberwiseClone").Invoke(itemDataClone.m_shared, Array.Empty<object>());
            var newItemData = new ExtendedItemData(
                itemDataClone,
                stack, 
                durability,
                pos,
                equiped,
                quality,
                variant,
                crafterID,
                crafterName);
            __instance.AddItem(newItemData, newItemData.m_stack, pos.x, pos.y);
            __result = true;
            return false;
        }
    }

    // Add from Crafting/Buying
    //public ItemDrop.ItemData AddItem(string name, int stack, int quality, int variant, long crafterID, string crafterName)
    [HarmonyPatch(typeof(Inventory), "AddItem", new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string) })]
    public static class Inventory_AddItemFromCraftingBuying_Patch
    {
        public static bool Prefix(Inventory __instance, ref ItemDrop.ItemData __result, string name, int stack, int quality, int variant, long crafterID, string crafterName)
        {
            __result = null;
            var itemPrefab = ObjectDB.instance.GetItemPrefab(name);
            if (itemPrefab == null)
            {
                ZLog.Log("Failed to find item prefab " + name);
                return false;
            }

            var itemDrop = itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                ZLog.Log("Invalid item " + name);
                return false;
            }

            if (!__instance.CanAddItem(itemPrefab, stack))
            {
                return false;
            }

            ItemDrop.ItemData itemData = null;
            var a = stack;
            while (a > 0)
            {
                var component2 = itemPrefab.GetComponent<ItemDrop>();
                if (component2 == null)
                {
                    ZLog.Log("Missing itemdrop in " + name);
                    return false;
                }
                itemData = component2.m_itemData.Clone();
                itemData.m_shared = (ItemDrop.ItemData.SharedData)AccessTools.Method(typeof(ItemDrop.ItemData), "MemberwiseClone").Invoke(itemData.m_shared, Array.Empty<object>());
                var num = Mathf.Min(a, itemData.m_shared.m_maxStackSize);
                a -= num;
                itemData.m_stack = num;
                itemData.m_quality = quality;
                itemData.m_variant = variant;
                itemData.m_durability = itemData.GetMaxDurability();
                itemData.m_crafterID = crafterID;
                itemData.m_crafterName = crafterName;

                component2.m_itemData = new ExtendedItemData(itemData);
                __instance.AddItem(itemData);
            }

            __result = itemData;
            return false;
        }
    }
}
