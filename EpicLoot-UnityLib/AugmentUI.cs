using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class AugmentUI : EnchantingTableUIPanelBase
    {
        public Text AvailableEffectsText;
        public Scrollbar AvailableEffectsScrollbar;
        public List<Toggle> AugmentSelectors;

        [Header("Cost")]
        public Text CostLabel;
        public MultiSelectItemList CostList;

        public delegate List<InventoryItemListElement> GetAugmentableItemsDelegate();
        public delegate List<Tuple<string, bool>> GetAugmentableEffectsDelegate(ItemDrop.ItemData item);
        public delegate string GetAvailableEffectsDelegate(ItemDrop.ItemData item, int augmentIndex);
        public delegate List<InventoryItemListElement> GetAugmentCostDelegate(ItemDrop.ItemData item, int augmentIndex);
        // Returns the augment choice dialog
        public delegate GameObject AugmentItemDelegate(ItemDrop.ItemData item, int augmentIndex);

        public static GetAugmentableItemsDelegate GetAugmentableItems;
        public static GetAugmentableEffectsDelegate GetAugmentableEffects;
        public static GetAvailableEffectsDelegate GetAvailableEffects;
        public static GetAugmentCostDelegate GetAugmentCost;
        public static AugmentItemDelegate AugmentItem;

        private int _augmentIndex;
        private GameObject _choiceDialog;

        public override void Awake()
        {
            base.Awake();

            for (var index = 0; index < AugmentSelectors.Count; index++)
            {
                var augmentSelector = AugmentSelectors[index];
                augmentSelector.onValueChanged.AddListener(OnAugmentSelectorToggled);
            }
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            _augmentIndex = -1;
            foreach (var augmentSelector in AugmentSelectors)
            {
                augmentSelector.isOn = false;
            }

            if (AvailableEffectsHeader != null)
            {
                var augmentChoices = 2;
                var featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Augment);
                if (!float.IsNaN(featureValues.Item1))
                    augmentChoices = (int)featureValues.Item1;

                var colorPre = augmentChoices > 2 ? "<color=#EAA800>" : "";
                var colorPost = augmentChoices > 2 ? "</color>" : "";
                AvailableEffectsHeader.text = Localization.instance.Localize($"$mod_epicloot_augment_availableeffects {colorPre}($mod_epicloot_augment_choices){colorPost}", augmentChoices.ToString());
            }

            OnAugmentIndexChanged();

            var items = GetAugmentableItems();
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
                    var activeAugmentCount = AugmentSelectors.Count(x => x.isActiveAndEnabled);
                    var nextAugmentIndex = (_augmentIndex + 1) % activeAugmentCount;
                    AugmentSelectors[nextAugmentIndex].isOn = true;
                    ZInput.ResetButtonStatus("JoyButtonY");
                }

                if (AvailableEffectsScrollbar != null)
                {
                    var rightStickAxis = ZInput.GetJoyRightStickY();
                    if (Mathf.Abs(rightStickAxis) > 0.5f)
                        AvailableEffectsScrollbar.value = Mathf.Clamp01(AvailableEffectsScrollbar.value + rightStickAxis * -0.1f);
                }
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
                        return;
                    e.SetItem(e.GetListElement());
                    e.Refresh();
                });
                RefreshAugmentSelectors();
                OnAugmentIndexChanged();
            }
        }

        public void OnAugmentSelectorToggled(bool isOn)
        {
            if (isOn)
            {
                for (var index = 0; index < AugmentSelectors.Count; index++)
                {
                    var selector = AugmentSelectors[index];
                    if (selector.isOn)
                    {
                        SelectAugmentIndex(index);
                        return;
                    }
                }
            }
            else
            {
                if (!AugmentSelectors.Any(x => x.isOn))
                    SelectAugmentIndex(-1);
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
            var selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            if (selectedItem?.Item1.GetItem() == null)
            {
                MainButton.interactable = false;
                AvailableEffectsText.text = "";
                CostLabel.enabled = false;
                CostList.SetItems(new List<IListElement>());
                _augmentIndex = -1;
                foreach (var augmentSelector in AugmentSelectors)
                {
                    augmentSelector.isOn = false;
                }
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
                var item = selectedItem.Item1.GetItem();
                var info = GetAvailableEffects(item, _augmentIndex);

                AvailableEffectsText.text = info;
                ScrollEnchantInfoToTop();

                CostLabel.enabled = true;
                var cost = GetAugmentCost(item, _augmentIndex);
                CostList.SetItems(cost.Cast<IListElement>().ToList());

                var featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Augment);
                var reenchantCostReduction = float.IsNaN(featureValues.Item2) ? 0 : featureValues.Item2;
                if (reenchantCostReduction > 0)
                    CostLabel.text = Localization.instance.Localize($"$mod_epicloot_augmentcost <color=#EAA800>(-{reenchantCostReduction}% $item_coins!)</color>");
                else
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_augmentcost");

                var canAfford = LocalPlayerCanAffordCost(cost);
                var featureUnlocked = EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.Augment);
                MainButton.interactable = featureUnlocked && canAfford && _augmentIndex >= 0;
            }
        }

        private void ScrollEnchantInfoToTop()
        {
            AvailableEffectsScrollbar.value = 1;
        }

        protected override void DoMainAction()
        {
            var selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            if (selectedItem?.Item1.GetItem() == null)
            {
                Cancel();
                return;
            }

            var item = selectedItem.Item1.GetItem();
            var cost = GetAugmentCost(item, _augmentIndex);

            var player = Player.m_localPlayer;
            if (!player.NoCostCheat())
            {
                if (!LocalPlayerCanAffordCost(cost))
                {
                    Debug.LogError("[Augment Item] ERROR: Tried to augment item but could not afford the cost. This should not happen!");
                    return;
                }

                var inventory = player.GetInventory();
                foreach (var costElement in cost)
                {
                    var costItem = costElement.GetItem();
                    inventory.RemoveItem(costItem.m_shared.m_name, costItem.m_stack);
                }
            }

            if (_choiceDialog != null)
                Destroy(_choiceDialog);

            _choiceDialog = AugmentItem(item, _augmentIndex);

            Lock();
        }

        protected override void OnSelectedItemsChanged()
        {
            var entry = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            var item = entry?.Item1.GetItem();
            var augmentableEffects = GetAugmentableEffects(item);

            if (augmentableEffects.Count > AugmentSelectors.Count)
            {
                Debug.LogError($"[Epic Loot] ERROR: Too many magic effects to show! (Max: {AugmentSelectors.Count})");
            }

            for (var index = 0; index < AugmentSelectors.Count; index++)
            {
                var selector = AugmentSelectors[index];
                if (selector == null)
                    continue;

                selector.SetIsOnWithoutNotify(index == 0);
                selector.gameObject.SetActive(item != null && index < augmentableEffects.Count);
                if (!selector.gameObject.activeSelf)
                    continue;

                var selectorText = selector.GetComponentInChildren<Text>();
                if (selectorText != null)
                {
                    selectorText.text = augmentableEffects[index].Item1;
                    selector.interactable = augmentableEffects[index].Item2;
                }
            }

            if (item == null)
                AvailableEffectsText.text = string.Empty;

            _augmentIndex = 0;
            OnAugmentIndexChanged();
        }

        private void RefreshAugmentSelectors()
        {
            var entry = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            var item = entry?.Item1.GetItem();
            var augmentableEffects = GetAugmentableEffects(item);

            for (var index = 0; index < AugmentSelectors.Count; index++)
            {
                var selector = AugmentSelectors[index];
                if (selector == null)
                    continue;

                selector.SetIsOnWithoutNotify(index == _augmentIndex);
                selector.gameObject.SetActive(item != null && index < augmentableEffects.Count);
                if (!selector.gameObject.activeSelf)
                    continue;

                var selectorText = selector.GetComponentInChildren<Text>();
                if (selectorText != null)
                {
                    selectorText.text = augmentableEffects[index].Item1;
                    selector.interactable = augmentableEffects[index].Item2;
                }
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
            foreach (var selector in AugmentSelectors)
            {
                selector.interactable = false;
            }
        }

        public override void Unlock()
        {
            base.Unlock();
            foreach (var selector in AugmentSelectors)
            {
                selector.interactable = true;
            }
        }

        public override void DeselectAll()
        {
            AvailableItems.DeselectAll();
        }
    }
}
