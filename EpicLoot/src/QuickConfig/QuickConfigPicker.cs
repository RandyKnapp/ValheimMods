using Common;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.QuickConfig;

/// <summary>
/// Runtime controller for the QuickConfigPicker prefab: a filterable "pick one of these" overlay
/// parented to CustomGUIFront, above everything and clipped by nothing. Contract (README):
/// Panel -> Title, Search (TMP_InputField), List (ScrollRect whose content holds an Entry template
/// Button + Text), Close (Button).
/// </summary>
internal static class QuickConfigPicker {
    internal const string AssetName = "QuickConfigPicker";
    private const int MaxVisible = 200;

    private static GameObject overlay;
    private static Transform content;
    private static GameObject entryTemplate;
    private static List<string> all;
    private static string current;
    private static Action<string> onPick;
    private static readonly List<GameObject> clones = new List<GameObject>();
    private static bool warnedMissing;

    internal static bool IsOpen => overlay != null;

    internal static void Show(string title, IList<string> options, string selected, Action<string> pick) {
        Close();
        if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null) { return; }
        GameObject prefab = EpicLoot.LoadAsset<GameObject>(AssetName);
        if (prefab == null) {
            if (warnedMissing == false) {
                warnedMissing = true;
                EpicLoot.LogErrorForce($"The {AssetName} prefab is missing from the asset bundle; pickers cannot open.");
            }
            return;
        }

        all = new List<string>();
        if (options != null) {
            foreach (string option in options) {
                if (string.IsNullOrEmpty(option) == false) { all.Add(option); }
            }
        }
        current = selected;
        onPick = pick;

        overlay = UnityEngine.Object.Instantiate(prefab, GUIManager.CustomGUIFront.transform, false);
        overlay.name = "EpicLootQuickConfigPicker";
        QuickConfigUi.Stretch(overlay);
        // Held for the picker's own lifetime. Refcounted in ConfigUI, so closing this does not unblock
        // input while the panel underneath is still open.
        overlay.AddComponent<ConfigUI.ConfigUIInputGuard>().Hold();
        overlay.transform.SetAsLastSibling();

        Transform panel = QuickConfigUi.FindChild(overlay.transform, "Panel") ?? overlay.transform;
        TMP_Text titleText = QuickConfigUi.TextAt(panel, "Title");
        if (titleText != null) { titleText.text = QuickConfigureTool.L(title ?? ""); }

        TMP_InputField search = QuickConfigUi.FindComponent<TMP_InputField>(panel, "Search");
        if (search != null) {
            if (search.placeholder is TMP_Text placeholder) { placeholder.text = QuickConfigureTool.L("$mod_epicloot_cfg_picker_search"); }
            search.SetTextWithoutNotify("");
            search.onValueChanged.RemoveAllListeners();
            search.onValueChanged.AddListener(Rebuild);
        }

        ScrollRect list = QuickConfigUi.FindComponent<ScrollRect>(panel, "List");
        content = list != null && list.content != null ? list.content : QuickConfigUi.FindChild(panel, "List") ?? panel;
        Transform template = QuickConfigUi.FindChild(content, "Entry");
        entryTemplate = template != null ? template.gameObject : null;
        if (entryTemplate == null) {
            EpicLoot.LogWarningForce($"The {AssetName} prefab has no Entry template under its List; the picker shows nothing.");
        } else {
            entryTemplate.SetActive(false);
        }

        Button close = QuickConfigUi.FindComponent<Button>(panel, "Close");
        QuickConfigUi.SetCaption(close, "$mod_epicloot_cfg_picker_close");
        QuickConfigUi.Wire(close, Close);

