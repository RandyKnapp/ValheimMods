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
                var featureIsEnabled = EnchantingTableUpgrades.IsFeatureAvailable((EnchantingFeature)index);
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
            var maxLevel = EnchantingTableUpgrades.IsFeatureMaxLevel(feature);
            var canAfford = true;
            if (maxLevel)
            {
                CostLabel.text = Localization.instance.Localize("$mod_epicloot_featuremaxlevel");
                CostList.SetItems(new List<IListElement>());
            }
            else
            {
                if (EnchantingTableUpgrades.IsFeatureLocked(feature))
                {
                    var cost = EnchantingTableUpgrades.GetUnlockCost(feature);
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_unlockcost");
                    CostList.SetItems(cost.Cast<IListElement>().ToList());
                    canAfford = LocalPlayerCanAffordCost(cost);
                    _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_featureunlock");
                }
                else
                {
                    var cost = EnchantingTableUpgrades.GetUpgradeCost(feature);
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

            var featureNames = new []
            {
                "$mod_epicloot_sacrifice",
                "$mod_epicloot_convertmaterials",
                "$mod_epicloot_enchant",
                "$mod_epicloot_augment",
                "$mod_epicloot_disenchant",
                "$mod_epicloot_helheim",
            };

            var feature = (EnchantingFeature)_selectedFeature;
            var locked = EnchantingTableUpgrades.IsFeatureLocked(feature);
            var currentLevel = EnchantingTableUpgrades.GetFeatureLevel(feature);
            var maxLevel = EnchantingTableUpgrades.GetFeatureMaxLevel(feature);
            sb.AppendLine(Localization.instance.Localize($"<size=26>{featureNames[_selectedFeature]}</size>"));
            sb.AppendLine();
            if (locked)
                sb.AppendLine(Localization.instance.Localize("$mod_epicloot_currentlevel: <color=#AD1616><b>$mod_epicloot_featurelocked</b></color>"));
            else if (currentLevel == 0)
                sb.AppendLine(Localization.instance.Localize($"$mod_epicloot_currentlevel: <color=gray><b>$mod_epicloot_featureunlocked</b></color> / {maxLevel}"));
            else
                sb.AppendLine(Localization.instance.Localize($"$mod_epicloot_currentlevel: <color=#EAA800><b>{currentLevel}</b></color> / {maxLevel}"));
            sb.AppendLine();

            var featureDescriptions = new []
            {
                "$mod_epicloot_featureinfo_sacrifice",
                "$mod_epicloot_featureinfo_convertmaterials",
                "$mod_epicloot_featureinfo_enchant",
                "$mod_epicloot_featureinfo_augment",
                "$mod_epicloot_featureinfo_disenchant",
                "$mod_epicloot_featureinfo_helheim",
            };
            sb.AppendLine(Localization.instance.Localize(featureDescriptions[_selectedFeature]));
            sb.AppendLine();
            sb.AppendLine(Localization.instance.Localize("$mod_epicloot_effectsperlevel"));

            var featureUpgradeDescriptions = new []
            {
                "$mod_epicloot_featureupgrade_sacrifice",
                "$mod_epicloot_featureupgrade_convertmaterials",
                "$mod_epicloot_featureupgrade_enchant",
                "$mod_epicloot_featureupgrade_augment",
                "$mod_epicloot_featureupgrade_disenchant",
                "$mod_epicloot_featureupgrade_helheim",
            };
            for (var i = 1; i <= maxLevel; ++i)
            {
                var values = EnchantingTableUpgrades.GetFeatureValue(feature, i);
                var text = Localization.instance.Localize(featureUpgradeDescriptions[_selectedFeature], values.Item1.ToString("0.#"), values.Item2.ToString("0.#"));
                sb.AppendLine($"<color=gray>{i}:</color> " + (i == currentLevel ? $"<color=#EAA800>{text}</color>" : text));
            }

            return sb.ToString();
        }

        protected override void DoMainAction()
        {
            Cancel();
            if (_selectedFeature < 0)
                return;
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
