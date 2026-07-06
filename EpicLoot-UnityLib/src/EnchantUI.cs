using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
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
            foreach(AudioSource audioSource in this.GetComponentsInChildren<AudioSource>())
            {
                audioSource.volume = AudioVolumeLevel();
            }

            _rarity = MagicRarityUnity.Magic;
            OnRarityChanged();
            RarityButtons[0].isOn = true;
            List<InventoryItemListElement> items = GetEnchantableItems();
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

                if (EnchantInfoScrollbar != null)
                {
                    float rightStickAxis = ZInput.GetJoyRightStickY();
                    if (Mathf.Abs(rightStickAxis) > 0.5f)
                    {
                        EnchantInfoScrollbar.value = Mathf.Clamp01(EnchantInfoScrollbar.value + rightStickAxis * -0.1f);
                    }
                }
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
            MagicRarityUnity prevRarity = _rarity;
            for (int index = 0; index < RarityButtons.Count; index++)
            {
                Toggle button = RarityButtons[index];
                if (button.isOn)
                {
                    _rarity = (MagicRarityUnity)index;
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
            string info = GetEnchantInfo(item, _rarity);

            EnchantInfo.text = info;
            ScrollEnchantInfoToTop();

            CostLabel.enabled = true;
            List<InventoryItemListElement> cost = GetEnchantCost(item, _rarity);
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
            List<InventoryItemListElement> cost = GetEnchantCost(item, _rarity);

            Player player = Player.m_localPlayer;
            if (!player.NoCostCheat())
            {
                if (!LocalPlayerCanAffordCost(cost))
                {
                    Debug.LogError("[Enchant Item] ERROR: Tried to enchant item but could not afford the cost. This should not happen!");
                    return;
                }

                foreach (InventoryItemListElement costElement in cost)
                {
                    InventoryManagement.Instance.RemoveItem(costElement.GetItem());
                }
            }

            if (_successDialog != null)
            {
                Destroy(_successDialog);
            }

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
            List<InventoryItemListElement> items = GetEnchantableItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
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

        public override void DeselectAll()
        {
            AvailableItems.DeselectAll();
        }
    }
}
