using System;
using System.Collections.Generic;
using EpicLoot.Config;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.ShardStones {
    public enum ShardSlotPanelPlacement {
        Automatic,
        Manual
    }

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

        // More than the background art's overhang.
        private const float PanelGap = 14f;
        private const float ScreenMargin = 4f;

        // What counts as another panel's art: a graphic at least this big that does not cover most of the
        // screen (a full-screen backdrop is not a panel; vanilla's click catcher behind the window is invisible
        // and left out anyway).
        private const float MinObstacleSize = 4f;
        private const float BackdropFraction = 0.9f;

        private const float LayoutCheckInterval = 0.25f;

        private static ShardSlotPanel _current;

        private readonly InventoryGui _gui;
        private readonly RectTransform _root;
        private readonly RectTransform _playerPanel;
        private readonly TMP_Text _title;
        private readonly UIGroupHandler _group;
        private readonly Vector3[] _corners = new Vector3[4];

        private readonly List<Rect> _obstacles = new List<Rect>();
        // From the inventory panel's corner, so the panel follows it.
        private Vector2 _autoOffset;
        private bool _hasAutoOffset;
        private int _layoutSignature;
        private float _nextLayoutCheck;

        private bool _dragging;
        private Vector2 _dragStartPointer;
        private Vector2 _dragStartTopLeft;

        public InventoryGrid Grid { get; }

        // Unity reports a destroyed object as null, so this turns false once the window is torn down.
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

            // Vanilla's own panel art, so the panel matches whatever restyles the window.
            GameObject dragSurface = null;
            Transform background = playerPanel.Find("Bkg");
            if (background != null) {
                dragSurface = Object.Instantiate(background.gameObject, root);
                dragSurface.name = "Bkg";
                dragSurface.SetActive(true);
                dragSurface.transform.SetAsFirstSibling();
            }

            // The frame is the drag handle, and it takes the clicks that land on it: a click there with an item
            // in hand would otherwise reach vanilla's click-outside-the-window catcher and drop the item. The
            // handle is on the background, beside the cells rather than above them, so a drag that starts on a
            // cell never reaches it.
            if (dragSurface == null || dragSurface.GetComponent<Graphic>() == null) {
                dragSurface = new GameObject("DragSurface", typeof(RectTransform), typeof(Image));
                var surfaceRect = (RectTransform)dragSurface.transform;
                surfaceRect.SetParent(root, false);
                surfaceRect.anchorMin = Vector2.zero;
                surfaceRect.anchorMax = Vector2.one;
                surfaceRect.offsetMin = Vector2.zero;
                surfaceRect.offsetMax = Vector2.zero;
                surfaceRect.SetAsFirstSibling();
                dragSurface.GetComponent<Image>().color = Color.clear;
            }

            dragSurface.GetComponent<Graphic>().raycastTarget = true;
            DragHandle dragHandle = dragSurface.AddComponent<DragHandle>();

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

            var cellsGo = new GameObject("Root", typeof(RectTransform));
            var cells = (RectTransform)cellsGo.transform;
            cells.SetParent(gridRect, false);
            cells.anchorMin = Vector2.zero;
            cells.anchorMax = Vector2.one;
            cells.pivot = new Vector2(0f, 1f);
            cells.offsetMin = Vector2.zero;
            cells.offsetMax = Vector2.zero;

            // Priority 1 is what every inventory panel uses, so the groups are told apart by InventoryGui's
            // choice alone.
            var group = rootGo.AddComponent<UIGroupHandler>();
            group.m_groupPriority = 1;
            group.m_enableWhenActiveAndGamepad = frame;

            var grid = gridGo.AddComponent<InventoryGrid>();
            grid.m_elementPrefab = playerGrid.m_elementPrefab;
            grid.m_elementSpace = playerGrid.m_elementSpace;
            grid.m_gridRoot = cells;
            grid.m_tooltipAnchor = playerGrid.m_tooltipAnchor;
            grid.m_uiGroup = group;

            // The handlers vanilla gives its own two grids in Awake.
            grid.m_onSelected += gui.OnSelectedItem;
            grid.m_onRightClick += gui.OnRightClickItem;
            grid.m_onReleased += gui.OnReleasedItem;
            grid.m_onEnter += gui.OnEnterElement;
            grid.OnSetTouchSelection += gui.SetTouchSelection;
            grid.CanDropDragOntoItem += gui.CanDropDragOntoItem;

            rootGo.SetActive(false);
            var panel = new ShardSlotPanel(gui, root, grid, title, group);
            dragHandle.Panel = panel;
            return panel;
        }

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

            _hasAutoOffset = false;
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

        public void Focus() {
            if (IsAlive) {
                _gui.SetActiveGroup(_group, playSound: false);
            }
        }

        public bool HasFocus => IsAlive && _group.IsActive;

        // Measured through world space so it follows whatever shape and scale the player panel has. Everything
        // here is in the local space of the panel's parent, the window root.
        public void Place() {
            if (!IsAlive || _playerPanel == null || _dragging) {
                return;
            }

            var parent = (RectTransform)_root.parent;
            Vector2 corner = PlayerCorner(parent);
            Vector2 offset;
            if (ELConfig.ShardSlotPanelPlacement != null && ELConfig.ShardSlotPanelPlacement.Value == ShardSlotPanelPlacement.Manual) {
                offset = ELConfig.ShardSlotPanelOffset.Value;
            } else {
                PlaceAutomatically(parent, corner);
                offset = _autoOffset;
            }

            SetTopLeft(parent, ClampToScreen(parent, corner + offset));
        }

        // A layout change that leaves the panel clear does not move it, so it does not jump about in use.
        private void PlaceAutomatically(RectTransform parent, Vector2 corner) {
            float now = Time.unscaledTime;
            if (_hasAutoOffset && now < _nextLayoutCheck) {
                return;
            }

            _nextLayoutCheck = now + LayoutCheckInterval;
            int signature = LayoutSignature(parent);
            if (_hasAutoOffset && signature == _layoutSignature) {
                return;
            }

            _layoutSignature = signature;
            CollectObstacles(parent);
            Vector2 size = _root.sizeDelta;
            Rect bounds = ScreenBounds(parent, size);
            Vector2 current = corner + _autoOffset;
            if (_hasAutoOffset && current.x >= bounds.xMin && current.x <= bounds.xMax &&
                current.y >= bounds.yMin && current.y <= bounds.yMax && IsFree(PanelRect(current, size))) {
                return;
            }

            _autoOffset = FindFreeSpot(parent, corner, size) - corner;
            _hasAutoOffset = true;
            EpicLoot.Log($"[ShardSlotPanel] Placed at {_autoOffset} from the inventory panel's corner, clear of " +
                $"{_obstacles.Count} other panel parts.");
        }

        // The free top-left nearest the preferred spot. A free spot is either the preferred one or one that sits
        // against an edge of the player panel or of another panel, so those edges are the only candidates. They
        // are tried nearest first, so the first free one along each axis ends that axis's search.
        private Vector2 FindFreeSpot(RectTransform parent, Vector2 corner, Vector2 size) {
            Vector2 preferred = corner + DefaultOffset;
            Rect bounds = ScreenBounds(parent, size);

            // The screen edges too, for a small screen where the preferred spot itself is off the edge.
            var xs = new List<float> { preferred.x, corner.x + PanelGap, corner.x - size.x, bounds.xMin, bounds.xMax };
            var ys = new List<float> { preferred.y, corner.y - PanelGap, bounds.yMin, bounds.yMax };
            if (TryRectInSpace(parent, _playerPanel, out Rect player)) {
                xs.Add(player.xMin);
                ys.Add(player.yMax);
            }

            foreach (Rect obstacle in _obstacles) {
                xs.Add(obstacle.xMax + PanelGap);
                xs.Add(obstacle.xMin - PanelGap - size.x);
                ys.Add(obstacle.yMin - PanelGap);
                ys.Add(obstacle.yMax + PanelGap + size.y);
            }

            xs.Sort((a, b) => Mathf.Abs(a - preferred.x).CompareTo(Mathf.Abs(b - preferred.x)));
            ys.Sort((a, b) => Mathf.Abs(a - preferred.y).CompareTo(Mathf.Abs(b - preferred.y)));

            Vector2 best = ClampToScreen(parent, preferred);
            float bestDistance = float.MaxValue;
            foreach (float x in xs) {
                float dx = x - preferred.x;
                if (dx * dx >= bestDistance) {
                    break;
                }

                if (x < bounds.xMin || x > bounds.xMax) {
                    continue;
                }

                foreach (float y in ys) {
                    float dy = y - preferred.y;
                    float distance = dx * dx + dy * dy;
                    if (distance >= bestDistance) {
                        break;
                    }

                    if (y < bounds.yMin || y > bounds.yMax || !IsFree(PanelRect(new Vector2(x, y), size))) {
                        continue;
                    }

                    best = new Vector2(x, y);
                    bestDistance = distance;
                    break;
                }
            }

            return best;
        }

        // Free means at least PanelGap from every other panel. A candidate sits exactly that far from the edge
        // it came from, so the test allows that much and no less.
        private bool IsFree(Rect panel) {
            const float gap = PanelGap - 0.5f;
            var padded = Rect.MinMaxRect(panel.xMin - gap, panel.yMin - gap, panel.xMax + gap, panel.yMax + gap);
            foreach (Rect obstacle in _obstacles) {
                if (padded.Overlaps(obstacle)) {
                    return false;
                }
            }

            return true;
        }

        // What the other panels draw: the rectangle of every visible graphic in the window, so a part another mod
        // hangs outside its panel (EAQS's equipment and quick slot backgrounds and cells, the chest panel's weight
        // box) counts where it is drawn. Graphics are clipped to any mask above them, so a scroll list's hidden
        // rows do not count, and one inside a rectangle already taken (a cell on its panel's background, an icon
        // on its cell) is left out, which keeps the list to a few dozen.
        private void CollectObstacles(RectTransform parent) {
            _obstacles.Clear();
            Rect screen = parent.rect;
            var unbounded = new Rect(float.MinValue / 2f, float.MinValue / 2f, float.MaxValue, float.MaxValue);
            foreach (Transform panel in parent) {
                if (panel != _root) {
                    Collect(parent, panel, unbounded, screen);
                }
            }
        }

        private void Collect(RectTransform parent, Transform node, Rect clip, Rect screen) {
            if (!IsShownSubtree(node)) {
                return;
            }

            AddGraphic(parent, node, clip, screen);
            clip = ClipBy(parent, node, clip);
            foreach (Transform child in node) {
                Collect(parent, child, clip, screen);
            }
        }

        private static bool IsShownSubtree(Transform node) {
            if (!node.gameObject.activeSelf) {
                return false;
            }

            var group = node.GetComponent<CanvasGroup>();
            return group == null || !group.enabled || group.alpha > 0.01f;
        }

        private void AddGraphic(RectTransform parent, Transform node, Rect clip, Rect screen) {
            var graphic = node.GetComponent<Graphic>();
            if (graphic == null || !graphic.enabled || graphic.color.a <= 0.01f ||
                (graphic is TMP_Text text && string.IsNullOrEmpty(text.text)) ||
                !TryRectInSpace(parent, graphic.rectTransform, out Rect rect)) {
                return;
            }

            if (rect.width >= screen.width * BackdropFraction && rect.height >= screen.height * BackdropFraction) {
                return;
            }

            rect = Rect.MinMaxRect(Mathf.Max(rect.xMin, clip.xMin), Mathf.Max(rect.yMin, clip.yMin),
                Mathf.Min(rect.xMax, clip.xMax), Mathf.Min(rect.yMax, clip.yMax));
            if (rect.width < MinObstacleSize || rect.height < MinObstacleSize) {
                return;
            }

            foreach (Rect taken in _obstacles) {
                if (rect.xMin >= taken.xMin && rect.xMax <= taken.xMax && rect.yMin >= taken.yMin && rect.yMax <= taken.yMax) {
                    return;
                }
            }

            _obstacles.Add(rect);
        }

        // The clip a mask on `node` puts on everything under it.
        private Rect ClipBy(RectTransform parent, Transform node, Rect clip) {
            var rectMask = node.GetComponent<RectMask2D>();
            var mask = node.GetComponent<Mask>();
            if ((rectMask == null || !rectMask.enabled) && (mask == null || !mask.enabled)) {
                return clip;
            }

            if (!TryRectInSpace(parent, (RectTransform)node, out Rect rect)) {
                return clip;
            }

            return Rect.MinMaxRect(Mathf.Max(rect.xMin, clip.xMin), Mathf.Max(rect.yMin, clip.yMin),
                Mathf.Min(rect.xMax, clip.xMax), Mathf.Min(rect.yMax, clip.yMax));
        }

        // Decides whether the full measurement in CollectObstacles has to run again.
        private int LayoutSignature(RectTransform parent) {
            unchecked {
                int hash = 17;
                hash = hash * 31 + Mathf.RoundToInt(parent.rect.width);
                hash = hash * 31 + Mathf.RoundToInt(parent.rect.height);
                hash = hash * 31 + Mathf.RoundToInt(_root.sizeDelta.x);
                foreach (Transform panel in parent) {
                    if (panel == _root) {
                        continue;
                    }

                    hash = hash * 31 + Fingerprint(panel);
                    if (!IsShownSubtree(panel)) {
                        continue;
                    }

                    foreach (Transform part in panel) {
                        hash = hash * 31 + Fingerprint(part);
                    }
                }

                return hash;
            }
        }

        // Shown or hidden counts by a fade group too, not only by being active: MidgardPlus keeps its equipment
        // panel active but faded out while it settles into place (up to a second and a half after the window or a
        // chest opens), and the panel has to be measured again once it fades in.
        private static int Fingerprint(Transform node) {
            unchecked {
                if (!IsShownSubtree(node) || !(node is RectTransform rect)) {
                    return 0;
                }

                Vector3 position = rect.localPosition;
                Vector2 size = rect.rect.size;
                return (((Mathf.RoundToInt(position.x) * 31 + Mathf.RoundToInt(position.y)) * 31
                    + Mathf.RoundToInt(size.x)) * 31 + Mathf.RoundToInt(size.y)) * 31
                    + Mathf.RoundToInt(rect.localScale.x * 100f) + 1;
            }
        }

        private bool TryRectInSpace(RectTransform parent, RectTransform target, out Rect rect) {
            rect = default;
            if (target == null) {
                return false;
            }

            target.GetWorldCorners(_corners);
            float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
            foreach (Vector3 worldCorner in _corners) {
                Vector3 local = parent.InverseTransformPoint(worldCorner);
                xMin = Mathf.Min(xMin, local.x);
                yMin = Mathf.Min(yMin, local.y);
                xMax = Mathf.Max(xMax, local.x);
                yMax = Mathf.Max(yMax, local.y);
            }

            rect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            return !float.IsNaN(rect.width) && !float.IsNaN(rect.height) && rect.width > 0f && rect.height > 0f;
        }

        private Vector2 PlayerCorner(RectTransform parent) {
            _playerPanel.GetWorldCorners(_corners);
            return parent.InverseTransformPoint(_corners[3]);
        }

        private static Rect PanelRect(Vector2 topLeft, Vector2 size) =>
            new Rect(topLeft.x, topLeft.y - size.y, size.x, size.y);

        private static Rect ScreenBounds(RectTransform parent, Vector2 size) {
            Rect screen = parent.rect;
            float xMin = screen.xMin + ScreenMargin;
            float yMax = screen.yMax - ScreenMargin;
            float xMax = Mathf.Max(xMin, screen.xMax - ScreenMargin - size.x);
            float yMin = Mathf.Min(yMax, screen.yMin + ScreenMargin + size.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private Vector2 ClampToScreen(RectTransform parent, Vector2 topLeft) {
            Rect bounds = ScreenBounds(parent, _root.sizeDelta);
            return new Vector2(Mathf.Clamp(topLeft.x, bounds.xMin, bounds.xMax), Mathf.Clamp(topLeft.y, bounds.yMin, bounds.yMax));
        }

        private Vector2 TopLeft(RectTransform parent) {
            Rect rect = parent.rect;
            return new Vector2(rect.xMin + _root.anchoredPosition.x, rect.yMax + _root.anchoredPosition.y);
        }

        private void SetTopLeft(RectTransform parent, Vector2 topLeft) {
            Rect rect = parent.rect;
            var position = new Vector2(topLeft.x - rect.xMin, topLeft.y - rect.yMax);
            if (_root.anchoredPosition != position) {
                _root.anchoredPosition = position;
            }
        }

        private void BeginDrag(PointerEventData data) {
            if (!IsShown || data.button != PointerEventData.InputButton.Left) {
                return;
            }

            var parent = (RectTransform)_root.parent;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, data.position, data.pressEventCamera,
                    out _dragStartPointer)) {
                return;
            }

            _dragStartTopLeft = TopLeft(parent);
            _dragging = true;
        }

        private void Drag(PointerEventData data) {
            var parent = (RectTransform)_root.parent;
            if (!_dragging || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, data.position,
                    data.pressEventCamera, out Vector2 pointer)) {
                return;
            }

            SetTopLeft(parent, ClampToScreen(parent, _dragStartTopLeft + pointer - _dragStartPointer));
        }

        private void EndDrag() {
            if (!_dragging) {
                return;
            }

            _dragging = false;
            if (!IsAlive || _playerPanel == null) {
                return;
            }

            var parent = (RectTransform)_root.parent;
            Vector2 offset = TopLeft(parent) - PlayerCorner(parent);
            ELConfig.ShardSlotPanelOffset.Value = new Vector2(Mathf.Round(offset.x), Mathf.Round(offset.y));
            ELConfig.ShardSlotPanelPlacement.Value = ShardSlotPanelPlacement.Manual;
        }

        private sealed class DragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
            public ShardSlotPanel Panel;

            public void OnBeginDrag(PointerEventData eventData) => Panel?.BeginDrag(eventData);

            public void OnDrag(PointerEventData eventData) => Panel?.Drag(eventData);

            public void OnEndDrag(PointerEventData eventData) => Panel?.EndDrag();

            // The window closed mid-drag: keep the spot reached so far.
            private void OnDisable() => Panel?.EndDrag();
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
