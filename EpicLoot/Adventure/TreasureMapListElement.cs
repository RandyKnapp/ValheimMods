using System;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure
{
    public class TreasureMapListElement : MonoBehaviour
    {
        public GameObject SelectedBackground;
        public Image Icon;
        public Text NameText;
        public Text CostText;
        public Button Button;
        public UITooltip Tooltip;

        public TreasureMapItemInfo ItemInfo;
        public int Price => ItemInfo?.Cost ?? 0;
        public Heightmap.Biome Biome => ItemInfo?.Biome ?? Heightmap.Biome.None;
        public bool CanAfford;
        public bool AlreadyPurchased;
        public bool IsSelected => SelectedBackground != null && SelectedBackground.activeSelf;

        public event Action<TreasureMapItemInfo> OnSelected;

        public void Awake()
        {
            Button = GetComponent<Button>();
            Tooltip = GetComponent<UITooltip>();
            SelectedBackground = transform.Find("Selected").gameObject;
            SelectedBackground.SetActive(false);
            Icon = transform.Find("Icon").GetComponent<Image>();
            NameText = transform.Find("Name").GetComponent<Text>();
            CostText = transform.Find("Price/PriceElement/Amount").GetComponent<Text>();

            var iconMaterial = StoreGui.instance.m_listElement.transform.Find("icon").GetComponent<Image>().material;
            if (iconMaterial != null)
            {
                Icon.material = iconMaterial;
            }
        }

        public void SetItem(TreasureMapItemInfo itemInfo, int currentCoins)
        {
            ItemInfo = itemInfo;
            CanAfford = Price <= currentCoins;
            AlreadyPurchased = itemInfo.AlreadyPurchased;

            var displayName = Localization.instance.Localize($"Treasure Map: $biome_{Biome.ToString().ToLower()}");

            Icon.color = (CanAfford && !AlreadyPurchased) ? Color.white : new Color(1.0f, 0.0f, 1.0f, 0.0f);
            NameText.text = displayName;
            NameText.color = (CanAfford && !AlreadyPurchased) ? Color.white : Color.gray;
            CostText.text = AlreadyPurchased ? "purchased" : Price.ToString();
            if (AlreadyPurchased)
            {
                CostText.color = Color.green;
            }
            else if (!CanAfford)
            {
                CostText.color = Color.grey;
            }

            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(() => OnSelected?.Invoke(ItemInfo));

            Tooltip.m_topic = displayName;
            Tooltip.m_text = Localization.instance.Localize(GetTooltip());
        }

        private string GetTooltip()
        {
            return $"This folded piece of ragged parchment has a big red X marked on it, somewhere in the $biome_{Biome.ToString().ToLower()}...";
        }

        public void SetSelected(bool selected)
        {
            SelectedBackground.SetActive(selected);
        }
    }
}
