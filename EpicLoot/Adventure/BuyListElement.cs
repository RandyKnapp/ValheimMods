using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure
{
    public class BuyListElement : MonoBehaviour
    {
        public GameObject SelectedBackground;
        public Image Icon;
        public Image MagicBG;
        public Text NameText;
        public Text CostText;
        public Button Button;
        public UITooltip Tooltip;

        public SecretStashItemInfo ItemInfo;
        public ItemDrop.ItemData Item => ItemInfo?.Item;
        public int Price => ItemInfo?.Cost ?? 0;
        public bool IsGamble => ItemInfo?.IsGamble ?? false;
        public bool CanAfford;
        public bool IsSelected => SelectedBackground != null && SelectedBackground.activeSelf;

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
            CostText = transform.Find("Price/PriceElement/Amount").GetComponent<Text>();

            var iconMaterial = StoreGui.instance.m_listElement.transform.Find("icon").GetComponent<Image>().material;
            if (iconMaterial != null)
            {
                Icon.material = iconMaterial;
                MagicBG.material = iconMaterial;
            }
        }

        public void SetItem(SecretStashItemInfo itemInfo, int currentCoins)
        {
            ItemInfo = itemInfo;
            CanAfford = Price <= currentCoins;

            Icon.sprite = Item.GetIcon();
            Icon.color = CanAfford ? Color.white : new Color(1.0f, 0.0f, 1.0f, 0.0f);
            NameText.text = Localization.instance.Localize(Item.GetDecoratedName(CanAfford ? null : "#AAA"));
            NameText.color = Color.white;
            CostText.text = Price.ToString();
            if (!CanAfford)
            {
                CostText.color = Color.grey;
            }
            MagicBG.enabled = Item.UseMagicBackground();
            MagicBG.color = CanAfford ? Item.GetRarityColor() : new Color(1.0f, 0.0f, 1.0f, 0.0f);

            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(() => OnSelected?.Invoke(ItemInfo));

            Tooltip.m_topic = Localization.instance.Localize(Item.GetDecoratedName());
            Tooltip.m_text = Localization.instance.Localize(Item.GetTooltip());

            if (IsGamble)
            {
                NameText.text = $"??? {Localization.instance.Localize(Item.m_shared.m_name)}";
                NameText.color = CanAfford ? Color.white : Color.gray;

                Tooltip.m_topic = NameText.text;
                Tooltip.m_text = GetGambleTooltip();
            }
        }

        public void SetSelected(bool selected)
        {
            SelectedBackground.SetActive(selected);
        }

        private static string GetGambleTooltip()
        {
            _sb.Clear();

            _sb.AppendLine("Pay a premium for a chance at a magic item!");
            _sb.AppendLine();
            _sb.AppendLine("Chance for:");

            var labels = new[]
            {
                "Non-magic",
                EpicLoot.GetRarityDisplayName(ItemRarity.Magic),
                EpicLoot.GetRarityDisplayName(ItemRarity.Rare),
                EpicLoot.GetRarityDisplayName(ItemRarity.Epic),
                EpicLoot.GetRarityDisplayName(ItemRarity.Legendary)
            };

            var totalWeight = (float)AdventureDataManager.Config.SecretStash.GambleRarityChance.Sum();
            for (var i = 0; i < 5; ++i)
            {
                var color = i == 0 ? "#FFF" : EpicLoot.GetRarityColor((ItemRarity) (i - 1));
                var percent = AdventureDataManager.Config.SecretStash.GambleRarityChance[i] / totalWeight * 100;
                _sb.AppendLine($"<color={color}>{labels[i]}: {percent:0.#}%</color>");
            }

            return _sb.ToString();
        }
    }
}
