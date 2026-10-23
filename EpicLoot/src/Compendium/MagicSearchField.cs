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

    private readonly bool _hasGlow;

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
        // The repair button's glow; under Auga m_crafting is the vanilla panel Auga destroyed, and the
        // compendium can be built before the inventory exists at all. Without it there is no glow.
        Transform crafting = InventoryGui.instance != null ? InventoryGui.instance.m_crafting : null;
        Image craftGlow = crafting != null ? crafting.Find("RepairButton/Glow")?.GetComponent<Image>() : null;
        _hasGlow = craftGlow != null;
        if (_hasGlow)
        {
            _glow.sprite = craftGlow.sprite;
            _glow.type = craftGlow.type;
            _glow.color = craftGlow.color;
            _glow.material = craftGlow.material;
        }

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
        _placeholder.text = Localization.instance.Localize("$mod_epicloot_search_placeholder");
        _placeholder.color = Color.gray;
        Input.placeholder = _placeholder;
        _placeholder.alignment = TextAnchor.MiddleLeft;
    }

    public void EnableGlow(bool enable) => _glow.enabled = enable && _hasGlow;

    /// <summary>Centres the field along the top edge of its parent, <paramref name="margin"/> below it.</summary>
    public void DockTop(float width, float height, float margin, float xOffset = 0f)
    {
        _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 1f);
        _rect.pivot = new Vector2(0.5f, 1f);
        _rect.localScale = Vector3.one;
        _rect.localRotation = Quaternion.identity;
        _rect.anchoredPosition3D = new Vector3(xOffset, -margin, 0f);
        SetSize(width, height);
    }

    public void SetBackgroundColor(Color color)
    {
        _background.sprite = null;
        _background.color = color;
    }

    public void SetPosition(Vector2 pos) => _rect.position = pos;

    public void Dock(Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition)
    {
        _rect.anchorMin = _rect.anchorMax = anchor;
        _rect.pivot = pivot;
        _rect.localScale = Vector3.one;
        _rect.localRotation = Quaternion.identity;
        _rect.anchoredPosition3D = new Vector3(anchoredPosition.x, anchoredPosition.y, 0f);
    }

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