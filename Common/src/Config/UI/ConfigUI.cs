using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Common {

    // A small widget kit for building in-game config panels out of Jotunn's GUIManager primitives.
    //
    // DEPENDENCY RULE FOR THIS WHOLE FOLDER: nothing under Common/Config/UI may reference YamlConfigFile,
    // YamlConfigManager, ConfigNetwork or ValidationReport. Its only in-repo dependency is Logger. That is
    // what lets a mod with a completely different config system -- StarLevelSystem has its own -- take
    // this folder and the shared launcher without also swallowing the yaml framework next door.
    //
    // Coordinate convention, used by everything here: top-left origin. Every rect sets
    // anchorMin = anchorMax = pivot = (0,1) and positions itself with anchoredPosition = (x, -y), so y
    // grows DOWNWARD and a column is just an increasing y. Mixing in Unity's centre-origin default is the
    // fastest way to make a panel that looks fine until someone resizes it.
    internal static class ConfigUI {
        internal const float RowHeight = 34f;
        internal const float SubRowHeight = 26f;
        internal const float RowGap = 4f;
        internal const float CloseXSize = 28f;

        // --- Input blocking -------------------------------------------------------------------------

        // Every InputField in a Valheim UI leaks keystrokes into the game -- typing a level name walks
        // your character around. GUIManager.BlockInput stops that, but it is a plain bool, so a picker
        // overlay closing on top of an editor would unblock while the editor is still open. Hence a
        // refcount.
        private static int inputBlockDepth;

        internal static void PushInputBlock() {
            // Only meaningful in-world; in the main menu there is no player to block and BlockInput(true)
            // there would fight the menu's own handling.
            if (Player.m_localPlayer == null) { return; }
            inputBlockDepth++;
            if (inputBlockDepth == 1) { GUIManager.BlockInput(true); }
        }

        internal static void PopInputBlock() {
            if (inputBlockDepth <= 0) { return; }
            inputBlockDepth--;
            if (inputBlockDepth == 0) { GUIManager.BlockInput(false); }
        }

        // Releases a block from OnDestroy rather than from a close handler. If an exception is thrown
        // between building a panel and closing it -- or the scene changes underneath it -- a close-handler
        // release never runs and the player is left unable to move with no way out but a relog.
        internal class ConfigUIInputGuard : MonoBehaviour {
            private bool held;

            internal void Hold() {
                if (held) { return; }
                held = true;
                PushInputBlock();
            }

            public void OnDestroy() {
                if (held == false) { return; }
                held = false;
                PopInputBlock();
            }
        }

        // --- Containers -----------------------------------------------------------------------------

        internal static GameObject NewUI(string name, Transform parent, params Type[] components) {
            GameObject go = new GameObject(name, components) { layer = GUIManager.UILayer };
            if (go.GetComponent<RectTransform>() == null) { go.AddComponent<RectTransform>(); }
            go.transform.SetParent(parent, false);
            return go;
        }

        internal static GameObject NewRect(string name, Transform parent, float x, float y, float w, float h) {
            GameObject go = NewUI(name, parent, typeof(RectTransform));
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
            return go;
        }

        // A row that will be placed later by LayoutColumn. It self-positions at (0,0) so a caller that
        // forgets to lay it out gets a visible pile rather than an invisible one.
        internal static GameObject NewRow(Transform parent, float width, float height) {
            return NewRect("Row", parent, 0f, 0f, width, height);
        }

        // A row for the inside of a scroll view. Jotunn's scroll content already carries a
        // VerticalLayoutGroup, which overwrites anchoredPosition -- so rows in there must size themselves
        // through a LayoutElement instead, and must NOT be positioned by LayoutColumn.
        internal static GameObject NewLayoutRow(Transform content, float width, float height) {
            GameObject row = NewUI("LayoutRow", content, typeof(RectTransform), typeof(LayoutElement));
            RectTransform rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, height);
            LayoutElement le = row.GetComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.minWidth = width;
            le.preferredWidth = width;
            return row;
        }

        // Positions each active row of a column top-to-bottom, advancing by the row's own height plus a
        // gap. Inactive rows are SKIPPED, so hiding a conditional row collapses the space it took --
        // re-run this after any SetActive to reflow.
        internal static void LayoutColumn(List<GameObject> rows, float x, float startY, float gap = RowGap) {
            float y = startY;
            foreach (GameObject row in rows) {
                if (row == null || row.activeSelf == false) { continue; }
                RectTransform rt = (RectTransform)row.transform;
                rt.anchoredPosition = new Vector2(x, -y);
                y += rt.sizeDelta.y + gap;
            }
        }

        internal static void PositionRow(GameObject row, float x, float y) {
            if (row == null) { return; }
            ((RectTransform)row.transform).anchoredPosition = new Vector2(x, -y);
        }

        // --- Panels and scroll views ----------------------------------------------------------------

        // The panel every editor sits in. Attaches the input guard, so the block is released by the
        // panel's own destruction whatever route that takes.
        internal static GameObject CreatePanel(string title, float w, float h, out Transform body) {
            GameObject panel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0f, 0f),
                width: w, height: h, draggable: true);

            panel.AddComponent<ConfigUIInputGuard>().Hold();

            AddText(panel.transform, 0f, 16f, w, RowHeight, title, 22, TextAnchor.MiddleCenter,
                GUIManager.Instance.ValheimYellow);

            body = panel.transform;
            return panel;
        }

        // Jotunn's scroll view hides its content behind a fixed child path, and the usable width is the
        // requested width minus the scrollbar and its border (handleSize 8 + 2 * border 4).
        internal static GameObject CreateScroll(Transform parent, float x, float y, float w, float h,
            out Transform content, out float contentWidth) {
            GameObject holder = NewRect("ScrollHolder", parent, x, y, w, h);
            GameObject canvas = GUIManager.Instance.CreateScrollView(
                holder.transform, false, true, 8f, 4f,
                GUIManager.Instance.ValheimScrollbarHandleColorBlock, new Color(0f, 0f, 0f, 0.5f), w, h);
            content = canvas.transform.Find("Scroll View/Viewport/Content");
            contentWidth = w - 16f;
            ScrollRect rect = canvas.GetComponentInChildren<ScrollRect>();
            if (rect != null) { rect.scrollSensitivity = 200f; }
            return canvas;
        }

        // --- Text -----------------------------------------------------------------------------------

        internal static string L(string text) {
            if (string.IsNullOrEmpty(text)) { return ""; }
            return Localization.instance != null ? Localization.instance.Localize(text) : text;
        }

        // The kit's own words: a $token, and the English to show when nothing defines it. Vanilla tokens
        // where Valheim has one, so those come translated for free; otherwise a shared $modquickconfig_*
        // token that every mod carrying this folder ships in its English file. The launcher belongs to
        // whichever copy loaded first, so its words must resolve whatever mod that is -- including one
        // with no localization at all, which would otherwise show "[modquickconfig_button]".
        internal static string L(string token, string english) {
            string text = L(token);
            // Valheim answers an unknown token with "[token]"; before Localization exists it is the token itself.
            if (text.Length == 0 || text == token || text == "[" + token.TrimStart('$') + "]") { return english; }
            return text;
        }

        internal static Text AddText(Transform parent, float x, float y, float w, float h, string text,
            int fontSize, TextAnchor anchor, Color? color = null) {
            GameObject go = GUIManager.Instance.CreateText(
                text: L(text), parent: parent,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                position: new Vector2(x, -y),
                font: GUIManager.Instance.AveriaSerifBold, fontSize: fontSize,
                color: color ?? GUIManager.Instance.ValheimBeige,
                outline: true, outlineColor: Color.black,
                width: w, height: h, addContentSizeFitter: false);

            RectTransform rt = (RectTransform)go.transform;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);

            Text component = go.GetComponent<Text>();
            component.alignment = anchor;
            component.horizontalOverflow = HorizontalWrapMode.Wrap;
            component.verticalOverflow = VerticalWrapMode.Truncate;
            return component;
        }

        internal static GameObject AddHeaderRow(Transform parent, float colWidth, string text,
            TextAnchor anchor = TextAnchor.MiddleLeft) {
            GameObject row = NewRow(parent, colWidth, RowHeight);
            AddText(row.transform, 0f, 0f, colWidth, RowHeight, text, 18, anchor, GUIManager.Instance.ValheimYellow);
            return row;
        }

        internal static GameObject AddTextRow(Transform parent, float colWidth, float height, string text,
            int fontSize, Color color, TextAnchor anchor = TextAnchor.UpperLeft) {
            GameObject row = NewRow(parent, colWidth, height);
            AddText(row.transform, 0f, 0f, colWidth, height, text, fontSize, anchor, color);
            return row;
        }

        internal static GameObject AddSpacerRow(Transform parent, float colWidth, float height) {
            return NewRow(parent, colWidth, height);
        }

        internal static GameObject AddDividerRow(Transform parent, float colWidth, float height = 12f) {
            GameObject row = NewRow(parent, colWidth, height);
            GameObject line = NewUI("Divider", row.transform, typeof(Image));
            RectTransform rt = (RectTransform)line.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(colWidth, 2f);
            rt.anchoredPosition = new Vector2(0f, -(height * 0.5f));
            Image img = line.GetComponent<Image>();
            img.color = new Color(0.6f, 0.5f, 0.35f, 0.6f);
            img.raycastTarget = false;
            return row;
        }

        // Paints a validation summary. Errors first because they are what blocks an apply.
        internal static void SetMessages(Text target, IList<string> errors, IList<string> warnings) {
            if (target == null) { return; }
            List<string> lines = new List<string>();
            if (errors != null) {
                foreach (string e in errors) { lines.Add("<color=#F87171>" + e + "</color>"); }
            }
            if (warnings != null) {
                foreach (string w in warnings) { lines.Add("<color=#FBBF24>" + w + "</color>"); }
            }
            target.text = string.Join("\n", lines.ToArray());
        }

        // --- Buttons and toggles ---------------------------------------------------------------------

        internal static GameObject AddButton(Transform parent, float x, float y, float w, string text,
            UnityAction onClick, float h = 40f) {
            GameObject go = GUIManager.Instance.CreateButton(L(text), parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), w, h);
            RectTransform rt = (RectTransform)go.transform;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            if (onClick != null) { go.GetComponent<Button>().onClick.AddListener(onClick); }
            return go;
        }

        // A dismiss button for a panel's top-right corner.
        //
        // Panel titles are centred across the full width, so the corner is free real estate -- and a close
        // control down among the content is a close control sitting on top of whatever row happens to be
        // last. Pass the panel width CreatePanel was given.
        internal static GameObject AddCloseX(Transform panel, float panelWidth, UnityAction onClick) {
            return AddButton(panel, panelWidth - CloseXSize - 16f, 12f, CloseXSize, "X", onClick, CloseXSize);
        }

        // The one place the Jotunn CreateToggle workaround lives.
        //
        // Jotunn parents the toggle with SetParent(parent) and no worldPositionStays:false, which drags
        // the parent's world scale into the child and leaves the toggle the wrong size. Re-parenting with
        // false and forcing localScale back to one is the fix; do not call CreateToggle anywhere else.
        internal static Toggle AddToggle(Transform parent, float x, float y, float size, bool value,
            Action<bool> onChange) {
            GameObject go = GUIManager.Instance.CreateToggle(parent, size, size);
            go.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)go.transform;
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);

            Toggle toggle = go.GetComponent<Toggle>();
            toggle.isOn = value;
            if (onChange != null) { toggle.onValueChanged.AddListener(b => onChange(b)); }
            return toggle;
        }

        internal static GameObject AddToggleRow(Transform parent, float colWidth, float labelW, string label,
            bool value, Action<bool> onChange, bool toggleOnLeft = false) {
            const float ToggleSize = 26f;
            const float ToggleGap = 8f;
            GameObject row = NewRow(parent, colWidth, RowHeight);
            float toggleX = toggleOnLeft ? 0f : labelW + 6f;
            float labelX = toggleOnLeft ? ToggleSize + ToggleGap : 0f;
            AddText(row.transform, labelX, 0f, labelW, RowHeight, label, 15, TextAnchor.MiddleLeft);
            AddToggle(row.transform, toggleX, 3f, ToggleSize, value, onChange);
            return row;
        }

        // --- Numbers ----------------------------------------------------------------------------------

        // Invariant, matching how typed values are parsed: under a comma-decimal culture this showed "1,50".
        internal static string Fmt(float v, bool whole) {
            return whole ? ((int)Mathf.Round(v)).ToString(CultureInfo.InvariantCulture) : v.ToString("0.00", CultureInfo.InvariantCulture);
        }

        // Jotunn has no CreateSlider, so this is hand-built out of the pieces Unity's Slider expects and
        // then handed to Jotunn's styler so it matches the rest of the game.
        internal static Slider BuildSlider(Transform parent, float x, float y, float width,
            float min, float max, float value, bool wholeNumbers) {
            GameObject go = NewUI("Slider", parent, typeof(RectTransform), typeof(Slider));
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(width, 20f);
            rt.anchoredPosition = new Vector2(x, -y);

            GameObject background = NewUI("Background", go.transform, typeof(Image));
            RectTransform bgRT = (RectTransform)background.transform;
            bgRT.anchorMin = new Vector2(0f, 0.25f);
            bgRT.anchorMax = new Vector2(1f, 0.75f);
            bgRT.sizeDelta = Vector2.zero;
            bgRT.anchoredPosition = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            GameObject fillArea = NewUI("Fill Area", go.transform, typeof(RectTransform));
            RectTransform faRT = (RectTransform)fillArea.transform;
            faRT.anchorMin = new Vector2(0f, 0.25f);
            faRT.anchorMax = new Vector2(1f, 0.75f);
            faRT.sizeDelta = new Vector2(-20f, 0f);
            faRT.anchoredPosition = Vector2.zero;

            GameObject fill = NewUI("Fill", fillArea.transform, typeof(Image));
            RectTransform fillRT = (RectTransform)fill.transform;
            fillRT.sizeDelta = new Vector2(10f, 0f);
            fill.GetComponent<Image>().color = new Color(0.7f, 0.6f, 0.4f, 0.9f);

            GameObject handleArea = NewUI("Handle Slide Area", go.transform, typeof(RectTransform));
            RectTransform haRT = (RectTransform)handleArea.transform;
            haRT.anchorMin = Vector2.zero;
            haRT.anchorMax = Vector2.one;
            haRT.sizeDelta = new Vector2(-20f, 0f);
            haRT.anchoredPosition = Vector2.zero;

            GameObject handle = NewUI("Handle", handleArea.transform, typeof(Image));
            RectTransform hRT = (RectTransform)handle.transform;
            hRT.sizeDelta = new Vector2(20f, 0f);
            Image handleImg = handle.GetComponent<Image>();
            handleImg.sprite = GUIManager.Instance.GetSprite("checkbox_marker");
            handleImg.type = Image.Type.Sliced;

            Slider slider = go.GetComponent<Slider>();
            slider.fillRect = fillRT;
            slider.handleRect = hRT;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.value = Mathf.Clamp(value, min, max);
            GUIManager.Instance.ApplySliderStyle(slider);
            return slider;
        }

        // Slider plus a typed value box, bound both ways. SetTextWithoutNotify on the reflect path is what
        // stops the two from driving each other in a loop.
        internal static GameObject AddSliderRow(Transform parent, float colWidth, float labelW, float sliderW,
            float valueW, string label, float min, float max, float value, bool wholeNumbers,
            Action<float> onChange) {
            GameObject row = NewRow(parent, colWidth, RowHeight);
            AddText(row.transform, 0f, 0f, labelW, RowHeight, label, 15, TextAnchor.MiddleLeft);
            Slider slider = BuildSlider(row.transform, labelW, 7f, sliderW, min, max, value, wholeNumbers);

            float boxX = labelW + sliderW + 10f;
            GameObject inputGO = GUIManager.Instance.CreateInputField(
                parent: row.transform,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                position: new Vector2(boxX, -3f),
                contentType: wholeNumbers ? InputField.ContentType.IntegerNumber : InputField.ContentType.DecimalNumber,
                placeholderText: null, fontSize: 15, width: valueW, height: 28f);
            RectTransform inputRT = (RectTransform)inputGO.transform;
            inputRT.pivot = new Vector2(0f, 1f);
            inputRT.anchoredPosition = new Vector2(boxX, -3f);

            InputField box = inputGO.GetComponent<InputField>();
            box.SetTextWithoutNotify(Fmt(slider.value, wholeNumbers));

            slider.onValueChanged.AddListener(v => {
                if (wholeNumbers) { v = Mathf.Round(v); }
                box.SetTextWithoutNotify(Fmt(v, wholeNumbers));
                onChange?.Invoke(v);
            });

            // Commit typed values on enter or focus loss: unparseable text snaps back to the slider, then
            // clamp, then normalise what is displayed.
            box.onEndEdit.AddListener(str => {
                // Invariant, with a comma taken as the decimal point: Unity's decimal field accepts both, and the
                // current culture read "1.5" on a German system (or "1,5" on an English one) as a grouped 15.
                if (float.TryParse(str.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) == false) { v = slider.value; }
                v = Mathf.Clamp(v, min, max);
                if (wholeNumbers) { v = Mathf.Round(v); }
                box.SetTextWithoutNotify(Fmt(v, wholeNumbers));
                if (slider.value != v) { slider.value = v; }   // the slider listener fires onChange
                else { onChange?.Invoke(v); }
            });

            return row;
        }

        // --- Free text --------------------------------------------------------------------------------

        internal static InputField AddTextField(Transform parent, float x, float y, float w, string value,
            Action<string> onCommit, InputField.ContentType contentType = InputField.ContentType.Standard,
            string placeholder = null, int charLimit = 0) {
            GameObject go = GUIManager.Instance.CreateInputField(
                parent: parent,
                anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
                position: new Vector2(x, -y),
                contentType: contentType, placeholderText: placeholder, fontSize: 15,
                width: w, height: 28f);
            RectTransform rt = (RectTransform)go.transform;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);

            InputField field = go.GetComponent<InputField>();
            if (charLimit > 0) { field.characterLimit = charLimit; }
            field.SetTextWithoutNotify(value ?? "");
            // onEndEdit rather than onValueChanged: committing per keystroke would re-validate and
            // re-serialize the whole document on every letter typed.
            if (onCommit != null) { field.onEndEdit.AddListener(s => onCommit(s)); }
            return field;
        }

        internal static GameObject AddTextFieldRow(Transform parent, float colWidth, float labelW, float fieldW,
            string label, string value, Action<string> onCommit, string placeholder = null, int charLimit = 0) {
            GameObject row = NewRow(parent, colWidth, RowHeight);
            AddText(row.transform, 0f, 0f, labelW, RowHeight, label, 15, TextAnchor.MiddleLeft);
            AddTextField(row.transform, labelW + 6f, 3f, fieldW, value, onCommit,
                InputField.ContentType.Standard, placeholder, charLimit);
            return row;
        }

        // --- Enums ------------------------------------------------------------------------------------

        // A button that cycles through the options. Deliberately not a dropdown: see the note on
        // AddPickerRow. Good up to about six members; past that use a picker.
        internal static GameObject AddEnumCycleRow(Transform parent, float colWidth, float labelW, float ctrlW,
            string label, string[] options, int currentIndex, Action<int> onChange) {
            GameObject row = NewRow(parent, colWidth, RowHeight);
            AddText(row.transform, 0f, 0f, labelW, RowHeight, label, 15, TextAnchor.MiddleLeft);

            int idx = Mathf.Clamp(currentIndex, 0, Math.Max(0, options.Length - 1));
            // The enum member name is NOT localized: it is the token an admin types into the yaml file,
            // so showing a translated form here would teach them the wrong word. The label beside it is.
            GameObject go = GUIManager.Instance.CreateButton(options.Length > 0 ? options[idx] : "", row.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(labelW + 6f, -2f), ctrlW, 28f);
            RectTransform rt = (RectTransform)go.transform;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(labelW + 6f, -2f);

            Text caption = go.GetComponentInChildren<Text>();
            go.GetComponent<Button>().onClick.AddListener(() => {
                if (options.Length == 0) { return; }
                idx = (idx + 1) % options.Length;
                caption.text = options[idx];
                onChange?.Invoke(idx);
            });
            return row;
        }

        // A small enum treated as a SET -- one toggle per member, several may be on at once.
        internal static GameObject AddEnumFlagsRow(Transform parent, float colWidth, float labelW, string label,
            string[] names, Func<int, bool> isOn, Action<int, bool> set) {
            GameObject row = NewRow(parent, colWidth, RowHeight);
            AddText(row.transform, 0f, 0f, labelW, RowHeight, label, 15, TextAnchor.MiddleLeft);

            float x = labelW + 6f;
            for (int i = 0; i < names.Length; i++) {
                int index = i;   // capture per iteration, not the shared loop variable
                AddToggle(row.transform, x, 4f, 22f, isOn(index), on => set(index, on));
                AddText(row.transform, x + 26f, 0f, 90f, RowHeight, names[index], 13, TextAnchor.MiddleLeft);
                x += 120f;
            }
            return row;
        }

        // --- Pickers and lists --------------------------------------------------------------------------

        // A value chosen from a long or open-ended list: a text box you can type into, plus a "..." button
        // that opens the filterable picker overlay.
        //
        // Not a Unity Dropdown, on purpose. Dropdown instantiates its option list as a child of its own
        // root, and every place this is needed sits inside a scroll view whose Mask clips that list --
        // the failure mode is a popup you cannot see. The picker parents to CustomGUIFront instead, so it
        // can never be clipped, and it scales to hundreds of prefab names where a dropdown would not.
        internal static GameObject AddPickerRow(Transform parent, float colWidth, float labelW, float ctrlW,
            string label, string current, Func<IList<string>> options, Action<string> onPick,
            Func<string, bool> isKnown = null) {
            GameObject row = NewRow(parent, colWidth, RowHeight);
            AddText(row.transform, 0f, 0f, labelW, RowHeight, label, 15, TextAnchor.MiddleLeft);

            float fieldW = ctrlW - 40f;
            InputField field = AddTextField(row.transform, labelW + 6f, 3f, fieldW, current, s => onPick?.Invoke(s));

            // An amber marker rather than a refusal: an admin may legitimately be naming something from a
            // mod that is not loaded right now.
            Text marker = AddText(row.transform, labelW + 6f + fieldW + 44f, 0f, 20f, RowHeight, "", 15,
                TextAnchor.MiddleLeft, new Color(0.98f, 0.75f, 0.14f));
            Action refreshMarker = () => {
                bool unknown = isKnown != null && string.IsNullOrEmpty(field.text) == false && isKnown(field.text) == false;
                marker.text = unknown ? "!" : "";
            };
            refreshMarker();
            field.onEndEdit.AddListener(_ => refreshMarker());

            AddButton(row.transform, labelW + 6f + fieldW + 4f, 3f, 34f, "...", () => {
                ConfigUIPicker.ShowPicker(label, options != null ? options() : new List<string>(), field.text, picked => {
                    field.SetTextWithoutNotify(picked);
                    refreshMarker();
                    onPick?.Invoke(picked);
                });
            }, 28f);

            return row;
        }

        // Add/remove editor for a List<string>, laid out inside a scroll view's content. Mutates the list
        // in place and calls onChanged after any structural change so the caller can rebuild its section.
        internal static GameObject AddStringListEditor(Transform content, float width, string label,
            List<string> items, Action onChanged, Func<IList<string>> options = null,
            Func<string, bool> isKnown = null) {
            GameObject header = NewLayoutRow(content, width, SubRowHeight);
            AddText(header.transform, 0f, 0f, width - 90f, SubRowHeight, label, 14, TextAnchor.MiddleLeft,
                GUIManager.Instance.ValheimOrange);
            AddButton(header.transform, width - 84f, 0f, 80f, L("$piece_smelter_add", "Add"), () => {
                items.Add("");
                onChanged?.Invoke();
            }, 24f);

            for (int i = 0; i < items.Count; i++) {
                int index = i;
                GameObject row = NewLayoutRow(content, width, SubRowHeight);

                InputField field = AddTextField(row.transform, 12f, 0f, width - 92f, items[index],
                    s => items[index] = s);

                Text marker = AddText(row.transform, width - 74f, 0f, 16f, SubRowHeight, "", 14,
                    TextAnchor.MiddleLeft, new Color(0.98f, 0.75f, 0.14f));
                Action refreshMarker = () => {
                    bool unknown = isKnown != null && string.IsNullOrEmpty(field.text) == false && isKnown(field.text) == false;
                    marker.text = unknown ? "!" : "";
                };
                refreshMarker();
                field.onEndEdit.AddListener(_ => refreshMarker());

                if (options != null) {
                    AddButton(row.transform, width - 56f, 0f, 26f, "...", () => {
                        ConfigUIPicker.ShowPicker(label, options(), field.text, picked => {
                            field.SetTextWithoutNotify(picked);
                            items[index] = picked;
                            refreshMarker();
                        });
                    }, 24f);
                }

                AddButton(row.transform, width - 26f, 0f, 24f, "x", () => {
                    items.RemoveAt(index);
                    onChanged?.Invoke();
                }, 24f);
            }

            return header;
        }
    }
}
