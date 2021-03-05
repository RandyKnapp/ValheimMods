using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    //public void UseHotbarItem(int index)
    [HarmonyPatch(typeof(Player), "UseHotbarItem")]
    class Player_UseHotbarItem_Patch
    {
        public static bool Prefix(Player __instance, int index)
        {
            if (index >= __instance.m_inventory.m_width)
            {
                Debug.Log($"UseHotbarItem: {index}");
                var itemAt = __instance.m_inventory.GetItemAt(
                    index - __instance.m_inventory.m_width - 1 + EquipmentAndQuickSlots.QuickUseSlotIndexStart, 
                    __instance.m_inventory.m_height - 1);
                if (itemAt == null)
                {
                    return false;
                }
                __instance.UseItem((Inventory)null, itemAt, false);
            }

            return true;
        }
    }
}
