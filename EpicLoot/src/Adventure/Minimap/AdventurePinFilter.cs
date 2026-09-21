using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EpicLoot.Adventure;

public class AdventurePinFilter
{
    public readonly GameObject instance;
    public readonly Image icon;
    public readonly Image selected;
    public readonly Minimap.PinType pinType;

    private readonly Minimap _minimap;

    public AdventurePinFilter(Minimap minimap, Minimap.PinType type, Sprite sprite, string tooltip)
    {
        _minimap = minimap;
        pinType = type;

        Transform template = minimap.m_selectedIconBoss.transform;
        GameObject source = template.parent.gameObject;
        int selectedIndex = template.GetSiblingIndex();

        instance = Object.Instantiate(source, source.transform.parent);
        instance.name = $"AdventurePinFilter{type}";
        instance.transform.SetAsLastSibling();

        icon = instance.GetComponent<Image>();
        icon.sprite = sprite;
        icon.color = Color.white;

        selected = instance.transform.GetChild(selectedIndex).GetComponent<Image>();
        selected.enabled = false;

        foreach (UIGamePad gamePad in instance.GetComponentsInChildren<UIGamePad>(true))
        {
            Object.Destroy(gamePad);
        }

        MouseClick mouseClick = instance.GetComponentInChildren<MouseClick>(true);
        if (mouseClick != null)
        {
            ClearListeners(mouseClick.m_leftClick);
            ClearListeners(mouseClick.m_middleClick);
            ClearListeners(mouseClick.m_rightClick);
            mouseClick.m_leftClick.AddListener(OnPressed);
            mouseClick.m_rightClick.AddListener(ToggleFilter);
        }

        Button button = instance.GetComponentInChildren<Button>(true);
        if (button != null)
        {
            ClearListeners(button.onClick);

            if (mouseClick == null)
            {
                button.onClick.AddListener(OnPressed);
            }
        }

        UITooltip uiTooltip = instance.GetComponentInChildren<UITooltip>(true);
        if (uiTooltip != null)
        {
            uiTooltip.m_text = Localization.instance.Localize(tooltip);
        }

        Reposition();
    }

    public bool Visible => _minimap.m_visibleIconTypes[(int)pinType];

    public bool Active => instance != null && instance.activeSelf;

    public void SetActive(bool active)
    {
        instance.SetActive(active);

        if (active)
        {
            icon.color = Visible ? Color.white : Color.gray;
            return;
        }

        if (_minimap.m_selectedType == pinType)
        {
            _minimap.SelectIcon(Minimap.PinType.Icon0);
        }
    }

    public void Destroy()
    {
        _minimap.m_selectedIcons.Remove(pinType);
        Object.Destroy(instance);
    }

    private void ToggleFilter() => _minimap.ToggleIconFilter(pinType);

    private void OnPressed()
    {
        if (ZInput.HasDoubleTapped())
        {
            _minimap.ToggleIconFilter(pinType);
        }
    }

    private void Reposition()
    {
        if (instance.transform is not RectTransform rect || rect.parent.GetComponent<LayoutGroup>() != null)
        {
            return;
        }

        if (_minimap.m_selectedIconDeath.transform.parent is not RectTransform step0 ||
            _minimap.m_selectedIconBoss.transform.parent is not RectTransform step1 ||
            rect.parent.GetChild(rect.GetSiblingIndex() - 1) is not RectTransform previous)
        {
            return;
        }

        rect.anchoredPosition = previous.anchoredPosition + (step1.anchoredPosition - step0.anchoredPosition);
    }

    private static void ClearListeners(UnityEventBase unityEvent)
    {
        if (unityEvent == null)
        {
            return;
        }

        for (int index = 0; index < unityEvent.GetPersistentEventCount(); ++index)
        {
            unityEvent.SetPersistentListenerState(index, UnityEventCallState.Off);
        }

        unityEvent.RemoveAllListeners();
    }
}
