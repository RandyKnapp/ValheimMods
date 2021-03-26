namespace EpicLoot
{
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
