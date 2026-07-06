using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class RuneUI : EnchantingTableUIPanelBase
    {
        public Toggle RuneExtractButton;
        public Toggle RuneEtchButton;

        [Header("Cost")]
        public Text CostLabel;
        public MultiSelectItemList CostList;

        [Header("Rune Selector")]
        public RectTransform EnchantList;
        public GameObject EnchantmentListPrefab;
        public GameObject AvailableRunesWindow;
        public MultiSelectItemList AvailableRunes;

        public Text Warning;

        public AudioClip RunicActionCompleted;

        // These use delegates which are connected at runtime from the non-unity side of EL
        public delegate List<InventoryItemListElement> GetRuneExtractItemsDelegate();
        public delegate List<InventoryItemListElement> GetRuneEtchItemsDelegate();
        public delegate List<InventoryItemListElement> GetApplyableRunesDelegate(ItemDrop.ItemData item, string selected_enchantment);
        public delegate List<Tuple<string, bool>> GetItemEnchantsDelegate(ItemDrop.ItemData item, bool runecheck);
        public delegate List<InventoryItemListElement> GetRuneExtractCostDelegate(ItemDrop.ItemData item, MagicRarityUnity rarity, float costModifier);
        public delegate List<InventoryItemListElement> GetRuneEtchCostDelegate(ItemDrop.ItemData item, MagicRarityUnity rarity, float costModifier);
        public delegate bool RuneItemDestructionEnabledDelegate();
        public delegate MagicRarityUnity GetItemRarityDelegate(ItemDrop.ItemData item);
        public delegate ItemDrop.ItemData GetItemEnchantedByRuneDelegate(ItemDrop.ItemData item, int enchantment, float powerModifier);
        public delegate string GetSelectedEnchantmentByIndexDelegate(ItemDrop.ItemData item, int enchantment);
        public delegate GameObject ApplyRuneToItemAndReturnSuccess(ItemDrop.ItemData item, ItemDrop.ItemData rune, int enchantment);

        public static GetApplyableRunesDelegate GetApplyableRunes;
        public static GetRuneExtractItemsDelegate GetRuneExtractItems;
        public static GetRuneEtchItemsDelegate GetRuneEtchItems;
        public static GetItemEnchantsDelegate GetItemEnchants;
        public static GetRuneExtractCostDelegate GetRuneExtractCost;
        public static GetRuneEtchCostDelegate GetRuneEtchCost;
        public static GetItemEnchantedByRuneDelegate ItemToBeRuned;
        public static RuneItemDestructionEnabledDelegate ExtractItemsDestroyed;
        public static GetItemRarityDelegate GetItemRarity;
        public static ApplyRuneToItemAndReturnSuccess RuneEnchancedItem;
        public static GetSelectedEnchantmentByIndexDelegate GetSelectedEnchantmentByIndex;

        private RuneAction _runeAction;
        private GameObject _successDialog;
        private ItemDrop.ItemData _selectedItem;
        private ItemDrop.ItemData _selectedOverrideRune;
        private MagicRarityUnity _selectedRarity = MagicRarityUnity.Magic;
        private int _selectedEnchantmentIndex = -1;

        private enum RuneAction
        {
            Extract,
            Etch
        }

        public override void Awake()
        {
            base.Awake();

            RuneExtractButton.onValueChanged.AddListener((isOn) =>
            {
                ExtractModeSelected(isOn);
            });

            RuneEtchButton.onValueChanged.AddListener((isOn) =>
            {
                EtchModeSelected(isOn);
            });

            AvailableRunes.OnSelectedItemsChanged += OnSelectedOverrideRuneChanged;
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            RuneExtractButton.isOn = false;
            RuneEtchButton.Select();
            RuneEtchButton.isOn = true;
            EtchModeSelected(true);
        }

        public override void Update()
        {
            base.Update();

            bool featureUnlocked = EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.Rune);
            if (!featureUnlocked && !Player.m_localPlayer.NoCostCheat())
            {
                return;
            }

            // Check if the action is completed, and unlock the UI
            if (_successDialog != null && !_successDialog.activeSelf)
            {
                Unlock();
                Destroy(_successDialog);
                _successDialog = null;
            }
        }

        public void UpdateDisplaySelectedItemEnchantments()
        {
            if (_selectedItem == null)
            {
                MainButton.interactable = false;
                return;
            }

            // Set the enchantments to be selected based on the enchantments on this item
            List<Tuple<string, bool>> info = GetItemEnchants(_selectedItem, true);
            RefreshSelectableEnchantments();
            UpdateDisplayAvailableOverwriteEnchantments(); //TODO remove?

            // Set enchantment list to the enchantments of the selected item
            Tuple<float, float> featureValues =
                EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Rune);

            float costReduction = GetCostReduction(featureValues.Item1);

            CostLabel.enabled = true;
            List<InventoryItemListElement> cost;

            if (_runeAction == RuneAction.Extract)
            {
                cost = GetRuneExtractCost(_selectedItem, _selectedRarity, costReduction);
            }
            else if (_runeAction == RuneAction.Etch)
            {
                cost = GetRuneEtchCost(_selectedItem, _selectedRarity, costReduction);
            }
            else
            {
                cost = new List<InventoryItemListElement>();
            }

            CostList.SetItems(cost.Cast<IListElement>().ToList());

            CheckIfActionDoable();
        }

        public void UpdateDisplayAvailableOverwriteEnchantments()
        {
            if (_selectedItem == null || _runeAction == RuneAction.Extract || _selectedEnchantmentIndex <= -1)
            {
                AvailableRunes.SetItems(new List<IListElement>());
                MainButton.interactable = false;
                return;
            }

            List<InventoryItemListElement> availableEnchantRunes =
                GetApplyableRunes(_selectedItem, GetSelectedEnchantmentByIndex(_selectedItem, _selectedEnchantmentIndex));
            AvailableRunes.SetItems(availableEnchantRunes.Cast<IListElement>().ToList());
        }

        private void ClearEnchantmentList()
        {
            // Clear the enchantment list
            if (EnchantList.childCount > 0)
            {
                foreach (Transform child in EnchantList)
                {
                    Destroy(child.gameObject);
                }
            }
            _selectedEnchantmentIndex = -1;
        }

        private void RefreshSelectableEnchantments()
        {
            Tuple<InventoryItemListElement, int> entry = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            ItemDrop.ItemData item = entry?.Item1.GetItem();
            List<Tuple<string, bool>> augmentableEffects = GetItemEnchants(item, true);

            ClearEnchantmentList();

            int enchantIndex = 0;
            foreach (Tuple<string, bool> effect in augmentableEffects)
            {
                GameObject enchantmentListElement = Instantiate(EnchantmentListPrefab, EnchantList);
                Text enchantmentElement = enchantmentListElement.GetComponentInChildren<Text>();
                Toggle enchantmentbutton = enchantmentListElement.GetComponent<Toggle>();
                enchantmentbutton.onValueChanged.AddListener((isOn) =>
                {
                    SetSelectedEnchantIndex();
                    UpdateDisplayAvailableOverwriteEnchantments();
                    CheckIfActionDoable();
                });

                if (enchantmentElement != null)
                {
                    enchantmentElement.text = effect.Item1;
                }

                enchantmentListElement.SetActive(true);
                enchantIndex++;
            }
        }

        private void SetSelectedEnchantIndex()
        {
            if (EnchantList.childCount > 0)
            {
                int index = 0;
                foreach (Transform child in EnchantList)
                {
                    if (child.GetComponent<Toggle>().isOn == true)
                    {
                        _selectedEnchantmentIndex = index;
                        return;
                    }
                    index++;
                }
            }

            _selectedEnchantmentIndex = -1;
        }

        public bool LocalPlayerCanAffordRuneCost(List<InventoryItemListElement> cost)
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
                    return false;
                }
            }

            return true;
        }

        public void ExtractModeSelected(bool enabled)
        {
            _runeAction = RuneAction.Extract;
            MainButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$mod_epicloot_rune_extract");
            Warning.text = Localization.instance.Localize("$mod_epicloot_rune_extract_warning");

            // Deselect runes and clear them
            AvailableRunesWindow.SetActive(false);
            if (AvailableRunes.GetItemCount() > 0)
            {
                AvailableRunes.SetItems(new List<IListElement>());
            }

            NewModeSelected(enabled);
        }

        public void EtchModeSelected(bool enabled)
        {
            _runeAction = RuneAction.Etch;
            MainButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$mod_epicloot_rune_etch");
            Warning.text = Localization.instance.Localize("$mod_epicloot_rune_etch_warning");

            AvailableRunesWindow.SetActive(true);

            NewModeSelected(enabled);
        }

        private void NewModeSelected(bool enabled)
        {
            RefreshAvailableItems();
            _selectedEnchantmentIndex = -1;

            if (!enabled)
            {
                MainButton.interactable = false;
                return;
            }

            Tuple<InventoryItemListElement, int> selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();

            // Clears the list of enchantments if no item is selected
            if (selectedItem?.Item1.GetItem() == null)
            {
                CostLabel.enabled = false;
                CostList.SetItems(new List<IListElement>());
                AvailableRunes.SetItems(new List<IListElement>());
                MainButton.interactable = false;
                return;
            }
            else
            {
                // Check the currently selected item
                if (selectedItem?.Item1.GetItem() != _selectedItem)
                {
                    _selectedItem = selectedItem.Item1.GetItem();
                    _selectedRarity = GetItemRarity(_selectedItem);
                }

                UpdateDisplaySelectedItemEnchantments();
            }

            bool featureUnlocked = EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.Rune);

            if (!featureUnlocked)
            {
                MainButton.interactable = featureUnlocked;
            }
        }

        protected override void DoMainAction()
        {
            Tuple<InventoryItemListElement, int> selectedItem = AvailableItems.GetSelectedItems<InventoryItemListElement>().FirstOrDefault();

            // Clear any currently existing success dialog
            Cancel();

            if (selectedItem?.Item1.GetItem() == null)
            {
                return;
            }

            Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Rune);
            float costReduction = GetCostReduction(featureValues.Item1);
            float powerModifier = GetPowerModifier(featureValues.Item2);
            ItemDrop.ItemData item = selectedItem.Item1.GetItem();

            if (_runeAction == RuneAction.Extract)
            {
                List<InventoryItemListElement> cost = GetRuneExtractCost(item, _selectedRarity, costReduction);
                ItemDrop.ItemData RuneWithEnchant = ItemToBeRuned(item, _selectedEnchantmentIndex, powerModifier);

                if (RuneWithEnchant == null)
                {
                    return;
                }

                Player player = Player.m_localPlayer;
                if (!player.NoCostCheat())
                {
                    if (!LocalPlayerCanAffordCost(cost))
                    {
                        return;
                    }

                    foreach (InventoryItemListElement costElement in cost)
                    {
                        InventoryManagement.Instance.RemoveItem(costElement.GetItem());
                    }
                }

                bool destroyExtractedItem = ExtractItemsDestroyed();

                if (destroyExtractedItem)
                {
                    // Destroy the item
                    InventoryManagement.Instance.RemoveExactItem(item, 1);
                }

                InventoryManagement.Instance.GiveItem(RuneWithEnchant);
                CostList.SetItems(new List<IListElement>());
            }
            else if (_runeAction == RuneAction.Etch)
            {
                // Modify an existing item and destroy the selected Rune
                ItemDrop.ItemData rune = AvailableRunes.GetSingleSelectedItem<InventoryItemListElement>().Item1.GetItem();
                ItemDrop.ItemData itemToEtch = selectedItem?.Item1.GetItem();

                if (_successDialog != null)
                {
                    Destroy(_successDialog);
                }

                _successDialog = RuneEnchancedItem(itemToEtch, rune, _selectedEnchantmentIndex);
                _successDialog.SetActive(true);
                // Remove the rune from the inventory
                InventoryManagement.Instance.RemoveExactItem(rune, 1);
                CostList.SetItems(new List<IListElement>());
            }

            DeselectAll();

            RefreshAvailableItems();
            _selectedEnchantmentIndex = -1;
            CostList.SetItems(new List<IListElement>());
            AvailableRunes.SetItems(new List<IListElement>());
        }

        protected override AudioClip GetCompleteAudioClip()
        {
            return RunicActionCompleted;
        }

        public void RefreshAvailableItems()
        {
            List<InventoryItemListElement> items;
            if (_runeAction == RuneAction.Extract)
            {
                items = GetRuneExtractItems();
            }
            else if (_runeAction == RuneAction.Etch)
            {
                items = GetRuneEtchItems();
            }
            else
            {
                items = new List<InventoryItemListElement>();
            }

            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            RefreshSelectableEnchantments();
            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
        }

        protected override void OnSelectedItemsChanged()
        {
            Tuple<InventoryItemListElement, int> selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            if (selectedItem?.Item1.GetItem() != null)
            {
                _selectedItem = selectedItem.Item1.GetItem();
                _selectedRarity = GetItemRarity(_selectedItem);
                UpdateDisplaySelectedItemEnchantments();
                _selectedEnchantmentIndex = -1;
            }
            else
            {
                ClearEnchantmentList();
            }
        }

        protected void OnSelectedOverrideRuneChanged()
        {
            Tuple<InventoryItemListElement, int> rune = AvailableRunes.GetSingleSelectedItem<InventoryItemListElement>();
            if (rune?.Item1.GetItem() != null)
            {
                _selectedOverrideRune = rune.Item1.GetItem();
                CheckIfActionDoable();
            }
            else
            {
                _selectedOverrideRune = null;
            }
        }

        private void CheckIfActionDoable()
        {
            bool state = true;

            if (_selectedItem == null || _selectedEnchantmentIndex == -1)
            {
                state = false;
                MainButton.interactable = false;
                return;
            }

            // Check costs, ignored if nocost mode
            Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Rune);
            float costReduction = GetCostReduction(featureValues.Item1);

            if (_runeAction == RuneAction.Etch)
            {
                List<InventoryItemListElement> cost = GetRuneEtchCost(_selectedItem, _selectedRarity, costReduction);
                CostList.SetItems(cost.Cast<IListElement>().ToList());
                state = LocalPlayerCanAffordRuneCost(cost);

                if (_selectedOverrideRune == null)
                {
                    // Etching but does not have an override rune selected
                    state = false;
                }
            }
            else if (_runeAction == RuneAction.Extract)
            {
                List<InventoryItemListElement> cost = GetRuneExtractCost(_selectedItem, _selectedRarity, costReduction);
                CostList.SetItems(cost.Cast<IListElement>().ToList());
                state = LocalPlayerCanAffordRuneCost(cost);
            }

            MainButton.interactable = state;
        }

        internal static float GetCostReduction(float value)
        {
            return value == 0f || value == float.NaN ? 1.0f : 1f - (value / 100f);
        }

        internal static float GetPowerModifier(float value)
        {
            return value == float.NaN ? 1.0f : (value / 100f);
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
        }

        public override void Lock()
        {
            base.Lock();

            RuneExtractButton.interactable = false;
            RuneEtchButton.interactable = false;
            MainButton.interactable = false;
        }

        public override void Unlock()
        {
            base.Unlock();

            RuneExtractButton.interactable = true;
            RuneEtchButton.interactable = true;
        }

        public override void DeselectAll()
        {
            AvailableItems?.DeselectAll();
        }
    }
}
