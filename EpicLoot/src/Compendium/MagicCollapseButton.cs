using EpicLoot_UnityLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EpicLoot.Compendium;

public class MagicCollapseButton
{
    private const string GamepadKey = "JoyRStick";
    private const float HintSize = 22f;
    private const float HintMargin = 4f;

    private readonly GameObject _obj;
    private readonly RectTransform _rect;
    private readonly Image _background;
    private readonly Button _button;
    private readonly TextMeshProUGUI _label;
    private readonly TextMeshProUGUI _hint;
    private bool _hintShown;

    public MagicCollapseButton(Transform parent, UnityAction onClick)
    {
        _obj = new GameObject("collapseAll", typeof(RectTransform));
        _rect = (RectTransform)_obj.transform;
        _rect.SetParent(parent, false);
        _rect.localScale = Vector3.one;
        _background = _obj.AddComponent<Image>();
        _button = _obj.AddComponent<Button>();
        _button.targetGraphic = _background;
        _button.navigation = new Navigation { mode = Navigation.Mode.None };
        _button.onClick.AddListener(onClick);

        _label = CreateText("label", MagicPages.FONT_SIZE, TextAlignmentOptions.Center);
        RectTransform labelRect = _label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);

        _hint = CreateText(GamepadKey, 16, TextAlignmentOptions.Center);
        RectTransform hintRect = _hint.rectTransform;
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 0.5f);
        hintRect.sizeDelta = new Vector2(HintSize, HintSize);
        hintRect.anchoredPosition = new Vector2(-HintMargin, HintMargin);
        // Fills the glyph from ZInput each time the hint is shown and whenever the pad layout changes while
        // it is, so it follows the connected pad without asking ZInput every frame.
        _hint.gameObject.AddComponent<GamepadGlyph>().ZInputKey = GamepadKey;
        _hint.gameObject.SetActive(false);
    }

    private TextMeshProUGUI CreateText(string name, int size, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(_rect, false);
        go.transform.localScale = Vector3.one;
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        MagicFontManager.Apply(text, MagicFontManager.TMP_FontOptions.AveriaSerifLibreOutline);
        text.fontSize = size;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        text.richText = true;
        return text;
    }

    public void SetBackground(Image source)
    {
        _background.sprite = source.sprite;
        _background.material = source.material;
        _background.color = source.color;
        _background.type = source.type;
    }

    public void SetBackgroundColor(Color color)
    {
        _background.sprite = null;
        _background.color = color;
    }

    public void SetPosition(Vector2 worldPosition) => _rect.position = worldPosition;

    public void Dock(Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition)
    {
        _rect.anchorMin = _rect.anchorMax = anchor;
        _rect.pivot = pivot;
        _rect.anchoredPosition3D = new Vector3(anchoredPosition.x, anchoredPosition.y, 0f);
    }

    public void SetSize(float width, float height) => _rect.sizeDelta = new Vector2(width, height);

    public void Enable(bool enable) => _obj.SetActive(enable);

    public bool IsEnabled => _obj.activeInHierarchy;

    public void SetCollapsed(bool collapsed)
    {
        _label.text = Localization.instance.Localize(collapsed ? "$mod_epicloot_expand_all" : "$mod_epicloot_collapse_all");
    }

    public void ShowHint(bool gamepadActive)
    {
        if (_hintShown != gamepadActive)
        {
            _hintShown = gamepadActive;
            _hint.gameObject.SetActive(gamepadActive);
        }
    }
}
