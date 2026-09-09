using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.CraftingV2;
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

        private ToggleGroup _toggleGroup;
        private ItemRarity _rarity;
        private GameObject _successDialog;

        public override void Awake()
        {
            base.Awake();

            EnsureRarityButtons();

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

        // The prefab is authored with one toggle per rarity of its day, and RefreshRarity casts a toggle's
        // index straight to ItemRarity. When the enum has outgrown the prefab (a bundle that predates a
        // rarity), the last toggle is cloned for each missing tier so every rarity stays selectable. The
        // authored prefab is still the real fix; this only keeps an older bundle usable.
        private void EnsureRarityButtons()
        {
            if (RarityButtons == null || RarityButtons.Count == 0)
            {
                return;
            }

            while (RarityButtons.Count < Rarities.Count)
            {
                Toggle template = RarityButtons[RarityButtons.Count - 1];
                var rarity = (ItemRarity)RarityButtons.Count;
                GameObject clone = Instantiate(template.gameObject, template.transform.parent);
                clone.name = $"EnchantRaritySelector ({(int)rarity})";

                // The panel root was localized before this tab ever woke, so the clone's label is
                // localized here rather than left as a token.
                foreach (Text label in clone.GetComponentsInChildren<Text>(true))
                {
                    label.text = Localization.instance.Localize($"$mod_epicloot_{rarity}");
                }

                if (clone.TryGetComponent(out SetRarityColor rarityColor))
                {
                    rarityColor.SetRarity(rarity);
                }

                Toggle toggle = clone.GetComponent<Toggle>();
                toggle.group = template.group;
                toggle.isOn = false;
                RarityButtons.Add(toggle);

                // The selector column is a fixed-height vertical layout; give it room for one more row.
                if (template.transform.parent is RectTransform column &&
                    template.transform is RectTransform templateRect)
                {
                    float spacing = column.TryGetComponent(out VerticalLayoutGroup layout) ? layout.spacing : 0f;
                    column.sizeDelta = new Vector2(column.sizeDelta.x, column.sizeDelta.y + templateRect.sizeDelta.y + spacing);
                }
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
            List<InventoryItemListElement> cost = EnchantingUIController.GetEnchantCost(item, _rarity);

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

            _successDialog = EnchantingUIController.EnchantItemAndReturnSuccessDialog(item, _rarity);

            RefreshAvailableItems();
        }

        protected override AudioClip GetCompleteAudioClip()
        {
            // A bundle that predates a rarity has fewer clips than toggles; the top clip covers the rest.
            return EnchantCompleteSFX[Mathf.Min((int)_rarity, EnchantCompleteSFX.Length - 1)];
        }

        public void RefreshAvailableItems()
        {
            List<InventoryItemListElement> items = EnchantingUIController.GetEnchantableItems();
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
