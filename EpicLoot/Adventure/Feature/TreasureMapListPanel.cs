using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Adventure.Feature
{
    class TreasureMapListPanel : MerchantListPanel<TreasureMapListElement>
    {
        private readonly MerchantPanel _merchantPanel;

        public TreasureMapListPanel(MerchantPanel merchantPanel, TreasureMapListElement elementPrefab) 
            : base(
                merchantPanel.transform.Find("TreasureMap/Panel/ItemList") as RectTransform,
                elementPrefab,
                merchantPanel.transform.Find("TreasureMap/TreasureMapBuyButton").GetComponent<Button>(),
                merchantPanel.transform.Find("TreasureMap/TimeLeft").GetComponent<Text>())
        {
            _merchantPanel = merchantPanel;
        }

        public override bool NeedsRefresh(bool currenciesChanged)
        {
            return currenciesChanged || _currentInterval != AdventureDataManager.TreasureMaps.GetCurrentInterval();
        }

        public override void RefreshButton(Currencies playerCurrencies)
        {
            var selectedItem = GetSelectedItem();
            MainButton.interactable = selectedItem != null && selectedItem.CanAfford && !selectedItem.AlreadyPurchased;
        }

        protected override void OnMainButtonClicked()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var treasureMap = GetSelectedItem();
            if (treasureMap != null)
            {
                player.StartCoroutine(AdventureDataManager.TreasureMaps.SpawnTreasureChest(treasureMap.Biome, player, (success, position) =>
                {
                    if (success)
                    {
                        var inventory = player.GetInventory();
                        inventory.RemoveItem(MerchantPanel.GetCoinsName(), treasureMap.Price);

                        StoreGui.instance.m_trader.OnBought(null);
                        StoreGui.instance.m_buyEffects.Create(player.transform.position, Quaternion.identity);
                    }
                }));
            }
        }

        public override void RefreshItems(Currencies currencies)
        {
            _currentInterval = AdventureDataManager.TreasureMaps.GetCurrentInterval();

            DestroyAllListElementsInList();
            var allItems = AdventureDataManager.TreasureMaps.GetTreasureMaps();
            for (var index = 0; index < allItems.Count; index++)
            {
                var itemInfo = allItems[index];
                var itemElement = Object.Instantiate(ElementPrefab, List);
                itemElement.gameObject.SetActive(true);
                itemElement.SetItem(itemInfo, currencies.Coins);
                var i = index;
                itemElement.OnSelected += (x) => OnItemSelected(i);
                itemElement.SetSelected(i == _selectedItemIndex);
            }
        }

        public override void UpdateRefreshTime()
        {
            UpdateRefreshTime(AdventureDataManager.TreasureMaps.GetSecondsUntilRefresh());

        }
    }
}
