using System;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class MultiSelectItemListElement : MonoBehaviour
    {
        public const string TotalQuantityFormat = "/ {0}";
        public const string ReadOnlyQuantityFormat = "{0}";

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

        [NonSerialized]
        public bool SuppressEvents;

        public event Action<MultiSelectItemListElement, bool, int> OnSelectionChanged;

        public delegate void SetMagicItemDelegate(MultiSelectItemListElement element, ItemDrop.ItemData item, UITooltip tooltip);

        public static SetMagicItemDelegate SetMagicItem;

        private Button _button;
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

            _button = GetComponent<Button>();

            if (!ReadOnly)
            {
                _button.onClick.AddListener(OnClicked);
                ItemSelectedQuantity.onEndEdit.AddListener(OnSelectedAmountChanged);
                SelectedToggle.onValueChanged.AddListener(OnSelectedToggleChanged);
                QuantityUpButton.onClick.AddListener(OnQuantityUpButtonClicked);
                QuantityDownButton.onClick.AddListener(OnQuantityDownButtonClicked);
            }

            Refresh();
        }

        private void OnClicked()
        {
            if (_item?.m_shared.m_maxStackSize == 1 && IsSelected())
                Deselect();
            else if (!IsSelected())
                SelectMaxQuantity();
        }

        public void SelectMaxQuantity()
        {
            var maxSelectedAmount = _item?.m_stack ?? 0;
            _selectedQuantity = maxSelectedAmount;
            if (!SuppressEvents)
                OnSelectionChanged?.Invoke(this, IsSelected(), _selectedQuantity);
            Refresh();
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
                Deselect();
            else
                SelectQuantity(result);
        }

        private void OnSelectedToggleChanged(bool _)
        {
            if (SelectedToggle.isOn)
                SelectMaxQuantity();
            else
                Deselect();
        }

        private void OnQuantityUpButtonClicked()
        {
            SelectQuantity(_selectedQuantity + 1);
        }

        private void OnQuantityDownButtonClicked()
        {
            SelectQuantity(_selectedQuantity - 1);
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

            Deselect();
        }

        public void Select()
        {
            if (_item == null)
                return;

            if (IsSelected() && _item.m_shared.m_maxStackSize == 1)
                Deselect();
            else
                SelectQuantity(_item.m_shared.m_maxStackSize);
        }

        public void Deselect()
        {
            _selectedQuantity = 0;
            if (!SuppressEvents)
                OnSelectionChanged?.Invoke(this, IsSelected(), _selectedQuantity);
            Refresh();
        }

        public void SelectQuantity(int quantity)
        {
            if (_item == null)
                return;

            if (_item.m_shared.m_maxStackSize == 1)
                _selectedQuantity = Mathf.Clamp(quantity, 0, 1);
            else
                _selectedQuantity = Mathf.Clamp(quantity, 0, _item.m_stack);

            if (!SuppressEvents)
                OnSelectionChanged?.Invoke(this, IsSelected(), _selectedQuantity);
            Refresh();
        }

        public void Refresh()
        {
            if (_item == null)
                return;

            _button.interactable = !_locked;
            SelectedToggle.interactable = !_locked;
            ItemSelectedQuantity.interactable = !_locked;
            QuantityUpButton.interactable = !_locked;
            QuantityDownButton.interactable = !_locked;

            var stackItem = _item.m_shared.m_maxStackSize > 1;
            SelectedToggle.gameObject.SetActive(!ReadOnly);
            SelectedToggle.isOn = _selectedQuantity > 0;
            SelectedBackground.SetActive(!ReadOnly && _selectedQuantity > 0);
            ItemSelectedQuantity.gameObject.SetActive(!ReadOnly && stackItem);
            ItemSelectedQuantity.text = _selectedQuantity.ToString();
            ItemTotalQuantity.gameObject.SetActive(ReadOnly || stackItem);
            ItemTotalQuantity.text = string.Format(ReadOnly ? ReadOnlyQuantityFormat : TotalQuantityFormat, _item.m_stack);
            QuantityUpButton.gameObject.SetActive(ReadOnly || stackItem);
            QuantityDownButton.gameObject.SetActive(ReadOnly || stackItem);

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
