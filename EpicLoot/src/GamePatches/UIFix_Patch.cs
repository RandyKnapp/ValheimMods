using EpicLoot.Config;
using HarmonyLib;
using Jotunn.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace EpicLoot;

[HarmonyPatch]
public static class PatchOnHoverFix
{
    public const float MIN_WIDTH = 150f;
    public const float MIN_HEIGHT = 350f;
    public const float PADDING = 10f;
    public const float CURSOR_PADDING = 20f;
    internal const float VERTICAL_PADDING = 5f;
    internal const float SCROLLBAR_WIDTH = PADDING;
    private const float COMPARISON_GAP = 5f;
    private const float SCREEN_MARGIN = 5f;
    private const string BoxName = "Canvas";
    private const string ComparisonBoxName = "ComparisonCanvas";

    // Upper bounds for the tooltip box, which is otherwise sized to its content (TooltipBox.Fit). Read on
    // every frame the tooltip is shown, so config changes apply live.
    public static float MaxWidth => Mathf.Max(ELConfig.TooltipMaxWidth.Value, MIN_WIDTH);
    public static float MaxHeight => Mathf.Max(ELConfig.TooltipMaxHeight.Value, MIN_HEIGHT);
    public static string ComparisonTitleString = "";
    public static string ComparisonTooltipString = "";
    public static bool ComparisonAdded = false;
    public static GameObject ComparisonTT = null;

