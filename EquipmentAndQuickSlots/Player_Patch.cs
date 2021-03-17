using HarmonyLib;

namespace EquipmentAndQuickSlots
{
    [HarmonyPatch(typeof(Player), "Save")]
    public static class Player_Save_Patch
    {
        public static bool Prefix(Player __instance)
        {
            __instance.BeforeSave();
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), "Load")]
    public static class Player_Load_Patch
    {
        public static void Postfix(Player __instance)
        {
            __instance.AfterLoad();
        }
    }

    [HarmonyPatch(typeof(Player), "Awake")]
    public static class Player_Awake_Patch
    {
        public static void Postfix(Player __instance)
        {
            var inv = __instance.m_inventory;
            inv.m_onChanged = null;
            __instance.m_inventory = new ExtendedInventory(__instance, inv.m_name, inv.m_bkg, inv.m_width, inv.m_height);
            __instance.m_inventory.m_onChanged += __instance.OnInventoryChanged;
            __instance.m_inventory.Extended().OverrideAwake();
        }
    }

    //public void CreateTombStone()
    [HarmonyPatch(typeof(Player), "CreateTombStone")]
    public static class Player_CreateTombStone_Patch
    {
        public static void Prefix(Player __instance)
        {
            // TODO: Extend tombstone inventory size
        }
    }
}
