using System.Collections.Generic;
using System.Linq;
using EpicLoot.Config;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.ShardStones {
    // Builds and manages the synthetic inventory that represents a MagicItem's sockets.
    public static class SocketsUI {
        public static ItemDrop.ItemData OpenEquipment;
        public static Inventory OpenInventory;
        private static bool _reconciling;

        private static bool IsSocketGridOpen => OpenEquipment != null && OpenInventory != null;

        // Adds `item` to the socket row at `pos` without vanilla's stack merging. Both Inventory.AddItem
        // overloads fold a stackable item into any existing stack of the same name (FindFreeStackItem),
        // and shards stack to 100 with one shared name per colour+rarity -- so two sockets holding
        // identical shards would collapse into a single slot, and SaveSockets would then reconcile them
        // back down to one socket, destroying a shard.
        private static void PlaceInSocketSlot(Inventory inv, ItemDrop.ItemData item, Vector2i pos) {
            item.m_gridPos = pos;
            item.m_stack = 1;
            inv.GetAllItems().Add(item);
            inv.Changed();
        }

        // The leftmost empty socket slot, or (-1, -1) when every socket is filled.
        private static Vector2i FindEmptySocketSlot() {
            for (var x = 0; x < OpenInventory.GetWidth(); x++) {
                if (OpenInventory.GetItemAt(x, 0) == null) {
                    return new Vector2i(x, 0);
                }
            }

            return new Vector2i(-1, -1);
        }

        // One grid column per socket, in socket order. Returns null when a socket's source prefab has
        // gone missing: opening anyway would leave that slot empty, and SaveSockets would then reconcile
        // the socket away on close -- silently destroying it. Better to refuse to open.
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
            if (_reconciling || OpenEquipment == null || OpenInventory == null) {
                return;
            }

            if (!OpenEquipment.IsMagic(out var magicItem)) {
                return;
            }

            _reconciling = true;
            try {
                magicItem.Sockets.Clear();
                // Grid order, not list order: drags and swaps append to the end of the backing list, and
                // the stored order is what tooltips render.
                foreach (var item in OpenInventory.GetAllItems().OrderBy(x => x.m_gridPos.x)) {
                    // effect may be null for an inert shard; it still occupies the socket.
                    if (!ShardSocketManager.ResolveSocketedEffect(OpenEquipment, item, out var effect, out var color, out var rarity)) {
                        continue;
                    }
                    magicItem.Sockets.Add(new SocketedEffect(
                        effect, ShardSocketManager.GetSourcePrefabName(item), rarity) {
                        ShardType = color
                    });
                }
                // Shard values depend on what else shares the item (same-color stacking decay), so they
                // are settled once the whole set is known rather than per item above.
                ShardSocketManager.RecomputeSocketValues(OpenEquipment, magicItem);
                OpenEquipment.SaveMagicItem(magicItem);

                if (Player.m_localPlayer != null) {
                    EquipmentEffectCache.Reset(Player.m_localPlayer);
                }
            } finally {
                _reconciling = false;
            }
        }

        // Builds and shows the socket overlay for the given equipment. The overlay reuses the
        // InventoryGui container panel; it stays open (keyed off OpenEquipment/OpenInventory) until
        // CloseContainer/Hide reconciles and clears it.
        private static void OpenSocketOverlay(InventoryGui invGui, ItemDrop.ItemData item) {
            if (item == null || !item.IsMagic(out var magicItem) || !magicItem.HasSockets()) {
                return;
            }

            // Tear down whatever the container panel currently holds -- a real container, or an earlier
            // socket overlay. Going through CloseContainer matters for the latter: the close prefix
            // reconciles the OLD equipment before the statics move on, and vanilla CloseContainer cancels
            // a drag whose source is that old socket inventory. Without it, a stone picked up from item A
            // and dropped into item B is added to B while staying on A.
            if (invGui.IsContainerOpen() || IsSocketGridOpen) {
                invGui.CloseContainer();
            }

            var inv = BuildSocketInventory(magicItem);
            if (inv == null) {
                ShowSocketMessage("$mod_epicloot_socket_unavailable");
                return;
            }

            inv.m_onChanged += SaveSockets;

            if (invGui.m_takeAllButton != null) {
                invGui.m_takeAllButton.gameObject.SetActive(false);
            }
            if (invGui.m_stackAllButton != null) {
                invGui.m_stackAllButton.gameObject.SetActive(false);
            }

            OpenEquipment = item;
            OpenInventory = inv;
            invGui.m_firstContainerUpdate = true;

            // Gamepad: park the cursor on the first socket and hand it focus, the way opening a real
            // container does. Harmless to skip on mouse, where focus follows the pointer.
            if (ZInput.IsGamepadActive()) {
                invGui.m_containerGrid.SetSelection(new Vector2i(0, 0));
                invGui.SetActiveGroup(0);
            }
        }

        // Claim the "Use" press before vanilla InventoryGui.Update consumes it. Vanilla Update reads
        // GetButtonDown("Use") and, while the inventory is visible, resets the button and Hide()s the
        // inventory -- all before it calls UpdateContainer. So the overlay's open/toggle has to happen
        // in an Update prefix (which runs first) and consume the button, or the press never survives to
        // reach the render path in UpdateContainer.
        //
        // Only the keyboard "Use" binding is read here. The gamepad equivalent lives in
        // InventoryGui_OnRightClickItem_Patch; see the note there for why "JoyUse" cannot be used.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        public static class InventoryGui_Update_Patch {
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

                if (!ZInput.GetButtonDown("Use")) {
                    return;
                }

                var pos = Input.mousePosition;
                var item = __instance.m_playerGrid.GetItem(
                    new Vector2i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)));

                // Over empty space: leave the press for vanilla, so Use still closes the inventory.
                if (item == null) {
                    return;
                }

                if (!item.IsMagic(out var magicItem) || !magicItem.HasSockets()) {
                    // Aiming at an item and hitting one without sockets is a miss, not a request to shut
                    // the whole inventory. Swallow the press: Player doesn't read Use while the inventory
                    // is visible, so nothing else wants it.
                    if (ELConfig.KeepInventoryOpenOverItems.Value) {
                        ZInput.ResetButtonStatus("Use");
                    }
                    return;
                }

                // We own this Use press. Consume it so vanilla's later GetButtonDown("Use") is false and
                // it won't Hide() the inventory out from under the overlay.
                ZInput.ResetButtonStatus("Use");

                // Toggle: pressing Use again on the already-open item closes the overlay.
                if (IsSocketGridOpen && item == OpenEquipment) {
                    __instance.CloseContainer();
                } else {
                    OpenSocketOverlay(__instance, item);
                }
            }
        }

        // Render path only. Opening/toggling is handled by InventoryGui_Update_Patch; here we just draw
        // the open overlay into the container panel and take over UpdateContainer while it is up.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
        public static class InventoryGui_UpdateContainer_Patch {
            [UsedImplicitly]
            private static bool Prefix(InventoryGui __instance) {
                if (!IsSocketGridOpen) {
                    return true;
                }

                __instance.m_containerHoldTime = 0;

                // If the equipment moved out of its slot or was consumed, close the overlay.
                var stillThere = __instance.m_playerGrid.GetInventory()
                    .GetItemAt(OpenEquipment.m_gridPos.x, OpenEquipment.m_gridPos.y);
                if (stillThere != OpenEquipment) {
                    __instance.CloseContainer();
                    return true;
                }

                __instance.m_container.gameObject.SetActive(true);
                __instance.m_containerGrid.UpdateInventory(OpenInventory, null, __instance.m_dragItem);
                __instance.m_containerName.text =
                    Localization.instance.Localize("$mod_epicloot_sockets") + ": " +
                    Localization.instance.Localize(OpenEquipment.m_shared.m_name);

                if (__instance.m_firstContainerUpdate) {
                    __instance.m_containerGrid.ResetView();
                    __instance.m_firstContainerUpdate = false;
                }

                return false;
            }
        }

        // The overlay lives in the container panel but never sets m_currentContainer, so vanilla's
        // IsContainerOpen() reports false -- and that one flag gates every gamepad route into the
        // container grid: UpdateGamepad forces m_activeGroup off group 0 each frame, SetActiveGroup's
        // cycling skips it, and MoveToLowerInventoryGrid refuses to move focus down into it. It also
        // picks which KeyHints set is shown, and the with-container set is the right one here.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.IsContainerOpen))]
        public static class InventoryGui_IsContainerOpen_Patch {
            [UsedImplicitly]
            private static void Postfix(ref bool __result) {
                if (IsSocketGridOpen) {
                    __result = true;
                }
            }
        }

        [HarmonyPatch]
        public static class InventoryGui_Close_Patch {
            [UsedImplicitly]
            private static IEnumerable<System.Reflection.MethodBase> TargetMethods() {
                yield return AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.Hide));
                yield return AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.CloseContainer));
            }

            [UsedImplicitly]
            private static void Prefix(InventoryGui __instance) {
                // An unanswered confirmation is cancelled rather than carried over. Ahead of the socket
                // grid check on purpose: a confirmation raised over the plain inventory (Brokkr's Gift)
                // has no socket grid behind it and must still be dropped when the window closes.
                InventoryPromptHost.Cancel();

                if (!IsSocketGridOpen) {
                    return;
                }

                if (Player.m_localPlayer != null) {
                    SaveSockets();
                }

                if (__instance.m_takeAllButton != null) {
                    __instance.m_takeAllButton.gameObject.SetActive(true);
                }
                if (__instance.m_stackAllButton != null) {
                    __instance.m_stackAllButton.gameObject.SetActive(true);
                }

                // The inventory is discarded here, but a drag started from it can still outlive this
                // call. Unsubscribe so a late change can never reconcile against whatever equipment
                // happens to be open by then.
                OpenInventory.m_onChanged -= SaveSockets;
                OpenEquipment = null;
                OpenInventory = null;
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
            if (item == null || OpenInventory == null || !OpenInventory.ContainsItem(item)) {
                return;
            }

            OpenInventory.RemoveItem(item);
        }

        // Ctrl+click (gamepad LT + X) a socketable in the player inventory: put one unit of it into the
        // first free socket.
        private static void QuickSocket(InventoryGui invGui, ItemDrop.ItemData item) {
            var player = Player.m_localPlayer;
            if (player == null) {
                return;
            }

            if (!ShardSocketManager.CanSocket(OpenEquipment, item, out var reason)) {
                ShowSocketMessage(reason);
                return;
            }

            var slot = FindEmptySocketSlot();
            if (slot.x < 0) {
                ShowSocketMessage("$mod_epicloot_socket_nofreeslot");
                return;
            }

            OpenInventory.MoveItemToThis(player.GetInventory(), item, 1, slot.x, slot.y);
            invGui.m_moveItemEffects.Create(invGui.transform.position, Quaternion.identity);
        }

        // Ctrl+click (gamepad LT + X) a socketed stone: hand it back to the player's inventory.
        private static void QuickRemoveFromSocket(InventoryGui invGui, ItemDrop.ItemData item) {
            var player = Player.m_localPlayer;
            if (player == null) {
                return;
            }

            var policy = ShardSocketManager.GetRemovalPolicy(OpenEquipment, item);
            if (policy != SocketRemoval.Free) {
                ShowSocketMessage(ShardSocketManager.DescribeRemovalPolicy(policy));
                return;
            }

            var playerInventory = player.GetInventory();
            if (!playerInventory.CanAddItem(item)) {
                ShowSocketMessage("$inventory_full");
                return;
            }

            // Taking it out of the socket inventory fires m_onChanged; SaveSockets drops the socket.
            playerInventory.MoveItemToThis(OpenInventory, item);
            invGui.m_moveItemEffects.Create(invGui.transform.position, Quaternion.identity);
        }

        // Every player-initiated pickup funnels through OnSelectedItem -- mouse and gamepad both route
        // here, and it dispatches drag-start, Split, Move and Drop. Two jobs: nothing may be picked up
        // out of a socket the current config doesn't let the player empty (blocking here also covers
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
                // Checked before the socket-grid guard, so it also covers a confirmation raised over the
                // plain inventory.
                if (InventoryPromptHost.IsOpen) {
                    return false;
                }

                if (!IsSocketGridOpen || grid == null) {
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

                var socketGrid = grid.GetInventory() == OpenInventory;

                // Quick-transfer. Vanilla's Move branch keys off m_currentContainer, which the overlay
                // never sets, so left to itself it falls through to Player.DropItem and throws the stone
                // on the ground -- for socketed stones and bag items alike.
                if (mod == InventoryGrid.Modifier.Move) {
                    if (socketGrid) {
                        QuickRemoveFromSocket(__instance, item);
                        return false;
                    }

                    if (grid == __instance.m_playerGrid) {
                        QuickSocket(__instance, item);
                        return false;
                    }

                    return true;
                }

                if (!socketGrid) {
                    return true;
                }

                var policy = ShardSocketManager.GetRemovalPolicy(OpenEquipment, item);
                if (policy == SocketRemoval.Free) {
                    return true;
                }

                ShowSocketMessage(ShardSocketManager.DescribeRemovalPolicy(policy));
                return false;
            }
        }

        // Right-click a socketed stone that can only be destroyed to open the break confirmation.
        // Vanilla's handler just calls Player.UseItem, a no-op for shard/runestone materials.
        //
        // This is also where the gamepad opens the overlay, with RT + X. "JoyUse" cannot be used for
        // that: it maps to FaceButtonA on the classic pad layout and to FaceButtonX on Alt1/Alt2, both
        // of which InventoryGrid already claims, and ZInput.ResetButtonStatus clears only the named
        // ButtonDef rather than the shared physical control -- so one press would open the overlay AND
        // pick the item up. InventoryGrid only ever tests LT alongside X, which leaves RT + X free; it
        // lands here, and both buttons are bound identically on all three layouts
        // (ZInput.ResetGamepadButtonsGeneric).
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

                if (grid == __instance.m_playerGrid && ZInput.GetButton("JoyRTrigger") &&
                    item != null && item.IsMagic(out var magicItem) && magicItem.HasSockets()) {
                    if (IsSocketGridOpen && item == OpenEquipment) {
                        __instance.CloseContainer();
                    } else {
                        OpenSocketOverlay(__instance, item);
                    }
                    return false;
                }

                if (!IsSocketGridOpen || grid.GetInventory() != OpenInventory) {
                    return true;
                }

                switch (ShardSocketManager.GetRemovalPolicy(OpenEquipment, item)) {
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

        // Puts one unit of `input` into socket `pos`, handing the stone that was there back to `source`.
        // Vanilla's own swap only runs when the whole dragged stack moves, and a socket takes exactly one
        // shard -- so with a stack in hand vanilla instead tries to merge into an occupied slot and either
        // fails silently or, for a same-named shard, stacks two stones into a single socket.
        private static bool SwapIntoSocket(Inventory source, ItemDrop.ItemData input, Vector2i pos,
            ItemDrop.ItemData occupant) {
            // Empty the slot first, so the insert below cannot merge onto the occupant's stack.
            OpenInventory.RemoveItem(occupant);

            if (!source.AddItem(occupant)) {
                // Nowhere to put the displaced stone: undo and leave the socket as it was.
                PlaceInSocketSlot(OpenInventory, occupant, pos);
                ShowSocketMessage("$inventory_full");
                return false;
            }

            OpenInventory.MoveItemToThis(source, input, 1, pos.x, pos.y);
            return true;
        }

        // Only allow legal Runestones/Shards to be dropped into a socket slot, and never more than one
        // per slot. `amount` is `ref` so we can clamp it to 1 and let vanilla's stack-split logic move a
        // single unit into the slot, leaving the remainder of the dragged stack in the source inventory.
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.DropItem))]
        public static class InventoryGrid_DropItem_Patch {
            [UsedImplicitly]
            private static bool Prefix(InventoryGrid __instance, Inventory fromInventory, ItemDrop.ItemData item,
                ref int amount, Vector2i pos, ref bool __result) {
                if (OpenInventory == null) {
                    return true;
                }

                // Case 1: dropping an item INTO the socket grid. Only legal socketables, one per slot.
                if (__instance.m_inventory == OpenInventory) {
                    var occupant = __instance.m_inventory.GetItemAt(pos.x, pos.y);
                    if (occupant == item) {
                        return true; // dropped back onto itself; vanilla no-ops this
                    }

                    // 1a. Rearranging within the socket row. Nothing enters or leaves the item, so there
                    // is nothing to validate -- and vanilla's path would merge two identical shards into
                    // one slot and destroy one. Move them by grid position instead.
                    if (fromInventory == OpenInventory) {
                        var vacated = item.m_gridPos;
                        item.m_gridPos = pos;
                        if (occupant != null) {
                            occupant.m_gridPos = vacated;
                        }
                        OpenInventory.Changed();
                        __result = true;
                        return false;
                    }

                    // 1b. Filling an empty socket.
                    if (occupant == null) {
                        if (!ShardSocketManager.CanSocket(OpenEquipment, item, out var reason)) {
                            ShowSocketMessage(reason);
                            __result = false;
                            return false;
                        }

                        // A socket holds exactly one shard: move a single unit regardless of the dragged
                        // stack size.
                        amount = 1;
                        return true;
                    }

                    // 1c. Exchanging for the stone already in that socket. The occupant leaves as the new
                    // one arrives, so this needs no free socket -- running CanSocket here is what made a
                    // swap report "No open sockets" -- but the occupant does have to be one the player is
                    // allowed to take out, and the incoming stone still has to be legal alongside the
                    // sockets that survive the exchange.
                    var occupantPolicy = ShardSocketManager.GetRemovalPolicy(OpenEquipment, occupant);
                    if (occupantPolicy != SocketRemoval.Free) {
                        ShowSocketMessage(ShardSocketManager.DescribeRemovalPolicy(occupantPolicy));
                        __result = false;
                        return false;
                    }

                    var survivors = OpenInventory.GetAllItems().FindAll(i => i != occupant);
                    if (!ShardSocketManager.CanCoexist(OpenEquipment, item, survivors, out var swapReason)) {
                        ShowSocketMessage(swapReason);
                        __result = false;
                        return false;
                    }

                    __result = SwapIntoSocket(fromInventory, item, pos, occupant);
                    return false;
                }

                // Case 2: dragging a socketed item OUT onto an occupied slot triggers vanilla's swap, which
                // pushes the destination item back INTO the socket (via fromInventory.MoveItemToThis). The
                // pushed item bypasses Case 1's CanSocket gate, so validate it here against the same rules --
                // otherwise a same-effect shard could be smuggled in past the duplicate check by swapping it
                // for an unrelated socketed shard. Measure duplicates against the sockets that remain after
                // the dragged item leaves.
                if (fromInventory == OpenInventory) {
                    var itemAt = __instance.m_inventory.GetItemAt(pos.x, pos.y);
                    if (itemAt != null && itemAt != item) {
                        var remaining = OpenInventory.GetAllItems().FindAll(i => i != item);
                        if (!ShardSocketManager.CanCoexist(OpenEquipment, itemAt, remaining, out var reason)) {
                            ShowSocketMessage(reason);
                            __result = false;
                            return false;
                        }

                        // Vanilla's swap moves itemAt's ENTIRE stack into the single socket slot, but a socket
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
