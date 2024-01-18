using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            foreach (var inventory in __instance.GetAllInventories())
            {
                inventory.m_onChanged = null;
                inventory.m_onChanged += __instance.OnInventoryChanged;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "Awake")]
    public static class Player_Awake_Patch
    {
        public static void Postfix(Player __instance)
        {
            var inv = __instance.m_inventory;
            inv.m_onChanged = null;
            
            //__instance.m_inventory = new ExtendedInventory(__instance, inv.m_name, inv.m_bkg, inv.m_width, inv.m_height);

            var t = typeof(Humanoid).GetField(nameof(Humanoid.m_inventory),
                BindingFlags.Instance | BindingFlags.NonPublic);
            t.SetValue(__instance,new ExtendedInventory(__instance, inv.m_name, inv.m_bkg, inv.m_width, inv.m_height));
            
            __instance.m_inventory.Extended().OverrideAwake();
        }
    }

    //public void CreateTombStone()
    [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
    public static class Player_CreateTombStone_Patch
    {
        public static bool Prefix(Player __instance)
        {
            var allInventories = __instance.GetAllInventories();
            var totalItemCount = allInventories.Sum(x => x.NrOfItems());
            if (totalItemCount == 0)
                return true;

            EquipmentSlotHelper.AllowMove = false;
            UnequipNonEAQSSlots(__instance);

            EquipmentAndQuickSlots.LogWarning("== PLAYER DIED ==");
            var equipSlotInventory = __instance.GetEquipmentSlotInventory();
            var quickSlotInventory = __instance.GetQuickSlotInventory();
            var extraInventories = new List<Inventory>();
            if (!EquipmentAndQuickSlots.DontDropEquipmentOnDeath.Value)
            {
                __instance.UnequipAllItems();
                extraInventories.Add(equipSlotInventory);
            }

            if (!EquipmentAndQuickSlots.DontDropQuickslotsOnDeath.Value)
            {
                extraInventories.Add(quickSlotInventory);
            }

            if (extraInventories.Count == 0)
                return true;

            var tombstoneGameObject = Object.Instantiate(__instance.m_tombstone, __instance.GetCenterPoint() + Vector3.up + __instance.transform.forward * 0.5f, __instance.transform.rotation);
            var tombStone = tombstoneGameObject.GetComponent<TombStone>();
            var playerProfile = Game.instance.GetPlayerProfile();
            var name = playerProfile.GetName();
            var playerId = playerProfile.GetPlayerID();
            tombStone.Setup($"{name}'s Equipment", playerId);

            var container = tombstoneGameObject.GetComponent<Container>();
            var containerInventory = container.GetInventory();

            foreach (var inventory in extraInventories)
            {
	            List<ItemDrop.ItemData> retainItems = new List<ItemDrop.ItemData>();
                foreach (var item in inventory.m_inventory)
                {
                    if (!CreatureLevelControl.API.DropItemOnDeath(item))
                    {
	                    retainItems.Add(item);
	                    continue;
                    }

                    var oldSlot = item.m_gridPos;
                    var newSlot = containerInventory.FindEmptySlot(false);

                    if (inventory == equipSlotInventory && EquipmentAndQuickSlots.InstantlyReequipArmorOnPickup.Value)
                        item.m_customData["eaqs-e"] = "1";

                    if (inventory == quickSlotInventory)
                        item.m_customData["eaqs-qs"] = $"{oldSlot.x},{oldSlot.y}";

                    containerInventory.AddItem(item, item.m_stack, newSlot.x, newSlot.y);
                }

                inventory.m_inventory = retainItems;
                inventory.Changed();
                containerInventory.Changed();
            }

            // Continue on making the regular tombstone
            return true;
        }

        public static void Postfix(Player __instance)
        {
            EquipmentSlotHelper.AllowMove = true;
            if (EquipmentAndQuickSlots.DontDropEquipmentOnDeath.Value)
            {
                var equipSlotInventory = __instance.GetEquipmentSlotInventory();
                foreach (var equipment in equipSlotInventory.m_inventory.ToArray())
                {
                    __instance.EquipItem(equipment);
                }
            }
        }

        public static void UnequipNonEAQSSlots(Player __instance)
        {
            if (__instance.m_rightItem != null)
                __instance.UnequipItem(__instance.m_rightItem, false);
            if (__instance.m_leftItem != null)
                __instance.UnequipItem(__instance.m_leftItem, false);
            if (__instance.m_ammoItem != null)
                __instance.UnequipItem(__instance.m_ammoItem, false);
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
    public static class Game_SpawnPlayer_Patch
    {
	    public static void Postfix(Game __instance)
	    {
		    if (!__instance.m_firstSpawn)
		    {
			    Player player = Player.m_localPlayer;
			    if (player.GetEquipmentSlotInventory() is Inventory inventory)
			    {
				    foreach (var equipment in inventory.m_inventory.ToArray())
				    {
					    player.EquipItem(equipment);
				    }
			    }
		    }
	    }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.OnTakeAllSuccess))]
    public static class TombStone_OnTakeAllSuccess_Patch
    {
        public static void Postfix(TombStone __instance)
        {
            if (__instance.IsOwner())
            {
                TryReequipQuickslotItems();
                TryReequipEquipmentSlotItems(Player.m_localPlayer);
            }
        }

        private static Vector2i ParseVector2i(string s)
        {
            var value = new Vector2i(-1, -1);
            var parts = s.Split(',');
            if (parts.Length != 2)
                return value;
            if (!int.TryParse(parts[0], out value.x))
                return value;
            int.TryParse(parts[1], out value.y);
            return value;
        }

        public static void TryReequipQuickslotItems()
        {
            var player = Player.m_localPlayer;
            if (player == null)
                return;

            var inventory = player.GetInventory();
            var originalQuickSlotItems = inventory.m_inventory.Where(itemData => itemData.m_customData.ContainsKey("eaqs-qs")).ToArray();
            var quickSlotInventory = player.GetQuickSlotInventory();
            EquipmentAndQuickSlots.Log($"Found {originalQuickSlotItems.Length} original quick slot items");
            for (var index = 0; index < originalQuickSlotItems.Length; index++)
            {
                var itemData = originalQuickSlotItems[index];
                if (itemData.m_customData.ContainsKey("eaqs-qs"))
                {
                    Vector2i slot = ParseVector2i(itemData.m_customData["eaqs-qs"]);
                    if (slot.x >= 0 && slot.y >= 0 && quickSlotInventory.GetItemAt(slot.x, slot.y) == null)
                    {
                        itemData.m_customData.Remove("eaqs-qs");
                        if (EquipmentAndQuickSlots.InstantlyReequipQuickslotsOnPickup.Value)
                        {
                            EquipmentAndQuickSlots.Log($"Moving {itemData.m_shared.m_name} x{itemData.m_stack} back to {slot.x}, {slot.y}");
                            quickSlotInventory.MoveItemToThis(inventory, itemData, itemData.m_stack, slot.x, slot.y);
                        }
                    }
                }
            }
        }

        public static void TryReequipEquipmentSlotItems(Player player)
        {
            if (player == null)
                return;

            var inventory = player.GetInventory();
            var originalEquippedItems = inventory.m_inventory.Where(itemData => itemData.m_customData.ContainsKey("eaqs-e")).ToArray();
            EquipmentAndQuickSlots.Log($"Found {originalEquippedItems.Length} original equip slot items");
            foreach (var item in originalEquippedItems)
            {
                if (item.m_customData.ContainsKey("eaqs-e"))
                {
                    item.m_customData.Remove("eaqs-e");
                    if (EquipmentAndQuickSlots.InstantlyReequipArmorOnPickup.Value)
                    {
                        EquipmentAndQuickSlots.Log($"Reequipping {item.m_shared.m_name}");
                        player.EquipItem(item, false);
                    }
                }
            }
        }
    }

    //public bool Interact(Humanoid character, bool hold)
    /*{
        if (hold || this.m_container.GetInventory().NrOfItems() == 0)
        return false;
        if (!this.IsOwner() || !this.EasyFitInInventory(character as Player))
        return this.m_container.Interact(character, false);
        ZLog.Log((object) "Grave should fit in inventory, loot all");
        this.m_container.TakeAll(character);
        return true;
    }*/
    [HarmonyPatch(typeof(TombStone), nameof(TombStone.Interact))]
    public static class Tombstone_Interact_Patch
    {
        public static bool Prefix(TombStone __instance, Humanoid character, bool hold)
        {
            if (hold)
                return true;

            if (__instance.IsOwner())
                TombStone_OnTakeAllSuccess_Patch.TryReequipEquipmentSlotItems(character as Player);
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetFirstRequiredItem))]
    public static class Player_GetFirstRequiredItem_Patch
    {
        public static bool Prefix(Player __instance, ref ItemDrop.ItemData __result, Recipe recipe, int qualityLevel, object[] __args)
        {
            foreach (var resource in recipe.m_resources)
            {
                if (resource.m_resItem)
                {
                    var requiredAmount = resource.GetAmount(qualityLevel);
                    for (var quality = 0; quality <= resource.m_resItem.m_itemData.m_shared.m_maxQuality; ++quality)
                    {
                        var count = CountItems(__instance, resource.m_resItem.m_itemData.m_shared.m_name, quality);
                        if (count >= requiredAmount)
                        {
                            __args[3] = requiredAmount;
                            __result = GetItem(__instance, resource.m_resItem.m_itemData.m_shared.m_name, quality);
                            return false;
                        }
                    }
                }
            }

            __args[3] = 0;
            __result = null;
            return false;
        }

        public static int CountItems(Player player, string name, int quality)
        {
            var count = 0;
            var inventories = player.GetAllInventories();
            foreach (var inventory in inventories)
            {
                if (inventory == player.GetEquipmentSlotInventory())
                    continue;

                foreach (var itemData in inventory.m_inventory)
                {
                    if (itemData.m_shared.m_name == name && (quality < 0 || quality == itemData.m_quality))
                        count += itemData.m_stack;
                }
            }

            return count;
        }

        public static ItemDrop.ItemData GetItem(Player player, string name, int quality)
        {
            var inventories = player.GetAllInventories();
            foreach (var inventory in inventories)
            {
                if (inventory == player.GetEquipmentSlotInventory())
                    continue;

                foreach (var itemData in inventory.m_inventory)
                {
                    if (itemData.m_shared.m_name == name && (quality < 0 || quality == itemData.m_quality))
                        return itemData;
                }
            }

            return null;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.Clone))]
    public static class ItemData_Clone_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref ItemDrop.ItemData __result)
        {
            // Fixes bug in vanilla valheim with cloning items with custom data
            __result.m_customData = new Dictionary<string, string>(__instance.m_customData);
        }
    }
}
