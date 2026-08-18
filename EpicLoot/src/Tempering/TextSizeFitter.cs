using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

public class TextSizeFitter : MonoBehaviour
{
    private VerticalLayoutGroup _layout;
    private RectTransform _root;
    private TextMeshProUGUI _text;
    public Scrollbar _scrollbar;
    public string text => _text.text;
    public bool autoScrollToBottom = false;

    public void Awake()
    {
        _layout = GetComponent<VerticalLayoutGroup>();
        _root = GetComponent<RectTransform>();
        _text = GetComponentInChildren<TextMeshProUGUI>();
        _scrollbar = transform.parent.GetComponentInChildren<Scrollbar>();
    }

    public void SetText(string txt)
    {
        _text.text = txt;
        float preferredHeight = _text.preferredHeight;
        _text.rectTransform.sizeDelta = new Vector2(_text.rectTransform.sizeDelta.x, preferredHeight);
        _root.sizeDelta = new Vector2(_root.sizeDelta.x, preferredHeight + _layout.padding.top + _layout.padding.bottom);
        if (autoScrollToBottom) _scrollbar.value = 0;
    }
}