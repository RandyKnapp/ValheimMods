using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

public class RequirementElement : MonoBehaviour
{
    public static readonly List<RequirementElement> elements = [];
    public TemperRequirement _requirement;
    public Image background;
    public Image magicBG;
    public Image icon;
    public TextMeshProUGUI label;
    public TextMeshProUGUI amount;

    public void Awake()
    {
        background = transform.Find("Background").GetComponent<Image>();
        magicBG = transform.Find("MagicBG").GetComponent<Image>();
        icon = transform.Find("Icon").GetComponent<Image>();
        label = transform.Find("Name").GetComponent<TextMeshProUGUI>();
        amount = transform.Find("Amount").GetComponent<TextMeshProUGUI>();

        elements.Add(this);
    }

    public void Update()
    {
        if (!Player.m_localPlayer || _requirement == null)
        {
            return;
        }

        int playerItemCount = Player.m_localPlayer
                .GetInventory()
                .CountItems(_requirement.item.m_itemData.m_shared.m_name);
        if (playerItemCount < _requirement.amount)
        {
            float time = Mathf.Sin(Time.time * 10f);
            amount.color = time > 0.0f ? Color.red : Color.white;
        }
        else
        {
            amount.color = Color.white;
        }
    }

    public void OnDestroy()
    {
        elements.Remove(this);
    }

    public void Set(TemperRequirement requirement)
    {
        if (requirement == null)
        {
            label.enabled = false;
            amount.enabled = false;
            icon.enabled = false;
        }
        else
        {
            _requirement = requirement;
            SetName(requirement.item.m_itemData.m_shared.m_name);
            SetAmount(requirement.amount);
            icon.sprite = requirement.item.m_itemData.GetIcon();
        }
    }

    public void SetName(string text) => label.text = Localization.instance.Localize(text);
    public void SetAmount(int i) => amount.text = i.ToString();
}