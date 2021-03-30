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
        public TreasureMapListElement TreasureMapElementPrefab;
        public Button TreasureMapBuyButton;
        public Text TreasureMapRefreshTime;
        public RectTransform AvailableBountiesList;
        public BountyListElement BountyElementPrefab;
        public Button AcceptBountyButton;
        public RectTransform ClaimableBountiesList;
        public Button ClaimBountyButton;
        public Text BountiesRefreshTime;
        public CraftSuccessDialog GambleSuccessDialog;

        public Text CoinsCount;
        public Text ForestTokensCount;
        public Text IronBountyTokensCount;
        public Text GoldBountyTokensCount;

        private int _currentSecretStashInterval = -1;
        private int _currentTreasureMapInterval = -1;
        private int _currentPlayerCoins = -1;
        private int _selectedStashItemIndex = -1;
        private int _selectedTreasureMapItemIndex = -1;
        private int _currentPlayerForestTokens = -1;
        private int _currentBountyInterval = -1;
        private int _selectedBountyItemIndex = -1;
        private int _selectedClaimBountyItemIndex = -1;
        private int _currentIronBountyTokens = -1;
        private int _currentGoldBountyTokens = -1;

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
            TreasureMapElementPrefab = transform.Find("TreasureMap/Panel/ItemElement").gameObject.AddComponent<TreasureMapListElement>();
            TreasureMapElementPrefab.gameObject.SetActive(false);
            TreasureMapBuyButton = transform.Find("TreasureMap/TreasureMapBuyButton").GetComponent<Button>();
            TreasureMapBuyButton.onClick.AddListener(OnTreasureMapBuyButtonPressed);
            TreasureMapRefreshTime = transform.Find("TreasureMap/TimeLeft").GetComponent<Text>();

            AvailableBountiesList = transform.Find("Bounties/AvailableBountiesPanel/ItemList") as RectTransform;
            BountyElementPrefab = transform.Find("Bounties/AvailableBountiesPanel/ItemElement").gameObject.AddComponent<BountyListElement>();
            BountyElementPrefab.gameObject.SetActive(false);
            AcceptBountyButton = transform.Find("Bounties/AcceptBountyButton").GetComponent<Button>();
            AcceptBountyButton.onClick.AddListener(OnAcceptBountyButtonPressed);

            ClaimableBountiesList = transform.Find("Bounties/ClaimableBountiesPanel/ItemList") as RectTransform;
            ClaimBountyButton = transform.Find("Bounties/ClaimBountyButton").GetComponent<Button>();
            ClaimBountyButton.onClick.AddListener(OnClaimBountyButtonPressed);
            BountiesRefreshTime = transform.Find("Bounties/TimeLeft").GetComponent<Text>();

            CoinsCount = transform.Find("Currencies/CoinsCount").GetComponent<Text>();
            ForestTokensCount = transform.Find("Currencies/ForestTokensCount").GetComponent<Text>();
            IronBountyTokensCount = transform.Find("Currencies/BountyTokensIronCount").GetComponent<Text>();
            GoldBountyTokensCount = transform.Find("Currencies/BountyTokensGoldCount").GetComponent<Text>();
        }

        public void OnEnable()
        {
            UpdateCurrencies();

            RefreshSecretStashItems();
            RefreshTreasureMapItems();
            RefreshBounties();
        }

        private void OnSecretStashBuyButtonPressed()
        {
            var player = Player.m_localPlayer;
            var selectedStashItem = GetSelectedStashItem();
            if (player != null && selectedStashItem != null && CanAfford(selectedStashItem))
            {
                BuyItem(player, selectedStashItem);
            }
        }

        private void BuyItem(Player player, BuyListElement listItem)
        {
            ItemDrop.ItemData item;
            if (listItem.IsGamble)
            {
                item = AdventureDataManager.GenerateGambleItem(listItem.ItemInfo);
            }
            else
            {
                item = listItem.Item.Clone();
            }

            var inventory = player.GetInventory();
            if (item == null || !inventory.AddItem(item))
            {
                EpicLoot.LogWarning($"Could not buy item {listItem.Item.m_shared.m_name}");
                return;
            }

            if (listItem.IsGamble)
            {
                GambleSuccessDialog.Show(item);
            }

            if (listItem.CoinsPrice > 0)
            {
                inventory.RemoveItem(GetCoinsName(), listItem.CoinsPrice);
            }

            if (listItem.ForestTokensPrice > 0)
            {
                inventory.RemoveItem(GetForestTokenName(), listItem.ForestTokensPrice);
            }

            StoreGui.instance.m_trader.OnBought(null);
            StoreGui.instance.m_buyEffects.Create(player.transform.position, Quaternion.identity);
            Player.m_localPlayer.ShowPickupMessage(listItem.Item, listItem.Item.m_stack);

            //Gogan.LogEvent("Game", "BoughtItem", selectedStashItem.Item, 0L);
        }

        private void OnTreasureMapBuyButtonPressed()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var treasureMap = GetSelectedTreasureMapItem();
            if (treasureMap != null)
            {
                if (CanAfford(treasureMap))
                {
                    player.StartCoroutine(AdventureDataManager.SpawnTreasureChest(treasureMap.Biome, player, (success, position) =>
                    {
                        if (success)
                        {
                            var inventory = player.GetInventory();
                            inventory.RemoveItem(GetCoinsName(), treasureMap.Price);

                            StoreGui.instance.m_trader.OnBought(null);
                            StoreGui.instance.m_buyEffects.Create(player.transform.position, Quaternion.identity);
                        }
                    }));
                }
            }
            else
            {
                var treasureMapBuyItem = GetSelectedItem<BuyListElement>(TreasureMapList, _selectedTreasureMapItemIndex);
                if (treasureMapBuyItem != null && CanAfford(treasureMapBuyItem))
                {
                    BuyItem(player, treasureMapBuyItem);
                }
            }
        }

        private static bool CanAfford(BuyListElement item)
        {
            return item.CanAfford;
        }

        private static bool CanAfford(TreasureMapListElement item)
        {
            return item.CanAfford;
        }

        private void OnAcceptBountyButtonPressed()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var bounty = GetSelectedAvailableBounty();
            if (bounty != null && bounty.BountyInfo.State == BountyState.Available)
            {
                player.StartCoroutine(AdventureDataManager.AcceptBounty(player, bounty.BountyInfo, (success, position) =>
                {
                    if (success)
                    {
                        RefreshBounties();

                        StoreGui.instance.m_trader.OnBought(null);
                        StoreGui.instance.m_buyEffects.Create(player.transform.position, Quaternion.identity);
                    }
                }));
            }
        }

        private void OnClaimBountyButtonPressed()
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var bounty = GetSelectedClaimableBounty();
            if (bounty != null && bounty.BountyInfo.State == BountyState.Complete)
            {
                AdventureDataManager.ClaimBountyReward(bounty.BountyInfo);

                RefreshBounties();

                StoreGui.instance.m_trader.OnBought(null);
                StoreGui.instance.m_buyEffects.Create(player.transform.position, Quaternion.identity);
            }
        }

        private static string GetRefreshTimeTooltip(int refreshInterval)
        {
            return $"<color=lightblue>Every {(refreshInterval > 1 ? $"{refreshInterval} " : "")}in-game day{(refreshInterval > 1 ? "s" : "")}</color>";
        }

        public void Update()
        {
            UpdateRefreshTime();
            var needsRefresh = UpdateCurrencies();

            var secretStashInterval = AdventureDataManager.GetCurrentSecretStashInterval();
            if (needsRefresh || _currentSecretStashInterval != secretStashInterval)
            {
                _currentSecretStashInterval = secretStashInterval;
                RefreshSecretStashItems();
            }

            var treasureMapInterval = AdventureDataManager.GetCurrentTreasureMapInterval();
            if (needsRefresh || _currentTreasureMapInterval != treasureMapInterval)
            {
                _currentTreasureMapInterval = treasureMapInterval;
                RefreshTreasureMapItems();
            }

            var bountyInterval = AdventureDataManager.GetCurrentBountiesInterval();
            if (_currentBountyInterval != bountyInterval)
            {
                _currentBountyInterval = bountyInterval;
                RefreshBounties();
            }

            RefreshBuyButtons();
        }

        private void RefreshBuyButtons()
        {
            var selectedStashItem = GetSelectedStashItem();
            SecretStashBuyButton.interactable = selectedStashItem != null && selectedStashItem.CanAfford;

            var selectedTreasureMapItem = GetSelectedTreasureMapItem();
            if (selectedTreasureMapItem != null)
            {
                TreasureMapBuyButton.interactable = selectedTreasureMapItem.CanAfford && !selectedTreasureMapItem.AlreadyPurchased;
            }
            else
            {
                var selectedTreasureMapPurchase = GetSelectedItem<BuyListElement>(TreasureMapList, _selectedTreasureMapItemIndex);
                TreasureMapBuyButton.interactable = selectedTreasureMapPurchase != null && selectedTreasureMapPurchase.CanAfford;
            }

            var selectedAvailableBounty = GetSelectedAvailableBounty();
            AcceptBountyButton.interactable = selectedAvailableBounty != null && selectedAvailableBounty.CanAccept;

            var selectedClaimableBounty = GetSelectedClaimableBounty();
            ClaimBountyButton.interactable = selectedClaimableBounty != null && selectedClaimableBounty.CanClaim;
        }

        private BuyListElement GetSelectedStashItem()
        {
            return GetSelectedItem<BuyListElement>(SecretStashList, _selectedStashItemIndex);
        }

        private TreasureMapListElement GetSelectedTreasureMapItem()
        {
            return GetSelectedItem<TreasureMapListElement>(TreasureMapList, _selectedTreasureMapItemIndex);
        }

        private BountyListElement GetSelectedAvailableBounty()
        {
            return GetSelectedItem<BountyListElement>(AvailableBountiesList, _selectedBountyItemIndex);
        }

        private BountyListElement GetSelectedClaimableBounty()
        {
            return GetSelectedItem<BountyListElement>(ClaimableBountiesList, _selectedClaimBountyItemIndex);
        }

        private T GetSelectedItem<T>(RectTransform list, int selectedIndex) where T : Component
        {
            for (int i = 0; i < list.childCount; i++)
            {
                var child = list.GetChild(i).GetComponent<T>();
                if (i == selectedIndex)
                {
                    return child;
                }
            }

            return null;
        }

        private void UpdateRefreshTime()
        {
            SecretStashRefreshTime.text = ConvertSecondsToDisplayTime(AdventureDataManager.GetSecondsUntilSecretStashRefresh());
            TreasureMapRefreshTime.text = ConvertSecondsToDisplayTime(AdventureDataManager.GetSecondsUntilTreasureMapRefresh());
            BountiesRefreshTime.text = ConvertSecondsToDisplayTime(AdventureDataManager.GetSecondsUntilBountiesRefresh());
        }

        private bool UpdateCurrencies()
        {
            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();
            var needsRefresh = false;

            var newCoinCount = inventory.CountItems(GetCoinsName());
            if (_currentPlayerCoins != newCoinCount)
            {
                _currentPlayerCoins = newCoinCount;
                CoinsCount.text = _currentPlayerCoins.ToString();
                needsRefresh = true;
            }

            var newForestTokenCount = inventory.CountItems(GetForestTokenName());
            if (_currentPlayerForestTokens != newForestTokenCount)
            {
                _currentPlayerForestTokens = newForestTokenCount;
                ForestTokensCount.text = _currentPlayerForestTokens.ToString();
                needsRefresh = true;
            }

            var newIronBountyTokenCount = inventory.CountItems(GetIronBountyTokenName());
            if (newIronBountyTokenCount != _currentIronBountyTokens)
            {
                _currentIronBountyTokens = newIronBountyTokenCount;
                IronBountyTokensCount.text = _currentIronBountyTokens.ToString();
            }

            var newGoldBountyTokenCount = inventory.CountItems(GetGoldBountyTokenName());
            if (newGoldBountyTokenCount != _currentGoldBountyTokens)
            {
                _currentGoldBountyTokens = newGoldBountyTokenCount;
                GoldBountyTokensCount.text = _currentGoldBountyTokens.ToString();
            }

            return needsRefresh;
        }

        public static string GetCoinsName()
        {
            return ObjectDB.instance.GetItemPrefab("Coins").GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
        }

        public static string GetForestTokenName()
        {
            return ObjectDB.instance.GetItemPrefab("ForestToken").GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
        }

        public static string GetIronBountyTokenName()
        {
            return ObjectDB.instance.GetItemPrefab("IronBountyToken").GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
        }

        public static string GetGoldBountyTokenName()
        {
            return ObjectDB.instance.GetItemPrefab("GoldBountyToken").GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
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
                var itemElement = Instantiate(SecretStashElementPrefab, SecretStashList);
                itemElement.gameObject.SetActive(true);
                itemElement.SetItem(itemInfo, _currentPlayerCoins, _currentPlayerForestTokens);
                var i = index;
                itemElement.OnSelected += (x) => OnStashItemSelected(i);
                itemElement.SetSelected(index == _selectedStashItemIndex);
            }
        }

        private void OnStashItemSelected(int index)
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
            _currentTreasureMapInterval = AdventureDataManager.GetCurrentTreasureMapInterval();

            DestroyAllActiveListElementsInList(TreasureMapList);

            var maps = AdventureDataManager.GetTreasureMaps();
            for (var index = 0; index < maps.Count; index++)
            {
                var itemInfo = maps[index];
                var itemElement = Instantiate(TreasureMapElementPrefab, TreasureMapList);
                itemElement.gameObject.SetActive(true);
                itemElement.SetItem(itemInfo, _currentPlayerCoins);
                var i = index;
                itemElement.OnSelected += (x) => OnTreasureMapItemSelected(i);
                itemElement.SetSelected(i == _selectedTreasureMapItemIndex);
            }

            var items = AdventureDataManager.GetForestTokenItems();
            for (int index = 0; index < items.Count; index++)
            {
                var itemInfo = items[index];
                var itemElement = Instantiate(SecretStashElementPrefab, TreasureMapList);
                itemElement.gameObject.SetActive(true);
                itemElement.SetItem(itemInfo, _currentPlayerCoins, _currentPlayerForestTokens);
                var i = maps.Count + index;
                itemElement.OnSelected += (x) => OnTreasureMapItemSelected(i);
                itemElement.SetSelected(i == _selectedTreasureMapItemIndex);
            }
        }

        private void OnTreasureMapItemSelected(int index)
        {
            _selectedTreasureMapItemIndex = index;

            for (int i = 0; i < TreasureMapList.childCount; i++)
            {
                var child = TreasureMapList.GetChild(i);
                var selected = i == _selectedTreasureMapItemIndex;
                var treasureMap = child.GetComponent<TreasureMapListElement>();
                if (treasureMap != null)
                {
                    treasureMap.SetSelected(selected);
                }

                var item = child.GetComponent<BuyListElement>();
                if (item != null)
                {
                    item.SetSelected(selected);
                }
            }
        }

        private void RefreshBounties()
        {
            _currentBountyInterval = AdventureDataManager.GetCurrentBountiesInterval();

            DestroyAllActiveListElementsInList(AvailableBountiesList);
            DestroyAllActiveListElementsInList(ClaimableBountiesList);

            var availableBounties = AdventureDataManager.GetAvailableBounties();
            for (int index = 0; index < availableBounties.Count; index++)
            {
                var itemInfo = availableBounties[index];
                var itemElement = Instantiate(BountyElementPrefab, AvailableBountiesList);
                itemElement.gameObject.SetActive(true);
                itemElement.SetItem(itemInfo);
                var i = index;
                itemElement.OnSelected += (x) => OnAvailableBountyItemSelected(i);
                itemElement.SetSelected(i == _selectedBountyItemIndex);
            }

            var claimableBounties = AdventureDataManager.GetClaimableBounties();
            for (int index = 0; index < claimableBounties.Count; index++)
            {
                var itemInfo = claimableBounties[index];
                var itemElement = Instantiate(BountyElementPrefab, ClaimableBountiesList);
                itemElement.gameObject.SetActive(true);
                itemElement.SetItem(itemInfo);
                var i = index;
                itemElement.OnSelected += (x) => OnClaimableBountyItemSelected(i);
                itemElement.SetSelected(i == _selectedClaimBountyItemIndex);
            }
        }

        private void OnAvailableBountyItemSelected(int index)
        {
            _selectedBountyItemIndex = index;

            for (int i = 0; i < AvailableBountiesList.childCount; i++)
            {
                var child = AvailableBountiesList.GetChild(i).GetComponent<BountyListElement>();
                child.SetSelected(i == _selectedBountyItemIndex);
            }
        }

        private void OnClaimableBountyItemSelected(int index)
        {
            _selectedClaimBountyItemIndex = index;

            for (int i = 0; i < ClaimableBountiesList.childCount; i++)
            {
                var child = ClaimableBountiesList.GetChild(i).GetComponent<BountyListElement>();
                child.SetSelected(i == _selectedClaimBountyItemIndex);
            }
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
