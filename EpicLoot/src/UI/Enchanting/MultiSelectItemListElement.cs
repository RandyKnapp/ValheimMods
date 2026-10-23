using System;
using System.Collections.Generic;
using EpicLoot;
using EpicLoot.Config;
using EpicLoot.CraftingV2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class MultiSelectItemListElement : MonoBehaviour
    {
        public const string TotalQuantityFormat = "/ {0}";
        public const string ReadOnlyQuantityFormat = "{0}";
        public const string StorageCountColor = "#9BDB9B";

        public Button MainButton;
        public Toggle SelectedToggle;
        public GameObject SelectedBackground;
        public TMP_Text ItemName;
        public Image MagicBG;
        public Image ItemIcon;
        public TMP_Text ItemTotalQuantity;
        public TMP_InputField ItemSelectedQuantity;
        public Button QuantityUpButton;
        public Button QuantityDownButton;
        public UITooltip Tooltip;
        public UITooltip ItemTooltip;
        public bool ReadOnly;
        public bool CheckPlayerInventory;
        public bool NoMax;
        public bool UseEnchantAsName = false;

        public AudioSource Audio;
        public AudioClip OnClickSFX;
        public GameObject GamepadFocusIndicator;

        [NonSerialized]
        public bool SuppressEvents;

        public event Action<MultiSelectItemListElement, bool, int> OnSelectionChanged;

        private IListElement _item;
        private int _selectedQuantity;
        private bool _locked;
        private bool _hasGamepadFocus;
        private List<string> _enchantmentSearchNames;

        public void Awake()
        {
            if (ItemIcon != null || MagicBG != null)
            {
                Material iconMaterial = InventoryGui.instance.m_dragItemPrefab.transform.Find("icon").GetComponent<Image>().material;
                if (iconMaterial != null)
                {
                    if (ItemIcon != null)
                        ItemIcon.material = iconMaterial;
                    if (MagicBG != null)
                        MagicBG.material = iconMaterial;
                }
            }

            if (Tooltip != null)
            {
                GameObject storeItemTooltip = StoreGui.instance.m_listElement.GetComponent<UITooltip>().m_tooltipPrefab;
                Tooltip.m_tooltipPrefab = storeItemTooltip;
            }
            else
            {
                ItemTooltip = gameObject.AddComponent<UITooltip>();
                ItemTooltip.m_tooltipPrefab = StoreGui.instance.m_listElement.GetComponent<UITooltip>().m_tooltipPrefab;
            }

            EnchantingUIController.SetupUIAudioSource(Audio);

            if (!ReadOnly)
            {
                if (MainButton != null)
                    MainButton.onClick.AddListener(OnClicked);
                if (ItemSelectedQuantity != null)
                    ItemSelectedQuantity.onEndEdit.AddListener(OnSelectedAmountChanged);
                if (SelectedToggle != null)
                    SelectedToggle.onValueChanged.AddListener(OnSelectedToggleChanged);
                if (QuantityUpButton != null)
                    QuantityUpButton.onClick.AddListener(OnQuantityUpButtonClicked);
                if (QuantityDownButton != null)
                    QuantityDownButton.onClick.AddListener(OnQuantityDownButtonClicked);
            }

            Refresh();
        }

        public void Update()
        {
            if (!_locked && ZInput.IsGamepadActive() && HasGamepadFocus() && !ReadOnly)
            {
                if (ZInput.GetButtonDown("JoyButtonA"))
                {
                    OnClicked();
                    ZInput.ResetButtonStatus("JoyButtonA");
                }
                // Deliberately no ResetButtonStatus on these two: it clears the held state ZInput's key
                // repeat runs off, which costs a held d-pad every repeat past the first.
                else if (ZInput.GetButtonDown("JoyDPadUp"))
                {
                    SelectQuantity(_selectedQuantity + 1, false);
                }
                else if (ZInput.GetButtonDown("JoyDPadDown"))
                {
                    SelectQuantity(_selectedQuantity - 1, false);
                }
            }

            RefreshGamepadFocusIndicator();
        }

        private void OnClicked()
        {
            if (IsSelected())
            {
                Deselect(false);
            }
            else
            {
                SelectMaxQuantity(false);
            }
        }

        public void SelectMaxQuantity(bool noSound)
        {
            int maxSelectedAmount = NoMax || _item == null ? 1 : (_item?.GetItem()?.m_stack ?? 0);
            SelectQuantity(maxSelectedAmount, noSound);
        }

        public bool IsSelected()
        {
            return _selectedQuantity > 0;
        }

        public bool IsMaxSelected()
        {
            return _item == null ? _selectedQuantity > 0 : _selectedQuantity >= _item.GetItem().m_stack;
        }

        private void OnSelectedAmountChanged(string typedInAmount)
        {
            bool successParse = int.TryParse(typedInAmount, out int result);
            if (!successParse)
            {
                Deselect(false);
            }
            else
            {
                SelectQuantity(result, false);
            }
        }

        private void OnSelectedToggleChanged(bool _)
        {
            if (SelectedToggle.isOn)
            {
                SelectMaxQuantity(true);
            }
            else
            {
                Deselect(true);
            }
        }

        private void OnQuantityUpButtonClicked()
        {
            SelectQuantity(_selectedQuantity + 1, false);
        }

        private void OnQuantityDownButtonClicked()
        {
            SelectQuantity(_selectedQuantity - 1, false);
        }

        public void SetItem(IListElement item)
        {
            bool sameItem = _item == item;
            _item = item;
            _enchantmentSearchNames = null;

            if (_item?.GetItem() == null)
            {
                if (MagicBG != null)
                    MagicBG.enabled = false;
                if (ItemIcon != null)
                    ItemIcon.sprite = null;
                if (ItemName != null)
                    ItemName.text = "<no item>";

                if (Tooltip != null)
                {
                    Tooltip.m_topic = string.Empty;
                    Tooltip.m_text = string.Empty;
                }
                
                if (ItemTooltip != null)
                {
                    ItemTooltip.Set("", "");
                }
            }
            else
            {
                EnchantingUIController.SetMagicItem(this, _item.GetItem(), Tooltip);
                CheckAndSetNameToEnchantingEffects();

                int stored = GetStorageCount();
                if (Tooltip != null && stored > 0)
                {
                    string storageLine = Localization.instance.Localize("$mod_epicloot_storagecount", stored.ToString());
                    Tooltip.m_text += $"\n<color={StorageCountColor}>{storageLine}</color>";
                }

                if (ItemName != null)
                {
                    ItemName.text += _item.GetDisplayNameSuffix();
                }

                if (ItemTooltip != null)
                {
                    EnchantingUIController.SetItemTooltip(_item.GetItem(), ItemTooltip);
                }
            }

            if (!sameItem)
            {
                Deselect(true);
            }

            // Every time, empty rows included: rows are pooled and would keep the last item's star. Cost and
            // product rows are read-only, and a star on "5 Magic Dust" would only say dust is favorited.
            EpicLoot.Compatibility.ItemFavorites.UpdateStar(ItemIcon, ReadOnly ? null : _item?.GetItem());
            ItemBackgroundHelper.UpdateIconSetMarker(ItemIcon, _item?.GetItem());

            RefreshGamepadFocusIndicator();

            
        }

        private int GetStorageCount()
        {
            if (!CheckPlayerInventory || !ELConfig.ShowStorageCounts.Value || _item?.GetItem() == null)
            {
                return 0;
            }

            return InventoryManagement.Instance.CountProviderItem(_item.GetItem());
        }

        private bool CheckAndSetNameToEnchantingEffects()
        {
            if (UseEnchantAsName && _item.GetEnchantName() != string.Empty)
            {
                ItemName.text = _item.GetEnchantName();
                MagicTextShimmer.Ensure(ItemName);
                ItemName.alignment = TextAlignmentOptions.Left;
                // Adjust the text box container to give it the whole width, which is normally used for quantity
                ItemName.GetComponent<RectTransform>().offsetMax = new Vector2(y: 0f, x: -5f);
                return true;
            }

            return false;
        }

        public void Deselect(bool noSound)
        {
            SelectQuantity(0, noSound);
        }

        public void SelectQuantity(int quantity, bool noSound)
        {
            int prevQuantity = _selectedQuantity;
            if (_item == null)
            {
                _selectedQuantity = Mathf.Clamp(quantity, 0, 1);
            }
            else
            {
                if (NoMax)
                    _selectedQuantity = Mathf.Clamp(quantity, 0, 999);
                else if (_item.GetItem().m_shared.m_maxStackSize == 1)
                    _selectedQuantity = Mathf.Clamp(quantity, 0, 1);
                else
                    _selectedQuantity = Mathf.Clamp(quantity, 0, _item.GetItem().m_stack);
            }

            if (!SuppressEvents && prevQuantity != _selectedQuantity)
            {
                OnSelectionChanged?.Invoke(this, IsSelected(), _selectedQuantity);
            }

            if (Audio != null && !ReadOnly && !noSound && prevQuantity != _selectedQuantity)
            {
                Audio.PlayOneShot(OnClickSFX);
            }

            Refresh();
        }

        public void Refresh()
        {
            RefreshGamepadFocusIndicator();

            bool stackItem = _item != null && _item.GetItem().m_shared.m_maxStackSize > 1;

            if (MainButton != null)
            {
                MainButton.interactable = !_locked;
            }

            if (SelectedToggle != null)
            {
                SelectedToggle.interactable = !_locked;
                SelectedToggle.gameObject.SetActive(!ReadOnly);
                SelectedToggle.SetIsOnWithoutNotify(_selectedQuantity > 0);
            }

            if (ItemSelectedQuantity != null)
            {
                ItemSelectedQuantity.interactable = !_locked;
                ItemSelectedQuantity.gameObject.SetActive(!ReadOnly && stackItem);
                ItemSelectedQuantity.text = _selectedQuantity.ToString();
            }

            if (ItemTotalQuantity != null && _item != null)
            {
                ItemTotalQuantity.gameObject.SetActive(ReadOnly || stackItem);
                string quantityText = string.Format(ReadOnly ? ReadOnlyQuantityFormat : TotalQuantityFormat, _item.GetMax());
                if (CheckPlayerInventory)
                {
                    if (!InventoryManagement.Instance.HasItem(_item.GetItem()))
                    {
                        quantityText = $"<color=red>{quantityText}</color>";
                    }
                }

                int stored = GetStorageCount();
                if (stored > 0)
                {
                    // The cost cell's label wraps and autosizes, so without nobr a long count can drop
                    // to a second line instead of shrinking.
                    quantityText = $"<nobr>{quantityText} <color={StorageCountColor}>+{stored}</color></nobr>";
                }
                ItemTotalQuantity.text = quantityText;
            }

            if (QuantityUpButton != null)
            {
                QuantityUpButton.interactable = !_locked;
                QuantityUpButton.gameObject.SetActive(!ReadOnly && stackItem);
            }

            if (QuantityDownButton != null)
            {
                QuantityDownButton.interactable = !_locked;
                QuantityDownButton.gameObject.SetActive(!ReadOnly && stackItem);
            }

            if (SelectedBackground != null)
            {
                SelectedBackground.SetActive(!ReadOnly && _selectedQuantity > 0);
            }

            Localization.instance.Localize(transform);
        }

        public ItemDrop.ItemData GetItem()
        {
            return _item.GetItem();
        }

        public IListElement GetListElement()
        {
            return _item;
        }

        /// <summary>
        /// The item's enchantments as the search box matches them, socketed ones included so it finds what
        /// the tooltip lists. Built on first use and dropped by SetItem, which every change to the row goes
        /// through (the re-population after an enchant, augment or etch, a sort change), so it is never
        /// staler than the name label.
        /// </summary>
        public IReadOnlyList<string> GetEnchantmentSearchNames()
        {
            if (_enchantmentSearchNames != null)
            {
                return _enchantmentSearchNames;
            }

            _enchantmentSearchNames = new List<string>();
            MagicItem magicItem = _item?.GetItem()?.GetMagicItem();
            if (magicItem == null)
            {
                return _enchantmentSearchNames;
            }

            foreach (MagicItemEffect effect in magicItem.GetEffects(includeSocketed: true))
            {
                if (!MagicItemEffectDefinitions.TryGet(effect.EffectType, out MagicItemEffectDefinition definition) ||
                    string.IsNullOrEmpty(definition.DisplayText))
                {
                    continue;
                }

                string name = ItemSearchFilter.NormalizeEffectName(Localization.instance.Localize(definition.DisplayText));
                if (name.Length > 0)
                {
                    _enchantmentSearchNames.Add(name);
                }
            }

            return _enchantmentSearchNames;
        }

        public int GetSelectedQuantity()
        {
            return _selectedQuantity;
        }

        public void Lock()
        {
            _locked = true;
            Refresh();
        }

        public void Unlock()
        {
            _locked = false;
            Refresh();
        }

        public void GiveFocus(bool focused)
        {
            _hasGamepadFocus = focused;
            RefreshGamepadFocusIndicator();
        }

        private void RefreshGamepadFocusIndicator()
        {
            if (GamepadFocusIndicator == null)
                return;

            GamepadFocusIndicator.SetActive(ZInput.IsGamepadActive() && _hasGamepadFocus);
        }

        public bool HasGamepadFocus()
        {
            return _hasGamepadFocus;
        }
    }
}
