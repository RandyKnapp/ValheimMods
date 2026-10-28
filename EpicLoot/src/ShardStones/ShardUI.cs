using System.Collections.Generic;
using System.Linq;
using EpicLoot.Compatibility;
using EpicLoot.Config;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.ShardStones {
    // Opens an item's shard slots in Epic Loot's own slot panel (ShardSlotPanel) and keeps the item's sockets
    // in step with what the player drags in and out of it.
    //
    // The cells are a synthetic one-row Inventory, one cell per socket, built from MagicItem.Sockets when the
    // panel opens and reconciled back into it on every change and on close. The panel is a grid of its own
    // beside the inventory, so a chest stays open alongside it and vanilla's chest panel is never touched; what
    // is patched are the grid-level rules every grid shares -- what may be picked up, dropped, swapped or
    // quick-moved -- on InventoryGui's and InventoryGrid's own entry points, and only for the slot grid.
    public static class ShardUI {
        // The item whose slots are open, and the synthetic inventory standing in for its sockets.
        private static ItemDrop.ItemData _item;
        private static Inventory _slots;
        private static ShardSlotPanel _panel;
        private static bool _reconciling;

        public static bool IsOpen => _item != null && _slots != null && _panel != null && _panel.IsAlive;

        public static bool IsOpenFor(ItemDrop.ItemData item) => IsOpen && item != null && _item == item;

        private static bool IsSlotInventory(Inventory inventory) => IsOpen && inventory != null && inventory == _slots;

        private static bool IsSlotGrid(InventoryGrid grid) => IsOpen && grid != null && grid == _panel.Grid;

        // Adds `item` to the slot row at `pos` without vanilla's stack merging. Both Inventory.AddItem
        // overloads fold a stackable item into any existing stack of the same name (FindFreeStackItem),
        // and shards stack to 100 with one shared name per colour+rarity -- so two slots holding
        // identical shards would collapse into a single cell, and SaveSockets would then reconcile them
        // back down to one socket, destroying a shard.
        private static void PlaceInSocketSlot(Inventory inv, ItemDrop.ItemData item, Vector2i pos) {
            item.m_gridPos = pos;
            item.m_stack = 1;
            inv.GetAllItems().Add(item);
            inv.Changed();
        }

        // The leftmost empty slot, or (-1, -1) when every socket is filled.
        private static Vector2i FindEmptySocketSlot() {
            for (var x = 0; x < _slots.GetWidth(); x++) {
                if (_slots.GetItemAt(x, 0) == null) {
                    return new Vector2i(x, 0);
                }
            }

            return new Vector2i(-1, -1);
        }

        // One cell per socket, in socket order. Returns null when a socket's source prefab has gone
        // missing: opening anyway would leave that cell empty, and SaveSockets would then reconcile the
        // socket away on close -- silently destroying it. Better to refuse to open.
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

        // Reconcile MagicItem.Sockets to mirror the synthetic inventory's current contents.
        private static void SaveSockets() {
            if (_reconciling || !IsOpen) {
                return;
            }

            if (!_item.IsMagic(out var magicItem)) {
                return;
            }

            _reconciling = true;
            try {
                magicItem.Sockets.Clear();
                // Grid order, not list order: drags and swaps append to the end of the backing list, and
                // the stored order is what tooltips render.
                foreach (var item in _slots.GetAllItems().OrderBy(x => x.m_gridPos.x)) {
                    // effect may be null for an inert shard; it still occupies the socket.
                    if (!ShardSocketManager.ResolveSocketedEffect(_item, item, out var effect, out var color, out var rarity)) {
                        continue;
                    }
                    magicItem.Sockets.Add(new SocketedEffect(
                        effect, ShardSocketManager.GetSourcePrefabName(item), rarity) {
                        ShardType = color
                    });
                }
                // Shard values depend on what else shares the item (same-color stacking decay), so they
                // are settled once the whole set is known rather than per item above.
                ShardSocketManager.RecomputeSocketValues(_item, magicItem);
                _item.SaveMagicItem(magicItem);

                if (Player.m_localPlayer != null) {
                    EquipmentEffectCache.Reset(Player.m_localPlayer);
                }
            } finally {
                _reconciling = false;
            }
        }

        // Opens the slots of `item` in the panel. It stays open, keyed off _item/_slots, until Close
        // reconciles and clears it.
        private static void Open(InventoryGui invGui, ItemDrop.ItemData item) {
            if (item == null || !item.IsMagic(out var magicItem) || !magicItem.HasSockets()) {
                return;
            }

            // Switching items settles the one that was open first. Close also cancels a drag that started
            // in its slots, so a stone picked up from item A cannot be dropped into item B while still
            // counted on A.
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

            // "Loptr Cowl - Shard Slots": the item first, so the panel reads as that item's.
            panel.Show(
                Localization.instance.Localize(item.m_shared.m_name) + " - " +
                Localization.instance.Localize("$mod_epicloot_sockets"),
                magicItem.SocketCount);
            panel.Grid.UpdateInventory(inv, null, invGui.m_dragItem);

            // Gamepad: park the cursor on the first slot and hand it focus, the way opening a chest does.
            // Harmless to skip on mouse, where focus follows the pointer.
            if (ZInput.IsGamepadActive()) {
                panel.Grid.SetGamepadSelection(new Vector2i(0, 0));
                panel.Focus();
            }
        }

        // Reconciles the open item and hides the panel. Safe to call when nothing is open.
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

        // Pressing the gesture again on the item whose slots are open closes them.
        private static void Toggle(InventoryGui invGui, ItemDrop.ItemData item) {
            if (IsOpenFor(item)) {
                Close(invGui);
            } else {
                Open(invGui, item);
            }
        }

        // The opener: the Socket Overlay Shortcut over the item under the cursor, or the gamepad gesture over the
        // selected one (SocketOverlayInput decides which press is which). It has to be an Update prefix that
        // consumes the press: vanilla Update reads GetButtonDown("Use") and, while the inventory is visible,
        // resets the button and Hide()s the inventory, and the player grid reads the gamepad buttons from
        // inside Update too.
        //
        // The postfix is the panel's own frame: it closes the panel when the item is no longer where it was
        // opened from, and draws the slot grid once vanilla has drawn its own grids.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        [HarmonyPriority(Priority.First)]
        public static class InventoryGui_Update_Patch {
            // Priority.First: this is a void prefix that only reads input and, at most, consumes it, so
            // running ahead of everyone is harmless -- and a container mod's own Update handler must not open
            // its container out from under a press aimed at the shard slots.
            [UsedImplicitly]
            private static void Prefix(InventoryGui __instance) {
                if (!InventoryGui.IsVisible()) {
                    return;
                }

                // A confirmation owns the interaction until it is answered.
                if (InventoryPromptHost.IsOpen) {
                    InventoryPromptHost.Update();
                    return;
                }

                // On a controller the panel's grid takes focus when it opens, and the open gesture is only read
                // over the player grid. Pressed again while the panel's grid has focus it closes the panel,
                // instead of reaching that grid as a plain X press (a right-click on the selected stone).
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

                // If the item moved out of its cell or was consumed, close the panel.
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
                // Over empty space: leave the press for vanilla, so Use still closes the inventory.
                if (item == null) {
                    return;
                }

                // A shortcut that is only the Use key is another mod's container gesture over that mod's item
                // (a backpack, a quiver): hand the press over untouched.
                if (SocketOverlayInput.IsBlockedFor(item, gamepad: false)) {
                    EpicLoot.Log($"[ShardUI] Yielding the Use press over {item.m_shared.m_name}: another mod " +
                        "owns its container, and the Socket Overlay Shortcut is only the Use key.");
                    return;
                }

                bool onUse = SocketOverlayInput.MainKeyIsUse();
                if (!SocketOverlayInput.HasSockets(item)) {
                    // Aiming the shortcut at an item and hitting one without sockets is a miss, not a request
                    // to shut the whole inventory. An item another mod made a container keeps its press.
                    if (onUse && ELConfig.KeepInventoryOpenOverItems.Value && !ForeignItemContainers.HasContainer(item)) {
                        ZInput.ResetButtonStatus("Use");
                    }
                    return;
                }

                // A shortcut on the Use key: consume Use so vanilla's later GetButtonDown("Use") is false and it
                // won't Hide() the inventory out from under the panel, and a container mod's own handler (which
                // reads Use later, in UpdateContainer) sees no press.
                if (onUse) {
                    ZInput.ResetButtonStatus("Use");
                }

                Toggle(invGui, item);
            }

            // Only over an item with shard slots: anywhere else the button keeps doing what vanilla does with it.
            // Consuming it by name is what stops the player grid from also using or picking up the item.
            private static void OnGamepadGesture(InventoryGui invGui, ItemDrop.ItemData item) {
                if (!SocketOverlayInput.HasSockets(item)) {
                    return;
                }

                ZInput.ResetButtonStatus(SocketOverlayInput.GamepadButtonName);
                Toggle(invGui, item);
            }

            // Plain Use over an item. Player doesn't read Use while the inventory is visible, so swallowing it
            // only stops the inventory closing. Another mod's container keeps it: plain Use is the gesture that
            // container advertises ("Press [Use] to open").
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
        // the player's inventory -- a stone picked up out of a slot would be put straight back. While the
        // panel is open and that branch would run (no chest, or a chest whose ownership has not arrived
        // yet), this does what it does (keep the chest panel hidden, drop a drag left over from a chest)
        // and leaves a drag from the slots alone. With an owned chest open vanilla runs untouched: its
        // chest branch cancels nothing.
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

        // The dragged item's ghost follows the selected cell on a controller, and vanilla only looks for one
        // in its own two grids. While the panel's grid holds the selection, the ghost follows it there.
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
                // An unanswered confirmation is cancelled rather than carried over. Ahead of the panel check
                // on purpose: a confirmation raised over the plain inventory (Brokkr's Gift) has no panel
                // behind it and must still be dropped when the window closes.
                InventoryPromptHost.Cancel();

                if (IsOpen) {
                    Close(__instance);
                }
            }
        }

        private static void ShowSocketMessage(string reason) {
            if (Player.m_localPlayer != null && !string.IsNullOrEmpty(reason)) {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize(reason));
            }
        }

        // Asks the player to confirm destroying a socketed stone. Nothing is mutated until they accept.
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

        // Destroys a socketed stone in place. Taking it out of the synthetic inventory fires
        // m_onChanged, and SaveSockets reconciles the socket away; nothing is returned to the player,
        // which is why this does not go through ShardSocketManager.RemoveShard.
        private static void BreakSocketedItem(ItemDrop.ItemData item) {
            if (item == null || !IsOpen || !_slots.ContainsItem(item)) {
                return;
            }

            _slots.RemoveItem(item);
        }

        // Ctrl+click (gamepad LT + X) a socketable in the player inventory: put one unit of it into the
        // first free slot.
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

        // Ctrl+click (gamepad LT + X) a socketed stone: hand it back to the player's inventory.
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

            // Taking it out of the slot inventory fires m_onChanged; SaveSockets drops the socket.
            playerInventory.MoveItemToThis(_slots, item);
            invGui.m_moveItemEffects.Create(invGui.transform.position, Quaternion.identity);
        }

        // Every player-initiated pickup funnels through OnSelectedItem -- mouse and gamepad both route
        // here, and it dispatches drag-start, Split, Move and Drop. Two jobs: nothing may be picked up
        // out of a slot the current config doesn't let the player empty (blocking here also covers
        // dragging a stone out of the window onto the ground, which goes through InventoryGui's own
        // m_dragItem handling and never InventoryGrid.DropItem), and quick-transfer, which vanilla
        // cannot do for us.
        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        public static class InventoryGui_OnSelectedItem_Patch {
            [UsedImplicitly]
            private static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item,
                InventoryGrid.Modifier mod) {
                // While a confirmation is up it owns the whole window: the prefab's input blocker only
                // stops pointer events, so a gamepad A press would still reach the grid underneath.
                // Checked before the panel guard, so it also covers a confirmation raised over the
                // plain inventory.
                if (InventoryPromptHost.IsOpen) {
                    return false;
                }

                if (!IsOpen || grid == null) {
                    return true;
                }

                // A drag in progress means we're dropping INTO a grid, which InventoryGrid.DropItem
                // already gates. Only pickups are our business here.
                if (__instance.m_dragGo != null) {
                    return true;
                }

                if (item == null) {
                    return true;
                }

                var slotGrid = IsSlotGrid(grid);

                // Quick-transfer. Vanilla's Move branch moves between the inventory and an open chest, and
                // with no chest throws the item on the ground -- for a slotted stone too, so the slot grid
                // always takes it. In the player grid the press keeps vanilla's meaning while a chest is
                // open (the item goes to the chest); with no chest it slots the item instead.
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

        // Right-click a socketed stone that can only be destroyed to open the break confirmation.
        // Vanilla's handler just calls Player.UseItem, a no-op for shard/runestone materials. (The gamepad
        // opens the panel from InventoryGui_Update_Patch, which consumes its button before the grid reads
        // it, so the open gesture never reaches here.)
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

        // Puts one unit of `input` into slot `pos`, handing the stone that was there back to `source`.
        // Vanilla's own swap only runs when the whole dragged stack moves, and a socket takes exactly one
        // shard -- so with a stack in hand vanilla instead tries to merge into an occupied cell and either
        // fails silently or, for a same-named shard, stacks two stones into a single socket.
        private static bool SwapIntoSocket(Inventory source, ItemDrop.ItemData input, Vector2i pos,
            ItemDrop.ItemData occupant) {
            // Empty the cell first, so the insert below cannot merge onto the occupant's stack.
            _slots.RemoveItem(occupant);

            if (!source.AddItem(occupant)) {
                // Nowhere to put the displaced stone: undo and leave the socket as it was.
                PlaceInSocketSlot(_slots, occupant, pos);
                ShowSocketMessage("$inventory_full");
                return false;
            }

            _slots.MoveItemToThis(source, input, 1, pos.x, pos.y);
            return true;
        }

        // Only allow legal Runestones/Shards to be dropped into a slot, and never more than one per
        // slot. `amount` is `ref` so we can clamp it to 1 and let vanilla's stack-split logic move a
        // single unit into the cell, leaving the remainder of the dragged stack in the source inventory.
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
        public static class InventoryGrid_DropItem_Patch {
            [UsedImplicitly]
            private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item,
                ref int amount, Vector2i pos, ref bool __result) {
                if (!IsOpen) {
                    return true;
                }

                // Case 1: dropping an item INTO the slot grid. Only legal socketables, one per cell.
                if (IsSlotInventory(__instance.m_inventory)) {
                    var occupant = __instance.m_inventory.GetItemAt(pos.x, pos.y);
                    if (occupant == item) {
                        return true; // dropped back onto itself; vanilla no-ops this
                    }

                    // 1a. Rearranging within the slot row. Nothing enters or leaves the item, so there
                    // is nothing to validate -- and vanilla's path would merge two identical shards into
                    // one cell and destroy one. Move them by grid position instead.
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

                    // 1b. Filling an empty slot.
                    if (occupant == null) {
                        if (!ShardSocketManager.CanSocket(_item, item, out var reason)) {
                            ShowSocketMessage(reason);
                            __result = false;
                            return false;
                        }

                        // A socket holds exactly one shard: move a single unit regardless of the dragged
                        // stack size.
                        amount = 1;
                        return true;
                    }

                    // 1c. Exchanging for the stone already in that slot. The occupant leaves as the new
                    // one arrives, so this needs no free socket -- running CanSocket here is what made a
                    // swap report "No open sockets" -- but the occupant does have to be one the player is
                    // allowed to take out, and the incoming stone still has to be legal alongside the
                    // sockets that survive the exchange.
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

                // Case 2: dragging a slotted stone OUT onto an occupied cell (in the inventory or a chest)
                // triggers vanilla's swap, which pushes the destination item back INTO the slot (via
                // fromInventory.MoveItemToThis). The pushed item bypasses Case 1's CanSocket gate, so validate
                // it here against the same rules -- otherwise a same-effect shard could be smuggled in past
                // the duplicate check by swapping it for an unrelated slotted shard. Measure duplicates
                // against the sockets that remain after the dragged item leaves.
                if (IsSlotInventory(fromInventory)) {
                    var itemAt = __instance.m_inventory.GetItemAt(pos.x, pos.y);
                    if (itemAt != null && itemAt != item) {
                        var remaining = _slots.GetAllItems().FindAll(i => i != item);
                        if (!ShardSocketManager.CanCoexist(_item, itemAt, remaining, out var reason)) {
                            ShowSocketMessage(reason);
                            __result = false;
                            return false;
                        }

                        // Vanilla's swap moves itemAt's ENTIRE stack into the single slot, but a socket
                        // holds one shard and reconstruction hands back a stack of 1 -- the remainder would be
                        // lost. Only a different-named item takes that swap path (a same-named one merges back
                        // onto the stack harmlessly), so reject the lossy case and ask the player to split first.
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
