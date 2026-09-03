using System;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // Safety net: on every save the slot-region contents are serialized into player custom data.
    // Custom data survives the mod being removed (vanilla load silently destroys out-of-range
    // items, but never touches unknown custom data keys), so a reinstall can restore the items.
    public static class InventoryBackup {
        public const string customKeyBackupID = "eaqs_backup";

        private const int EnvelopeVersion = 1;
        // TODO verify this is actually still needed
        private const string ServerCharactersGUID = "org.bepinex.plugins.servercharacters";

        private class BackupEnvelope {
            public string date;
            public string worldName;
            public int nrOfItems;
            public int width;
            public int height;
            public int visibleRows;
            public ZPackage inventoryPackage;
        }

        private static string SerializeBackup(Inventory inventory) {
            int width = InventoryWidth;
            int height = HiddenRows;
            Inventory backup = new Inventory(customKeyBackupID, null, width, height);

            foreach (ItemDrop.ItemData item in inventory.GetAllItemsInGridOrder().Where(item => item.m_gridPos.y >= VisibleRows)) {
                ItemDrop.ItemData backupItem = item.Clone();
                backup.AddItem(backupItem, new Vector2i(backupItem.m_gridPos.x, backupItem.m_gridPos.y - VisibleRows));
            }

            ZPackage inventoryPkg = new ZPackage();
            backup.Save(inventoryPkg);

            ZPackage envelope = new ZPackage();
            envelope.Write(EnvelopeVersion);
            envelope.Write(DateTime.Now.ToString("u"));
            envelope.Write(ZNet.instance?.GetWorldName() ?? "");
            envelope.Write(backup.NrOfItems());
            envelope.Write(width);
            envelope.Write(height);
            envelope.Write(VisibleRows);
            envelope.WriteCompressed(inventoryPkg);

            return envelope.GetBase64();
        }

        /// <summary>
        /// The visible row count in effect when the backup was written. The envelope has carried it
        /// since 3.0, which makes it the one durable record of where the slot region used to be for
        /// characters saved before the explicit marker existed.
        /// </summary>
        internal static bool TryGetBackupVisibleRows(Player player, out int visibleRows) {
            visibleRows = 0;

            if (!TryGetBackup(player, out BackupEnvelope envelope))
                return false;

            visibleRows = envelope.visibleRows;
            return visibleRows > 0;
        }

        private static bool TryGetBackup(Player player, out BackupEnvelope backup) {
            backup = null;

            if (!player.m_customData.TryGetValue(customKeyBackupID, out string base64) || string.IsNullOrEmpty(base64))
                return false;

            try {
                ZPackage envelope = new ZPackage(base64);
                int version = envelope.ReadInt();
                if (version != EnvelopeVersion) {
                    EquipmentAndQuickSlots.LogWarning($"Unknown backup envelope version {version}");
                    return false;
                }

                backup = new BackupEnvelope {
                    date = envelope.ReadString(),
                    worldName = envelope.ReadString(),
                    nrOfItems = envelope.ReadInt(),
                    width = envelope.ReadInt(),
                    height = envelope.ReadInt(),
                    visibleRows = envelope.ReadInt(),
                    inventoryPackage = envelope.ReadCompressedPackage(),
                };
            } catch (Exception ex) {
                EquipmentAndQuickSlots.LogWarning($"Error while reading inventory backup:\n{ex}");
                return false;
            }

            return backup.nrOfItems > 0;
        }

        private static bool PlayerCanRestoreBackup(Player player) {
            // Server-synced character profiles resolve conflicts on their own; a local restore
            // could duplicate items the server still knows about.
            if (Chainloader.PluginInfos.ContainsKey(ServerCharactersGUID))
                return false;

            if (player == null || !player.m_customData.ContainsKey(customKeyBackupID))
                return false;

            return !player.GetInventory().GetAllItems().Any(IsItemInSlot);
        }

        internal static bool TryRestoreBackup(Player player) {
            if (!TryGetBackup(player, out BackupEnvelope envelope))
                return false;

            Inventory inventory = player.GetInventory();
            if (inventory == null)
                return false;

            try {
                Inventory backup = new Inventory(customKeyBackupID, null, envelope.width, envelope.height);
                backup.Load(envelope.inventoryPackage);

                if (backup.NrOfItems() == 0)
                    return false;

                // If the visible row count grew since the backup, the old slot items may have
                // landed inside the visible grid on load; restoring on top would duplicate them.
                if (VisibleRows > envelope.visibleRows && AllBackupItemsPresentAtShiftedPositions(inventory, backup, envelope.visibleRows)) {
                    EquipmentAndQuickSlots.Log($"Backup restore skipped: visible rows changed {envelope.visibleRows} -> {VisibleRows} and the items are already in the inventory");
                    return false;
                }

                int restored = 0;
                foreach (ItemDrop.ItemData backupItem in backup.GetAllItemsInGridOrder()) {
                    Vector2i restoredPosition = new Vector2i(backupItem.m_gridPos.x, backupItem.m_gridPos.y + VisibleRows);
                    ItemDrop.ItemData restoredItem = backupItem.Clone();

                    if (!inventory.AddItem(restoredItem, restoredPosition))
                        continue;

                    restored++;
                    restoredItem = inventory.GetItemAt(restoredPosition.x, restoredPosition.y) ?? restoredItem;

                    if (restoredItem.IsEquipable() && restoredItem.m_equipped) {
                        restoredItem.m_equipped = false;
                        if (!player.EquipItem(restoredItem, triggerEquipEffects: false))
                            restoredItem.m_equipped = false;
                    }
                }

                if (restored > 0) {
                    EquipmentAndQuickSlots.Log($"Restored {restored} slot items from backup ({envelope.date}, world {envelope.worldName})");
                    SlotValidation.ValidateItems();
                }

                return restored > 0;
            } catch (Exception ex) {
                EquipmentAndQuickSlots.LogWarning($"Error while restoring inventory backup:\n{ex}");
                return false;
            }
        }

        private static bool AllBackupItemsPresentAtShiftedPositions(Inventory playerInventory, Inventory backup, int previousVisibleRows) {
            return backup.GetAllItems().All(item => playerInventory.GetItemAt(item.m_gridPos.x, item.m_gridPos.y + previousVisibleRows) is ItemDrop.ItemData playerItem
                                                    && playerItem.m_shared.m_name == item.m_shared.m_name
                                                    && playerItem.m_stack == item.m_stack
                                                    && playerItem.m_quality == item.m_quality);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Save))]
        private static class Player_Save_WriteBackup {
            [HarmonyPriority(Priority.Last)]
            private static void Prefix(Player __instance) {
                if (ValConfig.BackupEnabled.Value && __instance == CurrentPlayer)
                    __instance.m_customData[customKeyBackupID] = SerializeBackup(__instance.GetInventory());
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        private static class Player_Load_TryRestoreBackup {
            private static void Postfix(Player __instance) {
                if (!ValConfig.BackupEnabled.Value)
                    return;

                if (!IsValidPlayer(__instance))
                    return;

                if (!PlayerCanRestoreBackup(__instance))
                    return;

                TryRestoreBackup(__instance);
            }
        }
    }
}
