using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace EpicLoot.Compendium;

// One line of a compendium page.
//
// TextMeshPro rather than legacy UnityEngine.UI.Text: TMP is what the vanilla dialog this panel is
// cloned from uses, and only TMP resolves the <sprite="..."> tags that ShardTooltipSprites builds --
// which is what lets the shardstone page show a stone's real inventory icon inline with its name.
// The outline that legacy Text got from an Outline component comes from the font's "- Outline"
// material variant here; both materials are shared, and per-line colour is a vertex colour, so no
// material instances are created.
public class MagicTextElement
{
    private readonly GameObject _obj;
    private readonly RectTransform _rect;
    private readonly TextMeshProUGUI _text;

    // Rich-text markup stripped and lower-cased once at Set() time, and matched against instead of the
    // raw string. Without this a query for "color", "size" or a hex fragment matches every line on the
    // page, since every line carries <color=...>/<size=...> tags. Lower-casing here also keeps the
    // per-keystroke filter allocation-free.
    private static readonly Regex RichTextTag = new Regex("<[^>]*>", RegexOptions.Compiled);
    private string _searchText = string.Empty;

    public MagicTextElement(Transform parent)
    {
        _obj = new GameObject("text");
        _obj.SetActive(false);
        _rect = _obj.AddComponent<RectTransform>();
        _rect.sizeDelta = new Vector2(MagicPages.instance.MinWidth - 10f, 35f);
        _rect.SetParent(parent);
        _rect.localScale = Vector3.one;

        _text = _obj.AddComponent<TextMeshProUGUI>();
        ApplyFont(MagicFontManager.TMP_FontOptions.AveriaSerifLibre);
        _text.fontSize = MagicPages.FONT_SIZE;
        _text.richText = true;
        _text.enableWordWrapping = true;
        _text.overflowMode = TextOverflowModes.Overflow;
        _text.alignment = TextAlignmentOptions.TopLeft;
    }

    private MagicTextElement(GameObject source)
    {
        _obj = source;
        _rect = _obj.GetComponent<RectTransform>();
        _text = _obj.GetComponent<TextMeshProUGUI>();
        Enable(true);
    }

    // Goes through MagicFontManager rather than assigning fontSharedMaterial directly: TMP only
    // validates a shared material against the font's current atlas inside LoadFontAsset(), which the
    // setter does not run.
    private void ApplyFont(MagicFontManager.TMP_FontOptions option) =>
        MagicFontManager.Apply(_text, option);

    public void Resize()
    {
        float newHeight = GetTextPreferredHeight(_text, _rect);
        _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, Mathf.Max(newHeight, 35f));
    }

    private static float GetTextPreferredHeight(TMP_Text text, RectTransform rect)
    {
        if (string.IsNullOrEmpty(text.text))
        {
            return 0f;
        }

        // Measured against the element's own width rather than the laid-out width: the vertical layout
        // group has not run yet when a page is built, so rect.rect.width is still the width the template
        // was created at -- which is the width the group will hand back anyway.
        return text.GetPreferredValues(text.text, rect.rect.width, 0f).y;
    }

    public float GetHeight() => _rect.sizeDelta.y;

    // Expects an already-lower-cased query -- MagicTextGroup lowers it once for the whole group.
    public bool IsMatch(string query) => _searchText.Contains(query);

    public void SetFontSize(int size) => _text.fontSize = size;

    private void Set(string line)
    {
        _text.text = Localization.instance.Localize(line);
        _searchText = RichTextTag.Replace(_text.text, string.Empty).ToLowerInvariant();
        Resize();
    }

    public void SetParent(Transform parent) => _rect.SetParent(parent);
    public void Destroy() => UnityEngine.Object.Destroy(_obj);
    public void Enable(bool enable) => _obj.SetActive(enable);

    // No re-measure needed: TMP folds material padding into GenerateTextMesh's vertex/UV math only,
    // never into CalculatePreferredValues, so the height Set() computed holds across the swap.
    public void EnableOutline(bool enable) => ApplyFont(enable
        ? MagicFontManager.TMP_FontOptions.AveriaSerifLibreOutline
        : MagicFontManager.TMP_FontOptions.AveriaSerifLibre);

    public void SetSize(float width, float height) => _rect.sizeDelta = new Vector2(width, height);
    public void SetColor(Color color) => _text.color = color;

    // Kept on the legacy TextAnchor vocabulary so the call sites read the same as the rest of the UI code.
    public void SetAlignment(TextAnchor alignment) => _text.alignment = ToTMPAlignment(alignment);

    private static TextAlignmentOptions ToTMPAlignment(TextAnchor alignment)
    {
        switch (alignment)
        {
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            default: return TextAlignmentOptions.TopLeft;
        }
    }

    public MagicTextElement Create(string line, Transform parent)
    {
        GameObject go = GameObject.Instantiate(_obj, parent);
        MagicTextElement element = new MagicTextElement(go);
        element.Set(line);
        return element;
    }
}
