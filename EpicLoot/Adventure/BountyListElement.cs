using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure
{
    public class BountyListElement : BaseMerchantPanelListElement<BountyInfo>
    {
        public Image Icon;
        public Text NameText;
        public Text RewardTextIron;
        public Text RewardTextGold;
        public Text RewardTextCoin;
        public Button Button;
        public UITooltip Tooltip;
        public GameObject RewardLabel;
        public GameObject InProgressLabel;
        public GameObject CompleteLabel;

        public BountyInfo BountyInfo;
        public bool CanAccept => BountyInfo.State == BountyState.Available;
        public bool CanClaim => BountyInfo.State == BountyState.Complete;

        private static readonly StringBuilder _sb = new StringBuilder();

        public event Action<BountyInfo> OnSelected;

        public void Awake()
        {
            Button = GetComponent<Button>();
            Tooltip = GetComponent<UITooltip>();
            SelectedBackground = transform.Find("Selected").gameObject;
            SelectedBackground.SetActive(false);
            Icon = transform.Find("Icon").GetComponent<Image>();
            NameText = transform.Find("Name").GetComponent<Text>();
            RewardLabel = transform.Find("Rewards/RewardLabel").gameObject;
            InProgressLabel = transform.Find("Status/InProgressLabel").gameObject;
            CompleteLabel = transform.Find("Status/CompleteLabel").gameObject;
            RewardTextIron = transform.Find("Rewards/IronElement/Amount").GetComponent<Text>();
            RewardTextGold = transform.Find("Rewards/GoldElement/Amount").GetComponent<Text>();
            RewardTextCoin = transform.Find("Rewards/CoinElement/Amount").GetComponent<Text>();

            var iconMaterial = InventoryGui.instance.m_dragItemPrefab.transform.Find("icon").GetComponent<Image>().material;
            if (iconMaterial != null)
            {
                Icon.material = iconMaterial;
            }
        }

        public void SetItem(BountyInfo info)
        {
            BountyInfo = info;

            var displayName = GetDisplayName();
            var canUse = BountyInfo.State == BountyState.Available || BountyInfo.State == BountyState.Complete;

            NameText.text = displayName;
            NameText.color = canUse ? Color.white : Color.gray;

            RewardTextIron.text = BountyInfo.RewardIron.ToString();
            RewardTextIron.transform.parent.gameObject.SetActive(BountyInfo.RewardIron > 0);
            RewardTextGold.text = BountyInfo.RewardGold.ToString();
            RewardTextGold.transform.parent.gameObject.SetActive(BountyInfo.RewardGold > 0);
            RewardTextCoin.text = BountyInfo.RewardCoins.ToString();
            RewardTextCoin.transform.parent.gameObject.SetActive(BountyInfo.RewardCoins > 0);

            Icon.sprite = AdventureDataManager.GetTrophyIconForMonster(BountyInfo.Target.MonsterID, BountyInfo.RewardGold > 0);
            Icon.color = canUse ? Color.white : new Color(1.0f, 0.0f, 1.0f, 0.0f);

            RewardLabel.SetActive(BountyInfo.State == BountyState.Available);
            InProgressLabel.SetActive(BountyInfo.State == BountyState.InProgress);
            CompleteLabel.SetActive(BountyInfo.State == BountyState.Complete);

            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(() => OnSelected?.Invoke(BountyInfo));

            if (Tooltip != null)
            {
                Tooltip.m_topic = displayName;
                Tooltip.m_text = Localization.instance.Localize(GetTooltip());
            }
        }

        private string GetDisplayName()
        {
            return Localization.instance.Localize(AdventureDataManager.GetBountyName(BountyInfo));
        }

        private string GetTooltip()
        {
            _sb.Clear();
            var biome = $"$biome_{BountyInfo.Biome.ToString().ToLower()}";
            var monsterName = AdventureDataManager.GetMonsterName(BountyInfo.Target.MonsterID).ToLowerInvariant();
            var targetName = string.IsNullOrEmpty(BountyInfo.TargetName) ? "" : $"<color=orange>{BountyInfo.TargetName}</color>, ";
            var slayMessage = BountyInfo.Adds.Count > 0 ? "$mod_epicloot_bounties_tooltip_slaymultiple" : "$mod_epicloot_bounties_tooltip_slay";

            //"mod_epicloot_bounties_tooltip": "Travel to the <color=#31eb41>$1</color> and locate $2 the <color=#f03232>$3</color>. $4. Return to me when it is done.",
            var tooltipText = Localization.instance.Localize("$mod_epicloot_bounties_tooltip", biome, targetName, monsterName, slayMessage);
            _sb.AppendLine(tooltipText);
            _sb.AppendLine();

            _sb.AppendLine("<color=#ffc400>$mod_epicloot_bounties_tooltip_rewards</color>");
            if (BountyInfo.RewardIron > 0)
            {
                _sb.AppendLine($"  {MerchantPanel.GetIronBountyTokenName()} x{BountyInfo.RewardIron}");
            }
            if (BountyInfo.RewardGold > 0)
            {
                _sb.AppendLine($"  {MerchantPanel.GetGoldBountyTokenName()} x{BountyInfo.RewardGold}");
            }
            if (BountyInfo.RewardCoins > 0)
            {
                _sb.AppendLine($"  {MerchantPanel.GetCoinsName()} x{BountyInfo.RewardCoins}");
            }

            _sb.AppendLine();
            _sb.AppendLine("<color=#ffc400>$mod_epicloot_bounties_tooltip_status</color>");
            switch (BountyInfo.State)
            {
                case BountyState.Available:
                    _sb.AppendLine($"  {Localization.instance.Localize("$mod_epicloot_bounties_tooltip_available")}");
                    break;
                case BountyState.InProgress:
                    _sb.AppendLine("  <color=#00f0ff>$mod_epicloot_bounties_tooltip_inprogress</color>");
                    break;
                case BountyState.Complete:
                    _sb.AppendLine("  <color=#70f56c>$mod_epicloot_bounties_tooltip_vanquished</color>");
                    break;
                case BountyState.Claimed:
                    _sb.AppendLine("  $mod_epicloot_bounties_tooltip_claimed");
                    break;
            }

            return Localization.instance.Localize(_sb.ToString());
        }
    }
}
