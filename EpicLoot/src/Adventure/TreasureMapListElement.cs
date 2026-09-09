using EpicLoot.Adventure.Feature;
using EpicLoot.Biomes;
using System;
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

        /// <summary>
        /// The authored "affordable" cost colour, captured before anything greys it out. See the
        /// note on <see cref="ApplyAffordability"/>.
        /// </summary>
        private Color _costTextColor = Color.white;

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
            _costTextColor = CoinsCostText.color;

            var iconMaterial = InventoryGui.instance.m_dragItemPrefab.transform.Find("icon").GetComponent<Image>().material;
            if (iconMaterial != null)
            {
                Icon.material = iconMaterial;
            }
        }

        /// <summary>
        /// Applies everything that depends on WHICH map this row shows. Anything that depends on
        /// what the player can currently pay for belongs in <see cref="ApplyAffordability"/>, which
        /// this ends by calling.
        /// </summary>
        public void SetItem(TreasureMapItemInfo itemInfo, int currentCoins)
        {
            ItemInfo = itemInfo;
            AlreadyPurchased = itemInfo.AlreadyPurchased;

            var displayName = Localization.instance.Localize("$mod_epicloot_treasuremap_name", BiomeDataManager.GetLocalizationToken(Biome), (itemInfo.Interval + 1).ToString());

            NameText.text = Localization.instance.Localize(displayName);
            PriceContainer.SetActive(!AlreadyPurchased);
            PurchasedLabel.SetActive(AlreadyPurchased);

            CoinsCostText.text = Price.ToString();
            CoinsCostText.transform.parent.gameObject.SetActive(Price > 0);

            ApplyAffordability(currentCoins);

            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(() => OnSelected?.Invoke(ItemInfo));

            Tooltip.m_topic = Localization.instance.Localize(displayName);
            Tooltip.m_text = Localization.instance.Localize(GetTooltip());
        }

        /// <summary>
        /// Re-applies only the visuals that depend on the player's coin count, in place. EVERY
        /// branch must assign both states -- the panel updates rows on a currency change instead of
        /// rebuilding them, so the old one-way grey on the cost text would have stuck forever.
        /// </summary>
        public void ApplyAffordability(int currentCoins)
        {
            CanAfford = Price <= currentCoins || Player.m_localPlayer.NoCostCheat();
            var available = CanAfford && !AlreadyPurchased;

            Icon.color = available ? Color.white : new Color(1.0f, 0.0f, 1.0f, 0.0f);
            NameText.color = available ? Color.white : Color.gray;
            CoinsCostText.color = CanAfford ? _costTextColor : Color.grey;
        }

        private string GetTooltip()
        {
            var biome = BiomeDataManager.GetLocalizationToken(Biome);
            return Localization.instance.Localize("$mod_epicloot_treasuremap_tooltip", biome);
        }
    }
}
