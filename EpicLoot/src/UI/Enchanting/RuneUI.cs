using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.Compatibility;
using EpicLoot.CraftingV2;
using TMPro;
using EpicLoot.LegendarySystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class RuneUI : EnchantingTableUIPanelBase
    {
        public Toggle RuneExtractButton;
        public Toggle RuneEtchButton;

        [Header("Cost")]
        public TMP_Text CostLabel;
        public MultiSelectItemList CostList;

        [Header("Rune Selector")]
        public RectTransform EnchantList;
        public GameObject EnchantmentListPrefab;
        public GameObject AvailableRunesWindow;
        public MultiSelectItemList AvailableRunes;

        public TMP_Text Warning;

        public AudioClip RunicActionCompleted;

        public Toggle SetEtchButton;
        public Toggle SetExtractButton;
        public TMP_Text EnchantHeader;
        public TMP_Text RunesHeader;
        public ScrollRectEnsureVisible EnchantListEnsureVisible;
        // A row of the effect list's kind with its checkbox hidden, for the set modes' read-only lines.
        public GameObject InfoRowPrefab;

        private readonly List<EnchantmentRow> _enchantmentRows = new List<EnchantmentRow>();
        private EnchantmentColumn _enchantmentColumn;
        // The frame a rebuild asked for the focused row to be scrolled into view, or -1. Served on a later
        // frame, once the layout has placed the new rows.
        private int _focusScrollRequestFrame = -1;
        private RuneActions _runeAction;
        private GameObject _successDialog;
        private ItemDrop.ItemData _selectedItem;
        private ItemRarity _selectedRarity = ItemRarity.Magic;
        private int _selectedEnchantmentIndex = -1;

        // Every mode toggle with the action it selects, in the column's visual order (gamepad Y order).
        private readonly List<ModeToggle> _modes = new List<ModeToggle>();

        // What the prefab shipped in the two column titles, restored for the effect modes.
        private string _enchantHeaderDefault;
        private string _runesHeaderDefault;

        private struct ModeToggle
        {
            public Toggle Toggle;
            public RuneActions Action;
        }

        private bool IsSetMode => _runeAction == RuneActions.SetExtract || _runeAction == RuneActions.SetEtch;

        // Whether the current mode rewrites the selected item, rather than taking it or leaving it be. Such
        // an item has to sit where that change is saved (InventoryManagement.IsEditable).
        private bool EditsSelectedItem()
        {
            switch (_runeAction)
            {
                case RuneActions.Extract:
                    return EnchantingUIController.RuneExtractEditsItem();
                case RuneActions.SetExtract:
                    return EnchantingUIController.SetExtractEditsItem();
                default:
                    return true;
            }
        }

        private class EnchantmentRow
        {
            public Toggle Toggle;
            public EffectRowFocus Focus;
            // False for an effect the rune tab may not touch (CanBeRunified off, or no definition), so
            // Unlock knows to leave that row disabled.
            public bool Selectable;
        }

        // The enchantment rows are plain toggles with no navigation and no focus visuals of their own, so
        // the column rides the bumper rotation as a pane of its own, between the item list and the runes.
        private class EnchantmentColumn : IGamepadFocusPane
        {
            private readonly RuneUI _owner;
            private bool _focused;
            private int _focusIndex = -1;

            public EnchantmentColumn(RuneUI owner)
            {
                _owner = owner;
            }

            public int GetItemCount() => _owner._enchantmentRows.Count;
            public int GetFocusedIndex() => _focusIndex;
            public bool IsGrid() => false;
            public bool ShowSortHint => false;
            public bool ShowSelectAllHint => false;
            public bool ShowSelectHint => _focusIndex >= 0;

            public void GiveFocus(bool focused, int tryFocusIndex)
            {
                _focused = focused;
                int count = GetItemCount();
                _focusIndex = focused && count > 0 ? Mathf.Clamp(tryFocusIndex, 0, count - 1) : -1;
                _owner.RefreshEnchantmentFocus();
                _owner.ScrollToFocusedRow();
            }

            // The rows are rebuilt whenever the selected item changes, which empties the column for a
            // moment; without this the pane keeps the rotation's focus but loses its own row, and the
            // stick and A stop answering until the player bumpers away and back.
            public void ClampFocus()
            {
                int count = GetItemCount();
                _focusIndex = _focused && count > 0 ? Mathf.Clamp(Mathf.Max(_focusIndex, 0), 0, count - 1) : -1;
            }

            public void MoveFocus(int step)
            {
                int count = GetItemCount();
                if (_focusIndex < 0 || count == 0)
                {
                    return;
                }

                _focusIndex = Mathf.Clamp(_focusIndex + step, 0, count - 1);
                _owner.RefreshEnchantmentFocus();
                _owner.ScrollToFocusedRow();
            }

            public void SubmitFocused()
            {
                if (_focusIndex < 0 || _focusIndex >= GetItemCount())
                {
                    return;
                }

                Toggle toggle = _owner._enchantmentRows[_focusIndex].Toggle;
                if (toggle != null && toggle.isActiveAndEnabled && toggle.interactable)
                {
                    // Flipped rather than forced on, so A clears a row the way a click does. The group
                    // allows switching off; where it does not, Unity puts the toggle straight back on.
                    toggle.isOn = !toggle.isOn;
                }
            }
        }

        public override void Awake()
        {
            _enchantmentColumn = new EnchantmentColumn(this);

            base.Awake();

            // Only the toggle turning on selects: a group change also turns the previous one off.
            RegisterMode(RuneEtchButton, RuneActions.Etch);
            RegisterMode(RuneExtractButton, RuneActions.Extract);
            RegisterMode(SetEtchButton, RuneActions.SetEtch);
            RegisterMode(SetExtractButton, RuneActions.SetExtract);
            _modes.Sort((a, b) => a.Toggle.transform.GetSiblingIndex().CompareTo(b.Toggle.transform.GetSiblingIndex()));

            _enchantHeaderDefault = EnchantHeader.text;
            _runesHeaderDefault = RunesHeader.text;

            AvailableRunes.OnSelectedItemsChanged += OnSelectedOverrideRuneChanged;

            MultiSelectListFocusController focusController = GetComponent<MultiSelectListFocusController>();
            if (focusController != null)
            {
                // Visual order, minus the cost list the prefab includes: it is read-only, so a stop there
                // only costs the player a bumper press.
                focusController.SetPanes(new IGamepadFocusPane[] { AvailableItems, _enchantmentColumn, AvailableRunes });
            }
        }

        // Keeps the gamepad's row in view, the way the item and rune lists keep theirs.
        private void ScrollToFocusedRow()
        {
            int focusIndex = _enchantmentColumn.GetFocusedIndex();
            if (!ZInput.IsGamepadActive() ||
                focusIndex < 0 || focusIndex >= _enchantmentRows.Count)
            {
                return;
            }

            Toggle toggle = _enchantmentRows[focusIndex].Toggle;
            if (toggle != null)
            {
                EnchantListEnsureVisible.CenterOnItem((RectTransform)toggle.transform);
            }
        }

        private void RegisterMode(Toggle toggle, RuneActions action)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    SelectMode(action);
                }
            });
            _modes.Add(new ModeToggle { Toggle = toggle, Action = action });
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            // A config without any set has nothing to extract or etch, so its modes are not offered.
            bool anySet = UniqueLegendaryHelper.AllSets.Count > 0;
            if (SetEtchButton != null)
            {
                SetEtchButton.gameObject.SetActive(anySet);
            }

            if (SetExtractButton != null)
            {
                SetExtractButton.gameObject.SetActive(anySet);
            }

            foreach (ModeToggle mode in _modes)
            {
                mode.Toggle.SetIsOnWithoutNotify(mode.Toggle == RuneEtchButton);
            }

            SelectMode(RuneActions.Etch);
        }

        public override void Update()
        {
            base.Update();

            // Between death and respawn the local player is null for a few frames while this panel
            // is still active -- bail instead of NRE-ing (EnchantingTableUI.Update closes the UI).
            if (Player.m_localPlayer == null || EnchantingTableUI.instance == null ||
                EnchantingTableUI.instance.SourceTable == null)
            {
                return;
            }

            bool featureUnlocked = EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.Rune);
            if (!featureUnlocked && !Player.m_localPlayer.NoCostCheat())
            {
                return;
            }

            if (_successDialog != null && !_successDialog.activeSelf)
            {
                Unlock();
                Destroy(_successDialog);
                _successDialog = null;
            }

            if (ZInput.IsGamepadActive())
            {
                ClearToggleUISelection();
            }

            if (!_locked && ZInput.IsGamepadActive())
            {
                UpdateEnchantmentColumnInput();
            }

            RefreshEnchantmentFocus();

            if (_focusScrollRequestFrame >= 0 && Time.frameCount > _focusScrollRequestFrame)
            {
                _focusScrollRequestFrame = -1;
                ScrollToFocusedRow();
            }

            if (!_locked && ZInput.IsGamepadActive() && ZInput.GetButtonDown("JoyButtonY"))
            {
                ZInput.ResetButtonStatus("JoyButtonY");
                CycleMode();
            }
        }

        // Steps through the registered modes rather than the ToggleGroup: the group also holds
        // ModeImbueButton, which the prefab ships deactivated, and the set modes may be hidden.
        private void CycleMode()
        {
            List<ModeToggle> available = _modes
                .Where(x => x.Toggle.gameObject.activeInHierarchy && x.Toggle.interactable)
                .ToList();
            if (available.Count == 0)
            {
                return;
            }

            int current = available.FindIndex(x => x.Action == _runeAction);
            available[(current + 1) % available.Count].Toggle.isOn = true;
        }

        // The gamepad's A is bound to Unity's Submit axis as well as to JoyButtonA, so any toggle left
        // selected in the EventSystem re-fires the moment the player presses A elsewhere in the panel and
        // drags the mode or the enchantment back. Every toggle here is driven by Y or by the enchantment
        // column's own focus, never by EventSystem navigation.
        private void ClearToggleUISelection()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(transform) &&
                selected.GetComponent<Toggle>() != null)
            {
                eventSystem.SetSelectedGameObject(null);
            }
        }

        private void UpdateEnchantmentColumnInput()
        {
            if (_enchantmentColumn.GetFocusedIndex() < 0)
            {
                return;
            }

            // Deliberately no ZInput.ResetButtonStatus on the stick: a reset clears the held state ZInput's
            // own key repeat runs off, which costs a held stick every repeat past the first.
            if (ZInput.GetButtonDown("JoyLStickUp"))
            {
                _enchantmentColumn.MoveFocus(-1);
            }
            else if (ZInput.GetButtonDown("JoyLStickDown"))
            {
                _enchantmentColumn.MoveFocus(1);
            }
            else if (ZInput.GetButtonDown("JoyButtonA"))
            {
                ZInput.ResetButtonStatus("JoyButtonA");
                _enchantmentColumn.SubmitFocused();
            }
        }

        private void RefreshEnchantmentFocus()
        {
            int focusIndex = _enchantmentColumn.GetFocusedIndex();
            bool gamepadActive = ZInput.IsGamepadActive();

            for (int index = 0; index < _enchantmentRows.Count; ++index)
            {
                EnchantmentRow row = _enchantmentRows[index];
                row.Focus.SetGlow(gamepadActive ? index == focusIndex : row.Focus.Hovered);
            }
        }

        public void UpdateDisplaySelectedItemEnchantments()
        {
            if (_selectedItem == null)
            {
                MainButton.interactable = false;
                return;
            }

            if (IsSetMode)
            {
                // Set modes have no effect to pick: the middle column previews the set instead.
                if (_runeAction == RuneActions.SetEtch)
                {
                    RefreshSetRunes();
                }

                RefreshSetPreview();
            }
            else
            {
                RefreshSelectableEnchantments();
                UpdateDisplayAvailableOverwriteEnchantments();
            }

            CostLabel.enabled = true;
            CostList.SetItems(GetCostDisplay(GetCurrentCost(_selectedItem)));

            CheckIfActionDoable();
        }

        private float GetCurrentCostReduction()
        {
            Tuple<float, float> featureValues =
                EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Rune);
            return GetCostReduction(featureValues.Item1);
        }

        private float GetCurrentPowerModifier()
        {
            Tuple<float, float> featureValues =
                EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Rune);
            return GetPowerModifier(featureValues.Item2);
        }

        // What the current mode charges for the item, keyed on its rarity: the source's for an extract,
        // the target's for an etch.
        private List<InventoryItemListElement> GetCurrentCost(ItemDrop.ItemData item)
        {
            float costReduction = GetCurrentCostReduction();
            switch (_runeAction)
            {
                case RuneActions.Extract:
                    return EnchantingUIController.GetRuneExtractCost(item, _selectedRarity, costReduction);
                case RuneActions.Etch:
                    return EnchantingUIController.GetRuneEtchCost(item, _selectedRarity, costReduction);
                case RuneActions.SetExtract:
                    return EnchantingUIController.GetRuneSetExtractCost(item, _selectedRarity, costReduction);
                case RuneActions.SetEtch:
                    return EnchantingUIController.GetRuneSetEtchCost(item, _selectedRarity, costReduction);
                default:
                    return new List<InventoryItemListElement>();
            }
        }

        // The cost grid: the materials, led in Set Etch by the set rune it consumes. The rune is shown
        // only -- it is never part of the affordability check or the by-name payment, since it is taken
        // as that exact item.
        private List<IListElement> GetCostDisplay(List<InventoryItemListElement> cost)
        {
            List<IListElement> display = new List<IListElement>();
            ItemDrop.ItemData rune = _runeAction == RuneActions.SetEtch ? GetSelectedRune() : null;
            if (rune != null)
            {
                ItemDrop.ItemData shown = rune.Clone();
                shown.m_stack = 1;
                display.Add(new InventoryItemListElement { Item = shown });
            }

            display.AddRange(cost);
            return display;
        }

        private ItemDrop.ItemData GetSelectedRune()
        {
            return AvailableRunes.GetSingleSelectedItem<InventoryItemListElement>()?.Item1.GetItem();
        }

        // Set Etch: the set runes the player owns that fit the selected item.
        private void RefreshSetRunes()
        {
            List<InventoryItemListElement> runes = _selectedItem != null
                ? EnchantingUIController.GetApplyableSetRunesForItem(_selectedItem)
                : new List<InventoryItemListElement>();
            AvailableRunes.SetItems(runes.Cast<IListElement>().ToList());
        }

        // The middle column in the set modes: read-only lines describing what the action will do.
        private void RefreshSetPreview()
        {
            ClearEnchantmentList();
            if (_selectedItem == null)
            {
                return;
            }

            string setColor = EpicLoot.EpicLoot.GetSetItemColor();
            if (_runeAction == RuneActions.SetExtract)
            {
                if (!EnchantingUIController.TryGetSetExtractSource(_selectedItem, out LegendarySetInfo set))
                {
                    return;
                }

                MagicItem magicItem = _selectedItem.GetMagicItem();
                AddInfoRow($"<color={setColor}>{set.Name}</color>");
                AddInfoRow($"<color={magicItem.GetColorString()}>{_selectedItem.GetDisplayName()}</color>");
            }
            else if (_runeAction == RuneActions.SetEtch)
            {
                ItemDrop.ItemData rune = GetSelectedRune();
                if (rune == null ||
                    !EnchantingUIController.TryResolveSetEtch(_selectedItem, rune, out LegendarySetInfo set, out LegendaryInfo piece))
                {
                    AddInfoRow("<color=#c0c0c0ff>$mod_epicloot_rune_set_selectrune</color>");
                    return;
                }

                AddInfoRow($"<color={setColor}>{set.Name}</color>");
                AddInfoRow(Localization.instance.Localize("$mod_epicloot_rune_set_becomes",
                    $"<color={EpicLoot.EpicLoot.GetRarityColor(_selectedRarity)}>{Localization.instance.Localize(piece.Name)}</color>"));
            }
        }

        // One line of text in the enchantment column, built from the row prefab with its checkbox hidden.
        // Not tracked in _enchantmentRows, so gamepad focus and Lock/Unlock never see it; ClearEnchantmentList
        // still destroys it with everything else under EnchantList.
        private void AddInfoRow(string text)
        {
            GameObject row = Instantiate(InfoRowPrefab, EnchantList);
            row.GetComponentInChildren<TMP_Text>(true).text = Localization.instance.Localize(text);
            row.SetActive(true);
        }

        public void UpdateDisplayAvailableOverwriteEnchantments()
        {
            if (_selectedItem == null || _runeAction != RuneActions.Etch || _selectedEnchantmentIndex <= -1)
            {
                AvailableRunes.SetItems(new List<IListElement>());
                MainButton.interactable = false;
                return;
            }

            string selectedEffect = EnchantingUIController.GetSelectedEnchantmentNameByIndex(_selectedItem, _selectedEnchantmentIndex);
            List<InventoryItemListElement> runes = EnchantingUIController.GetApplyableRunesforItem(_selectedItem, selectedEffect);
            runes.AddRange(EnchantingUIController.GetBlockedRunesForItem(_selectedItem, selectedEffect, runes));
            AvailableRunes.SetItems(runes.Cast<IListElement>().ToList());
        }

        private void ClearEnchantmentList()
        {
            if (EnchantList.childCount > 0)
            {
                foreach (Transform child in EnchantList)
                {
                    Destroy(child.gameObject);
                }
            }
            _enchantmentRows.Clear();
            _enchantmentColumn.ClampFocus();
            _selectedEnchantmentIndex = -1;

            // Back to the top for the next item or mode.
            EnchantList.anchoredPosition = new Vector2(EnchantList.anchoredPosition.x, 0f);
        }

        private void RefreshSelectableEnchantments()
        {
            Tuple<InventoryItemListElement, int> entry = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            ItemDrop.ItemData item = entry?.Item1.GetItem();
            List<Tuple<string, bool>> augmentableEffects = EnchantingUIController.GetEnchantmentEffects(item, true);

            ClearEnchantmentList();

            float powerModifier = _runeAction == RuneActions.Extract ? GetCurrentPowerModifier() : 1f;
            for (int effectIndex = 0; effectIndex < augmentableEffects.Count; effectIndex++)
            {
                Tuple<string, bool> effect = augmentableEffects[effectIndex];
                GameObject enchantmentListElement = Instantiate(EnchantmentListPrefab, EnchantList);
                // Include inactive: the prefab ships deactivated, so the clone is still inactive here and
                // the plain overload would hand back null and leave every row reading "Enchant Selector".
                TMP_Text enchantmentElement = enchantmentListElement.GetComponentInChildren<TMP_Text>(true);
                Toggle enchantmentbutton = enchantmentListElement.GetComponent<Toggle>();
                enchantmentbutton.onValueChanged.AddListener((isOn) =>
                {
                    SetSelectedEnchantIndex();
                    RefreshWarning();
                    UpdateDisplayAvailableOverwriteEnchantments();
                    CheckIfActionDoable();
                });

                if (enchantmentElement != null)
                {
                    enchantmentElement.text = effect.Item1;
                    if (_runeAction == RuneActions.Extract &&
                        EnchantingUIController.TryGetCappedRuneExtractText(item, effectIndex, powerModifier, out string cappedText))
                    {
                        enchantmentElement.text += "\n<size=80%><color=#FFB347>" +
                            Localization.instance.Localize("$mod_epicloot_rune_extract_capped_row", cappedText) + "</color></size>";
                    }
                    MagicTextShimmer.Ensure(enchantmentElement);
                }

                // An effect with CanBeRunified off is not selectable, not just dimmed. Same treatment as
                // the augment tab.
                bool selectable = effect.Item2;
                enchantmentbutton.interactable = selectable && !_locked;

                enchantmentListElement.SetActive(true);

                _enchantmentRows.Add(new EnchantmentRow
                {
                    Toggle = enchantmentbutton,
                    Focus = enchantmentListElement.GetComponent<EffectRowFocus>(),
                    Selectable = selectable
                });
            }

            _enchantmentColumn.ClampFocus();
            RefreshEnchantmentFocus();
            RefreshWarning();

            // ClampFocus keeps the old row index, which may now sit below the fold of a list that just
            // went back to the top.
            _focusScrollRequestFrame = Time.frameCount;

            // The rune column lists runes for one chosen enchantment, so with none chosen it stays empty.
            if (item != null && !_locked)
            {
                EnchantmentRow first = _enchantmentRows.FirstOrDefault(row => row.Selectable);
                if (first != null)
                {
                    first.Toggle.isOn = true;
                }
            }
        }

        // Indexed off the tracked rows, not off EnchantList: a rebuild leaves the previous rows parented
        // there until their Destroy lands at the end of the frame, which shifts every index along.
        private void SetSelectedEnchantIndex()
        {
            for (int index = 0; index < _enchantmentRows.Count; ++index)
            {
                Toggle toggle = _enchantmentRows[index].Toggle;
                if (toggle != null && toggle.isOn)
                {
                    _selectedEnchantmentIndex = index;
                    return;
                }
            }

            _selectedEnchantmentIndex = -1;
        }

        private bool LocalPlayerCanAffordRuneCost(List<InventoryItemListElement> cost)
        {
            return LocalPlayerCanAffordCostOrNone(cost);
        }

        private void SelectMode(RuneActions action)
        {
            _runeAction = action;
            RefreshMainButtonLabel();
            RefreshWarning();

            // The rune column is only for the two etches; whatever it held belongs to the previous mode.
            AvailableRunesWindow.SetActive(action == RuneActions.Etch || action == RuneActions.SetEtch);
            if (AvailableRunes.GetItemCount() > 0)
            {
                AvailableRunes.SetItems(new List<IListElement>());
            }

            RefreshColumnHeaders();
            NewModeSelected(true);
        }

        private void RefreshWarning()
        {
            string text = Localization.instance.Localize(GetWarningKey(_runeAction));
            if (_runeAction == RuneActions.Extract && _selectedItem != null &&
                EnchantingUIController.TryGetCappedRuneExtractText(_selectedItem, _selectedEnchantmentIndex, GetCurrentPowerModifier(), out string cappedText))
            {
                text += " <color=#FFB347>" + Localization.instance.Localize("$mod_epicloot_rune_extract_capped_warning",
                    Localization.instance.Localize(EpicLoot.EpicLoot.GetRarityDisplayName(_selectedItem.GetRarity())), cappedText) + "</color>";
            }

            Warning.text = text;
        }

        private static string GetWarningKey(RuneActions action)
        {
            switch (action)
            {
                case RuneActions.Extract:
                    return EnchantingUIController.GetRuneExtractWarningKey();
                case RuneActions.SetExtract:
                    return EnchantingUIController.GetRuneSetExtractWarningKey();
                case RuneActions.SetEtch:
                    return "$mod_epicloot_rune_setetch_warning";
                case RuneActions.Etch:
                default:
                    return "$mod_epicloot_rune_etch_warning";
            }
        }

        private void RefreshColumnHeaders()
        {
            switch (_runeAction)
            {
                case RuneActions.SetExtract:
                    SetHeader(EnchantHeader, _enchantHeaderDefault, "$mod_epicloot_rune_set_preview");
                    break;
                case RuneActions.SetEtch:
                    SetHeader(EnchantHeader, _enchantHeaderDefault, "$mod_epicloot_rune_set_result");
                    SetHeader(RunesHeader, _runesHeaderDefault, "$mod_epicloot_rune_set_runes_header");
                    break;
                default:
                    SetHeader(EnchantHeader, _enchantHeaderDefault, null);
                    SetHeader(RunesHeader, _runesHeaderDefault, null);
                    break;
            }
        }

        // A null token puts back what the prefab shipped. Auga upper-cases these titles, so a replacement
        // follows the shipped text's casing.
        private static void SetHeader(TMP_Text header, string shipped, string token)
        {
            if (header == null)
            {
                return;
            }

            if (token == null)
            {
                header.text = shipped;
                return;
            }

            string text = Localization.instance.Localize(token);
            bool upperCase = !string.IsNullOrEmpty(shipped) && shipped.Any(char.IsLetter) && shipped == shipped.ToUpperInvariant();
            header.text = upperCase ? text.ToUpperInvariant() : text;
        }

        private void NewModeSelected(bool enabled)
        {
            RefreshAvailableItems();
            _selectedEnchantmentIndex = -1;

            if (!enabled)
            {
                MainButton.interactable = false;
                return;
            }

            Tuple<InventoryItemListElement, int> selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();

            if (selectedItem?.Item1.GetItem() == null)
            {
                CostLabel.enabled = false;
                CostList.SetItems(new List<IListElement>());
                AvailableRunes.SetItems(new List<IListElement>());
                MainButton.interactable = false;
                return;
            }
            else
            {
                if (selectedItem?.Item1.GetItem() != _selectedItem)
                {
                    _selectedItem = selectedItem.Item1.GetItem();
                    _selectedRarity = _selectedItem.GetRarity();
                }

                UpdateDisplaySelectedItemEnchantments();
            }

            bool featureUnlocked = EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.Rune);

            if (!featureUnlocked)
            {
                MainButton.interactable = featureUnlocked;
            }
        }

        protected override void DoMainAction()
        {
            Tuple<InventoryItemListElement, int> selectedItem = AvailableItems.GetSelectedItems<InventoryItemListElement>().FirstOrDefault();

            Cancel();

            if (selectedItem?.Item1.GetItem() == null)
            {
                return;
            }

            Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Rune);
            float costReduction = GetCostReduction(featureValues.Item1);
            float powerModifier = GetPowerModifier(featureValues.Item2);
            ItemDrop.ItemData item = selectedItem.Item1.GetItem();

            // Everything below acts on the selection as it stands when the countdown ends, so check it
            // still describes an item the player holds (somewhere that saves the change, for a mode that
            // rewrites it) and, for the effect modes, an effect the rune tab may touch (and the one the
            // cost was shown for). The set modes re-check their own rules.
            bool stillHeld = EditsSelectedItem()
                ? InventoryManagement.Instance.IsEditable(item)
                : InventoryManagement.Instance.GetAllItems().Contains(item);
            if (item != _selectedItem || !stillHeld ||
                (!IsSetMode && !EnchantingUIController.CanRunifyEffect(item.GetMagicItem(), _selectedEnchantmentIndex)))
            {
                AbortMainAction("the selected item or enchantment is no longer valid", unavailable: stillHeld ? null : item);
                return;
            }

            switch (_runeAction)
            {
                case RuneActions.Extract:
                    ExtractSelectedEnchantment(item, costReduction, powerModifier);
                    break;
                case RuneActions.Etch:
                    EtchSelectedRune(item, costReduction);
                    break;
                case RuneActions.SetExtract:
                    ExtractSelectedSet(item, costReduction);
                    break;
                case RuneActions.SetEtch:
                    EtchSelectedSetRune(item, costReduction);
                    break;
            }
        }

        // Each action calls this once its payment went through and its result is applied.
        private void FinishMainAction()
        {
            DeselectAll();

            RefreshAvailableItems();
            _selectedEnchantmentIndex = -1;
            CostList.SetItems(new List<IListElement>());
            AvailableRunes.SetItems(new List<IListElement>());
        }

        private void PaymentFailed()
        {
            AbortMainAction("the cost or the item could not be taken");
        }

        private void ExtractSelectedEnchantment(ItemDrop.ItemData item, float costReduction, float powerModifier)
        {
            // Favorited while the table was open. Only the modes that keep the item may still extract.
            if (EnchantingUIController.GetRuneExtractMode() != RuneExtractMode.KeepItem && ItemFavorites.IsProtected(item))
            {
                AbortMainAction("the item is favorited");
                return;
            }

            List<InventoryItemListElement> cost = EnchantingUIController.GetRuneExtractCost(item, _selectedRarity, costReduction);
            ItemDrop.ItemData RuneWithEnchant = EnchantingUIController.BuildEnchantedRune(item, _selectedEnchantmentIndex, powerModifier);

            if (RuneWithEnchant == null)
            {
                AbortMainAction("the rune could not be built");
                return;
            }

            Player player = Player.m_localPlayer;
            bool noCost = player.NoCostCheat();
            if (!noCost && !LocalPlayerCanAffordCost(cost))
            {
                AbortMainAction("the cost can no longer be paid", missingRequirements: true);
                return;
            }

            RuneExtractMode mode = EnchantingUIController.GetRuneExtractMode();
            List<InventoryItemListElement> reclaimedSockets = null;
            InventoryManagement.Payment payment = new InventoryManagement.Payment();
            if (mode == RuneExtractMode.DestroyItem)
            {
                // Socketed stones are the player's property: the non-Locked ones are handed back once
                // the item is gone, the same policy disenchanting uses. Read before it goes.
                if (item.IsMagic(out MagicItem extractedMagicItem) && extractedMagicItem.Sockets.Count > 0)
                {
                    reclaimedSockets = EnchantingUIController.ReclaimSockets(extractedMagicItem);
                }

                // Vanilla never auto-unequips a removed item: destroying an equipped piece (listed when
                // ShowEquippedAndHotbarItemsInSacrificeTab is on) would leave its stats and visuals.
                if (player.IsItemEquiped(item))
                {
                    player.UnequipItem(item, false);
                }

                payment.Exact(item, 1);
            }

            int effectIndex = _selectedEnchantmentIndex;
            PayThen(payment.Cost(cost), receipt =>
            {
                bool reduces = mode == RuneExtractMode.ReduceEnchants || mode == RuneExtractMode.ReduceEnchantsAndRarity;
                if (reduces && !InventoryManagement.Instance.IsEditable(item))
                {
                    receipt.Refund();
                    AbortMainAction("the item moved before it could be changed", unavailable: item);
                    return;
                }

                if (reduces)
                {
                    EnchantingUIController.ReduceItemAfterRuneExtract(item, effectIndex,
                        reduceRarity: mode == RuneExtractMode.ReduceEnchantsAndRarity);
                }

                if (reclaimedSockets != null)
                {
                    GiveItemsToPlayer(reclaimedSockets);
                }

                InventoryManagement.Instance.GiveItem(RuneWithEnchant);
                FinishMainAction();
            }, PaymentFailed);
        }

        // Modifies the existing item and consumes the selected rune. The etch is worked out on a copy
        // first; the rune and then the cost are taken, and only then is the result written to the item,
        // so a failure at any step leaves the item, the rune and the materials where they were.
        private void EtchSelectedRune(ItemDrop.ItemData item, float costReduction)
        {
            ItemDrop.ItemData rune = AvailableRunes.GetSingleSelectedItem<InventoryItemListElement>()?.Item1.GetItem();
            string targetEffect = EnchantingUIController.GetSelectedEnchantmentNameByIndex(item, _selectedEnchantmentIndex);
            bool runeHeld = rune != null && InventoryManagement.Instance.GetAllItems().Contains(rune);
            if (!runeHeld || rune == item ||
                !EnchantingUIController.GetApplyableRunesforItem(item, targetEffect).Any(x => x.GetItem() == rune))
            {
                AbortMainAction("the selected rune is no longer available for this enchantment", unavailable: runeHeld ? null : rune);
                return;
            }

            // The same cost CheckIfActionDoable showed and gated the button on.
            List<InventoryItemListElement> cost = EnchantingUIController.GetRuneEtchCost(item, _selectedRarity, costReduction);
            bool noCost = Player.m_localPlayer.NoCostCheat();
            if (!noCost && !LocalPlayerCanAffordCost(cost))
            {
                AbortMainAction("the cost can no longer be paid", missingRequirements: true);
                return;
            }

            MagicItem etched = EnchantingUIController.BuildRuneEtchResult(item, rune, _selectedEnchantmentIndex);
            if (etched == null)
            {
                AbortMainAction("the etch could not be applied");
                return;
            }

            PayThen(new InventoryManagement.Payment().Exact(rune, 1).Cost(cost), receipt =>
            {
                if (!InventoryManagement.Instance.IsEditable(item))
                {
                    receipt.Refund();
                    AbortMainAction("the item moved before it could be changed", unavailable: item);
                    return;
                }

                EnchantingUIController.ApplyRuneEtch(item, etched);

                if (_successDialog != null)
                {
                    Destroy(_successDialog);
                }

                _successDialog = EnchantingUIController.ShowRuneEtchSuccessDialog(item);
                _successDialog.SetActive(true);
                FinishMainAction();
            }, PaymentFailed);
        }

        // Turns the item's set into a set rune. The same order as an effect extract: everything that can
        // fail is checked, and a destroyed item is taken, before anything is charged or handed out.
        private void ExtractSelectedSet(ItemDrop.ItemData item, float costReduction)
        {
            // Favorited while the table was open; every set extract mode strips or destroys the item.
            if (ItemFavorites.IsProtected(item))
            {
                AbortMainAction("the item is favorited");
                return;
            }

            List<InventoryItemListElement> cost = EnchantingUIController.GetRuneSetExtractCost(item, _selectedRarity, costReduction);
            ItemDrop.ItemData setRune = EnchantingUIController.BuildSetRune(item);
            if (setRune == null)
            {
                AbortMainAction("the item is no longer a piece of a known set, or the set rune could not be built");
                return;
            }

            Player player = Player.m_localPlayer;
            bool noCost = player.NoCostCheat();
            if (!noCost && !LocalPlayerCanAffordCost(cost))
            {
                AbortMainAction("the cost can no longer be paid", missingRequirements: true);
                return;
            }

            RuneSetExtractMode mode = EnchantingUIController.GetRuneSetExtractMode();
            List<InventoryItemListElement> reclaimedSockets = null;
            InventoryManagement.Payment payment = new InventoryManagement.Payment();
            if (mode == RuneSetExtractMode.DestroyItem)
            {
                // Socketed stones that are not Locked go back to the player, as for a destroying extract.
                if (item.IsMagic(out MagicItem extractedMagicItem) && extractedMagicItem.Sockets.Count > 0)
                {
                    reclaimedSockets = EnchantingUIController.ReclaimSockets(extractedMagicItem);
                }

                if (player.IsItemEquiped(item))
                {
                    player.UnequipItem(item, false);
                }

                payment.Exact(item, 1);
            }

            PayThen(payment.Cost(cost), receipt =>
            {
                if (mode == RuneSetExtractMode.StripSet)
                {
                    if (!InventoryManagement.Instance.IsEditable(item))
                    {
                        receipt.Refund();
                        AbortMainAction("the item moved before it could be changed", unavailable: item);
                        return;
                    }

                    EnchantingUIController.StripSetAfterExtract(item);
                }

                if (reclaimedSockets != null)
                {
                    GiveItemsToPlayer(reclaimedSockets);
                }

                InventoryManagement.Instance.GiveItem(setRune);
                FinishMainAction();
            }, PaymentFailed);
        }

        // Makes the item a piece of the selected set rune's set. Worked out on a copy first; the rune and
        // then the cost are taken, and only then is the result written, as for an effect etch.
        private void EtchSelectedSetRune(ItemDrop.ItemData item, float costReduction)
        {
            ItemDrop.ItemData rune = GetSelectedRune();
            bool runeHeld = rune != null && InventoryManagement.Instance.GetAllItems().Contains(rune);
            if (!runeHeld || rune == item ||
                !EnchantingUIController.GetApplyableSetRunesForItem(item).Any(x => x.GetItem() == rune))
            {
                AbortMainAction("the selected set rune is no longer available for this item", unavailable: runeHeld ? null : rune);
                return;
            }

            // The configured materials; the rune itself is taken below as that exact item.
            List<InventoryItemListElement> cost = EnchantingUIController.GetRuneSetEtchCost(item, _selectedRarity, costReduction);
            bool noCost = Player.m_localPlayer.NoCostCheat();
            if (!noCost && !LocalPlayerCanAffordCost(cost))
            {
                AbortMainAction("the cost can no longer be paid", missingRequirements: true);
                return;
            }

            MagicItem etched = EnchantingUIController.BuildSetEtchResult(item, rune);
            if (etched == null)
            {
                AbortMainAction("the set rune no longer fits this item");
                return;
            }

            PayThen(new InventoryManagement.Payment().Exact(rune, 1).Cost(cost), receipt =>
            {
                if (!InventoryManagement.Instance.IsEditable(item))
                {
                    receipt.Refund();
                    AbortMainAction("the item moved before it could be changed", unavailable: item);
                    return;
                }

                EnchantingUIController.ApplySetEtch(item, etched);

                if (_successDialog != null)
                {
                    Destroy(_successDialog);
                }

                _successDialog = EnchantingUIController.ShowRuneEtchSuccessDialog(item);
                _successDialog.SetActive(true);
                FinishMainAction();
            }, PaymentFailed);
        }

        // A main action that could not go ahead: nothing was taken or changed. The panel is already
        // unlocked (DoMainAction cancels first); rebuild the lists so they show what is really there
        // now, and let the button state follow the fresh selection.
        private void AbortMainAction(string reason, bool missingRequirements = false, ItemDrop.ItemData unavailable = null)
        {
            EpicLoot.EpicLoot.LogWarning($"[Rune] {_runeAction} cancelled: {reason}.");
            if (missingRequirements)
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
            }
            NotifyItemsUnavailable(unavailable);
            RefreshAvailableItems();
            _selectedEnchantmentIndex = -1;
            CostList.SetItems(new List<IListElement>());
            AvailableRunes.SetItems(new List<IListElement>());
            CheckIfActionDoable();
        }

        // base.Cancel restores _defaultButtonLabelText, which Awake captured from the prefab's shipped
        // "$mod_epicloot_rune_slot" label, so every finished action would relabel the button "Apply Rune".
        private void RefreshMainButtonLabel()
        {
            switch (_runeAction)
            {
                case RuneActions.Extract:
                    SetMainButtonLabel("$mod_epicloot_rune_extract");
                    break;
                case RuneActions.SetExtract:
                    SetMainButtonLabel("$mod_epicloot_rune_setextract_action");
                    break;
                case RuneActions.SetEtch:
                    SetMainButtonLabel("$mod_epicloot_rune_setetch_action");
                    break;
                default:
                    SetMainButtonLabel("$mod_epicloot_rune_etch");
                    break;
            }
        }

        private void SetMainButtonLabel(string token)
        {
            string text = Localization.instance.Localize(token);
            if (_buttonLabel != null)
            {
                _buttonLabel.text = text;
            }
        }

        protected override AudioClip GetCompleteAudioClip()
        {
            return RunicActionCompleted;
        }

        public void RefreshAvailableItems()
        {
            List<InventoryItemListElement> items;
            switch (_runeAction)
            {
                case RuneActions.Extract:
                    items = EnchantingUIController.GetRuneModifyableItems(false);
                    break;
                case RuneActions.Etch:
                    items = EnchantingUIController.GetRuneModifyableItems(true);
                    break;
                case RuneActions.SetExtract:
                    items = EnchantingUIController.GetRuneSetExtractItems();
                    break;
                case RuneActions.SetEtch:
                    items = EnchantingUIController.GetRuneSetEtchItems();
                    break;
                default:
                    items = new List<InventoryItemListElement>();
                    break;
            }

            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            if (IsSetMode)
            {
                ClearEnchantmentList();
            }
            else
            {
                RefreshSelectableEnchantments();
            }

            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
        }

        protected override void OnSelectedItemsChanged()
        {
            Tuple<InventoryItemListElement, int> selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            if (selectedItem?.Item1.GetItem() != null)
            {
                _selectedItem = selectedItem.Item1.GetItem();
                _selectedRarity = _selectedItem.GetRarity();
                UpdateDisplaySelectedItemEnchantments();
            }
            else
            {
                ClearEnchantmentList();
                if (IsSetMode)
                {
                    // Nothing selected, nothing to preview or pay for.
                    _selectedItem = null;
                    CostList.SetItems(new List<IListElement>());
                    AvailableRunes.SetItems(new List<IListElement>());
                    MainButton.interactable = false;
                }
            }
        }

        protected void OnSelectedOverrideRuneChanged()
        {
            if (_runeAction == RuneActions.SetEtch)
            {
                RefreshSetPreview();
            }

            // Also on a deselect, so the button does not stay enabled for a rune no longer chosen.
            CheckIfActionDoable();
        }

        private void CheckIfActionDoable()
        {
            MainButton.interactable = IsActionDoable();
        }

        private bool IsActionDoable()
        {
            if (_selectedItem == null)
            {
                return false;
            }

            switch (_runeAction)
            {
                case RuneActions.Extract:
                case RuneActions.Etch:
                    if (_selectedEnchantmentIndex == -1 ||
                        !EnchantingUIController.CanRunifyEffect(_selectedItem.GetMagicItem(), _selectedEnchantmentIndex))
                    {
                        return false;
                    }
                    break;
                case RuneActions.SetExtract:
                    if (!EnchantingUIController.TryGetSetExtractSource(_selectedItem, out _))
                    {
                        return false;
                    }
                    break;
            }

            // Shown before the rune check, so selecting or clearing a Set Etch rune updates the grid.
            List<InventoryItemListElement> cost = GetCurrentCost(_selectedItem);
            CostList.SetItems(GetCostDisplay(cost));

            if (_runeAction == RuneActions.Etch || _runeAction == RuneActions.SetEtch)
            {
                // Read from the list rather than a cached field: SetItems drops the selection without
                // raising the list's change event.
                ItemDrop.ItemData rune = GetSelectedRune();
                if (rune == null)
                {
                    return false;
                }

                if (_runeAction == RuneActions.SetEtch &&
                    !EnchantingUIController.TryResolveSetEtch(_selectedItem, rune, out _, out _))
                {
                    return false;
                }
            }

            // Ignored in no-cost mode.
            return LocalPlayerCanAffordRuneCost(cost);
        }

        public override bool CanCancel()
        {
            return base.CanCancel() || (_successDialog != null && _successDialog.activeSelf);
        }

        public override void Cancel()
        {
            base.Cancel();
            RefreshMainButtonLabel();

            if (_successDialog != null && _successDialog.activeSelf)
            {
                Destroy(_successDialog);
                _successDialog = null;
            }
        }

        public override void Lock()
        {
            base.Lock();

            foreach (ModeToggle mode in _modes)
            {
                mode.Toggle.interactable = false;
            }

            MainButton.interactable = false;

            // The countdown acts on _selectedEnchantmentIndex when it ends; changing the row mid-way
            // would etch or extract a different effect from the one the cost was shown for.
            foreach (EnchantmentRow row in _enchantmentRows)
            {
                if (row.Toggle != null)
                {
                    row.Toggle.interactable = false;
                }
            }
        }

        public override void Unlock()
        {
            base.Unlock();

            foreach (ModeToggle mode in _modes)
            {
                mode.Toggle.interactable = true;
            }

            foreach (EnchantmentRow row in _enchantmentRows)
            {
                if (row.Toggle != null)
                {
                    row.Toggle.interactable = row.Selectable;
                }
            }
        }
    }
}
