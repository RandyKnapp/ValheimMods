using EpicLoot.Adventure.Feature;
using EpicLoot.Config;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using EpicLoot_UnityLib;
using System;
using System.Collections.Generic;
using TMPro;
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
        public AbandonBountyDialog AbandonBountyDialog;

        public Text CoinsCount;
        public Text ForestTokensCount;
        public Text IronBountyTokensCount;
        public Text GoldBountyTokensCount;

        public MerchantPanelGamepad Gamepad { get; private set; }

        private readonly Currencies _currencies = new Currencies(-1);
        public static MerchantPanel Instance => _instance;
        // The Accept button's label: Auga's TMP one, else the prefab's legacy Text. Resolved before the
        // gamepad hints are built, since their glyph is a TMP text under the same button.
        private static Component _acceptBountyLabel;
        private static MerchantPanel _instance;
        private AudioSource _audioSource;

        private RectTransform _rt;

        /// <summary>
        /// Indices into <see cref="Panels"/> that threw since this window was opened. Cleared by
        /// OnEnable, so closing and reopening the merchant gives every section another go.
        /// </summary>
        private readonly HashSet<int> _failedPanels = new HashSet<int>();

        public void Awake()
        {
            _instance = this;
            var storeGui = transform.parent.GetComponent<StoreGui>();
            gameObject.name = nameof(MerchantPanel);

            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            EnchantingUIController.SetupUIAudioSource(_audioSource);

            if (GambleSuccessDialog == null)
            {
                GambleSuccessDialog = EpicLoot.HasAuga ? CraftSuccessDialog.CreateAuga(transform) : null;
                if (GambleSuccessDialog == null)
                {
                    GambleSuccessDialog = CraftSuccessDialog.Create(transform);
                    GambleSuccessDialog.Frame.anchoredPosition = new Vector2(-700, -300);
                }
            }

            if (AbandonBountyDialog == null)
            {
                if (EpicLoot.HasAuga)
                {
                    EpicLootAuga.ReplaceButton(transform.Find("AbandonBountyDialog/YesButton").GetComponent<Button>());
                    EpicLootAuga.ReplaceButton(transform.Find("AbandonBountyDialog/NoButton").GetComponent<Button>());
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
                $"<color=#c0c0c0ff>$mod_epicloot_merchant_rollovertime</color>";

            var buyListPrefab = transform.Find("SecretStash/Panel/ItemElement").gameObject.AddComponent<BuyListElement>();
            buyListPrefab.gameObject.SetActive(false);
            var treasureMapElementPrefab = transform.Find("TreasureMap/Panel/ItemElement").gameObject.AddComponent<TreasureMapListElement>();
            treasureMapElementPrefab.gameObject.SetActive(false);
            var bountyElementPrefab = transform.Find("Bounties/AvailableBountiesPanel/ItemElement").gameObject.AddComponent<BountyListElement>();
            bountyElementPrefab.gameObject.SetActive(false);
            var activeBountyElementPrefab = transform.Find("Bounties/ClaimableBountiesPanel/ItemElement").gameObject.AddComponent<BountyListElement>();
            activeBountyElementPrefab.gameObject.SetActive(false);

            if (EpicLoot.HasAuga)
            {
                EpicLootAuga.ReplaceButton(transform.Find("SecretStash/SecretStashBuyButton").GetComponent<Button>());
                EpicLootAuga.ReplaceButton(transform.Find("Gamble/GambleBuyButton").GetComponent<Button>());
                EpicLootAuga.ReplaceButton(transform.Find("TreasureMap/TreasureMapBuyButton").GetComponent<Button>());
                EpicLootAuga.ReplaceButton(transform.Find("Bounties/AcceptBountyButton").GetComponent<Button>());
                EpicLootAuga.ReplaceButton(transform.Find("Bounties/ClaimBountyButton").GetComponent<Button>());
                EpicLootAuga.ReplaceButton(transform.Find("Bounties/AbandonBountyButton").GetComponent<Button>(), true);
            }
            
            Transform acceptBountyButton = transform.Find("Bounties/AcceptBountyButton");
            _acceptBountyLabel = acceptBountyButton == null ? null
                : (Component)acceptBountyButton.GetComponentInChildren<TMP_Text>(true) ?? acceptBountyButton.GetComponentInChildren<Text>(true);

            Panels.Add(new SecretStashListPanel(this, buyListPrefab));
            Panels.Add(new GambleListPanel(this, buyListPrefab));
            Panels.Add(new TreasureMapListPanel(this, treasureMapElementPrefab));
            Panels.Add(new AvailableBountiesListPanel(this, bountyElementPrefab));
            Panels.Add(new ClaimableBountiesListPanel(this, activeBountyElementPrefab));

            CoinsCount = transform.Find("Currencies/CoinsCount").GetComponent<Text>();
            ForestTokensCount = transform.Find("Currencies/ForestTokensCount").GetComponent<Text>();
            IronBountyTokensCount = transform.Find("Currencies/BountyTokensIronCount").GetComponent<Text>();
            GoldBountyTokensCount = transform.Find("Currencies/BountyTokensGoldCount").GetComponent<Text>();

            // Warm the world biome index while the player browses, so accepting a bounty does not wait
            // on a first build. It runs on its own driver, since StoreGui deactivates this panel on close.
            WorldBiomeIndex.EnsureBuilt();

            if (EpicLoot.HasAuga)
            {
                EpicLootAuga.ReplaceBackground(gameObject, true);
                EpicLootAuga.ReplaceBackground(AbandonBountyDialog.gameObject, false);

                EpicLootAuga.FixItemBG(buyListPrefab.gameObject);
                EpicLootAuga.FixItemBG(treasureMapElementPrefab.gameObject);
                EpicLootAuga.FixItemBG(bountyElementPrefab.gameObject);
                EpicLootAuga.FixItemBG(activeBountyElementPrefab.gameObject);
                EpicLootAuga.FixItemBG(AbandonBountyDialog.BountyDisplay.gameObject);

                EpicLootAuga.FixListElementColors(buyListPrefab.gameObject);
                EpicLootAuga.FixListElementColors(treasureMapElementPrefab.gameObject);
                EpicLootAuga.FixListElementColors(bountyElementPrefab.gameObject);
                EpicLootAuga.FixListElementColors(activeBountyElementPrefab.gameObject);
                EpicLootAuga.FixListElementColors(AbandonBountyDialog.BountyDisplay.gameObject);

                EpicLootAuga.FixFonts(gameObject);

                EpicLootAuga.MakeSimpleTooltip(treasureMapElementPrefab.gameObject);
                EpicLootAuga.MakeSimpleTooltip(bountyElementPrefab.gameObject);
                EpicLootAuga.MakeSimpleTooltip(activeBountyElementPrefab.gameObject);

                foreach (var scrollbar in GetComponentsInChildren<Scrollbar>())
                {
                    EpicLootAuga.FixupScrollbar(scrollbar);
                }
            }

            // Apply the configured position and make the panel movable (Left Alt, or the corner handle).
            // Done at the end of Awake so the root Graphic (which may be swapped by the Auga background
            // replacement above) is final, and so the Auga fixups never see the drag handle.
            _rt = (RectTransform)transform;
            var backgroundImage = GetComponent<Image>();
            if (backgroundImage != null)
            {
                // Still a raycast target, so presses on the panel's empty areas stop at the panel.
                backgroundImage.raycastTarget = true;
            }

            ApplyConfiguredPosition();
            TraderPanelDrag.Attach(_rt, position =>
            {
                // Persists automatically because cfg.SaveOnConfigSet is true.
                ELConfig.TraderPanelPositionX.Value = position.x;
                ELConfig.TraderPanelPositionY.Value = position.y;
            });

            // Last, so the hints it builds are not swept up by the Auga fixups above.
            Gamepad = new MerchantPanelGamepad(this, Panels);
        }

        public static void SetAcceptBountyLabel(string label)
        {
            if (_acceptBountyLabel != null)
            {
                EpicLootAuga.SetLabel(_acceptBountyLabel, label);
            }
        }

        public void OnEnable()
        {
            Gamepad?.Release();

            _failedPanels.Clear();
            UpdateCurrencies();
            ForEachPanel("RefreshItems", panel => panel.RefreshItems(_currencies));

            if (InputBlocker != null)
            {
                InputBlocker.SetActive(false);
            }
        }

        /// <summary>
        /// Runs one step against every list panel, isolating failures to the panel that caused them.
        /// A panel that throws is reported once and left out for the rest of this opening, so the
        /// panels after it still refresh and a failure in Update is not logged every frame.
        /// </summary>
        private void ForEachPanel(string step, Action<IMerchantListPanel> action)
        {
            for (var i = 0; i < Panels.Count; i++)
            {
                if (_failedPanels.Contains(i))
                {
                    continue;
                }

                try
                {
                    action(Panels[i]);
                }
                catch (Exception e)
                {
                    _failedPanels.Add(i);
                    EpicLoot.LogErrorForce($"[MerchantPanel] {Panels[i].GetType().Name}.{step} failed. " +
                        $"That section stays as it is until the merchant window is reopened.\n{e}");
                }
            }
        }

        public void OnDisable()
        {
            Gamepad?.Release();

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

        public void ApplyConfiguredPosition()
        {
            if (_rt == null)
            {
                return;
            }

            _rt.anchoredPosition = new Vector2(ELConfig.TraderPanelPositionX.Value, ELConfig.TraderPanelPositionY.Value);
        }

        public Currencies GetPlayerCurrencies()
        {
            return _currencies;
        }

        /// <summary>
        /// Returns whether the item actually reached the player's inventory -- the gamble panel needs
        /// to know, since it only strikes an offer off the list once it has been paid for.
        /// </summary>
        public bool BuyItem(Player player, BuyListElement listItem)
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

            if (item == null || !InventoryManagement.Instance.GiveItem(item))
            {
                EpicLoot.LogWarning($"Could not buy item {listItem.ItemInfo.Item.m_shared.m_name}");
                return false;
            }

            if (listItem.ItemInfo.IsGamble)
            {
                GambleSuccessDialog.Show(item);
            }

            if (listItem.ItemInfo.Cost.Coins > 0)
            {
                InventoryManagement.Instance.RemoveItem(GetCoinsName(), listItem.ItemInfo.Cost.Coins);
            }

            if (listItem.ItemInfo.Cost.ForestTokens > 0)
            {
                InventoryManagement.Instance.RemoveItem(GetForestTokenName(), listItem.ItemInfo.Cost.ForestTokens);
            }

            if (listItem.ItemInfo.Cost.IronBountyTokens > 0)
            {
                InventoryManagement.Instance.RemoveItem(GetIronBountyTokenName(), listItem.ItemInfo.Cost.IronBountyTokens);
            }

            if (listItem.ItemInfo.Cost.GoldBountyTokens > 0)
            {
                InventoryManagement.Instance.RemoveItem(GetGoldBountyTokenName(), listItem.ItemInfo.Cost.GoldBountyTokens);
            }

            _nextPoll = 0f;

            StoreGui.instance.m_trader.OnBought(new Trader.TradeItem { m_price = 0 });
            StoreGui.instance.m_buyEffects.Create(player.transform.position, Quaternion.identity);
            return true;
        }

        private static string GetRefreshTimeTooltip(int refreshInterval)
        {
            var message = refreshInterval > 1 ?
                Localization.instance.Localize("$mod_epicloot_merchant_refreshdays", refreshInterval.ToString()) :
                "$mod_epicloot_merchant_refreshday";
            return $"<color=#add8e6ff>{message}</color>";
        }

        // Counting a currency walks the inventory, the open table's storage and every chest a storage mod offers,
        // so the counts and the refresh-time text are updated a few times a second rather than every frame. A
        // purchase asks for a count on the next frame (BuyItem), so the buy buttons never act on stale counts.
        private const float PollSeconds = 0.25f;
        private float _nextPoll;

        /// <summary>Counts the currencies again on the next frame, after something spent them.</summary>
        public static void RefreshCurrenciesSoon()
        {
            if (_instance != null)
            {
                _instance._nextPoll = 0f;
            }
        }

        public void Update()
        {
            var currenciesChanged = false;
            if (Time.unscaledTime >= _nextPoll)
            {
                _nextPoll = Time.unscaledTime + PollSeconds;
                UpdateRefreshTime();
                currenciesChanged = UpdateCurrencies();
            }

            ForEachPanel("RefreshItems", panel =>
            {
                // A currency change only changes what the player can afford, never what is on offer, so
                // it does not rebuild the rows (which would wipe the selection and snap the scroll).
                // `else if` because a rebuild has already applied the new currencies through SetItem.
                if (panel.NeedsRefresh())
                {
                    panel.RefreshItems(_currencies);
                }
                else if (currenciesChanged)
                {
                    panel.UpdateAffordability(_currencies);
                }
            });

            RefreshBuyButtons();

            // After the buttons, so a press this frame lands on their current interactable state.
            Gamepad?.Update();
        }

        public void RefreshAll()
        {
            ForEachPanel("RefreshAll", panel =>
            {
                panel.RefreshItems(_currencies);
                panel.RefreshButton(_currencies);
            });
        }

        private void RefreshBuyButtons()
        {
            ForEachPanel("RefreshButton", panel => panel.RefreshButton(_currencies));
        }

        private void UpdateRefreshTime()
        {
            ForEachPanel("UpdateRefreshTime", panel => panel.UpdateRefreshTime());
        }

        private bool UpdateCurrencies()
        {
            var currenciesChanged = false;

            var newCoinCount = InventoryManagement.Instance.CountItem(GetCoinsName());
            if (_currencies.Coins != newCoinCount)
            {
                _currencies.Coins = newCoinCount;
                CoinsCount.text = _currencies.Coins.ToString();
                currenciesChanged = true;
            }

            var newForestTokenCount = InventoryManagement.Instance.CountItem(GetForestTokenName());
            if (_currencies.ForestTokens != newForestTokenCount)
            {
                _currencies.ForestTokens = newForestTokenCount;
                ForestTokensCount.text = _currencies.ForestTokens.ToString();
                currenciesChanged = true;
            }

            var newIronBountyTokenCount = InventoryManagement.Instance.CountItem(GetIronBountyTokenName());
            if (newIronBountyTokenCount != _currencies.IronBountyTokens)
            {
                _currencies.IronBountyTokens = newIronBountyTokenCount;
                IronBountyTokensCount.text = _currencies.IronBountyTokens.ToString();
                currenciesChanged = true;
            }

            var newGoldBountyTokenCount = InventoryManagement.Instance.CountItem(GetGoldBountyTokenName());
            if (newGoldBountyTokenCount != _currencies.GoldBountyTokens)
            {
                _currencies.GoldBountyTokens = newGoldBountyTokenCount;
                GoldBountyTokensCount.text = _currencies.GoldBountyTokens.ToString();
                currenciesChanged = true;
            }

            return currenciesChanged;
        }

        private static string _coinsName;
        private static string _forestTokenName;
        private static string _ironBountyTokenName;
        private static string _goldBountyTokenName;

        private static string SharedName(string prefabName, ref string cached)
        {
            return cached ??= ObjectDB.instance.GetItemPrefab(prefabName).GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
        }

        public static string GetCoinsName() => SharedName("Coins", ref _coinsName);

        public static string GetForestTokenName() => SharedName("ForestToken", ref _forestTokenName);

        public static string GetIronBountyTokenName() => SharedName("IronBountyToken", ref _ironBountyTokenName);

        public static string GetGoldBountyTokenName() => SharedName("GoldBountyToken", ref _goldBountyTokenName);

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
                _audioSource.PlayOneShot(EpicAssets.AbandonBountySFX);
            }
        }
    }
}
