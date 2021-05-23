using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace ExtendedItemDataFramework
{
    public static class Player_Patch
    {
        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        public static class WatchExtendedItemDataOnEquipment_Player_Update_Patch
        {
            [UsedImplicitly]
            public static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer && __instance.m_nview?.GetZDO() is ZDO zdo)
                {
                    check(zdo, "LeftItem", "LeftItem ExtendedItemData", ref __instance.m_leftItem);
                    check(zdo, "RightItem", "RightItem ExtendedItemData", ref __instance.m_rightItem);
                    check(zdo, "ChestItem", "ChestItem ExtendedItemData", ref __instance.m_chestItem);
                    check(zdo, "LegItem", "LegItem ExtendedItemData", ref __instance.m_legItem);
                    check(zdo, "HelmetItem", "HelmetItem ExtendedItemData", ref __instance.m_helmetItem);
                    check(zdo, "ShoulderItem", "ShoulderItem ExtendedItemData", ref __instance.m_shoulderItem);
                    check(zdo, "UtilityItem", "UtilityItem ExtendedItemData", ref __instance.m_utilityItem);
                }
            }

            private static void check(ZDO zdo, string equipKey, string extendedDataKey, ref ItemDrop.ItemData itemData)
            {
                string extendedData = zdo.GetString(extendedDataKey);

                if (extendedData == "")
                {
                    itemData = null;
                    return;
                }

                if (itemData?.m_crafterName == extendedData)
                {
                    return;
                }

                var itemHash = zdo.GetInt(equipKey);
                var itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
                if (itemPrefab?.GetComponent<ItemDrop>()?.m_itemData is ItemDrop.ItemData targetItemData)
                {
                    var itemDataCopy = targetItemData.Clone();
                    itemDataCopy.m_crafterName = extendedData;
                    itemDataCopy.m_durability = float.PositiveInfinity; // avoid durability checks running
                    itemData = new ExtendedItemData(itemDataCopy);
                }
            }
        }
    }
}
