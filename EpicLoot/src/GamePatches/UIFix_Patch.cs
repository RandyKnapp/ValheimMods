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
    public const float MAX_WIDTH = 350f;
    public const float MAX_HEIGHT = 700f;
    public const float PADDING = 10f;
    public const float CURSOR_PADDING = 20f;
    public static string ComparisonTitleString = "";
    public static string ComparisonTooltipString = "";
    public static bool ComparisonAdded = false;
    public static GameObject ComparisonTT = null;

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

        // Reset comparison tooltip if it was previously added
        if (ComparisonTT != null && ComparisonTooltipString == "")
        {
            GameObject.Destroy(ComparisonTT);
            ComparisonTT = null;
            ComparisonAdded = false;
        }

        // Build a comparison tooltip if we are requested to show one
        if (ComparisonTooltipString != "" && ComparisonAdded != true)
        {
            // Ensure the old tooltip is removed
            if (ComparisonTT != null)
            {
                GameObject.Destroy(ComparisonTT);
            }

            ComparisonTT = GameObject.Instantiate(UITooltip.m_tooltip, UITooltip.m_tooltip.transform);

            Transform scrollT = Utils.FindChild(ComparisonTT.transform, "Canvas");
            RectTransform scrollRT = scrollT.GetComponent<RectTransform>();
            Transform header = Utils.FindChild(ComparisonTT.transform, "Topic");
            header.GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize(ComparisonTitleString);
            Transform contentt = Utils.FindChild(ComparisonTT.transform, "Text");
            contentt.GetComponent<TextMeshProUGUI>().text = Localization.instance.Localize(ComparisonTooltipString);

            // Offset the comparision tooltip to the right of the original tooltip
            RectTransform tooltipTfm = (RectTransform)UITooltip.m_tooltip.transform;
            Vector3[] compareCorners = new Vector3[4];
            Vector3[] tooltipCorners = new Vector3[4];
            RectTransform tooltipTransform = Utils.FindChild(UITooltip.m_tooltip.transform, "Canvas").GetComponent<RectTransform>();
            tooltipTransform.GetWorldCorners(tooltipCorners);
            scrollRT.GetWorldCorners(compareCorners);
            scrollRT.anchoredPosition = new Vector2(tooltipTfm.anchoredPosition.x + MAX_WIDTH, tooltipTfm.anchoredPosition.y);

            // Offset calculation is needed to adjust the two canvases since they will have different heights
            float xoffset = Mathf.Abs(tooltipCorners[0].y - tooltipCorners[1].y);
            scrollRT.position = new Vector3(tooltipTransform.position.x + (xoffset / 2) + 5f, tooltipTransform.position.y, 0);
            ComparisonAdded = true;
        }
    }

    /// <summary>
    /// Add a scrollbar to the Tooltip on hover start
    /// with a set size to prevent sizing spasmings on screen.
    /// </summary>
    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnHoverStart))]
    [HarmonyPostfix]
    public static void Postfix(GameObject go)
    {
        if (UITooltip.m_tooltip != null)
        {
            RectTransform transform = (RectTransform)go.transform;
            AddScrollbar(UITooltip.m_tooltip, transform);
        }
    }

    public static void AddScrollbar(GameObject tooltipObject, RectTransform hoverTransform)
    {
        if (tooltipObject == null)
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
            tooltipObject.transform, false, true, PADDING, PADDING,
            GUIManager.Instance.ValheimScrollbarHandleColorBlock, Color.grey, MAX_WIDTH, MAX_HEIGHT);

        // Hide the scrollbar by default, it will show when needed
        scrollArea.GetComponentInChildren<Scrollbar>().gameObject.SetActive(false);

        Transform contentt = Utils.FindChild(tooltipObject.transform, "Content");
        Transform tooltipTextTransform = Utils.FindChild(tooltipObject.transform, "Text");
        header.SetParent(contentt, false);
        tooltipTextTransform.SetParent(contentt, false);

        Transform scrolltform = Utils.FindChild(tooltipObject.transform, "Scroll View");
        // Scroll sensitivity fix for combat update
        ScrollRect scrollRect = scrolltform.GetComponent<ScrollRect>();
        scrollRect.scrollSensitivity = 800;

        // Copy the existing background from the header tooltip section to the content of the scrollview
        // Set the header section to match the width of the scroll area
        Transform bkgtform = tooltipObject.transform.Find("Bkg");
        if (bkgtform != null)
        {
            Image backgroundImage = bkgtform.GetComponent<Image>();
            Image contentbkgImage = contentt.gameObject.AddComponent<Image>();
            contentbkgImage.color = backgroundImage.color;
            contentbkgImage.sprite = backgroundImage.sprite;
            contentbkgImage.type = backgroundImage.type;
            contentbkgImage.raycastTarget = false;
            // Remove the header background as it is no longer needed
            GameObject.Destroy(bkgtform.gameObject);
        }

        // Add a little padding to the viewport so text isn't jammed against the edge
        VerticalLayoutGroup vlg = contentt.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset((int)PADDING, 0, 0, 0);

        // Add the scrollbar handler to allow mouse wheel scrolling while not hovering over the scrollbar
        scrolltform.gameObject.AddComponent<ScrollWheelHandler>();

        // Adjust the location of the tooltip otherwise it will be on the cursor
        GetOffsets(hoverTransform, out float xoffset, out float yoffset);
        RectTransform scrollRT = scrollArea.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 1);
        scrollRT.anchorMax = new Vector2(0, 1);
        scrollRT.anchoredPosition = new Vector2(xoffset, yoffset);

        RectTransform contentRT = contentt.GetComponent<RectTransform>();
        contentRT.offsetMax = new Vector2(MAX_WIDTH + PADDING, 0); // Expand area so scrollbar isn't floating
        RectTransform textRT = tooltipTextTransform.GetComponent<RectTransform>();
        textRT.offsetMax = new Vector2(MAX_WIDTH - 25f, -22); // Expand text to get more of the full area
        header.GetComponent<RectTransform>().offsetMax = new Vector2(210, 0); // Expand title to get more of the full area
    }

    private static void GetOffsets(RectTransform transform, out float xoffset, out float yoffset)
    {
        float width = (MAX_WIDTH / 2f) + PADDING;
        // If on left side of screen display tooltip on right, right side on left.
        xoffset = transform.position.x > (Screen.width / 2f) ? -width : width + CURSOR_PADDING;
        yoffset = MAX_HEIGHT / -2f;
    }
}

/// <summary>
/// The following class is largely taken from Azumatt's Tooltip Expansion mod:
/// https://github.com/AzumattDev/TooltipExpansion/blob/main/CodeNShit/Monos/TooltipSizeAdjuster.cs
/// </summary>
public class ScrollWheelHandler : MonoBehaviour
{
    private ScrollRect _scrollRect = null;

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
}
