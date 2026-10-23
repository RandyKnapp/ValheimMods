using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class UpgradeTableUI : EnchantingTableUIPanelBase
    {
        public Transform ListContainer;
        public TMP_Text SelectedFeatureText;
        public Image SelectedFeatureImage;
        public FeatureStatus SelectedFeatureStatus;
        public TMP_Text SelectedFeatureInfoText;
        public TMP_Text CostLabel;
        public MultiSelectItemList CostList;

        private readonly List<MultiSelectItemListElement> _featureButtons = new List<MultiSelectItemListElement>();

        private int _selectedFeature = -1;
        private bool _featuresFocused = true;

        private class FeatureColumn : IGamepadFocusPane
        {
            private readonly UpgradeTableUI _owner;

            public FeatureColumn(UpgradeTableUI owner)
            {
                _owner = owner;
            }

            public int GetItemCount() => _owner.GetAvailableButtons().Count;
            public int GetFocusedIndex() => _owner.GetAvailableButtons().FindIndex(x => x.HasGamepadFocus());
            public bool IsGrid() => false;
            public bool ShowSortHint => false;
            public bool ShowSelectAllHint => false;
            public bool ShowSelectHint => GetFocusedIndex() >= 0;

            public void GiveFocus(bool focused, int tryFocusIndex)
            {
                _owner.SetFeaturesFocused(focused);
            }
        }

        protected override void OnSelectedItemsChanged() {}

        public override void Awake()
        {
            base.Awake();
            _featureButtons.Clear();

            foreach(MultiSelectItemListElement child in ListContainer.GetComponentsInChildren<MultiSelectItemListElement>(true))
            {
                _featureButtons.Add(child);
                child.OnSelectionChanged += OnButtonSelected;
                child.SelectMaxQuantity(true);
            }

            MultiSelectListFocusController focusController = GetComponent<MultiSelectListFocusController>();
            if (focusController != null)
            {
                focusController.SetPanes(new IGamepadFocusPane[] { new FeatureColumn(this), CostList });
            }
        }

        public override void Update()
        {
            base.Update();

            if (_locked || !_featuresFocused || !ZInput.IsGamepadActive())
            {
                return;
            }

            // Deliberately no ZInput.ResetButtonStatus on the navigation buttons: a reset clears the held
            // state ZInput's own key repeat runs off, which costs a held stick every repeat past the first.
            int step;
            if (ZInput.GetButtonDown("JoyLStickUp"))
            {
                step = -1;
            }
            else if (ZInput.GetButtonDown("JoyLStickDown"))
            {
                step = 1;
            }
            else
            {
                return;
            }

            List<MultiSelectItemListElement> available = GetAvailableButtons();
            if (available.Count == 0)
            {
                return;
            }

            int currentIndex = available.FindIndex(x => x.HasGamepadFocus());
            int newIndex = currentIndex < 0 ? 0 : Mathf.Clamp(currentIndex + step, 0, available.Count - 1);
            if (newIndex == currentIndex)
            {
                return;
            }

            FocusButton(available[newIndex]);
        }

        public void OnEnable()
        {
            if (EnchantingTableUI.instance.SourceTable != null)
            {
                EnchantingTableUI.instance.SourceTable.OnAnyFeatureLevelChanged -= Refresh;
                EnchantingTableUI.instance.SourceTable.OnAnyFeatureLevelChanged += Refresh;
            }

            Refresh();
        }

        private void OnButtonSelected(MultiSelectItemListElement selectedButton, bool selected, int _)
        {
            if (_inProgress)
            {
                return;
            }

            bool noneSelected = !_featureButtons.Any(x => x.IsSelected());
            _selectedFeature = -1;

            if (!noneSelected)
            {
                for (int index = 0; index < _featureButtons.Count; index++)
                {
                    MultiSelectItemListElement button = _featureButtons[index];
                    if (button == selectedButton)
                    {
                        _selectedFeature = index;
                    }
                    else
                    {
                        button.SuppressEvents = true;
                        button.Deselect(true);
                        button.SuppressEvents = false;
                    }
                }
            }

            Refresh();
        }

        public void Refresh()
        {
            if (EnchantingTableUI.instance.SourceTable == null)
            {
                return;
            }

            for (int index = 0; index < _featureButtons.Count; index++)
            {
                MultiSelectItemListElement button = _featureButtons[index];
                bool featureIsEnabled = EnchantingTableUI.instance.SourceTable.IsFeatureAvailable((EnchantingFeature)index);
                button.gameObject.SetActive(featureIsEnabled);
            }

            // Awake selects every button in turn and ends on the last one, which the config may have hidden.
            if (_selectedFeature >= 0 && !_featureButtons[_selectedFeature].gameObject.activeSelf)
            {
                List<MultiSelectItemListElement> available = GetAvailableButtons();
                MultiSelectItemListElement fallback = available.Count > 0 ? available[available.Count - 1] : null;
                _selectedFeature = fallback != null ? _featureButtons.IndexOf(fallback) : -1;
                foreach (MultiSelectItemListElement button in _featureButtons)
                {
                    button.SuppressEvents = true;
                    if (button == fallback)
                    {
                        button.SelectMaxQuantity(true);
                    }
                    else
                    {
                        button.Deselect(true);
                    }

                    button.SuppressEvents = false;
                }
            }

            RefreshGamepadFocus();

            if (_selectedFeature >= 0)
            {
                MultiSelectItemListElement selectedButton = _featureButtons[_selectedFeature];

                SelectedFeatureText.enabled = true;
                SelectedFeatureText.text = selectedButton.ItemName.text;
                SelectedFeatureImage.enabled = true;
                SelectedFeatureImage.sprite = selectedButton.ItemIcon.sprite;

                EnchantingFeature selectedFeature = (EnchantingFeature)_selectedFeature;
                SelectedFeatureStatus.gameObject.SetActive(true);
                SelectedFeatureStatus.SetFeature(selectedFeature);
            }
            else
            {
                SelectedFeatureText.enabled = false;
                SelectedFeatureImage.enabled = false;
                SelectedFeatureStatus.gameObject.SetActive(false);
            }

            SelectedFeatureInfoText.text = GenerateFeatureInfoText();

            if (_selectedFeature < 0)
            {
                CostLabel.enabled = false;
                CostList.SetItems(new List<IListElement>());
                CostList.gameObject.SetActive(false);
                MainButton.interactable = false;
                return;
            }

            EnchantingFeature feature = (EnchantingFeature)_selectedFeature;
            CostLabel.enabled = true;
            bool maxLevel = EnchantingTableUI.instance.SourceTable.IsFeatureMaxLevel(feature);
            bool canAfford = true;

            if (maxLevel)
            {
                CostLabel.text = Localization.instance.Localize("$mod_epicloot_featuremaxlevel");
                CostList.SetItems(new List<IListElement>());
            }
            else
            {
                if (EnchantingTableUI.instance.SourceTable.IsFeatureLocked(feature))
                {
                    List<InventoryItemListElement> cost = EnchantingTableUI.instance.SourceTable.GetFeatureUnlockCost(feature);
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_unlockcost");
                    CostList.SetItems(cost.Cast<IListElement>().ToList());
                    canAfford = LocalPlayerCanAffordCost(cost);
                    string buttonText = Localization.instance.Localize("$mod_epicloot_featureunlock");
                    _buttonLabel.text = buttonText;
                }
                else
                {
                    List<InventoryItemListElement> cost = EnchantingTableUI.instance.SourceTable.GetFeatureUpgradeCost(feature);
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_upgradecost");
                    CostList.SetItems(cost.Cast<IListElement>().ToList());
                    canAfford = LocalPlayerCanAffordCost(cost);
                    string buttonText = Localization.instance.Localize("$mod_epicloot_upgrade");
                    _buttonLabel.text = buttonText;
                }
            }

            CostList.gameObject.SetActive(!maxLevel && _selectedFeature >= 0);
            MainButton.interactable = !maxLevel && canAfford;
        }

        private List<MultiSelectItemListElement> GetAvailableButtons()
        {
            return _featureButtons.Where(x => x.gameObject.activeSelf).ToList();
        }

        private void SetFeaturesFocused(bool focused)
        {
            _featuresFocused = focused;
            if (focused)
            {
                RefreshGamepadFocus();
            }
            else
            {
                FocusButton(null);
            }
        }

        private void RefreshGamepadFocus()
        {
            if (!_featuresFocused)
            {
                return;
            }

            List<MultiSelectItemListElement> available = GetAvailableButtons();
            if (available.Count == 0 || available.Any(x => x.HasGamepadFocus()))
            {
                return;
            }

            MultiSelectItemListElement selectedButton = _selectedFeature >= 0 ? _featureButtons[_selectedFeature] : null;
            FocusButton(selectedButton != null && available.Contains(selectedButton) ? selectedButton : available[0]);
        }

        private void FocusButton(MultiSelectItemListElement button)
        {
            foreach (MultiSelectItemListElement featureButton in _featureButtons)
            {
                featureButton.GiveFocus(featureButton == button);
            }
        }

        private string GenerateFeatureInfoText()
        {
            if (_selectedFeature < 0)
            {
                return Localization.instance.Localize("$mod_epicloot_featureinfo_none");
            }

            StringBuilder sb = new StringBuilder();

            EnchantingFeature feature = (EnchantingFeature)_selectedFeature;
            bool locked = EnchantingTableUI.instance.SourceTable.IsFeatureLocked(feature);
            int currentLevel = EnchantingTableUI.instance.SourceTable.GetFeatureLevel(feature);
            int maxLevel = EnchantingTableUpgrades.GetFeatureMaxLevel(feature);
            sb.AppendLine(Localization.instance.Localize($"<size=26>{EnchantingTableUpgrades.GetFeatureName(feature)}</size>"));
            sb.AppendLine();

            if (locked)
            {
                sb.AppendLine(Localization.instance.Localize(
                    "$mod_epicloot_currentlevel: <color=#AD1616><b>$mod_epicloot_featurelocked</b></color>"));
            }
            else if (currentLevel == 0)
            {
                sb.AppendLine(Localization.instance.Localize(
                    $"$mod_epicloot_currentlevel: <color=#1AACEF><b>$mod_epicloot_featureunlocked</b></color> / {maxLevel}"));
            }
            else
            {
                sb.AppendLine(Localization.instance.Localize(
                    $"$mod_epicloot_currentlevel: <color=#EAA800><b>{currentLevel}</b></color> / {maxLevel}"));
            }

            sb.AppendLine();

            sb.AppendLine(Localization.instance.Localize(EnchantingTableUpgrades.GetFeatureDescription(feature)));
            sb.AppendLine();
            sb.AppendLine(Localization.instance.Localize("$mod_epicloot_effectsperlevel"));

            for (int i = 1; i <= maxLevel; ++i)
            {
                string text = EnchantingTableUpgrades.GetFeatureUpgradeLevelDescription(EnchantingTableUI.instance.SourceTable, feature, i);
                sb.AppendLine($"<color=#808080ff>{i}:</color> " + (i == currentLevel ? $"<color=#EAA800>{text}</color>" : text));
            }

            return sb.ToString();
        }

        protected override void DoMainAction()
        {
            Cancel();
            if (_selectedFeature < 0)
            {
                return;
            }

            EnchantingFeature feature = (EnchantingFeature)_selectedFeature;
            bool maxLevel = EnchantingTableUI.instance.SourceTable.IsFeatureMaxLevel(feature);
            if (maxLevel)
            {
                return;
            }

            List<InventoryItemListElement> cost = EnchantingTableUI.instance.SourceTable.IsFeatureLocked(feature)
                ? EnchantingTableUI.instance.SourceTable.GetFeatureUnlockCost(feature)
                : EnchantingTableUI.instance.SourceTable.GetFeatureUpgradeCost(feature);

            Player player = Player.m_localPlayer;
            List<ItemDrop.ItemData> paid = new List<ItemDrop.ItemData>();
            if (!LocalPlayerCanAffordCost(cost) || (!player.NoCostCheat() && !TryPayCost(cost, paid)))
            {
                Refund(paid);
                player.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
                Refresh();
                return;
            }

            int currentLevel = EnchantingTableUI.instance.SourceTable.GetFeatureLevel(feature);
            EnchantingTableUI.instance.SourceTable.RequestTableUpgrade(feature, currentLevel +1, (success)=>
            {
                if (!success)
                {
                    Debug.LogError($"[Enchanting Upgrade] ERROR: " +
                        $"Tried to upgrade ({feature}) to level ({currentLevel + 1}) but it failed!");
                    Refund(paid);
                }

                Refresh();
            });
        }

        private static bool TryPayCost(List<InventoryItemListElement> cost, List<ItemDrop.ItemData> paid)
        {
            foreach (InventoryItemListElement costElement in cost)
            {
                ItemDrop.ItemData costItem = costElement.GetItem();
                int taken = InventoryManagement.Instance.RemoveItem(costItem);
                if (taken > 0)
                {
                    ItemDrop.ItemData refund = costItem.Clone();
                    refund.m_stack = taken;
                    paid.Add(refund);
                }

                if (taken < costItem.m_stack)
                {
                    return false;
                }
            }

            return true;
        }

        private static void Refund(List<ItemDrop.ItemData> paid)
        {
            foreach (ItemDrop.ItemData item in paid)
            {
                InventoryManagement.Instance.GiveItem(item);
            }

            paid.Clear();
        }

        public override void Lock()
        {
            base.Lock();
            foreach (MultiSelectItemListElement button in _featureButtons)
            {
                button.Lock();
            }
        }

        public override void Unlock()
        {
            base.Unlock();
            foreach (MultiSelectItemListElement button in _featureButtons)
            {
                button.Unlock();
            }
        }
    }
}
