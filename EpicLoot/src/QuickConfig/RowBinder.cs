using EpicLoot_UnityLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EpicLoot.QuickConfig;

/// <summary>
/// Binds one instantiated page prefab to the staged configuration. A row is a direct child of the
/// page's Column / Left / Right container (or of that container's ScrollRect content, or of a Row_Group
/// in either) whose GameObject name is a registered key; the widget children are found by the names in
/// README.md. Nothing here positions anything: the column's VerticalLayoutGroup reflows when a
/// conditional row is hidden.
/// </summary>
internal sealed class RowBinder {
    private static readonly string[] ContainerNames = { "Column", "Left", "Right" };
    private static readonly HashSet<string> warned = new HashSet<string>(StringComparer.Ordinal);

    private sealed class BoundRow {
        internal Binding Binding;
        internal GameObject Root;
        internal CanvasGroup Group;
        internal Action Sync;
        internal bool ReadOnly;
    }

    private readonly GameObject page;
    private readonly StagedConfig staged;
    private readonly Action<string> onEdited;
    private readonly List<BoundRow> rows = new List<BoundRow>();

    /// <summary>True when the page holds a row the current player may only look at (JSON rows for a non-admin off-host).</summary>
    internal bool HasReadOnlyRows { get; private set; }

    /// <summary>The keys a page reset stages: every row the player may edit, and the icon index a colour row carries.</summary>
    internal List<string> ResettableKeys() {
        List<string> keys = new List<string>();
        foreach (BoundRow row in rows) {
            Binding binding = row.Binding;
            if (row.ReadOnly || binding.Kind == BindingKind.Action || binding.Kind == BindingKind.Readout) { continue; }
            keys.Add(binding.Key);
            if (binding.IconKey != null) { keys.Add(binding.IconKey); }
        }
        return keys;
    }

    internal RowBinder(GameObject page, StagedConfig staged, Action<string> onEdited) {
        this.page = page;
        this.staged = staged;
        this.onEdited = onEdited;
    }

    internal void Bind() {
        if (page == null) { return; }
        QuickConfigUi.LocalizeTexts(page);
        foreach (Transform container in RowContainers(page.transform)) {
            foreach (Transform child in ChildrenOf(container)) {
                // A Row_Group lays several rows side by side; its children are rows of the column.
                if (child.name.StartsWith(GroupPrefix, StringComparison.Ordinal)) {
                    foreach (Transform grouped in ChildrenOf(child)) { BindRow(grouped.gameObject); }
                } else {
                    BindRow(child.gameObject);
                }
            }
        }
    }

    private const string GroupPrefix = "Row_Group";

