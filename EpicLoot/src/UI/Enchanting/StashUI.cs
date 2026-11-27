using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class StashUI : EnchantingTableUIPanelBase
    {
        private const float EmptyCellAlpha = 0.25f;

        // Between the title above and the grids below, in line with the Take All buttons.
        private const float CapacityLabelMargin = 2f;
        private const float CapacityLabelHeight = 48f;
        private const float CapacityLabelY = -62f;
        private const float CapacityLabelMinFontSize = 10f;

        public TMP_Text CapacityLabel;
        public MultiSelectItemList MaterialsList;
        public MultiSelectItemList ShardstonesList;
        public Button TakeAllMaterialsButton;
        public Button TakeAllShardstonesButton;

        // Left stick click, the press vanilla uses to take everything from a chest.
        private const string TakeAllInput = "JoyLStick";

        private readonly Dictionary<string, StashEntry> _entries = new Dictionary<string, StashEntry>();

        // Right stick click, the same press vanilla uses to stack everything into a chest.
        protected override string MainActionInput => "JoyRStick";
        private EnchantingTable _table;
        private EnchantingTableStash _stash;
        private bool _hadAccess;
        private GameObject _takeAllMaterialsHint;
        private GameObject _takeAllShardstonesHint;

        protected override void OnSelectedItemsChanged()
        {
        }

        public override void Awake()
        {
            base.Awake();

            MultiSelectListFocusController focusController = GetComponent<MultiSelectListFocusController>();
            if (focusController != null)
            {
                focusController.SetPanes(new IGamepadFocusPane[] { MaterialsList, ShardstonesList });
            }

            if (TakeAllMaterialsButton != null)
            {
                TakeAllMaterialsButton.onClick.AddListener(() => OnTakeAllClicked(StashCatalog.MaterialGrid));
                _takeAllMaterialsHint = FindGamepadHint(TakeAllMaterialsButton.transform);
            }

            if (TakeAllShardstonesButton != null)
            {
                TakeAllShardstonesButton.onClick.AddListener(() => OnTakeAllClicked(StashCatalog.ShardstoneGrid));
                _takeAllShardstonesHint = FindGamepadHint(TakeAllShardstonesButton.transform);
            }

            FitCapacityLabelBetweenButtons();
        }

        // The message shares a row with the Take All buttons, in a rect far wider than the gap between them, so the
        // locked and warded messages and many translations would run across both. It stays in the gap, wrapping and then
        // shrinking to fit. Truncate would drop whatever did not fit, so overflow is off.
        private void FitCapacityLabelBetweenButtons()
        {
            if (CapacityLabel == null || TakeAllMaterialsButton == null || TakeAllShardstonesButton == null)
            {
                return;
            }

            RectTransform left = (RectTransform)TakeAllMaterialsButton.transform;
            RectTransform right = (RectTransform)TakeAllShardstonesButton.transform;
            float gap = right.anchoredPosition.x - right.rect.width / 2f - (left.anchoredPosition.x + left.rect.width / 2f);

            RectTransform label = CapacityLabel.rectTransform;
            label.sizeDelta = new Vector2(gap - 2f * CapacityLabelMargin, CapacityLabelHeight);
            label.anchoredPosition = new Vector2(label.anchoredPosition.x, CapacityLabelY);

            CapacityLabel.fontSizeMax = CapacityLabel.fontSize;
            CapacityLabel.fontSizeMin = CapacityLabelMinFontSize;
            CapacityLabel.enableAutoSizing = true;
            CapacityLabel.overflowMode = TextOverflowModes.Overflow;
        }

        public void OnEnable()
        {
            Unsubscribe();
            _table = EnchantingTableUI.instance != null ? EnchantingTableUI.instance.SourceTable : null;
            _stash = _table != null ? _table.Stash : null;
            if (_stash != null)
            {
                _stash.OnChanged += Refresh;
                _table.OnAnyFeatureLevelChanged += Refresh;
                _stash.HasAccess(flash: true);
            }

            Refresh();
        }

        public void OnDisable()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (_stash != null)
            {
                _stash.OnChanged -= Refresh;
            }

            if (_table != null)
            {
                _table.OnAnyFeatureLevelChanged -= Refresh;
            }

            _stash = null;
            _table = null;
        }

        public override void Update()
        {
            base.Update();

            if (_stash != null && _stash.HasAccess() != _hadAccess)
            {
                Refresh();
            }

            UpdateTakeAllGamepadInput();

            if (_locked || !ZInput.IsGamepadActive())
            {
                return;
            }

            if (ShardstonesList.HasGamepadFocus())
            {
                ShardstonesList.ScrollWithRightStick();
            }

            if (!ZInput.GetButtonDown("JoyButtonA"))
            {
                return;
            }

            MultiSelectItemListElement focused = MaterialsList.GetFocusedElement() ?? ShardstonesList.GetFocusedElement();
            if (focused != null)
            {
                ZInput.ResetButtonStatus("JoyButtonA");
                OnCellClicked(focused);
            }
        }

        public void Refresh()
        {
            if (MaterialsList == null || ShardstonesList == null)
            {
                return;
            }

            MaterialsList.SetItems(BuildEntries(StashCatalog.MaterialGrid));
            ShardstonesList.SetItems(BuildEntries(StashCatalog.ShardstoneGrid));
            StyleCells();

            bool canDeposit = _stash != null && _stash.CanDeposit;
            _hadAccess = _stash != null && _stash.HasAccess();
            if (CapacityLabel != null)
            {
                int capacity = canDeposit ? _stash.Capacity : 0;
                CapacityLabel.text = !canDeposit
                    ? Localization.instance.Localize("$mod_epicloot_storage_locked")
                    : !_hadAccess
                        ? Localization.instance.Localize("$mod_epicloot_storage_warded")
                        : capacity > 0
                            ? Localization.instance.Localize("$mod_epicloot_storage_capacity", capacity.ToString())
                            : Localization.instance.Localize("$mod_epicloot_storage_unlimited");
            }

            if (MainButton != null)
            {
                MainButton.interactable = canDeposit && _hadAccess && HasStashableItems();
            }

            if (TakeAllMaterialsButton != null)
            {
                TakeAllMaterialsButton.interactable = HasAnyStored(StashCatalog.MaterialGrid);
            }

            if (TakeAllShardstonesButton != null)
            {
                TakeAllShardstonesButton.interactable = HasAnyStored(StashCatalog.ShardstoneGrid);
            }
        }

        // Counts read 0 without ward access, so this also covers that.
        private bool HasAnyStored(IReadOnlyList<StashItemKind> kinds)
        {
            return _stash != null && kinds.Any(kind => _stash.GetCount(kind.PrefabName) > 0);
        }

        // One press serves both buttons, so it goes to the grid with focus. The buttons carry no UIGamePad of
        // their own for that reason: each would answer the same press.
        private void UpdateTakeAllGamepadInput()
        {
            Button focused = null;
            if (!_locked && ZInput.IsGamepadActive())
            {
                focused = MaterialsList.HasGamepadFocus() ? TakeAllMaterialsButton
                    : ShardstonesList.HasGamepadFocus() ? TakeAllShardstonesButton
                    : null;
            }

            bool usable = focused != null && focused.IsInteractable();
            SetHintActive(_takeAllMaterialsHint, usable && focused == TakeAllMaterialsButton);
            SetHintActive(_takeAllShardstonesHint, usable && focused == TakeAllShardstonesButton);

            if (usable && ZInput.GetButtonDown(TakeAllInput))
            {
                ZInput.ResetButtonStatus(TakeAllInput);
                focused.onClick.Invoke();
            }
        }

        private static void SetHintActive(GameObject hint, bool active)
        {
            if (hint != null && hint.activeSelf != active)
            {
                hint.SetActive(active);
            }
        }

        private void OnTakeAllClicked(IReadOnlyList<StashItemKind> kinds)
        {
            if (_locked || _stash == null)
            {
                return;
            }

            if (Audio != null && MainActionSFX != null)
            {
                Audio.PlayOneShot(MainActionSFX);
            }

            _stash.WithdrawAll(kinds);
        }

        private List<IListElement> BuildEntries(IReadOnlyList<StashItemKind> kinds)
        {
            HashSet<string> heldUnidentified = GetHeldUnidentifiedBiomes(kinds);
            List<IListElement> entries = new List<IListElement>(kinds.Count);
            foreach (StashItemKind kind in kinds)
            {
                // A biome's row of unidentified items shows only while the table holds some of them: a row for
                // every biome would bury the chisels and tokens under rows of empty cells. The row is whole, so
                // the rarities stay in their columns.
                if (kind.Kind == StashKind.Unidentified && !heldUnidentified.Contains(kind.Family))
                {
                    continue;
                }

                if (!_entries.TryGetValue(kind.PrefabName, out StashEntry entry))
                {
                    entry = new StashEntry { PrefabName = kind.PrefabName, Item = kind.CreateItem(0) };
                    _entries[kind.PrefabName] = entry;
                }

                entry.Item.m_stack = _stash != null ? _stash.GetCount(kind.PrefabName) : 0;
                entries.Add(entry);
            }

            return entries;
        }

        private HashSet<string> GetHeldUnidentifiedBiomes(IReadOnlyList<StashItemKind> kinds)
        {
            HashSet<string> biomes = new HashSet<string>();
            if (_stash == null)
            {
                return biomes;
            }

            foreach (StashItemKind kind in kinds)
            {
                if (kind.Kind == StashKind.Unidentified && _stash.GetCount(kind.PrefabName) > 0)
                {
                    biomes.Add(kind.Family);
                }
            }

            return biomes;
        }

        private void StyleCells()
        {
            MaterialsList.ForeachElement(StyleCell);
            ShardstonesList.ForeachElement(StyleCell);
        }

        // The cells are read-only list elements, which show the count on its own and never select; the
        // click is this panel's, and an empty kind stays in place so the grid keeps its rows.
        private void StyleCell(int _, MultiSelectItemListElement element)
        {
            StashEntry entry = element.GetListElement() as StashEntry;
            bool empty = entry == null || entry.GetMax() <= 0;

            if (element.ItemIcon != null)
            {
                Color color = element.ItemIcon.color;
                color.a = empty ? EmptyCellAlpha : 1f;
                element.ItemIcon.color = color;
            }

            if (element.MagicBG != null && empty)
            {
                element.MagicBG.enabled = false;
            }

            if (element.ItemTotalQuantity != null)
            {
                element.ItemTotalQuantity.text = empty ? string.Empty : entry.GetMax().ToString();
            }

            Button button = element.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnCellClicked(element));
            }
        }

        private void OnCellClicked(MultiSelectItemListElement element)
        {
            if (_locked || _stash == null || !(element.GetListElement() is StashEntry entry) || entry.GetMax() <= 0)
            {
                return;
            }

            if (Audio != null && MainActionSFX != null)
            {
                Audio.PlayOneShot(MainActionSFX);
            }

            _stash.Withdraw(entry.PrefabName, entry.Item.m_shared.m_maxStackSize);
        }

        private static bool HasStashableItems()
        {
            Player player = Player.m_localPlayer;
            return player != null && player.GetInventory().GetAllItems().Any(StashCatalog.IsStashable);
        }

        protected override void OnMainButtonClicked()
        {
            if (Audio != null && MainActionSFX != null)
            {
                Audio.PlayOneShot(MainActionSFX);
            }

            DoMainAction();
        }

        protected override void DoMainAction()
        {
            if (_stash != null && _stash.CanDeposit)
            {
                _stash.DepositAll();
            }

            Refresh();
        }

        public override void Lock()
        {
            base.Lock();
            StyleCells();
        }

        public override void Unlock()
        {
            base.Unlock();
            StyleCells();
        }
    }
}
