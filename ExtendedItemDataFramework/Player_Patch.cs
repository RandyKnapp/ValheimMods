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
                    var changed = DoCheck(__instance, zdo, "LeftItem", "LeftItem ExtendedItemData", ref __instance.m_leftItem);
                    changed = changed || DoCheck(__instance, zdo, "RightItem", "RightItem ExtendedItemData", ref __instance.m_rightItem);
                    changed = changed || DoCheck(__instance, zdo, "ChestItem", "ChestItem ExtendedItemData", ref __instance.m_chestItem);
                    changed = changed || DoCheck(__instance, zdo, "LegItem", "LegItem ExtendedItemData", ref __instance.m_legItem);
                    changed = changed || DoCheck(__instance, zdo, "HelmetItem", "HelmetItem ExtendedItemData", ref __instance.m_helmetItem);
                    changed = changed || DoCheck(__instance, zdo, "ShoulderItem", "ShoulderItem ExtendedItemData", ref __instance.m_shoulderItem);
                    changed = changed || DoCheck(__instance, zdo, "UtilityItem", "UtilityItem ExtendedItemData", ref __instance.m_utilityItem);

                    if (changed)
                    {
                        __instance.SetupVisEquipment(__instance.m_visEquipment, false);
                    }
                }
            }

            private static bool DoCheck(Player player, ZDO zdo, string equipKey, string extendedDataKey, ref ItemDrop.ItemData itemData)
            {
                var extendedData = zdo.GetString(extendedDataKey);

                if (string.IsNullOrEmpty(extendedData))
                {
                    var hadItem = itemData != null;
                    ForceResetVisEquipment(player, itemData);
                    itemData = null;
                    return hadItem;
                }

                if (itemData?.m_crafterName == extendedData)
                {
                    return false;
                }

                var itemHash = zdo.GetInt(equipKey);
                var itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
                if (itemPrefab?.GetComponent<ItemDrop>()?.m_itemData is ItemDrop.ItemData targetItemData)
                {
                    Debug.LogError($"Converting other player item to extended item: {itemPrefab.name}");
                    itemData = new ExtendedItemData(targetItemData,
                        targetItemData.m_stack,
                        float.PositiveInfinity, // avoid durability checks running
                        targetItemData.m_gridPos,
                        true,
                        targetItemData.m_quality,
                        targetItemData.m_variant,
                        targetItemData.m_crafterID,
                        extendedData);

                    ForceResetVisEquipment(player, itemData);
                }

                return false;
            }

            public static void ForceResetVisEquipment(Humanoid humanoid, ItemDrop.ItemData item)
            {
                if (humanoid == null || humanoid.m_visEquipment == null || item == null)
                {
                    return;
                }

                if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool)
                {
                    humanoid.m_visEquipment.m_currentRightItemHash = -1;
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
                {
                    if (humanoid.m_rightItem != null && humanoid.m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch && humanoid.m_leftItem == null)
                    {
                        humanoid.m_visEquipment.m_currentLeftItemHash = -1;
                    }
                    humanoid.m_visEquipment.m_currentRightItemHash = -1;
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                {
                    humanoid.m_visEquipment.m_currentLeftItemHash = -1;
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow)
                {
                    humanoid.m_visEquipment.m_currentLeftItemHash = -1;
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon)
                {
                    humanoid.m_visEquipment.m_currentRightItemHash = -1;
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Chest)
                {
                    humanoid.m_visEquipment.m_currentChestItemHash = -1;
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Legs)
                {
                    humanoid.m_visEquipment.m_currentLegItemHash = -1;
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Helmet)
                {
                    humanoid.m_visEquipment.m_currentHelmetItemHash = -1;
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shoulder)
                {
                    humanoid.m_visEquipment.m_currentShoulderItemHash = -1;
                }
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Utility)
                {
                    humanoid.m_visEquipment.m_currentUtilityItemHash = -1;
                }
            }
        }
    }
}
