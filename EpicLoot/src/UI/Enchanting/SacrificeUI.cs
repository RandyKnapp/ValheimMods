using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.Compatibility;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace EpicLoot_UnityLib
{
    public class SacrificeUI : EnchantingTableUIPanelBase
    {
        enum SacrificeMode
        {
            Sacrifice,
            Identify
        }

        public Toggle SacrificeToggle;
        public Toggle IdentifyToggle;
        public GameObject IdentifyStylePanel;
        public MultiSelectItemList CostList;

        public TMP_Dropdown IdentifyStyle;
        public GameObject SacrificeCategoryPanel;
        public TMP_Dropdown SacrificeCategory;

        public MultiSelectItemList SacrificeProducts;
        public EnchantBonus BonusPanel;
        public TMP_Text Warning;
        public TMP_Text Explainer;


        SacrificeMode _sacrificeMode = SacrificeMode.Sacrifice;

        private EnchantingUIController.SacrificeCategory CurrentSacrificeCategory
        {
            get
            {
                EnchantingUIController.SacrificeCategory[] options = EnchantingUIController.SacrificeCategoryOptions;
                int index = SacrificeCategory.value;
                return index >= 0 && index < options.Length ? options[index] : EnchantingUIController.SacrificeCategory.All;
            }
        }

        public override void Awake()
        {
            base.Awake();

            SacrificeCategory.ClearOptions();
            foreach (EnchantingUIController.SacrificeCategory category in EnchantingUIController.SacrificeCategoryOptions)
            {
                string label = EnchantingUIController.SacrificeCategoryLabels[category];
                SacrificeCategory.options.Add(new TMP_Dropdown.OptionData(Localization.instance.Localize(label)));
            }
            SacrificeCategory.SetValueWithoutNotify(0);
            SacrificeCategory.RefreshShownValue();
            SacrificeCategory.onValueChanged.AddListener(_ =>
            {
                if (_sacrificeMode == SacrificeMode.Sacrifice && !_locked)
                {
                    RefreshAvailableItems();
                }
            });

            SacrificeToggle.onValueChanged.AddListener((isOn) => {
                SacrificeModeSelected(isOn);
            });

            IdentifyToggle.onValueChanged.AddListener((isOn) => {
                IdentifyModeSelected(isOn);
            });

            IdentifyStyle.ClearOptions();
            foreach (KeyValuePair<string, string> entry in EnchantCostsHelper.GetIdentificationCategories())
            {
                IdentifyStyle.options.Add(new TMP_Dropdown.OptionData(Localization.instance.Localize(entry.Value)));
            }

            IdentifyStyle.onValueChanged.AddListener((value) =>
            {
                OnSelectedItemsChanged();
            });
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            // The prefab opens with the Sacrifice toggle already on, so setting isOn would not fire the handler.
            IdentifyToggle.SetIsOnWithoutNotify(false);
            SacrificeToggle.SetIsOnWithoutNotify(true);
            SacrificeCategory.SetValueWithoutNotify(0);
            SacrificeModeSelected(true);
        }

        protected override void OnDPadHorizontal(int direction)
        {
            if (_locked)
            {
                return;
            }

            TMP_Dropdown dropdown = _sacrificeMode == SacrificeMode.Identify ? IdentifyStyle : SacrificeCategory;
            int optionCount = dropdown == null ? 0 : dropdown.options.Count;
            if (optionCount == 0)
            {
                return;
            }

            dropdown.value = (dropdown.value + direction + optionCount) % optionCount;
        }

        public override void Update()
        {
            base.Update();

            if (_locked || !ZInput.IsGamepadActive())
            {
                return;
            }

            SacrificeProducts.ScrollWithRightStick();

            if (!ZInput.GetButtonDown("JoyButtonY"))
            {
                return;
            }

            ZInput.ResetButtonStatus("JoyButtonY");

            if (_sacrificeMode == SacrificeMode.Sacrifice)
            {
                IdentifyToggle.isOn = true;
            }
            else
            {
                SacrificeToggle.isOn = true;
            }
        }

        protected override void DoMainAction()
        {
            if (_sacrificeMode == SacrificeMode.Identify)
            {
                IdentifyItems();
            }
            else if (_sacrificeMode == SacrificeMode.Sacrifice)
            {
                SacrificeItems();
            }

            if (!_awaitingPayment)
            {
                Unlock();
            }
        }

        private void IdentifyItems()
        {
            List<Tuple<IListElement, int>> selectedItems = AvailableItems.GetSelectedItems<IListElement>();
            // Favorited while the table was open: leave it out rather than identify it away.
            List<Tuple<ItemDrop.ItemData, int>> unidentifiedItems = selectedItems
                .Where(x => !ItemFavorites.IsProtected(x.Item1.GetItem()))
                .Select(x => new Tuple<ItemDrop.ItemData, int>(x.Item1.GetItem(), x.Item2)).ToList();
            string filterType = IdentifyStyle.options[IdentifyStyle.value].text;
            Tuple<float, float> featureValues =
                EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Sacrifice);
            float costReduction = GetCostReduction(featureValues.Item1);
            float powerModifier = GetPowerModifier(featureValues.Item2);
            List<InventoryItemListElement> cost = EnchantingUIController.GetIdentifyCostForCategory(filterType, unidentifiedItems, costReduction);

            // Cancel on every way out, or the button keeps reading "Cancel".
            if (!LocalPlayerCanAffordCost(cost))
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                Cancel();
                RefreshAvailableItems();
                return;
            }

            EnchantingUIController.PreparedIdentify prepared =
                EnchantingUIController.PrepareIdentify(filterType, unidentifiedItems, costReduction, powerModifier);
            Cancel();
            if (prepared == null)
            {
                RefreshAvailableItems();
                return;
            }

            PayThen(prepared.Payment, receipt =>
            {
                foreach (ItemDrop.ItemData item in prepared.Results)
                {
                    InventoryManagement.Instance.GiveItem(item);
                }

                if (Player.m_localPlayer != null)
                {
                    EquipmentEffectCache.Reset(Player.m_localPlayer);
                }

                RefreshAvailableItems();
                AvailableItems.GiveFocus(true, 0);
            }, () =>
            {
                NotifyItemsUnavailable(unidentifiedItems.Select(x => x.Item1).ToArray());
                RefreshAvailableItems();
            });
        }

        private void SacrificeItems()
        {
            List<Tuple<IListElement, int>> selectedItems = AvailableItems.GetSelectedItems<IListElement>();

            Cancel();

            // Every input is taken in one payment, so the products follow from items that are really gone.
            Player player = Player.m_localPlayer;
            List<Tuple<ItemDrop.ItemData, int>> sacrificedItems = new List<Tuple<ItemDrop.ItemData, int>>();
            List<InventoryItemListElement> reclaimedSockets = new List<InventoryItemListElement>();
            InventoryManagement.Payment payment = new InventoryManagement.Payment();
            foreach (Tuple<IListElement, int> selectedItem in selectedItems)
            {
                ItemDrop.ItemData sacrificed = selectedItem.Item1.GetItem();
                int amount = selectedItem.Item2;
                // Re-asked now rather than trusted from the list: an external filter (API
                // RegisterSacrificeFilter) may have started vetoing the item during the countdown, or the
                // player may have favorited it.
                if (sacrificed == null || amount <= 0 || EnchantCostsHelper.GetSacrificeProducts(sacrificed) == null ||
                    ItemFavorites.IsProtected(sacrificed))
                {
                    continue;
                }

                // Socketed stones are the player's property, not part of the sacrifice yield: the
                // non-Locked ones go back once the item is gone, the same policy disenchanting uses.
                // Kept apart from the products so the double-yield bonus never doubles them.
                if (sacrificed.IsMagic(out MagicItem sacrificedMagicItem) && sacrificedMagicItem.Sockets.Count > 0)
                {
                    reclaimedSockets.AddRange(EnchantingUIController.ReclaimSockets(sacrificedMagicItem));
                }

                // Listed while equipped when ShowEquippedAndHotbarItemsInSacrificeTab is on, and vanilla
                // never auto-unequips a removed item (ghost stats and visuals).
                if (player != null && player.IsItemEquiped(sacrificed))
                {
                    player.UnequipItem(sacrificed, false);
                }

                payment.Exact(sacrificed, amount);
                sacrificedItems.Add(new Tuple<ItemDrop.ItemData, int>(sacrificed, amount));
            }

            if (sacrificedItems.Count == 0)
            {
                RefreshAvailableItems();
                return;
            }

            // Worked out before the items go: a removed item's data is what the products are read from.
            List<InventoryItemListElement> sacrificeProducts = EnchantingUIController.GetSacrificeProducts(sacrificedItems);
            Tuple<float, float> chanceToDoubleEntry =
                EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Sacrifice);
            float chanceToDouble = float.IsNaN(chanceToDoubleEntry.Item1) ? 0.0f : chanceToDoubleEntry.Item1 / 100.0f;

            PayThen(payment, receipt =>
            {
                if (sacrificeProducts.Count > 0 && Random.Range(0.0f, 1.0f) < chanceToDouble)
                {
                    EnchantingTableUI.instance.PlayEnchantBonusSFX();
                    BonusPanel.Show();

                    foreach (InventoryItemListElement sacrificeProduct in sacrificeProducts)
                    {
                        sacrificeProduct.Item.m_stack *= 2;
                    }
                }

                GiveItemsToPlayer(reclaimedSockets);
                GiveItemsToPlayer(sacrificeProducts);
                RefreshAvailableItems();
                AvailableItems.GiveFocus(true, 0);
            }, () =>
            {
                NotifyItemsUnavailable(sacrificedItems.Select(x => x.Item1).ToArray());
                RefreshAvailableItems();
            });
        }

        private void SacrificeModeSelected(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _sacrificeMode = SacrificeMode.Sacrifice;
            List<InventoryItemListElement> items = EnchantingUIController.GetSacrificeItems(CurrentSacrificeCategory);
            // The two modes list disjoint item sets, so a filter carried across the toggle would hide
            // nearly everything and read as a bug.
            AvailableItems.ClearFilter();
            AvailableItems.SetItemsAndDeselect(items.Cast<IListElement>().ToList());
            Warning.text = Localization.instance.Localize("$mod_epicloot_sacrifice_warning");
            Warning.color = Color.red;
            Explainer.text = Localization.instance.Localize("$mod_epicloot_sacrifice_productsexplainer");
            SetMainButtonLabel("$mod_epicloot_sacrifice");
            OnSelectedItemsChanged();
            IdentifyStylePanel.SetActive(false);
            SacrificeCategoryPanel.SetActive(true);
            CostList.gameObject.SetActive(false);
        }

        private void IdentifyModeSelected(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _sacrificeMode = SacrificeMode.Identify;
            List<InventoryItemListElement> items = EnchantingUIController.GetUnidentifiedItems();
            AvailableItems.ClearFilter();
            AvailableItems.SetItemsAndDeselect(items.Cast<IListElement>().ToList());
            // Set before the selection refresh, which appends the progress-gated marker when needed.
            Explainer.text = Localization.instance.Localize("$mod_epicloot_identify_productsexplainer");
            OnSelectedItemsChanged();
            Warning.text = Localization.instance.Localize("$mod_epicloot_identify_explain");
            Warning.color = new Color(1f, 0.631f, 0.235f);
            SetMainButtonLabel("$mod_epicloot_identify");
            IdentifyStylePanel.SetActive(true);
            SacrificeCategoryPanel.SetActive(false);
            CostList.gameObject.SetActive(true);
        }

        private void RefreshAvailableItems()
        {
            if (_sacrificeMode == SacrificeMode.Identify)
            {
                List<InventoryItemListElement> items = EnchantingUIController.GetUnidentifiedItems();
                AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            }
            else if (_sacrificeMode == SacrificeMode.Sacrifice)
            {
                List<InventoryItemListElement> items = EnchantingUIController.GetSacrificeItems(CurrentSacrificeCategory);
                AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            }

            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
        }

        protected override void OnSelectedItemsChanged()
        {
            List<Tuple<IListElement, int>> selectedItems = AvailableItems.GetSelectedItems<IListElement>();
            bool canAfford = true;

            if (_sacrificeMode == SacrificeMode.Sacrifice)
            {
                List<InventoryItemListElement> sacrificeProducts = EnchantingUIController.GetSacrificeProducts(selectedItems.Select(
                    x => new Tuple<ItemDrop.ItemData, int>(x.Item1.GetItem(), x.Item2)).ToList());
                SacrificeProducts.SetItems(sacrificeProducts.Cast<IListElement>().ToList());
            }
            else if (_sacrificeMode == SacrificeMode.Identify)
            {
                string identifyFilter = IdentifyStyle.options[IdentifyStyle.value].text;
                List<InventoryItemListElement> potentialIdentifyItems =
                    EnchantingUIController.GetPotentialItemRollsByCategory(identifyFilter,
                        selectedItems.Select(x => x.Item1.GetItem()).ToList(), out bool hasIdentifyLoot);
                SacrificeProducts.SetItems(potentialIdentifyItems.Cast<IListElement>().ToList());

                // Say so when progression gating will identify a selected item below its own biome.
                bool progressGated = EnchantingUIController.IsIdentifyGated(selectedItems.Select(x => x.Item1.GetItem()).ToList());
                Explainer.text = Localization.instance.Localize(progressGated ?
                    "$mod_epicloot_identify_productsexplainer $mod_epicloot_identify_progressgated" :
                    "$mod_epicloot_identify_productsexplainer");

                List<Tuple<ItemDrop.ItemData, int>> unidentifiedItems = selectedItems.Select(
                    x => new Tuple<ItemDrop.ItemData, int>(x.Item1.GetItem(), x.Item2)).ToList();
                Tuple<float, float> featureValues =
                    EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Sacrifice);
                float costReduction = GetCostReduction(featureValues.Item1);
                List<InventoryItemListElement> cost = EnchantingUIController.GetIdentifyCostForCategory(identifyFilter, unidentifiedItems, costReduction);
                CostList.SetItems(cost.Cast<IListElement>().ToList());
                canAfford = LocalPlayerCanAffordIdentifyCost(cost);

                // Whether the loot lists name anything at all, not whether the preview shows anything:
                // the preview hides entries gating would replace, and the roll still hands out those
                // replacements.
                if (!hasIdentifyLoot)
                {
                    canAfford = false;
                }
            }

            bool featureUnlocked = EnchantingTableUI.instance != null &&
                EnchantingTableUI.instance.SourceTable != null &&
                EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.Sacrifice);
            MainButton.interactable = featureUnlocked && selectedItems.Count > 0 && canAfford;
        }

        private bool LocalPlayerCanAffordIdentifyCost(List<InventoryItemListElement> cost)
        {
            return LocalPlayerCanAffordCostOrNone(cost);
        }

        public override void Cancel()
        {
            base.Cancel();
            RefreshMainButtonLabel();
        }

        private void RefreshMainButtonLabel()
        {
            SetMainButtonLabel(_sacrificeMode == SacrificeMode.Identify
                ? "$mod_epicloot_identify"
                : "$mod_epicloot_sacrifice");
        }

        private void SetMainButtonLabel(string token)
        {
            string text = Localization.instance.Localize(token);
            if (_buttonLabel != null)
            {
                _buttonLabel.text = text;
            }
        }

        public override void Lock()
        {
            base.Lock();

            SacrificeToggle.interactable = false;
            IdentifyToggle.interactable = false;
            if (SacrificeCategory != null)
            {
                SacrificeCategory.interactable = false;
            }
        }

        public override void Unlock()
        {
            base.Unlock();

            SacrificeToggle.interactable = true;
            IdentifyToggle.interactable = true;
            if (SacrificeCategory != null)
            {
                SacrificeCategory.interactable = true;
            }
        }
    }
}
