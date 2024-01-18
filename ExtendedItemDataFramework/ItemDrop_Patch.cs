using System.Collections.Generic;
using HarmonyLib;

namespace ExtendedItemDataFramework
{
    //public static string GetTooltip(ItemDrop.ItemData item, int qualityLevel, bool crafting)
    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", typeof(ItemDrop.ItemData), typeof(int), typeof(bool))]
    public static class ItemData_GetTooltip_Patch
    {
        private static string _realData;

        public static bool Prefix(ItemDrop.ItemData item, int qualityLevel, bool crafting)
        {
            _realData = item.m_crafterName;
            item.m_crafterName = item.GetCrafterName();
            return true;
        }

        public static void Postfix(ItemDrop.ItemData item, int qualityLevel, bool crafting)
        {
            item.m_crafterName = _realData;
        }
    }

    //public ItemDrop.ItemData Clone() => this.MemberwiseClone() as ItemDrop.ItemData;
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.Clone))]
    public static class ItemData_Clone_Patch
    {
        public static bool Prefix(ItemDrop.ItemData __instance, ref ItemDrop.ItemData __result)
        {
            if (__instance.IsExtended())
            {
                ExtendedItemDataFramework.Log($"Cloning extended item {__instance.m_shared.m_name}");
                __result = __instance.Extended().ExtendedClone();
                return false;
            }
            ExtendedItemDataFramework.Log($"Cloning DEFAULT item {__instance.m_shared.m_name}");
            return true;
        }

        //public static void Postfix(ItemDrop.ItemData __instance, ref ItemDrop.ItemData __result)
        //{
        //    // Fixes bug in vanilla valheim with cloning items with custom data
        //    __result.m_customData = new Dictionary<string, string>(__instance.m_customData);
        //}
    }

    //public void Load() => ItemDrop.LoadFromZDO(this.m_itemData, this.m_nview.GetZDO());
    [HarmonyPatch(typeof(ItemDrop), "Load")]
    public static class ItemDrop_Load_Patch
    {
        public static void Postfix(ItemDrop __instance)
        {
            if (!__instance.m_itemData.IsExtended())
            {
                __instance.m_itemData = new ExtendedItemData(
                    __instance.m_itemData,
                    __instance.m_itemData.m_stack,
                    __instance.m_itemData.m_durability,
                    new Vector2i(),
                    false,
                    __instance.m_itemData.m_quality,
                    __instance.m_itemData.m_variant,
                    __instance.m_itemData.m_crafterID,
                    __instance.m_itemData.m_crafterName);
            }
        }
    }

}
