using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

public class RequirementElement : MonoBehaviour
{
    public static readonly List<RequirementElement> elements = [];
    [NonSerialized] public TemperRequirement _requirement;
    public Image icon;
    public TextMeshProUGUI label;
    public TextMeshProUGUI amount;

    public void Awake()
    {
        elements.Add(this);
    }

    public void Update()
    {
        if (!Player.m_localPlayer || _requirement == null || !_requirement.isValid)
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
        // A requirement whose configured prefab is missing (removed adventure-mode token, edited
        // costMap) renders as an empty row instead of NRE-ing the panel.
        if (requirement != null && !requirement.isValid)
        {
            requirement = null;
        }
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