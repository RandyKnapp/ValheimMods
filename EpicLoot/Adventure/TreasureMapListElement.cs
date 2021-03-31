using System;
using EpicLoot.Adventure.Feature;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure
{
    public class TreasureMapListElement : BaseMerchantPanelListElement<TreasureMapItemInfo>
    {
        public Image Icon;
        public Text NameText;
        public GameObject PriceContainer;
        public GameObject PurchasedLabel;
        public Text CoinsCostText;
        public Button Button;
        public UITooltip Tooltip;

        public TreasureMapItemInfo ItemInfo;
        public int Price => ItemInfo?.Cost ?? 0;
        public Heightmap.Biome Biome => ItemInfo?.Biome ?? Heightmap.Biome.None;
        public bool CanAfford;
        public bool AlreadyPurchased;

        public event Action<TreasureMapItemInfo> OnSelected;

        public void Awake()
        {
            Button = GetComponent<Button>();
            Tooltip = GetComponent<UITooltip>();
            SelectedBackground = transform.Find("Selected").gameObject;
            SelectedBackground.SetActive(false);
            Icon = transform.Find("Icon").GetComponent<Image>();
            NameText = transform.Find("Name").GetComponent<Text>();
            PriceContainer = transform.Find("Price").gameObject;
            PriceContainer.SetActive(true);
            PurchasedLabel = transform.Find("Purchased").gameObject;
            PurchasedLabel.SetActive(false);
            CoinsCostText = transform.Find("Price/PriceElementCoins/Amount").GetComponent<Text>();

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

            var displayName = Localization.instance.Localize($"Treasure Map: $biome_{Biome.ToString().ToLower()} #{itemInfo.Interval + 1}");

            Icon.color = (CanAfford && !AlreadyPurchased) ? Color.white : new Color(1.0f, 0.0f, 1.0f, 0.0f);
            NameText.text = displayName;
            NameText.color = (CanAfford && !AlreadyPurchased) ? Color.white : Color.gray;
            PriceContainer.SetActive(!AlreadyPurchased);
            PurchasedLabel.SetActive(AlreadyPurchased);

            CoinsCostText.text = Price.ToString();
            CoinsCostText.transform.parent.gameObject.SetActive(Price > 0);
            if (!CanAfford)
            {
                CoinsCostText.color = Color.grey;
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
    }
}
