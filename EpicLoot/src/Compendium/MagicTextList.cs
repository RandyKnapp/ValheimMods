using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Compendium;

public class MagicTextList
{
    private readonly GameObject _obj;
    private readonly Image _background;
    private readonly ScrollRect _scrollRect;
    private readonly VerticalLayoutGroup _layout;
    private readonly MagicTextElement _template;
    private readonly RectTransform _overlayRect;

    public readonly List<MagicTextGroup> Elements = new();

    public MagicTextList(GameObject source, Transform parent)
    {
        _obj = GameObject.Instantiate(source, parent);
        _obj.name = "EpicLootMagicTextArea";
        _background = _obj.GetComponent<Image>();
        _scrollRect = _obj.transform.Find("ScrollArea").GetComponent<ScrollRect>();
        _layout = _obj.transform.Find("ScrollArea/Content").GetComponent<VerticalLayoutGroup>();
        _layout.childAlignment = TextAnchor.UpperLeft;
        _layout.childControlWidth = true;
        _layout.childControlHeight = false;
        _layout.childForceExpandWidth = true;
        _layout.padding.left = 5;
        _layout.spacing = 0;

        for (int i = 0; i < _layout.transform.childCount; ++i)
        {
            var child = _layout.transform.GetChild(i).gameObject;
            GameObject.Destroy(child);
        }

        // Add overlay to have something to raycast to scroll smoothly between gaps
        GameObject overlay = new GameObject("overlay");
        overlay.AddComponent<LayoutElement>().ignoreLayout = true;
        _overlayRect = overlay.GetComponent<RectTransform>();
        _overlayRect.SetParent(_layout.transform);
        overlay.AddComponent<Image>().color = Color.clear;
        _overlayRect.sizeDelta = new Vector2(MagicPages.instance.MinWidth, MagicPages.instance.MinHeight);
        _overlayRect.anchoredPosition = Vector2.zero;

        _template = new MagicTextElement(_layout.transform);
    }

    public void ResizeOverlay()
    {
        float total = MagicPages.instance.TitleElement?.GetHeight() ?? 0f;
        total += Elements.Sum(x => x.Title.GetHeight() + x.Content.Sum(y => y.GetHeight()));
        SetOverlayHeight(total);
    }

    private void SetOverlayHeight(float height) =>
        _overlayRect.sizeDelta = new Vector2(_overlayRect.sizeDelta.x, Mathf.Max(height, MagicPages.instance.MinHeight));

    public void Clear()
    {
        foreach (MagicTextGroup group in Elements)
        {
            group.Title.Destroy();
            foreach (MagicTextElement element in group.Content) element.Destroy();
        }

        Elements.Clear();
    }

    public void Add(string title, params string[] content)
    {
        MagicTextElement titleElement = _template.Create(title, _layout.transform);
        titleElement.EnableOutline(true);
        
        List<MagicTextElement> contentElements = new();
        foreach (string text in content)
        {
            MagicTextElement contentElement = _template.Create(text, _layout.transform);
            contentElements.Add(contentElement);
        }

        MagicTextGroup group = new MagicTextGroup(titleElement, contentElements.ToArray());
        Elements.Add(group);
    }

    public void Enable(bool enable) => _obj.SetActive(enable);

    public MagicTextElement Create(string line) => _template.Create(line, _layout.transform);
}
