using System;
using HarmonyLib;

namespace EquipmentAndQuickSlots
{
    [HarmonyPatch(typeof(Container), "Load")]
    public static class Container_Load_Patch
    {
        public static Container tombstoneContainer = null;

        public static void Prefix(Container __instance, out bool __state)
        {
            __state = false;
            if (__instance.GetComponent<TombStone>())
            {
                tombstoneContainer = __instance;
                __state = true;
            }
        }

        public static void Postfix(bool __state)
        {
            if (__state)
            {
                tombstoneContainer = null;
            }
        }
    }

    // private bool AddItem(ItemDrop.ItemData item, int amount, int x, int y)
    [HarmonyPatch(typeof(Inventory), "AddItem", new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) })]
    public static class Inventory_AddItem_Patch
    {
        public static void Prefix(Inventory __instance, int x, int y)
        {
            Container container = Container_Load_Patch.tombstoneContainer;
            if (container == null)
            {
                return;
            }

            if (x >= __instance.m_width)
            {
                int newWidth = x + 1;
                EquipmentAndQuickSlots.Log($"Changing tombstone container width ({container.m_width} -> {newWidth}) to fit item at pos {x},{y}");
                container.m_width = newWidth;
                __instance.m_width = newWidth;
            }

            if (y >= __instance.m_height)
            {
                int newHeight = y + 1;
                EquipmentAndQuickSlots.Log($"Changing tombstone container height ({container.m_height} -> {newHeight}) to fit item at pos {x},{y}");
                container.m_height = newHeight;
                __instance.m_height = newHeight;
            }
        }
    }
}
