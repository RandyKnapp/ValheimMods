using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Compendium;

public class MagicPages : MonoBehaviour
{
    public ExplainTextInfo ExplainPage;
    public TreasureBountyTextInfo TreasureBountyPage;
    public MagicEffectTextInfo MagicEffectsPage;
    public SetInfo SetInfos;
    public ShardStoneTextInfo ShardStonePage;

    public const int HEADER_FONT_SIZE = 40;
    public const int LARGE_FONT_SIZE = 24;
    public const int MEDIUM_FONT_SIZE = 20;
    public const int FONT_SIZE = 18;

    private const float GAMEPAD_SCROLL_SPEED = 1000f;
    private const float GAMEPAD_STICK_DEADZONE = 0.5f;

    public float MinWidth { get; private set; }
    public float MinHeight { get; private set; }

    public MagicSearchField Search;
    public MagicCollapseButton CollapseAll;

    /// <summary>Whether table rows start folded; kept across pages and dialog opens so the choice sticks.</summary>
    public static bool Collapsed;

    private const float COLLAPSE_BUTTON_WIDTH = 150f;
    private const float COLLAPSE_BUTTON_SPACING = 10f;
    public MagicTextList MagicPagesTextArea;
    public GameObject compendiumTextArea;
    [CanBeNull] public MagicTextElement TitleElement;

    private bool wasGlowing;

    /// <summary>The dialog had the parts the pages are built into; without them this component does nothing.</summary>
    public bool IsReady { get; private set; }

    // Auga's compendium dialogs have no close button to line the search bar up with, so it runs along the
    // top of Epic Loot's text area and the page content starts below it.
    private const float AUGA_SEARCH_BAR_HEIGHT = 32f;
    private const float AUGA_SEARCH_BAR_MARGIN = 8f;

    /// <summary>The dialog whose pages are being built or shown; the text list and elements size themselves from it.</summary>
    public static MagicPages instance;

