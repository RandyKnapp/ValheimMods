namespace ExtendedItemDataFramework
{
    public class InventoryGui_Patch
    {
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    public static class InventoryGui_DoCrafting_Patch
    {
        public static bool Prefix(InventoryGui __instance, Player player, bool __runOriginal)
        {
            if (!__runOriginal || __instance.m_craftRecipe == null)
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

            var upgradeItem = __instance.m_craftUpgradeItem;
            if (upgradeItem != null)
            {
                upgradeItem.m_quality = newQuality;
                upgradeItem.m_durability = upgradeItem.GetMaxDurability();

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
    }
}