        QuickConfigStyle.Apply(overlay);
        Rebuild("");
        if (search != null) { search.ActivateInputField(); }
    }

    private static void Rebuild(string filter) {
        foreach (GameObject clone in clones) {
            if (clone != null) { UnityEngine.Object.Destroy(clone); }
        }
        clones.Clear();
        if (entryTemplate == null || content == null) { return; }

        string needle = (filter ?? "").Trim();
        List<string> matches = new List<string>();
        foreach (string option in all) {
            if (needle.Length == 0 || option.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) { matches.Add(option); }
        }

        int shown = Mathf.Min(matches.Count, MaxVisible);
        for (int i = 0; i < shown; i++) {
            string option = matches[i];
            // Entry captions are orange like every button, so the current value is marked in Jotunn's ValheimYellow.
            GameObject entry = MakeEntry(option == current ? $"<color=#FFE300>{option}</color>" : option);
            Button button = entry.GetComponent<Button>() ?? entry.GetComponentInChildren<Button>(true);
            QuickConfigUi.Wire(button, () => {
                Action<string> callback = onPick;
                Close();
                callback?.Invoke(option);
            });
        }
        if (matches.Count > shown) {
            GameObject more = MakeEntry($"<i>... {matches.Count - shown} more. Type to filter the list.</i>");
            Button button = more.GetComponent<Button>() ?? more.GetComponentInChildren<Button>(true);
            if (button != null) { button.interactable = false; }
        }
    }

    private static GameObject MakeEntry(string text) {
        GameObject entry = UnityEngine.Object.Instantiate(entryTemplate, content, false);
        entry.name = "Entry";
        entry.SetActive(true);
        TMP_Text caption = entry.GetComponentInChildren<TMP_Text>(true);
        if (caption != null) { caption.text = text; }
        clones.Add(entry);
        return entry;
    }

    internal static void Close() {
        if (overlay != null) { UnityEngine.Object.Destroy(overlay); }
        overlay = null;
        content = null;
        entryTemplate = null;
        clones.Clear();
        all = null;
        onPick = null;
    }
}

/// <summary>
/// Runtime controller for the QuickConfigConfirm prefab: Panel -> Title, Body, Keep, Discard,
/// SaveClose (Buttons). A button whose label is null is hidden.
/// </summary>
internal static class QuickConfigConfirm {
    internal const string AssetName = "QuickConfigConfirm";

    private static GameObject overlay;
    private static bool warnedMissing;

    internal static bool IsOpen => overlay != null;

    /// <summary>True when the prefab is in the bundle, so a caller can fall back when it is not.</summary>
    internal static bool Available => EpicLoot.LoadAsset<GameObject>(AssetName) != null;

    internal static bool Show(string title, string body, string keepLabel, string discardLabel, string saveCloseLabel,
        Action onKeep, Action onDiscard, Action onSaveClose) {
        Close();
        if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null) { return false; }
        GameObject prefab = EpicLoot.LoadAsset<GameObject>(AssetName);
        if (prefab == null) {
            if (warnedMissing == false) {
                warnedMissing = true;
                EpicLoot.LogErrorForce($"The {AssetName} prefab is missing from the asset bundle; confirmations cannot open.");
            }
            return false;
        }

        overlay = UnityEngine.Object.Instantiate(prefab, GUIManager.CustomGUIFront.transform, false);
        overlay.name = "EpicLootQuickConfigConfirm";
        QuickConfigUi.Stretch(overlay);
        overlay.AddComponent<ConfigUI.ConfigUIInputGuard>().Hold();
        overlay.transform.SetAsLastSibling();

        Transform panel = QuickConfigUi.FindChild(overlay.transform, "Panel") ?? overlay.transform;
        TMP_Text titleText = QuickConfigUi.TextAt(panel, "Title");
        if (titleText != null) { titleText.text = QuickConfigureTool.L(title ?? ""); }
        TMP_Text bodyText = QuickConfigUi.TextAt(panel, "Body");
        if (bodyText != null) { bodyText.text = QuickConfigureTool.L(body ?? ""); }

        WireButton(panel, "Keep", keepLabel, onKeep);
        WireButton(panel, "Discard", discardLabel, onDiscard);
        WireButton(panel, "SaveClose", saveCloseLabel, onSaveClose);

        QuickConfigStyle.Apply(overlay);
        return true;
    }

    private static void WireButton(Transform panel, string name, string label, Action onClick) {
        Button button = QuickConfigUi.FindComponent<Button>(panel, name);
        if (button == null) { return; }
        if (label == null) {
            button.gameObject.SetActive(false);
            return;
        }
        button.gameObject.SetActive(true);
        QuickConfigUi.SetCaption(button, label);
        QuickConfigUi.Wire(button, () => {
            Close();
            onClick?.Invoke();
        });
    }

    internal static void Close() {
        if (overlay != null) { UnityEngine.Object.Destroy(overlay); }
        overlay = null;
    }
}