    // The raw (unlocalized) strings last written into ComparisonTT, so it is only re-localized on a change.
    private static string shownComparisonTitle;
    private static string shownComparisonText;

    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnPointerExit))]
    public static class PointerExitDestroyComparison
    {
        static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
    [HarmonyPostfix]
    public static void AddComparisonTooltip()
    {
        if (UITooltip.m_tooltip == null)
        {
            return;
        }

        if (ComparisonTooltipString == "")
        {
            if (ComparisonTT != null)
            {
                GameObject.Destroy(ComparisonTT);
            }

            ComparisonTT = null;
            ComparisonAdded = false;
            return;
        }

        TooltipBox mainBox = GetMainBox(UITooltip.m_tooltip);
        if (mainBox == null)
        {
            return;
        }

        // The comparison is a second copy of the main box beside it. It is rebuilt whenever it is gone:
        // vanilla destroys the whole tooltip, comparison included, when the cursor leaves the slot.
        if (ComparisonTT == null || ComparisonTT.transform.parent != UITooltip.m_tooltip.transform)
        {
            if (ComparisonTT != null)
            {
                GameObject.Destroy(ComparisonTT);
            }

            ComparisonTT = GameObject.Instantiate(mainBox.gameObject, UITooltip.m_tooltip.transform);
            ComparisonTT.name = ComparisonBoxName;
            shownComparisonTitle = null;
            shownComparisonText = null;
        }

        ComparisonAdded = true;

        // This runs every frame the slot is hovered, so the text follows the hovered item: moving from a
        // sword to a helmet with Ctrl held compares against the equipped helmet.
        if (ComparisonTitleString != shownComparisonTitle || ComparisonTooltipString != shownComparisonText)
        {
            Transform header = Utils.FindChild(ComparisonTT.transform, "Topic");
            header.GetComponent<TMP_Text>().text = Localization.instance.Localize(ComparisonTitleString);
            Transform contentt = Utils.FindChild(ComparisonTT.transform, "Text");
            TMP_Text content = contentt.GetComponent<TMP_Text>();
            content.text = Localization.instance.Localize(ComparisonTooltipString);
            MagicTextShimmer.Ensure(content);
            shownComparisonTitle = ComparisonTitleString;
            shownComparisonText = ComparisonTooltipString;
        }
    }

    /// <summary>
    /// Add a scrollbar to the Tooltip on hover start. The box is sized to its content and placed beside
    /// the cursor every frame by <see cref="PlaceTooltip"/>.
    /// </summary>
    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnHoverStart))]
    [HarmonyPostfix]
    public static void Postfix(GameObject go)
    {
        if (UITooltip.m_tooltip != null)
        {
            RectTransform transform = (RectTransform)go.transform;
            AddScrollbar(UITooltip.m_tooltip, transform);
            EnsureShimmer(UITooltip.m_tooltip);
        }
    }

    private static void EnsureShimmer(GameObject tooltipObject)
    {
        Transform textTransform = Utils.FindChild(tooltipObject.transform, "Text");
        if (textTransform != null)
        {
            MagicTextShimmer.Ensure(textTransform.GetComponent<TMP_Text>());
        }
    }

    // hoverTransform is no longer read (placement happens every frame in PlaceTooltip); the signature is
    // kept because other mods patch this method by it.
    public static void AddScrollbar(GameObject tooltipObject, RectTransform hoverTransform)
    {
        if (tooltipObject == null)
        {
            return;
        }

        // UITooltip.m_tooltip is static and outlives a single hover -- OnPointerExit is suppressed
        // above, so it survives the cursor moving between slots, and MagicTooltipPatches re-enters
        // OnHoverStart through UITooltip.Set. Everything below is a one-shot migration of the prefab's
        // children into a scroll view, so a second pass over the same object corrupts it: it finds the
        // first pass's "Content" (already carrying the copied background) and, within the same frame,
        // still finds the "Bkg" whose Destroy has not been processed yet.
        if (Utils.FindChild(tooltipObject.transform, "Scroll View") != null)
        {
            return;
        }

        Transform header = Utils.FindChild(tooltipObject.transform, "Topic");
        if (header == null)
        {
            // No scrollbar needed for this object
            return;
        }

        GameObject scrollArea = GUIManager.Instance.CreateScrollView(
            tooltipObject.transform, false, true, SCROLLBAR_WIDTH, PADDING,
            GUIManager.Instance.ValheimScrollbarHandleColorBlock, Color.grey, MaxWidth, MaxHeight);

        // Hide the scrollbar by default, it will show when needed
        Scrollbar scrollbar = scrollArea.GetComponentInChildren<Scrollbar>();
        scrollbar.gameObject.SetActive(false);

        Transform contentt = Utils.FindChild(scrollArea.transform, "Content");
        Transform tooltipTextTransform = Utils.FindChild(tooltipObject.transform, "Text");
        header.SetParent(contentt, false);
        tooltipTextTransform.SetParent(contentt, false);

        Transform scrolltform = Utils.FindChild(scrollArea.transform, "Scroll View");
        ScrollRect scrollRect = scrolltform.GetComponent<ScrollRect>();
        // Scroll sensitivity fix for combat update
        scrollRect.scrollSensitivity = 800;
        // TooltipBox decides when the box scrolls and shows the scrollbar itself; left to expand the
        // viewport, the ScrollRect would resize it a frame after the box was fitted.
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        // Copy the existing background from the header tooltip section to the content of the scrollview
        // Set the header section to match the width of the scroll area
        Transform bkgtform = tooltipObject.transform.Find("Bkg");
        if (bkgtform != null)
        {
            Image backgroundImage = bkgtform.GetComponent<Image>();
            // AddComponent returns null (after a Unity warning) if the object already carries a
            // Graphic. The guard at the top of this method is what keeps that from happening; don't
            // dereference the result blindly, because this runs inside a Harmony postfix and an NRE
            // here unwinds all the way out of InventoryGui.Update.
            Image contentbkgImage = contentt.gameObject.AddComponent<Image>();
            if (contentbkgImage != null)
            {
                contentbkgImage.color = backgroundImage.color;
                contentbkgImage.sprite = backgroundImage.sprite;
                contentbkgImage.type = backgroundImage.type;
                contentbkgImage.raycastTarget = false;
            }
            // Remove the header background as it is no longer needed
            GameObject.Destroy(bkgtform.gameObject);
        }

        // The title and text span the box; TooltipBox.Fit narrows the box to the text instead.
        VerticalLayoutGroup vlg = contentt.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;

        // Add the scrollbar handler to allow mouse wheel scrolling while not hovering over the scrollbar
        scrolltform.gameObject.AddComponent<ScrollWheelHandler>();

        // CreateScrollView gives every piece a fixed size; hang them all off the outer rect instead, so
        // TooltipBox.Fit only has to size that one. Pivot top-left: resizing keeps the corner in place.
        RectTransform scrollRT = scrollArea.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 1);
        scrollRT.anchorMax = new Vector2(0, 1);
        scrollRT.pivot = new Vector2(0, 1);
        Stretch((RectTransform)scrolltform);
        Stretch(scrollRect.viewport);

        RectTransform contentRT = (RectTransform)contentt;
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0, 1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, contentRT.sizeDelta.y);

        RectTransform scrollbarRT = (RectTransform)scrollbar.transform;
        scrollbarRT.anchorMin = new Vector2(1, 0);
        scrollbarRT.anchorMax = new Vector2(1, 1);
        scrollbarRT.pivot = new Vector2(1, 0.5f);
        scrollbarRT.offsetMin = new Vector2(-SCROLLBAR_WIDTH, PADDING);
        scrollbarRT.offsetMax = new Vector2(0, -PADDING);
        Stretch((RectTransform)scrollbar.transform.Find("Sliding Area"));

        // Some tooltip prefabs carry a layout group on their root, which would otherwise size the box.
        scrollArea.AddComponent<LayoutElement>().ignoreLayout = true;
        scrollArea.AddComponent<TooltipBox>();
    }

    /// <summary>
    /// Sizes the tooltip box (and the comparison box, when there is one) to its content and places it
    /// beside the cursor, after vanilla has moved the tooltip to the cursor and clamped it.
    /// </summary>
    /// <remarks>
    /// Vanilla clamps the tooltip's first child to the screen, which is the box AddScrollbar builds. That
    /// used to be MaxWidth x MaxHeight however little of it the text used, so a large maximum pushed the
    /// visible part away from the cursor. The position is also rebuilt from scratch here every frame,
    /// because vanilla's clamp moves the box permanently and the tooltip is reused from slot to slot.
    /// </remarks>
    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.LateUpdate))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void PlaceTooltip(UITooltip __instance)
    {
        GameObject tooltip = UITooltip.m_tooltip;
        if (UITooltip.m_current != __instance || tooltip == null || !tooltip.activeInHierarchy)
        {
            return;
        }

        TooltipBox main = GetMainBox(tooltip);
        if (main == null)
        {
            return;
        }

        TooltipBox comparison = ComparisonTT != null && ComparisonTT.transform.parent == tooltip.transform
            ? ComparisonTT.GetComponent<TooltipBox>() : null;

        // Worked out in the box's own units from the screen's bottom-left corner: the tooltip canvas is
        // screen-space overlay, so its world position is in pixels and lossyScale is the GUI scale.
        float scale = main.transform.lossyScale.y;
        if (scale <= 0f)
        {
            scale = 1f;
        }

        float screenWidth = Screen.width / scale;
        float screenHeight = Screen.height / scale;
        float maxWidth = Mathf.Min(MaxWidth, screenWidth - 2f * SCREEN_MARGIN);
        if (comparison != null)
        {
            maxWidth = Mathf.Min(maxWidth, (screenWidth - 2f * SCREEN_MARGIN - COMPARISON_GAP) / 2f);
        }

        float maxHeight = Mathf.Min(MaxHeight, screenHeight - 2f * SCREEN_MARGIN);
        Vector2 limit = new Vector2(Mathf.Max(maxWidth, MIN_WIDTH / 2f), Mathf.Max(maxHeight, MIN_WIDTH / 2f));

        if (!main.Fit(limit, out Vector2 mainSize))
        {
            return;
        }

        Vector2 comparisonSize = Vector2.zero;
        if (comparison != null && !comparison.Fit(limit, out comparisonSize))
        {
            comparison = null;
        }

        float groupWidth = mainSize.x + (comparison != null ? COMPARISON_GAP + comparisonSize.x : 0f);
        float groupHeight = Mathf.Max(mainSize.y, comparisonSize.y);

        // The cursor under a mouse, the focused slot under a gamepad (vanilla moved the tooltip there).
        Vector3 anchor = tooltip.transform.position;
        float anchorX = anchor.x / scale;
        float anchorY = anchor.y / scale;

        // Open toward the middle of the screen, the side with more room, then keep the whole group on
        // screen. The comparison sits on the far side of the main box, so it never covers the cursor.
        bool toLeft = anchorX > screenWidth / 2f;
        float left = toLeft ? anchorX - PADDING - groupWidth : anchorX + CURSOR_PADDING;
        left = Mathf.Max(SCREEN_MARGIN, Mathf.Min(left, screenWidth - SCREEN_MARGIN - groupWidth));
        float top = Mathf.Min(screenHeight - SCREEN_MARGIN, Mathf.Max(anchorY, SCREEN_MARGIN + groupHeight));

        if (comparison == null)
        {
            main.PlaceTopLeft(left, top, scale);
        }
        else if (toLeft)
        {
            comparison.PlaceTopLeft(left, top, scale);
            main.PlaceTopLeft(left + comparisonSize.x + COMPARISON_GAP, top, scale);
        }
        else
        {
            main.PlaceTopLeft(left, top, scale);
            comparison.PlaceTopLeft(left + mainSize.x + COMPARISON_GAP, top, scale);
        }
    }

    // The comparison box is renamed, so the only child named BoxName is the tooltip's own box.
    private static TooltipBox GetMainBox(GameObject tooltip)
    {
        Transform box = tooltip.transform.Find(BoxName);
        return box != null ? box.GetComponent<TooltipBox>() : null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

/// <summary>
/// One tooltip box: as wide as its longest line and as tall as its content, each capped by the limit
/// it is given, and scrolling past the height cap.
/// </summary>
/// <remarks>
/// The comparison box is an Instantiate copy of the main one, which does not carry these private
/// fields over, so the references are looked up by name on first use.
/// </remarks>
internal class TooltipBox : MonoBehaviour
{
    private RectTransform box;
    private RectTransform content;
    private Scrollbar scrollbar;
    private VerticalLayoutGroup layout;
    private TMP_Text topic;
    private TMP_Text text;

    private string fittedTopic;
    private string fittedText;
    private Vector2 fittedLimit;
    private Vector2 fittedSize;

    public bool Fit(Vector2 limit, out Vector2 size)
    {
        size = fittedSize;
        if (!Bind())
        {
            return false;
        }

        string topicValue = topic.text;
        string textValue = text.text;
        bool textChanged = topicValue != fittedTopic || textValue != fittedText;
        if (!textChanged && limit == fittedLimit)
        {
            return true;
        }

        // Unwrapped widths: the longest line decides the width, and only past the cap does it wrap.
        // The extra pixel keeps TMP from wrapping a line measured to fit exactly.
        float natural = Mathf.Max(topic.preferredWidth, text.preferredWidth) + 2f * PatchOnHoverFix.PADDING;
        float width = Mathf.Clamp(Mathf.Ceil(natural) + 1f, Mathf.Min(PatchOnHoverFix.MIN_WIDTH, limit.x), limit.x);
        float height = LayOut(width, PatchOnHoverFix.PADDING);

        bool scrolls = height > limit.y;
        if (scrolls)
        {
            // The scrollbar is drawn over the right edge; keep the text out from under it, widening
            // the box by the scrollbar where there is room rather than re-wrapping the text.
            width = Mathf.Min(limit.x, width + PatchOnHoverFix.SCROLLBAR_WIDTH);
            LayOut(width, PatchOnHoverFix.PADDING + PatchOnHoverFix.SCROLLBAR_WIDTH);
            height = limit.y;
        }

        box.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        scrollbar.gameObject.SetActive(scrolls);
        if (textChanged)
        {
            // New text (another item, or Shift toggling the details) starts at the top.
            content.anchoredPosition = Vector2.zero;
        }

        fittedTopic = topicValue;
        fittedText = textValue;
        fittedLimit = limit;
        fittedSize = new Vector2(width, height);
        size = fittedSize;
        return true;
    }

    public void PlaceTopLeft(float left, float top, float scale)
    {
        box.position = new Vector3(left * scale, top * scale, box.position.z);
    }

    // Lays the content out at this width and returns its height.
    private float LayOut(float width, float rightPadding)
    {
        box.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        int vertical = (int)PatchOnHoverFix.VERTICAL_PADDING;
        layout.padding = new RectOffset((int)PatchOnHoverFix.PADDING, (int)rightPadding, vertical, vertical);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        return content.rect.height;
    }

    private bool Bind()
    {
        if (topic != null && text != null && layout != null && scrollbar != null)
        {
            return true;
        }

        box = (RectTransform)transform;
        ScrollRect scrollRect = GetComponentInChildren<ScrollRect>(true);
        scrollbar = scrollRect != null ? scrollRect.verticalScrollbar : null;
        content = scrollRect != null ? scrollRect.content : null;
        layout = content != null ? content.GetComponent<VerticalLayoutGroup>() : null;
        Transform topicTransform = content != null ? content.Find("Topic") : null;
        Transform textTransform = content != null ? content.Find("Text") : null;
        topic = topicTransform != null ? topicTransform.GetComponent<TMP_Text>() : null;
        text = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
        return topic != null && text != null && layout != null && scrollbar != null;
    }
}

/// <summary>
/// The following class is largely taken from Azumatt's Tooltip Expansion mod:
/// https://github.com/AzumattDev/TooltipExpansion/blob/main/CodeNShit/Monos/TooltipSizeAdjuster.cs
/// </summary>
public class ScrollWheelHandler : MonoBehaviour
{
    private ScrollRect _scrollRect = null;
    private UITooltip _owner;
    private bool _ownerIsInventoryItem;

    public void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
        if (_scrollRect == null)
        {
            EpicLoot.LogWarning("ScrollWheelHandler: No ScrollRect found on " + gameObject.name);
        }
        else
        {
            _scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void Update()
    {
        // Only process if this object is active.
        if (!gameObject.activeInHierarchy || _scrollRect == null)
        {
            return;
        }

        if (ZInput.IsGamepadActive() && OwnerIsInventoryItem())
        {
            GamepadScroll.ApplyRightStickY(_scrollRect);
        }

        // Get scroll wheel input regardless of pointer location.
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (!(Mathf.Abs(scrollDelta) > float.Epsilon))
        {
            return;
        }

        // Adjust the vertical scroll position.
        float newScrollPosition = _scrollRect.verticalNormalizedPosition + scrollDelta * 0.7f;
        _scrollRect.verticalNormalizedPosition = Mathf.Clamp01(newScrollPosition);
    }

    private bool OwnerIsInventoryItem()
    {
        UITooltip owner = UITooltip.m_current;
        if (owner != _owner)
        {
            _owner = owner;
            _ownerIsInventoryItem = owner != null && owner.GetComponentInParent<InventoryGrid>() != null;
        }

        return _ownerIsInventoryItem;
    }
}
