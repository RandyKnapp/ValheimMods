using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot_UnityLib
{
    public class MultiSelectListFocusController : MonoBehaviour
    {
        public List<MultiSelectItemList> Lists = new List<MultiSelectItemList>();
        public GameObject[] SortHints;
        public GameObject[] SelectAllHints;
        public GameObject[] SelectHints;

        private int _focusedListIndex;
        private bool _gamepadWasEnabled;


        public void OnEnable()
        {
            _focusedListIndex = 0;
            for (int index = 0; index < Lists.Count; index++)
            {
                if (Lists[index] == null)
                {
                    continue;
                }
                Lists[index].GiveFocus(index == _focusedListIndex, 0);
            }

            RefreshHints();
        }

        public void Update()
        {
            if (Lists.Count == 0)
            {
                return;
            }

            MultiSelectItemList currentList = Lists[_focusedListIndex];
            int loopCount = 0;
            int itemCount = currentList.GetItemCount();
            while (itemCount == 0)
            {
                currentList.GiveFocus(false, 0);

                _focusedListIndex = (_focusedListIndex + 1) % Lists.Count;
                currentList = Lists[_focusedListIndex];
                if (currentList == null)
                {
                    continue;
                }

                itemCount = currentList.GetItemCount();
                if (currentList.GetItemCount() > 0)
                {
                    currentList.GiveFocus(true, 0);
                    RefreshHints();
                    break;
                }

                loopCount++;
                if (loopCount >= Lists.Count)
                {
                    return;
                }
            }

            if (ZInput.IsGamepadActive())
            {
                int newFocusedIndex = _focusedListIndex;
                if (ZInput.GetButtonDown("JoyTabLeft"))
                {
                    newFocusedIndex = Mathf.Max(_focusedListIndex - 1, 0);
                    ZInput.ResetButtonStatus("JoyTabLeft");
                }
                else if (ZInput.GetButtonDown("JoyTabRight"))
                {
                    newFocusedIndex = Mathf.Min(_focusedListIndex + 1, Lists.Count - 1);
                    ZInput.ResetButtonStatus("JoyTabRight");
                }

                if (newFocusedIndex != _focusedListIndex)
                {
                    int offset = newFocusedIndex - _focusedListIndex;
                    if (Lists[newFocusedIndex].GetItemCount() == 0)
                    {
                        newFocusedIndex = (newFocusedIndex + offset + Lists.Count) % Lists.Count;
                    }
                    if (Lists[newFocusedIndex].GetItemCount() == 0)
                    {
                        newFocusedIndex = _focusedListIndex;
                    }
                }

                FocusList(newFocusedIndex);
            }
                
            if (_gamepadWasEnabled != ZInput.IsGamepadActive())
            {
                RefreshHints();
            }

            _gamepadWasEnabled = ZInput.IsGamepadActive();
        }

        public void FocusList(int newFocusedIndex)
        {
            MultiSelectItemList list = Lists[_focusedListIndex];
            MultiSelectItemListElement currentFocusElement = list.GetFocusedElement();
            int currentFocusIndex = currentFocusElement != null ? currentFocusElement.transform.GetSiblingIndex() : -1;
            if (newFocusedIndex != _focusedListIndex && newFocusedIndex >= 0 && newFocusedIndex < Lists.Count)
            {
                _focusedListIndex = newFocusedIndex;
                for (int index = 0; index < Lists.Count; index++)
                {
                    bool isGrid = Lists[index].IsGrid();
                    Lists[index].GiveFocus(index == _focusedListIndex, isGrid ? 0 : currentFocusIndex);
                }

                RefreshHints();
            }
        }

        private void RefreshHints()
        {
            if (!isActiveAndEnabled || !ZInput.IsGamepadActive() || Lists.Count == 0)
                return;

            MultiSelectItemList focusedList = Lists[_focusedListIndex];
            foreach (GameObject hint in SortHints)
            {
                hint.SetActive(focusedList.Sortable && focusedList.SortByDropdown != null &&
                    focusedList.SortByDropdown.isActiveAndEnabled);
            }
            foreach (GameObject hint in SelectAllHints)
            {
                hint.SetActive(focusedList.Multiselect && focusedList.SelectAllToggle != null &&
                    focusedList.SelectAllToggle.isActiveAndEnabled);
            }
            foreach (GameObject hint in SelectHints)
            {
                hint.SetActive(!focusedList.ReadOnly && focusedList.GetFocusedElement() != null);
            }
        }
    }
}
