using System.Collections.Generic;
using System.Linq;
using EpicLoot.Compatibility;
using EpicLoot.Config;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.ShardStones {
    // The cells are a synthetic one-row Inventory, one cell per socket, reconciled back into
    // MagicItem.Sockets on every change and on close.
    public static class ShardUI {
        private static ItemDrop.ItemData _item;
        private static Inventory _slots;
        private static ShardSlotPanel _panel;
        private static bool _reconciling;

        public static bool IsOpen => _item != null && _slots != null && _panel != null && _panel.IsAlive;

        public static bool IsOpenFor(ItemDrop.ItemData item) => IsOpen && item != null && _item == item;

        private static bool IsSlotInventory(Inventory inventory) => IsOpen && inventory != null && inventory == _slots;

        private static bool IsSlotGrid(InventoryGrid grid) => IsOpen && grid != null && grid == _panel.Grid;

        // Bypasses Inventory.AddItem, which merges into an existing stack of the same name: two slots
        // holding identical shards would collapse into one cell and SaveSockets would destroy one.
        private static void PlaceInSocketSlot(Inventory inv, ItemDrop.ItemData item, Vector2i pos) {
            item.m_gridPos = pos;
            item.m_stack = 1;
            inv.GetAllItems().Add(item);
            inv.Changed();
        }

        private static Vector2i FindEmptySocketSlot() {
            for (var x = 0; x < _slots.GetWidth(); x++) {
                if (_slots.GetItemAt(x, 0) == null) {
                    return new Vector2i(x, 0);
                }
            }

            return new Vector2i(-1, -1);
        }

        // Null when a socket's source prefab is missing: opening anyway would leave that cell empty, and
        // SaveSockets would reconcile the socket away on close.
        private static Inventory BuildSocketInventory(MagicItem magicItem) {
            var width = Mathf.Max(1, magicItem.SocketCount);
            var inv = new Inventory("Sockets", null, width, 1);

            for (var i = 0; i < magicItem.Sockets.Count && i < width; i++) {
                var item = ShardSocketManager.ReconstructShardItem(magicItem.Sockets[i]);
                if (item == null) {
                    return null;
                }

                PlaceInSocketSlot(inv, item, new Vector2i(i, 0));
            }

            return inv;
        }

        private static void SaveSockets() {
            if (_reconciling || !IsOpen) {
                return;
            }

            if (!_item.IsMagic(out var magicItem)) {
                return;
            }

            _reconciling = true;
            try {
                var countBefore = magicItem.Sockets.Count;
                magicItem.Sockets.Clear();
                // Grid order, not list order: drags and swaps append to the end of the backing list, and
                // the stored order is what tooltips render.
                foreach (var item in _slots.GetAllItems().OrderBy(x => x.m_gridPos.x)) {
                    if (!ShardSocketManager.ResolveSocketedEffect(_item, item, out var effect, out var color, out var rarity)) {
                        continue;
                    }
                    magicItem.Sockets.Add(new SocketedEffect(
                        effect, ShardSocketManager.GetSourcePrefabName(item), rarity) {
                        ShardType = color
                    });
                }
                ShardSocketManager.RecomputeSocketValues(_item, magicItem);
                // A swap keeps the count and still places a stone, so it reports Socket.
                var reason = magicItem.Sockets.Count < countBefore
                    ? API.ChangeReason.Unsocket
                    : API.ChangeReason.Socket;
                API.WithChangeReason(reason, () => _item.SaveMagicItem(magicItem));

                if (Player.m_localPlayer != null) {
                    EquipmentEffectCache.Reset(Player.m_localPlayer);
                }
            } finally {
                _reconciling = false;
            }
        }

        private static void Open(InventoryGui invGui, ItemDrop.ItemData item) {
            if (item == null || !item.IsMagic(out var magicItem) || !magicItem.HasSockets()) {
                return;
            }

            // Close also cancels a drag that started in its slots, so a stone picked up from item A cannot
            // be dropped into item B while still counted on A.
            if (IsOpen) {
                Close(invGui);
            }

            var panel = ShardSlotPanel.Get(invGui);
            if (panel == null) {
                ShowSocketMessage("$mod_epicloot_socket_unavailable");
                return;
            }

            var inv = BuildSocketInventory(magicItem);
            if (inv == null) {
                ShowSocketMessage("$mod_epicloot_socket_unavailable");
                return;
            }

            inv.m_onChanged += SaveSockets;
            _item = item;
            _slots = inv;
            _panel = panel;

            panel.Show(
                Localization.instance.Localize(item.m_shared.m_name) + " - " +
                Localization.instance.Localize("$mod_epicloot_sockets"),
                magicItem.SocketCount);
            panel.Grid.UpdateInventory(inv, null, invGui.m_dragItem);

            if (ZInput.IsGamepadActive()) {
                panel.Grid.SetGamepadSelection(new Vector2i(0, 0));
                panel.Focus();
            }
        }

        public static void Close() {
            Close(InventoryGui.instance);
        }

        private static void Close(InventoryGui invGui) {
            if (!IsOpen) {
                _item = null;
                _slots = null;
                return;
            }

            if (Player.m_localPlayer != null) {
                SaveSockets();
            }

            // A drag that started in these slots must not outlive them: dropped later, it would be moved
            // out of an inventory nothing reconciles any more.
            if (invGui != null && invGui.m_dragInventory == _slots) {
                invGui.SetupDragItem(null, null, 1);
            }

            _slots.m_onChanged -= SaveSockets;
            _panel.Hide();
            _item = null;
            _slots = null;
        }

        private static void Toggle(InventoryGui invGui, ItemDrop.ItemData item) {
            if (IsOpenFor(item)) {
                Close(invGui);
            } else {
                Open(invGui, item);
            }
        }

        // Has to be an Update prefix that consumes the press: vanilla Update reads GetButtonDown("Use") and,
        // while the inventory is visible, resets the button and Hide()s the inventory, and the player grid
        // reads the gamepad buttons from inside Update too.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        [HarmonyPriority(Priority.First)]
        public static class InventoryGui_Update_Patch {
            // Priority.First: it only reads and consumes input, so running first is harmless, and a
            // container mod's own Update handler must not open its container on a press aimed at the slots.
            [UsedImplicitly]
            private static void Prefix(InventoryGui __instance) {
                if (!InventoryGui.IsVisible()) {
                    return;
                }

                if (InventoryPromptHost.IsOpen) {
                    InventoryPromptHost.Update();
                    return;
                }

                // The open gesture is only read over the player grid; with the panel's grid focused it closes
                // the panel instead of reaching that grid as a plain X press (a right-click on the stone).
                if (IsOpen && _panel.HasFocus && SocketOverlayInput.GamepadPressed()) {
                    ZInput.ResetButtonStatus(SocketOverlayInput.GamepadButtonName);
                    Close(__instance);
                    return;
                }

                if (SocketOverlayInput.TryGetPressTarget(__instance, out ItemDrop.ItemData item, out bool gamepad)) {
                    if (gamepad) {
                        OnGamepadGesture(__instance, item);
                    } else {
                        OnShortcut(__instance, item);
                    }
                    return;
                }

                KeepInventoryOpenOnUse(__instance);
            }

            [UsedImplicitly]
            private static void Postfix(InventoryGui __instance) {
                if (!IsOpen) {
                    return;
                }

                var stillThere = __instance.m_playerGrid.GetInventory()
                    .GetItemAt(_item.m_gridPos.x, _item.m_gridPos.y);
                if (stillThere != _item) {
                    Close(__instance);
                    return;
                }

                _panel.Place();
                _panel.Grid.UpdateInventory(_slots, null, __instance.m_dragItem);
            }

            private static void OnShortcut(InventoryGui invGui, ItemDrop.ItemData item) {
                if (item == null) {
                    return;
                }

                // A shortcut that is only the Use key is another mod's container gesture over that mod's item.
                if (SocketOverlayInput.IsBlockedFor(item, gamepad: false)) {
                    EpicLoot.Log($"[ShardUI] Yielding the Use press over {item.m_shared.m_name}: another mod " +
                        "owns its container, and the Socket Overlay Shortcut is only the Use key.");
                    return;
                }

                bool onUse = SocketOverlayInput.MainKeyIsUse();
                if (!SocketOverlayInput.HasSockets(item)) {
                    // A miss, not a request to shut the inventory.
                    if (onUse && ELConfig.KeepInventoryOpenOverItems.Value && !ForeignItemContainers.HasContainer(item)) {
                        ZInput.ResetButtonStatus("Use");
                    }
                    return;
                }

                // Consumed so vanilla does not Hide() the inventory and a container mod's handler (which reads
                // Use later, in UpdateContainer) sees no press.
                if (onUse) {
                    ZInput.ResetButtonStatus("Use");
                }

                Toggle(invGui, item);
            }

            // Consuming the button by name is what stops the player grid from also using or picking up the item.
            private static void OnGamepadGesture(InventoryGui invGui, ItemDrop.ItemData item) {
                if (!SocketOverlayInput.HasSockets(item)) {
                    return;
                }

                ZInput.ResetButtonStatus(SocketOverlayInput.GamepadButtonName);
                Toggle(invGui, item);
            }

            // Player doesn't read Use while the inventory is visible, so swallowing it only stops the inventory
            // closing. Another mod's container keeps it: plain Use is the gesture it advertises.
            private static void KeepInventoryOpenOnUse(InventoryGui invGui) {
                if (!ELConfig.KeepInventoryOpenOverItems.Value || !ZInput.GetButtonDown("Use")) {
                    return;
                }

                var pos = Input.mousePosition;
                var item = invGui.m_playerGrid.GetItem(new Vector2i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)));
                if (item == null) {
                    return;
                }

                if (ForeignItemContainers.HasContainer(item)) {
                    EpicLoot.Log($"[ShardUI] Yielding the Use press over {item.m_shared.m_name}: another mod owns its container.");
                    return;
                }

                ZInput.ResetButtonStatus("Use");
            }
        }

        // Vanilla's no-chest branch of UpdateContainer cancels, every frame, any drag that did not start in
        // the player's inventory, so a stone picked up out of a slot would be put straight back. This
        // replays that branch (no chest, or one whose ownership has not arrived) but leaves a drag from
        // the slots alone; an owned chest's branch cancels nothing, so vanilla runs untouched then.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
        public static class InventoryGui_UpdateContainer_Patch {
            [UsedImplicitly]
            private static bool Prefix(InventoryGui __instance) {
                var chest = __instance.m_currentContainer;
                if (!IsOpen || (chest != null && chest.IsOwner())) {
                    return true;
                }

                __instance.m_container.gameObject.SetActive(false);
                var dragInventory = __instance.m_dragInventory;
                if (dragInventory != null && dragInventory != _slots &&
                    (Player.m_localPlayer == null || dragInventory != Player.m_localPlayer.GetInventory())) {
                    __instance.SetupDragItem(null, null, 1);
                }

                return false;
            }
        }

        // Vanilla only looks for the controller selection, which the drag ghost follows, in its own two grids.
        [HarmonyPatch(typeof(InventoryGui), "GetSelectedGamepadElement")]
        public static class InventoryGui_GetSelectedGamepadElement_Patch {
            [UsedImplicitly]
            private static void Postfix(ref RectTransform __result) {
                if (__result == null && IsOpen && _panel.HasFocus) {
                    __result = _panel.Grid.GetGamepadSelectedElement();
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        public static class InventoryGui_Hide_Patch {
            [UsedImplicitly]
            private static void Prefix(InventoryGui __instance) {
                // Ahead of the panel check: a confirmation raised over the plain inventory (Brokkr's Gift)
                // has no panel behind it and must still be dropped when the window closes.
                InventoryPromptHost.Cancel();

                if (IsOpen) {
                    Close(__instance);
                }
            }
        }

        internal static void ShowSocketMessage(string reason) {
            if (Player.m_localPlayer != null && !string.IsNullOrEmpty(reason)) {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize(reason));
            }
        }

        private static void OpenBreakPrompt(ItemDrop.ItemData item) {
            if (item == null || InventoryGui.instance == null) {
                return;
            }

            var body = string.Format(
                Localization.instance.Localize("$mod_epicloot_socket_break_body"),
                Localization.instance.Localize(item.m_shared.m_name));

            var prompt = SocketBreakPrompt.Create(InventoryGui.instance.transform,
                Localization.instance.Localize("$mod_epicloot_socket_break_title"), body);

            if (!InventoryPromptHost.Open(prompt, () => BreakSocketedItem(item))) {
                // No prefab to confirm with -- refuse the removal rather than destroying it unconfirmed.
                ShowSocketMessage("$mod_epicloot_socket_mustbreak");
            }
        }

        // Taking it out of the synthetic inventory fires m_onChanged, and SaveSockets reconciles the
        // socket away.
        private static void BreakSocketedItem(ItemDrop.ItemData item) {
            if (item == null || !IsOpen || !_slots.ContainsItem(item)) {
                return;
            }

            _slots.RemoveItem(item);
        }

        private static void QuickSocket(InventoryGui invGui, ItemDrop.ItemData item) {
            var player = Player.m_localPlayer;
            if (player == null) {
                return;
            }

            if (!ShardSocketManager.CanSocket(_item, item, out var reason)) {
                ShowSocketMessage(reason);
                return;
            }

            var slot = FindEmptySocketSlot();
            if (slot.x < 0) {
                ShowSocketMessage("$mod_epicloot_socket_nofreeslot");
                return;
            }

            _slots.MoveItemToThis(player.GetInventory(), item, 1, slot.x, slot.y);
            invGui.m_moveItemEffects.Create(invGui.transform.position, Quaternion.identity);
        }

        private static void QuickRemoveFromSocket(InventoryGui invGui, ItemDrop.ItemData item) {
            var player = Player.m_localPlayer;
            if (player == null) {
                return;
            }

            var policy = ShardSocketManager.GetRemovalPolicy(_item, item);
            if (policy != SocketRemoval.Free) {
                ShowSocketMessage(ShardSocketManager.DescribeRemovalPolicy(policy));
                return;
            }

            var playerInventory = player.GetInventory();
            if (!playerInventory.CanAddItem(item)) {
                ShowSocketMessage("$inventory_full");
                return;
            }

            playerInventory.MoveItemToThis(_slots, item);
            invGui.m_moveItemEffects.Create(invGui.transform.position, Quaternion.identity);
        }

        // Every pickup, mouse or gamepad, funnels through OnSelectedItem. Blocking a pickup here also
        // covers dragging a stone out of the window onto the ground, which never reaches
        // InventoryGrid.DropItem.
        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        public static class InventoryGui_OnSelectedItem_Patch {
            [UsedImplicitly]
            private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item,
                InventoryGrid.Modifier mod) {
                // The prefab's input blocker only stops pointer events, so a gamepad press would still
                // reach the grid underneath. Before the panel guard, to cover a prompt over the plain inventory.
                if (InventoryPromptHost.IsOpen) {
                    return false;
                }

                if (!IsOpen || grid == null) {
                    return true;
                }

                // A drop; InventoryGrid.DropItem gates those.
                if (__instance.m_dragGo != null) {
                    return true;
                }

                if (item == null) {
                    return true;
                }

                var slotGrid = IsSlotGrid(grid);

                // Vanilla's Move with no chest open throws the item on the ground, a slotted stone too, so
                // the slot grid always takes it; the player grid takes it only with no chest open.
                if (mod == InventoryGrid.Modifier.Move) {
                    if (slotGrid) {
                        QuickRemoveFromSocket(__instance, item);
                        return false;
                    }

                    if (grid == __instance.m_playerGrid && __instance.m_currentContainer == null) {
                        QuickSocket(__instance, item);
                        return false;
                    }

                    return true;
                }

                if (!slotGrid) {
                    return true;
                }

                var policy = ShardSocketManager.GetRemovalPolicy(_item, item);
                if (policy == SocketRemoval.Free) {
                    return true;
                }

                ShowSocketMessage(ShardSocketManager.DescribeRemovalPolicy(policy));
                return false;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "OnRightClickItem")]
        public static class InventoryGui_OnRightClickItem_Patch {
            [UsedImplicitly]
            private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item) {
                if (grid == null) {
                    return true;
                }

                if (InventoryPromptHost.IsOpen) {
                    return false;
                }

                if (!IsSlotGrid(grid)) {
                    return true;
                }

                switch (ShardSocketManager.GetRemovalPolicy(_item, item)) {
                    case SocketRemoval.BreakOnly:
                        OpenBreakPrompt(item);
                        return false;
                    case SocketRemoval.Locked:
                        ShowSocketMessage("$mod_epicloot_socket_permanent");
                        return false;
                    default:
                        return true;
                }
            }
        }

        // Vanilla's own swap only runs when the whole dragged stack moves; with a stack in hand it instead
        // merges into the occupied cell, failing silently or stacking two same-named shards in one socket.
        private static bool SwapIntoSocket(Inventory source, ItemDrop.ItemData input, Vector2i pos,
            ItemDrop.ItemData occupant) {
            // Emptied first, so the insert below cannot merge onto the occupant's stack.
            _slots.RemoveItem(occupant);

            if (!source.AddItem(occupant)) {
                PlaceInSocketSlot(_slots, occupant, pos);
                ShowSocketMessage("$inventory_full");
                return false;
            }

            _slots.MoveItemToThis(source, input, 1, pos.x, pos.y);
            return true;
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
        public static class InventoryGrid_DropItem_Patch {
            [UsedImplicitly]
            private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item,
                ref int amount, Vector2i pos, ref bool __result) {
                if (!IsOpen) {
                    return true;
                }

                if (IsSlotInventory(__instance.m_inventory)) {
                    var occupant = __instance.m_inventory.GetItemAt(pos.x, pos.y);
                    if (occupant == item) {
                        return true;
                    }

                    // Rearranging within the row: vanilla's path would merge two identical shards into one
                    // cell and destroy one.
                    if (fromInventory == _slots) {
                        var vacated = item.m_gridPos;
                        item.m_gridPos = pos;
                        if (occupant != null) {
                            occupant.m_gridPos = vacated;
                        }
                        _slots.Changed();
                        __result = true;
                        return false;
                    }

                    if (occupant == null) {
                        if (!ShardSocketManager.CanSocket(_item, item, out var reason)) {
                            ShowSocketMessage(reason);
                            __result = false;
                            return false;
                        }

                        // Vanilla's stack split moves one unit and leaves the rest of the dragged stack.
                        amount = 1;
                        return true;
                    }

                    // An exchange needs no free socket, so CanSocket does not apply; the occupant must be
                    // removable and the incoming stone legal beside the surviving sockets.
                    var occupantPolicy = ShardSocketManager.GetRemovalPolicy(_item, occupant);
                    if (occupantPolicy != SocketRemoval.Free) {
                        ShowSocketMessage(ShardSocketManager.DescribeRemovalPolicy(occupantPolicy));
                        __result = false;
                        return false;
                    }

                    var survivors = _slots.GetAllItems().FindAll(i => i != occupant);
                    if (!ShardSocketManager.CanCoexist(_item, item, survivors, out var swapReason)) {
                        ShowSocketMessage(swapReason);
                        __result = false;
                        return false;
                    }

                    __result = SwapIntoSocket(fromInventory, item, pos, occupant);
                    return false;
                }

                // Dragging a slotted stone out onto an occupied cell makes vanilla's swap push the
                // destination item into the slot, bypassing the gate above, so it is validated here.
                if (IsSlotInventory(fromInventory)) {
                    var itemAt = __instance.m_inventory.GetItemAt(pos.x, pos.y);
                    if (itemAt != null && itemAt != item) {
                        var remaining = _slots.GetAllItems().FindAll(i => i != item);
                        if (!ShardSocketManager.CanCoexist(_item, itemAt, remaining, out var reason)) {
                            ShowSocketMessage(reason);
                            __result = false;
                            return false;
                        }

                        // Vanilla's swap moves itemAt's whole stack into the slot, and a socket keeps one, so
                        // the rest would be lost. A same-named item merges back harmlessly instead.
                        if (itemAt.m_stack > 1 && itemAt.m_shared.m_name != item.m_shared.m_name) {
                            ShowSocketMessage("$mod_epicloot_socket_singlestackonly");
                            __result = false;
                            return false;
                        }
                    }
                }

                return true;
            }
        }
    }
}
