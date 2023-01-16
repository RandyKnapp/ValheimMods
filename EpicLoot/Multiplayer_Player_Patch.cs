using EpicLoot.Data;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot
{
    public class Multiplayer_Player_Patch
    {
        public static void UpdatePlayerZDOForEquipment(Player player, ItemDrop.ItemData item, bool equip)
        {
            if (!(player?.m_nview?.GetZDO() is ZDO zdo))
                return;

            var magicItem = item.GetMagicItem();
            var data = equip ? (magicItem != null && magicItem.IsUniqueLegendary() ? magicItem.LegendaryID : "") : "";

            var itemType = item.m_shared.m_itemType;
            switch (itemType)
            {
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Shield:
                    if (player.m_leftItem?.m_dropPrefab.name == item.m_dropPrefab.name)
                        zdo.Set("lf-ell", data);
                    if (player.m_rightItem?.m_dropPrefab.name == item.m_dropPrefab.name)
                        zdo.Set("ri-ell", data);
                    break;

                case ItemDrop.ItemData.ItemType.Chest: zdo.Set("ch-ell", data); break;
                case ItemDrop.ItemData.ItemType.Legs: zdo.Set("lg-ell", data); break;
                case ItemDrop.ItemData.ItemType.Helmet: zdo.Set("hl-ell", data); break;
                case ItemDrop.ItemData.ItemType.Shoulder: zdo.Set("sh-ell", data); break;
                case ItemDrop.ItemData.ItemType.Utility: zdo.Set("ut-ell", data); break;
            }

            //EpicLoot.Log($"Setting Equipment ZDO: {itemType}='{data}'");
        }

        [HarmonyPatch]
        public static class Humanoid_Patch
        {
            [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
            public class AttachExtendedDataToZDO_Humanoid_EquipItem_Patch
            {
                [UsedImplicitly]
                private static void Postfix(ItemDrop.ItemData item, bool __result, Humanoid __instance)
                {
                    if (__result && __instance == Player.m_localPlayer && item != null)
                        UpdatePlayerZDOForEquipment((Player)__instance, item, true);
                }
            }

            [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
            public class AttachExtendedDataToZDO_Humanoid_UnequipItem_Patch
            {
                [UsedImplicitly]
                private static void Prefix(Humanoid __instance, ItemDrop.ItemData item)
                {
                    if (__instance == Player.m_localPlayer && item != null && Player.m_localPlayer.IsItemEquiped(item))
                        UpdatePlayerZDOForEquipment((Player)__instance, item, false);
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        public static class WatchLegendaryEquipment_Player_Update_Patch
        {
            [UsedImplicitly]
            public static void Postfix(Player __instance)
            {
                if (__instance != null && __instance != Player.m_localPlayer && __instance.m_nview != null && __instance.m_nview.GetZDO() is ZDO zdo)
                {
                    var changed = DoCheck(__instance, zdo, "LeftItem", "lf-ell", ref __instance.m_leftItem);
                    changed = changed || DoCheck(__instance, zdo, "RightItem", "ri-ell", ref __instance.m_rightItem);
                    changed = changed || DoCheck(__instance, zdo, "ChestItem", "ch-ell", ref __instance.m_chestItem);
                    changed = changed || DoCheck(__instance, zdo, "LegItem", "lg-ell", ref __instance.m_legItem);
                    changed = changed || DoCheck(__instance, zdo, "HelmetItem", "hl-ell", ref __instance.m_helmetItem);
                    changed = changed || DoCheck(__instance, zdo, "ShoulderItem", "sh-ell", ref __instance.m_shoulderItem);
                    changed = changed || DoCheck(__instance, zdo, "UtilityItem", "ut-ell", ref __instance.m_utilityItem);

                    if (changed)
                    {
                        __instance.SetupVisEquipment(__instance.m_visEquipment, false);
                    }
                }
            }

            private static bool DoCheck(Player player, ZDO zdo, string equipKey, string legendaryDataKey, ref ItemDrop.ItemData itemData)
            {
                var zdoLegendaryID = zdo.GetString(legendaryDataKey);

                if (string.IsNullOrEmpty(zdoLegendaryID))
                {
                    var hadItem = itemData != null;
                    if (hadItem)
                        ForceResetVisEquipment(player, itemData);
                    itemData = null;
                    return hadItem;
                }

                var currentLegendaryID = itemData?.GetMagicItem()?.LegendaryID;
                if (currentLegendaryID == zdoLegendaryID)
                    return false;

                var itemHash = zdo.GetInt(equipKey);
                var itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
                if (itemPrefab?.GetComponent<ItemDrop>()?.m_itemData is ItemDrop.ItemData targetItemData)
                {
                    itemData = targetItemData.Clone();
                    itemData.m_durability = float.PositiveInfinity;
                    var magicItemComponent = itemData.Data().GetOrCreate<MagicItemComponent>();
                    var stubMagicItem = new MagicItem { Rarity = ItemRarity.Legendary, LegendaryID = zdoLegendaryID };
                    magicItemComponent.SetMagicItem(stubMagicItem);

                    ForceResetVisEquipment(player, itemData);
                }

                return false;
            }

            public static void ForceResetVisEquipment(Humanoid humanoid, ItemDrop.ItemData item)
            {
                if (humanoid == null || humanoid.m_visEquipment == null || item == null)
                    return;

                EpicLoot.LogWarning($"Force Reset VisEquip: {item.m_shared.m_itemType}");
                switch (item.m_shared.m_itemType)
                {
                    case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                        if (humanoid.m_rightItem != null && humanoid.m_rightItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch && humanoid.m_leftItem == null)
                            humanoid.m_visEquipment.m_currentLeftItemHash = -1;
                        humanoid.m_visEquipment.m_currentRightItemHash = -1;
                        break;

                    case ItemDrop.ItemData.ItemType.Shield:
                    case ItemDrop.ItemData.ItemType.Bow:
                        humanoid.m_visEquipment.m_currentLeftItemHash = -1;
                        break;

                    case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                    case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                        humanoid.m_visEquipment.m_currentRightItemHash = -1;
                        humanoid.m_visEquipment.m_currentLeftItemHash = -1;
                        break;

                    case ItemDrop.ItemData.ItemType.Chest:              humanoid.m_visEquipment.m_currentChestItemHash = -1;    break;
                    case ItemDrop.ItemData.ItemType.Legs:               humanoid.m_visEquipment.m_currentLegItemHash = -1;      break;
                    case ItemDrop.ItemData.ItemType.Helmet:             humanoid.m_visEquipment.m_currentHelmetItemHash = -1;   break;
                    case ItemDrop.ItemData.ItemType.Shoulder:           humanoid.m_visEquipment.m_currentShoulderItemHash = -1; break;
                    case ItemDrop.ItemData.ItemType.Utility:            humanoid.m_visEquipment.m_currentUtilityItemHash = -1;  break;
                    case ItemDrop.ItemData.ItemType.Tool:               humanoid.m_visEquipment.m_currentRightItemHash = -1;    break;

                    default:
                        break;
                }
            }
        }

        [HarmonyPatch]
        public static class WatchMultiplayerMagicEffects_Player_Patch
        {
            [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
            [HarmonyPostfix]
            public static void EquipItem_Postfix(Humanoid __instance)
            {
                if (__instance is Player player)
                    UpdateRichesAndLuck(player);
            }

            [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
            [HarmonyPostfix]
            public static void UnequipItem_Postfix(Humanoid __instance)
            {
                if (__instance is Player player)
                    UpdateRichesAndLuck(player);
            }

            public static void UpdateRichesAndLuck(Player player)
            {
                if (player == Player.m_localPlayer && player.m_nview?.GetZDO() is ZDO zdo)
                {
                    var currentZdoLuck = zdo.GetInt("el-luk");
                    var currentZdoRiches = zdo.GetInt("el-rch");

                    var currentLuck = (int)player.GetTotalActiveMagicEffectValue(MagicEffectType.Luck);
                    var currentRiches = (int)player.GetTotalActiveMagicEffectValue(MagicEffectType.Riches);

                    if (currentLuck != currentZdoLuck)
                    {
                        zdo.Set("el-luk", currentLuck);
                    }

                    if (currentRiches != currentZdoRiches)
                    {
                        zdo.Set("el-rch", currentRiches);
                    }
                }
            }
        }
    }
}
