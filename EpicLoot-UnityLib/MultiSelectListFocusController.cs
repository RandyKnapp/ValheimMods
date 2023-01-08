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
            for (var index = 0; index < Lists.Count; index++)
            {
                Lists[index].GiveFocus(index == _focusedListIndex, 0);
            }
            RefreshHints();
        }

        public void Update()
        {
            if (Lists.Count == 0)
                return;

            var currentList = Lists[_focusedListIndex];
            var loopCount = 0;
            var itemCount = currentList.GetItemCount();
            while (itemCount == 0)
            {
                currentList.GiveFocus(false, 0);

                _focusedListIndex = (_focusedListIndex + 1) % Lists.Count;
                currentList = Lists[_focusedListIndex];
                itemCount = currentList.GetItemCount();
                if (currentList.GetItemCount() > 0)
                {
                    currentList.GiveFocus(true, 0);
                    RefreshHints();
                    break;
                }
                loopCount++;
                if (loopCount >= Lists.Count)
                    return;
            }

            if (ZInput.IsGamepadActive())
            {
                var newFocusedIndex = _focusedListIndex;
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
                    var offset = newFocusedIndex - _focusedListIndex;
                    if (Lists[newFocusedIndex].GetItemCount() == 0)
                        newFocusedIndex = (newFocusedIndex + offset + Lists.Count) % Lists.Count;
                    if (Lists[newFocusedIndex].GetItemCount() == 0)
                        newFocusedIndex = _focusedListIndex;
                }

                FocusList(newFocusedIndex);
            }
                
            if (_gamepadWasEnabled != ZInput.IsGamepadActive())
                RefreshHints();

            _gamepadWasEnabled = ZInput.IsGamepadActive();
        }

        public void FocusList(int newFocusedIndex)
        {
            var list = Lists[_focusedListIndex];
            var currentFocusElement = list.GetFocusedElement();
            var currentFocusIndex = currentFocusElement != null ? currentFocusElement.transform.GetSiblingIndex() : -1;
            if (newFocusedIndex != _focusedListIndex && newFocusedIndex >= 0 && newFocusedIndex < Lists.Count)
            {
                _focusedListIndex = newFocusedIndex;
                for (var index = 0; index < Lists.Count; index++)
                {
                    var isGrid = Lists[index].IsGrid();
                    Lists[index].GiveFocus(index == _focusedListIndex, isGrid ? 0 : currentFocusIndex);
                }

                RefreshHints();
            }
        }

        private void RefreshHints()
        {
            if (!isActiveAndEnabled || !ZInput.IsGamepadActive() || Lists.Count == 0)
                return;

            var focusedList = Lists[_focusedListIndex];
            foreach (var hint in SortHints)
            {
                hint.SetActive(focusedList.Sortable && focusedList.SortByDropdown != null && focusedList.SortByDropdown.isActiveAndEnabled);
            }
            foreach (var hint in SelectAllHints)
            {
                hint.SetActive(focusedList.Multiselect && focusedList.SelectAllToggle != null && focusedList.SelectAllToggle.isActiveAndEnabled);
            }
            foreach (var hint in SelectHints)
            {
                hint.SetActive(!focusedList.ReadOnly && focusedList.GetFocusedElement() != null);
            }
        }
    }
}