    public void Awake()
    {
        ExplainPage = new ExplainTextInfo(Localization.instance.Localize(
            $"{EpicLoot.GetMagicEffectPip(false)} $mod_epicloot_me_explaintitle"));
        TreasureBountyPage = new TreasureBountyTextInfo(Localization.instance.Localize(
            $"{EpicLoot.GetMagicEffectPip(false)} $mod_epicloot_adventure_title"));
        MagicEffectsPage = new MagicEffectTextInfo(Localization.instance.Localize(
            $"{EpicLoot.GetMagicEffectPip(false)} $mod_epicloot_active_magic_effects"));
        SetInfos = new SetInfo(Localization.instance.Localize(
            $"{EpicLoot.GetMagicEffectPip(false)} $mod_epicloot_legendary_sets"));
        ShardStonePage = new ShardStoneTextInfo(Localization.instance.Localize(
            $"{EpicLoot.GetMagicEffectPip(false)} $mod_epicloot_shardstones_title"));
        
        // The vanilla dialog keeps its parts in Texts_frame. Auga's compendium dialogs (AugaTextsDialog) have no
        // frame: the text area, in the same TextArea/ScrollArea/Content shape, sits under the root, and there
        // is no close button.
        Transform frame = transform.Find("Texts_frame");
        bool augaDialog = frame == null;
        if (augaDialog)
        {
            frame = transform;
        }

        Transform textArea = frame.Find("TextArea");
        if (textArea == null || textArea.Find("ScrollArea/Content") == null)
        {
            EpicLoot.LogWarning($"Compendium: {name} has no TextArea/ScrollArea/Content; Epic Loot's pages are left out of it.");
            return;
        }

        instance = this;

        compendiumTextArea = textArea.gameObject;
        RectTransform textAreaRect = compendiumTextArea.GetComponent<RectTransform>();
        MinWidth = textAreaRect.rect.width;
        MinHeight = textAreaRect.rect.height;

        if (augaDialog)
        {
            MagicPagesTextArea = new MagicTextList(compendiumTextArea, frame);
            MagicPagesTextArea.SetTopPadding(Mathf.RoundToInt(AUGA_SEARCH_BAR_HEIGHT + 2 * AUGA_SEARCH_BAR_MARGIN));
            float rowWidth = Mathf.Max(MinWidth - 40f, 200f);
            float searchWidth = rowWidth - COLLAPSE_BUTTON_WIDTH - COLLAPSE_BUTTON_SPACING;
            Search = new MagicSearchField(MagicPagesTextArea.Root);
            Search.DockTop(searchWidth, AUGA_SEARCH_BAR_HEIGHT, AUGA_SEARCH_BAR_MARGIN,
                -(COLLAPSE_BUTTON_WIDTH + COLLAPSE_BUTTON_SPACING) / 2f);
            Search.SetBackgroundColor(new Color(0f, 0f, 0f, 0.5f));
            CollapseAll = new MagicCollapseButton(MagicPagesTextArea.Root, ToggleCollapsed);
            CollapseAll.Dock(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(rowWidth / 2f - COLLAPSE_BUTTON_WIDTH / 2f, -AUGA_SEARCH_BAR_MARGIN));
            CollapseAll.SetSize(COLLAPSE_BUTTON_WIDTH, AUGA_SEARCH_BAR_HEIGHT);
            CollapseAll.SetBackgroundColor(new Color(0f, 0f, 0f, 0.5f));
            Search.SetFont(MagicFontManager.GetFont(MagicFontManager.FontOptions.AveriaSerifLibre));
            Search.Input.onValueChanged.AddListener(OnSearch);
            IsReady = true;
            Reset();
            return;
        }

        Image closeButtonImg = frame.Find("Closebutton").GetComponent<Image>();
        Button closeButton = closeButtonImg.GetComponent<Button>();
        RectTransform closeButtonRect = closeButtonImg.GetComponent<RectTransform>();
        Search = new MagicSearchField(frame);

        // Everything below is measured in the frame's own space: the close button's world position cannot be
        // mixed with rect widths, which are in canvas units. The search bar runs from the close button to the
        // text area; the collapse toggle goes left of the close button, narrower than it so it clears the topic
        // list, or between the close button and the search bar when that gap is too small.
        float spacing = 16f;
        RectTransform frameRect = (RectTransform)frame;
        Vector2 frameOrigin = frameRect.rect.center;
        Vector3 closeCenter = frameRect.InverseTransformPoint(closeButtonRect.TransformPoint(closeButtonRect.rect.center));
        float closeWidth = closeButtonRect.rect.width;
        float height = closeButtonRect.rect.height;
        float closeLeft = closeCenter.x - closeWidth / 2f;
        float closeRight = closeCenter.x + closeWidth / 2f;
        float boxLeft = frameRect.InverseTransformPoint(textAreaRect.TransformPoint(new Vector3(textAreaRect.rect.xMin, 0f, 0f))).x;
        float listRight = frameRect.rect.xMin;
        RectTransform listRoot = GetComponent<TextsDialog>()?.m_listRoot;
        if (listRoot != null)
        {
            RectTransform listRect = listRoot.parent as RectTransform ?? listRoot;
            listRight = frameRect.InverseTransformPoint(listRect.TransformPoint(new Vector3(listRect.rect.xMax, 0f, 0f))).x;
        }

        float toggleWidth = Mathf.Min(closeWidth * 0.6f, closeLeft - listRight - 2f * spacing);
        float toggleCenterX;
        float searchLeft = closeRight + spacing;
        if (toggleWidth >= 90f)
        {
            toggleCenterX = closeLeft - spacing - toggleWidth / 2f;
        }
        else
        {
            toggleWidth = closeWidth * 0.6f;
            toggleCenterX = closeRight + spacing + toggleWidth / 2f;
            searchLeft = toggleCenterX + toggleWidth / 2f + spacing;
        }

        CollapseAll = new MagicCollapseButton(frame, ToggleCollapsed);
        CollapseAll.SetBackground(closeButtonImg);
        CollapseAll.SetSize(toggleWidth, height);
        CollapseAll.Dock(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(toggleCenterX - frameOrigin.x, closeCenter.y - frameOrigin.y));

        float searchRight = boxLeft - spacing;
        float width = Mathf.Max(searchRight - searchLeft, 120f);
        Search.Dock(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(searchLeft + width / 2f - frameOrigin.x, closeCenter.y - frameOrigin.y));
        Search.SetSize(width, height);

        Search.SetBackground(closeButtonImg);
        Search.SetBackground(closeButton.spriteState.disabledSprite);
        Search.SetFont(MagicFontManager.GetFont(MagicFontManager.FontOptions.AveriaSerifLibre));
        Search.Input.onValueChanged.AddListener(OnSearch);
        
        MagicPagesTextArea = new MagicTextList(compendiumTextArea, frame);
        IsReady = true;
        Reset();
    }

