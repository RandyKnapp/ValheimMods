using System;
using System.Linq;
using EpicLoot.Crafting;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Adventure
{
    public class MerchantPanel : MonoBehaviour
    {
        public RectTransform SecretStashList;
        public BuyListElement SecretStashElementPrefab;
        public Button SecretStashBuyButton;
        public Text SecretStashRefreshTime;
        public RectTransform TreasureMapList;
        public BuyListElement TreasureMapElementPrefab;
        public Button TreasureMapBuyButton;
        public Text TreasureMapRefreshTime;
        public RectTransform AvailableBountiesList;
        public BountyListElement AvailableBountyElementPrefab;
        public Button AcceptBountyButton;
        public RectTransform ClaimableBountiesList;
        public BountyListElement ClaimableBountyElementPrefab;
        public Button ClaimBountyButton;
        public Text BountiesRefreshTime;
        public CraftSuccessDialog GambleSuccessDialog;

        public Text CoinsCount;

        private int _currentSecretStashInterval = -1;
        private int _currentPlayerCoins = 0;
        private int _selectedStashItemIndex = -1;

        public void Awake()
        {
            var storeGui = transform.parent.GetComponent<StoreGui>();
            gameObject.name = nameof(MerchantPanel);

            if (GambleSuccessDialog == null)
            {
                GambleSuccessDialog = CraftSuccessDialog.Create(transform);
                GambleSuccessDialog.Frame.anchoredPosition = new Vector2(-700, -300);
            }

            var existingBackground = storeGui.m_rootPanel.transform.Find("border (1)");
            if (existingBackground != null)
            {
                var image = existingBackground.GetComponent<Image>();
                GetComponent<Image>().material = image.material;
            }

            var scrollbars = GetComponentsInChildren<ScrollRect>(true);
            foreach (var scrollRect in scrollbars)
            {
                scrollRect.verticalScrollbar.size = 0.4f;
                scrollRect.onValueChanged.AddListener((_) => scrollRect.verticalScrollbar.size = 0.4f);
                scrollRect.normalizedPosition = new Vector2(0, 1);
            }

            var storeBuyButtonTooltip = storeGui.m_buyButton.GetComponent<UITooltip>().m_tooltipPrefab;
            var storeItemTooltip = storeGui.m_listElement.GetComponent<UITooltip>().m_tooltipPrefab;
            var tooltips = GetComponentsInChildren<UITooltip>(true);
            foreach (var tooltip in tooltips)
            {
                if (tooltip.name == "Sundial" || tooltip.name == "ItemElement")
                {
                    tooltip.m_tooltipPrefab = storeItemTooltip;
                }
                else
                {
                    tooltip.m_tooltipPrefab = storeBuyButtonTooltip;
                }
            }

            var secretStashRefreshTooltip = GetRefreshTimeTooltip(AdventureDataManager.SecretStashRefreshInterval);
            var treasureMapRefreshTooltip = GetRefreshTimeTooltip(AdventureDataManager.TreasureMapRefreshInterval);
            var bountiesRefreshTooltip = GetRefreshTimeTooltip(AdventureDataManager.BountiesRefreshInterval);
            transform.Find("Sundial").GetComponent<UITooltip>().m_text = $"Secret Stash: {secretStashRefreshTooltip}\nTreasure Maps: {treasureMapRefreshTooltip}\nBounties: {bountiesRefreshTooltip}";

            SecretStashList = transform.Find("SecretStash/Panel/ItemList") as RectTransform;
            SecretStashElementPrefab = transform.Find("SecretStash/Panel/ItemElement").gameObject.AddComponent<BuyListElement>();
            SecretStashElementPrefab.gameObject.SetActive(false);
            SecretStashBuyButton = transform.Find("SecretStash/SecretStashBuyButton").GetComponent<Button>();
            SecretStashBuyButton.onClick.AddListener(OnSecretStashBuyButtonPressed);
            SecretStashRefreshTime = transform.Find("SecretStash/TimeLeft").GetComponent<Text>();

            TreasureMapList = transform.Find("TreasureMap/Panel/ItemList") as RectTransform;
            TreasureMapElementPrefab = transform.Find("TreasureMap/Panel/ItemElement").gameObject.AddComponent<BuyListElement>();
            TreasureMapElementPrefab.gameObject.SetActive(false);
            TreasureMapBuyButton = transform.Find("TreasureMap/TreasureMapBuyButton").GetComponent<Button>();
            TreasureMapRefreshTime = transform.Find("TreasureMap/TimeLeft").GetComponent<Text>();

            AvailableBountiesList = transform.Find("Bounties/AvailableBountiesPanel/ItemList") as RectTransform;
            AvailableBountyElementPrefab = transform.Find("Bounties/AvailableBountiesPanel/ItemElement").gameObject.AddComponent<BountyListElement>();
            AvailableBountyElementPrefab.gameObject.SetActive(false);
            AcceptBountyButton = transform.Find("Bounties/AcceptBountyButton").GetComponent<Button>();

            ClaimableBountiesList = transform.Find("Bounties/ClaimableBountiesPanel/ItemList") as RectTransform;
            ClaimableBountyElementPrefab = transform.Find("Bounties/ClaimableBountiesPanel/ItemElement").gameObject.AddComponent<BountyListElement>();
            ClaimableBountyElementPrefab.gameObject.SetActive(false);
            ClaimBountyButton = transform.Find("Bounties/ClaimBountyButton").GetComponent<Button>();
            BountiesRefreshTime = transform.Find("Bounties/TimeLeft").GetComponent<Text>();

            CoinsCount = transform.Find("Currencies/CoinsCount").GetComponent<Text>();
        }

        private void OnSecretStashBuyButtonPressed()
        {
            var player = Player.m_localPlayer;
            var selectedStashItem = GetSelectedStashItem();
            if (player != null && selectedStashItem != null && CanAfford(selectedStashItem))
            {
                ItemDrop.ItemData item;
                if (selectedStashItem.IsGamble)
                {
                    item = AdventureDataManager.GenerateGambleItem(selectedStashItem.ItemInfo);
                }
                else
                {
                    item = selectedStashItem.Item.Clone();
                }

                var inventory = player.GetInventory();
                if (item == null || !inventory.AddItem(item))
                {
                    EpicLoot.LogWarning($"Could not buy item {selectedStashItem.Item.m_shared.m_name}");
                    return;
                }

                if (selectedStashItem.IsGamble)
                {
                    GambleSuccessDialog.Show(item);
                }

                inventory.RemoveItem(GetCoinsName(), selectedStashItem.Price);
                StoreGui.instance.m_trader.OnBought(null);
                StoreGui.instance.m_buyEffects.Create(player.transform.position, Quaternion.identity);
                Player.m_localPlayer.ShowPickupMessage(selectedStashItem.Item, selectedStashItem.Item.m_stack);

                //Gogan.LogEvent("Game", "BoughtItem", selectedStashItem.Item, 0L);
            }
        }

        private bool CanAfford(BuyListElement item)
        {
            return item.Price <= _currentPlayerCoins;
        }

        public void OnEnable()
        {
            RefreshSecretStashItems();
            RefreshTreasureMapItems();
        }

        private static string GetRefreshTimeTooltip(int refreshInterval)
        {
            return $"Every {(refreshInterval > 1 ? $"{refreshInterval} " : "")}in-game day{(refreshInterval > 1 ? "s" : "")}";
        }

        public void Update()
        {
            UpdateRefreshTime();
            UpdateCurrencies();

            var secretStashInterval = AdventureDataManager.GetCurrentSecretStashInterval();
            if (_currentSecretStashInterval != secretStashInterval)
            {
                _currentSecretStashInterval = secretStashInterval;
                RefreshSecretStashItems();
            }

            RefreshBuyButtons();
        }

        private void RefreshBuyButtons()
        {
            var selectedStashItem = GetSelectedStashItem();
            SecretStashBuyButton.interactable = selectedStashItem != null && selectedStashItem.Price <= _currentPlayerCoins;

            var selectedTreasureMapItem = GetSelectedTreasureMapItem();
            TreasureMapBuyButton.interactable = selectedTreasureMapItem != null && selectedTreasureMapItem.Price <= _currentPlayerCoins;
        }

        private BuyListElement GetSelectedStashItem()
        {
            return GetSelectedItem(SecretStashList);
        }

        private BuyListElement GetSelectedTreasureMapItem()
        {
            return GetSelectedItem(TreasureMapList);
        }

        private BuyListElement GetSelectedItem(RectTransform list)
        {
            for (int i = 0; i < list.childCount; i++)
            {
                var child = list.GetChild(i).GetComponent<BuyListElement>();
                if (child.IsSelected)
                {
                    return child;
                }
            }

            return null;
        }

        private void UpdateRefreshTime()
        {
            SecretStashRefreshTime.text = $"({AdventureDataManager.GetCurrentSecretStashInterval()}) " + ConvertSecondsToDisplayTime(AdventureDataManager.GetSecondsUntilSecretStashRefresh());
            TreasureMapRefreshTime.text = ConvertSecondsToDisplayTime(AdventureDataManager.GetSecondsUntilTreasureMapRefresh());
            BountiesRefreshTime.text = ConvertSecondsToDisplayTime(AdventureDataManager.GetSecondsUntilBountiesRefresh());
        }

        private void UpdateCurrencies()
        {
            var newCoinCount = Player.m_localPlayer.GetInventory().CountItems(GetCoinsName());
            if (_currentPlayerCoins != newCoinCount)
            {
                _currentPlayerCoins = newCoinCount;
                CoinsCount.text = _currentPlayerCoins.ToString();
                RefreshSecretStashItems();
                RefreshTreasureMapItems();
            }
        }

        private static string GetCoinsName()
        {
            return ObjectDB.instance.GetItemPrefab("Coins").GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
        }

        private static string ConvertSecondsToDisplayTime(int seconds)
        {
            if (seconds < 0)
            {
                return "???";
            }

            var timeSpan = new TimeSpan(0, 0, 0, seconds);
            if (timeSpan.Days > 0)
            {
                return timeSpan.ToString("d'd 'h'h 'm'm 's's'");

            }
            else if (timeSpan.Hours > 0)
            {
                return timeSpan.ToString(@"h'h 'm'm 's's'");
            }
            else
            {
                return timeSpan.ToString(@"m'm 's's'");
            }
        }

        public void RefreshSecretStashItems()
        {
            _currentSecretStashInterval = AdventureDataManager.GetCurrentSecretStashInterval();

            DestroyAllActiveListElementsInList(SecretStashList);
            var items = AdventureDataManager.GetItemsForSecretStash();
            var gambles = AdventureDataManager.GetGamblesForSecretStash();

            var allItems = items.Concat(gambles).ToList();
            for (var index = 0; index < allItems.Count; index++)
            {
                var itemInfo = allItems[index];
                var itemElement = Object.Instantiate(SecretStashElementPrefab, SecretStashList);
                itemElement.gameObject.SetActive(true);
                itemElement.SetItem(itemInfo, _currentPlayerCoins);
                var i = index;
                itemElement.OnSelected += (x) => OnStashItemSelected(x, i);
                itemElement.SetSelected(index == _selectedStashItemIndex);
            }
        }

        private void OnStashItemSelected(SecretStashItemInfo itemInfo, int index)
        {
            _selectedStashItemIndex = index;

            for (int i = 0; i < SecretStashList.childCount; i++)
            {
                var child = SecretStashList.GetChild(i).GetComponent<BuyListElement>();
                child.SetSelected(i == _selectedStashItemIndex);
            }
        }

        public void RefreshTreasureMapItems()
        {
        }

        public void DestroyAllActiveListElementsInList(RectTransform container)
        {
            for (int i = 0; i < container.childCount; i++)
            {
                var item = container.GetChild(i);
                if (!item.gameObject.activeSelf)
                {
                    continue;
                }
                Destroy(item.gameObject);
            }
        }
    }
}
