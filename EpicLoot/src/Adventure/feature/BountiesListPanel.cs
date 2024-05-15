using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Adventure.Feature
{
    public class AvailableBountiesListPanel : MerchantListPanel<BountyListElement>
    {
        private readonly MerchantPanel _merchantPanel;

        public AvailableBountiesListPanel(MerchantPanel merchantPanel, BountyListElement elementPrefab) 
            : base(
                merchantPanel.transform.Find("Bounties/AvailableBountiesPanel/ItemList") as RectTransform, 
                elementPrefab,
                merchantPanel.transform.Find("Bounties/AcceptBountyButton").GetComponent<Button>(),
                merchantPanel.transform.Find("Bounties/TimeLeft").GetComponent<Text>())
        {
            _merchantPanel = merchantPanel;
        }

        public override bool NeedsRefresh(bool currenciesChanged)
        {
            return _currentInterval != AdventureDataManager.Bounties.GetCurrentInterval();
        }

        public override void RefreshButton(Currencies playerCurrencies)
        {
            var selectedItem = GetSelectedItem();
            
            var saveData = Player.m_localPlayer.GetAdventureSaveData();
            var bountyInProgressCount = saveData.GetInProgressBounties().Count;
            bool allowedToBuy = !(EpicLoot.EnableLimitedBountiesInProgress.Value &&
                                  bountyInProgressCount >= EpicLoot.MaxInProgressBounties.Value);

            if (MerchantPanel.AcceptBountyText != null)
            {
                MerchantPanel.AcceptBountyText.text = Localization.instance.Localize(!allowedToBuy ? string.Format("$mod_epicloot_merchant_max_bounties ({0})", EpicLoot.MaxInProgressBounties.Value): "$mod_epicloot_merchant_acceptbounty");
            }
            
            MainButton.interactable = selectedItem != null && selectedItem.CanAccept && allowedToBuy;
        }

        protected override void OnMainButtonClicked()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var bounty = GetSelectedItem();
            if (bounty != null && bounty.BountyInfo.State == BountyState.Available)
            {
                player.StartCoroutine(AdventureDataManager.Bounties.AcceptBounty(player, bounty.BountyInfo, (success, position) =>
                {
                    if (success)
                    {
                        RefreshItems(_merchantPanel.GetPlayerCurrencies());

                        StoreGui.instance.m_trader.OnBought(null);
                        StoreGui.instance.m_buyEffects.Create(player.transform.position, Quaternion.identity);
                    }
                }));
            }
        }

        public override void RefreshItems(Currencies currencies)
        {
            _currentInterval = AdventureDataManager.Bounties.GetCurrentInterval();

            DestroyAllListElementsInList();

            var allItems = AdventureDataManager.Bounties.GetAvailableBounties();
            for (int index = 0; index < allItems.Count; index++)
            {
                var itemInfo = allItems[index];
                var itemElement = Object.Instantiate(ElementPrefab, List);
                itemElement.gameObject.SetActive(true);
                itemElement.SetItem(itemInfo);
                var i = index;
                itemElement.OnSelected += (x) => OnItemSelected(i);
                itemElement.SetSelected(i == _selectedItemIndex);
            }
        }

        public override void UpdateRefreshTime()
        {
            UpdateRefreshTime(AdventureDataManager.Bounties.GetSecondsUntilRefresh());
        }
    }

    public class ClaimableBountiesListPanel : MerchantListPanel<BountyListElement>
    {
        private readonly MerchantPanel _merchantPanel;
        public Button AbandonButton;
        public Image AbandonButtonIcon;

        public ClaimableBountiesListPanel(MerchantPanel merchantPanel, BountyListElement elementPrefab)
            : base(
                merchantPanel.transform.Find("Bounties/ClaimableBountiesPanel/ItemList") as RectTransform,
                elementPrefab,
                merchantPanel.transform.Find("Bounties/ClaimBountyButton").GetComponent<Button>(),
                null)
        {
            _merchantPanel = merchantPanel;

            AbandonButton = merchantPanel.transform.Find("Bounties/AbandonBountyButton").GetComponent<Button>();
            AbandonButton.onClick.AddListener(OnAbandonButtonClicked);

            AbandonButtonIcon = AbandonButton.transform.Find("Icon").GetComponent<Image>();
        }

        public override bool NeedsRefresh(bool currenciesChanged)
        {
            return _currentInterval != AdventureDataManager.Bounties.GetCurrentInterval();
        }

        public override void RefreshButton(Currencies playerCurrencies)
        {
            var selectedItem = GetSelectedItem();
            var haveSpace = CanAddRewardToInventory(selectedItem);
            MainButton.interactable = selectedItem != null && selectedItem.CanClaim && haveSpace;
            var tooltip = MainButton.GetComponent<UITooltip>();
            if (tooltip != null)
            {
                tooltip.m_text = "";
                if (selectedItem != null && !selectedItem.CanClaim)
                {
                    tooltip.m_text = "$mod_epicloot_bounties_notcompletetooltip";
                }
                else if (selectedItem != null && !haveSpace)
                {
                    tooltip.m_text = "$mod_epicloot_bounties_noroomtooltip";
                }
            }

            var canAbandon = selectedItem != null && selectedItem.BountyInfo.State == BountyState.InProgress;
            AbandonButton.interactable = canAbandon;
            AbandonButtonIcon.color = canAbandon ? Color.red : Color.grey;
        }

        public bool CanAddRewardToInventory(BountyListElement selectedItem)
        {
            if (selectedItem == null)
            {
                return false;
            }

            var rewardCount = (selectedItem.BountyInfo.RewardIron > 0 ? 1 : 0) + (selectedItem.BountyInfo.RewardGold > 0 ? 1 : 0) + (selectedItem.BountyInfo.RewardCoins > 0 ? 1 : 0);
            var hasEmptySlots = Player.m_localPlayer.GetInventory().GetEmptySlots() >= rewardCount;
            if (hasEmptySlots)
            {
                return true;
            }

            if (selectedItem.BountyInfo.RewardIron > 0)
            {
                var haveSpace = Player.m_localPlayer.GetInventory().FindFreeStackSpace(MerchantPanel.GetIronBountyTokenName(), Game.m_worldLevel) > selectedItem.BountyInfo.RewardIron;
                if (!haveSpace)
                {
                    return false;
                }
            }

            if (selectedItem.BountyInfo.RewardGold > 0)
            {
                var haveSpace = Player.m_localPlayer.GetInventory().FindFreeStackSpace(MerchantPanel.GetGoldBountyTokenName(), Game.m_worldLevel) > selectedItem.BountyInfo.RewardGold;
                if (!haveSpace)
                {
                    return false;
                }
            }

            if (selectedItem.BountyInfo.RewardCoins > 0)
            {
                var haveSpace = Player.m_localPlayer.GetInventory().FindFreeStackSpace(MerchantPanel.GetCoinsName(), Game.m_worldLevel) > selectedItem.BountyInfo.RewardCoins;
                if (!haveSpace)
                {
                    return false;
                }
            }

            return true;
        }

        protected override void OnMainButtonClicked()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var bounty = GetSelectedItem();
            if (bounty != null && bounty.BountyInfo.State == BountyState.Complete)
            {
                AdventureDataManager.Bounties.ClaimBountyReward(bounty.BountyInfo);

                _merchantPanel.RefreshAll();

                StoreGui.instance.m_trader.OnBought(null);
                StoreGui.instance.m_buyEffects.Create(player.transform.position, Quaternion.identity);
            }
        }

        private void OnAbandonButtonClicked()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var bounty = GetSelectedItem();
            if (bounty != null && bounty.BountyInfo.State == BountyState.InProgress)
            {
                _merchantPanel.AbandonBountyDialog.Show(bounty.BountyInfo);
            }
        }

        public override void RefreshItems(Currencies currencies)
        {
            _currentInterval = AdventureDataManager.Bounties.GetCurrentInterval();

            DestroyAllListElementsInList();

            var allItems = AdventureDataManager.Bounties.GetClaimableBounties();
            for (int index = 0; index < allItems.Count; index++)
            {
                var itemInfo = allItems[index];
                var itemElement = Object.Instantiate(ElementPrefab, List);
                itemElement.gameObject.SetActive(true);
                itemElement.SetItem(itemInfo);
                var i = index;
                itemElement.OnSelected += (x) => OnItemSelected(i);
                itemElement.SetSelected(i == _selectedItemIndex);
            }
        }

        public override void UpdateRefreshTime()
        {
        }
    }
}
