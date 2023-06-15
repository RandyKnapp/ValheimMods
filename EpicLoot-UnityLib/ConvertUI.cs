using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public enum MaterialConversionMode
    {
        Upgrade,
        Convert,
        Junk
    }

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

        public ItemDrop.ItemData GetItem() => Product;
        public string GetDisplayNameSuffix() => Amount > 1 ? $" x{Amount}" : string.Empty;

        public int GetMax()
        {
            var player = Player.m_localPlayer;
            if (player == null)
                return 0;

            var inventory = player.GetInventory();
            var min = int.MaxValue;
            foreach (var cost in Cost)
            {
                var count = inventory.CountItems(cost.Item.m_shared.m_name);
                var canMake = Mathf.FloorToInt(count / (float)cost.Amount);
                min = Mathf.Min(min, canMake);
            }

            return min;
        }
    }

    public class ConvertUI : EnchantingTableUIPanelBase
    {
        public MultiSelectItemList Products;
        public List<Toggle> ModeButtons;

        [Header("Cost")]
        public Text CostLabel;
        public MultiSelectItemList CostList;

        public delegate List<ConversionRecipeUnity> GetConversionRecipesDelegate(int mode);

        public static GetConversionRecipesDelegate GetConversionRecipes;

        private Text _progressLabel;
        private ToggleGroup _toggleGroup;
        private MaterialConversionMode _mode;

        public override void Awake()
        {
            base.Awake();

            _progressLabel = ProgressBar.gameObject.GetComponentInChildren<Text>();

            if (ModeButtons.Count > 0)
            {
                _toggleGroup = ModeButtons[0].group;
                _toggleGroup.EnsureValidState();
            }

            for (var index = 0; index < ModeButtons.Count; index++)
            {
                var modeButton = ModeButtons[index];
                modeButton.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                        RefreshMode();
                });
            }
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            _mode = 0;
            RefreshMode();
            var items = GetConversionRecipes((int)_mode);
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
        }

        public override void Update()
        {
            base.Update();

            if (!_locked && ZInput.IsGamepadActive())
            {
                if (ZInput.GetButtonDown("JoyButtonY"))
                {
                    var nextModeIndex = ((int)_mode + 1) % ModeButtons.Count;
                    ModeButtons[nextModeIndex].isOn = true;
                    ZInput.ResetButtonStatus("JoyButtonY");
                }
            }
        }

        public void RefreshMode()
        {
            var prevMode = _mode;
            for (var index = 0; index < ModeButtons.Count; index++)
            {
                var button = ModeButtons[index];
                if (button.isOn)
                {
                    _mode = (MaterialConversionMode)index;
                }
            }

            if (prevMode != _mode)
                OnModeChanged();
        }

        public void OnModeChanged()
        {
            DeselectAll();
            RefreshAvailableItems();

            switch (_mode)
            {
                case MaterialConversionMode.Upgrade:
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_upgradecost");
                    _progressLabel.text = Localization.instance.Localize("$mod_epicloot_upgradeprogress");
                    _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_upgrade");
                    break;

                case MaterialConversionMode.Convert:
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_convertcost");
                    _progressLabel.text = Localization.instance.Localize("$mod_epicloot_convertprogress");
                    _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_convert");
                    break;

                case MaterialConversionMode.Junk:
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_junkcost");
                    _progressLabel.text = Localization.instance.Localize("$mod_epicloot_junkprogress");
                    _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_junk");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void DoMainAction()
        {
            var selectedRecipes = AvailableItems.GetSelectedItems<ConversionRecipeUnity>();
            var allProducts = GetConversionProducts(selectedRecipes);
            var cost = GetConversionCost(selectedRecipes);

            Cancel();

            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();
            foreach (var costElement in cost)
            {
                var costItem = costElement.GetItem();
                inventory.RemoveItem(costItem.m_shared.m_name, costItem.m_stack);
            }

            foreach (var productElement in allProducts)
            {
                var product = productElement.GetItem();
                if (inventory.CanAddItem(product))
                {
                    inventory.AddItem(product);
                    player.Message(MessageHud.MessageType.TopLeft, $"$msg_added {product.m_shared.m_name}", product.m_stack, product.GetIcon());
                }
                else
                {
                    var itemDrop = ItemDrop.DropItem(product, product.m_stack, player.transform.position + player.transform.forward + player.transform.up, player.transform.rotation);
                    itemDrop.GetComponent<Rigidbody>().velocity = Vector3.up * 5f;
                    player.Message(MessageHud.MessageType.TopLeft, $"$msg_dropped {itemDrop.m_itemData.m_shared.m_name} $mod_epicloot_sacrifice_inventoryfullexplanation", itemDrop.m_itemData.m_stack, itemDrop.m_itemData.GetIcon());
                }
            }

            DeselectAll();
            RefreshAvailableItems();
        }

        public static List<InventoryItemListElement> GetConversionProducts(List<Tuple<ConversionRecipeUnity, int>> selectedRecipes)
        {
            var products = new Dictionary<string, ItemDrop.ItemData>();

            foreach (var entry in selectedRecipes)
            {
                var recipe = entry.Item1;
                var multiple = entry.Item2;

                if (products.TryGetValue(recipe.Product.m_shared.m_name, out var item))
                {
                    item.m_stack += recipe.Amount * multiple;
                }
                else
                {
                    item = recipe.Product.Clone();
                    item.m_stack = recipe.Amount * multiple;
                    products.Add(item.m_shared.m_name, item);
                }
            }

            return products.Values.OrderBy(x => Localization.instance.Localize(x.m_shared.m_name)).Select(x => new InventoryItemListElement() { Item = x }).ToList();
        }

        public static List<InventoryItemListElement> GetConversionCost(List<Tuple<ConversionRecipeUnity, int>> selectedRecipes)
        {
            var costs = new Dictionary<string, ItemDrop.ItemData>();

            foreach (var entry in selectedRecipes)
            {
                var recipe = entry.Item1;
                var multiple = entry.Item2;

                foreach (var recipeCost in recipe.Cost)
                {
                    if (costs.TryGetValue(recipeCost.Item.m_shared.m_name, out var item))
                    {
                        item.m_stack += recipeCost.Amount * multiple;
                    }
                    else
                    {
                        item = recipeCost.Item.Clone();
                        item.m_stack = recipeCost.Amount * multiple;
                        costs.Add(item.m_shared.m_name, item);
                    }
                }
            }

            return costs.Values.OrderBy(x => Localization.instance.Localize(x.m_shared.m_name)).Select(x => new InventoryItemListElement() { Item = x }).ToList();
        }

        public void RefreshAvailableItems()
        {
            var items = GetConversionRecipes((int)_mode);
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
        }

        protected override void OnSelectedItemsChanged()
        {
            var selectedRecipes = AvailableItems.GetSelectedItems<ConversionRecipeUnity>();
            var allProducts = GetConversionProducts(selectedRecipes);
            Products.SetItems(allProducts.Cast<IListElement>().ToList());

            var cost = GetConversionCost(selectedRecipes);
            CostList.SetItems(cost.Cast<IListElement>().ToList());

            var baseFeatureValues = EnchantingTableUI.instance.SourceTable.GetFeatureValue(EnchantingFeature.ConvertMaterials, 0);
            var currentFeatureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.ConvertMaterials);
            var isBonusCost = false;
            if (_mode == MaterialConversionMode.Upgrade)
            {
                if (currentFeatureValues.Item1 < baseFeatureValues.Item1 && allProducts.Any(x => x.Item.m_shared.m_ammoType.EndsWith("MagicCraftingMaterial")))
                    isBonusCost = true;
                if (currentFeatureValues.Item2 < baseFeatureValues.Item2 && allProducts.Any(x => x.Item.m_shared.m_ammoType.EndsWith("Runestone")))
                    isBonusCost = true;

                if (isBonusCost && cost.Count > 0)
                    CostLabel.text = Localization.instance.Localize("<color=#EAA800>($mod_epicloot_bonus)</color> $mod_epicloot_upgradecost");
                else
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_upgradecost");
            }

            var canAfford = LocalPlayerCanAffordCost(cost);
            var featureUnlocked = EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.ConvertMaterials);
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
    }
}
