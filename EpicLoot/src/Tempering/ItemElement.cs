using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EpicLoot;

public class ItemElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static readonly List<ItemElement> elements = [];
    public static implicit operator bool(ItemElement e) => e != null;
    
    public Button button;
    public Image background;
    public Image selected;
    public Image MagicBG;
    public Image icon;
    public TextMeshProUGUI label;
    public UITooltip uiTooltip;

    public ItemDrop.ItemData _item;
    public MagicItem _magicItem;
    public string _rarityColor;

    public int index;

    public void Awake()
    {
        button = GetComponent<Button>();
        uiTooltip = GetComponent<UITooltip>();
        background = transform.Find("Background").GetComponent<Image>();
        selected = transform.Find("Selected").GetComponent<Image>();
        MagicBG = transform.Find("MagicBG").GetComponent<Image>();
        icon = transform.Find("Icon").GetComponent<Image>();
        label = transform.Find("Name").GetComponent<TextMeshProUGUI>();
        
        background.color = new Color(0.35f , 0.35f ,0.35f, 1f);
        
        elements.Add(this);
    }

    public void OnDestroy()
    {
        elements.Remove(this);
    }

    public void SetItem(ItemDrop.ItemData item)
    {
        _item = item;
        _magicItem = item.GetMagicItem();
        _rarityColor = EpicLoot.GetRarityColor(_magicItem.Rarity);
        MagicBG.color = EpicLoot.GetRarityColorARGB(_magicItem.Rarity);
        icon.sprite = item.GetIcon();
        string displayName = Localization.instance.Localize(_item.GetDisplayName());
        label.text = $"<color={_rarityColor}>{displayName}</color>";
        uiTooltip.Set(label.text, _item.GetTooltip(), TemperPanel.Instance.tooltipAnchor);
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        background.color = new Color(0.625f, 0.625f, 0.625f, 1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        background.color = new Color(0.35f , 0.35f ,0.35f, 1f);
    }
}