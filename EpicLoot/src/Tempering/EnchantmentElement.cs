using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EpicLoot;

public class EnchantmentElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static readonly List<EnchantmentElement> elements = [];
    public static implicit operator bool (EnchantmentElement e) => e != null;
    public Button button;
    public Image selected;
    public Image background;
    public TextMeshProUGUI label;
    public UITooltip uiTooltip;

    public MagicItemEffect _effect;
    public MagicItemEffectDefinition _definition;
    public MagicItemEffectDefinition.ValueDef _values;

    private bool canBeTempered;
    
    public int index;

    public void Awake()
    {
        background = GetComponent<Image>();
        button = GetComponent<Button>();
        uiTooltip = GetComponent<UITooltip>();
        selected = transform.Find("Selected").GetComponent<Image>();
        label = GetComponentInChildren<TextMeshProUGUI>();
        
        background.color = new Color(0.35f , 0.35f ,0.35f, 1f);

        elements.Add(this);
    }

    public void SetEffect(MagicItemEffect effect, ItemRarity rarity)
    {
        _effect = effect;
        _definition = MagicItemEffectDefinitions.Get(effect.EffectType);
        _values = _definition.GetValuesForRarity(rarity);
        string text = MagicItem.GetEffectTextGeneric(_definition, $"<b><color=yellow>{effect.EffectValue}</color></b>");
        canBeTempered = _values != null;
        label.text = text;
        if (_values == null)
        {
            button.interactable = false;
            label.color = Color.gray;
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"$mod_epicloot_min: {_values.MinValue}");
            sb.AppendLine($"$mod_epicloot_max: {_values.MaxValue}");
            sb.AppendLine($"$mod_epicloot_increment: {_values.Increment}\n");
            uiTooltip.Set($"<color={EpicLoot.GetRarityColor(rarity)}>{text}</color>",sb.ToString(), TemperPanel.Instance.tooltipAnchor);
        }
    }

    public void OnDestroy()
    {
        elements.Remove(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canBeTempered)
        {
            return;
        }
        background.color = new Color(0.625f, 0.625f, 0.625f, 1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!canBeTempered)
        {
            return;
        }
        background.color = new Color(0.35f , 0.35f ,0.35f, 1f);
    }
}