using System;
using System.Collections.Generic;
using EpicLoot.Adventure.Feature;
using EpicLoot.Crafting;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure
{
    [Serializable]
    public class Currencies
    {
        public int Coins;
        public int ForestTokens;
        public int IronBountyTokens;
        public int GoldBountyTokens;

        public Currencies(int init = 0)
        {
            Coins = init;
            ForestTokens = init;
            IronBountyTokens = init;
            GoldBountyTokens = init;
        }

        public Currencies Clone()
        {
            return new Currencies()
            {
                Coins = Coins,
                ForestTokens = ForestTokens,
                IronBountyTokens = IronBountyTokens,
                GoldBountyTokens = GoldBountyTokens
            };
        }
    }

    public class MerchantPanel : MonoBehaviour
    {
        public readonly List<IMerchantListPanel> Panels = new List<IMerchantListPanel>();

        public CraftSuccessDialog GambleSuccessDialog;
        public GameObject InputBlocker;

        public Text CoinsCount;
        public Text ForestTokensCount;
        public Text IronBountyTokensCount;
        public Text GoldBountyTokensCount;

        private readonly Currencies _currencies = new Currencies(-1);
        private static MerchantPanel _instance;

        public void Awake()
        {
            _instance = this;
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

            InputBlocker = transform.Find("InputBlocker").gameObject;
            InputBlocker.SetActive(false);

            var secretStashRefreshTooltip = GetRefreshTimeTooltip(AdventureDataManager.SecretStash.RefreshInterval);
            var gambleRefreshTooltip = GetRefreshTimeTooltip(AdventureDataManager.Gamble.RefreshInterval);
            var treasureMapRefreshTooltip = GetRefreshTimeTooltip(AdventureDataManager.TreasureMaps.RefreshInterval);
            var bountiesRefreshTooltip = GetRefreshTimeTooltip(AdventureDataManager.Bounties.RefreshInterval);
            transform.Find("Sundial").GetComponent<UITooltip>().m_text =
                $"Secret Stash: {secretStashRefreshTooltip}\n" +
                $"Gamble: {gambleRefreshTooltip}\n" +
                $"Treasure Maps: {treasureMapRefreshTooltip}\n" +
                $"Bounties: {bountiesRefreshTooltip}\n\n" +
                $"<color=silver>Rollover Time: Midnight</color>";

            var buyListPrefab = transform.Find("SecretStash/Panel/ItemElement").gameObject.AddComponent<BuyListElement>();
            buyListPrefab.gameObject.SetActive(false);
            var treasureMapElementPrefab = transform.Find("TreasureMap/Panel/ItemElement").gameObject.AddComponent<TreasureMapListElement>();
            treasureMapElementPrefab.gameObject.SetActive(false);
            var bountyElementPrefab = transform.Find("Bounties/AvailableBountiesPanel/ItemElement").gameObject.AddComponent<BountyListElement>();
            bountyElementPrefab.gameObject.SetActive(false);

            Panels.Add(new SecretStashListPanel(this, buyListPrefab));
            Panels.Add(new GambleListPanel(this, buyListPrefab));
            Panels.Add(new TreasureMapListPanel(this, treasureMapElementPrefab));
            Panels.Add(new AvailableBountiesListPanel(this, bountyElementPrefab));
            Panels.Add(new ClaimableBountiesListPanel(this, bountyElementPrefab));

            CoinsCount = transform.Find("Currencies/CoinsCount").GetComponent<Text>();
            ForestTokensCount = transform.Find("Currencies/ForestTokensCount").GetComponent<Text>();
            IronBountyTokensCount = transform.Find("Currencies/BountyTokensIronCount").GetComponent<Text>();
            GoldBountyTokensCount = transform.Find("Currencies/BountyTokensGoldCount").GetComponent<Text>();
        }

        public void OnEnable()
        {
            UpdateCurrencies();
            foreach (var panel in Panels)
            {
                panel.RefreshItems(_currencies);
            }

            if (InputBlocker != null)
            {
                InputBlocker.SetActive(false);
            }
        }

        public void OnDisable()
        {
            if (GambleSuccessDialog != null)
            {
                GambleSuccessDialog.OnClose();
            }
        }

        public void OnDestroy()
        {
            _instance = null;
        }

        public Currencies GetPlayerCurrencies()
        {
            return _currencies;
        }

        public void BuyItem(Player player, BuyListElement listItem)
        {
            ItemDrop.ItemData item;
            if (listItem.ItemInfo.IsGamble)
            {
                item = AdventureDataManager.Gamble.GenerateGambleItem(listItem.ItemInfo);
            }
            else
            {
                item = listItem.ItemInfo.Item.Clone();
            }

            var inventory = player.GetInventory();
            if (item == null || !inventory.AddItem(item))
            {
                EpicLoot.LogWarning($"Could not buy item {listItem.ItemInfo.Item.m_shared.m_name}");
                return;
            }

            if (listItem.ItemInfo.IsGamble)
            {
                GambleSuccessDialog.Show(item);
            }

            if (listItem.ItemInfo.Cost.Coins > 0)
            {
                inventory.RemoveItem(GetCoinsName(), listItem.ItemInfo.Cost.Coins);
            }

            if (listItem.ItemInfo.Cost.ForestTokens > 0)
            {
                inventory.RemoveItem(GetForestTokenName(), listItem.ItemInfo.Cost.ForestTokens);
            }

            if (listItem.ItemInfo.Cost.IronBountyTokens > 0)
            {
                inventory.RemoveItem(GetIronBountyTokenName(), listItem.ItemInfo.Cost.IronBountyTokens);
            }

            if (listItem.ItemInfo.Cost.GoldBountyTokens > 0)
            {
                inventory.RemoveItem(GetGoldBountyTokenName(), listItem.ItemInfo.Cost.GoldBountyTokens);
            }

            StoreGui.instance.m_trader.OnBought(null);
            StoreGui.instance.m_buyEffects.Create(player.transform.position, Quaternion.identity);
            Player.m_localPlayer.ShowPickupMessage(listItem.ItemInfo.Item, listItem.ItemInfo.Item.m_stack);

            //Gogan.LogEvent("Game", "BoughtItem", selectedStashItem.Item, 0L);
        }

        private static string GetRefreshTimeTooltip(int refreshInterval)
        {
            return $"<color=lightblue>Every {(refreshInterval > 1 ? $"{refreshInterval} " : "")}in-game day{(refreshInterval > 1 ? "s" : "")}</color>";
        }

        public void Update()
        {
            UpdateRefreshTime();
            var currenciesChanged = UpdateCurrencies();

            foreach (var panel in Panels)
            {
                if (panel.NeedsRefresh(currenciesChanged))
                {
                    panel.RefreshItems(_currencies);
                }
            }

            RefreshBuyButtons();
        }

        public void RefreshAll()
        {
            foreach (var panel in Panels)
            {
                panel.RefreshItems(_currencies);
                panel.RefreshButton(_currencies);
            }
        }

        private void RefreshBuyButtons()
        {
            foreach (var panel in Panels)
            {
                panel.RefreshButton(_currencies);
            }
        }

        private void UpdateRefreshTime()
        {
            foreach (var panel in Panels)
            {
                panel.UpdateRefreshTime();
            }
        }

        private bool UpdateCurrencies()
        {
            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();
            var currenciesChanged = false;

            var newCoinCount = inventory.CountItems(GetCoinsName());
            if (_currencies.Coins != newCoinCount)
            {
                _currencies.Coins = newCoinCount;
                CoinsCount.text = _currencies.Coins.ToString();
                currenciesChanged = true;
            }

            var newForestTokenCount = inventory.CountItems(GetForestTokenName());
            if (_currencies.ForestTokens != newForestTokenCount)
            {
                _currencies.ForestTokens = newForestTokenCount;
                ForestTokensCount.text = _currencies.ForestTokens.ToString();
                currenciesChanged = true;
            }

            var newIronBountyTokenCount = inventory.CountItems(GetIronBountyTokenName());
            if (newIronBountyTokenCount != _currencies.IronBountyTokens)
            {
                _currencies.IronBountyTokens = newIronBountyTokenCount;
                IronBountyTokensCount.text = _currencies.IronBountyTokens.ToString();
                currenciesChanged = true;
            }

            var newGoldBountyTokenCount = inventory.CountItems(GetGoldBountyTokenName());
            if (newGoldBountyTokenCount != _currencies.GoldBountyTokens)
            {
                _currencies.GoldBountyTokens = newGoldBountyTokenCount;
                GoldBountyTokensCount.text = _currencies.GoldBountyTokens.ToString();
                currenciesChanged = true;
            }

            return currenciesChanged;
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

        public static void ShowInputBlocker(bool show)
        {
            if (_instance != null && _instance.gameObject.activeSelf)
            {
                _instance.InputBlocker.SetActive(show);
            }
        }
    }
}
