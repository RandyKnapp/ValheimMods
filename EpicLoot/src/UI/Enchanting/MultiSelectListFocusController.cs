using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot_UnityLib
{
    public interface IGamepadFocusPane
    {
        int GetItemCount();
        int GetFocusedIndex();
        void GiveFocus(bool focused, int tryFocusIndex);
        bool IsGrid();
        bool ShowSortHint { get; }
        bool ShowSelectAllHint { get; }
        bool ShowSelectHint { get; }
    }

    public class MultiSelectListFocusController : MonoBehaviour
    {
        public List<MultiSelectItemList> Lists = new List<MultiSelectItemList>();
        public GameObject[] SortHints;
        public GameObject[] SelectAllHints;
        public GameObject[] SelectHints;

        private readonly List<IGamepadFocusPane> _panes = new List<IGamepadFocusPane>();
        private List<IGamepadFocusPane> _paneOverride;
        private int _focusedPaneIndex;
        private bool _gamepadWasEnabled;

        // For panels whose panes are not all MultiSelectItemLists, or that want a different order than the
        // prefab's. Call it before this component is enabled.
        public void SetPanes(IEnumerable<IGamepadFocusPane> panes)
        {
            _paneOverride = new List<IGamepadFocusPane>(panes);
            RebuildPanes();
            ClampFocusedPane();
        }

        public void OnEnable()
        {
            RebuildPanes();

            _focusedPaneIndex = 0;
            for (int index = 0; index < _panes.Count; index++)
            {
                _panes[index].GiveFocus(index == _focusedPaneIndex, 0);
            }

            RefreshHints();
        }

        private void RebuildPanes()
        {
            _panes.Clear();

            if (_paneOverride != null)
            {
                foreach (IGamepadFocusPane pane in _paneOverride)
                {
                    if (pane != null)
                    {
                        _panes.Add(pane);
                    }
                }

                return;
            }

            foreach (MultiSelectItemList list in Lists)
            {
                if (list != null)
                {
                    _panes.Add(list);
                }
            }
        }

        private void ClampFocusedPane()
        {
            _focusedPaneIndex = _panes.Count > 0 ? Mathf.Clamp(_focusedPaneIndex, 0, _panes.Count - 1) : 0;
        }

        public void Update()
        {
            if (_panes.Count == 0)
            {
                return;
            }

            ClampFocusedPane();

            IGamepadFocusPane currentPane = _panes[_focusedPaneIndex];
            int loopCount = 0;
            while (currentPane.GetItemCount() == 0)
            {
                currentPane.GiveFocus(false, 0);

                _focusedPaneIndex = (_focusedPaneIndex + 1) % _panes.Count;
                currentPane = _panes[_focusedPaneIndex];

                if (currentPane.GetItemCount() > 0)
                {
                    currentPane.GiveFocus(true, 0);
                    RefreshHints();
                    break;
                }

                loopCount++;
                if (loopCount >= _panes.Count)
                {
                    return;
                }
            }

            if (ZInput.IsGamepadActive())
            {
                int newFocusedIndex = _focusedPaneIndex;
                if (ZInput.GetButtonDown("JoyTabLeft"))
                {
                    newFocusedIndex = Mathf.Max(_focusedPaneIndex - 1, 0);
                    ZInput.ResetButtonStatus("JoyTabLeft");
                }
                else if (ZInput.GetButtonDown("JoyTabRight"))
                {
                    newFocusedIndex = Mathf.Min(_focusedPaneIndex + 1, _panes.Count - 1);
                    ZInput.ResetButtonStatus("JoyTabRight");
                }

                if (newFocusedIndex != _focusedPaneIndex)
                {
                    int offset = newFocusedIndex - _focusedPaneIndex;
                    if (_panes[newFocusedIndex].GetItemCount() == 0)
                    {
                        newFocusedIndex = (newFocusedIndex + offset + _panes.Count) % _panes.Count;
                    }
                    if (_panes[newFocusedIndex].GetItemCount() == 0)
                    {
                        newFocusedIndex = _focusedPaneIndex;
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
            int currentFocusIndex = _panes[_focusedPaneIndex].GetFocusedIndex();
            if (newFocusedIndex != _focusedPaneIndex && newFocusedIndex >= 0 && newFocusedIndex < _panes.Count)
            {
                _focusedPaneIndex = newFocusedIndex;
                for (int index = 0; index < _panes.Count; index++)
                {
                    bool isGrid = _panes[index].IsGrid();
                    _panes[index].GiveFocus(index == _focusedPaneIndex, isGrid ? 0 : currentFocusIndex);
                }

                RefreshHints();
            }
        }

        private void RefreshHints()
        {
            if (!isActiveAndEnabled || !ZInput.IsGamepadActive() || _panes.Count == 0)
                return;

            IGamepadFocusPane focusedPane = _panes[_focusedPaneIndex];
            foreach (GameObject hint in SortHints)
            {
                hint.SetActive(focusedPane.ShowSortHint);
            }
            foreach (GameObject hint in SelectAllHints)
            {
                hint.SetActive(focusedPane.ShowSelectAllHint);
            }
            foreach (GameObject hint in SelectHints)
            {
                hint.SetActive(focusedPane.ShowSelectHint);
            }
        }
    }
}
