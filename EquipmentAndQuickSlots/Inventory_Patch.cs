using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    //public bool CanAddItem(GameObject prefab, int stack = -1)
    [HarmonyPatch(typeof(Inventory), "CanAddItem", typeof(GameObject), typeof(int))]
    public static class Inventory_CanAddItem_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result, GameObject prefab, int stack)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideCanAddItem(prefab, stack);
                return false;
            }

            return true;
        }
    }

    //public bool CanAddItem(ItemDrop.ItemData item, int stack = -1)
    [HarmonyPatch(typeof(Inventory), "CanAddItem", typeof(ItemDrop.ItemData), typeof(int))]
    public static class Inventory_CanAddItem2_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item, int stack)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideCanAddItem(item, stack);
                return false;
            }

            return true;
        }
    }

    //public bool AddItem(ItemDrop.ItemData item)
    [HarmonyPatch(typeof(Inventory), "AddItem", typeof(ItemDrop.ItemData))]
    public static class Inventory_AddItem2_Patch
    {
        public static bool CallingExtended = false;

        public static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item, out bool __state)
        {
            __state = false;
            if (__instance.DoExtendedCall())
            {
                CallingExtended = true;
                __state = true;
                __result = __instance.Extended().OverrideAddItem(item);
                return false;
            }

            return true;
        }

        public static void Postfix(bool __state)
        {
            if (__state)
            {
                CallingExtended = false;
            }
        }
    }

    //private Vector2i FindEmptySlot(bool topFirst)
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindEmptySlot))]
    public static class Inventory_FindEmptySlot_Patch
    {
        public static bool Prefix(Inventory __instance, bool topFirst, ref Vector2i __result)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideFindEmptySlot(topFirst);
                return false;
            }
            return true;
        }
    }

    //private ItemDrop.ItemData FindFreeStackItem(string name, int quality)
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindFreeStackItem))]
    public static class Inventory_FindFreeStackItem_Patch
    {
        private static bool DoExtended = true;

        public static bool Prefix(Inventory __instance, string name, int quality, ref ItemDrop.ItemData __result, out bool __state)
        {
            __state = false;
            if (DoExtended && Inventory_AddItem2_Patch.CallingExtended && __instance.IsExtended())
            {
                __state = true;
                DoExtended = false;
                __result = __instance.Extended().OverrideFindFreeStackItem(name, quality);
                return false;
            }
            return true;
        }

        public static void Postfix(bool __state)
        {
            if (__state)
            {
                DoExtended = true;
            }
        }
    }

    //public bool ContainsItem(ItemDrop.ItemData item) => this.m_inventory.Contains(item);
    [HarmonyPatch(typeof(Inventory), "ContainsItem")]
    public static class Inventory_ContainsItem_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideContainsItem(item);
                return false;
            }

            return true;
        }
    }

    //public bool RemoveOneItem(ItemDrop.ItemData item)
    [HarmonyPatch(typeof(Inventory), "RemoveOneItem")]
    public static class Inventory_RemoveOneItem_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideRemoveOneItem(item);
                return false;
            }

            return true;
        }
    }

    //public bool RemoveItem(ItemDrop.ItemData item)
    [HarmonyPatch(typeof(Inventory), "RemoveItem", typeof(ItemDrop.ItemData))]
    public static class Inventory_RemoveItem2_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideRemoveItem(item);
                return false;
            }

            return true;
        }
    }

    //public bool RemoveItem(ItemDrop.ItemData item, int amount)
    [HarmonyPatch(typeof(Inventory), "RemoveItem", typeof(ItemDrop.ItemData), typeof(int))]
    public static class Inventory_RemoveItem3_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item, int amount)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideRemoveItem(item, amount);
                return false;
            }

            return true;
        }
    }

    //public void RemoveItem(string name, int amount)
    [HarmonyPatch(typeof(Inventory), "RemoveItem", typeof(string), typeof(int))]
    public static class Inventory_RemoveItem4_Patch
    {
        public static bool Prefix(Inventory __instance, string name, int amount)
        {
            if (__instance.DoExtendedCall())
            {
                __instance.Extended().OverrideRemoveItem(name, amount);
                return false;
            }

            return true;
        }
    }

    //public bool HaveItem(string name)
    [HarmonyPatch(typeof(Inventory), "HaveItem")]
    public static class Inventory_HaveItem_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result, string name)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideHaveItem(name);
                return false;
            }

            return true;
        }
    }

    //public void GetAllPieceTables(List<PieceTable> tables)
    [HarmonyPatch(typeof(Inventory), "GetAllPieceTables")]
    public static class Inventory_GetAllPieceTables_Patch
    {
        public static bool Prefix(Inventory __instance, List<PieceTable> tables)
        {
            if (__instance.DoExtendedCall())
            {
                __instance.Extended().OverrideGetAllPieceTables(tables);
                return false;
            }

            return true;
        }
    }

    //public int CountItems(string name)
    [HarmonyPatch(typeof(Inventory), "CountItems")]
    public static class Inventory_CountItems_Patch
    {
        public static bool Prefix(Inventory __instance, ref int __result, string name)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideCountItems(name);
                return false;
            }

            return true;
        }
    }

    //public ItemDrop.ItemData GetItem(string name)
    [HarmonyPatch(typeof(Inventory), "GetItem", typeof(string))]
    public static class Inventory_GetItem2_Patch
    {
        public static bool Prefix(Inventory __instance, ref ItemDrop.ItemData __result, string name)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideGetItem(name);
                return false;
            }

            return true;
        }
    }

    //public ItemDrop.ItemData GetAmmoItem(string ammoName)
    [HarmonyPatch(typeof(Inventory), "GetAmmoItem", typeof(string))]
    public static class Inventory_GetAmmoItem_Patch
    {
        public static bool Prefix(Inventory __instance, ref ItemDrop.ItemData __result, string ammoName)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideGetAmmoItem(ammoName);
                return false;
            }

            return true;
        }
    }

    //public int FindFreeStackSpace(string name)
    [HarmonyPatch(typeof(Inventory), "FindFreeStackSpace")]
    public static class Inventory_FindFreeStackSpace_Patch
    {
        public static bool Prefix(Inventory __instance, ref int __result, string name)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideFindFreeStackSpace(name);
                return false;
            }

            return true;
        }
    }

    //public int NrOfItems() => this.m_inventory.Count;
    [HarmonyPatch(typeof(Inventory), "NrOfItems")]
    public static class Inventory_NrOfItems_Patch
    {
        public static bool Prefix(Inventory __instance, ref int __result)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideNrOfItems();
                return false;
            }

            return true;
        }
    }

    //public float SlotsUsedPercentage() => (float)((double)this.m_inventory.Count / (double)(this.m_width * this.m_height) * 100.0);
    [HarmonyPatch(typeof(Inventory), "SlotsUsedPercentage")]
    public static class Inventory_SlotsUsedPercentage_Patch
    {
        public static bool Prefix(Inventory __instance, ref float __result)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideSlotsUsedPercentage();
                return false;
            }

            return true;
        }
    }

    //public int GetEmptySlots() => this.m_height * this.m_width - this.m_inventory.Count;
    [HarmonyPatch(typeof(Inventory), "GetEmptySlots")]
    public static class Inventory_GetEmptySlots_Patch
    {
        public static bool Prefix(Inventory __instance, ref int __result)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideGetEmptySlots();
                return false;
            }

            return true;
        }
    }

    //public bool HaveEmptySlot() => this.m_inventory.Count < this.m_width * this.m_height;
    [HarmonyPatch(typeof(Inventory), "HaveEmptySlot")]
    public static class Inventory_HaveEmptySlot_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideHaveEmptySlot();
                return false;
            }

            return true;
        }
    }

    //public List<ItemDrop.ItemData> GetEquipedtems()
    [HarmonyPatch(typeof(Inventory), "GetEquipedtems")]
    public static class Inventory_GetEquipedtems_Patch
    {
        public static bool Prefix(Inventory __instance, ref List<ItemDrop.ItemData> __result)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideGetEquipedItems();
                return false;
            }

            return true;
        }
    }

    //public void GetWornItems(List<ItemDrop.ItemData> worn)
    [HarmonyPatch(typeof(Inventory), "GetWornItems")]
    public static class Inventory_GetWornItems_Patch
    {
        public static bool Prefix(Inventory __instance, List<ItemDrop.ItemData> worn)
        {
            if (__instance.DoExtendedCall())
            {
                __instance.Extended().OverrideGetWornItems(worn);
                return false;
            }

            return true;
        }
    }

    //public void GetValuableItems(List<ItemDrop.ItemData> items)
    [HarmonyPatch(typeof(Inventory), "GetValuableItems")]
    public static class Inventory_GetValuableItems_Patch
    {
        public static bool Prefix(Inventory __instance, List<ItemDrop.ItemData> items)
        {
            if (__instance.DoExtendedCall())
            {
                __instance.Extended().OverrideGetValuableItems(items);
                return false;
            }

            return true;
        }
    }

    //public List<ItemDrop.ItemData> GetAllItems() => this.m_inventory;
    [HarmonyPatch(typeof(Inventory), "GetAllItems", new Type[] {})]
    public static class Inventory_GetAllItems_Patch
    {
        public static bool Prefix(Inventory __instance, ref List<ItemDrop.ItemData> __result)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideGetAllItems();
                return false;
            }

            return true;
        }
    }

    //public void GetAllItems(string name, List<ItemDrop.ItemData> items)
    [HarmonyPatch(typeof(Inventory), "GetAllItems", typeof(string), typeof(List<ItemDrop.ItemData>))]
    public static class Inventory_GetAllItems2_Patch
    {
        public static bool Prefix(Inventory __instance, string name, List<ItemDrop.ItemData> items)
        {
            if (__instance.DoExtendedCall())
            {
                __instance.Extended().OverrideGetAllItems(name, items);
                return false;
            }

            return true;
        }
    }

    //public void GetAllItems(ItemDrop.ItemData.ItemType type, List<ItemDrop.ItemData> items)
    [HarmonyPatch(typeof(Inventory), "GetAllItems", typeof(ItemDrop.ItemData.ItemType), typeof(List<ItemDrop.ItemData>))]
    public static class Inventory_GetAllItems3_Patch
    {
        public static bool Prefix(Inventory __instance, ItemDrop.ItemData.ItemType type, List<ItemDrop.ItemData> items)
        {
            if (__instance.DoExtendedCall())
            {
                __instance.Extended().OverrideGetAllItems(type, items);
                return false;
            }

            return true;
        }
    }

    //public void UpdateTotalWeight()
    [HarmonyPatch(typeof(Inventory), "UpdateTotalWeight")]
    public static class Inventory_UpdateTotalWeight_Patch
    {
        public static bool Prefix(Inventory __instance)
        {
            if (__instance.DoExtendedCall())
            {
                __instance.Extended().OverrideUpdateTotalWeight();
                return false;
            }

            return true;
        }
    }

    //public bool IsTeleportable()
    [HarmonyPatch(typeof(Inventory), "IsTeleportable")]
    public static class Inventory_IsTeleportable_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result)
        {
            if (__instance.DoExtendedCall())
            {
                __result = __instance.Extended().OverrideIsTeleportable();
                return false;
            }

            return true;
        }
    }
}
