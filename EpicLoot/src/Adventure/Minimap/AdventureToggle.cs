using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EpicLoot.Adventure;

public class AdventureToggle
{
    public readonly GameObject instance;
    public readonly Toggle toggle;
    public readonly TextMeshProUGUI label;
    public readonly RectTransform rect;
    public readonly Image checkbox;
    public readonly Image checkmark;
    public readonly Image darken;
    public readonly UIGamePad gamepad;
    public readonly TextMeshProUGUI inputKey;

    public AdventureToggle(GameObject source, Transform parent, string name, UnityAction<bool> onToggle)
    {
        instance = UnityEngine.Object.Instantiate(source, parent);
        instance.name = name;
        rect = instance.GetComponent<RectTransform>();
        toggle = instance.GetComponentInChildren<Toggle>();
        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(onToggle);
        label = Utils.FindChild(instance.transform, "Label").GetComponent<TextMeshProUGUI>();
        label.text = name;
        checkbox = Utils.FindChild(instance.transform, "Background").GetComponent<Image>();
        checkmark = Utils.FindChild(checkbox.transform, "Checkmark").GetComponent<Image>();
        darken = instance.GetComponent<Image>();
        gamepad = instance.GetComponentInChildren<UIGamePad>();
        inputKey = Utils.FindChild(instance.transform, "Key").GetComponent<TextMeshProUGUI>();
        ButtonSfx sfx = instance.GetComponentInChildren<ButtonSfx>();
        sfx.Start();
    }

    public void SetGamepadKey(string key)
    {
        gamepad.m_zinputKey = key;
        inputKey.text = Localization.instance.Localize(ZInput.instance.GetBoundKeyString(key, true));
    }

    public void SetLabel(string text) => label.text = Localization.instance.Localize(text);

    public void SetIcon(Sprite icon) => checkmark.sprite = icon;

    // Unused?
    public void SetBackground(float transparency) =>
        darken.color = new Color(darken.color.r, darken.color.g, darken.color.b, transparency);
}