    public void Update()
    {
        if (!IsReady)
        {
            return;
        }

        UpdateGamepadScroll();
        UpdateCollapseButton();

        //  makes search field glow when focused
        if (wasGlowing && !InSearchField())
        {
            Search.EnableGlow(false);
            wasGlowing = false;
        }
        else if (!wasGlowing && InSearchField())
        {
            Search.EnableGlow(true);
            wasGlowing = true;
        }
    }

    private void UpdateCollapseButton()
    {
        if (CollapseAll == null || !CollapseAll.IsEnabled)
        {
            return;
        }

        bool gamepad = ZInput.IsGamepadActive();
        CollapseAll.ShowHint(gamepad);
        if (gamepad && MagicPagesTextArea.IsEnabled && ZInput.GetButtonDown("JoyRStick"))
        {
            ToggleCollapsed();
        }
    }

    public void ToggleCollapsed()
    {
        Collapsed = !Collapsed;
        CollapseAll?.SetCollapsed(Collapsed);
        MagicPagesTextArea.SetAllExpanded(!Collapsed);
    }

    private void UpdateGamepadScroll()
    {
        if (!MagicPagesTextArea.IsEnabled || !ZInput.IsGamepadActive())
        {
            return;
        }

        float rightStickAxis = ZInput.GetJoyRightStickY();
        if (Mathf.Abs(rightStickAxis) > GAMEPAD_STICK_DEADZONE)
        {
            MagicPagesTextArea.ScrollBy(-rightStickAxis * GAMEPAD_SCROLL_SPEED * Time.unscaledDeltaTime);
        }
    }

    public static bool InSearchField()
    {
        if (instance == null || !instance.IsReady || instance.Search == null)
        {
            return false;
        }

        return instance.Search.Input.isFocused;
    }

    public void OnSearch(string query)
    {
        foreach (MagicTextGroup element in MagicPagesTextArea.Elements)
        {
            element.Filter(query.Trim());
        }
    }

    public void Reset()
    {
        if (!IsReady)
        {
            return;
        }

        // One retry per open for anything that missed because its asset was not loaded yet; the lookup
        // scans every loaded object, so it must not run per line.
        MagicFontManager.RetryFailedLookups();

        compendiumTextArea.SetActive(true);
        Search.Enable(false);
        CollapseAll?.Enable(false);
        MagicPagesTextArea.Enable(false);
        MagicPagesTextArea.Clear();
        TitleElement?.Destroy();
    }

    public void OnSelectText(MagicTextInfo text)
    {
        if (!IsReady)
        {
            return;
        }

        instance = this;
        compendiumTextArea.SetActive(false);
        MagicPagesTextArea.Enable(true);
        Search.Enable(text.ShowSearchBar);
        Search.Input.SetTextWithoutNotify(string.Empty);
        
        TitleElement = MagicPagesTextArea.Create(text.m_topic);
        TitleElement!.SetSize(MinWidth, 100f);
        TitleElement.SetFontSize(HEADER_FONT_SIZE);
        TitleElement.SetColor(new Color(1f, 0.65f, 0.15f));
        TitleElement.SetAlignment(TextAnchor.MiddleCenter);
        
        text.Build(this);
        MagicPagesTextArea.ResizeOverlay();
        if (CollapseAll != null)
        {
            CollapseAll.SetCollapsed(Collapsed);
            CollapseAll.Enable(MagicPagesTextArea.HasTables);
        }
    }
}
