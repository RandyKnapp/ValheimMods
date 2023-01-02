using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    }

    public class ConvertUI : MonoBehaviour
    {
        public const float CountdownTime = 0.8f;

        public MultiSelectItemList AvailableRecipes;
        public MultiSelectItemList Products;
        public Button MainButton;
        public GuiBar ProgressBar;
        public List<Toggle> ModeButtons;

        [Header("Cost")]
        public Text CostLabel;
        public MultiSelectItemList CostList;

        [Header("Audio")]
        public AudioSource Audio;
        public AudioClip ProgressLoopSFX;
        public AudioClip CompleteSFX;

        public delegate List<ConversionRecipeUnity> GetConversionRecipesDelegate(int mode);

        public static GetConversionRecipesDelegate GetConversionRecipes;

        private bool _inProgress;
        private float _countdown;
        private Text _buttonLabel;
        private Text _progressLabel;
        private ToggleGroup _toggleGroup;
        private MaterialConversionMode _mode;

        public void Awake()
        {
            AvailableRecipes.OnSelectedItemsChanged += OnSelectedRecipesChanged;
            MainButton.onClick.AddListener(OnMainButtonClicked);
            _buttonLabel = MainButton.GetComponentInChildren<Text>();
            _progressLabel = ProgressBar.gameObject.GetComponentInChildren<Text>();

            var uiSFX = GameObject.Find("sfx_gui_button");
            if (uiSFX)
                Audio.outputAudioMixerGroup = uiSFX.GetComponent<AudioSource>().outputAudioMixerGroup;

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

        public void OnEnable()
        {
            _mode = 0;
            RefreshMode();
            var items = GetConversionRecipes((int)_mode);
            AvailableRecipes.SetItems(items.Cast<IListElement>().ToList());
        }

        public void Update()
        {
            ProgressBar.gameObject.SetActive(_inProgress);
            if (_inProgress)
            {
                ProgressBar.SetValue(CountdownTime - _countdown);

                _countdown -= Time.deltaTime;
                if (_countdown < 0)
                {
                    DoConversion();
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

        public void DoConversion()
        {
            var selectedRecipes = AvailableRecipes.GetSelectedItems<ConversionRecipeUnity>();
            var allProducts = GetConversionProducts(selectedRecipes);
            var cost = GetConversionCost(selectedRecipes);

            // Doesn't really cancel, just does all the same stuff
            Cancel();

            Audio.PlayOneShot(CompleteSFX);

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
            AvailableRecipes.SetItems(items.Cast<IListElement>().ToList());
            AvailableRecipes.DeselectAll();
            OnSelectedRecipesChanged();
        }

        public void OnSelectedRecipesChanged()
        {
            var selectedRecipes = AvailableRecipes.GetSelectedItems<ConversionRecipeUnity>();
            var allProducts = GetConversionProducts(selectedRecipes);
            Products.SetItems(allProducts.Cast<IListElement>().ToList());

            var cost = GetConversionCost(selectedRecipes);
            CostList.SetItems(cost.Cast<IListElement>().ToList());

            var canAfford = LocalPlayerCanAffordCost(cost);
            MainButton.interactable = canAfford && selectedRecipes.Count > 0;
        }

        private bool LocalPlayerCanAffordCost(List<InventoryItemListElement> cost)
        {
            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();
            foreach (var element in cost)
            {
                var item = element.GetItem();
                if (inventory.CountItems(item.m_shared.m_name) < item.m_stack)
                    return false;
            }

            return true;
        }

        public bool CanCancel()
        {
            return _inProgress;
        }

        public void Cancel()
        {
            _inProgress = false;
            _countdown = 0;

            Audio.loop = false;
            Audio.Stop();

            UnlockSelector();
            OnModeChanged();
        }

        public void OnMainButtonClicked()
        {
            if (_inProgress)
                Cancel();
            else
                StartCountdown();
        }

        public void StartCountdown()
        {
            _buttonLabel.text = Localization.instance.Localize("$menu_cancel");
            _inProgress = true;
            _countdown = CountdownTime;
            ProgressBar.SetMaxValue(CountdownTime);

            Audio.loop = true;
            Audio.clip = ProgressLoopSFX;
            Audio.Play();

            LockSelector();
        }

        public void LockSelector()
        {
            AvailableRecipes.Lock();
            Products.Lock();
            EnchantingTableUI.instance.LockTabs();
            foreach (var modeButton in ModeButtons)
            {
                modeButton.interactable = false;
            }
        }

        public void UnlockSelector()
        {
            AvailableRecipes.Unlock();
            Products.Unlock();
            EnchantingTableUI.instance.UnlockTabs();
            foreach (var modeButton in ModeButtons)
            {
                modeButton.interactable = true;
            }
        }

        public void DeselectAll()
        {
            AvailableRecipes.DeselectAll();
        }
    }
}
