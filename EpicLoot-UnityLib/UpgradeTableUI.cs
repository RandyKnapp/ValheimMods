using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class UpgradeTableUI : EnchantingTableUIPanelBase
    {
        public Transform ListContainer;
        public Text SelectedFeatureText;
        public Image SelectedFeatureImage;
        public FeatureStatus SelectedFeatureStatus;
        public Text SelectedFeatureInfoText;
        public Text CostLabel;
        public MultiSelectItemList CostList;

        private readonly List<MultiSelectItemListElement> _featureButtons = new List<MultiSelectItemListElement>();

        private int _selectedFeature = -1;

        protected override void OnSelectedItemsChanged() {}

        public override void Awake()
        {
            base.Awake();
            _featureButtons.Clear();
            for (var i = 0; i < ListContainer.childCount; ++ i)
            {
                var child = ListContainer.GetChild(i);
                var button = child.GetComponentInChildren<MultiSelectItemListElement>();
                if (button != null)
                {
                    _featureButtons.Add(button);
                    button.OnSelectionChanged += OnButtonSelected;
                    if (i == 0)
                        button.SelectMaxQuantity(true);
                }
            }

            EnchantingTableUI.instance.SourceTable.OnAnyFeatureLevelChanged += Refresh;
            Refresh();
        }

        private void OnButtonSelected(MultiSelectItemListElement selectedButton, bool selected, int _)
        {
            if (_inProgress)
                return;

            var noneSelected = !_featureButtons.Any(x => x.IsSelected());
            if (noneSelected)
            {
                _selectedFeature = -1;
                Refresh();
                return;
            }

            _selectedFeature = -1;
            for (var index = 0; index < _featureButtons.Count; index++)
            {
                var button = _featureButtons[index];
                if (button == selectedButton)
                {
                    _selectedFeature = index;
                }
                else
                {   button.SuppressEvents = true;
                    button.Deselect(true);
                    button.SuppressEvents = false;
                }
            }

            Refresh();
        }

        public void Refresh()
        {
            for (var index = 0; index < _featureButtons.Count; index++)
            {
                var button = _featureButtons[index];
                var featureIsEnabled = EnchantingTableUI.instance.SourceTable.IsFeatureAvailable((EnchantingFeature)index);
                button.gameObject.SetActive(featureIsEnabled);
            }

            if (_selectedFeature >= 0)
            {
                var selectedButton = _featureButtons[_selectedFeature];

                SelectedFeatureText.enabled = true;
                SelectedFeatureText.text = selectedButton.ItemName.text;
                SelectedFeatureImage.enabled = true;
                SelectedFeatureImage.sprite = selectedButton.ItemIcon.sprite;

                var selectedFeature = (EnchantingFeature)_selectedFeature;
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
                CostList.gameObject.SetActive(false);
                MainButton.interactable = false;
                return;
            }

            var feature = (EnchantingFeature)_selectedFeature;
            CostLabel.enabled = true;
            var maxLevel = EnchantingTableUI.instance.SourceTable.IsFeatureMaxLevel(feature);
            var canAfford = true;
            if (maxLevel)
            {
                CostLabel.text = Localization.instance.Localize("$mod_epicloot_featuremaxlevel");
                CostList.SetItems(new List<IListElement>());
            }
            else
            {
                if (EnchantingTableUI.instance.SourceTable.IsFeatureLocked(feature))
                {
                    var cost = EnchantingTableUI.instance.SourceTable.GetFeatureUnlockCost(feature);
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_unlockcost");
                    CostList.SetItems(cost.Cast<IListElement>().ToList());
                    canAfford = LocalPlayerCanAffordCost(cost);
                    _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_featureunlock");
                }
                else
                {
                    var cost = EnchantingTableUI.instance.SourceTable.GetFeatureUpgradeCost(feature);
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_upgradecost");
                    CostList.SetItems(cost.Cast<IListElement>().ToList());
                    canAfford = LocalPlayerCanAffordCost(cost);
                    _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_upgrade");
                }
            }

            CostList.gameObject.SetActive(!maxLevel && _selectedFeature >= 0);
            MainButton.interactable = !maxLevel && canAfford;
        }

        private string GenerateFeatureInfoText()
        {
            if (_selectedFeature < 0)
                return Localization.instance.Localize("$mod_epicloot_featureinfo_none");

            var sb = new StringBuilder();

            var feature = (EnchantingFeature)_selectedFeature;
            var locked = EnchantingTableUI.instance.SourceTable.IsFeatureLocked(feature);
            var currentLevel = EnchantingTableUI.instance.SourceTable.GetFeatureLevel(feature);
            var maxLevel = EnchantingTableUpgrades.GetFeatureMaxLevel(feature);
            sb.AppendLine(Localization.instance.Localize($"<size=26>{EnchantingTableUpgrades.GetFeatureName(feature)}</size>"));
            sb.AppendLine();
            if (locked)
                sb.AppendLine(Localization.instance.Localize("$mod_epicloot_currentlevel: <color=#AD1616><b>$mod_epicloot_featurelocked</b></color>"));
            else if (currentLevel == 0)
                sb.AppendLine(Localization.instance.Localize($"$mod_epicloot_currentlevel: <color=#1AACEF><b>$mod_epicloot_featureunlocked</b></color> / {maxLevel}"));
            else
                sb.AppendLine(Localization.instance.Localize($"$mod_epicloot_currentlevel: <color=#EAA800><b>{currentLevel}</b></color> / {maxLevel}"));
            sb.AppendLine();

            sb.AppendLine(Localization.instance.Localize(EnchantingTableUpgrades.GetFeatureDescription(feature)));
            sb.AppendLine();
            sb.AppendLine(Localization.instance.Localize("$mod_epicloot_effectsperlevel"));

            for (var i = 1; i <= maxLevel; ++i)
            {
                var text = EnchantingTableUpgrades.GetFeatureUpgradeLevelDescription(EnchantingTableUI.instance.SourceTable, feature, i);
                sb.AppendLine($"<color=gray>{i}:</color> " + (i == currentLevel ? $"<color=#EAA800>{text}</color>" : text));
            }

            return sb.ToString();
        }

        protected override void DoMainAction()
        {
            Cancel();
            if (_selectedFeature < 0)
                return;

            var feature = (EnchantingFeature)_selectedFeature;
            var maxLevel = EnchantingTableUI.instance.SourceTable.IsFeatureMaxLevel(feature);
            if (maxLevel)
                return;

            var cost = EnchantingTableUI.instance.SourceTable.IsFeatureLocked(feature) 
                ? EnchantingTableUI.instance.SourceTable.GetFeatureUnlockCost(feature) 
                : EnchantingTableUI.instance.SourceTable.GetFeatureUpgradeCost(feature);

            var canAfford = LocalPlayerCanAffordCost(cost);
            if (canAfford)
            {
                var currentLevel = EnchantingTableUI.instance.SourceTable.GetFeatureLevel(feature);
                EnchantingTableUI.instance.SourceTable.RequestTableUpgrade(feature, currentLevel +1, (success)=>
                {
                    if (!success)
                    {
                        Debug.LogError($"[Enchanting Upgrade] ERROR: Tried to upgrade ({feature}) to level ({currentLevel + 1}) but it failed!");
                        return;
                    }

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

                    Refresh();
                });
            }
        }

        public override void Lock()
        {
            base.Lock();
            foreach (var button in _featureButtons)
            {
                button.Lock();
            }
        }

        public override void Unlock()
        {
            base.Unlock();
            foreach (var button in _featureButtons)
            {
                button.Unlock();
            }
        }
    }
}