    // Snapshot first: binding a Flags row clones children into the row, not the container, but a
    // static copy is cheap insurance against reflow during iteration.
    private static List<Transform> ChildrenOf(Transform parent) {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in parent) { children.Add(child); }
        return children;
    }

    /// <summary>Re-evaluates every row's visibility, enabled state and displayed value from the staged config.</summary>
    internal void Refresh() {
        foreach (BoundRow row in rows) {
            if (row.Root == null) { continue; }
            Binding binding = row.Binding;
            bool visible = binding.IsVisible(staged)
                && (binding.Scope != BindingScope.Json || staged.IsAvailable(binding.Key));
            if (row.Root.activeSelf != visible) { row.Root.SetActive(visible); }
            if (visible == false) { continue; }

            bool enabled = row.ReadOnly == false && binding.IsEnabled(staged);
            if (row.Group != null) {
                row.Group.interactable = enabled;
                row.Group.alpha = enabled ? 1f : 0.55f;
            }
            try {
                row.Sync?.Invoke();
            } catch (Exception e) {
                EpicLoot.LogWarning($"Quick Configure could not refresh row '{binding.Key}': {e.Message}");
            }
        }
    }

    // ------------------------------------------------------------------------------------------------
    //  Finding rows
    // ------------------------------------------------------------------------------------------------

    private static IEnumerable<Transform> RowContainers(Transform pageRoot) {
        List<Transform> containers = new List<Transform>();
        foreach (string name in ContainerNames) {
            Transform column = QuickConfigUi.FindChild(pageRoot, name);
            if (column != null) { containers.Add(ContentOf(column)); }
        }
        if (containers.Count == 0) { containers.Add(pageRoot); }
        return containers;
    }

    // A column that scrolls holds its rows in the ScrollRect's content. Only the column itself or a
    // direct child is checked: a list row further down carries a ScrollRect of its own.
    private static Transform ContentOf(Transform column) {
        ScrollRect scroll = column.GetComponent<ScrollRect>();
        if (scroll == null) {
            foreach (Transform child in column) {
                scroll = child.GetComponent<ScrollRect>();
                if (scroll != null) { break; }
            }
        }
        return scroll != null && scroll.content != null ? scroll.content : column;
    }

    private void BindRow(GameObject root) {
        string key = root.name;
        Binding binding = QuickConfigBindings.Get(key);
        if (binding == null) {
            // Decorative rows keep their template name and are simply left alone.
            if (string.IsNullOrEmpty(key) || key.StartsWith("Row_", StringComparison.Ordinal)) { return; }
            if (warned.Add(key)) {
                EpicLoot.LogWarningForce($"Quick Configure page '{page.name}' has a row named '{key}' that matches no setting; the row is disabled.");
            }
            Disable(root);
            return;
        }

        TMP_Text label = QuickConfigUi.TextAt(root.transform, "Label");
        // An action row's button already carries the display name; its Label is side text that may
        // legitimately be empty, so only value rows get a fallback label.
        if (label != null && binding.Kind != BindingKind.Action && string.IsNullOrWhiteSpace(label.text)
            && string.IsNullOrEmpty(binding.DisplayName) == false) {
            label.text = QuickConfigureTool.L(binding.DisplayName);
        }
        AttachTooltip(root, binding);

        BoundRow row = new BoundRow {
            Binding = binding,
            Root = root,
            Group = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>(),
            ReadOnly = binding.EditsFiles && QuickConfigBindings.CanEditFiles() == false
        };

        bool wired;
        try {
            wired = Wire(row);
        } catch (Exception e) {
            EpicLoot.LogWarningForce($"Quick Configure could not bind row '{key}': {e}");
            wired = false;
        }
        if (wired == false) {
            Disable(root);
            return;
        }
        if (row.ReadOnly) { HasReadOnlyRows = true; }
        rows.Add(row);
    }

    // List rows carry the tooltip on their Head (the label strip) so it does not pop over every
    // field in the list; a plain row carries it on its root. An inactive child named Tooltip with a
    // TMP_Text overrides the binding's text.
    private static void AttachTooltip(GameObject root, Binding binding) {
        string text = TooltipOverride(root.transform) ?? binding.TooltipText();
        GameObject target = root;
        if (IsListKind(binding.Kind)) {
            Transform head = QuickConfigUi.FindChild(root.transform, "Head");
            if (head != null) { target = head.gameObject; }
        }
        QuickConfigTooltip.Attach(target, text);
    }

    private static bool IsListKind(BindingKind kind) {
        return kind == BindingKind.BiomeCosts || kind == BindingKind.ItemCategories || kind == BindingKind.BiomeDrops
            || kind == BindingKind.EffectConfigs || kind == BindingKind.Bounties || kind == BindingKind.RarityCounts
            || kind == BindingKind.UpgradeCosts;
    }

    private static string TooltipOverride(Transform root) {
        TMP_Text text = QuickConfigUi.TextAt(root, "Tooltip");
        if (text == null || string.IsNullOrWhiteSpace(text.text)) { return null; }
        return QuickConfigureTool.L(text.text);
    }

    private static void Disable(GameObject root) {
        CanvasGroup group = root.GetComponent<CanvasGroup>() ?? root.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.alpha = 0.4f;
    }

    private bool Wire(BoundRow row) {
        switch (row.Binding.Kind) {
            case BindingKind.Bool: return WireToggle(row);
            case BindingKind.Float:
            case BindingKind.Int: return WireNumber(row);
            case BindingKind.Enum:
            case BindingKind.Key:
            case BindingKind.String: return WireChoice(row);
            case BindingKind.Flags: return WireFlags(row);
            case BindingKind.Color: return WireColor(row);
            case BindingKind.RarityCounts: return WireRarityCounts(row);
            case BindingKind.BiomeCosts: return WireBiomeCosts(row);
            case BindingKind.ItemCategories: return WireItemCategories(row);
            case BindingKind.BiomeDrops: return WireBiomeDrops(row);
            case BindingKind.EffectConfigs: return WireEffectConfigs(row);
            case BindingKind.Bounties: return WireBounties(row);
            case BindingKind.UpgradeCosts: return WireUpgradeCosts(row);
            case BindingKind.Action: return WireButton(row);
            case BindingKind.Readout: return WireReadout(row);
            default: return false;
        }
    }

    private bool MissingWidget(BoundRow row, string widget) {
        if (warned.Add(row.Binding.Key + "/" + widget)) {
            EpicLoot.LogWarningForce($"Quick Configure row '{row.Binding.Key}' has no '{widget}' child; the row is disabled.");
        }
        return false;
    }

    private void Edited(string message) {
        onEdited?.Invoke(message);
    }

    // ------------------------------------------------------------------------------------------------
    //  Simple widgets
    // ------------------------------------------------------------------------------------------------

    private bool WireToggle(BoundRow row) {
        Toggle toggle = QuickConfigUi.FindComponent<Toggle>(row.Root.transform, "Toggle");
        if (toggle == null) { return MissingWidget(row, "Toggle"); }
        Binding binding = row.Binding;
        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(value => {
            binding.Set(staged, value);
            Edited(null);
        });
        row.Sync = () => toggle.SetIsOnWithoutNotify(binding.Get(staged) is bool on && on);
        return true;
    }

    private bool WireNumber(BoundRow row) {
        Binding binding = row.Binding;
        Slider slider = QuickConfigUi.FindComponent<Slider>(row.Root.transform, "Slider");
        TMP_InputField value = QuickConfigUi.FindComponent<TMP_InputField>(row.Root.transform, "Value")
            ?? QuickConfigUi.FindComponent<TMP_InputField>(row.Root.transform, "Field");
        if (slider == null && value == null) { return MissingWidget(row, "Slider"); }

        bool whole = binding.Kind == BindingKind.Int
            || (binding.Step >= 1f && Math.Abs(binding.Step - Math.Round(binding.Step)) < 0.0001f);
        if (slider != null) {
            // Max first: setting a min above the old max would clamp the value for a frame.
            slider.maxValue = binding.Max;
            slider.minValue = binding.Min;
            slider.wholeNumbers = whole;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v => {
                binding.Set(staged, v);
                Edited(null);
            });
        }
        if (value != null) {
            value.contentType = whole ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.DecimalNumber;
            value.onEndEdit.RemoveAllListeners();
            value.onEndEdit.AddListener(text => {
                binding.Set(staged, text);
                Edited(null);
            });
        }
        row.Sync = () => {
            float current = Current(binding);
            if (slider != null) { slider.SetValueWithoutNotify(current); }
            if (value != null && value.isFocused == false) { value.SetTextWithoutNotify(Format(binding, current)); }
        };
        return true;
    }

    private float Current(Binding binding) {
        object raw = binding.Get(staged);
        return raw is int i ? i : QuickConfigBindings.ToFloat(raw, binding.Min);
    }

    private static string Format(Binding binding, float value) {
        return binding.Kind == BindingKind.Int
            ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
            : QuickConfigBindings.FormatFloat(value, binding.Step);
    }

    // Enum / key / restricted string: a Cycle button, or a Field with a Pick button; whichever the
    // template carries. Both may be present (Row_Color reuses this for its Field/Pick).
    private bool WireChoice(BoundRow row) {
        Binding binding = row.Binding;
        Button cycle = QuickConfigUi.FindComponent<Button>(row.Root.transform, "Cycle");
        bool any = false;
        if (cycle != null) {
            WireCycle(row, cycle, binding);
            any = true;
        }
        if (WireFieldAndPick(row, binding)) { any = true; }
        return any || MissingWidget(row, "Cycle/Field");
    }

    private void WireCycle(BoundRow row, Button cycle, Binding binding) {
        TMP_Text caption = QuickConfigUi.CaptionOf(cycle);
        QuickConfigUi.Wire(cycle, () => {
            IList<string> options = binding.Options?.Invoke();
            if (options == null || options.Count == 0) { return; }
            int index = OptionIndex(binding, options);
            SetOption(binding, options, (index + 1) % options.Count);
            Edited(null);
        });
        row.Sync += () => {
            if (caption == null) { return; }
            IList<string> options = binding.Options?.Invoke();
            int index = options != null ? OptionIndex(binding, options) : -1;
            string text = index >= 0 && options != null ? options[index] : binding.Get(staged)?.ToString() ?? "";
            caption.text = QuickConfigureTool.L(text);
        };
    }

    // For an Int binding with options (the icon index) the value IS the index; otherwise the value is the option text.
    private int OptionIndex(Binding binding, IList<string> options) {
        if (binding.Kind == BindingKind.Int) {
            int index = QuickConfigBindings.ToInt(binding.Get(staged), 0);
            return Mathf.Clamp(index, 0, options.Count - 1);
        }
        string current = binding.Get(staged) as string ?? "";
        for (int i = 0; i < options.Count; i++) {
            if (string.Equals(options[i], current, StringComparison.OrdinalIgnoreCase)) { return i; }
        }
        return -1;
    }

    private void SetOption(Binding binding, IList<string> options, int index) {
        if (binding.Kind == BindingKind.Int) {
            binding.Set(staged, index);
        } else {
            binding.Set(staged, options[index]);
        }
    }

    private bool WireFieldAndPick(BoundRow row, Binding binding) {
        TMP_InputField field = QuickConfigUi.FindComponent<TMP_InputField>(row.Root.transform, "Field");
        if (field == null) { return false; }
        Button pick = QuickConfigUi.FindComponent<Button>(row.Root.transform, "Pick");
        TMP_Text status = QuickConfigUi.TextAt(row.Root.transform, "Status");

        field.onEndEdit.RemoveAllListeners();
        field.onEndEdit.AddListener(text => {
            binding.Set(staged, text);
            Edited(null);
        });

        if (pick != null) {
            BindPicker(pick, binding.Options, binding.DisplayName, () => binding.Get(staged) as string, picked => {
                binding.Set(staged, picked);
                Edited(null);
            });
        }

        row.Sync += () => {
            if (field.isFocused == false) { field.SetTextWithoutNotify(binding.Get(staged) as string ?? ""); }
            if (status != null && binding.Validate != null) {
                string error = binding.Validate(staged);
                status.text = error ?? "";
            }
        };
        return true;
    }

    private bool WireTextField(BoundRow row) {
        return WireFieldAndPick(row, row.Binding) || MissingWidget(row, "Field");
    }

    private bool WireFlags(BoundRow row) {
        Binding binding = row.Binding;
        Transform flags = QuickConfigUi.FindChild(row.Root.transform, "Flags");
        Transform template = flags != null ? QuickConfigUi.FindChild(flags, "Flag") : null;
        if (template == null) { return MissingWidget(row, "Flags/Flag"); }
        template.gameObject.SetActive(false);

        IList<string> members = binding.Options?.Invoke() ?? new List<string>();
        List<(string Member, Toggle Toggle)> toggles = new List<(string, Toggle)>();
        foreach (string member in members) {
            GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, template.parent, false);
            clone.name = member;
            clone.SetActive(true);
            TMP_Text label = QuickConfigUi.TextAt(clone.transform, "Label") ?? clone.GetComponentInChildren<TMP_Text>(true);
            if (label != null) { label.text = member; }
            Toggle toggle = clone.GetComponent<Toggle>() ?? clone.GetComponentInChildren<Toggle>(true);
            if (toggle == null) { continue; }
            string name = member;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(on => {
                HashSet<string> set = binding.Get(staged) as HashSet<string> ?? new HashSet<string>(StringComparer.Ordinal);
                if (on) { set.Add(name); } else { set.Remove(name); }
                binding.Set(staged, set);
                Edited(null);
            });
            toggles.Add((name, toggle));
        }

        row.Sync = () => {
            HashSet<string> set = binding.Get(staged) as HashSet<string> ?? new HashSet<string>(StringComparer.Ordinal);
            foreach ((string member, Toggle toggle) in toggles) {
                toggle.SetIsOnWithoutNotify(set.Contains(member));
            }
        };
        return true;
    }

    private bool WireColor(BoundRow row) {
        Binding binding = row.Binding;
        Image swatch = QuickConfigUi.FindComponent<Image>(row.Root.transform, "Swatch");
        bool fieldWired = WireFieldAndPick(row, binding);
        if (fieldWired == false && swatch == null) { return MissingWidget(row, "Field/Swatch"); }

        Button cycle = QuickConfigUi.FindComponent<Button>(row.Root.transform, "Cycle");
        Binding icon = binding.IconKey != null ? QuickConfigBindings.Get(binding.IconKey) : null;
        if (cycle != null) {
            if (icon != null) {
                WireCycle(row, cycle, icon);
            } else {
                // The set-item colour has no icon index. The button keeps its place, unseen, so the
                // swatch, field and picker line up with the rarity rows above.
                CanvasGroup hidden = cycle.gameObject.GetComponent<CanvasGroup>() ?? cycle.gameObject.AddComponent<CanvasGroup>();
                hidden.alpha = 0f;
                hidden.interactable = false;
                hidden.blocksRaycasts = false;
            }
        }

        row.Sync += () => {
            if (swatch == null) { return; }
            Color? color = QuickConfigBindings.ParseColor(binding.Get(staged) as string);
            swatch.color = color ?? new Color(0.35f, 0.35f, 0.35f, 1f);
        };
        return true;
    }

    private bool WireButton(BoundRow row) {
        Binding binding = row.Binding;
        Button button = QuickConfigUi.FindComponent<Button>(row.Root.transform, "Button")
            ?? row.Root.GetComponent<Button>()
            ?? row.Root.GetComponentInChildren<Button>(true);
        if (button == null) { return MissingWidget(row, "Button"); }
        TMP_Text caption = QuickConfigUi.CaptionOf(button);
        if (caption != null && string.IsNullOrWhiteSpace(caption.text)) { caption.text = QuickConfigureTool.L(binding.DisplayName); }
        QuickConfigUi.Wire(button, () => {
            string message = null;
            try {
                message = binding.Act?.Invoke(staged);
            } catch (Exception e) {
                EpicLoot.LogWarningForce($"Quick Configure action '{binding.Key}' failed: {e}");
                message = e.Message;
            }
            Edited(message);
        });
        return true;
    }

    private bool WireReadout(BoundRow row) {
        Binding binding = row.Binding;
        TMP_Text label = QuickConfigUi.TextAt(row.Root.transform, "Label") ?? row.Root.GetComponentInChildren<TMP_Text>(true);
        if (label == null) { return MissingWidget(row, "Label"); }
        row.Sync = () => {
            try { label.text = binding.Readout?.Invoke(staged) ?? ""; } catch (Exception e) { label.text = e.Message; }
        };
        return true;
    }

    // ------------------------------------------------------------------------------------------------
    //  List rows: one Item template under Items (a ScrollRect), cloned per entry
    // ------------------------------------------------------------------------------------------------

    // Clones an inactive template once per entry and rebuilds only when the entries changed: a slider
    // elsewhere on the panel refreshes every row, and rebuilding a list of input fields on every tick
    // would eat a field being typed into.
    private sealed class ClonedList {
        private readonly Transform template;
        private readonly List<GameObject> clones = new List<GameObject>();
        private string built;

        internal ClonedList(Transform template) {
            this.template = template;
            template.gameObject.SetActive(false);
        }

        /// <summary>True when the clones were rebuilt (the signature changed and nothing inside had focus).</summary>
        internal bool Rebuild<T>(IList<T> entries, string signature, Action<Transform, T> bind, GameObject styleRoot) {
            if (signature == built) { return false; }
            // Committing one field (onEndEdit) changes the signature while the field the player clicked
            // into next has just taken focus; destroying the clones now would take that focus with it.
            // The clones already show what was typed, so the rebuild waits for the next refresh.
            if (FocusIsInside(template.parent)) { return false; }
            built = signature;
            foreach (GameObject clone in clones) {
                if (clone != null) { UnityEngine.Object.Destroy(clone); }
            }
            clones.Clear();
            foreach (T entry in entries) {
                GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, template.parent, false);
                clone.name = template.name;
                clone.SetActive(true);
                clones.Add(clone);
                bind(clone.transform, entry);
            }
            QuickConfigStyle.Apply(styleRoot);
            return true;
        }
    }

    // Only a text box inside the list counts. A button pressed in it (+, Remove) is selected too, and
    // treating that as focus deferred the very rebuild that shows what the button just added or removed.
    private static bool FocusIsInside(Transform root) {
        GameObject selected = UnityEngine.EventSystems.EventSystem.current != null
            ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject
            : null;
        return root != null && selected != null && selected.transform.IsChildOf(root)
            && selected.GetComponent<TMP_InputField>() != null;
    }

    // The Item template under the row's Items ScrollRect content, or null (and a logged warning).
    private Transform ItemTemplate(BoundRow row) {
        ScrollRect items = QuickConfigUi.FindComponent<ScrollRect>(row.Root.transform, "Items");
        Transform content = items != null && items.content != null ? items.content : QuickConfigUi.FindChild(row.Root.transform, "Items");
        Transform template = content != null ? QuickConfigUi.FindChild(content, "Item") : null;
        if (template == null) { MissingWidget(row, "Items/Item"); }
        return template;
    }

    private static Transform HeadOf(BoundRow row) {
        return QuickConfigUi.FindChild(row.Root.transform, "Head") ?? row.Root.transform;
    }

    private static TMP_InputField BindField(Transform parent, string name, string value, TMP_InputField.ContentType type, Action<string> commit) {
        TMP_InputField field = QuickConfigUi.FindComponent<TMP_InputField>(parent, name);
        if (field == null) { return null; }
        field.contentType = type;
        field.SetTextWithoutNotify(value ?? "");
        field.onEndEdit.RemoveAllListeners();
        field.onEndEdit.AddListener(text => commit(text ?? ""));
        return field;
    }

    private static Button BindButton(Transform parent, string name, UnityAction onClick) {
        Button button = QuickConfigUi.FindComponent<Button>(parent, name);
        if (button != null) { QuickConfigUi.Wire(button, onClick); }
        return button;
    }

    // A picker button over an option list that may not exist (the item list exists only in a world);
    // a hidden button is clearer than one that opens an empty picker. Availability is decided once.
    private static void BindPicker(Button pick, Func<IList<string>> options, string title, Func<string> current, Action<string> picked) {
        if (pick == null) { return; }
        bool hasOptions = options != null && options() != null;
        pick.gameObject.SetActive(hasOptions);
        if (hasOptions) {
            QuickConfigUi.Wire(pick, () => QuickConfigPicker.Show(title, options(), current(), picked));
        }
    }

    private static string Caption(Button button, string text) {
        TMP_Text caption = QuickConfigUi.CaptionOf(button);
        if (caption != null) { caption.text = text ?? ""; }
        return text;
    }

    // Effect configs: every loaded effect's Config block; the shown effect is UI state on the row.
    // Keys are fixed (read-only Key field, no Add/Remove) except for open-key effects (Riches).
    private bool WireEffectConfigs(BoundRow row) {
        Binding binding = row.Binding;
        Transform template = ItemTemplate(row);
        if (template == null) { return false; }
        ClonedList list = new ClonedList(template);
        Transform head = HeadOf(row);
        Button effectButton = QuickConfigUi.FindComponent<Button>(head, "Effect") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Effect");
        Button add = QuickConfigUi.FindComponent<Button>(head, "Add") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Add");
        string selected = null;

        EffectConfigsValue Value() => binding.Get(staged) as EffectConfigsValue ?? new EffectConfigsValue();

        void Commit(EffectConfigsValue value) {
            binding.Set(staged, value);
            Edited(null);
        }

        if (effectButton != null) {
            QuickConfigUi.Wire(effectButton, () => {
                List<EffectConfigSet> sets = Value().PickerOrder();
                List<string> names = sets.Select(set => set.PickerName).ToList();
                string current = Value().Get(selected)?.PickerName;
                QuickConfigPicker.Show(binding.DisplayName, names, current, picked => {
                    int index = names.IndexOf(picked);
                    if (index >= 0) { selected = sets[index].Type; }
                    row.Sync?.Invoke();
                    EffectConfigSet chosen = Value().Get(selected);
                    if (chosen != null && chosen.Source == EffectSource.ReadOnly) {
                        Edited($"{chosen.DisplayName} is read-only: it is registered by another mod or synthesized in code, so its values live outside these files.");
                    }
                });
            });
        }
        if (add != null) {
            QuickConfigUi.Wire(add, () => {
                EffectConfigsValue value = Value();
                EffectConfigSet set = value.Get(selected);
                if (set == null || set.OpenKeys == false || set.Source == EffectSource.ReadOnly) { return; }
                set.Entries.Add(new EffectConfigEntry { Key = "", Value = 1f });
                Commit(value);
            });
        }

        row.Sync = () => {
            EffectConfigsValue value = Value();
            if (selected == null || value.Get(selected) == null) { selected = value.PickerOrder().FirstOrDefault()?.Type; }
            EffectConfigSet set = value.Get(selected);
            Caption(effectButton, set?.PickerName ?? "");
            bool editable = set != null && set.Source != EffectSource.ReadOnly;
            bool open = editable && set.OpenKeys;
            if (add != null) { add.gameObject.SetActive(open); }
            List<EffectConfigEntry> entries = set?.Entries ?? new List<EffectConfigEntry>();
            string signature = $"{selected}|{editable}|{open}|" + string.Join("|", entries.Select(entry => $"{entry.Key}={entry.Value.ToString("R", CultureInfo.InvariantCulture)}"));
            list.Rebuild(entries, signature, (clone, entry) => {
                QuickConfigTooltip.Attach(clone.gameObject, QuickConfigTooltip.Text(entry.Key, EffectConfigTables.KeyLabel(selected, entry.Key)));
                TMP_InputField keyField = BindField(clone, "Key", entry.Key, TMP_InputField.ContentType.Standard, text => {
                    entry.Key = text.Trim();
                    Commit(Value());
                });
                if (keyField != null) { keyField.interactable = open; }
                TMP_InputField valueField = BindField(clone, "Value", entry.Value.ToString("0.####", CultureInfo.InvariantCulture), TMP_InputField.ContentType.Standard, text => {
                    if (float.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)) { entry.Value = parsed; }
                    Commit(Value());
                });
                if (valueField != null) { valueField.interactable = editable; }
                BindPicker(QuickConfigUi.FindComponent<Button>(clone, "Pick"), open ? binding.Options : null, binding.DisplayName, () => entry.Key, picked => {
                    entry.Key = picked;
                    Commit(Value());
                });
                Button remove = QuickConfigUi.FindComponent<Button>(clone, "Remove");
                if (remove != null) {
                    remove.gameObject.SetActive(open);
                    if (open) {
                        QuickConfigUi.Wire(remove, () => {
                            EffectConfigsValue current = Value();
                            current.Get(selected)?.Entries.Remove(entry);
                            Commit(current);
                        });
                    }
                }
            }, row.Root);
        };
        return true;
    }

    // Bounty targets: every biome is staged; the shown biome is UI state on the row.
    private bool WireBounties(BoundRow row) {
        Binding binding = row.Binding;
        Transform template = ItemTemplate(row);
        if (template == null) { return false; }
        ClonedList list = new ClonedList(template);
        Transform head = HeadOf(row);
        Button biomeButton = QuickConfigUi.FindComponent<Button>(head, "Biome") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Biome");
        Button add = QuickConfigUi.FindComponent<Button>(head, "Add") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Add");
        string selected = null;

        BountiesValue Value() => binding.Get(staged) as BountiesValue ?? new BountiesValue();

        void Commit(BountiesValue value) {
            binding.Set(staged, value);
            Edited(null);
        }

        if (biomeButton != null) {
            QuickConfigUi.Wire(biomeButton, () => {
                List<string> biomes = Value().BiomeOrder.ToList();
                List<string> display = biomes.Select(QuickConfigBindings.BiomeDisplayName).ToList();
                QuickConfigPicker.Show(binding.DisplayName, display, QuickConfigBindings.BiomeDisplayName(selected), picked => {
                    int index = display.IndexOf(picked);
                    if (index >= 0) { selected = biomes[index]; }
                    row.Sync?.Invoke();
                });
            });
        }
        if (add != null) {
            QuickConfigUi.Wire(add, () => {
                BountiesValue value = Value();
                List<BountyEntry> entries = value.Get(selected);
                if (entries == null) { return; }
                entries.Add(new BountyEntry { TargetID = "" });
                Commit(value);
            });
        }

        row.Sync = () => {
            BountiesValue value = Value();
            if (selected == null || value.Get(selected) == null) { selected = value.BiomeOrder.FirstOrDefault(); }
            Caption(biomeButton, QuickConfigBindings.BiomeDisplayName(selected));
            List<BountyEntry> entries = value.Get(selected) ?? new List<BountyEntry>();
            string signature = selected + "|" + string.Join("|", entries.Select(entry => $"{entry.TargetID}:{entry.RewardIron}:{entry.RewardGold}:{entry.RewardCoins}"));
            list.Rebuild(entries, signature, (clone, entry) => {
                BindField(clone, "Target", entry.TargetID, TMP_InputField.ContentType.Standard, text => {
                    entry.TargetID = text.Trim();
                    Commit(Value());
                });
                BindPicker(QuickConfigUi.FindComponent<Button>(clone, "Pick"), binding.Options, binding.DisplayName, () => entry.TargetID, picked => {
                    entry.TargetID = picked;
                    Commit(Value());
                });
                BindField(clone, "Iron", entry.RewardIron.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.IntegerNumber, text => {
                    entry.RewardIron = Mathf.Max(0, QuickConfigBindings.ToInt(text, entry.RewardIron));
                    Commit(Value());
                });
                BindField(clone, "Gold", entry.RewardGold.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.IntegerNumber, text => {
                    entry.RewardGold = Mathf.Max(0, QuickConfigBindings.ToInt(text, entry.RewardGold));
                    Commit(Value());
                });
                BindField(clone, "Coins", entry.RewardCoins.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.IntegerNumber, text => {
                    entry.RewardCoins = Mathf.Max(0, QuickConfigBindings.ToInt(text, entry.RewardCoins));
                    Commit(Value());
                });
                BindButton(clone, "Remove", () => {
                    BountiesValue current = Value();
                    current.Get(selected)?.Remove(entry);
                    Commit(current);
                });
            }, row.Root);
        };
        return true;
    }

    // Treasure map costs: one fixed row per biome of the file (no Add/Remove), coins and forest tokens.
    private bool WireBiomeCosts(BoundRow row) {
        Binding binding = row.Binding;
        Transform template = ItemTemplate(row);
        if (template == null) { return false; }
        ClonedList list = new ClonedList(template);

        List<BiomeCostEntry> Entries() => binding.Get(staged) as List<BiomeCostEntry> ?? new List<BiomeCostEntry>();

        void Commit() {
            binding.Set(staged, Entries());
            Edited(null);
        }

        row.Sync = () => {
            List<BiomeCostEntry> entries = Entries();
            string signature = string.Join("|", entries.Select(entry => $"{entry.Biome}:{entry.Cost}:{entry.ForestTokens}"));
            list.Rebuild(entries, signature, (clone, entry) => {
                TMP_Text name = QuickConfigUi.TextAt(clone, "Name");
                if (name != null) { name.text = QuickConfigBindings.BiomeDisplayName(entry.Biome); }
                BindField(clone, "Cost", entry.Cost.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.IntegerNumber, text => {
                    entry.Cost = Mathf.Max(-1, QuickConfigBindings.ToInt(text, entry.Cost));
                    Commit();
                });
                BindField(clone, "Tokens", entry.ForestTokens.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.IntegerNumber, text => {
                    entry.ForestTokens = Mathf.Max(0, QuickConfigBindings.ToInt(text, entry.ForestTokens));
                    Commit();
                });
            }, row.Root);
        };
        return true;
    }

    // Item categories: every category is staged; the one on show is UI state on the row.
    private bool WireItemCategories(BoundRow row) {
        Binding binding = row.Binding;
        Transform template = ItemTemplate(row);
        if (template == null) { return false; }
        ClonedList list = new ClonedList(template);
        Transform head = HeadOf(row);
        Button category = QuickConfigUi.FindComponent<Button>(head, "Category") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Category");
        Button add = QuickConfigUi.FindComponent<Button>(head, "Add") ?? QuickConfigUi.FindComponent<Button>(row.Root.transform, "Add");
        string selected = null;

        ItemCategoriesValue Value() => binding.Get(staged) as ItemCategoriesValue ?? new ItemCategoriesValue();

        void Commit(ItemCategoriesValue value) {
            binding.Set(staged, value);
            Edited(null);
        }

        if (category != null) {
            QuickConfigUi.Wire(category, () => {
                ItemCategoriesValue value = Value();
                QuickConfigPicker.Show(binding.DisplayName, value.Categories, selected, picked => {
                    selected = picked;
                    row.Sync?.Invoke();
                });
            });
        }
        if (add != null) {
            QuickConfigUi.Wire(add, () => {
                ItemCategoriesValue value = Value();
                List<ItemByBossEntry> entries = value.Get(selected);
                if (entries == null) { return; }
                entries.Add(new ItemByBossEntry { Boss = "none", Item = "" });
                Commit(value);
            });
        }

        row.Sync = () => {
            ItemCategoriesValue value = Value();
            if (selected == null || value.Entries.ContainsKey(selected) == false) {
                selected = value.Categories.Count > 0 ? value.Categories[0] : null;
            }
            Caption(category, selected ?? "");
            List<ItemByBossEntry> entries = value.Get(selected) ?? new List<ItemByBossEntry>();
            string signature = selected + "|" + string.Join("|", entries.Select(entry => $"{entry.Boss}:{entry.Item}"));
            list.Rebuild(entries, signature, (clone, entry) => {
                Button boss = QuickConfigUi.FindComponent<Button>(clone, "Boss");
                Caption(boss, entry.Boss);
                if (boss != null) {
                    QuickConfigUi.Wire(boss, () => QuickConfigPicker.Show("Boss key",
                        binding.KeyOptions?.Invoke(staged) ?? new List<string> { "none" }, entry.Boss, picked => {
                            entry.Boss = picked;
                            Commit(Value());
                        }));
                }
                BindField(clone, "Field", entry.Item, TMP_InputField.ContentType.Standard, text => {
                    entry.Item = text.Trim();
                    Commit(Value());
                });
                BindPicker(QuickConfigUi.FindComponent<Button>(clone, "Pick"), binding.Options, binding.DisplayName, () => entry.Item, picked => {
                    entry.Item = picked;
                    Commit(Value());
                });
                BindButton(clone, "Remove", () => {
                    ItemCategoriesValue current = Value();
                    current.Get(selected)?.Remove(entry);
                    Commit(current);
                });
            }, row.Root);
        };
        return true;
    }

    // ------------------------------------------------------------------------------------------------
    //  Weight lists: a count (or fixed key) and a 0-100 slider per entry, meant to add up to 100
    // ------------------------------------------------------------------------------------------------

    // One WeightList inside a row (see README): Head -> Label, Total, Add; Items -> Item (Count or Name,
    // Slider, Value, Remove). The clones are rebuilt only when the entries' structure changes (a count
    // edited, an entry added or removed, another table shown); weights are pushed into the widgets
    // already there, so the slider being dragged is never destroyed under the pointer.
    private sealed class WeightListView {
        // What + gives a new count; Save then trims the others to make room.
        private const float NewEntryWeight = 10f;

        private readonly ClonedList list;
        private readonly Button add;
        private readonly TMP_Text total;
        private readonly GameObject styleRoot;
        private List<Action> refreshers = new List<Action>();
        private List<WeightEntry> entries;
        private int minCount;
        private int maxCount;
        private Action commit;

        private WeightListView(Transform container, Transform template, GameObject styleRoot) {
            list = new ClonedList(template);
            this.styleRoot = styleRoot;
            Transform head = QuickConfigUi.FindChild(container, "Head") ?? container;
            add = QuickConfigUi.FindComponent<Button>(head, "Add");
            total = QuickConfigUi.TextAt(head, "Total");
            if (add != null) { QuickConfigUi.Wire(add, OnAdd); }
        }

        /// <summary>The list in the row's child of that name, or null when there is none or it has no Item template.</summary>
        internal static WeightListView Find(Transform row, string name) {
            Transform container = QuickConfigUi.FindChild(row, name);
            if (container == null) { return null; }
            ScrollRect items = QuickConfigUi.FindComponent<ScrollRect>(container, "Items");
            Transform content = items != null && items.content != null ? items.content : QuickConfigUi.FindChild(container, "Items");
            Transform template = content != null ? QuickConfigUi.FindChild(content, "Item") : null;
            return template != null ? new WeightListView(container, template, row.gameObject) : null;
        }

        /// <summary>
        /// Shows <paramref name="shown"/>, edited in place. <paramref name="keyNames"/> names the entries of a
        /// fixed-key table by index (no Add or Remove); null for an open table whose counts run from
        /// <paramref name="min"/> to <paramref name="max"/>. <paramref name="onCommit"/> runs after every edit.
        /// </summary>
        internal void Show(List<WeightEntry> shown, IList<string> keyNames, int min, int max, Action onCommit) {
            entries = shown ?? new List<WeightEntry>();
            minCount = min;
            maxCount = max;
            commit = onCommit;
            if (add != null) { add.gameObject.SetActive(keyNames == null); }

            List<WeightEntry> owner = entries;
            string signature = RuntimeHelpers.GetHashCode(owner) + "|" + string.Join(",", owner.Select(entry => entry.Count));
            List<Action> built = new List<Action>();
            if (list.Rebuild(owner, signature, (clone, entry) => built.Add(BindItem(clone, entry, owner, keyNames)), styleRoot)) {
                refreshers = built;
            }
            foreach (Action refresh in refreshers) { refresh(); }
            if (total != null) { total.text = WeightTable.TotalWarning(owner) ?? ""; }
        }

        private Action BindItem(Transform clone, WeightEntry entry, List<WeightEntry> owner, IList<string> keyNames) {
            bool open = keyNames == null;
            TMP_Text name = QuickConfigUi.TextAt(clone, "Name");
            TMP_InputField count = QuickConfigUi.FindComponent<TMP_InputField>(clone, "Count");
            Slider slider = QuickConfigUi.FindComponent<Slider>(clone, "Slider");
            TMP_InputField value = QuickConfigUi.FindComponent<TMP_InputField>(clone, "Value");
            Button remove = QuickConfigUi.FindComponent<Button>(clone, "Remove");

            if (name != null) {
                name.gameObject.SetActive(open == false);
                name.text = open == false && entry.Count >= 0 && entry.Count < keyNames.Count ? keyNames[entry.Count] : "";
            }
            if (count != null) {
                count.gameObject.SetActive(open);
                count.contentType = TMP_InputField.ContentType.IntegerNumber;
                count.onEndEdit.RemoveAllListeners();
                count.onEndEdit.AddListener(text => {
                    entry.Count = Mathf.Clamp(QuickConfigBindings.ToInt(text, entry.Count), minCount, maxCount);
                    commit?.Invoke();
                });
            }
            if (slider != null) {
                slider.maxValue = WeightTable.Target;
                slider.minValue = 0f;
                slider.wholeNumbers = true;
                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener(weight => {
                    entry.Weight = weight;
                    commit?.Invoke();
                });
            }
            if (value != null) {
                value.contentType = TMP_InputField.ContentType.IntegerNumber;
                value.onEndEdit.RemoveAllListeners();
                value.onEndEdit.AddListener(text => {
                    entry.Weight = Mathf.Clamp(QuickConfigBindings.ToInt(text, Mathf.RoundToInt(entry.Weight)), 0, (int)WeightTable.Target);
                    commit?.Invoke();
                });
            }
            if (remove != null) {
                remove.gameObject.SetActive(open);
                QuickConfigUi.Wire(remove, () => {
                    owner.Remove(entry);
                    commit?.Invoke();
                });
            }

            return () => {
                if (slider != null) { slider.SetValueWithoutNotify(entry.Weight); }
                if (value != null && value.isFocused == false) {
                    value.SetTextWithoutNotify(entry.Weight.ToString("0.#", CultureInfo.InvariantCulture));
                }
                if (count != null && count.isFocused == false) {
                    count.SetTextWithoutNotify(entry.Count.ToString(CultureInfo.InvariantCulture));
                }
            };
        }

        // The lowest free count, kept in count order.
        private void OnAdd() {
            if (entries == null) { return; }
            int next = WeightTable.NextFreeCount(entries, minCount, maxCount);
            if (next < 0) { return; }
            int index = entries.FindIndex(entry => entry.Count > next);
            entries.Insert(index < 0 ? entries.Count : index, new WeightEntry { Count = next, Weight = NewEntryWeight });
            commit?.Invoke();
        }
    }

    // Enchantments and sockets per rarity: both tables of the shown rarity, which is UI state on the row.
    private bool WireRarityCounts(BoundRow row) {
        Binding binding = row.Binding;
        WeightListView effects = WeightListView.Find(row.Root.transform, "Enchantments");
        WeightListView sockets = WeightListView.Find(row.Root.transform, "Sockets");
        if (effects == null && sockets == null) { return MissingWidget(row, "Enchantments/Sockets"); }
        Button select = QuickConfigUi.FindComponent<Button>(HeadOf(row), "Rarity");
        string selected = null;

        RarityCountsValue Value() => binding.Get(staged) as RarityCountsValue ?? new RarityCountsValue();
        IList<string> Names() => binding.Options?.Invoke() ?? new List<string>();

        void Commit() {
            binding.Set(staged, Value());
            Edited(null);
        }

        if (select != null) {
            QuickConfigUi.Wire(select, () => QuickConfigPicker.Show(binding.DisplayName, Names(), selected, picked => {
                selected = picked;
                row.Sync?.Invoke();
            }));
        }

        row.Sync = () => {
            IList<string> names = Names();
            if (selected == null || names.Contains(selected) == false) { selected = names.FirstOrDefault(); }
            Caption(select, selected ?? "");
            RarityCountsValue value = Value();
            effects?.Show(RarityCountsValue.Get(value.Effects, selected), null, QuickConfigBindings.MinEffectCount, int.MaxValue, Commit);
            sockets?.Show(RarityCountsValue.Get(value.Sockets, selected), null, 0, LootRoller.MaxSocketCount, Commit);
        };
        return true;
    }

    // Biome drop tables: pick a biome and one of its loot tables, then edit that table's drop amount
    // (item counts and their chances) and rarity chances. The shown biome and table are UI state on the row.
    private bool WireBiomeDrops(BoundRow row) {
        Binding binding = row.Binding;
        WeightListView amount = WeightListView.Find(row.Root.transform, "Amount");
        WeightListView rarity = WeightListView.Find(row.Root.transform, "Rarity");
        if (amount == null && rarity == null) { return MissingWidget(row, "Amount/Rarity"); }
        Transform head = HeadOf(row);
        Button biomeButton = QuickConfigUi.FindComponent<Button>(head, "Biome");
        Button targetButton = QuickConfigUi.FindComponent<Button>(head, "Target");
        List<string> rarityNames = Rarities.All.Select(r => r.ToString()).ToList();
        string selectedBiome = null;
        string selectedId = null;

        List<BiomeDropRow> Rows() => binding.Get(staged) as List<BiomeDropRow> ?? new List<BiomeDropRow>();

        // Rows arrive grouped in progression order, so the distinct biomes are already ordered.
        List<string> Groups(List<BiomeDropRow> rows) => rows.Select(r => r.Biome).Distinct().ToList();

        List<BiomeDropRow> Targets(List<BiomeDropRow> rows) => rows.Where(r => r.Biome == selectedBiome).ToList();

        string TargetName(BiomeDropRow target) => target.Label + (target.Mixed ? " (mixed)" : "");

        void Commit() {
            binding.Set(staged, Rows());
            Edited(null);
        }

        void Step(int delta) {
            List<BiomeDropRow> targets = Targets(Rows());
            if (targets.Count == 0) { return; }
            int index = targets.FindIndex(target => target.Id == selectedId);
            selectedId = targets[(Math.Max(index, 0) + delta + targets.Count) % targets.Count].Id;
            row.Sync?.Invoke();
        }

        if (biomeButton != null) {
            QuickConfigUi.Wire(biomeButton, () => {
                List<string> groups = Groups(Rows());
                List<string> display = groups.Select(BiomeDropTables.GroupDisplayName).ToList();
                QuickConfigPicker.Show(binding.DisplayName, display, BiomeDropTables.GroupDisplayName(selectedBiome), picked => {
                    int index = display.IndexOf(picked);
                    if (index >= 0 && groups[index] != selectedBiome) {
                        selectedBiome = groups[index];
                        selectedId = null;
                    }
                    row.Sync?.Invoke();
                });
            });
        }
        if (targetButton != null) {
            QuickConfigUi.Wire(targetButton, () => {
                List<BiomeDropRow> targets = Targets(Rows());
                List<string> display = targets.Select(TargetName).ToList();
                string current = targets.FirstOrDefault(target => target.Id == selectedId) is BiomeDropRow shown ? TargetName(shown) : null;
                QuickConfigPicker.Show("Loot table", display, current, picked => {
                    int index = display.IndexOf(picked);
                    if (index >= 0) { selectedId = targets[index].Id; }
                    row.Sync?.Invoke();
                });
            });
        }
        BindButton(head, "Prev", () => Step(-1));
        BindButton(head, "Next", () => Step(1));

        row.Sync = () => {
            List<BiomeDropRow> rows = Rows();
            List<string> groups = Groups(rows);
            if (selectedBiome == null || groups.Contains(selectedBiome) == false) {
                selectedBiome = groups.FirstOrDefault();
                selectedId = null;
            }
            Caption(biomeButton, BiomeDropTables.GroupDisplayName(selectedBiome));
            List<BiomeDropRow> targets = Targets(rows);
            BiomeDropRow target = targets.FirstOrDefault(t => t.Id == selectedId) ?? targets.FirstOrDefault();
            selectedId = target?.Id;
            Caption(targetButton, target != null ? TargetName(target) : "");
            // A table the file gives no Drops starts empty; + creates them.
            if (target != null && target.Amount == null) { target.Amount = new List<WeightEntry>(); }
            amount?.Show(target?.Amount, null, 0, int.MaxValue, Commit);
            rarity?.Show(target?.Rarity, rarityNames, 0, rarityNames.Count - 1, Commit);
        };
        return true;
    }

    // Enchanting table upgrade costs: pick a feature, and every level of it is shown at once, a Level
    // block per level (its name, + for another item, then its items, each with Remove). The levels shown
    // are Unlock up to the feature's staged max level (QuickConfigBindings.FitUpgradeLevels). When Items
    // lays the blocks out with a GridLayoutGroup, its cell height follows the level with the most items,
    // so no block spills into the one below it.
    private bool WireUpgradeCosts(BoundRow row) {
        Binding binding = row.Binding;
        ScrollRect items = QuickConfigUi.FindComponent<ScrollRect>(row.Root.transform, "Items");
        Transform content = items != null && items.content != null ? items.content : QuickConfigUi.FindChild(row.Root.transform, "Items");
        Transform levelTemplate = content != null ? QuickConfigUi.FindChild(content, "Level") : null;
        if (levelTemplate == null) { return MissingWidget(row, "Items/Level"); }
        Transform itemTemplate = QuickConfigUi.FindChild(levelTemplate, "Item");
        if (itemTemplate == null) { return MissingWidget(row, "Items/Level/Item"); }
        ClonedList blocks = new ClonedList(levelTemplate);
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        Func<int, float> blockHeight = BlockHeight(levelTemplate, itemTemplate);
        Button featureButton = QuickConfigUi.FindComponent<Button>(HeadOf(row), "Feature");
        EnchantingFeature? feature = null;

        UpgradeCostsValue Value() => binding.Get(staged) as UpgradeCostsValue ?? new UpgradeCostsValue();

        string FeatureName(EnchantingFeature f) => QuickConfigureTool.L(EnchantingTableUpgrades.GetFeatureName(f));

        void Commit() {
            binding.Set(staged, Value());
            Edited(null);
        }

        void BindLevel(Transform block, List<List<CostEntry>> levels, int index) {
            TMP_Text label = QuickConfigUi.TextAt(QuickConfigUi.FindChild(block, "Head") ?? block, "Label");
            if (label != null) { label.text = QuickConfigBindings.UpgradeLevelName(index); }
            BindButton(block, "Add", () => {
                (levels[index] ??= new List<CostEntry>()).Add(new CostEntry { Item = "", Amount = 1 });
                Commit();
            });

            Transform template = QuickConfigUi.FindChild(block, "Item");
            if (template == null) { return; }
            template.gameObject.SetActive(false);
            foreach (CostEntry entry in levels[index] ?? new List<CostEntry>()) {
                GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, template.parent, false);
                clone.name = template.name;
                clone.SetActive(true);
                TMP_InputField field = BindField(clone.transform, "Field", entry.Item, TMP_InputField.ContentType.Standard, text => {
                    entry.Item = text.Trim();
                    Commit();
                });
                BindPicker(QuickConfigUi.FindComponent<Button>(clone.transform, "Pick"), binding.Options, binding.DisplayName, () => entry.Item, picked => {
                    entry.Item = picked;
                    // Shown at once: the blocks are rebuilt on the refresh, but never under a focused field.
                    if (field != null) { field.SetTextWithoutNotify(picked); }
                    Commit();
                });
                BindField(clone.transform, "Amount", entry.Amount.ToString(CultureInfo.InvariantCulture), TMP_InputField.ContentType.IntegerNumber, text => {
                    entry.Amount = Mathf.Max(1, QuickConfigBindings.ToInt(text, entry.Amount));
                    Commit();
                });
                BindButton(clone.transform, "Remove", () => {
                    levels[index]?.Remove(entry);
                    Commit();
                });
            }
        }

        if (featureButton != null) {
            QuickConfigUi.Wire(featureButton, () => {
                List<EnchantingFeature> features = Value().Levels.Keys.OrderBy(f => f).ToList();
                List<string> display = features.Select(FeatureName).ToList();
                QuickConfigPicker.Show(binding.DisplayName, display, feature.HasValue ? FeatureName(feature.Value) : null, picked => {
                    int index = display.IndexOf(picked);
                    if (index >= 0) { feature = features[index]; }
                    row.Sync?.Invoke();
                });
            });
        }

        row.Sync = () => {
            UpgradeCostsValue value = Value();
            // The levels follow each feature's max level on the same page. Read-only (another machine's
            // file), they are only cut to it: padding there would be an edit that cannot be saved.
            if (row.ReadOnly == false) { QuickConfigBindings.FitUpgradeLevels(staged, value); }
            if (feature.HasValue == false || value.Get(feature.Value) == null) {
                feature = value.Levels.Keys.OrderBy(f => f).Cast<EnchantingFeature?>().FirstOrDefault();
            }
            Caption(featureButton, feature.HasValue ? FeatureName(feature.Value) : "");
            List<List<CostEntry>> levels = (feature.HasValue ? value.Get(feature.Value) : null) ?? new List<List<CostEntry>>();
            int shown = feature.HasValue ? Math.Min(levels.Count, QuickConfigBindings.UpgradeLevelsShown(staged, feature.Value, levels.Count)) : 0;
            string signature = $"{feature}|{shown}|{RuntimeHelpers.GetHashCode(levels)}|" + string.Join("|", levels.Take(shown).Select(level => level == null
                ? "-"
                : RuntimeHelpers.GetHashCode(level) + ":" + string.Join(",", level.Select(entry => $"{entry.Item}*{entry.Amount}"))));
            blocks.Rebuild(Enumerable.Range(0, shown).ToList(), signature, (block, index) => BindLevel(block, levels, index), row.Root);
            if (grid != null) {
                int most = shown == 0 ? 0 : levels.Take(shown).Max(level => level?.Count ?? 0);
                grid.cellSize = new Vector2(grid.cellSize.x, blockHeight(Math.Max(1, most)));
            }
        };
        return true;
    }

    // The height a Level block needs for n items, from the template's own layout: padding, the Head line,
    // then n Item lines with the spacing before each.
    private static Func<int, float> BlockHeight(Transform levelTemplate, Transform itemTemplate) {
        VerticalLayoutGroup layout = levelTemplate.GetComponent<VerticalLayoutGroup>();
        float padding = layout != null ? layout.padding.top + layout.padding.bottom : 0f;
        float spacing = layout != null ? layout.spacing : 0f;
        Transform head = QuickConfigUi.FindChild(levelTemplate, "Head");
        float headHeight = head != null && head.GetComponent<LayoutElement>() is LayoutElement headLayout ? headLayout.preferredHeight : 24f;
        float itemHeight = itemTemplate.GetComponent<LayoutElement>() is LayoutElement itemLayout ? itemLayout.preferredHeight : 26f;
        return count => padding + headHeight + count * (spacing + itemHeight);
    }
}
