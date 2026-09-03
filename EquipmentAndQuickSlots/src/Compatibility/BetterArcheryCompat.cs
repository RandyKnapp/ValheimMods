using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // Better Archery (ishid4.mods.betterarchery) does not give the quiver an inventory of its own.
    // In a Player.Awake PREFIX it throws away the player's Inventory object and installs a new one
    // two rows taller, then treats the first three cells of the last row as the quiver:
    //
    //     Traverse.Create(player).Field("m_inventory")
    //         .SetValue(new Inventory("Inventory", null, 8, player.m_inventory.m_height + 2));
    //     QuiverRowIndex = player.m_inventory.m_height - 1;
    //
    // A prefix always beats a postfix, so Slots.CaptureBaseRows -- which runs from a Player.Awake
    // postfix -- used to latch BaseRows = 6 instead of 4. Both of Better Archery's rows were then
    // promoted to "visible" rows that nothing hides and no panel background covers: the block of
    // empty cells players reported hanging under the inventory. They were live capacity too, since
    // GetEmptySlots counts VisibleRows * width.
    //
    // Rather than switch the quiver off (what ExtraSlots does), this hosts it: the three quiver
    // cells become real EAQS custom slots. Better Archery's only positional contract is
    // IsQuiverSlot(x, y) => y == QuiverRowIndex && x >= 0 && x < 3, and reserved slot indices
    // 16/17/18 sit at exactly x = 0, 1, 2 of one hidden row, so pointing QuiverRowIndex at that row
    // keeps every retained Better Archery code path working -- its hotkeys, its drag rules, its
    // unequip guard -- while EAQS owns the cells, the capacity accounting and the death handling.
    //
    // The patches removed below are the ones that assume it still owns the inventory. Everything
    // else Better Archery does (arrow recovery, bow zoom, multishot, the quiver item and its
    // visuals) is untouched.
    //
    // Known rough edge, left alone: the "ba drop" console command drops every item at
    // m_gridPos.y >= 5 that is not a quiver slot -- i.e. all EAQS equipment and quick slot
    // contents. It is explicitly user-typed, and the same patch registers unrelated commands.
    internal static class BetterArcheryCompat {
        public const string BetterArcheryGUID = "ishid4.mods.betterarchery";
        private const string PluginTypeName = "BetterArchery.BetterArchery";
        private const string QuiverConfigSection = "Quiver";
        private const string QuiverConfigKey = "Enable Quiver";
        private const string HoldingKeyConfigKey = "Quiver slot hotkey holding key";

        // The version this shim was written against. Named in the warning when we have to fall
        // back, so a report after a Better Archery update points straight at the cause.
        private const string KnownVersion = "1.9.8x";

        // Reserved cells 16-18. Slot index -> grid position is (index % width, VisibleRows +
        // index / width), so on a stock 8-wide inventory these are x = 0, 1, 2 of the third hidden
        // row -- the shape Better Archery's IsQuiverSlot expects. Verified after claiming, because
        // a width-changing mod would break the mapping.
        private static readonly int[] QuiverSlotIndices = { 16, 17, 18 };
        private const string QuiverSlotIdPrefix = "BetterArcheryQuiver";

        private static PluginInfo _plugin;
        private static Assembly _assembly;
        private static ConfigEntry<bool> _quiverEnabled;
        private static ConfigEntry<KeyboardShortcut> _holdingKey;

        private static Func<bool> _isQuiverEquipped;
        private static Func<int, string> _getBindingLabel;
        private static FieldInfo _quiverRowIndex;

        private static bool _initialized;
        private static bool _quiverDisableHooked;
        private static bool _quiverStateWarned;

        /// <summary>True once the three quiver cells are hosted as EAQS custom slots.</summary>
        internal static bool QuiverHosted {
            get; private set;
        }

        public static bool IsLoaded => Chainloader.PluginInfos.ContainsKey(BetterArcheryGUID);

        internal static void Initialize(Harmony harmony) {
            if (_initialized)
                return;

            _initialized = true;

            if (!Chainloader.PluginInfos.TryGetValue(BetterArcheryGUID, out _plugin) || _plugin?.Instance == null)
                return;

            _assembly = _plugin.Instance.GetType().Assembly;

            // Not gated by Better Archery's quiver setting: this postfix rips out every item at
            // m_gridPos.y >= 5 and re-adds it or drops it on the ground. Under EAQS that is the
            // whole slot region, so it has to go whether or not the quiver is in play.
            CompatibilityHelper.RemoveHarmonyPatch(harmony, _assembly, typeof(TombStone), nameof(TombStone.OnTakeAllSuccess),
                "BetterArchery.Tombstone+TombStone_OnTakeAllSuccess_Patch", "Postfix",
                "stop it dropping equipment and quick slot items when a gravestone is emptied");

            if (!_plugin.Instance.Config.TryGetEntry(QuiverConfigSection, QuiverConfigKey, out _quiverEnabled)) {
                EquipmentAndQuickSlots.LogWarning($"Better Archery is loaded but its '{QuiverConfigKey}' setting could not be read (this logic was written for {KnownVersion}, the installed version is {_plugin.Metadata.Version}). Its quiver may conflict with the equipment and quick slots.");
                return;
            }

            if (!_quiverEnabled.Value)
                return;

            if (!ValConfig.BetterArcheryQuiverIntegration.Value) {
                DisableQuiver("the Better Archery quiver integration is turned off in this mod's config");
                return;
            }

            if (!BindBetterArchery() || !ClaimQuiverSlots()) {
                DisableQuiver("its quiver could not be hosted in the equipment panel");
                return;
            }

            QuiverHosted = true;
            SyncQuiverRow();

            CompatibilityHelper.RemoveHarmonyPatch(harmony, _assembly, typeof(Player), nameof(Player.Awake),
                "BetterArchery.Player_Awake_Patch", "Prefix",
                "stop it replacing the player inventory with a taller one (the empty cells under the inventory)");

            CompatibilityHelper.RemoveHarmonyPatch(harmony, _assembly, typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui),
                "BetterArchery.InventoryGrid_UpdateGui_Patch", "Postfix",
                "let the equipment panel own the quiver cells and their positions");

            CompatibilityHelper.RemoveHarmonyPatch(harmony, _assembly, typeof(Inventory), nameof(Inventory.FindEmptySlot),
                "BetterArchery.Inventory_FindEmptySlot_Patch", "Prefix",
                "stop it handing out equipment and quick slot cells as free space");

            CompatibilityHelper.RemoveHarmonyPatch(harmony, _assembly, typeof(Inventory), nameof(Inventory.HaveEmptySlot),
                "BetterArchery.Inventory_HaveEmptySlot_Patch", "Prefix",
                "stop its hardcoded capacity arithmetic overriding the slot-aware count");

            CompatibilityHelper.RemoveHarmonyPatch(harmony, _assembly, typeof(Player), nameof(Player.CreateTombStone),
                "BetterArchery.Player_CreateTombStone_Patch", "Prefix",
                "keep a single gravestone instead of a separate one for the quiver");

            EquipmentAndQuickSlots.LogInfo($"Better Archery {_plugin.Metadata.Version} detected: its quiver is hosted in three ammo-only slots in the equipment panel.");
        }

        private static bool BindBetterArchery() {
            Type plugin = _assembly.GetType(PluginTypeName);
            if (plugin == null) {
                EquipmentAndQuickSlots.LogWarning($"Better Archery is loaded but {PluginTypeName} could not be resolved.");
                return false;
            }

            _isQuiverEquipped = CompatibilityHelper.BindStatic<Func<bool>>(plugin, "IsQuiverEquipped");
            _getBindingLabel = CompatibilityHelper.BindStatic<Func<int, string>>(plugin, "GetBindingLabel");
            _quiverRowIndex = AccessTools.Field(plugin, "QuiverRowIndex");
            _plugin.Instance.Config.TryGetEntry(QuiverConfigSection, HoldingKeyConfigKey, out _holdingKey);

            if (_isQuiverEquipped != null && _quiverRowIndex != null && _quiverRowIndex.FieldType == typeof(int))
                return true;

            EquipmentAndQuickSlots.LogWarning($"Better Archery's quiver members could not be bound (this logic was written for {KnownVersion}, the installed version is {_plugin.Metadata.Version}).");
            return false;
        }

        private static bool ClaimQuiverSlots() {
            int claimed = 0;
            for (int i = 0; i < QuiverSlotIndices.Length; i++) {
                int ordinal = i;
                if (!TryAddCustomSlotAt(QuiverSlotIndices[i], QuiverSlotIdPrefix + (i + 1), BetterArcheryGUID,
                        () => SlotLabel(ordinal), IsQuiverAmmo, IsQuiverEquipped))
                    break;

                claimed++;
            }

            if (claimed == QuiverSlotIndices.Length && QuiverCellsAreWhereBetterArcheryExpects())
                return true;

            // Somebody else holds one of the cells, or a width-changing mod moved them off the
            // first three columns. Give back whatever we took: a half-claimed row would strand
            // arrows in a cell Better Archery cannot see.
            for (int i = 0; i < claimed; i++)
                TryRemoveCustomSlot(QuiverSlotIdPrefix + (i + 1));

            return false;
        }

        // Better Archery addresses the quiver as row QuiverRowIndex, columns 0-2. Our cells only
        // land there on a stock-width inventory; anything else and the integration is off.
        private static bool QuiverCellsAreWhereBetterArcheryExpects() {
            int row = slots[QuiverSlotIndices[0]].GridPosition.y;
            for (int i = 0; i < QuiverSlotIndices.Length; i++) {
                Vector2i pos = slots[QuiverSlotIndices[i]].GridPosition;
                if (pos.x != i || pos.y != row)
                    return false;
            }

            return true;
        }

        // Slot.IsActive is read every frame from the panel and the validation sweeps, so this may
        // not throw. Better Archery's IsQuiverEquipped indexes a Dictionary<Humanoid, ...> that is
        // filled from a Humanoid.Awake postfix; an entry it never got would take EAQS down with it.
        private static bool IsQuiverEquipped() {
            if (_isQuiverEquipped == null)
                return false;

            try {
                return _isQuiverEquipped();
            } catch (Exception ex) {
                if (!_quiverStateWarned) {
                    _quiverStateWarned = true;
                    EquipmentAndQuickSlots.LogWarning($"Better Archery's IsQuiverEquipped threw; the quiver slots stay hidden: {ex.Message}");
                }

                return false;
            }
        }

        private static bool IsQuiverAmmo(ItemDrop.ItemData item) =>
            item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo;

        // Better Archery draws its own hotkey on the cell; mirror it, holding key included, so the
        // label reads the same as it did when Better Archery owned the row.
        private static string SlotLabel(int ordinal) {
            string key = _getBindingLabel != null ? _getBindingLabel(ordinal) : (ordinal + 1).ToString();
            KeyCode holding = _holdingKey == null ? KeyCode.None : _holdingKey.Value.MainKey;
            return holding == KeyCode.None ? key : $"{holding} + {key}";
        }

        /// <summary>
        /// Points Better Archery's quiver row at the cells we host. Called from
        /// Slots.UpdateSlotsGridPosition, so it follows the slot region when the extra-rows config
        /// changes. No-op unless the quiver is actually hosted.
        /// </summary>
        internal static void SyncQuiverRow() {
            if (!QuiverHosted)
                return;

            int row = slots[QuiverSlotIndices[0]].GridPosition.y;
            if ((int)_quiverRowIndex.GetValue(null) != row)
                _quiverRowIndex.SetValue(null, row);
        }

        // The ExtraSlots approach, kept as the fallback: with the quiver off, every Better Archery
        // patch that touches the inventory no-ops, and the quick slots cover the same ground.
        private static void DisableQuiver(string reason) {
            if (_quiverEnabled == null || !_quiverEnabled.Value)
                return;

            // Subscribe before the write, and only once: setting Value fires SettingChanged
            // synchronously, and a handler added per call would pile up on every re-enable.
            if (!_quiverDisableHooked) {
                _quiverDisableHooked = true;
                _quiverEnabled.SettingChanged += (_, _) => DisableQuiver(reason);
            }

            _quiverEnabled.Value = false;

            EquipmentAndQuickSlots.LogWarning($"Better Archery's quiver was disabled because {reason}. Arrows already in it are not lost -- they move into your inventory. Use the quick slots for ammo instead. (This logic was written for Better Archery {KnownVersion}; the installed version is {_plugin.Metadata.Version}.)");
        }
    }
}
