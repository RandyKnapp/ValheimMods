using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // Two dirty-flag validators drained once per frame from the plugin's LateUpdate. Game events
    // only set a bool; all item movement happens here, outside any vanilla call stack — no
    // re-entrancy, no global equip locks.
    //
    // SlotsValidation:  an item sitting in a slot it no longer fits (unequipped armor in a
    //                   paperdoll cell, item in a deactivated quick slot) is relocated out.
    // ItemsValidation:  an equipped paperdoll-type item outside its cell is moved in; overlapping
    //                   and out-of-grid items are rescued.
    public static class SlotValidation {
        public static void ValidateSlots() => SlotsValidation.MarkDirty();
        public static void ValidateItems() => ItemsValidation.MarkDirty();

        public static void Validate() {
            ItemsValidation.Validate();
            SlotsValidation.Validate();
        }

        private static bool PutIntoFirstEmptySlot(ItemDrop.ItemData item) {
            if (TryGetSavedPlayerSlot(item, out Slot prevSlot) && prevSlot.IsActive && prevSlot.ItemBelongs(item) && (prevSlot.IsFree || item == prevSlot.Item)) {
                item.m_gridPos = prevSlot.GridPosition;
                return true;
            }

            Vector2i gridPos = PlayerInventory.FindEmptySlot(true);
            if (gridPos.x > -1 && gridPos.y > -1) {
                item.m_gridPos = gridPos;
                return true;
            }

            if (TryFindFreeSlotForItem(item, out Slot slot)) {
                item.m_gridPos = slot.GridPosition;
                return true;
            }

            if (TryMakeFreeSpaceInPlayerInventory(out Vector2i gridPosEmptied)) {
                item.m_gridPos = gridPosEmptied;
                return true;
            }

            return false;
        }

        internal static class SlotsValidation {
            private static bool isDirty = false;

            internal static void MarkDirty() => isDirty = true;

            internal static void Validate() {
                if (!isDirty || !Player.m_localPlayer || Player.m_localPlayer.m_isLoading)
                    return;

                isDirty = false;

                bool moved = false;
                for (int i = 0; i < slots.Length; i++) {
                    Slot slot = slots[i];
                    ItemDrop.ItemData item = slot.Item;
                    if (item == null || slot.ItemBelongs(item))
                        continue;

                    EquipmentAndQuickSlots.Log($"SlotValidation: item {item.m_shared.m_name} no longer belongs in slot {slot}");

                    if (slot.IsEquipmentSlot && IsEquippedByPlayer(item)) {
                        // Still equipped but in the wrong cell (type changed by an upgrade, or a
                        // slot got deactivated): try the matching cell first.
                        slot.ClearItemCache();
                        if (TryFindFreeEquipmentSlotForItem(item, out Slot freeEquipmentSlot)) {
                            item.m_gridPos = freeEquipmentSlot.GridPosition;
                            freeEquipmentSlot.ClearItemCache();
                            moved = true;
                            continue;
                        }

                        if (TryFindFirstUnequippedSlotForItem(item, out Slot slotToSwap)) {
                            ItemDrop.ItemData itemToSwap = slotToSwap.Item;
                            itemToSwap.m_gridPos = item.m_gridPos;
                            item.m_gridPos = slotToSwap.GridPosition;
                            // Clear THIS slot's cache too: the slot scans above re-cached the original
                            // occupant, so without this the re-read below saw the just-moved equipped
                            // item instead of the swapped-in one -- and immediately dragged it back
                            // out of the cell it was swapped into.
                            slot.ClearItemCache();
                            slotToSwap.ClearItemCache();
                            moved = true;
                            if (slot.ItemBelongs(item = slot.Item))
                                continue;

                            if (item == null)
                                continue;
                        }
                    }

                    if (PutIntoFirstEmptySlot(item))
                        moved = true;
                }

                if (moved)
                    PlayerInventory.Changed();
            }

            [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupEquipment))]
            private static class Humanoid_SetupEquipment_MarkSlotsDirty {
                private static void Postfix(Humanoid __instance) {
                    if (__instance is Player player && IsValidPlayer(player) && !player.m_isLoading)
                        MarkDirty();
                }
            }

            [HarmonyPatch(typeof(Player), nameof(Player.OnInventoryChanged))]
            private static class Player_OnInventoryChanged_MarkSlotsDirty {
                private static void Postfix(Player __instance) {
                    ClearCachedItems();

                    if (!IsValidPlayer(__instance) || __instance.m_isLoading)
                        return;

                    MarkDirty();
                }
            }
        }

        internal static class ItemsValidation {
            private static readonly HashSet<Vector2i> occupiedPositions = new HashSet<Vector2i>();
            private static readonly List<ItemDrop.ItemData> misplacedItems = new List<ItemDrop.ItemData>();

            private static bool isDirty = false;

            internal static void MarkDirty() => isDirty = true;

            internal static void Validate() {
                if (!isDirty || !Player.m_localPlayer || Player.m_localPlayer.m_isLoading)
                    return;

                isDirty = false;

                if (PlayerInventory == null || PlayerInventory.m_inventory == null)
                    return;

                occupiedPositions.Clear();
                misplacedItems.Clear();
                for (int index = 0; index < PlayerInventory.m_inventory.Count; index++) {
                    ItemDrop.ItemData item = PlayerInventory.m_inventory[index];
                    if (item == null)
                        continue;

                    // An equipped paperdoll-type item outside its equipment cell moves in — unless
                    // its unequip is already queued and animating (it was dragged out of the cell)
                    if (Player.m_localPlayer.IsItemEquiped(item) && !IsUnequipQueued(item) && IsEquipmentSlotItem(item)
                        && (GetItemSlot(item) is not Slot slotItem || !slotItem.IsEquipmentSlot)) {
                        if (TryFindFreeEquipmentSlotForItem(item, out Slot slot)) {
                            item.m_gridPos = slot.GridPosition;
                            PlayerInventory.Changed();
                        } else if (TryFindFirstUnequippedSlotForItem(item, out Slot slotToSwap)) {
                            if (slotToSwap.IsFree) {
                                item.m_gridPos = slotToSwap.GridPosition;
                            } else {
                                ItemDrop.ItemData itemToSwap = slotToSwap.Item;
                                itemToSwap.m_gridPos = item.m_gridPos;
                                item.m_gridPos = slotToSwap.GridPosition;
                            }
                            PlayerInventory.Changed();
                        }
                    }

                    if (ItemIsOverlapping(item) && PlayerInventory.GetOtherItemAt(item.m_gridPos.x, item.m_gridPos.y, item) != null) {
                        EquipmentAndQuickSlots.LogWarning($"ItemsValidation: item {item.m_shared.m_name} {item.m_gridPos} overlaps another item");
                        misplacedItems.Add(item);
                    } else if (ItemIsOutOfGrid(item)) {
                        EquipmentAndQuickSlots.LogWarning($"ItemsValidation: item {item.m_shared.m_name} {item.m_gridPos} is out of the inventory grid");
                        misplacedItems.Add(item);
                    }

                    occupiedPositions.Add(item.m_gridPos);
                }

                if (misplacedItems.Count(PutIntoFirstEmptySlot) > 0)
                    PlayerInventory.Changed();

                // Items settled in a slot no longer need their return-address tag. An item that
                // merely sits in a cell it doesn't belong in yet (armor awaiting the pickup
                // auto-equip, or about to be evicted) keeps it — the tag is what says where it goes.
                foreach (Slot slot in slots)
                    if (slot.Item is ItemDrop.ItemData slotItem && slot.ItemBelongs(slotItem))
                        PruneLastEquippedSlotFromItem(slotItem);

                // A parked marker only means something while the item is unequipped and still in
                // the cell it was parked in; once it is worn or has been moved, drop it so the
                // equipped-only rule applies again.
                foreach (ItemDrop.ItemData item in PlayerInventory.m_inventory) {
                    if (item == null || !item.m_customData.TryGetValue(customKeyParked, out string parkedSlot))
                        continue;

                    if (Player.m_localPlayer.IsItemEquiped(item) || GetItemSlot(item)?.ID != parkedSlot)
                        item.m_customData.Remove(customKeyParked);
                }
            }

            private static bool ItemIsOverlapping(ItemDrop.ItemData itemData) => occupiedPositions.Contains(itemData.m_gridPos);

            private static bool ItemIsOutOfGrid(ItemDrop.ItemData itemData) => itemData.m_gridPos.x < 0 || itemData.m_gridPos.x >= InventoryWidth
                                                                            || itemData.m_gridPos.y < 0 || itemData.m_gridPos.y >= FullHeight;

            [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveAll))]
            internal static class Inventory_MoveAll_MarkItemsDirty {
                private static void Postfix(Inventory __instance, Inventory fromInventory) {
                    if (__instance == PlayerInventory || fromInventory == PlayerInventory)
                        MarkDirty();
                }
            }

            [HarmonyPatch(typeof(TombStone), nameof(TombStone.EasyFitInInventory))]
            internal static class TombStone_EasyFitInInventory_MarkItemsDirty {
                private static void Postfix(Player player) {
                    if (IsValidPlayer(player))
                        MarkDirty();
                }
            }

            [HarmonyPatch]
            internal static class Humanoid_OnEquipUnequip_MarkItemsDirty {
                private static IEnumerable<MethodBase> TargetMethods() {
                    yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.EquipItem));
                    yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.UnequipItem));
                }

                private static void Prefix(Humanoid __instance) {
                    if (IsValidPlayer(__instance))
                        MarkDirty();
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        private static class InventoryGui_Show_Validate {
            private static void Postfix() {
                if (Player.m_localPlayer == null)
                    return;

                ValidateSlots();
                ValidateItems();
            }
        }
    }
}
