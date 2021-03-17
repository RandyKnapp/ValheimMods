using System.Linq;
using HarmonyLib;
using UnityEngine;

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
            if (__instance.m_inventory.NrOfItems() == 0)
            {
                return;
            }

            EquipmentSlotHelper.AllowMove = false;
            __instance.UnequipAllItems();
            var allInventories = __instance.GetAllInventories();

            var gameObject = Object.Instantiate(__instance.m_tombstone, __instance.GetCenterPoint(), __instance.transform.rotation);
            var container = gameObject.GetComponent<Container>();

            // Modify tombstone prefab
            var totalPossibleSlots = allInventories.Sum(x => x.m_width * x.m_height);
            var width = __instance.m_inventory.m_width;
            var height = (totalPossibleSlots / width) + 1;
            container.m_width = width;
            container.m_height = height;
            container.m_inventory.m_width = width;
            container.m_inventory.m_height = height;

            var containerInventory = container.GetInventory();
            foreach (var inventory in allInventories)
            {
                foreach (var item in inventory.m_inventory)
                {
                    if (!item.m_shared.m_questItem && !item.m_equiped)
                    {
                        if (containerInventory.GetItemAt(item.m_gridPos.x, item.m_gridPos.y) != null)
                        {
                            containerInventory.AddItem(item);
                        }
                        else
                        {
                            containerInventory.m_inventory.Add(item);
                        }
                    }
                }

                inventory.m_inventory.RemoveAll(item => !item.m_shared.m_questItem && !item.m_equiped);
                inventory.Changed();
            }
            containerInventory.Changed();

            var tombStone = gameObject.GetComponent<TombStone>();
            var playerProfile = Game.instance.GetPlayerProfile();
            var name = playerProfile.GetName();
            var playerId = playerProfile.GetPlayerID();
            tombStone.Setup(name, playerId);

            Debug.LogWarning($"Creating tombstone for ({name}) with w:{width} h:{height} (total:{totalPossibleSlots})");

            EquipmentSlotHelper.AllowMove = true;
        }
    }
}
