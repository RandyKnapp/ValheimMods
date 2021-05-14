using System;
using HarmonyLib;

namespace EquipmentAndQuickSlots.BetterArchery_Fix
{
    public static class BetterArcheryState
    {
        public const int RowCount = 2;
        public static bool quiverEnabled = false;
        public static int RowStartIndex = 0;
        public static int RowEndIndex = 0;

        public static void UpdateRowIndex(int quiverRowIndex)
        {
            if (quiverRowIndex > 0)
            {
                quiverEnabled = true;
                RowStartIndex = quiverRowIndex - 1;
                RowEndIndex = RowStartIndex + RowCount;
            }
            else
            {
                quiverEnabled = false;
                RowStartIndex = 0;
                RowEndIndex = 0;
            }
        }
    }
    [HarmonyPatch(typeof(Player), "Awake")]
    public static class Player_Awake_Patch
    {
        public static void Postfix()
        {
            Type BetterArcheryPlugin = Type.GetType("BetterArchery.BetterArchery, BetterArchery");
            if (BetterArcheryPlugin != null)
            {
                // I have no idea why this works.
                int QuiverRowIndex = Traverse.Create(BetterArcheryPlugin).Field("QuiverRowIndex").GetValue<int>();
                BetterArcheryState.UpdateRowIndex(QuiverRowIndex);
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), "GetEmptySlots")]
    public static class Inventory_GetEmptySlots_Patch
    {
        public static void Postfix(Inventory __instance, ref int __result)
        {
            if (!BetterArcheryState.quiverEnabled) {
                return;
            }

            if (!__instance.DoExtendedCall() && __instance.GetName() == "Inventory")
            {
                // Better Archery adds 2 (hidden from UI) rows whose slots we need to ignore
                // Prevents trying to move items to Better Archery's hidden inventory when interacting with tombstone
                // (TombStone.EasyFitInInventory calls GetEmptySlots)
                int subtractSlots = __instance.m_width * BetterArcheryState.RowCount;
                __result = Math.Max(0, __result - subtractSlots);
                EquipmentAndQuickSlots.LogWarning($"GetEmptySlots: subtracted {subtractSlots} BetterArchery slots (now {__result})");
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "OnTakeAll")]
    public static class InventoryGui_OnTakeAll_Patch
    {
        public static bool takingAllTombstone = false;

        public static void Prefix(InventoryGui __instance)
        {
            Container container = __instance.m_currentContainer;
            if (container != null && container.GetComponent<TombStone>())
            {
                takingAllTombstone = true;
            }
        }

        public static void Postfix()
        {
            takingAllTombstone = false;
        }
    }

    // public bool Interact(Humanoid character, bool hold)
    [HarmonyPatch(typeof(TombStone), "Interact")]
    public static class TombStone_Interact_Patch
    {
        public static bool interactingTombstone = false;

        public static void Prefix()
        {
            interactingTombstone = true;
        }

        public static void Postfix()
        {
            interactingTombstone = false;
        }
    }

    [HarmonyPatch(typeof(Inventory), "AddItem", new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) })]
    public static class Inventory_AddItem_Patch
    {
        public static bool Prefix(int x, int y, ref bool __result)
        {
            if (!BetterArcheryState.quiverEnabled)
            {
                return true;
            }

            if (InventoryGui_OnTakeAll_Patch.takingAllTombstone || TombStone_Interact_Patch.interactingTombstone)
            {
                if (y >= BetterArcheryState.RowStartIndex && y <= BetterArcheryState.RowEndIndex)
                {
                    EquipmentAndQuickSlots.Log($"Preventing Inventory.AddItem from inventory to BetterArchery slot ({x},{y})");
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}
