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

        public MultiSelectItemList SacrificeProducts;
        public EnchantBonus BonusPanel;
        public TMP_Text Warning;
        public TMP_Text Explainer;


        SacrificeMode _sacrificeMode = SacrificeMode.Sacrifice;

        private GameObject _sacrificeCategoryPanel;
        private TMP_Dropdown _sacrificeCategory;

        private EnchantingUIController.SacrificeCategory CurrentSacrificeCategory
        {
            get
            {
                EnchantingUIController.SacrificeCategory[] options = EnchantingUIController.SacrificeCategoryOptions;
                int index = _sacrificeCategory == null ? 0 : _sacrificeCategory.value;
                return index >= 0 && index < options.Length ? options[index] : EnchantingUIController.SacrificeCategory.All;
            }
        }

        public override void Awake()
        {
            base.Awake();

            CreateCategoryHint();
            CreateSacrificeCategoryPanel();

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
                IdentifyStyle.options.Add(new TMP_Dropdown.OptionData(Localization.instance.Localize(entry.Value)));
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
            // The prefab opens with the Sacrifice toggle already on, so setting isOn would not fire the handler.
            IdentifyToggle.SetIsOnWithoutNotify(false);
            SacrificeToggle.SetIsOnWithoutNotify(true);
            _sacrificeCategory?.SetValueWithoutNotify(0);
            SacrificeModeSelected(true);
        }

        // A runtime clone of the identify style panel rather than a second dropdown in the prefab.
        private void CreateSacrificeCategoryPanel()
        {
            if (IdentifyStylePanel == null || IdentifyStyle == null)
            {
                return;
            }

            _sacrificeCategoryPanel = Instantiate(IdentifyStylePanel, IdentifyStylePanel.transform.parent, false);
            _sacrificeCategoryPanel.name = "SacrificeCategoryPanel";
            _sacrificeCategoryPanel.transform.SetSiblingIndex(IdentifyStylePanel.transform.GetSiblingIndex() + 1);
            _sacrificeCategory = _sacrificeCategoryPanel.GetComponentInChildren<TMP_Dropdown>(true);
            if (_sacrificeCategory == null)
            {
                Destroy(_sacrificeCategoryPanel);
                _sacrificeCategoryPanel = null;
                return;
            }

            // The prefab's dropdown carries its caption as a child named SortByLabel.
            Transform caption = _sacrificeCategory.transform.Find("SortByLabel");
            TMP_Text captionText = caption != null ? caption.GetComponent<TMP_Text>() : null;
            if (captionText != null)
            {
                captionText.text = Localization.instance.Localize("$mod_epicloot_sacrifice_selection");
            }

            _sacrificeCategory.ClearOptions();
            foreach (EnchantingUIController.SacrificeCategory category in EnchantingUIController.SacrificeCategoryOptions)
            {
                string label = EnchantingUIController.SacrificeCategoryLabels[category];
                _sacrificeCategory.options.Add(new TMP_Dropdown.OptionData(Localization.instance.Localize(label)));
            }
            _sacrificeCategory.SetValueWithoutNotify(0);
            _sacrificeCategory.RefreshShownValue();
            _sacrificeCategory.onValueChanged.AddListener(_ =>
            {
                if (_sacrificeMode == SacrificeMode.Sacrifice && !_locked)
                {
                    RefreshAvailableItems();
                }
            });
        }

        private void CreateCategoryHint()
        {
            Transform bottomRow = transform.Find("GamepadHints/BottomRow");
            if (bottomRow == null)
            {
                return;
            }

            Transform spacing = bottomRow.Find("Spacing");
            Transform label = bottomRow.Find("Quantity");
            Transform glyph = bottomRow.Find("QuantityButton");
            if (spacing == null || label == null || glyph == null)
            {
                return;
            }

            Instantiate(spacing.gameObject, bottomRow, false).name = "Spacing";

            GameObject labelClone = Instantiate(label.gameObject, bottomRow, false);
            labelClone.name = "Category";
            TMP_Text labelText = labelClone.GetComponentInChildren<TMP_Text>(true);
            if (labelText != null)
            {
                labelText.text = Localization.instance.Localize("$mod_epicloot_enchanting_identifycategory");
            }

            GameObject glyphClone = Instantiate(glyph.gameObject, bottomRow, false);
            glyphClone.name = "CategoryButton";
            GamepadGlyph glyphKey = glyphClone.GetComponentInChildren<GamepadGlyph>(true);
            if (glyphKey != null)
            {
                glyphKey.ZInputKey = "JoyDPadLeft";
            }
        }

        protected override void OnDPadHorizontal(int direction)
        {
            if (_locked)
            {
                return;
            }

            TMP_Dropdown dropdown = _sacrificeMode == SacrificeMode.Identify ? IdentifyStyle : _sacrificeCategory;
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

            Unlock();
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

            // Cancel on every way out: returning without it left the button reading "Cancel".
            if (!LocalPlayerCanAffordCost(cost))
            {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                Cancel();
                RefreshAvailableItems();
                return;
            }

            // Rolls, re-checks the cost, charges it, removes the inputs and hands the results over --
            // or, if any stack cannot be identified in full, does none of that.
            EnchantingUIController.LootRollSelectedItems(filterType, unidentifiedItems, costReduction, powerModifier);

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

            Cancel();

            // Remove first, pay out after: the products and the returned socket stones follow from what
            // was actually taken, so an item that is no longer there yields nothing.
            Player player = Player.m_localPlayer;
            List<Tuple<ItemDrop.ItemData, int>> sacrificedItems = new List<Tuple<ItemDrop.ItemData, int>>();
            List<InventoryItemListElement> reclaimedSockets = new List<InventoryItemListElement>();
            List<ItemDrop.ItemData> unavailableItems = new List<ItemDrop.ItemData>();
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
                List<InventoryItemListElement> sockets =
                    sacrificed.IsMagic(out MagicItem sacrificedMagicItem) && sacrificedMagicItem.Sockets.Count > 0
                        ? EnchantingUIController.ReclaimSockets(sacrificedMagicItem)
                        : null;

                // Listed while equipped when ShowEquippedAndHotbarItemsInSacrificeTab is on, and vanilla
                // never auto-unequips a removed item (ghost stats and visuals).
                if (player != null && player.IsItemEquiped(sacrificed))
                {
                    player.UnequipItem(sacrificed, false);
                }

                int removed = InventoryManagement.Instance.RemoveExactItem(sacrificed, amount);
                if (removed < amount)
                {
                    unavailableItems.Add(sacrificed);
                }

                if (removed <= 0)
                {
                    Debug.LogWarning($"[Sacrifice] {sacrificed.m_shared.m_name} could not be removed; skipped.");
                    continue;
                }

                sacrificedItems.Add(new Tuple<ItemDrop.ItemData, int>(sacrificed, removed));
                if (sockets != null)
                {
                    reclaimedSockets.AddRange(sockets);
                }
            }

            List<InventoryItemListElement> sacrificeProducts = EnchantingUIController.GetSacrificeProducts(sacrificedItems);

            Tuple<float, float> chanceToDoubleEntry =
                EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Sacrifice);
            float chanceToDouble = float.IsNaN(chanceToDoubleEntry.Item1) ? 0.0f : chanceToDoubleEntry.Item1 / 100.0f;

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
            NotifyItemsUnavailable(unavailableItems.ToArray());

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
            List<InventoryItemListElement> items = EnchantingUIController.GetSacrificeItems(CurrentSacrificeCategory);
            // The two modes list disjoint item sets, so a filter carried across the toggle would hide
            // nearly everything and read as a bug.
            AvailableItems.ClearFilter();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            Warning.text = Localization.instance.Localize("$mod_epicloot_sacrifice_warning");
            Warning.color = Color.red;
            Explainer.text = Localization.instance.Localize("$mod_epicloot_sacrifice_productsexplainer");
            SetMainButtonLabel("$mod_epicloot_sacrifice");
            OnSelectedItemsChanged();
            IdentifyStylePanel.SetActive(false);
            _sacrificeCategoryPanel?.SetActive(true);
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
            // Set before the selection refresh, which appends the progress-gated marker when needed.
            Explainer.text = Localization.instance.Localize("$mod_epicloot_identify_productsexplainer");
            OnSelectedItemsChanged();
            Warning.text = Localization.instance.Localize("$mod_epicloot_identify_explain");
            Warning.color = new Color(1f, 0.631f, 0.235f);
            SetMainButtonLabel("$mod_epicloot_identify");
            IdentifyStylePanel.SetActive(true);
            _sacrificeCategoryPanel?.SetActive(false);
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
                float costReduction = featureValues.Item1 == 0f || float.IsNaN(featureValues.Item1) ?
                    1.0f : 1f - (featureValues.Item1 / 100f);
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
        
        public override void DeselectAll()
        {
            AvailableItems.DeselectAll();
        }

        public override void Lock()
        {
            base.Lock();

            SacrificeToggle.interactable = false;
            IdentifyToggle.interactable = false;
            if (_sacrificeCategory != null)
            {
                _sacrificeCategory.interactable = false;
            }
        }

        public override void Unlock()
        {
            base.Unlock();

            SacrificeToggle.interactable = true;
            IdentifyToggle.interactable = true;
            if (_sacrificeCategory != null)
            {
                _sacrificeCategory.interactable = true;
            }
        }
    }
}
