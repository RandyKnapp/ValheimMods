using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Common {

    // A filterable "pick one of these" overlay.
    //
    // This is what stands in for a dropdown throughout the kit. Unity's Dropdown builds its option list as
    // a child of its own root, so putting one inside a scroll view means the viewport's Mask clips the
    // list and the popup is simply invisible. This parents to CustomGUIFront instead -- above everything,
    // clipped by nothing -- and a filter box makes it usable for the several-hundred-entry lists (prefab
    // names) that a dropdown could never handle anyway.
    internal static class ConfigUIPicker {
        private const float PanelW = 420f;
        private const float PanelH = 520f;
        private const float RowH = 30f;
        private const int MaxRows = 400;

        private static GameObject overlay;

        internal static bool IsOpen {
            get { return overlay != null; }
        }

        internal static void ShowPicker(string title, IList<string> options, string current, Action<string> onPick) {
            Close();
            if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null) { return; }

            List<string> all = new List<string>();
            if (options != null) {
                foreach (string option in options) {
                    if (string.IsNullOrEmpty(option) == false) { all.Add(option); }
                }
            }
            all.Sort(StringComparer.OrdinalIgnoreCase);

            // A full-screen transparent catcher behind the panel, so a click anywhere outside dismisses
            // the picker and, more importantly, so clicks cannot fall through onto the editor beneath.
            overlay = ConfigUI.NewUI("ConfigUIPickerOverlay", GUIManager.CustomGUIFront.transform, typeof(Image));
            RectTransform overlayRT = (RectTransform)overlay.transform;
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;
            Image catcher = overlay.GetComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0.35f);
            Button catcherButton = overlay.AddComponent<Button>();
            catcherButton.transition = Selectable.Transition.None;
            catcherButton.onClick.AddListener(Close);

            // Held for the picker's own lifetime. Refcounted in ConfigUI, so closing this does not unblock
            // input while the editor underneath is still open.
            overlay.AddComponent<ConfigUI.ConfigUIInputGuard>().Hold();

            GameObject panel = GUIManager.Instance.CreateWoodpanel(
                parent: overlay.transform,
                anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f), width: PanelW, height: PanelH, draggable: true);

            ConfigUI.AddText(panel.transform, 0f, 14f, PanelW, 30f, title, 18, TextAnchor.MiddleCenter,
                GUIManager.Instance.ValheimYellow);

            InputField filter = ConfigUI.AddTextField(panel.transform, 16f, 52f, PanelW - 100f, "", null,
                InputField.ContentType.Standard, ConfigUI.L("$menu_filter", "Filter") + "...");
            ConfigUI.AddButton(panel.transform, PanelW - 76f, 52f, 60f, ConfigUI.L("$menu_close", "Close"), Close, 28f);

            ConfigUI.CreateScroll(panel.transform, 16f, 92f, PanelW - 32f, PanelH - 116f,
                out Transform content, out float contentW);
            if (content == null) { return; }

            Action<string> rebuild = null;
            rebuild = needle => {
                foreach (Transform child in content) { UnityEngine.Object.Destroy(child.gameObject); }

                int shown = 0;
                foreach (string option in all) {
                    if (string.IsNullOrEmpty(needle) == false
                        && option.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) { continue; }
                    if (shown >= MaxRows) { break; }
                    shown++;

                    string picked = option;
                    GameObject row = ConfigUI.NewLayoutRow(content, contentW, RowH);
                    ConfigUI.AddButton(row.transform, 0f, 0f, contentW, picked, () => {
                        Close();
                        onPick?.Invoke(picked);
                    }, RowH - 2f);
                }

                if (shown == 0) {
                    GameObject row = ConfigUI.NewLayoutRow(content, contentW, RowH);
                    ConfigUI.AddText(row.transform, 4f, 0f, contentW, RowH,
                        ConfigUI.L("$modquickconfig_no_matches", "No matches"), 14, TextAnchor.MiddleLeft);
                } else if (shown >= MaxRows) {
                    GameObject row = ConfigUI.NewLayoutRow(content, contentW, RowH);
                    string more = ConfigUI.L("$modquickconfig_more_matches", "... $1 more, type to narrow");
                    ConfigUI.AddText(row.transform, 4f, 0f, contentW, RowH,
                        more.Replace("$1", (all.Count - shown).ToString()), 13, TextAnchor.MiddleLeft);
                }
            };

            // Filtering on every keystroke: these lists are in-memory strings and the row cap keeps the
            // rebuild bounded, so it stays responsive even against the full prefab table.
            filter.onValueChanged.AddListener(needle => rebuild(needle));
            rebuild("");
        }

        internal static void Close() {
            if (overlay == null) { return; }
            UnityEngine.Object.Destroy(overlay);
            overlay = null;
        }
    }
}
