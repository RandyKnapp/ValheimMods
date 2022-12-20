using System;
using System.Collections.Generic;
using EpicLoot.Adventure.Feature;
using EpicLoot.Crafting;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

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
        public AbandonBountyDialog AbandonBountyDialog;

        public Text CoinsCount;
        public Text ForestTokensCount;
        public Text IronBountyTokensCount;
        public Text GoldBountyTokensCount;

        private readonly Currencies _currencies = new Currencies(-1);
        private static MerchantPanel _instance;
        private AudioSource _audioSource;

        public void Awake()
        {
            _instance = this;
            var storeGui = transform.parent.GetComponent<StoreGui>();
            gameObject.name = nameof(MerchantPanel);

            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (GambleSuccessDialog == null)
            {
                if (EpicLoot.HasAuga)
                {
                    var resultsPanel = Auga.API.Workbench_CreateNewResultsPanel();
                    resultsPanel.SetActive(false);
                    resultsPanel.transform.SetParent(transform);
                    GambleSuccessDialog = resultsPanel.gameObject.AddComponent<CraftSuccessDialog>();
                    GambleSuccessDialog.NameText = GambleSuccessDialog.transform.Find("Topic").GetComponent<Text>();
                    GambleSuccessDialog.Frame = (RectTransform)GambleSuccessDialog.transform;
                    GambleSuccessDialog.Frame.anchoredPosition = new Vector2(0, 0);
                }
                else
                {
                    GambleSuccessDialog = CraftSuccessDialog.Create(transform);
                    GambleSuccessDialog.Frame.anchoredPosition = new Vector2(-700, -300);
                }
            }

            if (AbandonBountyDialog == null)
            {
                if (EpicLoot.HasAuga)
                {
                    AugaReplaceButton(transform.Find("AbandonBountyDialog/YesButton").GetComponent<Button>());
                    AugaReplaceButton(transform.Find("AbandonBountyDialog/NoButton").GetComponent<Button>());
                }

                AbandonBountyDialog = transform.Find("AbandonBountyDialog").gameObject.AddComponent<AbandonBountyDialog>();
                AbandonBountyDialog.gameObject.SetActive(false);
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

            var sundialTooltip = transform.Find("Sundial").GetComponent<UITooltip>();
            if (EpicLoot.HasAuga)
            {
                Auga.API.Tooltip_MakeSimpleTooltip(sundialTooltip.gameObject);
                var rt = (RectTransform)sundialTooltip.transform;
                rt.anchoredPosition = new Vector2(20, -20);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 40);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40);
            }

            sundialTooltip.m_text =
                $"$mod_epicloot_merchant_secretstash: {secretStashRefreshTooltip}\n" +
                $"$mod_epicloot_merchant_gamble: {gambleRefreshTooltip}\n" +
                $"$mod_epicloot_merchant_treasuremaps: {treasureMapRefreshTooltip}\n" +
                $"$mod_epicloot_merchant_bounties: {bountiesRefreshTooltip}\n\n" +
                $"<color=silver>$mod_epicloot_merchant_rollovertime</color>";

            var buyListPrefab = transform.Find("SecretStash/Panel/ItemElement").gameObject.AddComponent<BuyListElement>();
            buyListPrefab.gameObject.SetActive(false);
            var treasureMapElementPrefab = transform.Find("TreasureMap/Panel/ItemElement").gameObject.AddComponent<TreasureMapListElement>();
            treasureMapElementPrefab.gameObject.SetActive(false);
            var bountyElementPrefab = transform.Find("Bounties/AvailableBountiesPanel/ItemElement").gameObject.AddComponent<BountyListElement>();
            bountyElementPrefab.gameObject.SetActive(false);
            var activeBountyElementPrefab = transform.Find("Bounties/ClaimableBountiesPanel/ItemElement").gameObject.AddComponent<BountyListElement>();
            bountyElementPrefab.gameObject.SetActive(false);

            if (EpicLoot.HasAuga)
            {
                AugaReplaceButton(transform.Find("SecretStash/SecretStashBuyButton").GetComponent<Button>());
                AugaReplaceButton(transform.Find("Gamble/GambleBuyButton").GetComponent<Button>());
                AugaReplaceButton(transform.Find("TreasureMap/TreasureMapBuyButton").GetComponent<Button>());
                AugaReplaceButton(transform.Find("Bounties/AcceptBountyButton").GetComponent<Button>());
                AugaReplaceButton(transform.Find("Bounties/ClaimBountyButton").GetComponent<Button>());
                AugaReplaceButton(transform.Find("Bounties/AbandonBountyButton").GetComponent<Button>(), true);
            }

            Panels.Add(new SecretStashListPanel(this, buyListPrefab));
            Panels.Add(new GambleListPanel(this, buyListPrefab));
            Panels.Add(new TreasureMapListPanel(this, treasureMapElementPrefab));
            Panels.Add(new AvailableBountiesListPanel(this, bountyElementPrefab));
            Panels.Add(new ClaimableBountiesListPanel(this, activeBountyElementPrefab));

            CoinsCount = transform.Find("Currencies/CoinsCount").GetComponent<Text>();
            ForestTokensCount = transform.Find("Currencies/ForestTokensCount").GetComponent<Text>();
            IronBountyTokensCount = transform.Find("Currencies/BountyTokensIronCount").GetComponent<Text>();
            GoldBountyTokensCount = transform.Find("Currencies/BountyTokensGoldCount").GetComponent<Text>();

            if (EpicLoot.HasAuga)
            {
                AugaReplaceBackground(gameObject, true);
                AugaReplaceBackground(AbandonBountyDialog.gameObject, false);

                AugaFixItemBG(buyListPrefab.gameObject);
                AugaFixItemBG(treasureMapElementPrefab.gameObject);
                AugaFixItemBG(bountyElementPrefab.gameObject);
                AugaFixItemBG(activeBountyElementPrefab.gameObject);
                AugaFixItemBG(AbandonBountyDialog.BountyDisplay.gameObject);

                AugaFixListElementColors(buyListPrefab.gameObject);
                AugaFixListElementColors(treasureMapElementPrefab.gameObject);
                AugaFixListElementColors(bountyElementPrefab.gameObject);
                AugaFixListElementColors(activeBountyElementPrefab.gameObject);
                AugaFixListElementColors(AbandonBountyDialog.BountyDisplay.gameObject);

                AugaFixFonts(gameObject);

                AugaMakeSimpleTooltip(treasureMapElementPrefab.gameObject);
                AugaMakeSimpleTooltip(bountyElementPrefab.gameObject);
                AugaMakeSimpleTooltip(activeBountyElementPrefab.gameObject);
            }
        }

        public static void AugaReplaceBackground(GameObject obj, bool withCornerDecoration)
        {
            var image = obj.GetComponent<Image>();
            Destroy(image);
            var backgroundPanel = Auga.API.Panel_Create(obj.transform, Vector2.one, "AugaBackground", withCornerDecoration);
            var rt = (RectTransform)backgroundPanel.transform;
            rt.SetSiblingIndex(0);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(40, 40);
        }

        public static void AugaFixItemBG(GameObject obj)
        {
            var magicBG = obj.transform.Find("MagicBG");
            if (magicBG != null)
            {
                var image = magicBG.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = EpicLoot.GetMagicItemBgSprite();
                }
            }

            var icon = obj.transform.Find("Icon");
            if (icon != null)
            {
                var iconBG = Object.Instantiate(icon, icon.parent);
                iconBG.SetSiblingIndex(2);
                var image = iconBG.GetComponent<Image>();
                image.sprite = Auga.API.GetItemBackgroundSprite();
                image.color = new Color(0, 0, 0, 0.5f);
            }
        }

        public static void AugaFixListElementColors(GameObject obj)
        {
            var selected = obj.transform.Find("Selected");
            if (selected != null)
            {
                var image = selected.GetComponent<Image>();
                if (image != null)
                {
                    ColorUtility.TryParseHtmlString(Auga.API.Blue, out var color);
                    image.color = color;
                }
            }

            var background = obj.transform.Find("Background");
            if (background != null)
            {
                var image = background.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0, 0, 0, 0.5f);
                }
            }

            var button = obj.GetComponent<Button>();
            if (button != null)
            {
                button.colors = ColorBlock.defaultColorBlock;
            }
        }

        public static void AugaFixFonts(GameObject obj)
        {
            var texts = obj.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                if (text.font.name == "Norsebold")
                {
                    text.font = Auga.API.GetBoldFont();
                    ColorUtility.TryParseHtmlString(Auga.API.Brown1, out var color);
                    text.color = color;
                    text.text = text.text.ToUpperInvariant();
                }
                else
                {
                    text.font = Auga.API.GetSemiBoldFont();
                }

                if (text.name == "Count" || text.name == "RewardLabel" || text.name.EndsWith("Count"))
                {
                    ColorUtility.TryParseHtmlString(Auga.API.BrightGold, out var color);
                    text.color = color;
                }

                text.text = text.text.Replace("<color=yellow>", $"<color={Auga.API.BrightGold}>");
            }
        }

        public static Button AugaReplaceButton(Button button, bool icon = false)
        {
            var newButton = Auga.API.MediumButton_Create(button.transform.parent, button.name, string.Empty);
            if (icon)
            {
                Object.Destroy(newButton.GetComponentInChildren<Text>().gameObject);
                var newIcon = Object.Instantiate(button.transform.Find("Icon"), newButton.transform);
                newIcon.name = "Icon";
            }
            else
            {
                var newLabel = button.GetComponentInChildren<Text>().text;
                newButton.GetComponentInChildren<Text>().text = newLabel;
            }
            var oldRt = (RectTransform)button.transform;
            var rt = (RectTransform)newButton.transform;
            rt.anchorMin = oldRt.anchorMin;
            rt.anchorMax = oldRt.anchorMax;
            rt.pivot = oldRt.pivot;
            rt.anchoredPosition = oldRt.anchoredPosition;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, oldRt.rect.width);

            Object.DestroyImmediate(button.gameObject);
            return newButton;
        }

        public static void AugaMakeSimpleTooltip(GameObject obj)
        {
            Auga.API.Tooltip_MakeSimpleTooltip(obj);
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
                GambleSuccessDialog.Close();
            }

            if (AbandonBountyDialog != null)
            {
                AbandonBountyDialog.Close();
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
                var itemDrop = AdventureFeature.CreateItemDrop(listItem.ItemInfo.ItemID);
                item = itemDrop.m_itemData.Clone();
                ZNetScene.instance.Destroy(itemDrop.gameObject);
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
            var message = refreshInterval > 1 ?
                Localization.instance.Localize("$mod_epicloot_merchant_refreshdays", refreshInterval.ToString()) : 
                "$mod_epicloot_merchant_refreshday";
            return $"<color=lightblue>{message}</color>";
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

        public void OnAbandonBounty()
        {
            RefreshAll();
            if (_audioSource != null)
            {
                _audioSource.PlayOneShot(EpicLoot.Assets.AbandonBountySFX);
            }
        }
    }
}
