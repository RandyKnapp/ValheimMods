using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.CraftingV2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class EnchantUI : EnchantingTableUIPanelBase
    {
        public TMP_Text EnchantInfo;
        public Scrollbar EnchantInfoScrollbar;
        public List<Toggle> RarityButtons;

        [Header("Cost")]
        public TMP_Text CostLabel;
        public MultiSelectItemList CostList;

        public AudioClip[] EnchantCompleteSFX;

        private ItemRarity _rarity;
        private GameObject _successDialog;

        public override void Awake()
        {
            base.Awake();

            MagicTextShimmer.Ensure(EnchantInfo);

            if (RarityButtons.Count > 0)
            {
                RarityButtons[0].group.EnsureValidState();
            }

            for (int index = 0; index < RarityButtons.Count; index++)
            {
                Toggle rarityButton = RarityButtons[index];
                rarityButton.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                        RefreshRarity();
                });
            }
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            EnchantingUIController.SetupUIAudioSources(gameObject);

            _rarity = ItemRarity.Magic;
            OnRarityChanged();
            RarityButtons[0].isOn = true;
            List<InventoryItemListElement> items = EnchantingUIController.GetEnchantableItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
        }

        public override void Update()
        {
            base.Update();

            if (!_locked && ZInput.IsGamepadActive())
            {
                if (ZInput.GetButtonDown("JoyButtonY"))
                {
                    int nextModeIndex = ((int)_rarity + 1) % RarityButtons.Count;
                    RarityButtons[nextModeIndex].isOn = true;
                    ZInput.ResetButtonStatus("JoyButtonY");
                }

                GamepadScroll.ApplyRightStickY(EnchantInfoScrollbar);
            }

            if (_successDialog != null && !_successDialog.activeSelf)
            {
                Unlock();
                Destroy(_successDialog);
                _successDialog = null;
            }
        }

        public void RefreshRarity()
        {
            ItemRarity prevRarity = _rarity;
            for (int index = 0; index < RarityButtons.Count; index++)
            {
                Toggle button = RarityButtons[index];
                if (button.isOn)
                {
                    _rarity = (ItemRarity)index;
                }
            }

            if (prevRarity != _rarity)
            {
                OnRarityChanged();
            }
        }

        public void OnRarityChanged()
        {
            System.Tuple<InventoryItemListElement, int> selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            if (selectedItem?.Item1.GetItem() == null)
            {
                MainButton.interactable = false;
                EnchantInfo.text = "";
                CostLabel.enabled = false;
                CostList.SetItems(new List<IListElement>());
                return;
            }

            ItemDrop.ItemData item = selectedItem.Item1.GetItem();
            string info = EnchantingUIController.GetEnchantInfo(item, _rarity);

            EnchantInfo.text = info;
            ScrollEnchantInfoToTop();

            CostLabel.enabled = true;
            List<InventoryItemListElement> cost = EnchantingUIController.GetEnchantCost(item, _rarity);
            CostList.SetItems(cost.Cast<IListElement>().ToList());

            bool canAfford = LocalPlayerCanAffordCost(cost);
            bool featureUnlocked = EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.Enchant);
            MainButton.interactable = featureUnlocked && canAfford;
        }

        private void ScrollEnchantInfoToTop()
        {
            EnchantInfoScrollbar.value = 1;
        }

        protected override void DoMainAction()
        {
            System.Tuple<InventoryItemListElement, int> selectedItem =
                AvailableItems.GetSelectedItems<InventoryItemListElement>().FirstOrDefault();

            Cancel();

            if (selectedItem?.Item1.GetItem() == null)
            {
                return;
            }

            ItemDrop.ItemData item = selectedItem.Item1.GetItem();

            // Checked before paying: the item may have left the inventory, or its chest reloaded and
            // replaced it, since it was selected. An enchant only lasts on an item whose storage saves it.
            if (!InventoryManagement.Instance.IsEditable(item))
            {
                Debug.LogWarning("[Enchant Item] Cancelled: the item is no longer held anywhere the change would be saved.");
                NotifyItemsUnavailable(item);
                RefreshAvailableItems();
                return;
            }

            List<InventoryItemListElement> cost = EnchantingUIController.GetEnchantCost(item, _rarity);
            ItemRarity rarity = _rarity;
            PayThen(new InventoryManagement.Payment().Cost(cost), receipt =>
            {
                // The item may have moved while the storage's owner answered.
                if (!InventoryManagement.Instance.IsEditable(item))
                {
                    receipt.Refund();
                    NotifyItemsUnavailable(item);
                    RefreshAvailableItems();
                    return;
                }

                if (_successDialog != null)
                {
                    Destroy(_successDialog);
                }

                DeselectAll();
                Lock();

                _successDialog = EnchantingUIController.EnchantItemAndReturnSuccessDialog(item, rarity);

                RefreshAvailableItems();
            }, RefreshAvailableItems);
        }

        protected override AudioClip GetCompleteAudioClip()
        {
            // A bundle that predates a rarity has fewer clips than toggles; the top clip covers the rest.
            return EnchantCompleteSFX[Mathf.Min((int)_rarity, EnchantCompleteSFX.Length - 1)];
        }

        public void RefreshAvailableItems()
        {
            List<InventoryItemListElement> items = EnchantingUIController.GetEnchantableItems();
            AvailableItems.SetItemsAndDeselect(items.Cast<IListElement>().ToList());
        }

        protected override void OnSelectedItemsChanged()
        {
            OnRarityChanged();
        }
        
        public override bool CanCancel()
        {
            return base.CanCancel() || (_successDialog != null && _successDialog.activeSelf);
        }

        public override void Cancel()
        {
            base.Cancel();

            if (_successDialog != null && _successDialog.activeSelf)
            {
                Destroy(_successDialog);
                _successDialog = null;
            }

            OnRarityChanged();
        }

        public override void Lock()
        {
            base.Lock();

            foreach (Toggle modeButton in RarityButtons)
            {
                modeButton.interactable = false;
            }
        }

        public override void Unlock()
        {
            base.Unlock();

            foreach (Toggle modeButton in RarityButtons)
            {
                modeButton.interactable = true;
            }
        }
    }
}
