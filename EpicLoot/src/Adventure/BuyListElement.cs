using EpicLoot.Adventure.Feature;
using System;
using System.Linq;
using System.Text;
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

        /// <summary>
        /// The authored "affordable" cost colour, captured before anything greys it out.
        /// <see cref="ApplyAffordability"/> restores this rather than assuming white, so the prefab
        /// (and Auga's colour pass over it) stays the authority on what affordable looks like.
        /// </summary>
        private Color _costTextColor = Color.white;

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
            _costTextColor = CoinsCostText.color;

            var iconMaterial = InventoryGui.instance.m_dragItemPrefab.transform.Find("icon").GetComponent<Image>().material;
            if (iconMaterial != null)
            {
                Icon.material = iconMaterial;
                MagicBG.material = iconMaterial;
            }
        }

        public bool CanAfford(Currencies currencies)
        {
            return (ItemInfo.Cost.Coins <= currencies.Coins
                   && ItemInfo.Cost.ForestTokens <= currencies.ForestTokens
                   && ItemInfo.Cost.IronBountyTokens <= currencies.IronBountyTokens
                   && ItemInfo.Cost.GoldBountyTokens <= currencies.GoldBountyTokens)
                   || Player.m_localPlayer.NoCostCheat();
        }

        /// <summary>
        /// Applies everything that depends on WHICH item this row shows. Anything that depends on
        /// what the player can currently pay for belongs in <see cref="ApplyAffordability"/>, which
        /// this ends by calling -- the panel re-runs that alone on a currency change rather than
        /// rebuilding the row.
        /// </summary>
        public void SetItem(SecretStashItemInfo itemInfo, Currencies currencies)
        {
            ItemInfo = itemInfo;

            Icon.sprite = ItemInfo.Item.GetIcon();

            CoinsCostText.text = ItemInfo.Cost.Coins.ToString();
            CoinsCostText.transform.parent.gameObject.SetActive(ItemInfo.Cost.Coins > 0);

            ForestTokenCostText.text = ItemInfo.Cost.ForestTokens.ToString();
            ForestTokenCostText.transform.parent.gameObject.SetActive(ItemInfo.Cost.ForestTokens > 0);

            IronBountyTokenCostText.text = ItemInfo.Cost.IronBountyTokens.ToString();
            IronBountyTokenCostText.transform.parent.gameObject.SetActive(ItemInfo.Cost.IronBountyTokens > 0);

            GoldBountyTokenCostText.text = ItemInfo.Cost.GoldBountyTokens.ToString();
            GoldBountyTokenCostText.transform.parent.gameObject.SetActive(ItemInfo.Cost.GoldBountyTokens > 0);

            MagicBG.enabled = itemInfo.GuaranteedRarity || ItemInfo.Item.UseMagicBackground();

            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(() => OnSelected?.Invoke(ItemInfo));

            if (ItemInfo.IsGamble)
            {
                if (EpicLoot.HasAuga)
                {
                    //Auga.API.Tooltip_MakeSimpleTooltip(gameObject);
                }

                // The affordable spelling on purpose: a tooltip heading has no business greying out,
                // and building it here keeps GetGambleTooltip off the per-currency-change path.
                Tooltip.m_topic = BuildGambleName(true);
                Tooltip.m_text = GetGambleTooltip();
            }
            else
            {
                if (EpicLoot.HasAuga)
                {
                    //Auga.API.Tooltip_MakeItemTooltip(gameObject, ItemInfo.Item);
                }
                else
                {
                    Tooltip.m_topic = Localization.instance.Localize(ItemInfo.Item.GetDecoratedName());
                    Tooltip.m_text = Localization.instance.Localize(ItemInfo.Item.GetTooltip());
                }
            }

            ApplyAffordability(currencies);
        }

        /// <summary>
        /// Re-applies only the visuals that depend on what the player can currently pay for, in
        /// place. EVERY branch here must assign both states: the merchant panel updates rows on a
        /// currency change instead of rebuilding them, so a colour left over from the previous state
        /// would stick. (The one-way `if (!canAfford)` this replaced got away with it only because
        /// rows were always freshly instantiated from the prefab.)
        /// </summary>
        public void ApplyAffordability(Currencies currencies)
        {
            var canAfford = CanAfford(currencies);
            var costColor = canAfford ? _costTextColor : Color.grey;

            Icon.color = canAfford ? Color.white : new Color(1.0f, 0.0f, 1.0f, 0.0f);

            CoinsCostText.color = costColor;
            ForestTokenCostText.color = costColor;
            IronBountyTokenCostText.color = costColor;
            GoldBountyTokenCostText.color = costColor;

            MagicBG.color = canAfford
                ? (ItemInfo.GuaranteedRarity ? EpicLoot.GetRarityColorARGB(ItemInfo.Rarity) : ItemInfo.Item.GetRarityColor())
                : new Color(1.0f, 0.0f, 1.0f, 0.0f);

            NameText.text = ItemInfo.IsGamble ?
                BuildGambleName(canAfford) :
                Localization.instance.Localize(ItemInfo.Item.GetDecoratedName(canAfford ? null : "#808080ff"));
        }

        private string BuildGambleName(bool canAfford)
        {
            var color = canAfford ?
                (ItemInfo.GuaranteedRarity ? EpicLoot.GetRarityColor(ItemInfo.Rarity) : "white") :
                "#808080ff";
            var rarityDisplay = ItemInfo.GuaranteedRarity ?
                EpicLoot.GetRarityDisplayName(ItemInfo.Rarity) :
                "$mod_epicloot_merchant_unknown";
            return Localization.instance.Localize($"<color={color}>{rarityDisplay} {ItemInfo.Item.m_shared.m_name}</color>");
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

            // Column 0 is the non-magic chance, then one column per rarity, matching GambleRarityChance.
            var labels = new[] { "$mod_epicloot_gamble_tooltip_nonmagic" }
                .Concat(Rarities.All.Select(EpicLoot.GetRarityDisplayName))
                .ToArray();

            var totalWeight = AdventureDataManager.Config.Gamble.GambleRarityChance.Sum();
            for (var i = 0; i < labels.Length; ++i)
            {
                var color = i == 0 ? "white" : EpicLoot.GetRarityColor((ItemRarity) (i - 1));
                var percent = (i < rarityChance.Length ? rarityChance[i] : 0) / totalWeight * 100;
                if (percent >= 0.01)
                {
                    _sb.AppendLine($"<color={color}>{labels[i]}: {percent:0.#}%</color>");
                }
            }

            return Localization.instance.Localize(_sb.ToString());
        }
    }
}
