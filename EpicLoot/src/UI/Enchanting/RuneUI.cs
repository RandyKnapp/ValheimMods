using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.CraftingV2;
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
        public Text CostLabel;
        public MultiSelectItemList CostList;

        [Header("Rune Selector")]
        public RectTransform EnchantList;
        public GameObject EnchantmentListPrefab;
        public GameObject AvailableRunesWindow;
        public MultiSelectItemList AvailableRunes;

        public Text Warning;

        public AudioClip RunicActionCompleted;


        private readonly List<EnchantmentRow> _enchantmentRows = new List<EnchantmentRow>();
        private EnchantmentColumn _enchantmentColumn;
        private GameObject _rowFocusTemplate;
        private RuneAction _runeAction;
        private GameObject _successDialog;
        private ItemDrop.ItemData _selectedItem;
        private ItemDrop.ItemData _selectedOverrideRune;
        private ItemRarity _selectedRarity = ItemRarity.Magic;
        private int _selectedEnchantmentIndex = -1;

        private enum RuneAction
        {
            Extract,
            Etch
        }

        private class EnchantmentRow
        {
            public Toggle Toggle;
            public GameObject FocusGlow;
            public RowHover Hover;
            // False for an effect the rune tab may not touch (CanBeRunified off, or no definition), so
            // Unlock knows to leave that row disabled.
            public bool Selectable;
        }

        private class RowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            public bool Hovered;

            public void OnPointerEnter(PointerEventData eventData)
            {
                Hovered = true;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                Hovered = false;
            }
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

            RuneExtractButton.onValueChanged.AddListener((isOn) =>
            {
                ExtractModeSelected(isOn);
            });

            RuneEtchButton.onValueChanged.AddListener((isOn) =>
            {
                EtchModeSelected(isOn);
            });

            AvailableRunes.OnSelectedItemsChanged += OnSelectedOverrideRuneChanged;

            _rowFocusTemplate = AvailableRunes != null && AvailableRunes.ElementPrefab != null
                ? AvailableRunes.ElementPrefab.GamepadFocusIndicator
                : null;

            MultiSelectListFocusController focusController = GetComponent<MultiSelectListFocusController>();
            if (focusController != null)
            {
                // Visual order, minus the cost list the prefab includes: it is read-only, so a stop there
                // only costs the player a bumper press.
                focusController.SetPanes(new IGamepadFocusPane[] { AvailableItems, _enchantmentColumn, AvailableRunes });
            }

            HideStrayModeHint();
        }

        // Cloned from the enchant tab, which parks a Y glyph beside its rarity column. Here it lands on top
        // of the mode selectors' own Y glyph -- two stacked glyphs, and nothing answers this one.
        private void HideStrayModeHint()
        {
            Transform strayHint = transform.Find("GamepadHints/Hint");
            if (strayHint != null)
            {
                strayHint.gameObject.SetActive(false);
            }
        }

        [UsedImplicitly]
        public void OnEnable()
        {
            RuneExtractButton.isOn = false;
            RuneEtchButton.isOn = true;
            EtchModeSelected(true);
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

            // Check if the action is completed, and unlock the UI
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

            if (!_locked && ZInput.IsGamepadActive() && ZInput.GetButtonDown("JoyButtonY"))
            {
                ZInput.ResetButtonStatus("JoyButtonY");

                // Named rather than cycled through the ToggleGroup: it also holds ModeImbueButton, which the
                // prefab ships deactivated.
                if (_runeAction == RuneAction.Etch)
                {
                    RuneExtractButton.isOn = true;
                }
                else
                {
                    RuneEtchButton.isOn = true;
                }
            }
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
                if (row.FocusGlow == null)
                {
                    continue;
                }

                bool highlighted = gamepadActive
                    ? index == focusIndex
                    : row.Hover != null && row.Hover.Hovered;

                if (row.FocusGlow.activeSelf != highlighted)
                {
                    row.FocusGlow.SetActive(highlighted);
                }
            }
        }

        // The rows the prefab ships carry no highlight of their own -- the toggle only tints its own
        // checkbox -- so a focused row looked no different from the rest. This is the same glow the item
        // and rune rows use, borrowed off their element prefab.
        private GameObject CreateRowFocusGlow(Transform row)
        {
            if (_rowFocusTemplate == null)
            {
                return null;
            }

            GameObject glow = Instantiate(_rowFocusTemplate, row, false);
            glow.name = "Focused";
            glow.transform.SetAsFirstSibling();

            Image glowImage = glow.GetComponent<Image>();
            if (glowImage != null)
            {
                glowImage.raycastTarget = false;
            }

            glow.SetActive(false);
            return glow;
        }

        public void UpdateDisplaySelectedItemEnchantments()
        {
            if (_selectedItem == null)
            {
                MainButton.interactable = false;
                return;
            }

            // Set the enchantments to be selected based on the enchantments on this item
            List<Tuple<string, bool>> info = EnchantingUIController.GetEnchantmentEffects(_selectedItem, true);
            RefreshSelectableEnchantments();
            UpdateDisplayAvailableOverwriteEnchantments(); //TODO remove?

            // Set enchantment list to the enchantments of the selected item
            Tuple<float, float> featureValues =
                EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Rune);

            float costReduction = GetCostReduction(featureValues.Item1);

            CostLabel.enabled = true;
            List<InventoryItemListElement> cost;

            if (_runeAction == RuneAction.Extract)
            {
                cost = EnchantingUIController.GetRuneExtractCost(_selectedItem, _selectedRarity, costReduction);
            }
            else if (_runeAction == RuneAction.Etch)
            {
                cost = EnchantingUIController.GetRuneEtchCost(_selectedItem, _selectedRarity, costReduction);
            }
            else
            {
                cost = new List<InventoryItemListElement>();
            }

            CostList.SetItems(cost.Cast<IListElement>().ToList());

            CheckIfActionDoable();
        }

        public void UpdateDisplayAvailableOverwriteEnchantments()
        {
            if (_selectedItem == null || _runeAction == RuneAction.Extract || _selectedEnchantmentIndex <= -1)
            {
                AvailableRunes.SetItems(new List<IListElement>());
                MainButton.interactable = false;
                return;
            }

            List<InventoryItemListElement> availableEnchantRunes =
                EnchantingUIController.GetApplyableRunesforItem(_selectedItem, EnchantingUIController.GetSelectedEnchantmentNameByIndex(_selectedItem, _selectedEnchantmentIndex));
            AvailableRunes.SetItems(availableEnchantRunes.Cast<IListElement>().ToList());
        }

        private void ClearEnchantmentList()
        {
            // Clear the enchantment list
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
        }

        private void RefreshSelectableEnchantments()
        {
            Tuple<InventoryItemListElement, int> entry = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            ItemDrop.ItemData item = entry?.Item1.GetItem();
            List<Tuple<string, bool>> augmentableEffects = EnchantingUIController.GetEnchantmentEffects(item, true);

            ClearEnchantmentList();

            foreach (Tuple<string, bool> effect in augmentableEffects)
            {
                GameObject enchantmentListElement = Instantiate(EnchantmentListPrefab, EnchantList);
                // Include inactive: the prefab ships deactivated, so the clone is still inactive here and
                // the plain overload would hand back null and leave every row reading "Enchant Selector".
                Text enchantmentElement = enchantmentListElement.GetComponentInChildren<Text>(true);
                Toggle enchantmentbutton = enchantmentListElement.GetComponent<Toggle>();
                enchantmentbutton.onValueChanged.AddListener((isOn) =>
                {
                    SetSelectedEnchantIndex();
                    UpdateDisplayAvailableOverwriteEnchantments();
                    CheckIfActionDoable();
                });

                if (enchantmentElement != null)
                {
                    enchantmentElement.text = effect.Item1;
                }

                // Dimming the row was all that marked an effect with CanBeRunified off, and it could
                // still be selected, extracted and overwritten. Same treatment as the augment tab.
                bool selectable = effect.Item2;
                enchantmentbutton.interactable = selectable && !_locked;

                enchantmentListElement.SetActive(true);

                _enchantmentRows.Add(new EnchantmentRow
                {
                    Toggle = enchantmentbutton,
                    FocusGlow = CreateRowFocusGlow(enchantmentListElement.transform),
                    Hover = enchantmentListElement.AddComponent<RowHover>(),
                    Selectable = selectable
                });
            }

            _enchantmentColumn.ClampFocus();
            RefreshEnchantmentFocus();
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

        public bool LocalPlayerCanAffordRuneCost(List<InventoryItemListElement> cost)
        {
            if (cost == null || cost.Count == 0)
            {
                return true;
            }

            if (Player.m_localPlayer == null)
            {
                return false;
            }

            if (Player.m_localPlayer.NoCostCheat())
            {
                return true;
            }

            foreach (InventoryItemListElement element in cost)
            {
                ItemDrop.ItemData item = element.GetItem();
                if (!InventoryManagement.Instance.HasItem(item))
                {
                    return false;
                }
            }

            return true;
        }

        public void ExtractModeSelected(bool enabled)
        {
            _runeAction = RuneAction.Extract;
            RefreshMainButtonLabel();
            Warning.text = Localization.instance.Localize(EnchantingUIController.GetRuneExtractWarningKey());

            // Deselect runes and clear them
            AvailableRunesWindow.SetActive(false);
            if (AvailableRunes.GetItemCount() > 0)
            {
                AvailableRunes.SetItems(new List<IListElement>());
            }

            NewModeSelected(enabled);
        }

        public void EtchModeSelected(bool enabled)
        {
            _runeAction = RuneAction.Etch;
            RefreshMainButtonLabel();
            Warning.text = Localization.instance.Localize("$mod_epicloot_rune_etch_warning");

            AvailableRunesWindow.SetActive(true);

            NewModeSelected(enabled);
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

            // Clears the list of enchantments if no item is selected
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
                // Check the currently selected item
                if (selectedItem?.Item1.GetItem() != _selectedItem)
                {
                    _selectedItem = selectedItem.Item1.GetItem();
                    _selectedRarity = EnchantingUIController.GetItemRarity(_selectedItem);
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

            // Clear any currently existing success dialog
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
            // still describes an item the player holds and an effect the rune tab may touch (and the
            // one the cost was shown for).
            if (item != _selectedItem || !InventoryManagement.Instance.GetAllItems().Contains(item) ||
                !EnchantingUIController.CanRunifyEffect(item.GetMagicItem(), _selectedEnchantmentIndex))
            {
                AbortMainAction("the selected item or enchantment is no longer valid");
                return;
            }

            bool completed = _runeAction == RuneAction.Extract
                ? ExtractSelectedEnchantment(item, costReduction, powerModifier)
                : _runeAction == RuneAction.Etch && EtchSelectedRune(item, costReduction);
            if (!completed)
            {
                return;
            }

            DeselectAll();

            RefreshAvailableItems();
            _selectedEnchantmentIndex = -1;
            CostList.SetItems(new List<IListElement>());
            AvailableRunes.SetItems(new List<IListElement>());
        }

        private bool ExtractSelectedEnchantment(ItemDrop.ItemData item, float costReduction, float powerModifier)
        {
            List<InventoryItemListElement> cost = EnchantingUIController.GetRuneExtractCost(item, _selectedRarity, costReduction);
            ItemDrop.ItemData RuneWithEnchant = EnchantingUIController.BuildEnchantedRune(item, _selectedEnchantmentIndex, powerModifier);

            if (RuneWithEnchant == null)
            {
                AbortMainAction("the rune could not be built");
                return false;
            }

            Player player = Player.m_localPlayer;
            bool noCost = player.NoCostCheat();
            if (!noCost && !LocalPlayerCanAffordCost(cost))
            {
                AbortMainAction("the cost can no longer be paid", missingRequirements: true);
                return false;
            }

            RuneExtractMode mode = EnchantingUIController.GetRuneExtractMode();
            List<InventoryItemListElement> reclaimedSockets = null;
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

                // Taken before anything is charged or handed out, so an item that could not be
                // removed costs nothing and yields nothing.
                if (InventoryManagement.Instance.RemoveExactItem(item, 1) < 1)
                {
                    AbortMainAction("the item could not be removed");
                    return false;
                }
            }

            if (!noCost)
            {
                foreach (InventoryItemListElement costElement in cost)
                {
                    InventoryManagement.Instance.RemoveItem(costElement.GetItem());
                }
            }

            // Apply the configured effect to the source item. The rune was already built above.
            switch (mode)
            {
                case RuneExtractMode.KeepItem:
                case RuneExtractMode.DestroyItem:
                    // Item returned untouched, or already removed above.
                    break;
                case RuneExtractMode.ReduceEnchants:
                    EnchantingUIController.ReduceItemAfterRuneExtract(item, _selectedEnchantmentIndex, reduceRarity: false);
                    break;
                case RuneExtractMode.ReduceEnchantsAndRarity:
                    EnchantingUIController.ReduceItemAfterRuneExtract(item, _selectedEnchantmentIndex, reduceRarity: true);
                    break;
            }

            if (reclaimedSockets != null)
            {
                GiveItemsToPlayer(reclaimedSockets);
            }

            InventoryManagement.Instance.GiveItem(RuneWithEnchant);
            return true;
        }

        // Modifies the existing item and consumes the selected rune. The etch is worked out on a copy
        // first; the rune and then the cost are taken, and only then is the result written to the item,
        // so a failure at any step leaves the item, the rune and the materials where they were.
        private bool EtchSelectedRune(ItemDrop.ItemData item, float costReduction)
        {
            ItemDrop.ItemData rune = AvailableRunes.GetSingleSelectedItem<InventoryItemListElement>()?.Item1.GetItem();
            string targetEffect = EnchantingUIController.GetSelectedEnchantmentNameByIndex(item, _selectedEnchantmentIndex);
            if (rune == null || rune == item || !InventoryManagement.Instance.GetAllItems().Contains(rune) ||
                !EnchantingUIController.GetApplyableRunesforItem(item, targetEffect).Any(x => x.GetItem() == rune))
            {
                AbortMainAction("the selected rune is no longer available for this enchantment");
                return false;
            }

            // The same cost CheckIfActionDoable showed and gated the button on.
            List<InventoryItemListElement> cost = EnchantingUIController.GetRuneEtchCost(item, _selectedRarity, costReduction);
            bool noCost = Player.m_localPlayer.NoCostCheat();
            if (!noCost && !LocalPlayerCanAffordCost(cost))
            {
                AbortMainAction("the cost can no longer be paid", missingRequirements: true);
                return false;
            }

            MagicItem etched = EnchantingUIController.BuildRuneEtchResult(item, rune, _selectedEnchantmentIndex);
            if (etched == null)
            {
                AbortMainAction("the etch could not be applied");
                return false;
            }

            // The rune first: the cost is only taken once the rune is actually gone.
            if (InventoryManagement.Instance.RemoveExactItem(rune, 1) < 1)
            {
                AbortMainAction("the rune could not be removed");
                return false;
            }

            if (!noCost)
            {
                foreach (InventoryItemListElement costElement in cost)
                {
                    InventoryManagement.Instance.RemoveItem(costElement.GetItem());
                }
            }

            EnchantingUIController.ApplyRuneEtch(item, etched);

            if (_successDialog != null)
            {
                Destroy(_successDialog);
            }

            _successDialog = EnchantingUIController.ShowRuneEtchSuccessDialog(item);
            _successDialog.SetActive(true);
            return true;
        }

        // A main action that could not go ahead: nothing was taken or changed. The panel is already
        // unlocked (DoMainAction cancels first); rebuild the lists so they show what is really there
        // now, and let the button state follow the fresh selection.
        private void AbortMainAction(string reason, bool missingRequirements = false)
        {
            Debug.LogWarning($"[Rune] {(_runeAction == RuneAction.Etch ? "Etch" : "Extract")} cancelled: {reason}.");
            if (missingRequirements)
            {
                Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
            }
            RefreshAvailableItems();
            _selectedEnchantmentIndex = -1;
            CostList.SetItems(new List<IListElement>());
            AvailableRunes.SetItems(new List<IListElement>());
            CheckIfActionDoable();
        }

        // base.Cancel restores _defaultButtonLabelText, which Awake captured from the prefab's shipped
        // "$mod_epicloot_rune_slot" label, so every finished action relabelled the button "Apply Rune".
        private void RefreshMainButtonLabel()
        {
            SetMainButtonLabel(_runeAction == RuneAction.Extract
                ? "$mod_epicloot_rune_extract"
                : "$mod_epicloot_rune_etch");
        }

        private void SetMainButtonLabel(string token)
        {
            string text = Localization.instance.Localize(token);
            if (_useTMP)
            {
                if (_tmpButtonLabel != null)
                {
                    _tmpButtonLabel.text = text;
                }
            }
            else if (_buttonLabel != null)
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
            if (_runeAction == RuneAction.Extract)
            {
                items = EnchantingUIController.GetRuneExtractItems();
            }
            else if (_runeAction == RuneAction.Etch)
            {
                items = EnchantingUIController.GetRuneEtchItems();
            }
            else
            {
                items = new List<InventoryItemListElement>();
            }

            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            RefreshSelectableEnchantments();
            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
        }

        protected override void OnSelectedItemsChanged()
        {
            Tuple<InventoryItemListElement, int> selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            if (selectedItem?.Item1.GetItem() != null)
            {
                _selectedItem = selectedItem.Item1.GetItem();
                _selectedRarity = EnchantingUIController.GetItemRarity(_selectedItem);
                UpdateDisplaySelectedItemEnchantments();
                _selectedEnchantmentIndex = -1;
            }
            else
            {
                ClearEnchantmentList();
            }
        }

        protected void OnSelectedOverrideRuneChanged()
        {
            Tuple<InventoryItemListElement, int> rune = AvailableRunes.GetSingleSelectedItem<InventoryItemListElement>();
            if (rune?.Item1.GetItem() != null)
            {
                _selectedOverrideRune = rune.Item1.GetItem();
                CheckIfActionDoable();
            }
            else
            {
                _selectedOverrideRune = null;
            }
        }

        private void CheckIfActionDoable()
        {
            bool state = true;

            if (_selectedItem == null || _selectedEnchantmentIndex == -1 ||
                !EnchantingUIController.CanRunifyEffect(_selectedItem.GetMagicItem(), _selectedEnchantmentIndex))
            {
                state = false;
                MainButton.interactable = false;
                return;
            }

            // Check costs, ignored if nocost mode
            Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Rune);
            float costReduction = GetCostReduction(featureValues.Item1);

            if (_runeAction == RuneAction.Etch)
            {
                List<InventoryItemListElement> cost = EnchantingUIController.GetRuneEtchCost(_selectedItem, _selectedRarity, costReduction);
                CostList.SetItems(cost.Cast<IListElement>().ToList());
                state = LocalPlayerCanAffordRuneCost(cost);

                if (_selectedOverrideRune == null)
                {
                    // Etching but does not have an override rune selected
                    state = false;
                }
            }
            else if (_runeAction == RuneAction.Extract)
            {
                List<InventoryItemListElement> cost = EnchantingUIController.GetRuneExtractCost(_selectedItem, _selectedRarity, costReduction);
                CostList.SetItems(cost.Cast<IListElement>().ToList());
                state = LocalPlayerCanAffordRuneCost(cost);
            }

            MainButton.interactable = state;
        }

        internal static float GetCostReduction(float value)
        {
            return value == 0f || float.IsNaN(value) ? 1.0f : 1f - (value / 100f);
        }

        internal static float GetPowerModifier(float value)
        {
            return float.IsNaN(value) ? 1.0f : (value / 100f);
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

            RuneExtractButton.interactable = false;
            RuneEtchButton.interactable = false;
            MainButton.interactable = false;

            // The countdown acts on _selectedEnchantmentIndex when it ends; changing the row mid-way
            // used to etch or extract a different effect from the one the cost was shown for.
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

            RuneExtractButton.interactable = true;
            RuneEtchButton.interactable = true;

            foreach (EnchantmentRow row in _enchantmentRows)
            {
                if (row.Toggle != null)
                {
                    row.Toggle.interactable = row.Selectable;
                }
            }
        }

        public override void DeselectAll()
        {
            AvailableItems?.DeselectAll();
        }
    }
}
