using EpicLoot.Config;
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
                merchantPanel.transform.Find("Bounties/AvailableLabel"),
                elementPrefab,
                merchantPanel.transform.Find("Bounties/AcceptBountyButton").GetComponent<Button>(),
                merchantPanel.transform.Find("Bounties/TimeLeft").GetComponent<Text>())
        {
            _merchantPanel = merchantPanel;
        }

        public override bool NeedsRefresh()
        {
            return _currentInterval != AdventureDataManager.Bounties.GetCurrentInterval();
        }

        public override void RefreshButton(Currencies playerCurrencies)
        {
            var selectedItem = GetSelectedItem();

            // Runs from Update, which keeps ticking through a death, a respawn and a world change --
            // every other panel already tolerates a missing player here.
            var player = Player.m_localPlayer;
            if (player == null)
            {
                MainButton.interactable = false;
                return;
            }

            var saveData = player.GetAdventureSaveData();
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
            if (bounty == null || bounty.BountyInfo.State != BountyState.Available || !TryBeginAction())
            {
                return;
            }

            EpicLoot.Log("Trying to accept bounty...");

            // Hosted on the adventure driver rather than the Player: a coroutine on the player dies
            // with that object on death, logout or world change, and its completion callback -- the
            // one that clears the latch and refreshes the list -- would never run.
            AdventureCacheDriver.Run(AdventureDataManager.Bounties.AcceptBounty(
                player, bounty.BountyInfo, (success, position) =>
            {
                if (success && StoreGui.instance != null && _merchantPanel != null)
                {
                    RefreshItems(_merchantPanel.GetPlayerCurrencies());

                    if (StoreGui.instance.m_trader != null)
                    {
                        StoreGui.instance.m_trader.OnBought(new Trader.TradeItem { m_price = 0 });
                    }

                    // The player can be gone by the time this lands, since the coroutine now
                    // outlives them.
                    var localPlayer = Player.m_localPlayer;
                    if (localPlayer != null)
                    {
                        StoreGui.instance.m_buyEffects?.Create(localPlayer.transform.position, Quaternion.identity);
                    }
                }

                EndAction();
                EpicLoot.Log($"Done trying to accept bounty. Success: {success}");
            }));
        }

        public override void RefreshItems(Currencies currencies)
        {
            // Rows are gathered before the old ones are destroyed. The other order left the list
            // permanently empty whenever the gather threw, since nothing puts rows back until the next
            // refresh -- and the next refresh throws in the same place.
            var allItems = AdventureDataManager.Bounties.GetAvailableBounties();

            _currentInterval = AdventureDataManager.Bounties.GetCurrentInterval();
            DestroyAllListElementsInList();

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
                merchantPanel.transform.Find("Bounties/ClaimLabel"),
                elementPrefab,
                merchantPanel.transform.Find("Bounties/ClaimBountyButton").GetComponent<Button>(),
                null)
        {
            _merchantPanel = merchantPanel;

            AbandonButton = merchantPanel.transform.Find("Bounties/AbandonBountyButton").GetComponent<Button>();
            AbandonButton.onClick.AddListener(OnAbandonButtonClicked);

            AbandonButtonIcon = AbandonButton.transform.Find("Icon").GetComponent<Image>();
        }

        public override Button GetSecondaryButton()
        {
            return AbandonButton;
        }

        public override bool NeedsRefresh()
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
            // Gathered before the destroy, for the reason given in AvailableBountiesListPanel.
            var allItems = AdventureDataManager.Bounties.GetClaimableBounties();

            _currentInterval = AdventureDataManager.Bounties.GetCurrentInterval();
            DestroyAllListElementsInList();

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
