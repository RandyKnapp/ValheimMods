using System;
using System.Collections.Generic;
using EpicLoot.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.ShardStones {
    // Epic Loot's shard slot panel: a small window of its own beside the inventory that holds one inventory
    // grid, one cell per socket of the item whose slots are open. It is built once per InventoryGui from the
    // pieces vanilla already draws the player panel with (its background, its controller selection frame, the
    // chest title's text style) and the same cell prefab as the player grid, so a cell here is an ordinary
    // inventory cell: vanilla's drag and drop, quick-move and right-click route through InventoryGui's own
    // handlers, and Epic Loot's tooltips and magic-item backgrounds, which patch InventoryGrid for every grid,
    // apply without any special case.
    //
    // Being a panel of its own is the point. A chest and the slots can be open together, nothing about the
    // chest panel or vanilla's container bookkeeping is borrowed, and the panel is placed off the inventory
    // panel's bottom-right corner (ShardSlotPanelOffset), which is the one spot vanilla, Equipment and Quick
    // Slots and the chest panel all leave free. ShardUI decides what the cells hold and what may move.
    internal sealed class ShardSlotPanel {
        // Inset from the panel edge to the cells. The background art overhangs the panel by as much again.
        private const float Pad = 10f;
        private const float TitleTop = 6f;
        private const float TitleHeight = 30f;
        private const float TitleGap = 2f;
        private const float TitleFontSize = 20f;

        // The player grid is group 1 in vanilla's m_uiGroups (0 is the chest, 2 the info bar, 3 crafting).
        private const int PlayerGridGroup = 1;

        private static readonly Vector2 DefaultOffset = new Vector2(10f, -40f);

        private static ShardSlotPanel _current;

        private readonly InventoryGui _gui;
        private readonly RectTransform _root;
        private readonly RectTransform _playerPanel;
        private readonly TMP_Text _title;
        private readonly UIGroupHandler _group;
        private readonly Vector3[] _corners = new Vector3[4];

        public InventoryGrid Grid { get; }

        // False once the InventoryGui this was built for has been torn down (Unity reports a destroyed object
        // as null), which is the signal to build a fresh one for the next window.
        public bool IsAlive => _root != null;

        public bool IsShown => IsAlive && _root.gameObject.activeSelf;

        private ShardSlotPanel(InventoryGui gui, RectTransform root, InventoryGrid grid, TMP_Text title,
            UIGroupHandler group) {
            _gui = gui;
            _root = root;
            _playerPanel = gui.m_player;
            _title = title;
            _group = group;
            Grid = grid;
        }

        // The panel for the live inventory window, built on first use and again after a scene change has
        // replaced the window. Null when the window has nothing to build from.
        public static ShardSlotPanel Get(InventoryGui gui) {
            if (gui == null) {
                return null;
            }

            if (_current != null && _current.IsAlive && _current._gui == gui) {
                return _current;
            }

            _current = Build(gui);
            return _current;
        }

        private static ShardSlotPanel Build(InventoryGui gui) {
            InventoryGrid playerGrid = gui.m_playerGrid;
            RectTransform playerPanel = gui.m_player;
            if (playerGrid == null || playerGrid.m_elementPrefab == null || playerPanel == null) {
                EpicLoot.LogWarning("The inventory window has no player grid to build the shard slot panel from; " +
                    "shard slots cannot be opened.");
                return null;
            }

            // A sibling of the player panel rather than a child: it keeps the window's own transform, which
            // another mod may have scaled the player panel out of, and sits right above the player panel in
            // draw order so the panels and dialogs vanilla draws later stay on top of it.
            Transform parent = playerPanel.parent != null ? playerPanel.parent : playerPanel;
            var rootGo = new GameObject("EpicLoot_ShardSlots", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(parent, false);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            if (parent != playerPanel) {
                root.SetSiblingIndex(playerPanel.GetSiblingIndex() + 1);
            }

            // Vanilla's own panel art, so the panel matches the window whatever restyles it. Instantiate keeps
            // the source's stretch anchors, so the clones fit whatever size the panel is given.
            Transform background = playerPanel.Find("Bkg");
            if (background != null) {
                GameObject clone = Object.Instantiate(background.gameObject, root);
                clone.name = "Bkg";
                clone.SetActive(true);
                clone.transform.SetAsFirstSibling();
            }

            GameObject frame = null;
            Transform selectedFrame = playerPanel.Find("selected_frame");
            if (selectedFrame != null) {
                frame = Object.Instantiate(selectedFrame.gameObject, root);
                frame.name = "selected_frame";
                frame.SetActive(false);
            }

            TMP_Text title = null;
            if (gui.m_containerName != null) {
                GameObject titleGo = Object.Instantiate(gui.m_containerName.gameObject, root);
                titleGo.name = "Title";
                titleGo.SetActive(true);
                var titleRect = (RectTransform)titleGo.transform;
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);
                titleRect.anchoredPosition = new Vector2(0f, -TitleTop);
                titleRect.sizeDelta = new Vector2(0f, TitleHeight);
                // A copy keeps whatever the chest title was set up with, and that may auto-size to its own
                // panel's width: in a band this short an auto-sized line shrinks to nothing. Everything that
                // decides how the text is laid out is set here instead.
                foreach (ContentSizeFitter fitter in titleGo.GetComponents<ContentSizeFitter>()) {
                    Object.Destroy(fitter);
                }

                title = titleGo.GetComponent<TMP_Text>();
                if (title != null) {
                    title.enableAutoSizing = false;
                    title.fontSize = TitleFontSize;
                    title.textWrappingMode = TextWrappingModes.NoWrap;
                    title.overflowMode = TextOverflowModes.Ellipsis;
                    title.alignment = TextAlignmentOptions.Center;
                    title.raycastTarget = false;
                }
            }

            var gridGo = new GameObject("Grid", typeof(RectTransform));
            var gridRect = (RectTransform)gridGo.transform;
            gridRect.SetParent(root, false);
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(0f, 1f);
            gridRect.pivot = new Vector2(0f, 1f);
            gridRect.anchoredPosition = new Vector2(Pad, -(TitleTop + TitleHeight + TitleGap));

            // InventoryGrid lays its cells out from the top-left of this root and sizes it to the rows it
            // draws, the same shape vanilla gives its grids.
            var cellsGo = new GameObject("Root", typeof(RectTransform));
            var cells = (RectTransform)cellsGo.transform;
            cells.SetParent(gridRect, false);
            cells.anchorMin = Vector2.zero;
            cells.anchorMax = Vector2.one;
            cells.pivot = new Vector2(0f, 1f);
            cells.offsetMin = Vector2.zero;
            cells.offsetMax = Vector2.zero;

            // One UI group for the controller: the panel is a stop in the bumper cycle, and while it is the
            // stop the player grid does not read A or X. Priority 1 is what every inventory panel uses, so
            // the groups are told apart by InventoryGui's choice alone.
            var group = rootGo.AddComponent<UIGroupHandler>();
            group.m_groupPriority = 1;
            group.m_enableWhenActiveAndGamepad = frame;

            var grid = gridGo.AddComponent<InventoryGrid>();
            grid.m_elementPrefab = playerGrid.m_elementPrefab;
            grid.m_elementSpace = playerGrid.m_elementSpace;
            grid.m_gridRoot = cells;
            grid.m_tooltipAnchor = playerGrid.m_tooltipAnchor;
            grid.m_uiGroup = group;

            // The handlers vanilla gives its own two grids in Awake. With these a cell here is an inventory
            // cell: drag and drop, quick-move, right-click, touch selection and the drop-focus rule all go
            // through InventoryGui, where ShardUI's patches apply the socket rules.
            grid.m_onSelected += gui.OnSelectedItem;
            grid.m_onRightClick += gui.OnRightClickItem;
            grid.m_onReleased += gui.OnReleasedItem;
            grid.m_onEnter += gui.OnEnterElement;
            grid.OnSetTouchSelection += gui.SetTouchSelection;
            grid.CanDropDragOntoItem += gui.CanDropDragOntoItem;

            rootGo.SetActive(false);
            return new ShardSlotPanel(gui, root, grid, title, group);
        }

        // Sizes the panel for `sockets` cells, titles it, places it and shows it.
        public void Show(string title, int sockets) {
            if (!IsAlive) {
                return;
            }

            int cells = Mathf.Max(1, sockets);
            float space = Grid.m_elementSpace;
            float width = Pad * 2f + cells * space;
            float height = TitleTop + TitleHeight + TitleGap + space + Pad;
            _root.sizeDelta = new Vector2(width, height);
            ((RectTransform)Grid.transform).sizeDelta = new Vector2(width - Pad * 2f, space);

            if (_title != null) {
                _title.text = title;
            }

            Place();
            _root.gameObject.SetActive(true);
            RegisterGroup();
        }

        public void Hide() {
            if (!IsAlive) {
                return;
            }

            UnregisterGroup();
            _root.gameObject.SetActive(false);
        }

        // Hands the controller to the panel's grid.
        public void Focus() {
            if (IsAlive) {
                _gui.SetActiveGroup(_group, playSound: false);
            }
        }

        public bool HasFocus => IsAlive && _group.IsActive;

        // Hangs the panel off the player panel's bottom-right corner, measured through world space so it
        // follows whatever shape and scale the player panel has: vanilla's, a taller inventory, or another
        // mod's restyle. Run every frame the panel is up, so an edit to the offset shows at once.
        public void Place() {
            if (!IsAlive || _playerPanel == null) {
                return;
            }

            var parent = (RectTransform)_root.parent;
            _playerPanel.GetWorldCorners(_corners);
            Vector3 corner = parent.InverseTransformPoint(_corners[3]);
            Rect rect = parent.rect;
            Vector2 offset = ELConfig.ShardSlotPanelOffset != null ? ELConfig.ShardSlotPanelOffset.Value : DefaultOffset;
            var position = new Vector2(corner.x - rect.xMin + offset.x, corner.y - rect.yMax + offset.y);
            if (_root.anchoredPosition != position) {
                _root.anchoredPosition = position;
            }
        }

        // Appends the panel's group to InventoryGui's cycle. Appended, never inserted: vanilla addresses the
        // info bar and crafting groups by index. A group added to the array starts user-active, so vanilla's
        // own loop is re-run on the current choice to leave exactly one active.
        private void RegisterGroup() {
            if (Array.IndexOf(_gui.m_uiGroups, _group) < 0) {
                var groups = new UIGroupHandler[_gui.m_uiGroups.Length + 1];
                _gui.m_uiGroups.CopyTo(groups, 0);
                groups[groups.Length - 1] = _group;
                _gui.m_uiGroups = groups;
            }

            // Group 0 is the chest's. With no chest open, vanilla's cycling resolves a choice of 0 to the
            // last group, which is now this panel; keep the choice on the player grid in that case.
            int current = _gui.m_activeGroup;
            if (current == 0 && !_gui.IsContainerOpen()) {
                current = PlayerGridGroup;
            }
            _gui.SetActiveGroup(current, playSound: false);
        }

        private void UnregisterGroup() {
            int index = Array.IndexOf(_gui.m_uiGroups, _group);
            if (index < 0) {
                return;
            }

            if (_gui.m_activeGroup == index) {
                _gui.SetActiveGroup(PlayerGridGroup, playSound: false);
            }

            var groups = new List<UIGroupHandler>(_gui.m_uiGroups);
            groups.RemoveAt(index);
            _gui.m_uiGroups = groups.ToArray();
            if (_gui.m_activeGroup > index) {
                _gui.m_activeGroup--;
            }
        }
    }
}
