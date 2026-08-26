using System.Linq;
using UnityEngine;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // Auga variant of the floating panel: an Auga-styled panel with the paperdoll backdrop and a
    // small divider, with the equipment cells laid out over the doll and the quick row beneath.
    // Uses the same element-relocation mechanism as the vanilla panel — only the background
    // construction and the position table differ.
    public static class AugaPanel {
        private const string PanelName = "EAQS";

        // All layout constants are in player-grid-root space, matching where the relocated
        // elements live. Tune in-game with Auga installed.
        private static readonly Vector2 panelBase = new Vector2(752, -166);
        private const float panelWidth = 255f;
        private const float panelHeight = 352f;
        private const float paperdollHeight = 157f;
        private const float tileSize = 74f;

        // Equipment diamond over the paperdoll (relative to the equipment cluster center):
        // Head top, Chest/Shoulder at the sides, Legs bottom, Utility right, Trinket left, the
        // extra utility cells continuing down the right edge.
        private static readonly Vector2 equipClusterCenter = new Vector2(110.5f, -57f);
        private static readonly Vector2[] equipPositions =
        {
            new Vector2(0f, 0f),        // Helmet
            new Vector2(-36f, -72f),    // Chest
            new Vector2(0f, -144f),     // Legs
            new Vector2(36f, -72f),     // Shoulder
            new Vector2(104f, 0f),      // Utility
            new Vector2(-104f, 0f),     // Trinket
            new Vector2(104f, -72f),    // Utility 2
            new Vector2(104f, -144f),   // Utility 3
        };

        private static GameObject _panel;

        private const int customSlotsPerColumn = 4;

        private static int ActiveQuickSlots => ValConfig.QuickSlotsEnabled.Value ? ValConfig.QuickSlotCount.Value : 0;
        private static Slot[] ActiveCustomSlots => GetCustomSlots().Where(slot => slot.IsActive).OrderBy(slot => slot.Index).ToArray();

        private static float PanelWidth => Mathf.Max(panelWidth, ActiveQuickSlots * tileSize + 20f);

        internal static Vector2 GetSlotPosition(Slot slot) {
            if (slot.IsEquipmentSlot)
                return panelBase + equipClusterCenter + equipPositions[slot.Index - EquipmentSlotStartIndex];

            if (slot.IsQuickSlot) {
                float rowStart = (PanelWidth - ActiveQuickSlots * tileSize) / 2f + 5f;
                return panelBase + new Vector2(rowStart + slot.Index * tileSize, -(paperdollHeight + 30f));
            }

            if (slot.IsCustomSlot) {
                // Columns down the right edge of the panel, packed with no gaps and wrapping once
                // a column is full — the API can register more slots than one column holds.
                int ordinal = System.Math.Max(0, System.Array.IndexOf(ActiveCustomSlots, slot));
                int row = ordinal % customSlotsPerColumn;
                int col = ordinal / customSlotsPerColumn;
                return panelBase + new Vector2(PanelWidth + 10f + col * tileSize, -(row * tileSize));
            }

            return panelBase;
        }

        // Runs from InventoryGui.Update while visible.
        internal static void UpdatePanel() {
            if (!InventoryGui.instance || !InventoryGui.instance.m_player)
                return;

            if (_panel == null) {
                _panel = Auga.API.Panel_Create(InventoryGui.instance.m_player, new Vector2(PanelWidth, panelHeight), PanelName, false);
                if (_panel == null)
                    return;

                var rt = (RectTransform)_panel.transform;
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.anchoredPosition = panelBase;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, PanelWidth);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);

                var paperdolls = Object.Instantiate(EquipmentAndQuickSlots.Paperdolls, _panel.transform, false);
                paperdolls.name = "Paperdolls";

                var divider = Auga.API.Divider_CreateSmall(_panel.transform, "Divider", PanelWidth - 40);
                ((RectTransform)divider.transform).anchoredPosition = new Vector2(0, -paperdollHeight);
            }

            _panel.SetActive(ValConfig.EquipmentSlotsEnabled.Value || ActiveQuickSlots > 0);

            UpdatePaperdollGender();
        }

        private static void UpdatePaperdollGender() {
            var player = Player.m_localPlayer;
            if (player == null || _panel == null)
                return;

            var paperdolls = _panel.transform.Find("Paperdolls");
            if (paperdolls == null)
                return;

            bool female = player.m_visEquipment != null && player.m_visEquipment.GetModelIndex() == 1;
            paperdolls.Find("Male")?.gameObject.SetActive(!female);
            paperdolls.Find("Female")?.gameObject.SetActive(female);
        }

        internal static void Clear() {
            _panel = null;
        }
    }
}
