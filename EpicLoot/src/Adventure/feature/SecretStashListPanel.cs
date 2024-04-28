using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Adventure.Feature
{
    public class SecretStashListPanel : MerchantListPanel<BuyListElement>
    {
        private readonly MerchantPanel _merchantPanel;

        public SecretStashListPanel(MerchantPanel merchantPanel, BuyListElement elementPrefab) 
            : base(
                merchantPanel.transform.Find("SecretStash/Panel/ItemList") as RectTransform,
                elementPrefab,
                merchantPanel.transform.Find("SecretStash/SecretStashBuyButton").GetComponent<Button>(),
                merchantPanel.transform.Find("SecretStash/TimeLeft").GetComponent<Text>())
        {
            _merchantPanel = merchantPanel;
        }

        public override bool NeedsRefresh(bool currenciesChanged)
        {
            return currenciesChanged || _currentInterval != AdventureDataManager.SecretStash.GetCurrentInterval();
        }

        public override void RefreshButton(Currencies playerCurrencies)
        {
            var selectedItem = GetSelectedItem();
            var haveSpace = Player.m_localPlayer.GetInventory().FindEmptySlot(false).x >= 0 || Player.m_localPlayer.GetInventory().FindFreeStackSpace(selectedItem?.ItemInfo.Item.m_shared.m_name, default) > 0;
            MainButton.interactable = selectedItem != null && selectedItem.CanAfford(playerCurrencies) && haveSpace;
            var tooltip = MainButton.GetComponent<UITooltip>();
            if (tooltip != null)
            {
                tooltip.m_text = "";
                if (selectedItem != null && !selectedItem.CanAfford(playerCurrencies))
                {
                    tooltip.m_text = "$mod_epicloot_merchant_cannotafford";
                }
                else if (!haveSpace)
                {
                    tooltip.m_text = "$mod_epicloot_merchant_noroomtooltip";
                }
            }
        }

        public override void UpdateRefreshTime()
        {
            UpdateRefreshTime(AdventureDataManager.SecretStash.GetSecondsUntilRefresh());
        }

        public override void RefreshItems(Currencies currencies)
        {
            _currentInterval = AdventureDataManager.SecretStash.GetCurrentInterval();

            DestroyAllListElementsInList();
            var items = AdventureDataManager.SecretStash.GetSecretStashItems();
            var forestTokenItems = AdventureDataManager.SecretStash.GetForestTokenItems();

            var allItems = items.Concat(forestTokenItems).ToList();
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
