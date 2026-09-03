using EquipmentAndQuickSlots.src;
using HarmonyLib;
using System.Globalization;
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

        // The visible row count the character was last saved with. The slot region sits directly
        // under the visible grid, so this is what says whether the saved grid positions still mean
        // what they meant last session.
        public const string VisibleRowsKey = "eaqs_visible_rows";

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

        // The slot rows move whenever the visible row count changes: a config edit, a rows mod
        // installed or removed, or -- the case this was written for -- Better Archery stopping
        // inflating the player inventory by two rows. Saved items keep the grid position they had,
        // so without this the equipment and quick slot contents come back on the wrong rows, or
        // past the end of the inventory where nothing can reach them again.
        //
        // The shift is a bijection over the slot region (everything at or below the old first slot
        // row moves by the same delta), so it cannot make two items collide. Anything that was in
        // the old visible grid and now finds itself on a slot cell is left to the validation sweep.
        internal static void MigrateSlotRegionRows(Player player, int previousVisibleRows) {
            int delta = VisibleRows - previousVisibleRows;
            Inventory inventory = player.GetInventory();
            if (delta == 0 || inventory == null)
                return;

            int moved = 0;
            foreach (ItemDrop.ItemData item in inventory.m_inventory) {
                if (item.m_gridPos.y < previousVisibleRows)
                    continue;

                item.m_gridPos = new Vector2i(item.m_gridPos.x, item.m_gridPos.y + delta);
                moved++;
            }

            if (moved == 0)
                return;

            // Record the new count now: a second Load on this same object would otherwise read the
            // stale marker and shift everything a second time.
            player.m_customData[VisibleRowsKey] = VisibleRows.ToString(CultureInfo.InvariantCulture);

            ClearCachedItems();
            inventory.Changed();
            SlotValidation.ValidateItems();
            SlotValidation.ValidateSlots();

            // Ungated: this moved the player's gear, and a report about misplaced items has to
            // be answerable from the log even with logging turned off.
            EquipmentAndQuickSlots.LogInfo($"Visible rows changed {previousVisibleRows} -> {VisibleRows}; moved {moved} item(s) with the slot region");
        }

        private static bool TryGetPreviousVisibleRows(Player player, out int visibleRows) {
            if (player.m_customData.TryGetValue(VisibleRowsKey, out string stored)
                && int.TryParse(stored, NumberStyles.Integer, CultureInfo.InvariantCulture, out visibleRows)
                && visibleRows > 0)
                return true;

            // No marker: characters saved before it existed still carry the row count inside the
            // slots backup envelope.
            return InventoryBackup.TryGetBackupVisibleRows(player, out visibleRows);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Save))]
        private static class Player_Save_WriteVisibleRows {
            [HarmonyPriority(Priority.Last)]
            private static void Prefix(Player __instance) {
                if (__instance == CurrentPlayer)
                    __instance.m_customData[VisibleRowsKey] = VisibleRows.ToString(CultureInfo.InvariantCulture);
            }
        }

        // Ahead of the backup restore (default priority), so that runs against corrected positions.
        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        public static class Player_Load_MigrateSlotRegionRows {
            [HarmonyPriority(Priority.High)]
            public static void Postfix(Player __instance) {
                if (!FejdStartup.instance && !IsValidPlayer(__instance))
                    return;

                if (!TryGetPreviousVisibleRows(__instance, out int previousVisibleRows))
                    return;

                loadedPlayer = __instance;
                try {
                    MigrateSlotRegionRows(__instance, previousVisibleRows);
                } finally {
                    loadedPlayer = null;
                }
            }
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
