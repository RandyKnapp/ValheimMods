using BepInEx.Configuration;
using Common;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EpicLoot.QuickConfig;

// Hover tooltips for the Quick Configure panel.
//
// Hand-built rather than vanilla's UITooltip: that one draws through a prefab the game wires up for
// its own GUI, and this panel also opens on the main menu. One shared box is moved around and
// re-filled instead of one per row.
//
// The text comes from what already documents each setting: a BepInEx entry's ConfigDescription (the
// same line an admin reads in the .cfg), or a hand-written string for the JSON-backed rows, so the
// panel and the files cannot drift apart.
internal static class QuickConfigTooltip {
    private const float MaxWidth = 360f;
    private const float Padding = 10f;
    private const float CursorGap = 18f;

    private static GameObject root;
    private static Text label;
    private static RectTransform rect;
    private static TooltipTarget active;

    // Attaches a tooltip and returns the target, so a row can be wrapped where it is bound.
    internal static GameObject Attach(GameObject target, string text) {
        if (target == null || string.IsNullOrEmpty(text)) { return target; }
        // Pointer events need something that takes raycasts, and a laid-out row may be a bare
        // RectTransform. The catcher sits behind the row's own widgets, which are raycast first, so it
        // never steals a click from them.
        if (target.GetComponent<Graphic>() == null) {
            Image catcher = target.AddComponent<Image>();
            catcher.color = new Color(0f, 0f, 0f, 0f);
            catcher.raycastTarget = true;
        }
        TooltipTarget tip = target.GetComponent<TooltipTarget>();
        if (tip == null) { tip = target.AddComponent<TooltipTarget>(); }
        tip.Text = text;
        return target;
    }

    // --- text sources ---------------------------------------------------------------------------

    // A BepInEx setting: its key, and the description written for the config file.
    internal static string Of(ConfigEntryBase entry) {
        if (entry == null) { return ""; }
        return Compose(entry.Definition?.Key, entry.Description?.Description);
    }

    internal static string Text(string key, string description) {
        return Compose(key, description);
    }

    private static string Compose(string key, string description) {
        if (string.IsNullOrEmpty(description)) { return string.IsNullOrEmpty(key) ? "" : key; }
        if (string.IsNullOrEmpty(key)) { return description; }
        return $"<color=#FFCB6B>{key}</color>\n{description}";
    }

    // --- the box --------------------------------------------------------------------------------

    internal static void Show(TooltipTarget target) {
        if (target == null || string.IsNullOrEmpty(target.Text)) { return; }
        EnsureRoot();
        if (root == null) { return; }
        active = target;
        label.text = target.Text;
        Layout();
        root.SetActive(true);
        // Drawn over the panel, which shares this parent.
        root.transform.SetAsLastSibling();
        Reposition();
    }

    internal static void Hide(TooltipTarget target) {
        if (active != target) { return; }
        active = null;
        if (root != null) { root.SetActive(false); }
    }

    // Called when the panel closes: CustomGUIFront is rebuilt on every scene change, so nothing here
    // outlives it.
    internal static void Close() {
        active = null;
        if (root != null) { Object.Destroy(root); }
        root = null;
        label = null;
        rect = null;
    }

    private static void EnsureRoot() {
        if (root != null) { return; }
        if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null) { return; }

        root = ConfigUI.NewUI("EpicLootQuickConfigTooltip", GUIManager.CustomGUIFront.transform, typeof(Image));
        rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 1f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0.06f, 0.05f, 0.04f, 0.96f);
        // Never a raycast target: a box under the cursor would count as leaving the row it describes,
        // and the two would flicker against each other.
        background.raycastTarget = false;

        label = ConfigUI.AddText(root.transform, Padding, Padding, MaxWidth - 2 * Padding, 20f, "", 14,
            TextAnchor.UpperLeft, GUIManager.Instance.ValheimBeige);
        label.raycastTarget = false;

        root.AddComponent<TooltipFollower>();
        root.SetActive(false);
    }

    private static void Layout() {
        float width = Mathf.Min(MaxWidth - 2 * Padding, label.preferredWidth);
        label.rectTransform.sizeDelta = new Vector2(width, 20f);
        float height = label.preferredHeight;
        label.rectTransform.sizeDelta = new Vector2(width, height);
        rect.sizeDelta = new Vector2(width + 2 * Padding, height + 2 * Padding);
    }

    // Follows the cursor, kept inside the parent canvas so a row near an edge does not push its
    // tooltip off screen.
    private static void Reposition() {
        if (root == null || root.activeSelf == false) { return; }
        RectTransform parent = root.transform.parent as RectTransform;
        if (parent == null) { return; }

        Canvas canvas = parent.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Input.mousePosition, camera, out Vector2 local) == false) {
            return;
        }

        Vector2 size = rect.sizeDelta;
        Rect bounds = parent.rect;
        float x = local.x + CursorGap;
        float y = local.y - CursorGap;
        // Flip to the other side of the cursor rather than sliding, so the box never sits on top of
        // what it describes.
        if (x + size.x > bounds.xMax) { x = local.x - CursorGap - size.x; }
        if (y - size.y < bounds.yMin) { y = local.y + CursorGap + size.y; }
        rect.anchoredPosition = new Vector2(
            Mathf.Clamp(x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - size.x)),
            Mathf.Clamp(y, Mathf.Min(bounds.yMax, bounds.yMin + size.y), bounds.yMax));
    }

    internal class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        internal string Text;

        public void OnPointerEnter(PointerEventData eventData) {
            Show(this);
        }

        public void OnPointerExit(PointerEventData eventData) {
            Hide(this);
        }

        // A conditional row can be switched off while hovered, and no exit event follows.
        public void OnDisable() {
            Hide(this);
        }
    }

    private class TooltipFollower : MonoBehaviour {
        public void Update() {
            Reposition();
        }
    }
}
