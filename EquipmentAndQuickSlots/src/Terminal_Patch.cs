using System.Collections.Generic;
using EquipmentAndQuickSlots.src.MultiUtility;
using HarmonyLib;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    public static class Terminal_Patch {
        // InitTerminal early-returns after its first run, but a postfix still fires on every
        // Terminal.Awake (console, chat, scene reloads) — register once.
        private static bool _initialized;

        public static void Postfix() {
            if (_initialized)
                return;
            _initialized = true;

            Register("eaqs_validate", "Revalidate EAQS slots: relocates overlapping, out-of-grid and misplaced slot items", args => {
                if (Player.m_localPlayer == null) {
                    args.Context.AddString("No local player");
                    return;
                }

                SlotValidation.ValidateSlots();
                SlotValidation.ValidateItems();
                args.Context.AddString("EAQS: slot and item validation queued");
            });

            Register("invcheck", "Prints the player inventory grid contents and slot assignments", args => {
                var player = Player.m_localPlayer;
                if (player == null) {
                    args.Context.AddString("No local player");
                    return;
                }

                var inventory = player.GetInventory();
                args.Context.AddString($"Inventory {inventory.m_width}x{inventory.m_height} (visible rows: {VisibleRows}), {inventory.m_inventory.Count} items");
                args.Context.AddString($"Utility: {WearableUtilityItems} wearable, vanilla slot holds {player.m_utilityItem?.m_shared.m_name ?? "nothing"}");
                for (int i = 0; i < ExtraWearableUtilityItems; i++)
                    args.Context.AddString($"  extra utility {i + 2}: {MultiUtility.GetExtra(player, i)?.m_shared.m_name ?? "empty"}");
                foreach (var item in new List<ItemDrop.ItemData>(inventory.m_inventory)) {
                    var slot = GetItemSlot(item);
                    var location = slot != null ? $"slot {slot}" : IsGridPositionASlot(item.m_gridPos) ? "unassigned slot cell" : "grid";
                    args.Context.AddString($"  {item.m_shared.m_name} x{item.m_stack} at {item.m_gridPos} ({location}){(item.m_equipped ? " [equipped]" : "")}");
                }
            });

            Register("eaqs_api", "Prints EAQS API version, endpoints and slot states", args => {
                args.Context.AddString($"API version {API.GetApiVersion()}, plugin {API.GetPluginId()} {API.GetPluginVersion()}");
                args.Context.AddString($"Endpoints: {string.Join(", ", API.GetEndpointNames())}");
                args.Context.AddString($"Slots: {API.GetSlotIdsJson()}");
                foreach (var slot in slots) {
                    if (!slot.IsEmptySlot)
                        args.Context.AddString(API.GetSlotInfoJson(slot.ID.StartsWith(customSlotPrefix) ? slot.ID.Substring(customSlotPrefix.Length) : slot.ID));
                }
            });

            Register("eaqs_restorebackup", "Restores the slot-content backup into free slots", args => {
                var player = Player.m_localPlayer;
                if (player == null) {
                    args.Context.AddString("No local player");
                    return;
                }

                args.Context.AddString(InventoryBackup.TryRestoreBackup(player)
                    ? "EAQS: backup restored"
                    : "EAQS: no backup restored (missing, empty, or slots occupied)");
            });

            Register("breakequipment", "Sets durability of all equipped items to zero", args => {
                var player = Player.m_localPlayer;
                if (player == null)
                    return;

                foreach (var item in player.GetInventory().GetEquippedItems()) {
                    if (item.m_shared.m_useDurability)
                        item.m_durability = 0;
                }
                args.Context.AddString("EAQS: equipped items broken");
            });

            Register("dropall", "Drops the entire player inventory on the ground", args => {
                var player = Player.m_localPlayer;
                if (player == null)
                    return;

                var inventory = player.GetInventory();
                foreach (var item in new List<ItemDrop.ItemData>(inventory.m_inventory)) {
                    player.DropItem(inventory, item, item.m_stack);
                }
                args.Context.AddString("EAQS: inventory dropped");
            });
        }

        /// <summary>
        /// Registers a console command as a cheat: it needs devcommands and the cheat confirmation, and
        /// flags the profile as cheated. Vanilla only enables cheats on the machine running the world
        /// (Terminal.IsCheatsEnabled requires ZNet.IsServer), so a client of a dedicated server needs a
        /// mod that grants them to the server's admins, such as Server Devcommands.
        /// </summary>
        private static void Register(string name, string description, Terminal.ConsoleEvent action) {
            new Terminal.ConsoleCommand(name, description, action, isCheat: true);
        }
    }
}
