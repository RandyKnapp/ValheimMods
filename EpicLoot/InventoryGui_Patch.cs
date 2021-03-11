using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    // public void DoCrafting(Player player)
    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    public static class InventoryGui_DoCrafting_Patch
    {
        public static bool Prefix(InventoryGui __instance, Player player)
        {
            if (__instance.m_craftRecipe == null)
            {
                return false;
            }

            var newQuality = __instance.m_craftUpgradeItem?.m_quality + 1 ?? 1;
            if (newQuality > __instance.m_craftRecipe.m_item.m_itemData.m_shared.m_maxQuality 
                || !player.HaveRequirements(__instance.m_craftRecipe, false, newQuality) && !player.NoCostCheat() 
                || (__instance.m_craftUpgradeItem != null 
                    && !player.GetInventory().ContainsItem(__instance.m_craftUpgradeItem) 
                    || __instance.m_craftUpgradeItem == null 
                    && !player.GetInventory().HaveEmptySlot()))
            {
                return false;
            }

            if (__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc))
            {
                return false;
            }

            if (__instance.m_craftUpgradeItem != null && __instance.m_craftUpgradeItem.IsMagic())
            {
                Debug.LogWarning("Trying to upgrade magic item");
                var upgradeItem = __instance.m_craftUpgradeItem;
                player.UnequipItem(upgradeItem);

                upgradeItem.m_crafterID = player.GetPlayerID();
                upgradeItem.SetCrafterName(player.GetPlayerName());
                upgradeItem.m_quality = newQuality;

                if (!player.NoCostCheat())
                {
                    player.ConsumeResources(__instance.m_craftRecipe.m_resources, newQuality);
                }
                __instance.UpdateCraftingPanel();

                var currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
                if (currentCraftingStation != null)
                {
                    currentCraftingStation.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                }
                else
                {
                    __instance.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                }

                ++Game.instance.GetPlayerProfile().m_playerStats.m_crafts;
                Gogan.LogEvent("Game", "Crafted", __instance.m_craftRecipe.m_item.m_itemData.m_shared.m_name, (long)newQuality);

                return false;
            }

            return true;
        }
    }

    //public bool CanRepair(ItemDrop.ItemData item)
    /*[HarmonyPatch(typeof(InventoryGui), "CanRepair")]
    class InventoryGui_CanRepair_Patch
    {
        public static void Postfix(ItemDrop.ItemData item, ref bool __result)
        {
            if (Player.m_localPlayer == null || !item.m_shared.m_canBeReparied || Player.m_localPlayer.NoCostCheat())
            {
                return;
            }
            CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
            if (currentCraftingStation == null)
            {
                return;
            }
            if (item.IsMagic())
            {
                if (currentCraftingStation.m_name == "$piece_artisanstation")
                {
                    __result = true;
                }
                else
                {
                    __result = false;
                }
            }
        }
    }*/
}
