using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    public static class InventoryPatches {
        public static void UpdatePlayerInventorySize() {
            if (CurrentPlayer == null)
                return;

            if (CurrentPlayer.m_inventory.m_height != FullHeight) {
                EquipmentAndQuickSlots.Log($"Player inventory height changed {CurrentPlayer.m_inventory.m_height} -> {FullHeight}");
                CurrentPlayer.m_inventory.m_height = FullHeight;
                CurrentPlayer.m_inventory.Changed();
            }

            SlotValidation.ValidateItems();
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_SetInventoryHeight {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Player __instance) {
                // Base rows come from the prefab (or a rows mod); the slot positions depend on
                // them, so recompute before the first Load places items.
                CaptureBaseRows(__instance.m_inventory);
                UpdateSlotsGridPosition();
                __instance.m_inventory.m_height = FullHeight;

                ApplyBaseCarryWeight(__instance);
            }
        }

        internal static void ApplyBaseCarryWeight(Player player) {
            if (player == null)
                return;

            float baseCarryWeight = ValConfig.BaseCarryWeight.Value;
            if (Mathf.Approximately(baseCarryWeight, ValConfig.VanillaCarryWeight))
                return;

            player.m_maxCarryWeight = baseCarryWeight;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
        private static class Player_OnSpawned_UpdateInventoryOnSpawn {
            private static void Postfix(Player __instance) {
                if (__instance != Player.m_localPlayer)
                    return;

                UpdatePlayerInventorySize();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Player_Update_UpdateInventoryHeight {
            private static void Postfix(Player __instance) {
                if (__instance != Player.m_localPlayer)
                    return;

                __instance.m_inventory.m_height = FullHeight;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Save))]
        private static class Player_Save_SaveLastEquippedSlots {
            private static void Prefix(Player __instance) {
                if (__instance.GetInventory() != PlayerInventory)
                    return;

                SaveLastEquippedSlotsToItems();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.AutoPickup))]
        public static class Player_AutoPickup_PreventAutoPickupInSlots {
            public static bool preventAddItem = false;

            [HarmonyPriority(Priority.First)]
            private static void Prefix(Player __instance) => preventAddItem = ValConfig.PreventAutoPickup.Value && __instance == CurrentPlayer;

            [HarmonyPriority(Priority.First)]
            private static void Postfix() => preventAddItem = false;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.SlotsUsedPercentage))]
        private static class Inventory_SlotsUsedPercentage_ExcludeInactiveSlots {
            private static void Postfix(Inventory __instance, ref float __result) {
                if (__instance != PlayerInventory)
                    return;

                __result = (float)__instance.m_inventory.Count / InventorySizeActive * 100f;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetEmptySlots))]
        private static class Inventory_GetEmptySlots_CountVisibleAndQuickSlots {
            [HarmonyPriority(Priority.First)]
            private static void Postfix(Inventory __instance, ref int __result) {
                if (__instance != PlayerInventory)
                    return;

                __result = VisibleRows * __instance.m_width
                           - __instance.m_inventory.Count(item => !IsItemInSlot(item))
                           + (Player_AutoPickup_PreventAutoPickupInSlots.preventAddItem ? 0 : GetEmptyQuickSlots());
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveEmptySlot))]
        private static class Inventory_HaveEmptySlot_CountVisibleAndQuickSlots {
            [HarmonyPriority(Priority.First)]
            private static void Postfix(Inventory __instance, ref bool __result) {
                if (__instance != PlayerInventory)
                    return;

                __result = __instance.GetEmptySlots() > 0;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.FindEmptySlot))]
        private static class Inventory_FindEmptySlot_VisibleRowsThenQuickSlots {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Inventory __instance) {
                if (__instance != PlayerInventory)
                    return;

                __instance.m_height = VisibleRows;
            }

            [HarmonyPriority(Priority.First)]
            private static void Postfix(Inventory __instance, ref Vector2i __result) {
                if (__instance != PlayerInventory)
                    return;

                __instance.m_height = FullHeight;

                // A finished craft upgrade re-adds the (possibly equipped) item: hand the capacity
                // probe the cell it came from. The old gate compared m_craftTimer against
                // m_craftDuration, but vanilla completes the craft at the SKILL-REDUCED duration
                // (InventoryGui.UpdateRecipe), and TryFindFreeSlotForItem could never return an
                // equipment cell for an item DoCrafting had just unequipped -- so with a full visible
                // grid the upgraded item was destroyed (AddItem's FindEmptySlot capacity probe failed
                // after the original was already removed from the inventory).
                if (__result == emptyPosition
                    && InventoryGui_DoCrafting_UpgradeInSlot.UpgradeSourceSlot is Slot sourceSlot
                    && sourceSlot.IsFree) {
                    __result = sourceSlot.GridPosition;
                }

                if (__result == emptyPosition && Inventory_AddItem_ByName_FindAppropriateSlot.itemToFindSlot != null
                    && TryFindFreeSlotForItem(Inventory_AddItem_ByName_FindAppropriateSlot.itemToFindSlot, out Slot byNameSlot)) {
                    __result = byNameSlot.GridPosition;
                }

                if (__result == emptyPosition && !Player_AutoPickup_PreventAutoPickupInSlots.preventAddItem)
                    __result = FindEmptyQuickSlot();
            }

            // The prefix shrinks the height; if the original (or another mod's patch) throws, the
            // postfix never runs and every later size check would see a 4-row inventory. Restore
            // unconditionally.
            [HarmonyPriority(Priority.First)]
            private static void Finalizer(Inventory __instance) {
                if (__instance == PlayerInventory)
                    __instance.m_height = FullHeight;
            }
        }

        // Tracks the cell and equipped state of the item a craft upgrade consumes, for the duration
        // of DoCrafting. The FindEmptySlot postfix above hands the freed cell to the capacity probe,
        // and the postfix here re-equips the upgraded item: vanilla leaves an upgraded item
        // unequipped, and an unequipped item does not belong in an equipment cell (the validation
        // sweep would evict it to the visible grid a frame later).
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
        internal static class InventoryGui_DoCrafting_UpgradeInSlot {
            internal static Slot UpgradeSourceSlot;
            private static bool wasEquipped;

            [HarmonyPriority(Priority.First)]
            private static void Prefix(InventoryGui __instance) {
                UpgradeSourceSlot = null;
                wasEquipped = false;

                if (__instance.m_craftUpgradeItem is not ItemDrop.ItemData item)
                    return;

                UpgradeSourceSlot = GetSlotInGrid(item.m_gridPos);
                wasEquipped = CurrentPlayer != null && CurrentPlayer.IsItemEquiped(item);
            }

            private static void Postfix() {
                Slot slot = UpgradeSourceSlot;
                bool reequip = wasEquipped;
                UpgradeSourceSlot = null;
                wasEquipped = false;

                if (!reequip || slot == null || CurrentPlayer == null)
                    return;

                // Vanilla re-added the upgraded item at its old grid position. Re-equip it so it
                // stays in its cell; when the craft did not go through, the occupant is still the
                // old, already-equipped item and EquipItem's IsItemEquiped guard makes this a no-op.
                ItemDrop.ItemData occupant = slot.Item;
                if (occupant != null && !CurrentPlayer.IsItemEquiped(occupant))
                    CurrentPlayer.EquipItem(occupant, triggerEquipEffects: false);
            }

            // The source slot must not survive an exception in DoCrafting: FindEmptySlot would keep
            // rerouting unrelated additions into that cell.
            private static void Finalizer() {
                UpgradeSourceSlot = null;
                wasEquipped = false;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Drag & drop rules

        private static bool PassDropItem(string source, InventoryGrid grid, Inventory fromInventory, ItemDrop.ItemData item, Vector2i pos) {
            if (item.m_gridPos == pos && grid.m_inventory == fromInventory)
                return true;

            Player player = Player.m_localPlayer;

            // Dragging an equipped item out of its equipment cell: allowed onto an empty regular
            // cell (the OnSelectedItem prefix unequips first); blocked onto occupied cells so a
            // swap can't push a random item into the paperdoll.
            if (fromInventory == PlayerInventory && GetItemSlot(item) is Slot itemSlot && itemSlot.IsEquipmentSlot && player.IsItemEquiped(item)) {
                if (grid.m_inventory.GetItemAt(pos.x, pos.y) != null) {
                    EquipmentAndQuickSlots.Log($"{source}: prevented swapping equipped item {item.m_shared.m_name} out of {itemSlot}");
                    return false;
                }
            }

            // Dropping into a slot cell of the player inventory: the item must fit the cell by
            // type. Whether it may stay (equipped-only for paperdoll cells) is the validation
            // sweep's job — vanilla unequips an item for the duration of a drag move, so the
            // equipped state can't be judged here. Equipment cells additionally have to pass the
            // drag-to-equip check, or a second copy of a worn utility item would land in a cell
            // only to be swept back out a frame later.
            if (grid.m_inventory == PlayerInventory && GetSlotInGrid(pos) is Slot targetSlot
                && (!targetSlot.ItemFits(item) || targetSlot.IsEquipmentSlot && !WouldFitEquipmentSlot(targetSlot, item))) {
                EquipmentAndQuickSlots.Log($"{source}: prevented dropping {item.m_shared.m_name} into unfit slot {targetSlot}");
                return false;
            }

            // Swapping: the displaced item must fit the dragged item's source slot
            ItemDrop.ItemData itemAt = grid.m_inventory.GetItemAt(pos.x, pos.y);
            if (itemAt != null && itemAt != item && fromInventory == PlayerInventory && GetSlotInGrid(item.m_gridPos) is Slot sourceSlot && !sourceSlot.ItemFits(itemAt)) {
                EquipmentAndQuickSlots.Log($"{source}: prevented swapping {item.m_shared.m_name} with unfit item {itemAt.m_shared.m_name}");
                return false;
            }

            return true;
        }

        // Equip and unequip by drag & drop go through vanilla's minor-action queue (animated,
        // interruptible, gated by the item's equip duration) — never the instant EquipItem.
        // Vanilla's OnSelectedItem body calls RemoveEquipAction on both items before it moves
        // them, so anything that must outlive the move is queued from the postfix.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnSelectedItem))]
        public static class InventoryGui_OnSelectedItem_DragRules {
            private static ItemDrop.ItemData _equipAfterDrop;
            private static ItemDrop.ItemData _unequipAfterDrop;

            public static bool Prefix(InventoryGui __instance, InventoryGrid grid, Vector2i pos) {
                _equipAfterDrop = null;
                _unequipAfterDrop = null;

                Player player = Player.m_localPlayer;
                if (player == null || player.IsTeleporting() || !__instance.m_dragGo || __instance.m_dragItem == null || __instance.m_dragInventory == null)
                    return true;

                ItemDrop.ItemData dragItem = __instance.m_dragItem;
                bool targetIsPlayerGrid = grid.m_inventory == PlayerInventory;
                bool dragFromPlayerGrid = __instance.m_dragInventory == PlayerInventory;
                Slot targetSlot = targetIsPlayerGrid ? GetSlotInGrid(pos) : null;
                Slot sourceSlot = dragFromPlayerGrid ? GetItemSlot(dragItem) : null;
                bool dragItemEquipped = player.IsItemEquiped(dragItem);

                // Drag-to-equip: an unequipped equippable dropped on its matching equipment cell
                // queues its equip; the item shows the equip progress where it sits and the
                // validation sweep moves it into the cell once it is worn.
                if (targetSlot != null && WouldFitEquipmentSlot(targetSlot, dragItem) && !dragItemEquipped) {
                    if (dragFromPlayerGrid) {
                        __instance.SetupDragItem(null, null, 1);
                        QueueEquip(player, dragItem);
                        return false;
                    }

                    // From a container: vanilla moves it into the cell first, the postfix queues
                    // the equip; ItemBelongs keeps it in the cell while the equip is pending.
                    _equipAfterDrop = dragItem;
                }

                if (sourceSlot != null && sourceSlot.IsEquipmentSlot && dragItemEquipped) {
                    ItemDrop.ItemData targetItem = grid.m_inventory.GetItemAt(pos.x, pos.y);

                    // Swap-equip: the worn item dragged onto an unequipped item of the same type
                    // in the grid means "wear that one instead" — queue its equip; the equip
                    // unequips the dragged one and the sweep swaps the positions.
                    if (targetIsPlayerGrid && targetItem != null && targetItem != dragItem
                        && WouldFitEquipmentSlot(sourceSlot, targetItem) && !player.IsItemEquiped(targetItem)) {
                        __instance.SetupDragItem(null, null, 1);
                        QueueEquip(player, targetItem);
                        return false;
                    }

                    // Drag-to-unequip: the worn item dragged onto an empty regular or quick slot
                    // cell. Vanilla moves it (and keeps it worn); the postfix queues the animated
                    // unequip, and the sweep leaves it alone while that is pending. Into a
                    // container vanilla unequips on its own — an item can't stay worn outside the
                    // inventory.
                    if (targetIsPlayerGrid && (targetSlot == null || !targetSlot.IsEquipmentSlot) && targetItem == null)
                        _unequipAfterDrop = dragItem;
                }

                return PassDropItem("InventoryGui.OnSelectedItem", grid, __instance.m_dragInventory, dragItem, pos);
            }

            public static void Postfix() {
                ItemDrop.ItemData equip = _equipAfterDrop;
                ItemDrop.ItemData unequip = _unequipAfterDrop;
                _equipAfterDrop = null;
                _unequipAfterDrop = null;

                Player player = Player.m_localPlayer;
                if (player == null || PlayerInventory == null)
                    return;

                if (equip != null && PlayerInventory.ContainsItem(equip))
                    QueueEquip(player, equip);

                if (unequip != null && PlayerInventory.ContainsItem(unequip) && (GetItemSlot(unequip) is not Slot landed || !landed.IsEquipmentSlot))
                    QueueUnequip(player, unequip);
            }
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
        public static class InventoryGrid_DropItem_DropPrevention {
            public static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item, Vector2i pos) => PassDropItem("InventoryGrid.DropItem", __instance, fromInventory, item, pos);
        }

        // ---------------------------------------------------------------------------------------
        // AddItem rerouting

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int))]
        private static class Inventory_AddItem_ItemData_amount_x_y_TargetPositionRerouting {
            [HarmonyPriority(Priority.Last)]
            private static void Prefix(Inventory __instance, ItemDrop.ItemData item, ref int x, ref int y) {
                if (__instance != PlayerInventory)
                    return;

                // During load the player's equipment state isn't established yet; validation will
                // sort misplaced items out after EquipInventoryItems has run.
                if (Inventory_AddItem_OnLoad_Marker.inCall && CurrentPlayer.m_isLoading)
                    return;

                if (item == null)
                    return;

                // If another item is at the position, let vanilla stack logic run
                if (__instance.GetItemAt(x, y) != null)
                    return;

                if (GetSlotInGrid(new Vector2i(x, y)) is not Slot slot || slot.ItemFits(item))
                    return;

                if (TryFindFreeSlotForItem(item, out Slot freeSlot)) {
                    x = freeSlot.GridPosition.x;
                    y = freeSlot.GridPosition.y;
                    return;
                }

                if (TryMakeFreeSpaceInPlayerInventory(out Vector2i gridPos)) {
                    x = gridPos.x;
                    y = gridPos.y;
                }
            }

            // Vanilla Inventory.Load ignores this method's return value and destroys the item.
            // If the add failed during load, force the item in and let validation place it.
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Inventory __instance, ItemDrop.ItemData item, int x, int y, int amount, ref bool __result) {
                if (__instance != PlayerInventory || !Inventory_AddItem_OnLoad_Marker.inCall || __result)
                    return;

                amount = Mathf.Min(amount, item.m_stack);

                ItemDrop.ItemData itemData = item.Clone();
                itemData.m_stack = amount;

                EquipmentAndQuickSlots.LogWarning($"Item loss prevention on load: {item.m_shared.m_name} at {x},{y} amount {amount}");

                if (TryFindFreeSlotForItem(itemData, out Slot slot))
                    itemData.m_gridPos = slot.GridPosition;
                else if (TryMakeFreeSpaceInPlayerInventory(out Vector2i gridPos))
                    itemData.m_gridPos = gridPos;
                else
                    itemData.m_gridPos = new Vector2i(InventoryWidth - 1, FullHeight - 1); // last cell; validation will find it a home

                __instance.m_inventory.Add(itemData);
                item.m_stack -= amount;
                __result = true;
                __instance.Changed();
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData))]
        private static class Inventory_AddItem_ItemData_QuickSlotFallback {
            [HarmonyPriority(Priority.First)]
            private static void Postfix(Inventory __instance, ItemDrop.ItemData item, bool __runOriginal, ref bool __result) {
                if (__instance != PlayerInventory)
                    return;

                if (__result || !__runOriginal)
                    return;

                if (Player_AutoPickup_PreventAutoPickupInSlots.preventAddItem)
                    return;

                if (!TryFindFreeSlotForItem(item, out Slot slot))
                    return;

                item.m_gridPos = slot.GridPosition;
                __instance.m_inventory.Add(item);

                __instance.Changed();
                __result = true;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(Vector2i))]
        private static class Inventory_AddItem_ItemData_pos_TargetPositionRerouting {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Inventory __instance, ItemDrop.ItemData item, ref Vector2i pos) {
                if (__instance != PlayerInventory)
                    return;

                if (item == null)
                    return;

                if (__instance.GetItemAt(pos.x, pos.y) != null || GetSlotInGrid(pos) is not Slot slot || slot.ItemFits(item))
                    return;

                // If free stack space exists for this item, let vanilla stack logic handle it
                if (item.m_shared.m_maxStackSize > 1) {
                    int freeStacks = __instance.GetAllItems()
                        .Where(itemInv => item.m_shared.m_name == itemInv.m_shared.m_name && item.m_quality == itemInv.m_quality && item.m_worldLevel == itemInv.m_worldLevel)
                        .Sum(itemInv => itemInv.m_shared.m_maxStackSize - itemInv.m_stack);

                    if (freeStacks > item.m_stack)
                        return;
                }

                if (TryFindFreeSlotForItem(item, out Slot freeSlot)) {
                    pos = freeSlot.GridPosition;
                    return;
                }

                if (TryMakeFreeSpaceInPlayerInventory(out Vector2i gridPos))
                    pos = gridPos;
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool))]
        public static class Inventory_AddItem_ByName_FindAppropriateSlot {
            public static ItemDrop.ItemData itemToFindSlot = null;

            [HarmonyPriority(Priority.First)]
            private static void Prefix(Inventory __instance, string name) {
                if (__instance != PlayerInventory)
                    return;

                ItemDrop component = ObjectDB.instance?.GetItemPrefab(name)?.GetComponent<ItemDrop>();
                if (component == null)
                    return;

                if (component.m_itemData.m_shared.m_maxStackSize > 1)
                    return;

                itemToFindSlot = component.m_itemData;
            }

            [HarmonyPriority(Priority.First)]
            private static void Postfix() => itemToFindSlot = null;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(float), typeof(Vector2i), typeof(bool), typeof(int), typeof(int), typeof(long), typeof(string), typeof(Dictionary<string, string>), typeof(int), typeof(bool))]
        public static class Inventory_AddItem_OnLoad_Marker {
            public static bool inCall = false;

            [HarmonyPriority(Priority.First)]
            private static void Prefix() => inCall = true;

            [HarmonyPriority(Priority.First)]
            private static void Postfix() => inCall = false;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CanAddItem), typeof(ItemDrop.ItemData), typeof(int))]
        private static class Inventory_CanAddItem_ItemData_CountSlotCapacity {
            private static readonly List<ItemDrop.ItemData> tempItems = new List<ItemDrop.ItemData>();

            [HarmonyPriority(Priority.First)]
            private static void Prefix(Inventory __instance) {
                if (__instance != PlayerInventory)
                    return;

                __instance.m_height = VisibleRows;

                tempItems.Clear();

                for (int i = __instance.m_inventory.Count - 1; i >= 0; i--) {
                    ItemDrop.ItemData invItem = __instance.m_inventory[i];
                    if (IsItemInSlot(invItem)) {
                        tempItems.Add(invItem);
                        __instance.m_inventory.RemoveAt(i);
                    }
                }

                tempItems.Reverse();
            }

            [HarmonyPriority(Priority.First)]
            private static void Postfix(Inventory __instance, ItemDrop.ItemData item, int stack, ref bool __result) {
                if (__instance != PlayerInventory)
                    return;

                RestoreState(__instance);

                if (__result)
                    return;

                int freeStackSpace = __instance.FindFreeStackSpace(item.m_shared.m_name, item.m_worldLevel);
                int freeQuickSlotStackSpace = __instance.GetEmptySlots() * item.m_shared.m_maxStackSize;

                int sizeCombined = freeStackSpace + freeQuickSlotStackSpace;
                if (sizeCombined < 0)
                    sizeCombined = int.MaxValue;

                if (sizeCombined >= stack) {
                    __result = true;
                } else if (stack <= item.m_shared.m_maxStackSize && !Player_AutoPickup_PreventAutoPickupInSlots.preventAddItem) {
                    __result = TryFindFreeSlotForItem(item, out _);
                }
            }

            [HarmonyPriority(Priority.First)]
            private static void Finalizer(Inventory __instance) {
                if (__instance == PlayerInventory)
                    RestoreState(__instance);
            }

            private static void RestoreState(Inventory inventory) {
                inventory.m_height = FullHeight;

                if (tempItems.Count > 0) {
                    inventory.m_inventory.AddRange(tempItems);
                    tempItems.Clear();
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.MoveInventoryToGrave))]
        private static class Inventory_MoveInventoryToGrave_KeepFullHeight {
            private static void Prefix(Inventory original) {
                if (original != PlayerInventory)
                    return;

                original.m_height = FullHeight;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Stack All protection: slot items are pulled out of the list before a container's
        // Stack All snapshots it, so they can never be dumped into the chest.

        private static class PreventStackAll {
            private static readonly List<ItemDrop.ItemData> removedItems = new List<ItemDrop.ItemData>();

            internal static void RemoveItemsFromPlayerInventory() {
                for (int i = PlayerInventory.m_inventory.Count - 1; i >= 0; i--) {
                    ItemDrop.ItemData item = PlayerInventory.m_inventory[i];
                    if (!IsItemInSlot(item))
                        continue;

                    removedItems.Add(item);
                    PlayerInventory.m_inventory.RemoveAt(i);
                }
            }

            internal static void BringItemsBack() {
                if (removedItems.Count == 0)
                    return;

                PlayerInventory.m_inventory.AddRange(removedItems);
                removedItems.Clear();
            }

            [HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll))]
            internal static class Inventory_StackAll_PreventStackingItemsFromSlots {
                public static bool inCall = false;

                [HarmonyPriority(Priority.First)]
                private static void Prefix(Inventory fromInventory) {
                    if ((inCall = fromInventory == PlayerInventory && ValConfig.PreventStackAll.Value) == false)
                        return;

                    RemoveItemsFromPlayerInventory();
                }

                [HarmonyPriority(Priority.First)]
                private static void Finalizer() {
                    if (inCall)
                        BringItemsBack();

                    inCall = false;
                }
            }

            // StackAll fires Changed() on the player inventory mid-call; restore before any
            // observer sees the shortened list. The vanilla loop iterates a snapshot taken after
            // our prefix, so protection is unaffected.
            [HarmonyPatch(typeof(Inventory), nameof(Inventory.Changed))]
            private static class Inventory_Changed_BringItemsBack {
                [HarmonyPriority(Priority.First)]
                private static void Prefix(Inventory __instance) {
                    if (__instance == PlayerInventory && Inventory_StackAll_PreventStackingItemsFromSlots.inCall)
                        BringItemsBack();
                }
            }
        }
    }
}
