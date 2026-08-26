using System;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // The floating panel next to the inventory, in the classic 2.x EAQS arrangement: the equipment
    // cells form the paperdoll-style two-column cluster (Head/Chest/Legs down the left, Shoulders/
    // Utility/Trinket half a row lower on the right) on their own background, with the quick slots
    // on a separate background row below it. The grid elements for the hidden slot rows already
    // exist (the player grid renders the full-height inventory); this class only shrinks the
    // visible grid and physically relocates those elements. All vanilla behavior — drag/drop,
    // tooltips, gamepad selection, other mods' icon overlays — keeps working because these are
    // real InventoryGrid elements.
    public static class EquipmentPanel {
        // --- 2.x geometry, in player-grid-root space. The panel used to be an empty 255x352 rect
        // holding two center-anchored InventoryGrids; these values are that layout flattened into
        // positions relative to the panel origin (grid center + 100x100 root offset + the old
        // (-20,79) element offset table). The origin itself is the user's config position, or the
        // live drag position while the panel is being dragged. ---
        private static Vector2? _dragPosition;
        private static Vector2 PanelBase => _dragPosition ?? ValConfig.EquipmentPanelPosition.Value;

        private static bool CanDrag => ValConfig.EquipmentPanelDraggable.Value
                                       || (ValConfig.EquipmentPanelDragKey.Value.MainKey != KeyCode.None && ZInput.GetKey(ValConfig.EquipmentPanelDragKey.Value.MainKey));

        private const float elementSpace = 70f;                 // vanilla InventoryGrid.m_elementSpace
        private const float slotSpacing = elementSpace + 10f;   // 2.x equipment grid spacing
        private static readonly Vector2 equipmentOrigin = new Vector2(60.5f, -27f);
        private static readonly Vector2[] equipmentOffsets = {
            new Vector2(0f, 0f),                                // Head
            new Vector2(0f, -slotSpacing),                      // Chest
            new Vector2(0f, -2f * slotSpacing),                 // Legs
            new Vector2(slotSpacing, -0.5f * slotSpacing),      // Shoulders
            new Vector2(slotSpacing, -1.5f * slotSpacing),      // Utility
            new Vector2(slotSpacing, -2.5f * slotSpacing),      // Trinket
            new Vector2(2f * slotSpacing, -1.5f * slotSpacing), // Utility 2
            new Vector2(2f * slotSpacing, -2.5f * slotSpacing), // Utility 3
        };
        private static readonly Vector2 equipmentBackgroundCenter = new Vector2(132.5f, -159f);
        private static readonly Vector2 equipmentBackgroundSize = new Vector2(210f, 300f);

        // The extra utility cells form a third column. The background grows to cover it and the
        // centre moves half as far, so its left edge — and the rest of the panel — stay put. With
        // one utility slot the panel is pixel-for-pixel what it was.
        private static float ExtraUtilityColumnWidth => ActiveExtraUtilitySlots > 0 ? slotSpacing : 0f;
        private static Vector2 EquipmentBackgroundSize => equipmentBackgroundSize + new Vector2(ExtraUtilityColumnWidth, 0f);
        private static Vector2 EquipmentBackgroundCenter => equipmentBackgroundCenter + new Vector2(ExtraUtilityColumnWidth / 2f, 0f);
        private static readonly Vector2 equipmentLabelPosition = new Vector2(32f, 5f);

        private const float rowLeft = 25.5f;                    // first cell of a slot row
        private const float rowGap = 20f;                       // breathing room between the background strips
        private const float quickRowTop = -342f;                // quick strip starts rowGap below the equipment background
        private const float rowBackgroundLeft = 14.5f;
        private const float rowBackgroundHeight = 90f;
        private const float rowBackgroundPitch = 74f;           // 2.x: background width = 74 per slot + 10
        private const float customRowTop = quickRowTop - rowBackgroundHeight - rowGap;
        private const int customSlotsPerRow = 3;                // API slots wrap to a new row past this

        // The doll images are center-anchored in their prefab; nudge them under the Head/Chest/Legs
        // column of the equipment background.
        private static readonly Vector2 paperdollOffset = new Vector2(-35f, 0f);

        private static float originalLabelFontSize;

        private static RectTransform inventoryBackground;
        private static Image inventoryBackgroundImage;
        private static RectTransform inventoryDarken;
        private static RectTransform inventorySelectedFrame;
        private static Vector2? containerOriginalPivot;
        private static RectTransform equipmentBackground;
        private static RectTransform quickBackground;
        private static RectTransform customBackground;
        private static GameObject paperdoll;
        private static RectTransform[] paperdollImages;

        private static Color normalColor = Color.clear;
        private static Color highlightedColor = Color.clear;

        private static int ActiveQuickSlots => ValConfig.QuickSlotsEnabled.Value ? ValConfig.QuickSlotCount.Value : 0;
        private static bool EquipmentVisible => ValConfig.EquipmentSlotsEnabled.Value;
        private static Slot[] ActiveCustomSlots => GetCustomSlots().Where(slot => slot.IsActive).OrderBy(slot => slot.Index).ToArray();

        internal static Vector2 GetSlotPosition(Slot slot) {
            if (slot.IsEquipmentSlot)
                return PanelBase + equipmentOrigin + equipmentOffsets[slot.Index - EquipmentSlotStartIndex];

            if (slot.IsQuickSlot)
                return PanelBase + new Vector2(rowLeft + slot.Index * elementSpace, quickRowTop);

            if (slot.IsCustomSlot) {
                // API slots fill rows of three under the quick slots, packed left without gaps
                int ordinal = Math.Max(0, Array.IndexOf(ActiveCustomSlots, slot));
                int col = ordinal % customSlotsPerRow;
                int row = ordinal / customSlotsPerRow;
                return PanelBase + new Vector2(rowLeft + col * elementSpace, customRowTop - row * elementSpace);
            }

            return PanelBase;
        }

        // Runs from InventoryGui.Update while visible: clones the inventory background once per
        // slot group, then keeps size, position and skin in sync.
        internal static void UpdateEquipmentBackground() {
            if (!InventoryGui.instance || !InventoryGui.instance.m_player)
                return;

            if (inventoryBackground == null) {
                inventoryBackground = InventoryGui.instance.m_player.Find("Bkg")?.GetComponent<RectTransform>();
                inventoryBackgroundImage = inventoryBackground?.GetComponent<Image>();
                inventoryDarken = InventoryGui.instance.m_player.Find("Darken")?.GetComponent<RectTransform>();
                inventorySelectedFrame = InventoryGui.instance.m_player.GetComponent<UIGroupHandler>()?.m_enableWhenActiveAndGamepad?.transform.GetChild(0) as RectTransform;
            }
            if (inventoryBackground == null)
                return;

            UpdateInventoryPanelForExtraRows();

            if (!equipmentBackground)
                equipmentBackground = CreateBackground("EaqsEquipmentBkg");
            if (!quickBackground)
                quickBackground = CreateBackground("EaqsQuickSlotBkg");
            if (!customBackground)
                customBackground = CreateBackground("EaqsCustomSlotBkg");

            SyncBackground(equipmentBackground, EquipmentVisible, PanelBase + EquipmentBackgroundCenter, EquipmentBackgroundSize);
            UpdatePaperdoll();

            int quickCount = ActiveQuickSlots;
            SyncBackground(quickBackground, quickCount > 0, RowBackgroundCenter(quickCount, quickRowTop), RowBackgroundSize(quickCount));

            int customCount = ActiveCustomSlots.Length;
            int customRows = (customCount + customSlotsPerRow - 1) / customSlotsPerRow;
            SyncBackground(customBackground, customCount > 0, CustomBackgroundCenter(customCount, customRows), CustomBackgroundSize(customCount, customRows));
        }

        // Optional character paperdoll behind the equipment cells: the same prefab the Auga panel
        // uses, stretched over the equipment background with the doll re-centered under the body
        // column, gender following the player model.
        private static void UpdatePaperdoll() {
            bool show = ValConfig.ShowPaperdoll.Value && EquipmentVisible && EquipmentAndQuickSlots.Paperdolls != null && equipmentBackground;
            if (!show) {
                if (paperdoll)
                    paperdoll.SetActive(false);
                return;
            }

            if (!paperdoll) {
                paperdoll = UnityEngine.Object.Instantiate(EquipmentAndQuickSlots.Paperdolls, equipmentBackground, false);
                paperdoll.name = "Paperdolls";

                RectTransform rect = paperdoll.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.SetAsFirstSibling();

                Image[] images = paperdoll.GetComponentsInChildren<Image>(true);
                paperdollImages = new RectTransform[images.Length];
                for (int i = 0; i < images.Length; i++) {
                    images[i].raycastTarget = false;
                    images[i].preserveAspect = true;
                    paperdollImages[i] = images[i].rectTransform;
                }
            }

            paperdoll.SetActive(true);

            // The doll sits under the body column, so it has to move back by whatever the extra
            // utility column added to the background it is stretched across.
            Vector2 dollOffset = paperdollOffset - new Vector2(ExtraUtilityColumnWidth / 2f, 0f);
            foreach (RectTransform image in paperdollImages)
                image.anchoredPosition = dollOffset;

            Player player = Player.m_localPlayer;
            bool female = player != null && player.m_visEquipment != null && player.m_visEquipment.GetModelIndex() == 1;
            paperdoll.transform.Find("Male")?.gameObject.SetActive(!female);
            paperdoll.transform.Find("Female")?.gameObject.SetActive(female);
        }

        // Extra visible rows: the player panel's stretch-anchored background (and its darken /
        // gamepad selection frame) is extended downward by one base-row-fraction per extra row,
        // and the container panel is nudged down so it doesn't sit under the taller inventory.
        private static void UpdateInventoryPanelForExtraRows() {
            int extraRows = ExtraRows;
            float anchorY = -1f * (extraRows / (float)BaseRows - 0.01f * Math.Max(extraRows - 1, 0));

            inventoryBackground.anchorMin = new Vector2(0f, anchorY);
            if (inventoryDarken)
                inventoryDarken.anchorMin = inventoryBackground.anchorMin;
            if (inventorySelectedFrame)
                inventorySelectedFrame.anchorMin = inventoryBackground.anchorMin;

            RectTransform container = InventoryGui.instance.m_container;
            if (container) {
                if (containerOriginalPivot == null)
                    containerOriginalPivot = container.pivot;
                container.pivot = new Vector2(containerOriginalPivot.Value.x, containerOriginalPivot.Value.y + extraRows * 0.2f);
            }
        }

        private static Vector2 RowBackgroundSize(int slotCount) => new Vector2(rowBackgroundPitch * slotCount + 10f, rowBackgroundHeight);

        // One background behind all API-slot rows: as wide as the fullest row, one cell pitch
        // taller per extra row.
        private static Vector2 CustomBackgroundSize(int slotCount, int rows) =>
            new Vector2(rowBackgroundPitch * Math.Min(slotCount, customSlotsPerRow) + 10f, rowBackgroundHeight + Math.Max(0, rows - 1) * elementSpace);

        private static Vector2 CustomBackgroundCenter(int slotCount, int rows) =>
            PanelBase + new Vector2(rowBackgroundLeft + CustomBackgroundSize(slotCount, rows).x / 2f,
                                    customRowTop - (elementSpace - 6f) / 2f - Math.Max(0, rows - 1) * elementSpace / 2f);

        // Row backgrounds are centered on their cells: the 64-tall cell sits mid-way in the 90-tall strip
        private static Vector2 RowBackgroundCenter(int slotCount, float rowTop) =>
            PanelBase + new Vector2(rowBackgroundLeft + RowBackgroundSize(slotCount).x / 2f, rowTop - (elementSpace - 6f) / 2f);

        private static RectTransform CreateBackground(string name) {
            Transform player = InventoryGui.instance.m_player;
            Transform selectedFrames = player.GetComponent<UIGroupHandler>()?.m_enableWhenActiveAndGamepad?.transform;
            Transform darken = player.Find("Darken");

            RectTransform background = UnityEngine.Object.Instantiate(inventoryBackground, player, worldPositionStays: false);
            background.name = name;
            // Drawn behind the grid: right after the panel's own Darken / selection frames
            int anchorIndex = selectedFrames != null ? selectedFrames.GetSiblingIndex() : darken != null ? darken.GetSiblingIndex() : inventoryBackground.GetSiblingIndex();
            background.SetSiblingIndex(anchorIndex + 1);
            background.anchorMin = new Vector2(0f, 1f);
            background.anchorMax = new Vector2(0f, 1f);
            background.pivot = new Vector2(0.5f, 0.5f);
            background.localScale = Vector3.one;

            // The backgrounds are the drag handles: grabbing any of them moves the whole panel
            Image image = background.GetComponent<Image>();
            if (image)
                image.raycastTarget = true;
            background.gameObject.AddComponent<PanelDragHandle>();

            return background;
        }

        // Drag-to-move on the panel backgrounds. Gated behind the drag key (or the always-draggable
        // toggle) so ordinary clicks around the slots can't nudge the panel. The live position is
        // applied every frame while dragging; the config (and therefore the file) is written once
        // on release.
        private class PanelDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
            private bool _dragging;
            private float _scaleFactor = 1f;

            public void OnBeginDrag(PointerEventData eventData) {
                if (!CanDrag || eventData.button != PointerEventData.InputButton.Left)
                    return;

                _dragging = true;
                _dragPosition = PanelBase;
                _scaleFactor = GetComponentInParent<Canvas>()?.scaleFactor ?? 1f;
                if (_scaleFactor <= 0f)
                    _scaleFactor = 1f;
            }

            public void OnDrag(PointerEventData eventData) {
                if (!_dragging || _dragPosition == null)
                    return;

                _dragPosition = _dragPosition.Value + eventData.delta / _scaleFactor;
            }

            public void OnEndDrag(PointerEventData eventData) {
                if (!_dragging)
                    return;

                _dragging = false;
                if (_dragPosition != null)
                    ValConfig.EquipmentPanelPosition.Value = _dragPosition.Value;
                _dragPosition = null;
            }

            private void OnDisable() {
                // Inventory closed mid-drag: keep whatever was dragged so far
                if (_dragging)
                    OnEndDrag(null);
            }
        }

        private static void SyncBackground(RectTransform background, bool visible, Vector2 center, Vector2 size) {
            if (!background)
                return;

            background.gameObject.SetActive(visible);
            if (!visible)
                return;

            background.sizeDelta = size;
            background.anchoredPosition = center;

            Image image = background.GetComponent<Image>();
            if (image && inventoryBackgroundImage) {
                image.sprite = inventoryBackgroundImage.sprite;
                image.overrideSprite = inventoryBackgroundImage.overrideSprite;
                image.color = inventoryBackgroundImage.color;
            }
        }

        // Runs from InventoryGrid.UpdateGui on the player grid: shrink the visible grid, relocate
        // slot elements, label them, tint unfit targets while dragging.
        internal static void UpdateInventorySlots() {
            InventoryGrid grid = InventoryGui.instance.m_playerGrid;

            grid.m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, VisibleRows * grid.m_elementSpace);

            int startIndex = InventorySizeVisible;
            ItemDrop.ItemData dragItem = InventoryGui.instance.m_dragItem;

            for (int i = 0; i < Math.Min(slots.Length, grid.m_elements.Count - startIndex); ++i) {
                InventoryGrid.Element element = grid.m_elements[startIndex + i];
                Slot slot = slots[i];

                GameObject go = element?.m_go;
                if (!go)
                    continue;

                go.SetActive(slot.IsActive);
                if (!slot.IsActive)
                    continue;

                go.GetComponent<RectTransform>().anchoredPosition = EquipmentAndQuickSlots.HasAuga ? AugaPanel.GetSlotPosition(slot) : GetSlotPosition(slot);
                SetSlotLabel(go.transform.Find("binding"), slot);
                SetSlotColor(go.GetComponent<Button>(), dragItem != null && !DragItemFits(slot, dragItem));
            }

            for (int i = startIndex + slots.Length; i < grid.m_elements.Count; i++)
                grid.m_elements[i]?.m_go?.SetActive(false);
        }

        private static bool DragItemFits(Slot slot, ItemDrop.ItemData dragItem) {
            // For equipment cells the drag lands via drag-to-equip, so the tint should follow the
            // type check, not the equipped-state predicate.
            if (slot.IsEquipmentSlot)
                return WouldFitEquipmentSlot(slot, dragItem);

            return slot.ItemFits(dragItem);
        }

        private static void SetSlotLabel(Transform binding, Slot slot) {
            if (!binding)
                return;

            TMP_Text text = binding.GetComponent<TMP_Text>();
            if (!text)
                return;

            // The paperdoll itself communicates the equipment slots; no labels there under Auga
            if (EquipmentAndQuickSlots.HasAuga && slot.IsEquipmentSlot) {
                text.enabled = false;
                return;
            }

            // Remember the vanilla label size before any auto-sizing touches a label
            if (originalLabelFontSize <= 0f && !text.enableAutoSizing)
                originalLabelFontSize = text.fontSize;

            binding.gameObject.SetActive(true);
            text.enabled = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.text = slot.IsHotkeySlot ? slot.GetShortcutText() : slot.Name;

            // Equipment labels sit inside the cell like 2.x did; hotkey labels keep the vanilla
            // hotbar-number placement.
            if (slot.IsEquipmentSlot)
                text.rectTransform.anchoredPosition = equipmentLabelPosition;

            // API slot names are arbitrary length: pin the label to the cell width, shrink to fit
            // and ellipsize instead of spilling over the neighbouring cell.
            if (slot.IsCustomSlot) {
                text.rectTransform.anchoredPosition = equipmentLabelPosition;
                text.rectTransform.sizeDelta = new Vector2(elementSpace - 6f, text.rectTransform.sizeDelta.y);
                text.overflowMode = TextOverflowModes.Ellipsis;
                if (originalLabelFontSize > 0f) {
                    text.fontSizeMax = originalLabelFontSize;
                    text.fontSizeMin = Mathf.Min(10f, originalLabelFontSize);
                    text.enableAutoSizing = true;
                }
            }
        }

        private static void SetSlotColor(Button button, bool unfit) {
            if (!button)
                return;

            if (normalColor == Color.clear) {
                normalColor = button.colors.normalColor;
                highlightedColor = button.colors.highlightedColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = unfit ? new Color(0.8f, 0.2f, 0.2f, 0.5f) : normalColor;
            colors.highlightedColor = unfit ? new Color(0.9f, 0.3f, 0.3f, 0.7f) : highlightedColor;
            button.colors = colors;
        }

        private static void ClearPanel() {
            inventoryBackground = null;
            inventoryBackgroundImage = null;
            inventoryDarken = null;
            inventorySelectedFrame = null;
            containerOriginalPivot = null;
            equipmentBackground = null;
            quickBackground = null;
            customBackground = null;
            paperdoll = null;
            paperdollImages = null;
            _dragPosition = null;
            normalColor = Color.clear;
            highlightedColor = Color.clear;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
        private static class InventoryGui_OnDestroy_ClearPanel {
            private static void Postfix() {
                ClearPanel();
                AugaPanel.Clear();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
        private static class InventoryGui_Update_UpdateEquipmentPanel {
            private static void Postfix() {
                if (!Player.m_localPlayer)
                    return;

                if (!InventoryGui.IsVisible())
                    return;

                if (EquipmentAndQuickSlots.HasAuga)
                    AugaPanel.UpdatePanel();
                else
                    UpdateEquipmentBackground();
            }
        }

        // Vanilla gamepad navigation walks the raw grid; steer the selection off reserved and
        // inactive slot cells (their elements are hidden) onto the nearest active slot.
        [HarmonyPatch(typeof(InventoryGrid), "UpdateGamepad")]
        private static class InventoryGrid_UpdateGamepad_SkipInactiveSlotCells {
            private static void Postfix(InventoryGrid __instance) {
                if (!InventoryGui.instance || __instance != InventoryGui.instance.m_playerGrid)
                    return;

                Vector2i sel = __instance.m_selected;
                if (sel.y < VisibleRows)
                    return;

                int slotIndex = (sel.y - VisibleRows) * InventoryWidth + sel.x;
                if (slotIndex >= 0 && slotIndex < slots.Length && slots[slotIndex].IsActive)
                    return;

                int best = -1;
                int bestDist = int.MaxValue;
                for (int i = 0; i < slots.Length; i++) {
                    if (!slots[i].IsActive)
                        continue;

                    int dist = Math.Abs(i - slotIndex);
                    if (dist < bestDist) {
                        bestDist = dist;
                        best = i;
                    }
                }

                __instance.m_selected = best >= 0 ? slots[best].GridPosition : new Vector2i(Math.Min(sel.x, InventoryWidth - 1), VisibleRows - 1);
            }
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        private static class InventoryGrid_UpdateGui_RelocateSlotElements {
            private static void Postfix(InventoryGrid __instance) {
                if (!InventoryGui.instance || __instance != InventoryGui.instance.m_playerGrid)
                    return;

                if (Player.m_localPlayer == null)
                    return;

                UpdateInventorySlots();
            }
        }
    }
}
