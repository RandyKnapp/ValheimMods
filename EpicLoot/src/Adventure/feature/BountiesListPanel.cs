using EpicLoot.Config;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Adventure.Feature
{
    public class AvailableBountiesListPanel : MerchantListPanel<BountyListElement>
    {
        private readonly MerchantPanel _merchantPanel;

        private static bool _generatingBounty = false; // Flag to prevent spam accepting multiple bounties

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
            bool allowedToBuy = !(ELConfig.EnableLimitedBountiesInProgress.Value &&
                bountyInProgressCount >= ELConfig.MaxInProgressBounties.Value);

            if (MerchantPanel.AcceptBountyText != null)
            {
                MerchantPanel.AcceptBountyText.text = Localization.instance.Localize(
                    !allowedToBuy ? string.Format("$mod_epicloot_merchant_max_bounties ({0})",
                    ELConfig.MaxInProgressBounties.Value): "$mod_epicloot_merchant_acceptbounty");
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
            if (!_generatingBounty && bounty != null && bounty.BountyInfo.State == BountyState.Available)
            {
                _generatingBounty = true;
                EpicLoot.Log("Trying to accept bounty...");
                player.StartCoroutine(AdventureDataManager.Bounties.AcceptBounty(
                    player, bounty.BountyInfo, (success, position) =>
                {
                    if (success && StoreGui.instance != null && _merchantPanel != null)
                    {
                        RefreshItems(_merchantPanel.GetPlayerCurrencies());

                        if (StoreGui.instance.m_trader != null)
                        {
                            StoreGui.instance.m_trader.OnBought(new Trader.TradeItem { m_price = 0 });
                        }

                        StoreGui.instance.m_buyEffects?.Create(player.transform.position, Quaternion.identity);
                    }

                    _generatingBounty = false;
                    EpicLoot.Log($"Done trying to accept bounty. Success: {success}");
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
            MainButton.interactable = selectedItem != null && selectedItem.CanClaim;
            var tooltip = MainButton.GetComponent<UITooltip>();
            if (tooltip != null)
            {
                tooltip.m_text = "";
                if (selectedItem != null && !selectedItem.CanClaim)
                {
                    tooltip.m_text = "$mod_epicloot_bounties_notcompletetooltip";
                }
            }

            var canAbandon = selectedItem != null && selectedItem.BountyInfo.State == BountyState.InProgress;
            AbandonButton.interactable = canAbandon;
            AbandonButtonIcon.color = canAbandon ? Color.red : Color.grey;
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

                StoreGui.instance.m_trader.OnBought(new Trader.TradeItem { m_price = 0 });
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
