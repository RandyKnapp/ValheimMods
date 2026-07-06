using System;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class MultiSelectItemListElement : MonoBehaviour
    {
        public const string TotalQuantityFormat = "/ {0}";
        public const string ReadOnlyQuantityFormat = "{0}";

        public Button MainButton;
        public Toggle SelectedToggle;
        public GameObject SelectedBackground;
        public Text ItemName;
        public Image MagicBG;
        public Image ItemIcon;
        public Text ItemTotalQuantity;
        public InputField ItemSelectedQuantity;
        public Button QuantityUpButton;
        public Button QuantityDownButton;
        public UITooltip Tooltip;
        public UITooltip ItemTooltip;
        public bool ReadOnly;
        public bool CheckPlayerInventory;
        public bool NoMax;
        public bool UseEnchantAsName = false;

        public delegate float AudioVolumeLevelDelegate();
        public static AudioVolumeLevelDelegate AudioVolumeLevel;

        public AudioSource Audio;
        public AudioClip OnClickSFX;
        public GameObject GamepadFocusIndicator;

        [NonSerialized]
        public bool SuppressEvents;

        public event Action<MultiSelectItemListElement, bool, int> OnSelectionChanged;

        public delegate void SetMagicItemDelegate(MultiSelectItemListElement element, ItemDrop.ItemData item, UITooltip tooltip);

        public static SetMagicItemDelegate SetMagicItem;
        
        public delegate void SetItemTooltipDelegate(ItemDrop.ItemData item, UITooltip tooltip);
        
        public static SetItemTooltipDelegate SetItemTooltip;

        private IListElement _item;
        private int _selectedQuantity;
        private bool _locked;
        private bool _hasGamepadFocus;

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

            if (Audio != null)
            {
                GameObject uiSFX = GameObject.Find("sfx_gui_button");
                if (uiSFX != null)
                {
                    Audio.outputAudioMixerGroup = uiSFX.GetComponent<AudioSource>().outputAudioMixerGroup;
                }

                Audio.volume = AudioVolumeLevel();
            }

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
                else if (ZInput.GetButtonDown("JoyDPadUp"))
                {
                    SelectQuantity(_selectedQuantity + 1, false);
                    ZInput.ResetButtonStatus("JoyDPadUp");
                }
                else if (ZInput.GetButtonDown("JoyDPadDown"))
                {
                    SelectQuantity(_selectedQuantity - 1, false);
                    ZInput.ResetButtonStatus("JoyDPadDown");
                }
                else if (ZInput.GetButtonDown("JoyDPadLeft"))
                {
                    ZInput.ResetButtonStatus("JoyDPadLeft");
                }
                else if (ZInput.GetButtonDown("JoyDPadRight"))
                {
                    ZInput.ResetButtonStatus("JoyDPadRight");
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
                if (SetMagicItem != null)
                {
                    SetMagicItem(this, _item.GetItem(), Tooltip);
                    CheckAndSetNameToEnchantingEffects();
                }
                else
                {
                    if (MagicBG != null)
                        MagicBG.enabled = false;
                    if (ItemIcon != null)
                        ItemIcon.sprite = _item.GetItem().GetIcon();
                    if (ItemName != null)
                    {
                        if (CheckAndSetNameToEnchantingEffects() == false)
                        {
                            ItemName.text = Localization.instance.Localize(_item.GetItem().m_shared.m_name);
                        }
                    }

                    if (Tooltip != null)
                    {
                        Tooltip.m_topic = Localization.instance.Localize(_item.GetItem().m_shared.m_name);
                        Tooltip.m_text = Localization.instance.Localize(_item.GetItem().GetTooltip());
                    }
                }

                if (ItemName != null)
                {
                    ItemName.text += _item.GetDisplayNameSuffix();
                }

                if (ItemTooltip != null)
                {
                    if (SetItemTooltip != null)
                    {
                        SetItemTooltip(_item.GetItem(), ItemTooltip);
                    }
                }
            }

            if (!sameItem)
            {
                Deselect(true);
            }

            RefreshGamepadFocusIndicator();

            
        }

        private bool CheckAndSetNameToEnchantingEffects()
        {
            if (UseEnchantAsName && _item.GetEnchantName() != string.Empty)
            {
                ItemName.text = _item.GetEnchantName();
                ItemName.alignment = TextAnchor.MiddleLeft;
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
                _selectedQuantity = quantity;
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
                Audio.PlayOneShot(OnClickSFX, AudioVolumeLevel());
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
