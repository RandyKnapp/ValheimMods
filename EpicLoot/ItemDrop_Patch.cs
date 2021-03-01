using System;
using HarmonyLib;

namespace EpicLoot
{
    //public static string GetTooltip(ItemDrop.ItemData item, int qualityLevel, bool crafting)
    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", new Type[] {typeof(ItemDrop.ItemData), typeof(int), typeof(bool)})]
    public static class ItemData_GetTooltip_Patch
    {
        public static void Postfix(ItemDrop.ItemData item, ref string __result)
        {
            /*if (item is UniqueItemData uniqueItemData)
            {
                __result += $"\n<color=magenta>Unique: {uniqueItemData.m_guid}</color>";

                var magicItem = EpicLoot.GetMagicItem(uniqueItemData.m_guid);
                if (magicItem != null)
                {
                    __result += magicItem.GetTooltip();
                }
            }*/
        }
    }
}
