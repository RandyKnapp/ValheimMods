using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EpicLoot.CraftingV2;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public interface IListElement
    {
        ItemDrop.ItemData GetItem();

        public List<string> GetEffectNames();
        string GetEnchantName();
        int GetMax();
        string GetDisplayNameSuffix();
    }

    public class InventoryItemListElement : IListElement
    {
        public ItemDrop.ItemData Item;
        public List<Tuple<string, float>> Effects;
        public string EnchantName;

        public List<string> GetEffectNames() => Effects?.Select(x => x.Item1).ToList() ?? new List<string>();
        public string GetEnchantName() => EnchantName ?? string.Empty;
        public ItemDrop.ItemData GetItem() => Item;
        public int GetMax() => Item?.m_stack ?? 0;
        public string GetDisplayNameSuffix() => string.Empty;
    }

    public class MultiSelectItemList : MonoBehaviour
    {
        public enum SortMode { Rarity, Name, Quantity }

        private static readonly Regex RichTextRegex = new Regex(@"<[^>]*>", RegexOptions.Compiled);

        public bool Multiselect = true;
        public bool Filterable = true;
        public bool Sortable = true;
        public bool ReadOnly = false;
        public bool UseEnchantAsName = false;
        public Transform ListContainer;
        public MultiSelectItemListElement ElementPrefab;
        public Dropdown SortByDropdown;
        public InputField FilterByText;
        public Toggle SelectAllToggle;

        public event Action OnSelectedItemsChanged;
        public event Action OnItemsChanged;

        private bool _locked;
        private bool _hasGamepadFocus;
        private ScrollRectEnsureVisible _scrollRectEnsureVisible;

        public void Awake()
        {
            ScrollRect scrollRect = GetComponentInChildren<ScrollRect>();
            _scrollRectEnsureVisible = scrollRect != null ? scrollRect.GetComponent<ScrollRectEnsureVisible>() : null;

            if (SelectAllToggle != null)
            {
                SelectAllToggle.onValueChanged.AddListener(OnSelectAllToggled);
            }
            
            if (SortByDropdown != null)
            {
                foreach (Dropdown.OptionData optionData in SortByDropdown.options)
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
            {
                return;
            }

            int elementCount = ListContainer.childCount;
            MultiSelectItemListElement focusedElement = GetFocusedElement();
            if (focusedElement == null)
            {
                return;
            }

            int focusedElementIndex = focusedElement.transform.GetSiblingIndex();
            GridLayoutGroup grid = ListContainer.GetComponent<GridLayoutGroup>();
            if (ListContainer.GetComponent<VerticalLayoutGroup>() != null)
            {
                if (focusedElementIndex > 0 && ZInput.GetButtonDown("JoyLStickUp"))
                {
                    focusedElement.GiveFocus(false);
                    MultiSelectItemListElement newElement = GetElement(focusedElementIndex - 1);
                    newElement.GiveFocus(true);
                    CenterOnItem(newElement);
                    ZInput.ResetButtonStatus("JoyLStickUp");
                }
                else if (focusedElementIndex < elementCount - 1 && ZInput.GetButtonDown("JoyLStickDown"))
                {
                    focusedElement.GiveFocus(false);
                    MultiSelectItemListElement newElement = GetElement(focusedElementIndex + 1);
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
                int columnCount = grid.constraintCount;

                if (focusedElementIndex >= columnCount &&
                    ZInput.GetButtonDown("JoyLStickUp"))
                {
                    focusedElement.GiveFocus(false);
                    MultiSelectItemListElement newElement = GetElement(focusedElementIndex - columnCount);
                    newElement.GiveFocus(true);
                    CenterOnItem(newElement);
                    ZInput.ResetButtonStatus("JoyLStickUp");
                }
                else if (focusedElementIndex < elementCount - columnCount &&
                    ZInput.GetButtonDown("JoyLStickDown"))
                {
                    focusedElement.GiveFocus(false);
                    MultiSelectItemListElement newElement = GetElement(focusedElementIndex + columnCount);
                    newElement.GiveFocus(true);
                    CenterOnItem(newElement);
                    ZInput.ResetButtonStatus("JoyLStickDown");
                }
                else if ((focusedElementIndex % columnCount) > 0 &&
                    ZInput.GetButtonDown("JoyLStickLeft"))
                {
                    focusedElement.GiveFocus(false);
                    MultiSelectItemListElement newElement = GetElement(focusedElementIndex - 1);
                    newElement.GiveFocus(true);
                    CenterOnItem(newElement);
                    ZInput.ResetButtonStatus("JoyLStickLeft");
                }
                else if ((focusedElementIndex % columnCount) < columnCount - 1 &&
                    focusedElementIndex < elementCount - 1 &&
                    ZInput.GetButtonDown("JoyLStickRight"))
                {
                    focusedElement.GiveFocus(false);
                    MultiSelectItemListElement newElement = GetElement(focusedElementIndex + 1);
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
                    int currentSortMode = SortByDropdown.value;
                    int sortModeCount = SortByDropdown.options.Count;
                    currentSortMode = ((currentSortMode + 1) % sortModeCount);
                    SortByDropdown.value = currentSortMode;
                    ZInput.ResetButtonStatus("JoyRStick");
                }
            }
        }

        private void CenterOnItem(MultiSelectItemListElement element)
        {
            if (_scrollRectEnsureVisible != null)
            {
                _scrollRectEnsureVisible.CenterOnItem((RectTransform)element.transform);
            }
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

            if (!Filterable || FilterByText == null || ListContainer == null)
                return;

            string filterText = FilterByText.text;
            bool filterIsEmpty = string.IsNullOrEmpty(filterText) || string.IsNullOrWhiteSpace(filterText);

            string[] filterParts = filterIsEmpty ? Array.Empty<string>() :
                filterText.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries);
            int elementCount = ListContainer.childCount;

            for (int i = 0; i < elementCount; ++i)
            {
                MultiSelectItemListElement element = GetElement(i);
                if (element == null)
                {
                    continue;
                }

                // A row with no name label can't be matched against, so never hide it.
                bool nameMatches = filterIsEmpty || element.ItemName == null;
                if (!nameMatches)
                {
                    // Strip rich text tags from item name
                    string itemName = RichTextRegex.Replace(element.ItemName.text ?? string.Empty, string.Empty);

                    foreach (string part in filterParts)
                    {
                        if (itemName.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            nameMatches = true;
                            break;
                        }
                    }
                }

                element.gameObject.SetActive(nameMatches);
            }
        }

        /// <summary>
        /// Clears the filter text, revealing every element again. Does nothing on an unfilterable list.
        /// </summary>
        public void ClearFilter()
        {
            if (FilterByText == null || string.IsNullOrEmpty(FilterByText.text))
            {
                return;
            }

            // Fires OnFilterChanged, which refreshes the visibility mask.
            FilterByText.text = string.Empty;
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

                // Only the visible rows count: Select All acts on what the filter is showing, so its
                // checked state has to describe that same set. An empty list is never "all selected".
                int visibleCount = 0;
                int maxSelectedCount = 0;
                ForeachVisibleElement((_, element) =>
                {
                    ++visibleCount;
                    if (element.IsMaxSelected())
                    {
                        ++maxSelectedCount;
                    }
                });

                SelectAllToggle.SetIsOnWithoutNotify(visibleCount > 0 && maxSelectedCount == visibleCount);
            }
        }

        private void OnSelectAllToggled(bool _ = true)
        {
            if (SelectAllToggle == null)
            {
                return;
            }

            // Visible only. Rows the filter has hidden keep whatever selection they already had, but
            // the player can only ever select or clear what they can actually see.
            if (SelectAllToggle.isOn)
            {
                ForeachVisibleElement((_, x) => x.SelectMaxQuantity(true));
            }
            else
            {
                ForeachVisibleElement((_, x) => x.Deselect(true));
            }

            RefreshSelectAllToggle();
        }

        private void OnSortModeChanged(int sortModeValue)
        {
            if (!Sortable || SortByDropdown == null)
            {
                return;
            }

            Dictionary<IListElement, int> previousSelectionAmounts = GetCurrentSelectionAmounts();

            List<IListElement> items = previousSelectionAmounts.Keys.ToList();
            SortMode sortMode = (SortMode)SortByDropdown.value;
            List<IListElement> sortedItems = SortItems(sortMode, items);

            for (int i = 0; i < sortedItems.Count; ++i)
            {
                Transform childToSet = ListContainer.GetChild(i);
                IListElement itemToSet = sortedItems[i];
                MultiSelectItemListElement element = childToSet.GetComponent<MultiSelectItemListElement>();
                element.SuppressEvents = true;
                element.SetItem(itemToSet);
                if (previousSelectionAmounts.TryGetValue(itemToSet, out int previousQuantity))
                {
                    element.SelectQuantity(previousQuantity, true);
                }

                element.SuppressEvents = false;
            }

            // Rows were just reassigned by index, so the old show/hide mask now describes the wrong items.
            Refresh();
        }

        // Unlike the selection getters, this walks every row including filtered-out ones. It exists to
        // carry selection across a re-population, and hidden rows keep their selection by design.
        public Dictionary<IListElement, int> GetCurrentSelectionAmounts()
        {
            Dictionary<IListElement, int> selectionAmounts = new Dictionary<IListElement, int>();
            int elementCount = ListContainer.childCount;
            for (int i = 0; i < elementCount; ++i)
            {
                Transform childToCache = ListContainer.GetChild(i);
                MultiSelectItemListElement element = childToCache.GetComponent<MultiSelectItemListElement>();
                if (element != null && element.GetItem() != null)
                {
                    selectionAmounts.Add(element.GetListElement(), element.GetSelectedQuantity());
                }
            }

            return selectionAmounts;
        }

        private void MakeEnoughElements(int itemCount)
        {
            int elementCount = ListContainer.childCount;
            if (elementCount > itemCount)
            {
                for (int i = elementCount - 1; i >= itemCount; --i)
                {
                    Transform childToDestroy = ListContainer.GetChild(i);
                    MultiSelectItemListElement element = childToDestroy.GetComponent<MultiSelectItemListElement>();
                    element.OnSelectionChanged -= OnElementSelectionChanged;
                    DestroyImmediate(childToDestroy.gameObject);
                }
            }
            else if (elementCount < itemCount)
            {
                for (int i = elementCount; i < itemCount; ++i)
                {
                    MultiSelectItemListElement newElement = Instantiate(ElementPrefab, ListContainer);
                    newElement.SuppressEvents = true;
                    newElement.OnSelectionChanged += OnElementSelectionChanged;
                }
            }
        }

        public void SetItems(List<IListElement> items)
        {
            int itemCount = items.Count;

            Dictionary<IListElement, int> previousSelectionAmounts = GetCurrentSelectionAmounts();
            MultiSelectItemListElement focusedElement = GetFocusedElement();

            MakeEnoughElements(itemCount);

            List<IListElement> sortedItems = items;
            if (Sortable && SortByDropdown != null)
            {
                SortMode sortMode = (SortMode)SortByDropdown.value;
                sortedItems = SortItems(sortMode, items);
            }

            bool didFocus = false;
            for (int i = 0; i < itemCount; ++i)
            {
                Transform childToSet = ListContainer.GetChild(i);
                IListElement itemToSet = sortedItems[i];
                MultiSelectItemListElement element = childToSet.GetComponent<MultiSelectItemListElement>();
                element.UseEnchantAsName = UseEnchantAsName;
                element.SuppressEvents = true;
                element.SetItem(itemToSet);

                if (previousSelectionAmounts.TryGetValue(itemToSet, out int previousQuantity))
                {
                    element.SelectQuantity(previousQuantity, true);
                }

                element.SuppressEvents = false;
                bool shouldFocus = HasGamepadFocus() && ((focusedElement == null && i == 0) || element == focusedElement);
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

            // Reapply the filter before the events fire: elements were reused and reassigned by index, so
            // the previous mask hides the wrong rows, and OnSelectedItemsChanged reads through
            // GetSelectedItems, which now depends on that mask being correct.
            RefreshFilter();

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
                    return EnchantingUIController.SortByRarity(items);
                case SortMode.Name:
                    return EnchantingUIController.SortByName(items);
                case SortMode.Quantity:
                    return items.OrderByDescending(x => x.GetItem().m_stack)
                        .ThenBy(x => Localization.instance.Localize(x.GetItem().m_shared.m_name)).ToList();
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        // The three selection getters below all skip filtered-out rows. Whatever they return is what
        // the panel previews and then acts on, so a row the player cannot see must never be in it -
        // otherwise filtering to one item and hitting Select All sacrifices the whole inventory.
        public List<Tuple<T, int>> GetSelectedItems<T>()
        {
            List<Tuple<T, int>> result = new List<Tuple<T, int>>();
            int elementCount = ListContainer.childCount;
            for (int i = 0; i < elementCount; ++i)
            {
                MultiSelectItemListElement element = GetElement(i);
                if (!IsVisible(element))
                {
                    continue;
                }

                int quantity = element.GetSelectedQuantity();
                if (quantity > 0)
                {
                    result.Add(new Tuple<T, int>((T)element.GetListElement(), quantity));
                }
            }

            return result;
        }

        public Tuple<T, int> GetSingleSelectedItem<T>()
        {
            int elementCount = ListContainer.childCount;
            for (int i = 0; i < elementCount; ++i)
            {
                MultiSelectItemListElement element = GetElement(i);
                if (!IsVisible(element))
                {
                    continue;
                }

                int quantity = element.GetSelectedQuantity();
                if (quantity > 0)
                {
                    return new Tuple<T, int>((T)element.GetListElement(), quantity);
                }
            }

            return null;
        }

        public int GetFirstSelectedIndex()
        {
            int elementCount = ListContainer.childCount;
            for (int i = 0; i < elementCount; ++i)
            {
                MultiSelectItemListElement element = GetElement(i);
                if (!IsVisible(element))
                {
                    continue;
                }

                if (element.GetSelectedQuantity() > 0)
                {
                    return i;
                }
            }

            return -1;
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
            Transform child = ListContainer.GetChild(index);
            return child == null ? null : child.GetComponent<MultiSelectItemListElement>();
        }

        /// <summary>
        /// False for a row the filter has hidden. RefreshFilter only calls SetActive(false) on
        /// non-matching rows - they stay in ListContainer and keep their selected quantity - so this is
        /// what separates "in the list" from "on screen".
        /// </summary>
        private static bool IsVisible(MultiSelectItemListElement element)
        {
            return element != null && element.gameObject.activeSelf;
        }

        public void ForeachElement(Action<int, MultiSelectItemListElement> func)
        {
            if (ListContainer == null)
            {
                return;
            }

            int elementCount = ListContainer.childCount;
            for (int i = 0; i < elementCount; ++i)
            {
                MultiSelectItemListElement element = GetElement(i);
                if (element != null)
                {
                    func(i, element);
                }
            }
        }

        /// <summary>
        /// Like <see cref="ForeachElement"/>, but skips rows hidden by the filter. Use this for anything
        /// the player perceives as acting on "the list"; use ForeachElement for bookkeeping that must
        /// cover every row regardless of the filter (Lock/Unlock, SuppressEvents, DeselectAll).
        /// The index passed to <paramref name="func"/> is the sibling index, not a visible-only counter.
        /// </summary>
        public void ForeachVisibleElement(Action<int, MultiSelectItemListElement> func)
        {
            ForeachElement((i, element) =>
            {
                if (IsVisible(element))
                {
                    func(i, element);
                }
            });
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

                int focusIndex = focused ? Mathf.Clamp(tryFocusIndex, 0, ListContainer.childCount - 1) : -1;
                ForeachElement((i, e) =>
                {
                    bool shouldFocus = i == focusIndex;
                    e.GiveFocus(shouldFocus);
                    if (shouldFocus)
                    {
                        CenterOnItem(e);
                    }
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
            {
                return null;
            }

            int elementCount = ListContainer.childCount;
            for (int i = 0; i < elementCount; ++i)
            {
                Transform child = ListContainer.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                MultiSelectItemListElement element = child.GetComponent<MultiSelectItemListElement>();
                if (element != null && element.HasGamepadFocus())
                {
                    return element;
                }
            }

            return null;
        }

        public int GetItemCount()
        {
            if (ListContainer == null)
            {
                return 0;
            }

            int elementCount = ListContainer.childCount;
            int activeChildCount = 0;
            for (int i = 0; i < elementCount; ++i)
            {
                Transform child = ListContainer.GetChild(i);
                if (child != null && child.gameObject.activeSelf)
                {
                    activeChildCount++;
                }
            }

            return activeChildCount;
        }

        public bool IsGrid()
        {
            return ListContainer != null && ListContainer.GetComponent<GridLayoutGroup>() != null;
        }

        public void InitWithExistingItems()
        {
            for (int i = 0; i < ListContainer.childCount; ++i)
            {
                Transform childToSet = ListContainer.GetChild(i);
                MultiSelectItemListElement element = childToSet.GetComponent<MultiSelectItemListElement>();
                element.OnSelectionChanged += OnElementSelectionChanged;
            }

            DeselectAll();

            RefreshFilter();

            OnItemsChanged?.Invoke();
            OnSelectedItemsChanged?.Invoke();
            RefreshSelectAllToggle();
        }
    }
}
