using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class MultiSelectItemList : MonoBehaviour
    {
        public enum SortMode { Rarity, Name, Quantity }

        public bool Multiselect = true;
        public bool Filterable = true;
        public bool Sortable = true;
        public bool ReadOnly = false;
        public Transform ListContainer;
        public MultiSelectItemListElement ElementPrefab;
        public Dropdown SortByDropdown;
        public InputField FilterByText;
        public Toggle SelectAllToggle;

        public event Action OnSelectedItemsChanged;

        public delegate List<ItemDrop.ItemData> SortByRarityDelegate(List<ItemDrop.ItemData> items);
        public delegate List<ItemDrop.ItemData> SortByNameDelegate(List<ItemDrop.ItemData> items);

        public static SortByRarityDelegate SortByRarity;
        public static SortByNameDelegate SortByName;

        public void Awake()
        {
            if (SelectAllToggle != null)
                SelectAllToggle.onValueChanged.AddListener(OnSelectAllToggled);
            
            if (SortByDropdown != null)
            {
                foreach (var optionData in SortByDropdown.options)
                {
                    optionData.text = Localization.instance.Localize(optionData.text);
                }

                SortByDropdown.onValueChanged.AddListener(OnSortModeChanged);
            }

            if (FilterByText != null)
            {
                FilterByText.onValueChanged.AddListener(OnFilterChanged);
            }

            Refresh();
        }

        private void OnFilterChanged(string _)
        {
            Refresh();
        }

        public void Refresh()
        {
            RefreshFilter();
            RefreshSelectAllToggle();
        }

        private void RefreshFilter()
        {
            if (FilterByText != null && !Filterable && FilterByText.gameObject.activeSelf)
            {
                FilterByText.gameObject.SetActive(false);
            }

            if (!Filterable || FilterByText == null)
                return;

            var filterText = FilterByText.text;
            var filterIsEmpty = string.IsNullOrEmpty(filterText) || string.IsNullOrWhiteSpace(filterText);

            var filterParts = filterIsEmpty ? Array.Empty<string>() : filterText.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries);
            var elementCount = ListContainer.childCount;
            for (var i = 0; i < elementCount; ++i)
            {
                var childToCache = ListContainer.GetChild(i);
                var element = childToCache.GetComponent<MultiSelectItemListElement>();

                // Strip rich text tags from item name
                var itemName = element.ItemName.text;
                var richTextRegex = new Regex(@"<[^>]*>");
                itemName = richTextRegex.Replace(itemName, string.Empty);

                var nameMatches = filterIsEmpty;
                foreach (var part in filterParts)
                {
                    if (itemName.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        nameMatches = true;
                        break;
                    }
                }

                element.gameObject.SetActive(nameMatches);
            }
        }

        public void RefreshSelectAllToggle()
        {
            if (SelectAllToggle != null)
            {
                if (!Multiselect && SelectAllToggle.gameObject.activeSelf)
                {
                    SelectAllToggle.gameObject.SetActive(false);
                    return;
                }

                var allAreSelected = true;
                var elementCount = ListContainer.childCount;
                for (var i = 0; i < elementCount; ++i)
                {
                    var childToCache = ListContainer.GetChild(i);
                    var element = childToCache.GetComponent<MultiSelectItemListElement>();
                    if (!element.IsMaxSelected())
                        allAreSelected = false;
                }

                SelectAllToggle.isOn = allAreSelected;
            }
        }

        private void OnSelectAllToggled(bool selectAll)
        {
            if (SelectAllToggle == null)
                return;

            if (SelectAllToggle.isOn)
                ForeachElement((_, x) => x.SelectMaxQuantity(true));
            else
                ForeachElement((_, x) => x.Deselect(true));
        }

        private void OnSortModeChanged(int sortModeValue)
        {
            if (!Sortable || SortByDropdown == null)
                return;

            var previousSelectionAmounts = GetCurrentSelectionAmounts();

            var items = previousSelectionAmounts.Keys.ToList();
            var sortMode = (SortMode)SortByDropdown.value;
            var sortedItems = SortItems(sortMode, items);

            for (var i = 0; i < sortedItems.Count; ++i)
            {
                var childToSet = ListContainer.GetChild(i);
                var itemToSet = sortedItems[i];
                var element = childToSet.GetComponent<MultiSelectItemListElement>();
                element.SetItem(itemToSet);
                if (previousSelectionAmounts.TryGetValue(itemToSet, out var previousQuantity))
                    element.SelectQuantity(previousQuantity, true);
            }
        }

        public Dictionary<ItemDrop.ItemData, int> GetCurrentSelectionAmounts()
        {
            var selectionAmounts = new Dictionary<ItemDrop.ItemData, int>();
            var elementCount = ListContainer.childCount;
            for (var i = 0; i < elementCount; ++i)
            {
                var childToCache = ListContainer.GetChild(i);
                var element = childToCache.GetComponent<MultiSelectItemListElement>();
                if (element != null && element.GetItem() != null)
                    selectionAmounts.Add(element.GetItem(), element.GetSelectedQuantity());
            }

            return selectionAmounts;
        }

        public void SetItems(List<ItemDrop.ItemData> items)
        {
            var elementCount = ListContainer.childCount;
            var itemCount = items.Count;

            var previousSelectionAmounts = GetCurrentSelectionAmounts();

            if (elementCount > itemCount)
            {
                for (var i = elementCount - 1; i >= itemCount; --i)
                {
                    var childToDestroy = ListContainer.GetChild(i);
                    var element = childToDestroy.GetComponent<MultiSelectItemListElement>();
                    element.OnSelectionChanged -= OnElementSelectionChanged;
                    Destroy(childToDestroy.gameObject);
                }
            }
            else if (elementCount < itemCount)
            {
                for (var i = elementCount; i < itemCount; ++i)
                {
                    var newElement = Instantiate(ElementPrefab, ListContainer);
                    newElement.SuppressEvents = true;
                    newElement.OnSelectionChanged += OnElementSelectionChanged;
                }
            }

            var sortedItems = items;
            if (Sortable && SortByDropdown != null)
            {
                var sortMode = (SortMode)SortByDropdown.value;
                sortedItems = SortItems(sortMode, items);
            }

            for (var i = 0; i < itemCount; ++i)
            {
                var childToSet = ListContainer.GetChild(i);
                var itemToSet = sortedItems[i];
                var element = childToSet.GetComponent<MultiSelectItemListElement>();
                element.SuppressEvents = true;
                element.SetItem(itemToSet);
                if (previousSelectionAmounts.TryGetValue(itemToSet, out var previousQuantity))
                    element.SelectQuantity(previousQuantity, true);
                element.SuppressEvents = false;
            }

            OnSelectedItemsChanged?.Invoke();
        }

        private void OnElementSelectionChanged(MultiSelectItemListElement element, bool isSelected, int selectedQuantity)
        {
            OnSelectedItemsChanged?.Invoke();
        }

        public List<ItemDrop.ItemData> SortItems(SortMode mode, List<ItemDrop.ItemData> items)
        {
            switch (mode)
            {
                case SortMode.Rarity:
                    if (SortByRarity != null)
                        return SortByRarity(items);
                    break;

                case SortMode.Name:
                    if (SortByName != null)
                        return SortByName(items);
                    else
                        return items.OrderBy(x => Localization.instance.Localize(x.m_shared.m_name)).ThenByDescending(x => x.m_stack).ToList();

                case SortMode.Quantity: 
                    return items.OrderByDescending(x => x.m_stack).ThenBy(x => Localization.instance.Localize(x.m_shared.m_name)).ToList();

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            return items.ToList();
        }

        public List<Tuple<ItemDrop.ItemData, int>> GetSelectedItems()
        {
            var result = new List<Tuple<ItemDrop.ItemData, int>>();
            var elementCount = ListContainer.childCount;
            for (var i = 0; i < elementCount; ++i)
            {
                var childToCache = ListContainer.GetChild(i);
                var element = childToCache.GetComponent<MultiSelectItemListElement>();
                var quantity = element.GetSelectedQuantity();
                if (quantity > 0)
                    result.Add(new Tuple<ItemDrop.ItemData, int>(element.GetItem(), quantity));
            }

            return result;
        }

        public void Lock()
        {
            if (SortByDropdown != null)
                SortByDropdown.interactable = false;
            if (FilterByText != null)
                FilterByText.interactable = false;
            if (SelectAllToggle != null)
                SelectAllToggle.interactable = false;
            ForeachElement((_, e) => e.Lock());
        }

        public void Unlock()
        {
            if (SortByDropdown != null)
                SortByDropdown.interactable = Sortable && !ReadOnly;
            if (FilterByText != null)
                FilterByText.interactable = Filterable && !ReadOnly;
            if (SelectAllToggle != null)
                SelectAllToggle.interactable = Multiselect && !ReadOnly;
            ForeachElement((_, e) => e.Unlock());
        }

        public void ForeachElement(Action<int, MultiSelectItemListElement> func)
        {
            var elementCount = ListContainer.childCount;
            for (var i = 0; i < elementCount; ++i)
            {
                var childToCache = ListContainer.GetChild(i);
                var element = childToCache.GetComponent<MultiSelectItemListElement>();
                func(i, element);
            }
        }

        public void DeselectAll()
        {
            ForeachElement((_, e) => e.Deselect(true));
        }
    }
}
