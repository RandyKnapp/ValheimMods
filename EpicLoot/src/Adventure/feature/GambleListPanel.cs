using EpicLoot.Config;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Adventure.Feature
{
    public class GambleListPanel : MerchantListPanel<BuyListElement>
    {
        private readonly MerchantPanel _merchantPanel;

        public GambleListPanel(MerchantPanel merchantPanel, BuyListElement elementPrefab) 
            : base(
                merchantPanel.transform.Find("Gamble/Panel/ItemList") as RectTransform,
                elementPrefab,
                merchantPanel.transform.Find("Gamble/GambleBuyButton").GetComponent<Button>(),
                merchantPanel.transform.Find("Gamble/TimeLeft").GetComponent<Text>())
        {
            _merchantPanel = merchantPanel;
        }

        public override bool NeedsRefresh()
        {
            return _currentInterval != AdventureDataManager.Gamble.GetCurrentInterval();
        }

        public override void UpdateAffordability(Currencies currencies)
        {
            ForEachElement(x => x.ApplyAffordability(currencies));
        }

        public override void RefreshButton(Currencies playerCurrencies)
        {
            var selectedItem = GetSelectedItem();
            MainButton.interactable = selectedItem != null && selectedItem.CanAfford(playerCurrencies);
            var tooltip = MainButton.GetComponent<UITooltip>();
            if (tooltip != null)
            {
                tooltip.m_text = "";
                if (selectedItem != null && !selectedItem.CanAfford(playerCurrencies))
                {
                    tooltip.m_text = "$mod_epicloot_merchant_cannotafford";
                }
            }
        }

        public override void UpdateRefreshTime()
        {
            UpdateRefreshTime(AdventureDataManager.Gamble.GetSecondsUntilRefresh());
        }

        public override void RefreshItems(Currencies currencies)
        {
            _currentInterval = AdventureDataManager.Gamble.GetCurrentInterval();

            DestroyAllListElementsInList();
            var allItems = AdventureDataManager.Gamble.GetGambleItems();
            for (var index = 0; index < allItems.Count; index++)
            {
                var itemInfo = allItems[index];
                var itemElement = Object.Instantiate(ElementPrefab, List);
                itemElement.gameObject.SetActive(true);
                itemElement.SetItem(itemInfo, currencies);
                var i = index;
                itemElement.OnSelected += (x) => OnItemSelected(i);
                itemElement.SetSelected(index == _selectedItemIndex);
            }
        }

        protected override void OnMainButtonClicked()
        {
            var player = Player.m_localPlayer;
            var selectedItem = GetSelectedItem();
            if (player == null || selectedItem == null)
            {
                return;
            }

            // Captured before the rebuild below destroys the row that holds it.
            var itemInfo = selectedItem.ItemInfo;
            if (!_merchantPanel.BuyItem(player, selectedItem))
            {
                return;
            }

            AdventureDataManager.Gamble.RecordGamblePurchase(player, itemInfo);

            if (ELConfig.RemovePurchasedGambles.Value)
            {
                // The offer is gone from the list now, so the rows have to be rebuilt -- a currency
                // change on its own only recolours them. This clears the selection, which is right:
                // the row the player had selected no longer exists. The cached currencies are one
                // frame stale (the coins were just spent); the next Update fixes the colours.
                RefreshItems(_merchantPanel.GetPlayerCurrencies());
            }
        }
    }
}
