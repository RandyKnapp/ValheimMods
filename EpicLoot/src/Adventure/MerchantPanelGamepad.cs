using System.Collections.Generic;
using EpicLoot.Adventure.Feature;
using EpicLoot.Compendium;
using EpicLoot.Crafting;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EpicLoot.Adventure
{
    /// <summary>
    /// Gamepad navigation for the merchant panel. The bumpers walk one chain -- the vanilla store
    /// list, then each of this panel's lists left to right -- the left stick and d-pad move the
    /// selection inside the focused list, X presses that list's action button and Y abandons a
    /// bounty.
    /// </summary>
    public class MerchantPanelGamepad
    {
        public const string NextListButton = "JoyTabRight";
        public const string PrevListButton = "JoyTabLeft";
        public const string MainActionButton = "JoyButtonX";
        public const string SecondaryActionButton = "JoyButtonY";

        private const int Unfocused = -1;
        private const float GlyphSize = 28f;

        private class ListHints
        {
            public TMP_Text Prev;
            public TMP_Text Next;
            public TMP_Text MainAction;
            public TMP_Text SecondaryAction;
        }

        private readonly MerchantPanel _panel;
        private readonly List<IMerchantListPanel> _lists;
        private readonly List<ListHints> _hints = new List<ListHints>();

        private int _focusedIndex = Unfocused;
        private bool _fontsApplied;

        public bool IsFocused => _focusedIndex != Unfocused;

        public MerchantPanelGamepad(MerchantPanel panel, List<IMerchantListPanel> lists)
        {
            _panel = panel;
            _lists = lists;

            foreach (IMerchantListPanel list in lists)
            {
                _hints.Add(CreateHints(list));
            }
        }

        public void Update()
        {
            if (!ZInput.IsGamepadActive())
            {
                Release();
                RefreshHints(false);
                return;
            }

            if (IsBlocked())
            {
                RefreshHints(false);
                return;
            }

            if (IsFocused)
            {
                UpdateFocusedInput();
            }
            else if (ZInput.GetButtonDown(NextListButton))
            {
                int target = FindList(Unfocused, 1);
                if (target != Unfocused)
                {
                    ZInput.ResetButtonStatus(NextListButton);
                    Focus(target);
                }
            }

            RefreshHints(true);
        }

        /// <summary>
        /// Answers the panel's modal dialogs and reports whether one is up. Returns true for the whole
        /// time a dialog is showing, not just the frame it answers: every press while it is up belongs
        /// to the dialog, and the store window behind it must not act on any of them.
        /// </summary>
        public bool UpdateDialogs()
        {
            AbandonBountyDialog abandonDialog = _panel.AbandonBountyDialog;
            if (abandonDialog != null && abandonDialog.gameObject.activeSelf)
            {
                if (ZInput.GetButtonDown("JoyButtonA"))
                {
                    ZInput.ResetButtonStatus("JoyButtonA");
                    abandonDialog.OnYesButtonClicked();
                }
                else if (ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape))
                {
                    ZInput.ResetButtonStatus("JoyButtonB");
                    abandonDialog.OnNoButtonClicked();
                }

                return true;
            }

            return AnyDialogOpen();
        }

        private bool AnyDialogOpen()
        {
            AbandonBountyDialog abandonDialog = _panel.AbandonBountyDialog;
            if (abandonDialog != null && abandonDialog.gameObject.activeSelf)
            {
                return true;
            }

            CraftSuccessDialog gambleDialog = _panel.GambleSuccessDialog;
            return gambleDialog != null && gambleDialog.gameObject.activeSelf;
        }

        public void Release()
        {
            if (!IsFocused)
            {
                return;
            }

            _focusedIndex = Unfocused;

            foreach (IMerchantListPanel list in _lists)
            {
                list.ClearSelection();
            }

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            // m_trader is already null when the store is closing, and SelectItem reads its item list
            // for anything but -1.
            StoreGui storeGui = StoreGui.instance;
            if (storeGui != null && storeGui.m_trader != null && storeGui.m_itemList.Count > 0)
            {
                storeGui.SelectItem(0, true);
            }
        }

        private void UpdateFocusedInput()
        {
            IMerchantListPanel focused = _lists[_focusedIndex];
            if (focused.GetItemCount() == 0)
            {
                int replacement = FindList(_focusedIndex, 1);
                if (replacement == Unfocused)
                {
                    replacement = FindList(_focusedIndex, -1);
                }

                if (replacement == Unfocused)
                {
                    Release();
                }
                else
                {
                    Focus(replacement);
                }

                return;
            }

            DeselectStoreItem();

            // A purchase or an accepted bounty rebuilds the rows, which drops the selection with them.
            if (focused.GetSelectedIndex() < 0)
            {
                focused.SelectIndex(0);
            }

            if (ZInput.GetButtonDown(PrevListButton))
            {
                ZInput.ResetButtonStatus(PrevListButton);
                int target = FindList(_focusedIndex, -1);
                if (target == Unfocused)
                {
                    Release();
                }
                else
                {
                    Focus(target);
                }

                return;
            }

            if (ZInput.GetButtonDown(NextListButton))
            {
                ZInput.ResetButtonStatus(NextListButton);
                int target = FindList(_focusedIndex, 1);
                if (target != Unfocused)
                {
                    Focus(target);
                }

                return;
            }

            if (ZInput.GetButtonDown(MainActionButton))
            {
                ZInput.ResetButtonStatus(MainActionButton);
                Press(focused.GetMainButton());
                return;
            }

            Button secondaryButton = focused.GetSecondaryButton();
            if (secondaryButton != null && ZInput.GetButtonDown(SecondaryActionButton))
            {
                ZInput.ResetButtonStatus(SecondaryActionButton);
                Press(secondaryButton);
                return;
            }

            // Deliberately no ZInput.ResetButtonStatus on the navigation buttons: a reset clears the
            // held state ZInput's own key repeat runs off, which costs a held stick every repeat past
            // the first.
            int step;
            if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
            {
                step = -1;
            }
            else if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
            {
                step = 1;
            }
            else
            {
                return;
            }

            MoveSelection(focused, step);
        }

        private void MoveSelection(IMerchantListPanel focused, int step)
        {
            if (focused.SelectIndex(focused.GetSelectedIndex() + step))
            {
                return;
            }

            // The two bounty lists sit one above the other in the same column, so running off the end
            // of one carries into the other rather than stopping.
            int neighbour = _focusedIndex + step;
            if (neighbour < 0 || neighbour >= _lists.Count || !SharesColumn(_focusedIndex, neighbour) ||
                _lists[neighbour].GetItemCount() == 0)
            {
                return;
            }

            Focus(neighbour, step > 0 ? 0 : _lists[neighbour].GetItemCount() - 1);
        }

        private void Focus(int index, int selectIndex = -1)
        {
            _focusedIndex = index;

            DeselectStoreItem();

            for (int i = 0; i < _lists.Count; ++i)
            {
                if (i != index)
                {
                    _lists[i].ClearSelection();
                }
            }

            IMerchantListPanel list = _lists[index];
            list.SelectIndex(selectIndex >= 0 ? selectIndex : Mathf.Max(list.GetSelectedIndex(), 0));
        }

        // Vanilla's own buy hotkey is gated on its selected item, so dropping that selection is what
        // stops the store acting on presses aimed at this panel.
        private static void DeselectStoreItem()
        {
            StoreGui storeGui = StoreGui.instance;
            if (storeGui != null && storeGui.m_selectedItem != null)
            {
                storeGui.SelectItem(-1, false);
            }
        }

        private static void Press(Button button)
        {
            if (button != null && button.IsInteractable())
            {
                button.onClick.Invoke();
            }
        }

        /// <summary>
        /// The next list with rows in it, walking from <paramref name="from"/> in
        /// <paramref name="direction"/>. Returns <see cref="Unfocused"/> when there is none, which is
        /// the signal to hand control back to the store list (going left) or to stay put (going right).
        /// </summary>
        private int FindList(int from, int direction)
        {
            for (int i = from + direction; i >= 0 && i < _lists.Count; i += direction)
            {
                if (_lists[i].GetItemCount() > 0)
                {
                    return i;
                }
            }

            return Unfocused;
        }

        private bool SharesColumn(int a, int b)
        {
            Transform headerA = _lists[a].GetHeader();
            Transform headerB = _lists[b].GetHeader();
            return headerA != null && headerB != null && headerA.parent == headerB.parent;
        }

        private bool IsBlocked()
        {
            return (_panel.InputBlocker != null && _panel.InputBlocker.activeSelf) || AnyDialogOpen();
        }

        private ListHints CreateHints(IMerchantListPanel list)
        {
            Transform header = list.GetHeader();
            Button mainButton = list.GetMainButton();
            Button secondaryButton = list.GetSecondaryButton();

            return new ListHints
            {
                Prev = CreateGlyph(header, PrevListButton, new Vector2(0f, 0.5f), new Vector2(GlyphSize * 0.5f, 0f)),
                Next = CreateGlyph(header, NextListButton, new Vector2(1f, 0.5f), new Vector2(GlyphSize * -0.5f, 0f)),
                MainAction = CreateGlyph(mainButton != null ? mainButton.transform : null, MainActionButton,
                    new Vector2(1f, 0.5f), new Vector2(GlyphSize * -0.75f, 0f)),
                SecondaryAction = CreateGlyph(secondaryButton != null ? secondaryButton.transform : null,
                    SecondaryActionButton, new Vector2(1f, 0f), new Vector2(GlyphSize * -0.4f, GlyphSize * 0.4f))
            };
        }

        // Named after the ZInput key, the way the temper panel's authored hints are, so the glyph can
        // be re-read from the binding whenever it is shown.
        private static TMP_Text CreateGlyph(Transform parent, string zinputKey, Vector2 anchor, Vector2 offset)
        {
            if (parent == null)
            {
                return null;
            }

            GameObject hint = new GameObject(zinputKey, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rectTransform = (RectTransform)hint.transform;
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(GlyphSize, GlyphSize);
            rectTransform.anchoredPosition = offset;

            TMP_Text text = hint.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 20;
            text.raycastTarget = false;

            hint.SetActive(false);
            return text;
        }

        private void RefreshHints(bool gamepadActive)
        {
            if (!_fontsApplied)
            {
                ApplyFonts();
            }

            for (int i = 0; i < _lists.Count; ++i)
            {
                IMerchantListPanel list = _lists[i];
                ListHints hints = _hints[i];
                bool focused = gamepadActive && i == _focusedIndex;

                SetHint(hints.Prev, focused);
                // Unfocused, the glyph on the first list reads as "press this to come here".
                SetHint(hints.Next, focused
                    ? FindList(i, 1) != Unfocused
                    : gamepadActive && !IsFocused && i == FindList(Unfocused, 1));

                SetHint(hints.MainAction, focused && IsUsable(list.GetMainButton()));
                SetHint(hints.SecondaryAction, focused && IsUsable(list.GetSecondaryButton()));
            }
        }

        private static bool IsUsable(Button button)
        {
            return button != null && button.gameObject.activeInHierarchy && button.IsInteractable();
        }

        private static void SetHint(TMP_Text hint, bool visible)
        {
            if (hint == null)
            {
                return;
            }

            if (visible)
            {
                hint.text = ZInput.instance.GetBoundKeyString(hint.name, true);
            }

            if (hint.gameObject.activeSelf != visible)
            {
                hint.gameObject.SetActive(visible);
            }
        }

        // Only latched on success: the font assets are found among loaded objects, so a lookup that
        // missed because nothing had loaded them yet gets another go instead of sticking on the default.
        private void ApplyFonts()
        {
            bool applied = true;
            foreach (ListHints hints in _hints)
            {
                applied &= ApplyFont(hints.Prev);
                applied &= ApplyFont(hints.Next);
                applied &= ApplyFont(hints.MainAction);
                applied &= ApplyFont(hints.SecondaryAction);
            }

            _fontsApplied = applied;
        }

        private static bool ApplyFont(TMP_Text text)
        {
            return text == null || MagicFontManager.Apply(text, MagicFontManager.TMP_FontOptions.AveriaSansLibre);
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.UpdateRecipeGamepadInput))]
        private static class StoreGui_UpdateRecipeGamepadInput_Patch
        {
            [UsedImplicitly]
            private static bool Prefix()
            {
                MerchantPanel panel = MerchantPanel.Instance;
                return panel == null || !panel.gameObject.activeSelf || panel.Gamepad == null ||
                    !panel.Gamepad.IsFocused;
            }
        }

        // StoreGui.Update turns B, Escape and Use into a Hide() of the whole store, and it does it
        // before any MonoBehaviour Update could answer for a dialog sitting on top of the panel. A
        // prefix always runs first, and skipping the frame outright is the only way to swallow an
        // Escape -- ZInput.ResetButtonStatus cannot consume a raw key.
        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Update))]
        private static class StoreGui_Update_Patch
        {
            [UsedImplicitly]
            private static bool Prefix()
            {
                MerchantPanel panel = MerchantPanel.Instance;
                if (panel == null || !panel.gameObject.activeSelf || panel.Gamepad == null)
                {
                    return true;
                }

                return !panel.Gamepad.UpdateDialogs();
            }
        }
    }
}
