using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.CraftingV2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class AugmentUI : EnchantingTableUIPanelBase
    {
        public TMP_Text AvailableEffectsText;
        public TMP_Text AvailableEffectsHeader;
        public Scrollbar AvailableEffectsScrollbar;

        public RectTransform EnchantList;
        public GameObject EnchantmentListPrefab;

        [Header("Cost")]
        public TMP_Text CostLabel;
        public MultiSelectItemList CostList;

        private int _augmentIndex;
        private GameObject _choiceDialog;
        private List<Toggle> _AugmentSelectors = new List<Toggle>();
        private ScrollRectEnsureVisible _enchantListEnsureVisible;

        public override void Awake()
        {
            base.Awake();
            AvailableItems.AddEquippedSortMode();
            MagicTextShimmer.Ensure(AvailableEffectsText);
            _enchantListEnsureVisible = EnchantList.GetComponentInParent<ScrollRectEnsureVisible>(true);
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            if (EnchantList.childCount > 0)
            {
                foreach (Transform child in EnchantList)
                {
                    Destroy(child.gameObject);
                }
            }
            _AugmentSelectors.Clear();
            _augmentIndex = -1;

            if (AvailableEffectsHeader != null)
            {
                int augmentChoices = 2;
                Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Augment);
                if (!float.IsNaN(featureValues.Item1))
                {
                    augmentChoices = (int)featureValues.Item1;
                }

                string colorPre = augmentChoices > 2 ? "<color=#EAA800>" : "";
                string colorPost = augmentChoices > 2 ? "</color>" : "";
                AvailableEffectsHeader.text = Localization.instance.Localize(
                    $"$mod_epicloot_augment_availableeffects {colorPre}($mod_epicloot_augment_choices){colorPost}", augmentChoices.ToString());
            }

            OnAugmentIndexChanged();

            List<InventoryItemListElement> items = EnchantingUIController.GetAugmentableItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            DeselectAll();
        }

        public override void Update()
        {
            base.Update();

            if (!_locked && ZInput.IsGamepadActive())
            {
                if (ZInput.GetButtonDown("JoyButtonY"))
                {
                    int activeAugmentCount = _AugmentSelectors.Count;
                    // No selectors when nothing is selected on the tab, and the modulo would divide by zero.
                    if (activeAugmentCount > 0)
                    {
                        int nextAugmentIndex = (_augmentIndex + 1) % activeAugmentCount;
                        // Skip selectors disabled as non-augmentable (bounded by one full loop).
                        for (int step = 0; step < activeAugmentCount &&
                            !_AugmentSelectors[nextAugmentIndex].interactable; ++step)
                        {
                            nextAugmentIndex = (nextAugmentIndex + 1) % activeAugmentCount;
                        }
                        _AugmentSelectors[nextAugmentIndex].isOn = true;

                        // An item with many effects scrolls the list; keep the pick in view.
                        if (_enchantListEnsureVisible != null)
                        {
                            _enchantListEnsureVisible.CenterOnItem((RectTransform)_AugmentSelectors[nextAugmentIndex].transform);
                        }
                    }
                    ZInput.ResetButtonStatus("JoyButtonY");
                }

                GamepadScroll.ApplyRightStickY(AvailableEffectsScrollbar);
            }

            if (_choiceDialog != null && !_choiceDialog.activeSelf)
            {
                Unlock();
                Destroy(_choiceDialog);
                _choiceDialog = null;
                Cancel();

                AvailableItems.ForeachElement((i, e) =>
                {
                    if (!e.IsSelected())
                    {
                        return;
                    }
                    e.SetItem(e.GetListElement());
                    e.Refresh();
                });
                RefreshAugmentSelectors();
                OnAugmentIndexChanged();
            }
        }

        public void SelectAugmentIndex(int index)
        {
            if (index != _augmentIndex)
            {
                _augmentIndex = index;
                OnAugmentIndexChanged();
            }
        }

        public void OnAugmentIndexChanged()
        {
            Tuple<InventoryItemListElement, int> selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            if (selectedItem?.Item1.GetItem() == null)
            {
                MainButton.interactable = false;
                AvailableEffectsText.text = "";
                CostLabel.enabled = false;
                CostList.SetItems(new List<IListElement>());
                _augmentIndex = -1;
                return;
            }

            if (_augmentIndex < 0)
            {
                AvailableEffectsText.text = string.Empty;
                CostLabel.enabled = false;
                CostList.SetItems(new List<IListElement>());
                MainButton.interactable = false;
            }
            else
            {
                ItemDrop.ItemData item = selectedItem.Item1.GetItem();
                string info = EnchantingUIController.GetAvailableAugmentEffects(item, _augmentIndex);

                AvailableEffectsText.text = info;
                ScrollEnchantInfoToTop();

                CostLabel.enabled = true;
                List<InventoryItemListElement> cost = EnchantingUIController.GetAugmentCost(item, _augmentIndex);
                CostList.SetItems(cost.Cast<IListElement>().ToList());

                Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Augment);
                float reenchantCostReduction = float.IsNaN(featureValues.Item2) ? 0 : featureValues.Item2;
                if (reenchantCostReduction > 0)
                {
                    CostLabel.text = Localization.instance.Localize($"$mod_epicloot_augmentcost " +
                        $"<color=#EAA800>(-{reenchantCostReduction}% $item_coins!)</color>");
                }
                else
                {
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_augmentcost");
                }

                bool canAfford = LocalPlayerCanAffordCost(cost);
                bool featureUnlocked = EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.Augment);
                MainButton.interactable = featureUnlocked && canAfford && _augmentIndex >= 0;
            }
        }

        private void ScrollEnchantInfoToTop()
        {
            AvailableEffectsScrollbar.value = 1;
        }

        protected override void DoMainAction()
        {
            Tuple<InventoryItemListElement, int> selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            if (selectedItem?.Item1.GetItem() == null)
            {
                Cancel();
                return;
            }

            ItemDrop.ItemData item = selectedItem.Item1.GetItem();

            // Checked before paying: the item may have left the inventory, or its chest reloaded and
            // replaced it, since it was selected. An augment only lasts on an item whose storage saves it.
            if (!InventoryManagement.Instance.IsEditable(item))
            {
                Debug.LogWarning("[Augment Item] Cancelled: the item is no longer held anywhere the change would be saved.");
                NotifyItemsUnavailable(item);
                Cancel();
                AvailableItems.SetItems(EnchantingUIController.GetAugmentableItems().Cast<IListElement>().ToList());
                DeselectAll();
                return;
            }

            List<InventoryItemListElement> cost = EnchantingUIController.GetAugmentCost(item, _augmentIndex);
            int augmentIndex = _augmentIndex;
            PayThen(new InventoryManagement.Payment().Cost(cost), receipt =>
            {
                if (!InventoryManagement.Instance.IsEditable(item))
                {
                    receipt.Refund();
                    NotifyItemsUnavailable(item);
                    AvailableItems.SetItems(EnchantingUIController.GetAugmentableItems().Cast<IListElement>().ToList());
                    DeselectAll();
                    return;
                }

                if (_choiceDialog != null)
                {
                    Destroy(_choiceDialog);
                }

                _choiceDialog = EnchantingUIController.AugmentItem(item, augmentIndex);
                if (_choiceDialog == null)
                {
                    receipt.Refund();
                    return;
                }

                EnchantingUIController.SetupUIAudioSources(_choiceDialog);
                Lock();
            }, () => Cancel());
        }

        protected override void OnSelectedItemsChanged()
        {
            Tuple<InventoryItemListElement, int> entry = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            ItemDrop.ItemData item = entry?.Item1.GetItem();

            RefreshAugmentSelectors();

            if (item == null)
            {
                AvailableEffectsText.text = string.Empty;
            }

            // The first augmentable effect, or -1 for none: never a selector disabled as non-augmentable.
            _augmentIndex = _AugmentSelectors.FindIndex(selector => selector.interactable);
            HighlightSelectedAugment();
            OnAugmentIndexChanged();
        }

        private void RefreshAugmentSelectors()
        {
            Tuple<InventoryItemListElement, int> entry = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            if (EnchantList.childCount > 0)
            {
                foreach (Transform child in EnchantList)
                {
                    Destroy(child.gameObject);
                }
            }

            _AugmentSelectors.Clear();
            if (entry == null || entry.Item1 == null)
            {
                return;
            }

            ItemDrop.ItemData item = entry?.Item1.GetItem();
            List<Tuple<string, bool>> augmentableEffects = EnchantingUIController.GetEnchantmentEffects(item, false);

            foreach (Tuple<string, bool> effect in augmentableEffects)
            {
                GameObject enchantmentListElement = Instantiate(EnchantmentListPrefab, EnchantList);
                TMP_Text enchantmentElement = enchantmentListElement.GetComponentInChildren<TMP_Text>(true);
                Toggle enchantmentbutton = enchantmentListElement.GetComponent<Toggle>();
                EnchantingUIController.SetupUIAudioSources(enchantmentListElement);

                _AugmentSelectors.Add(enchantmentbutton);
                enchantmentbutton.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        SelectAugmentIndex(_AugmentSelectors.IndexOf(enchantmentbutton));
                    }
                });

                if (enchantmentElement != null)
                {
                    enchantmentElement.text = effect.Item1;
                    MagicTextShimmer.Ensure(enchantmentElement);
                }

                if (effect.Item2 == false)
                {
                    enchantmentbutton.interactable = false;
                }
                enchantmentListElement.SetActive(true);
            }

            // Rebuilt selectors all come back unchecked (e.g. after an augment completes), while
            // _augmentIndex still drives the cost and the Augment button.
            HighlightSelectedAugment();
        }

        // Matches the selector toggles to _augmentIndex without notifying, so restoring the
        // highlight does not re-enter SelectAugmentIndex or replay the selection sound.
        private void HighlightSelectedAugment()
        {
            for (int index = 0; index < _AugmentSelectors.Count; ++index)
            {
                _AugmentSelectors[index].SetIsOnWithoutNotify(index == _augmentIndex);
            }
        }

        public override bool CanCancel()
        {
            return base.CanCancel() || (_choiceDialog != null && _choiceDialog.activeSelf);
        }

        public override void Cancel()
        {
            base.Cancel();
            OnAugmentIndexChanged();
        }

        public override void Lock()
        {
            base.Lock();
            foreach (Toggle selector in _AugmentSelectors)
            {
                selector.interactable = false;
            }
        }

        public override void Unlock()
        {
            base.Unlock();
            foreach (Toggle selector in _AugmentSelectors)
            {
                selector.interactable = true;
            }
        }
    }
}
