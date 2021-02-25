using System;
using HarmonyLib;
using UnityEngine;

namespace Jam
{
    [HarmonyPatch(typeof(ZNetView), "Awake")]
    public static class ZNetView_Awake_Patch
    {
        public static bool Prefix(ZNetView __instance)
        {
            return true;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "DoCrafting", new Type[] {typeof(Player)})]
    public static class InventoryGui_DoCrafting_Patch
    {
        public static bool Prefix(InventoryGui __instance, Player player)
        {
            return true;
            /*if ((UnityEngine.Object)__instance.m_craftRecipe == (UnityEngine.Object)null)
                return false;
            int num = __instance.m_craftUpgradeItem != null ? __instance.m_craftUpgradeItem.m_quality + 1 : 1;
            if (num > __instance.m_craftRecipe.m_item.m_itemData.m_shared.m_maxQuality || !player.HaveRequirements(__instance.m_craftRecipe, false, num) && !player.NoCostCheat() || (__instance.m_craftUpgradeItem != null && !player.GetInventory().ContainsItem(__instance.m_craftUpgradeItem) || __instance.m_craftUpgradeItem == null && !player.GetInventory().HaveEmptySlot()))
                return false;
            if (__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_dlcrequired", 0, (Sprite)null);
            }
            else
            {
                int variant = __instance.m_craftVariant;
                if (__instance.m_craftUpgradeItem != null)
                {
                    variant = __instance.m_craftUpgradeItem.m_variant;
                    player.UnequipItem(__instance.m_craftUpgradeItem);
                    player.GetInventory().RemoveItem(__instance.m_craftUpgradeItem);
                }
                long playerId = player.GetPlayerID();
                string playerName = player.GetPlayerName();

                if (__instance.m_craftRecipe.name == "BlueberryJam")
                {

                }
                Debug.Log($"__instance.m_craftRecipe.m_item:{__instance.m_craftRecipe.m_item}");
                if (player.GetInventory().AddItem(__instance.m_craftRecipe.m_item.gameObject.name, __instance.m_craftRecipe.m_amount, num, variant, playerId, playerName) != null)
                {
                    if (!player.NoCostCheat())
                    {
                        player.ConsumeResources(__instance.m_craftRecipe.m_resources, num);
                    }
                    __instance.UpdateCraftingPanel();
                }
                CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
                if ((bool)(UnityEngine.Object)currentCraftingStation)
                    currentCraftingStation.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                else
                    __instance.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                ++Game.instance.GetPlayerProfile().m_playerStats.m_crafts;
                Gogan.LogEvent("Game", "Crafted", __instance.m_craftRecipe.m_item.m_itemData.m_shared.m_name, (long)num);
            }

            return false;*/
        }
    }
}
