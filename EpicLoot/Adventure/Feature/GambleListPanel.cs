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

        public override bool NeedsRefresh(bool currenciesChanged)
        {
            return currenciesChanged || _currentInterval != AdventureDataManager.Gamble.GetCurrentInterval();
        }

        public override void RefreshButton(Currencies playerCurrencies)
        {
            var selectedItem = GetSelectedItem();
            MainButton.interactable = selectedItem != null && selectedItem.CanAfford(playerCurrencies);
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
            if (player != null && selectedItem != null)
            {
                _merchantPanel.BuyItem(player, selectedItem);
            }
        }
    }
}
