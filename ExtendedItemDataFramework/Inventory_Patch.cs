using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ExtendedItemDataFramework
{
    //public void MoveAll(Inventory fromInventory)
    //public bool MoveItemToThis(Inventory fromInventory, ItemDrop.ItemData item, int amount, int x, int y)

    // Add from load:
    //public bool AddItem(string name, int stack, float durability, Vector2i pos, bool equiped, int quality, int variant, long crafterID, string crafterName, Dictionary<string, string> customData)
    [HarmonyPatch(typeof(Inventory), "AddItem", new[] { typeof(string), typeof(int), typeof(float), typeof(Vector2i), typeof(bool), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Dictionary<string, string>) })]
    public static class Inventory_AddItemFromLoad_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result, string name, int stack, float durability, Vector2i pos, bool equiped, int quality, int variant, long crafterID, string crafterName, Dictionary<string, string> customData)
        {
            __result = false;
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
            if (itemPrefab == null)
            {
                ZLog.Log("Failed to find item prefab " + name);
                return false;
            }
            ZNetView.m_forceDisableInit = true;
            GameObject gameObject = Object.Instantiate(itemPrefab);
            ZNetView.m_forceDisableInit = false;
            ItemDrop component = gameObject.GetComponent<ItemDrop>();
            if (component == null)
            {
                ZLog.Log("Missing itemdrop in " + name);
                Object.Destroy(gameObject);
                return false;
            }
            var newItemData = new ExtendedItemData(
                component.m_itemData,
                stack, 
                durability,
                pos,
                equiped,
                quality,
                variant,
                crafterID,
                crafterName);
            newItemData.m_customData = customData;
            __instance.AddItem(newItemData, newItemData.m_stack, pos.x, pos.y);
            Object.Destroy(gameObject);
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
                ZNetView.m_forceDisableInit = true;
                var gameObject = Object.Instantiate(itemPrefab);
                ZNetView.m_forceDisableInit = false;
                var component2 = gameObject.GetComponent<ItemDrop>();
                if (component2 == null)
                {
                    ZLog.Log("Missing itemdrop in " + name);
                    Object.Destroy(gameObject);
                    return false;
                }
                var num = Mathf.Min(a, component2.m_itemData.m_shared.m_maxStackSize);
                a -= num;
                component2.m_itemData.m_stack = num;
                component2.m_itemData.m_quality = quality;
                component2.m_itemData.m_variant = variant;
                component2.m_itemData.m_durability = component2.m_itemData.GetMaxDurability();
                component2.m_itemData.m_crafterID = crafterID;
                component2.m_itemData.m_crafterName = crafterName;

                component2.m_itemData = new ExtendedItemData(component2.m_itemData);
                __instance.AddItem(component2.m_itemData);
                itemData = component2.m_itemData;
                Object.Destroy(gameObject);
            }

            __result = itemData;
            return false;
        }
    }
}
