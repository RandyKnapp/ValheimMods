using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public interface IListElement
    {
        ItemDrop.ItemData GetItem();
        int GetMax();
        string GetDisplayNameSuffix();
    }

    public class InventoryItemListElement : IListElement
    {
        public ItemDrop.ItemData Item;

        public ItemDrop.ItemData GetItem() => Item;
        public int GetMax() => Item?.m_stack ?? 0;
        public string GetDisplayNameSuffix() => string.Empty;
    }

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
        public event Action OnItemsChanged;

        public delegate List<IListElement> SortByRarityDelegate(List<IListElement> items);
        public delegate List<IListElement> SortByNameDelegate(List<IListElement> items);

        public static SortByRarityDelegate SortByRarity;
        public static SortByNameDelegate SortByName;

        private bool _locked;
        private bool _hasGamepadFocus;
        private ScrollRectEnsureVisible _scrollRectEnsureVisible;

        public void Awake()
        {
            var scrollRect = GetComponentInChildren<ScrollRect>();
            _scrollRectEnsureVisible = scrollRect != null ? scrollRect.GetComponent<ScrollRectEnsureVisible>() : null;

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

        public void Update()
        {
            if (_locked || !HasGamepadFocus() || !ZInput.IsGamepadActive() || ListContainer == null)
                return;

            var elementCount = ListContainer.childCount;
            var focusedElement = GetFocusedElement();
            if (focusedElement == null)
                return;

            var focusedElementIndex = focusedElement.transform.GetSiblingIndex();
            var grid = ListContainer.GetComponent<GridLayoutGroup>();
            if (ListContainer.GetComponent<VerticalLayoutGroup>() != null)
            {
                if (focusedElementIndex > 0 && ZInput.GetButtonDown("JoyLStickUp"))
                {
                    focusedElement.GiveFocus(false);
                    var newElement = GetElement(focusedElementIndex - 1);
                    newElement.GiveFocus(true);
                    CenterOnItem(newElement);
                    ZInput.ResetButtonStatus("JoyLStickUp");
                }
                else if (focusedElementIndex < elementCount - 1 && ZInput.GetButtonDown("JoyLStickDown"))
                {
                    focusedElement.GiveFocus(false);
                    var newElement = GetElement(focusedElementIndex + 1);
                    newElement.GiveFocus(true);
                    CenterOnItem(newElement);
                    ZInput.ResetButtonStatus("JoyLStickDown");
                }
                else if (ZInput.GetButtonDown("JoyLStickLeft"))
                {
                    ZInput.ResetButtonStatus("JoyLStickLeft");
                }
                else if (ZInput.GetButtonDown("JoyLStickRight"))
                {
                    ZInput.ResetButtonStatus("JoyLStickRight");
                }
            }
            else if (grid != null)
            {
                var columnCount = grid.constraintCount;

                if (focusedElementIndex >= columnCount && ZInput.GetButtonDown("JoyLStickUp"))
                {
                    focusedElement.GiveFocus(false);
                    var newElement = GetElement(focusedElementIndex - columnCount);
                    newElement.GiveFocus(true);
                    CenterOnItem(newElement);
                    ZInput.ResetButtonStatus("JoyLStickUp");
                }
                else if (focusedElementIndex < elementCount - columnCount && ZInput.GetButtonDown("JoyLStickDown"))
                {
                    focusedElement.GiveFocus(false);
                    var newElement = GetElement(focusedElementIndex + columnCount);
                    newElement.GiveFocus(true);
                    CenterOnItem(newElement);
                    ZInput.ResetButtonStatus("JoyLStickDown");
                }
                else if ((focusedElementIndex % columnCount) > 0 && ZInput.GetButtonDown("JoyLStickLeft"))
                {
                    focusedElement.GiveFocus(false);
                    var newElement = GetElement(focusedElementIndex - 1);
                    newElement.GiveFocus(true);
                    CenterOnItem(newElement);
                    ZInput.ResetButtonStatus("JoyLStickLeft");
                }
                else if ((focusedElementIndex % columnCount) < columnCount - 1 && focusedElementIndex < elementCount - 1 && ZInput.GetButtonDown("JoyLStickRight"))
                {
                    focusedElement.GiveFocus(false);
                    var newElement = GetElement(focusedElementIndex + 1);
                    newElement.GiveFocus(true);
                    CenterOnItem(newElement);
                    ZInput.ResetButtonStatus("JoyLStickRight");
                }
            }

            if (Multiselect && SelectAllToggle != null)
            {
                if (ZInput.GetButtonDown("JoyLStick"))
                {
                    SelectAllToggle.isOn = !SelectAllToggle.isOn;
                    ZInput.ResetButtonStatus("JoyLStick");
                }
            }

            if (Sortable && SortByDropdown != null)
            {
                if (ZInput.GetButtonDown("JoyRStick"))
                {
                    var currentSortMode = SortByDropdown.value;
                    var sortModeCount = SortByDropdown.options.Count;
                    currentSortMode = ((currentSortMode + 1) % sortModeCount);
                    SortByDropdown.value = currentSortMode;
                    ZInput.ResetButtonStatus("JoyRStick");
                }
            }
        }

        private void CenterOnItem(MultiSelectItemListElement element)
        {
            if (_scrollRectEnsureVisible != null)
                _scrollRectEnsureVisible.CenterOnItem((RectTransform)element.transform);
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

                SelectAllToggle.SetIsOnWithoutNotify(allAreSelected);
            }
        }

        private void OnSelectAllToggled(bool _ = true)
        {
            if (SelectAllToggle == null)
                return;

            if (SelectAllToggle.isOn)
                ForeachElement((_, x) => x.SelectMaxQuantity(true));
            else
                ForeachElement((_, x) => x.Deselect(true));
            RefreshSelectAllToggle();
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
                element.SuppressEvents = true;
                element.SetItem(itemToSet);
                if (previousSelectionAmounts.TryGetValue(itemToSet, out var previousQuantity))
                    element.SelectQuantity(previousQuantity, true);
                element.SuppressEvents = false;
            }

            RefreshSelectAllToggle();
        }

        public Dictionary<IListElement, int> GetCurrentSelectionAmounts()
        {
            var selectionAmounts = new Dictionary<IListElement, int>();
            var elementCount = ListContainer.childCount;
            for (var i = 0; i < elementCount; ++i)
            {
                var childToCache = ListContainer.GetChild(i);
                var element = childToCache.GetComponent<MultiSelectItemListElement>();
                if (element != null && element.GetItem() != null)
                    selectionAmounts.Add(element.GetListElement(), element.GetSelectedQuantity());
            }

            return selectionAmounts;
        }

        private void MakeEnoughElements(int itemCount)
        {
            var elementCount = ListContainer.childCount;
            if (elementCount > itemCount)
            {
                for (var i = elementCount - 1; i >= itemCount; --i)
                {
                    var childToDestroy = ListContainer.GetChild(i);
                    var element = childToDestroy.GetComponent<MultiSelectItemListElement>();
                    element.OnSelectionChanged -= OnElementSelectionChanged;
                    DestroyImmediate(childToDestroy.gameObject);
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
        }

        public void SetItems(List<IListElement> items)
        {
            var itemCount = items.Count;

            var previousSelectionAmounts = GetCurrentSelectionAmounts();
            var focusedElement = GetFocusedElement();

            MakeEnoughElements(itemCount);

            var sortedItems = items;
            if (Sortable && SortByDropdown != null)
            {
                var sortMode = (SortMode)SortByDropdown.value;
                sortedItems = SortItems(sortMode, items);
            }

            var didFocus = false;
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
                var shouldFocus = HasGamepadFocus() && ((focusedElement == null && i == 0) || element == focusedElement);
                element.GiveFocus(shouldFocus);
                if (shouldFocus)
                {
                    didFocus = true;
                    CenterOnItem(element);
                }
            }

            if (HasGamepadFocus() && !didFocus && ListContainer.childCount > 0)
            {
                // Force GiveFocus to fire
                _hasGamepadFocus = false;
                GiveFocus(true, 0);
                CenterOnItem(GetElement(0));
            }

            OnItemsChanged?.Invoke();
            OnSelectedItemsChanged?.Invoke();
            RefreshSelectAllToggle();
        }

        private void OnElementSelectionChanged(MultiSelectItemListElement element, bool isSelected, int selectedQuantity)
        {
            if (!Multiselect)
            {
                ForeachElement((_, x) =>
                {
                    if (x != element)
                    {
                        x.SuppressEvents = true;
                        x.Deselect(true);
                        x.SuppressEvents = false;
                    }
                });
            }

            OnSelectedItemsChanged?.Invoke();
            RefreshSelectAllToggle();
        }

        public List<IListElement> SortItems(SortMode mode, List<IListElement> items)
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
                        return items.OrderBy(x => Localization.instance.Localize(x.GetItem().m_shared.m_name)).ThenByDescending(x => x.GetItem().m_stack).ToList();

                case SortMode.Quantity: 
                    return items.OrderByDescending(x => x.GetItem().m_stack).ThenBy(x => Localization.instance.Localize(x.GetItem().m_shared.m_name)).ToList();

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            return items.ToList();
        }

        public List<Tuple<T, int>> GetSelectedItems<T>()
        {
            var result = new List<Tuple<T, int>>();
            var elementCount = ListContainer.childCount;
            for (var i = 0; i < elementCount; ++i)
            {
                var childToCache = ListContainer.GetChild(i);
                var element = childToCache.GetComponent<MultiSelectItemListElement>();
                var quantity = element.GetSelectedQuantity();
                if (quantity > 0)
                    result.Add(new Tuple<T, int>((T)element.GetListElement(), quantity));
            }

            return result;
        }

        public Tuple<T, int> GetSingleSelectedItem<T>()
        {
            var elementCount = ListContainer.childCount;
            for (var i = 0; i < elementCount; ++i)
            {
                var childToCache = ListContainer.GetChild(i);
                var element = childToCache.GetComponent<MultiSelectItemListElement>();
                var quantity = element.GetSelectedQuantity();
                if (quantity > 0)
                    return new Tuple<T, int>((T)element.GetListElement(), quantity);
            }

            return null;
        }

        public void Lock()
        {
            _locked = true;
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
            _locked = false;
            if (SortByDropdown != null)
                SortByDropdown.interactable = Sortable && !ReadOnly;
            if (FilterByText != null)
                FilterByText.interactable = Filterable && !ReadOnly;
            if (SelectAllToggle != null)
                SelectAllToggle.interactable = Multiselect && !ReadOnly;
            ForeachElement((_, e) => e.Unlock());
        }

        private MultiSelectItemListElement GetElement(int index)
        {
            var child = ListContainer.GetChild(index);
            return child == null ? null : child.GetComponent<MultiSelectItemListElement>();
        }

        public void ForeachElement(Action<int, MultiSelectItemListElement> func)
        {
            if (ListContainer == null)
                return;

            var elementCount = ListContainer.childCount;
            for (var i = 0; i < elementCount; ++i)
            {
                var element = GetElement(i);
                if (element != null)
                    func(i, element);
            }
        }

        public void DeselectAll()
        {
            SuppressEvents(true);
            ForeachElement((_, e) => e.Deselect(true));
            SuppressEvents(false);

            OnSelectedItemsChanged?.Invoke();
            RefreshSelectAllToggle();
        }

        public void SuppressEvents(bool suppress)
        {
            ForeachElement((_, e) => e.SuppressEvents = suppress);
        }

        public void GiveFocus(bool focused, int tryFocusIndex)
        {
            if (_hasGamepadFocus != focused)
            {
                _hasGamepadFocus = focused;

                var focusIndex = focused ? Mathf.Clamp(tryFocusIndex, 0, ListContainer.childCount - 1) : -1;
                ForeachElement((i, e) =>
                {
                    var shouldFocus = i == focusIndex;
                    e.GiveFocus(shouldFocus);
                    if (shouldFocus)
                        CenterOnItem(e);
                });
            }
        }

        public bool HasGamepadFocus()
        {
            return _hasGamepadFocus;
        }

        public MultiSelectItemListElement GetFocusedElement()
        {
            if (ListContainer == null || !ZInput.IsGamepadActive())
                return null;

            var elementCount = ListContainer.childCount;
            for (var i = 0; i < elementCount; ++i)
            {
                var child = ListContainer.GetChild(i);
                if (child == null)
                    continue;

                var element = child.GetComponent<MultiSelectItemListElement>();
                if (element != null && element.HasGamepadFocus())
                    return element;
            }

            return null;
        }

        public int GetItemCount()
        {
            if (ListContainer == null)
                return 0;

            var elementCount = ListContainer.childCount;
            var activeChildCount = 0;
            for (var i = 0; i < elementCount; ++i)
            {
                var child = ListContainer.GetChild(i);
                if (child != null && child.gameObject.activeSelf)
                    activeChildCount++;
            }

            return activeChildCount;
        }

        public bool IsGrid()
        {
            return ListContainer != null && ListContainer.GetComponent<GridLayoutGroup>() != null;
        }
    }
}
