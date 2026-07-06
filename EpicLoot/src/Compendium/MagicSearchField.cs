using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Compendium;

public class MagicSearchField
{
    private readonly GameObject _obj;
    private readonly RectTransform _rect;
    private readonly Text _placeholder;
    private readonly Image _background;
    private readonly Image _glow;
    private readonly RectTransform _textRect;
    private readonly RectTransform _placeholderRect;

    public readonly InputField Input;

    private const float WIDTH_PADDING = 20f;

    public MagicSearchField(Transform parent)
    {
        _obj = new GameObject("searchField");
        _rect = _obj.AddComponent<RectTransform>();
        _rect.localScale = Vector3.one;
        _obj.transform.SetParent(parent);
        _background = _obj.AddComponent<Image>();
        Input = _obj.AddComponent<InputField>();
        Input.targetGraphic = _background;

        _glow = new GameObject("glow").AddComponent<Image>();
        var craftGlow = InventoryGui.instance.m_crafting.Find("RepairButton/Glow").GetComponent<Image>();
        _glow.sprite = craftGlow.sprite;
        _glow.type = craftGlow.type;
        _glow.color = craftGlow.color;
        _glow.material = craftGlow.material;
        _glow.rectTransform.SetParent(_rect);
        _glow.rectTransform.localScale = Vector3.one;
        _glow.enabled = false;

        GameObject text = new GameObject("Text");
        _textRect = text.AddComponent<RectTransform>();
        text.transform.SetParent(_obj.transform);
        Input.textComponent = text.AddComponent<Text>(); ;
        Input.textComponent.alignment = TextAnchor.MiddleLeft;

        GameObject placeholderObj = new GameObject("Placeholder");
        _placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderObj.transform.SetParent(_obj.transform);
        _placeholder = placeholderObj.AddComponent<Text>();
        //TODO: localize search text
        _placeholder.text = "Search...";
        _placeholder.color = Color.gray;
        Input.placeholder = _placeholder;
        _placeholder.alignment = TextAnchor.MiddleLeft;
    }

    public void EnableGlow(bool enable) => _glow.enabled = enable;

    public void SetPosition(Vector2 pos) => _rect.position = pos;

    public void SetSize(float x, float y)
    {
        _rect.sizeDelta = new Vector2(x, y);
        _glow.rectTransform.sizeDelta = new Vector2(x, y);
        _textRect.sizeDelta = new Vector2(x - WIDTH_PADDING, y);
        _placeholderRect.sizeDelta = new Vector2(x - WIDTH_PADDING, y);
    }

    public void SetBackground(Image source)
    {
        _background.sprite = source.sprite;
        _background.material = source.material;
        _background.color = source.color;
        _background.type = source.type;
    }

    public void SetBackground(Sprite sprite)
    {
        _background.sprite = sprite;
    }

    public void SetFont(Font font)
    {
        Input.textComponent.font = font;
        _placeholder.font = font;
    }

    public void Enable(bool enable) => _obj.SetActive(enable);
}