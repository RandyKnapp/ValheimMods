using System;
using System.Linq;
using System.Text;
using EpicLoot.Adventure.Feature;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure
{
    public class BuyListElement : BaseMerchantPanelListElement<SecretStashItemInfo>
    {
        public Image Icon;
        public Image MagicBG;
        public Text NameText;
        public Text CoinsCostText;
        public Text ForestTokenCostText;
        public Text IronBountyTokenCostText;
        public Text GoldBountyTokenCostText;
        public Button Button;
        public UITooltip Tooltip;

        public SecretStashItemInfo ItemInfo;

        public event Action<SecretStashItemInfo> OnSelected;

        private static readonly StringBuilder _sb = new StringBuilder();

        public void Awake()
        {
            Button = GetComponent<Button>();
            Tooltip = GetComponent<UITooltip>();
            SelectedBackground = transform.Find("Selected").gameObject;
            SelectedBackground.SetActive(false);
            Icon = transform.Find("Icon").GetComponent<Image>();
            MagicBG = transform.Find("MagicBG").GetComponent<Image>();
            NameText = transform.Find("Name").GetComponent<Text>();
            CoinsCostText = transform.Find("Price/Coins/Amount").GetComponent<Text>();
            ForestTokenCostText = transform.Find("Price/ForestTokens/Amount").GetComponent<Text>();
            IronBountyTokenCostText = transform.Find("Price/IronBountyToken/Amount").GetComponent<Text>();
            GoldBountyTokenCostText = transform.Find("Price/GoldBountyToken/Amount").GetComponent<Text>();

            var iconMaterial = InventoryGui.instance.m_dragItemPrefab.transform.Find("icon").GetComponent<Image>().material;
            if (iconMaterial != null)
            {
                Icon.material = iconMaterial;
                MagicBG.material = iconMaterial;
            }
        }

        public bool CanAfford(Currencies currencies)
        {
            return ItemInfo.Cost.Coins <= currencies.Coins
                   && ItemInfo.Cost.ForestTokens <= currencies.ForestTokens
                   && ItemInfo.Cost.IronBountyTokens <= currencies.IronBountyTokens
                   && ItemInfo.Cost.GoldBountyTokens <= currencies.GoldBountyTokens;
        }

        public void SetItem(SecretStashItemInfo itemInfo, Currencies currencies)
        {
            ItemInfo = itemInfo;
            var canAfford = CanAfford(currencies);

            Icon.sprite = ItemInfo.Item.GetIcon();
            Icon.color = canAfford ? Color.white : new Color(1.0f, 0.0f, 1.0f, 0.0f);
            NameText.text = Localization.instance.Localize(ItemInfo.Item.GetDecoratedName(canAfford ? null : "grey"));

            CoinsCostText.text = ItemInfo.Cost.Coins.ToString();
            CoinsCostText.transform.parent.gameObject.SetActive(ItemInfo.Cost.Coins > 0);

            ForestTokenCostText.text = ItemInfo.Cost.ForestTokens.ToString();
            ForestTokenCostText.transform.parent.gameObject.SetActive(ItemInfo.Cost.ForestTokens > 0);

            IronBountyTokenCostText.text = ItemInfo.Cost.IronBountyTokens.ToString();
            IronBountyTokenCostText.transform.parent.gameObject.SetActive(ItemInfo.Cost.IronBountyTokens > 0);

            GoldBountyTokenCostText.text = ItemInfo.Cost.GoldBountyTokens.ToString();
            GoldBountyTokenCostText.transform.parent.gameObject.SetActive(ItemInfo.Cost.GoldBountyTokens > 0);

            if (!canAfford)
            {
                CoinsCostText.color = Color.grey;
                ForestTokenCostText.color = Color.grey;
                IronBountyTokenCostText.color = Color.grey;
                GoldBountyTokenCostText.color = Color.grey;
            }

            MagicBG.enabled = itemInfo.GuaranteedRarity || ItemInfo.Item.UseMagicBackground();
            if (canAfford)
            {
                MagicBG.color = itemInfo.GuaranteedRarity ? EpicLoot.GetRarityColorARGB(itemInfo.Rarity) : ItemInfo.Item.GetRarityColor();
            }
            else
            {
                MagicBG.color = new Color(1.0f, 0.0f, 1.0f, 0.0f);
            }
            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(() => OnSelected?.Invoke(ItemInfo));

            if (ItemInfo.IsGamble)
            {
                var color = canAfford ? (itemInfo.GuaranteedRarity ? EpicLoot.GetRarityColor(itemInfo.Rarity) : "white") : "grey";
                var rarityDisplay = itemInfo.GuaranteedRarity ? EpicLoot.GetRarityDisplayName(itemInfo.Rarity) : "$mod_epicloot_merchant_unknown";
                NameText.text = Localization.instance.Localize($"<color={color}>{rarityDisplay} {ItemInfo.Item.m_shared.m_name}</color>");

                if (EpicLoot.HasAuga)
                {
                    Auga.API.Tooltip_MakeSimpleTooltip(gameObject);
                }

                Tooltip.m_topic = NameText.text;
                Tooltip.m_text = GetGambleTooltip();
            }
            else
            {
                if (EpicLoot.HasAuga)
                {
                    Auga.API.Tooltip_MakeItemTooltip(gameObject, ItemInfo.Item);
                }
                else
                {
                    Tooltip.m_topic = Localization.instance.Localize(ItemInfo.Item.GetDecoratedName());
                    Tooltip.m_text = Localization.instance.Localize(ItemInfo.Item.GetTooltip());
                }
            }
        }

        private string GetGambleTooltip()
        {
            _sb.Clear();

            _sb.AppendLine(Localization.instance.Localize("$mod_epicloot_gamble_tooltip"));
            _sb.AppendLine();
            _sb.AppendLine(Localization.instance.Localize("$mod_epicloot_gamble_tooltip_chance"));

            var rarityChance = AdventureDataManager.Config.Gamble.GambleRarityChance;
            if (ItemInfo.GuaranteedRarity)
            {
                rarityChance = AdventureDataManager.Config.Gamble.GambleRarityChanceByRarity[(int)ItemInfo.Rarity];
            }

            var labels = new[]
            {
                "$mod_epicloot_gamble_tooltip_nonmagic",
                EpicLoot.GetRarityDisplayName(ItemRarity.Magic),
                EpicLoot.GetRarityDisplayName(ItemRarity.Rare),
                EpicLoot.GetRarityDisplayName(ItemRarity.Epic),
                EpicLoot.GetRarityDisplayName(ItemRarity.Legendary),
                EpicLoot.GetRarityDisplayName(ItemRarity.Mythic),
            };

            var totalWeight = (float)AdventureDataManager.Config.Gamble.GambleRarityChance.Sum();
            for (var i = 0; i < 5; ++i)
            {
                var color = i == 0 ? "white" : EpicLoot.GetRarityColor((ItemRarity) (i - 1));
                var percent = rarityChance[i] / totalWeight * 100;
                if (percent >= 0.01)
                {
                    _sb.AppendLine($"<color={color}>{labels[i]}: {percent:0.#}%</color>");
                }
            }

            return Localization.instance.Localize(_sb.ToString());
        }
    }
}
