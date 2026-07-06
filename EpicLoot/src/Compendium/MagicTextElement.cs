using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Compendium;

public class MagicTextElement
{
    private readonly GameObject _obj;
    private readonly RectTransform _rect;
    private readonly Text _text;
    private readonly Outline _outline;

    public MagicTextElement(Transform parent)
    {
        _obj = new GameObject("text");
        _obj.SetActive(false);
        _rect = _obj.AddComponent<RectTransform>();
        _rect.sizeDelta = new Vector2(MagicPages.instance.MinWidth - 10f, 35f);
        _rect.SetParent(parent);
        _rect.localScale = Vector3.one;

        _text = _obj.AddComponent<Text>();
        _outline = _obj.AddComponent<Outline>();
        _outline.enabled = false;
        _text.horizontalOverflow = HorizontalWrapMode.Wrap;
        _text.verticalOverflow = VerticalWrapMode.Truncate;
        _text.font = MagicFontManager.GetFont(MagicFontManager.FontOptions.AveriaSerifLibre);
        _text.fontSize = MagicPages.FONT_SIZE;
        _text.supportRichText = true;
    }

    private MagicTextElement(GameObject source)
    {
        _obj = source;
        _rect = _obj.GetComponent<RectTransform>();
        _text = _obj.GetComponent<Text>();
        _outline = _obj.GetComponent<Outline>();
        Enable(true);
    }
    
    public void Resize()
    {
        float newHeight = GetTextPreferredHeight(_text, _rect);
        _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, Mathf.Max(newHeight, 35f));
    }
    
    private static float GetTextPreferredHeight(Text text, RectTransform rect)
    {
        if (string.IsNullOrEmpty(text.text))
        {
            return 0f;
        }
        TextGenerator textGen = text.cachedTextGenerator;
        TextGenerationSettings settings = text.GetGenerationSettings(rect.rect.size);
        float preferredHeight = textGen.GetPreferredHeight(text.text, settings);
        return preferredHeight;
    }

    public float GetHeight() => _rect.sizeDelta.y;

    public bool IsMatch(string query)
    {
        return _text.text.ToLower().Contains(query.ToLower());
    }

    public void SetFont(Font font) => _text.font = font;
    public void SetFontSize(int size) => _text.fontSize = size;

    private void Set(string line)
    {
        _text.text = Localization.instance.Localize(line);
        Resize();
    }
    public void SetParent(Transform parent) => _rect.SetParent(parent);
    public void Destroy() => UnityEngine.Object.Destroy(_obj);
    public void Enable(bool enable) => _obj.SetActive(enable);
    public void EnableOutline(bool enable) => _outline.enabled = enable;
    public void SetSize(float width, float height) => _rect.sizeDelta = new Vector2(width, height);
    public void SetColor(Color color) => _text.color = color;
    public void SetAlignment(TextAnchor alignment) => _text.alignment = alignment;

    public MagicTextElement Create(string line, Transform parent)
    {
        GameObject go = GameObject.Instantiate(_obj, parent);
        MagicTextElement element = new MagicTextElement(go);
        element.Set(line);
        return element;
    }
}
