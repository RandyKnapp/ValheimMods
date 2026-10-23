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

        public TMP_Text CapacityLabel;
        public MultiSelectItemList MaterialsList;
        public MultiSelectItemList ShardstonesList;

        private readonly Dictionary<string, StashEntry> _entries = new Dictionary<string, StashEntry>();

        // Right stick click, the same press vanilla uses to stack everything into a chest.
        protected override string MainActionInput => "JoyRStick";
        private EnchantingTable _table;
        private EnchantingTableStash _stash;
        private bool _hadAccess;

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
        }

        private List<IListElement> BuildEntries(IReadOnlyList<StashItemKind> kinds)
        {
            List<IListElement> entries = new List<IListElement>(kinds.Count);
            foreach (StashItemKind kind in kinds)
            {
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
