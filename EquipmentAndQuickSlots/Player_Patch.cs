using HarmonyLib;

namespace EquipmentAndQuickSlots
{
    //public void UseHotbarItem(int index)
    [HarmonyPatch(typeof(Player), "UseHotbarItem")]
    public static class Player_UseHotbarItem_Patch
    {
        public static bool Prefix(Player __instance, int index)
        {
            if (index > __instance.m_inventory.m_width)
            {
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
