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
        public bool ReadOnly;
        public AudioSource Audio;
        public AudioClip OnClickSFX;

        [NonSerialized]
        public bool SuppressEvents;

        public event Action<MultiSelectItemListElement, bool, int> OnSelectionChanged;

        public delegate void SetMagicItemDelegate(MultiSelectItemListElement element, ItemDrop.ItemData item, UITooltip tooltip);

        public static SetMagicItemDelegate SetMagicItem;

        private ItemDrop.ItemData _item;
        private int _selectedQuantity;
        private bool _locked;

        public void Awake()
        {
            var iconMaterial = InventoryGui.instance.m_dragItemPrefab.transform.Find("icon").GetComponent<Image>().material;
            if (iconMaterial != null)
            {
                ItemIcon.material = iconMaterial;
                MagicBG.material = iconMaterial;
            }

            var storeItemTooltip = StoreGui.instance.m_listElement.GetComponent<UITooltip>().m_tooltipPrefab;
            Tooltip.m_tooltipPrefab = storeItemTooltip;

            var uiSFX = GameObject.Find("sfx_gui_button");
            if (uiSFX != null)
                Audio.outputAudioMixerGroup = uiSFX.GetComponent<AudioSource>().outputAudioMixerGroup;

            if (!ReadOnly)
            {
                MainButton.onClick.AddListener(OnClicked);
                ItemSelectedQuantity.onEndEdit.AddListener(OnSelectedAmountChanged);
                SelectedToggle.onValueChanged.AddListener(OnSelectedToggleChanged);
                QuantityUpButton.onClick.AddListener(OnQuantityUpButtonClicked);
                QuantityDownButton.onClick.AddListener(OnQuantityDownButtonClicked);
            }

            Refresh();
        }

        private void OnClicked()
        {
            if (IsSelected())
                Deselect(false);
            else
                SelectMaxQuantity(false);
        }

        public void SelectMaxQuantity(bool noSound)
        {
            var maxSelectedAmount = _item?.m_stack ?? 0;
            SelectQuantity(maxSelectedAmount, noSound);
        }

        public bool IsSelected()
        {
            return _item != null && _selectedQuantity > 0;
        }

        public bool IsMaxSelected()
        {
            return _item != null && _selectedQuantity >= _item.m_stack;
        }

        private void OnSelectedAmountChanged(string typedInAmount)
        {
            var successParse = int.TryParse(typedInAmount, out var result);
            if (!successParse)
                Deselect(false);
            else
                SelectQuantity(result, false);
        }

        private void OnSelectedToggleChanged(bool _)
        {
            if (SelectedToggle.isOn)
                SelectMaxQuantity(true);
            else
                Deselect(true);
        }

        private void OnQuantityUpButtonClicked()
        {
            SelectQuantity(_selectedQuantity + 1, false);
        }

        private void OnQuantityDownButtonClicked()
        {
            SelectQuantity(_selectedQuantity - 1, false);
        }

        public void SetItem(ItemDrop.ItemData item)
        {
            _item = item;

            if (_item == null)
            {
                MagicBG.enabled = false;
                ItemIcon.sprite = null;
                ItemName.text = "<no item>";

                Tooltip.m_topic = string.Empty;
                Tooltip.m_text = string.Empty;
            }
            else
            {
                if (SetMagicItem != null)
                {
                    SetMagicItem(this, item, Tooltip);
                }
                else
                {
                    MagicBG.enabled = false;
                    ItemIcon.sprite = _item.GetIcon();
                    ItemName.text = Localization.instance.Localize(_item.m_shared.m_name);

                    Tooltip.m_topic = Localization.instance.Localize(_item.m_shared.m_name);
                    Tooltip.m_text = Localization.instance.Localize(_item.GetTooltip());
                }
            }

            Deselect(true);
        }

        public void Deselect(bool noSound)
        {
            SelectQuantity(0, noSound);
        }

        public void SelectQuantity(int quantity, bool noSound)
        {
            if (_item == null)
                return;

            var prevQuantity = _selectedQuantity;
            if (_item.m_shared.m_maxStackSize == 1)
                _selectedQuantity = Mathf.Clamp(quantity, 0, 1);
            else
                _selectedQuantity = Mathf.Clamp(quantity, 0, _item.m_stack);

            if (!SuppressEvents && prevQuantity != _selectedQuantity)
                OnSelectionChanged?.Invoke(this, IsSelected(), _selectedQuantity);

            if (!ReadOnly && !noSound && prevQuantity != _selectedQuantity)
                Audio.PlayOneShot(OnClickSFX);

            Refresh();
        }

        public void Refresh()
        {
            if (_item == null)
                return;

            MainButton.interactable = !_locked;
            SelectedToggle.interactable = !_locked;
            ItemSelectedQuantity.interactable = !_locked;
            QuantityUpButton.interactable = !_locked;
            QuantityDownButton.interactable = !_locked;

            var stackItem = _item.m_shared.m_maxStackSize > 1;
            SelectedToggle.gameObject.SetActive(!ReadOnly);
            SelectedToggle.SetIsOnWithoutNotify(_selectedQuantity > 0);
            SelectedBackground.SetActive(!ReadOnly && _selectedQuantity > 0);
            ItemSelectedQuantity.gameObject.SetActive(!ReadOnly && stackItem);
            ItemSelectedQuantity.text = _selectedQuantity.ToString();
            ItemTotalQuantity.gameObject.SetActive(ReadOnly || stackItem);
            ItemTotalQuantity.text = string.Format(ReadOnly ? ReadOnlyQuantityFormat : TotalQuantityFormat, _item.m_stack);
            QuantityUpButton.gameObject.SetActive(!ReadOnly && stackItem);
            QuantityDownButton.gameObject.SetActive(!ReadOnly && stackItem);

            Localization.instance.Localize(transform);
        }

        public ItemDrop.ItemData GetItem()
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
    }
}
