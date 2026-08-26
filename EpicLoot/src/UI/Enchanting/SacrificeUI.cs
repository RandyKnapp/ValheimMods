using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.CraftingV2;
using JetBrains.Annotations;
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

        public Dropdown IdentifyStyle;

        public MultiSelectItemList SacrificeProducts;
        public EnchantBonus BonusPanel;
        public Text Warning;
        public Text Explainer;


        SacrificeMode _sacrificeMode = SacrificeMode.Sacrifice;

        public override void Awake()
        {
            base.Awake();

            SacrificeToggle.onValueChanged.AddListener((isOn) => {
                SacrificeModeSelected(isOn);
            });

            IdentifyToggle.onValueChanged.AddListener((isOn) => {
                IdentifyModeSelected(isOn);
            });

            // Build the identify style dropdown options based on the configured styles
            IdentifyStyle.ClearOptions();
            foreach (KeyValuePair<string, string> entry in EnchantingUIController.GetIdentifyStyles())
            {
                IdentifyStyle.options.Add(new Dropdown.OptionData(Localization.instance.Localize(entry.Value)));
            }

            // Trigger cost update when the identify style changes
            IdentifyStyle.onValueChanged.AddListener((value) =>
            {
                OnSelectedItemsChanged();
            });
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            List<InventoryItemListElement> items = EnchantingUIController.GetSacrificeItems();
            _sacrificeMode = SacrificeMode.Sacrifice;
            IdentifyStylePanel.SetActive(false);
            IdentifyToggle.isOn = false;
            SacrificeToggle.isOn = true;
            AvailableItems.ClearFilter();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
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

            Unlock();
        }

        private void IdentifyItems()
        {
            List<Tuple<IListElement, int>> selectedItems = AvailableItems.GetSelectedItems<IListElement>();
            List<Tuple<ItemDrop.ItemData, int>> unidentifiedItems = selectedItems
                .Select(x => new Tuple<ItemDrop.ItemData, int>(x.Item1.GetItem(), x.Item2)).ToList();
            string filterType = IdentifyStyle.options[IdentifyStyle.value].text;
            Tuple<float, float> featureValues =
                EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Sacrifice);
            float costReduction = GetCostReduction(featureValues.Item1);
            float powerModifier = GetPowerModifier(featureValues.Item2);
            List<InventoryItemListElement> cost = EnchantingUIController.GetIdentifyCostForCategory(filterType, unidentifiedItems, costReduction);

            if (!LocalPlayerCanAffordCost(cost))
            {
                return;
            }

            if (!Player.m_localPlayer.NoCostCheat())
            {
                foreach (InventoryItemListElement costElement in cost)
                {
                    InventoryManagement.Instance.RemoveItem(costElement.GetItem());
                }
            }

            List<InventoryItemListElement> identifiedItems = EnchantingUIController.LootRollSelectedItems(filterType, unidentifiedItems, powerModifier);

            Cancel();
            RefreshAvailableItems();
            AvailableItems.GiveFocus(true, 0);
        }

        private float GetCostReduction(float value)
        {
            return RuneUI.GetCostReduction(value);
        }

        private float GetPowerModifier(float value)
        {
            return RuneUI.GetPowerModifier(value);
        }

        private void SacrificeItems()
        {
            List<Tuple<IListElement, int>> selectedItems = AvailableItems.GetSelectedItems<IListElement>();
            List<InventoryItemListElement> sacrificeProducts = EnchantingUIController.GetSacrificeProducts(selectedItems
                .Select(x => new Tuple<ItemDrop.ItemData, int>(x.Item1.GetItem(), x.Item2)).ToList());

            Cancel();

            Tuple<float, float> chanceToDoubleEntry =
                EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Sacrifice);
            float chanceToDouble = float.IsNaN(chanceToDoubleEntry.Item1) ? 0.0f : chanceToDoubleEntry.Item1 / 100.0f;

            if (Random.Range(0.0f, 1.0f) < chanceToDouble)
            {
                EnchantingTableUI.instance.PlayEnchantBonusSFX();
                BonusPanel.Show();

                foreach (InventoryItemListElement sacrificeProduct in sacrificeProducts)
                {
                    sacrificeProduct.Item.m_stack *= 2;
                }
            }

            foreach (Tuple<IListElement, int> selectedItem in selectedItems)
            {
                // Socketed stones are the player's property, not part of the sacrifice yield: hand the
                // non-Locked ones back before the item is destroyed, the same policy disenchanting uses.
                // Given separately from sacrificeProducts so the double-yield bonus never doubles them.
                ItemDrop.ItemData sacrificed = selectedItem.Item1.GetItem();
                if (sacrificed.IsMagic(out MagicItem sacrificedMagicItem) && sacrificedMagicItem.Sockets.Count > 0)
                {
                    GiveItemsToPlayer(EnchantingUIController.ReclaimSockets(sacrificedMagicItem));
                }

                InventoryManagement.Instance.RemoveExactItem(sacrificed, selectedItem.Item2);
            }

            GiveItemsToPlayer(sacrificeProducts);

            RefreshAvailableItems();
            AvailableItems.GiveFocus(true, 0);
        }

        private void SacrificeModeSelected(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _sacrificeMode = SacrificeMode.Sacrifice;
            List<InventoryItemListElement> items = EnchantingUIController.GetSacrificeItems();
            // The two modes list disjoint item sets, so a filter carried across the toggle would hide
            // nearly everything and read as a bug.
            AvailableItems.ClearFilter();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            Warning.text = Localization.instance.Localize("$mod_epicloot_sacrifice_warning");
            Warning.color = Color.red;
            Explainer.text = Localization.instance.Localize("$mod_epicloot_sacrifice_productsexplainer");
            MainButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$mod_epicloot_sacrifice");
            OnSelectedItemsChanged();
            IdentifyStylePanel.SetActive(false);
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
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
            Warning.text = Localization.instance.Localize("$mod_epicloot_identify_explain");
            Warning.color = new Color(1f, 0.631f, 0.235f);
            Explainer.text = Localization.instance.Localize("$mod_epicloot_identify_productsexplainer");
            MainButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$mod_epicloot_identify");
            IdentifyStylePanel.SetActive(true);
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
                List<InventoryItemListElement> items = EnchantingUIController.GetSacrificeItems();
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
                    EnchantingUIController.GetPotentialItemRollsByCategory(identifyFilter, selectedItems.Select(x => x.Item1.GetItem()).ToList());
                SacrificeProducts.SetItems(potentialIdentifyItems.Cast<IListElement>().ToList());
                List<Tuple<ItemDrop.ItemData, int>> unidentifiedItems = selectedItems.Select(
                    x => new Tuple<ItemDrop.ItemData, int>(x.Item1.GetItem(), x.Item2)).ToList();
                Tuple<float, float> featureValues =
                    EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Sacrifice);
                float costReduction = featureValues.Item1 == 0f || float.IsNaN(featureValues.Item1) ?
                    1.0f : 1f - (featureValues.Item1 / 100f);
                List<InventoryItemListElement> cost = EnchantingUIController.GetIdentifyCostForCategory(identifyFilter, unidentifiedItems, costReduction);
                CostList.SetItems(cost.Cast<IListElement>().ToList());
                canAfford = LocalPlayerCanAffordIdentifyCost(cost);

                if (potentialIdentifyItems.Count() == 0)
                {
                    canAfford = false;
                }
            }

            bool featureUnlocked = EnchantingTableUI.instance != null &&
                EnchantingTableUI.instance.SourceTable != null &&
                EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.Sacrifice);
            MainButton.interactable = featureUnlocked && selectedItems.Count > 0 && canAfford;
        }

        public bool LocalPlayerCanAffordIdentifyCost(List<InventoryItemListElement> cost)
        {
            if (cost == null || cost.Count == 0)
            {
                return true;
            }

            if (Player.m_localPlayer == null)
            {
                return false;
            }

            if (Player.m_localPlayer.NoCostCheat())
            {
                return true;
            }

            foreach (InventoryItemListElement element in cost)
            {
                ItemDrop.ItemData item = element.GetItem();
                if (!InventoryManagement.Instance.HasItem(item))
                {
                    Debug.Log($"Identify Cost failed, user does not have item {item.m_shared.m_name}.");
                    return false;
                }
            }

            return true;
        }

        public override void Cancel()
        {
            if (_sacrificeMode == SacrificeMode.Sacrifice)
            {
                if (_useTMP)
                {
                    _tmpButtonLabel.text = Localization.instance.Localize("$mod_epicloot_sacrifice");
                }
                else
                {
                    _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_sacrifice");
                }
            }
            if (SacrificeMode.Identify == _sacrificeMode)
            {
                if (_buttonLabel != null)
                {
                    _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_identify");
                }
            }

            Unlock();
        }
        
        public override void DeselectAll()
        {
            AvailableItems.DeselectAll();
        }

        public override void Lock()
        {
            base.Lock();

            SacrificeToggle.interactable = false;
            IdentifyToggle.interactable = false;
        }

        public override void Unlock()
        {
            base.Unlock();

            SacrificeToggle.interactable = true;
            IdentifyToggle.interactable = true;
        }
    }
}
