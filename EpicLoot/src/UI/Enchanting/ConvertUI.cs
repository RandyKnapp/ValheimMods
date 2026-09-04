using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.CraftingV2;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class ConversionRecipeCostUnity
    {
        public ItemDrop.ItemData Item;
        public int Amount;
    }

    public class ConversionRecipeUnity : IListElement
    {
        public ItemDrop.ItemData Product;
        public int Amount;
        public List<ConversionRecipeCostUnity> Cost;

        public List<string> GetEffectNames() => new List<string>();
        public string GetEnchantName() => string.Empty;
        public ItemDrop.ItemData GetItem() => Product;
        public string GetDisplayNameSuffix() => Amount > 1 ? $" x{Amount}" : string.Empty;

        public int GetMax()
        {
            int min = int.MaxValue;
            foreach (ConversionRecipeCostUnity cost in Cost)
            {
                int count = InventoryManagement.Instance.CountItem(cost.Item);
                int canMake = Mathf.FloorToInt(count / (float)cost.Amount);
                min = Mathf.Min(min, canMake);
            }

            return min;
        }
    }

    public class ConvertUI : EnchantingTableUIPanelBase
    {
        public MultiSelectItemList Products;

        // Order is load-bearing: RefreshMode casts a button's index in this list straight to
        // MaterialConversionType, so entry N must be the button for ordinal N. Reordering this in the
        // Inspector silently relabels every mode and lists the wrong recipes under each.
        public List<Toggle> ModeButtons;

        [Header("Cost")]
        public Text CostLabel;
        public MultiSelectItemList CostList;

        private Text _progressLabel;
        private ToggleGroup _toggleGroup;
        private MaterialConversionType _mode;

        public override void Awake()
        {
            base.Awake();

            _progressLabel = ProgressBar.gameObject.GetComponentInChildren<Text>();

            if (ModeButtons.Count > 0)
            {
                _toggleGroup = ModeButtons[0].group;
                _toggleGroup.EnsureValidState();
            }

            for (int index = 0; index < ModeButtons.Count; index++)
            {
                Toggle modeButton = ModeButtons[index];
                modeButton.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        RefreshMode();
                    }
                });
            }
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            _mode = 0;
            RefreshMode();
            List<ConversionRecipeUnity> items = EnchantingUIController.GetConversionRecipes(_mode);
            AvailableItems.ClearFilter();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
        }

        public override void Update()
        {
            base.Update();

            if (!_locked && ZInput.IsGamepadActive())
            {
                if (ZInput.GetButtonDown("JoyButtonY"))
                {
                    int nextModeIndex = ((int)_mode + 1) % ModeButtons.Count;
                    ModeButtons[nextModeIndex].isOn = true;
                    ZInput.ResetButtonStatus("JoyButtonY");
                }
            }
        }

        public void RefreshMode()
        {
            MaterialConversionType prevMode = _mode;
            for (int index = 0; index < ModeButtons.Count; index++)
            {
                Toggle button = ModeButtons[index];
                if (button.isOn)
                {
                    _mode = (MaterialConversionType)index;
                }
            }

            if (prevMode != _mode)
            {
                OnModeChanged();
            }
        }

        public void OnModeChanged()
        {
            DeselectAll();

            // Each mode lists a disjoint set of recipes, so a filter typed against the previous one leaves the
            // new list looking empty rather than filtered. SetItems re-applies the filter but never clears it.
            AvailableItems.ClearFilter();
            RefreshAvailableItems();

            switch (_mode)
            {
                case MaterialConversionType.Upgrade:
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_upgradecost");
                    _progressLabel.text = Localization.instance.Localize("$mod_epicloot_upgradeprogress");
                    if (_useTMP)
                        _tmpButtonLabel.text = Localization.instance.Localize("$mod_epicloot_upgrade");
                    else
                        _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_upgrade");
                    break;

                case MaterialConversionType.Convert:
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_convertcost");
                    _progressLabel.text = Localization.instance.Localize("$mod_epicloot_convertprogress");
                    if (_useTMP)
                        _tmpButtonLabel.text = Localization.instance.Localize("$mod_epicloot_convert");
                    else
                        _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_convert");
                    break;

                case MaterialConversionType.Junk:
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_junkcost");
                    _progressLabel.text = Localization.instance.Localize("$mod_epicloot_junkprogress");
                    if (_useTMP)
                        _tmpButtonLabel.text = Localization.instance.Localize("$mod_epicloot_junk");
                    else
                        _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_junk");
                    break;

                case MaterialConversionType.ShardUpgrade:
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_shardupgradecost");
                    _progressLabel.text = Localization.instance.Localize("$mod_epicloot_shardupgradeprogress");
                    if (_useTMP)
                        _tmpButtonLabel.text = Localization.instance.Localize("$mod_epicloot_shardupgrade");
                    else
                        _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_shardupgrade");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void DoMainAction()
        {
            List<Tuple<ConversionRecipeUnity, int>> selectedRecipes = AvailableItems.GetSelectedItems<ConversionRecipeUnity>();
            List<InventoryItemListElement> allProducts = GetConversionProducts(selectedRecipes);
            List<InventoryItemListElement> cost = GetConversionCost(selectedRecipes);

            Cancel();

            // Re-check affordability at execution time like every sibling panel: the button state
            // is only recomputed on selection change, so materials drained in the meantime used to
            // yield free products.
            if (!Player.m_localPlayer.NoCostCheat() && !LocalPlayerCanAffordCost(cost))
            {
                DeselectAll();
                RefreshAvailableItems();
                return;
            }

            if (!Player.m_localPlayer.NoCostCheat())
            {
                foreach (InventoryItemListElement costElement in cost)
                {
                    InventoryManagement.Instance.RemoveItem(costElement.GetItem());
                }
            }

            foreach (InventoryItemListElement productElement in allProducts)
            {
                InventoryManagement.Instance.GiveItem(productElement.GetItem());
            }

            DeselectAll();
            RefreshAvailableItems();
        }

        public static List<InventoryItemListElement> GetConversionProducts(List<Tuple<ConversionRecipeUnity, int>> selectedRecipes)
        {
            Dictionary<string, ItemDrop.ItemData> products = new Dictionary<string, ItemDrop.ItemData>();

            foreach (Tuple<ConversionRecipeUnity, int> entry in selectedRecipes)
            {
                ConversionRecipeUnity recipe = entry.Item1;
                int multiple = entry.Item2;

                // Each (color, rarity) ShardStone has a distinct display name, so grouping by name keeps
                // different-rarity products in separate stacks.
                string key = recipe.Product.m_shared.m_name;
                if (products.TryGetValue(key, out ItemDrop.ItemData item))
                {
                    item.m_stack += recipe.Amount * multiple;
                }
                else
                {
                    item = recipe.Product.Clone();
                    item.m_stack = recipe.Amount * multiple;
                    products.Add(key, item);
                }
            }

            return products.Values.OrderBy(x => Localization.instance.Localize(x.m_shared.m_name)).Select(x => new InventoryItemListElement() { Item = x }).ToList();
        }

        public static List<InventoryItemListElement> GetConversionCost(List<Tuple<ConversionRecipeUnity, int>> selectedRecipes)
        {
            Dictionary<string, ItemDrop.ItemData> costs = new Dictionary<string, ItemDrop.ItemData>();

            foreach (Tuple<ConversionRecipeUnity, int> entry in selectedRecipes)
            {
                ConversionRecipeUnity recipe = entry.Item1;
                int multiple = entry.Item2;

                foreach (ConversionRecipeCostUnity recipeCost in recipe.Cost)
                {
                    // Each (color, rarity) ShardStone has a distinct display name, so grouping by name keeps
                    // a required shard of one rarity separate from the same-color shard at another rarity.
                    string key = recipeCost.Item.m_shared.m_name;
                    if (costs.TryGetValue(key, out ItemDrop.ItemData item))
                    {
                        item.m_stack += recipeCost.Amount * multiple;
                    }
                    else
                    {
                        item = recipeCost.Item.Clone();
                        item.m_stack = recipeCost.Amount * multiple;
                        costs.Add(key, item);
                    }
                }
            }

            return costs.Values.OrderBy(x => Localization.instance.Localize(x.m_shared.m_name))
                .Select(x => new InventoryItemListElement() { Item = x }).ToList();
        }

        public void RefreshAvailableItems()
        {
            List<ConversionRecipeUnity> items = EnchantingUIController.GetConversionRecipes(_mode);
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
        }

        protected override void OnSelectedItemsChanged()
        {
            List<Tuple<ConversionRecipeUnity, int>> selectedRecipes = AvailableItems.GetSelectedItems<ConversionRecipeUnity>();
            List<InventoryItemListElement> allProducts = GetConversionProducts(selectedRecipes);
            Products.SetItems(allProducts.Cast<IListElement>().ToList());

            List<InventoryItemListElement> cost = GetConversionCost(selectedRecipes);
            CostList.SetItems(cost.Cast<IListElement>().ToList());

            Tuple<float, float> baseFeatureValues = EnchantingTableUI.instance.SourceTable.GetFeatureValue(EnchantingFeature.ConvertMaterials, 0);
            Tuple<float, float> currentFeatureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.ConvertMaterials);
            bool isBonusCost = false;
            if (_mode == MaterialConversionType.Upgrade)
            {
                if (currentFeatureValues.Item1 < baseFeatureValues.Item1 &&
                    allProducts.Any(x => x.Item.m_shared.m_ammoType.EndsWith("MagicCraftingMaterial")))
                {
                    isBonusCost = true;
                }

                if (currentFeatureValues.Item2 < baseFeatureValues.Item2 &&
                    allProducts.Any(x => x.Item.m_shared.m_ammoType.EndsWith("Runestone")))
                {
                    isBonusCost = true;
                }

                if (isBonusCost && cost.Count > 0)
                {
                    CostLabel.text = Localization.instance.Localize("<color=#EAA800>($mod_epicloot_bonus)</color> $mod_epicloot_upgradecost");
                }
                else
                {
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_upgradecost");
                }
            }

            bool canAfford = LocalPlayerCanAffordCost(cost);
            bool featureUnlocked = EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.ConvertMaterials);
            MainButton.interactable = featureUnlocked && canAfford && selectedRecipes.Count > 0;
        }
        
        public override void Cancel()
        {
            base.Cancel();
            OnModeChanged();
        }

        public override void DeselectAll()
        {
            AvailableItems.DeselectAll();
        }

        public override void Lock()
        {
            base.Lock();
            SetModeButtonsInteractable(false);
        }

        public override void Unlock()
        {
            base.Unlock();
            SetModeButtonsInteractable(true);
        }

        // The gamepad mode cycle in Update already refuses to fire while locked, but a mouse click on a toggle
        // does not: it would switch modes mid-countdown, deselect everything, and let the progress bar finish
        // on an empty selection. EnchantUI locks its rarity buttons for the same reason.
        private void SetModeButtonsInteractable(bool interactable)
        {
            foreach (Toggle modeButton in ModeButtons)
            {
                if (modeButton != null)
                {
                    modeButton.interactable = interactable;
                }
            }
        }
    }
}
