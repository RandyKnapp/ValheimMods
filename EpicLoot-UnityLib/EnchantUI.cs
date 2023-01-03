using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class EnchantUI : EnchantingTableUIPanelBase
    {
        public Text EnchantInfo;
        public Scrollbar EnchantInfoScrollbar;
        public List<Toggle> RarityButtons;

        [Header("Cost")]
        public Text CostLabel;
        public MultiSelectItemList CostList;

        public AudioClip[] EnchantCompleteSFX;

        public delegate List<InventoryItemListElement> GetEnchantableItemsDelegate();
        public delegate string GetEnchantInfoDelegate(ItemDrop.ItemData item, MagicRarityUnity rarity);
        public delegate List<InventoryItemListElement> GetEnchantCostDelegate(ItemDrop.ItemData item, MagicRarityUnity rarity);
        // Returns the success dialog
        public delegate GameObject EnchantItemDelegate(ItemDrop.ItemData item, MagicRarityUnity rarity);

        public static GetEnchantableItemsDelegate GetEnchantableItems;
        public static GetEnchantInfoDelegate GetEnchantInfo;
        public static GetEnchantCostDelegate GetEnchantCost;
        public static EnchantItemDelegate EnchantItem;

        private ToggleGroup _toggleGroup;
        private MagicRarityUnity _rarity;
        private GameObject _successDialog;

        public override void Awake()
        {
            base.Awake();

            if (RarityButtons.Count > 0)
            {
                _toggleGroup = RarityButtons[0].group;
                _toggleGroup.EnsureValidState();
            }

            for (var index = 0; index < RarityButtons.Count; index++)
            {
                var rarityButton = RarityButtons[index];
                rarityButton.onValueChanged.AddListener((isOn) => {
                    if (isOn)
                        RefreshRarity();
                });
            }
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            _rarity = MagicRarityUnity.Magic;
            OnRarityChanged();
            RarityButtons[0].isOn = true;
            var items = GetEnchantableItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
        }

        public override void Update()
        {
            base.Update();

            if (_successDialog != null && !_successDialog.activeSelf)
            {
                Unlock();
                Destroy(_successDialog);
                _successDialog = null;
            }
        }

        public void RefreshRarity()
        {
            var prevRarity = _rarity;
            for (var index = 0; index < RarityButtons.Count; index++)
            {
                var button = RarityButtons[index];
                if (button.isOn)
                {
                    _rarity = (MagicRarityUnity)index;
                }
            }

            if (prevRarity != _rarity)
                OnRarityChanged();
        }

        public void OnRarityChanged()
        {
            var selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            if (selectedItem?.Item1.GetItem() == null)
            {
                MainButton.interactable = false;
                EnchantInfo.text = "";
                CostLabel.enabled = false;
                CostList.SetItems(new List<IListElement>());
                return;
            }

            var item = selectedItem.Item1.GetItem();
            var info = GetEnchantInfo(item, _rarity);

            EnchantInfo.text = info;
            ScrollEnchantInfoToTop();

            CostLabel.enabled = true;
            var cost = GetEnchantCost(item, _rarity);
            CostList.SetItems(cost.Cast<IListElement>().ToList());

            var canAfford = LocalPlayerCanAffordCost(cost);
            MainButton.interactable = canAfford;
        }

        private void ScrollEnchantInfoToTop()
        {
            EnchantInfoScrollbar.value = 1;
        }

        protected override void DoMainAction()
        {
            var selectedItem = AvailableItems.GetSelectedItems<InventoryItemListElement>().FirstOrDefault();
            if (selectedItem?.Item1.GetItem() == null)
                return;

            var item = selectedItem.Item1.GetItem();
            var cost = GetEnchantCost(item, _rarity);

            var player = Player.m_localPlayer;
            if (!player.NoCostCheat())
            {
                var inventory = player.GetInventory();
                foreach (var costElement in cost)
                {
                    var costItem = costElement.GetItem();
                    inventory.RemoveItem(costItem.m_shared.m_name, costItem.m_stack);
                }
            }

            if (_successDialog != null)
                Destroy(_successDialog);

            DeselectAll();
            Lock();

            _successDialog = EnchantItem(item, _rarity);

            RefreshAvailableItems();
        }

        protected override AudioClip GetCompleteAudioClip()
        {
            return EnchantCompleteSFX[(int)_rarity];
        }

        public void RefreshAvailableItems()
        {
            var items = GetEnchantableItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
        }

        protected override void OnSelectedItemsChanged()
        {
            OnRarityChanged();
        }

        private bool LocalPlayerCanAffordCost(List<InventoryItemListElement> cost)
        {
            var player = Player.m_localPlayer;
            if (player.NoCostCheat())
                return true;

            var inventory = player.GetInventory();
            foreach (var element in cost)
            {
                var item = element.GetItem();
                if (inventory.CountItems(item.m_shared.m_name) < item.m_stack)
                    return false;
            }

            return true;
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

            foreach (var modeButton in RarityButtons)
            {
                modeButton.interactable = false;
            }
        }

        public override void Unlock()
        {
            base.Unlock();

            foreach (var modeButton in RarityButtons)
            {
                modeButton.interactable = true;
            }
        }

        public override void DeselectAll()
        {
            AvailableItems.DeselectAll();
        }
    }
}
