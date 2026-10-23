using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.QuickConfig;

/// <summary>Small helpers for finding the named children the prefab contract promises.</summary>
internal static class QuickConfigUi {
    /// <summary>Breadth-first search by exact name, inactive children included, so a direct child wins over a nested namesake.</summary>
    internal static Transform FindChild(Transform root, string name) {
        if (root == null || string.IsNullOrEmpty(name)) { return null; }
        Queue<Transform> queue = new Queue<Transform>();
        foreach (Transform child in root) { queue.Enqueue(child); }
        while (queue.Count > 0) {
            Transform current = queue.Dequeue();
            if (current.name == name) { return current; }
            foreach (Transform child in current) { queue.Enqueue(child); }
        }
        return null;
    }

    internal static T FindComponent<T>(Transform root, string name) where T : Component {
        Transform found = FindChild(root, name);
        return found != null ? found.GetComponent<T>() : null;
    }

    /// <summary>The TMP text on the named child, or on one of its children (a Button's caption).</summary>
    internal static TMP_Text TextAt(Transform root, string name) {
        Transform found = FindChild(root, name);
        return found != null ? found.GetComponentInChildren<TMP_Text>(true) : null;
    }

    internal static TMP_Text CaptionOf(Component widget) {
        return widget != null ? widget.GetComponentInChildren<TMP_Text>(true) : null;
    }

    internal static void SetCaption(Component widget, string text) {
        TMP_Text caption = CaptionOf(widget);
        if (caption != null) { caption.text = QuickConfigureTool.L(text); }
    }

    internal static void Stretch(GameObject go) {
        if (go == null || go.transform is RectTransform rect == false) { return; }
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    /// <summary>Runs every $token in the tree through the localizer, inactive objects included.</summary>
    internal static void LocalizeTexts(GameObject root) {
        if (root == null) { return; }
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true)) {
            if (text.text != null && text.text.StartsWith("$")) { text.text = QuickConfigureTool.L(text.text); }
        }
    }

    internal static void Wire(Button button, UnityEngine.Events.UnityAction onClick) {
        if (button == null) { return; }
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
    }
}
