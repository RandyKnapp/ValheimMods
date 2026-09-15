using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // Controller navigation on the player grid. Vanilla InventoryGrid.UpdateGamepad walks raw grid
    // coordinates, but the slot cells sit in hidden rows and are drawn wherever the panel puts them
    // (config position, the extra utility column, API and quiver rows, the Auga diamond). Walked in
    // grid order, Right off the inventory went nowhere, the paperdoll was stepped through in index
    // order, and the container was out of reach: vanilla only jumps into it from the grid's last
    // row, which is reserved capacity that is normally empty.
    //
    // So a direction press inside the visible grid keeps vanilla's one-cell step, and every other
    // move picks the nearest on-screen cell in that direction, measured from the cells' real
    // transforms — whatever the layout, the selection goes where it looks like it should. Down with
    // nothing below jumps into the container. A / X and the trigger modifiers stay vanilla.
    internal static class GamepadNavigation {
        // Vanilla only jumps into the container once the stick was released at the edge, so a held
        // stick repeating down the grid stops at the bottom first. Tracked here rather than in
        // vanilla's jumpToNextContainer, which vanilla rewrites on every frame it runs using the
        // full grid height, hidden rows included.
        private static bool _downJumpArmed;

        [HarmonyPatch(typeof(InventoryGrid), "UpdateGamepad")]
        private static class InventoryGrid_UpdateGamepad_SpatialNavigation {
            private static bool Prefix(InventoryGrid __instance) {
                if (!InventoryGui.instance || __instance != InventoryGui.instance.m_playerGrid || Player.m_localPlayer == null)
                    return true;

                // Vanilla's own conditions for reading the controller at all
                if (!__instance.m_uiGroup.IsActive || Console.IsVisible() || ZInput.IsTouchActive() || !ZInput.IsExclusiveGamepadActive())
                    return true;

                ResolveStaleSelection(__instance);

                if (!ZInput.GetButton("JoyDPadDown") && !ZInput.GetButton("JoyLStickDown"))
                    _downJumpArmed = true;

                bool left = ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyLStickLeft");
                bool right = ZInput.GetButtonDown("JoyDPadRight") || ZInput.GetButtonDown("JoyLStickRight");
                bool up = ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyLStickUp");
                bool down = ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyLStickDown");

                if (!left && !right && !up && !down)
                    return true;

                // Horizontal first and vertical second, like vanilla, so a diagonal still moves on
                // both axes and a container jump is the last thing that happens this frame.
                if (left)
                    Navigate(__instance, Vector2.left);
                else if (right)
                    Navigate(__instance, Vector2.right);

                if (up)
                    Navigate(__instance, Vector2.up);
                else if (down)
                    Navigate(__instance, Vector2.down);

                // Skipping vanilla drops an A / X press landing on the very same frame as a move.
                return false;
            }
        }

        // Leaving a container upward: vanilla keeps the player grid's previous row, which may be a
        // slot row. The container sits below the inventory, so land on the bottom inventory row.
        [HarmonyPatch(typeof(InventoryGui), "MoveToUpperInventoryGrid")]
        private static class InventoryGui_MoveToUpperInventoryGrid_LandOnInventory {
            private static void Prefix(InventoryGui __instance, out int __state) {
                __state = __instance.ActiveGroup;
            }

            private static void Postfix(InventoryGui __instance, int __state) {
                // Vanilla bails without switching groups when the inventory group isn't active
                if (__instance.ActiveGroup == __state || !__instance.m_playerGrid)
                    return;

                Vector2i selection = __instance.m_playerGrid.SelectionGridPosition;
                if (selection.y < VisibleRows)
                    return;

                __instance.m_playerGrid.SetGamepadSelection(new Vector2i(Mathf.Clamp(selection.x, 0, InventoryWidth - 1), VisibleRows - 1));
            }
        }

        private static void Navigate(InventoryGrid grid, Vector2 direction) {
            Vector2i from = grid.m_selected;
            bool fromSlot = IsGridPositionASlot(from);

            if ((!fromSlot && TryGridStep(from, direction, out Vector2i target))
                || TryFindNeighbour(grid, from, direction, includeInventoryCells: fromSlot, out target)) {
                Select(grid, target);
                if (direction.y < 0f)
                    _downJumpArmed = false;
                return;
            }

            // Nothing below: on to the container, if one is open (vanilla checks that). Its column
            // mapping expects a player grid column, so a slot cell passes the column under it.
            if (direction.y < 0f && _downJumpArmed)
                grid.OnMoveToLowerInventoryGrid?.Invoke(new Vector2i(fromSlot ? NearestInventoryColumn(grid, from) : from.x, VisibleRows - 1));
        }

        // Vanilla's one-cell step, as long as it stays inside the visible grid
        private static bool TryGridStep(Vector2i from, Vector2 direction, out Vector2i target) {
            target = new Vector2i(from.x + Mathf.RoundToInt(direction.x), from.y - Mathf.RoundToInt(direction.y));
            return target.x >= 0 && target.x < InventoryWidth && target.y >= 0 && target.y < VisibleRows;
        }

        // The cell whose on-screen center lies nearest in the pressed direction, within a 45° cone
        // either side of it. Scored as cos(angle) / distance, the heuristic Unity's own Selectable
        // navigation uses: a close cell slightly off-axis beats a distant one dead ahead. Slot cells
        // are always candidates; visible inventory cells only when leaving a slot cell, since
        // inside the grid the plain step already covers them.
        private static bool TryFindNeighbour(InventoryGrid grid, Vector2i from, Vector2 direction, bool includeInventoryCells, out Vector2i target) {
            target = from;

            Camera camera = GetCanvasCamera(grid);
            if (!TryGetScreenCenter(grid, from, camera, out Vector2 origin))
                return false;

            Vector2i best = from;
            float bestScore = 0f;

            void Consider(Vector2i candidate) {
                if (candidate == from || !TryGetScreenCenter(grid, candidate, camera, out Vector2 center))
                    return;

                Vector2 offset = center - origin;
                float along = Vector2.Dot(offset, direction);
                float across = Mathf.Abs(offset.x * direction.y - offset.y * direction.x);
                if (along <= 1f || across > along)
                    return;

                float score = along / offset.sqrMagnitude;
                if (score > bestScore) {
                    bestScore = score;
                    best = candidate;
                }
            }

            foreach (Slot slot in slots)
                if (slot.IsActive)
                    Consider(slot.GridPosition);

            if (includeInventoryCells)
                for (int y = 0; y < VisibleRows; y++)
                    for (int x = 0; x < InventoryWidth; x++)
                        Consider(new Vector2i(x, y));

            target = best;
            return bestScore > 0f;
        }

        // The bottom-row inventory column horizontally closest to a slot cell
        private static int NearestInventoryColumn(InventoryGrid grid, Vector2i slotPosition) {
            Camera camera = GetCanvasCamera(grid);
            if (!TryGetScreenCenter(grid, slotPosition, camera, out Vector2 origin))
                return InventoryWidth - 1;

            int bestColumn = InventoryWidth - 1;
            float bestDistance = float.MaxValue;
            for (int x = 0; x < InventoryWidth; x++) {
                if (!TryGetScreenCenter(grid, new Vector2i(x, VisibleRows - 1), camera, out Vector2 center))
                    continue;

                float distance = Mathf.Abs(center.x - origin.x);
                if (distance < bestDistance) {
                    bestDistance = distance;
                    bestColumn = x;
                }
            }

            return bestColumn;
        }

        // A selection resting on a cell that is no longer shown (a slot switched off by config, or
        // reserved capacity) is steered onto the nearest active slot, and the element is selected
        // so the controller tooltip follows it.
        private static void ResolveStaleSelection(InventoryGrid grid) {
            Vector2i selection = grid.m_selected;
            if (!IsGridPositionASlot(selection))
                return;

            int slotIndex = (selection.y - VisibleRows) * InventoryWidth + selection.x;
            if (slotIndex >= 0 && slotIndex < slots.Length && slots[slotIndex].IsActive)
                return;

            int best = -1;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < slots.Length; i++) {
                if (!slots[i].IsActive)
                    continue;

                int distance = Mathf.Abs(i - slotIndex);
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = i;
                }
            }

            Select(grid, best >= 0 ? slots[best].GridPosition : new Vector2i(Mathf.Min(selection.x, InventoryWidth - 1), VisibleRows - 1));
        }

        // What vanilla does after a move: select the element (the controller tooltip only shows on
        // the EventSystem's selected object) and scroll a visible cell into view. A relocated slot
        // cell is not part of the grid's scroll content, so it never scrolls the grid.
        private static void Select(InventoryGrid grid, Vector2i position) {
            grid.m_selected = position;

            RectTransform rect = GetElementRect(grid, position);
            if (!rect)
                return;

            Selectable selectable = rect.GetComponent<Selectable>();
            if (selectable)
                selectable.Select();

            if (!IsGridPositionASlot(position) && grid.m_ensureVisible)
                grid.m_ensureVisible.CenterOnItem(rect);
        }

        private static RectTransform GetElementRect(InventoryGrid grid, Vector2i position) {
            if (position.x < 0 || position.x >= InventoryWidth || position.y < 0)
                return null;

            int index = position.y * InventoryWidth + position.x;
            if (index >= grid.m_elements.Count)
                return null;

            InventoryElement element = grid.m_elements[index];
            return element ? element.transform as RectTransform : null;
        }

        private static Camera GetCanvasCamera(InventoryGrid grid) {
            Canvas canvas = grid.GetComponentInParent<Canvas>();
            if (!canvas)
                return null;

            Canvas root = canvas.rootCanvas;
            return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        }

        // Only local-to-world conversions: they stay valid under the zero z-scale BetterUI writes
        // onto the Player panel, where world-to-local would divide by it (see BetterUICompat).
        // Hidden cells are parked under an inactive holder and are never candidates.
        private static bool TryGetScreenCenter(InventoryGrid grid, Vector2i position, Camera camera, out Vector2 center) {
            center = Vector2.zero;

            RectTransform rect = GetElementRect(grid, position);
            if (!rect || !rect.gameObject.activeInHierarchy)
                return false;

            center = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            return true;
        }
    }
}
