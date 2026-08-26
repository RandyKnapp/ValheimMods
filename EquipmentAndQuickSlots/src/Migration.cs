using EquipmentAndQuickSlots.src;
using HarmonyLib;
using System.Linq;
using static EquipmentAndQuickSlots.Slots;
using static EquipmentAndQuickSlots.src.MiscPatches;

namespace EquipmentAndQuickSlots {
    // One-way migration of 2.x save data. Old versions kept two side inventories serialized into
    // player custom data (or, before Mistlands, into known texts behind a "<|>" sentinel). Their
    // items are moved into the real grid slots and the legacy keys are deleted.
    public static class Migration {
        public const string QuickSlotInventoryKey = "QuickSlotInventory";
        public const string EquipmentSlotInventoryKey = "EquipmentSlotInventory";
        public const string ExtendedPlayerDataKey = "ExtendedPlayerData";

        private const int LegacyQuickSlotCount = 3;
        private const int LegacyEquipSlotCount = 5;

        public static void MigrateLegacyData(Player player) {
            loadedPlayer = player;
            try {
                bool migrated = false;
                migrated |= MigrateInventory(player, EquipmentSlotInventoryKey, LegacyEquipSlotCount, equipItems: true);
                migrated |= MigrateInventory(player, QuickSlotInventoryKey, LegacyQuickSlotCount, equipItems: false);

                if (migrated) {
                    RemoveLegacyKey(player, ExtendedPlayerDataKey);
                    player.GetInventory().Changed();
                    SlotValidation.ValidateItems();
                    EquipmentAndQuickSlots.Log("Migrated legacy EAQS 2.x slot data into the inventory grid");
                }
            } finally {
                loadedPlayer = null;
            }
        }

        private static bool MigrateInventory(Player player, string key, int slotCount, bool equipItems) {
            if (!LoadValue(player, key, out string data) || string.IsNullOrEmpty(data))
                return false;

            var legacyInventory = new Inventory(key, null, slotCount, 1);
            try {
                var pkg = new ZPackage(data);
                legacyInventory.Load(pkg);
            } catch (System.Exception ex) {
                // A corrupt payload must not eat the rest of the load; leave the key in place so
                // the data can still be inspected or repaired.
                EquipmentAndQuickSlots.LogError($"Failed to parse legacy {key}, leaving it untouched: {ex.Message}");
                return false;
            }

            foreach (ItemDrop.ItemData item in legacyInventory.GetAllItemsInGridOrder().Where(item => item != null).ToList()) {
                legacyInventory.RemoveItem(item);
                PlaceMigratedItem(player, item, equipItems);
            }

            RemoveLegacyKey(player, key);
            return true;
        }

        private static void PlaceMigratedItem(Player player, ItemDrop.ItemData item, bool equip) {
            Inventory inventory = player.GetInventory();

            bool wasEquipped = item.m_equipped;
            item.m_equipped = false;

            if (!(TryFindSlotForItem(item, wasEquipped, out Slot slot) ? inventory.AddItem(item, slot.GridPosition) : inventory.AddItem(item))) {
                if (TryMakeFreeSpaceInPlayerInventory(out Vector2i gridPos)) {
                    item.m_gridPos = gridPos;
                } else {
                    // Park it in the last slot cell; the validation sweep finds it a home.
                    EquipmentAndQuickSlots.LogWarning($"No room for migrated item {item.m_shared.m_name}; parking it for validation to relocate");
                    item.m_gridPos = new Vector2i(InventoryWidth - 1, FullHeight - 1);
                }

                inventory.m_inventory.Add(item);
            }

            if (equip && wasEquipped && !player.EquipItem(item, triggerEquipEffects: false))
                item.m_equipped = false;
        }

        private static bool TryFindSlotForItem(ItemDrop.ItemData item, bool wasEquipped, out Slot slot) {
            slot = null;

            // The equipment cell predicate wants the item already equipped; during migration it is
            // not yet, so match by type for items that were saved as worn.
            if (wasEquipped)
                slot = GetEquipmentSlots().FirstOrDefault(s => s.IsFree && WouldFitEquipmentSlot(s, item));

            if (slot == null && TryFindEmptyQuickSlot(out Slot quickSlot))
                slot = quickSlot;

            return slot != null;
        }

        private static bool LoadValue(Player player, string key, out string value) {
            if (player.m_customData.TryGetValue(key, out value))
                return true;

            if (player.m_knownTexts.TryGetValue(key, out value))
                return true;

            return player.m_knownTexts.TryGetValue(TextsDialog_UpdateTextsList_Patch.LegacySentinel + key, out value);
        }

        private static void RemoveLegacyKey(Player player, string key) {
            player.m_customData.Remove(key);
            player.m_knownTexts.Remove(key);
            player.m_knownTexts.Remove(TextsDialog_UpdateTextsList_Patch.LegacySentinel + key);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        public static class Player_Load_MigrateLegacyData {
            [HarmonyPriority(Priority.High)]
            public static void Postfix(Player __instance) {
                bool hasLegacyData = __instance.m_customData.ContainsKey(EquipmentSlotInventoryKey)
                                     || __instance.m_customData.ContainsKey(QuickSlotInventoryKey)
                                     || __instance.m_knownTexts.ContainsKey(TextsDialog_UpdateTextsList_Patch.LegacySentinel + EquipmentSlotInventoryKey)
                                     || __instance.m_knownTexts.ContainsKey(TextsDialog_UpdateTextsList_Patch.LegacySentinel + QuickSlotInventoryKey);

                if (!hasLegacyData)
                    return;

                if (!FejdStartup.instance && !IsValidPlayer(__instance))
                    return;

                MigrateLegacyData(__instance);
            }
        }
    }
}
