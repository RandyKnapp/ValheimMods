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

    public const int HEADER_FONT_SIZE = 40;
    public const int LARGE_FONT_SIZE = 24;
    public const int MEDIUM_FONT_SIZE = 20;
    public const int FONT_SIZE = 18;

    public float MinWidth { get; private set; }
    public float MinHeight { get; private set; }

    public MagicSearchField Search;
    public MagicTextList MagicPagesTextArea;
    public GameObject compendiumTextArea;
    [CanBeNull] public MagicTextElement TitleElement;

    private bool wasGlowing;

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
        
        instance = this;
        
        Transform frame = transform.Find("Texts_frame");
        Image closeButtonImg = frame.Find("Closebutton").GetComponent<Image>();
        Button closeButton = closeButtonImg.GetComponent<Button>();
        RectTransform closeButtonRect = closeButtonImg.GetComponent<RectTransform>();
        compendiumTextArea = frame.Find("TextArea").gameObject;
        RectTransform textAreaRect = compendiumTextArea.GetComponent<RectTransform>();
        MinWidth = textAreaRect.rect.width;
        MinHeight = textAreaRect.rect.height;
        Search = new MagicSearchField(frame);

        // Calculate the Seach bar position
        float spacing = 30f;
        float buttonEdge = closeButtonRect.position.x + (closeButtonRect.rect.width / 2); // Button is centered
        float boxEdge = textAreaRect.position.x; // Box is anchored to the bottom right
        float width = boxEdge - buttonEdge - spacing;
        float height = closeButtonRect.rect.height;
        Vector3 position = new Vector3(buttonEdge + (width / 2) + spacing, closeButtonRect.position.y);
        Search.SetPosition(position);
        Search.SetSize(width, height);

        Search.SetBackground(closeButtonImg);
        Search.SetBackground(closeButton.spriteState.disabledSprite);
        Search.SetFont(MagicFontManager.GetFont(MagicFontManager.FontOptions.AveriaSerifLibre));
        Search.Input.onValueChanged.AddListener(OnSearch);
        
        MagicPagesTextArea = new MagicTextList(compendiumTextArea, frame);
        Reset();
    }

    public void Update()
    {
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

    public static bool InSearchField()
    {
        if (instance == null || instance.Search == null)
        {
            return false;
        }

        return instance.Search.Input.isFocused;
    }

    public void OnSearch(string query)
    {
        foreach (MagicTextGroup element in MagicPagesTextArea.Elements)
        {
            element.Enable(element.IsMatch(query.Trim()));
        }
    }

    public void Reset()
    {
        compendiumTextArea.SetActive(true);
        Search.Enable(false);
        MagicPagesTextArea.Enable(false);
        MagicPagesTextArea.Clear();
        TitleElement?.Destroy();
    }

    public void OnSelectText(MagicTextInfo text)
    {
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
    }
}
