using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // The on-screen quick slot bar: a clone of the vanilla HotKeyBar whose items come from the
    // quick slot cells. One controller drives both bars as a single gamepad navigation strip
    // (JoyHotbarLeft/Right walks off the end of one bar into the other, JoyHotbarUse uses the
    // selection) — no transpilers, and the vanilla bar keeps stock behavior when the quick bar
    // is inactive.
    public static class QuickSlotsHotBar {
        public const string vanillaBarName = "HotKeyBar";
        public const string barName = "QuickSlotsHotkeyBar";

        private static readonly List<HotkeyBar> bars = new List<HotkeyBar>();
        private static readonly List<HotkeyBarRefreshGate> barGates = new List<HotkeyBarRefreshGate>();
        private static int _currentBarIndex = -1;
        private static HotkeyBar _quickBar;

        public static ItemDrop.ItemData GetItemInSlot(int index) =>
            index >= 0 && index < ValConfig.MaxQuickSlots ? slots[QuickSlotStartIndex + index].Item : null;

        private static void GetQuickSlotItems(List<ItemDrop.ItemData> items) {
            items.Clear();
            foreach (Slot slot in slots) {
                if (slot.IsQuickSlot && slot.IsActive && slot.Item is ItemDrop.ItemData item)
                    items.Add(item);
            }
        }

        private static bool QuickBarEnabled => ValConfig.QuickSlotsEnabled.Value && ValConfig.QuickSlotCount.Value > 0;

        private static bool IsBarToControl(HotkeyBar bar) => bars.Count > 1 && bars.Contains(bar);

        private static bool NoBarsToControl() => bars.Count < 2 || !QuickBarEnabled;

        private static bool IsHotkeyBarsActive() => !InventoryGui.IsVisible() && !Menu.IsVisible() && !GameCamera.InFreeFly()
                                                    && !Minimap.IsOpen() && !Hud.IsPieceSelectionVisible() && !StoreGui.IsVisible()
                                                    && !Console.IsVisible() && !Chat.instance.HasFocus() && !PlayerCustomizaton.IsBarberGuiVisible()
                                                    && !Hud.InRadial();

        private static void ResetBars() {
            bars.Clear();
            barGates.Clear();
            _quickBar = null;
            _currentBarIndex = -1;
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.Awake))]
        private static class Hud_Awake_CreateQuickSlotsBar {
            private static void Postfix(Hud __instance) {
                Transform vanillaBar = __instance.m_rootObject.transform.Find(vanillaBarName);
                if (vanillaBar == null)
                    return;

                RectTransform clone = Object.Instantiate(vanillaBar.GetComponent<RectTransform>(), __instance.m_rootObject.transform, true);
                clone.name = barName;
                clone.localPosition = Vector3.zero;
                clone.SetSiblingIndex(vanillaBar.GetSiblingIndex() + 1);

                for (int i = clone.childCount - 1; i >= 0; i--)
                    Object.Destroy(clone.GetChild(i).gameObject);

                // Reset first: it clears _quickBar, and a stale (null) quick bar would leave the
                // clone running vanilla Update — a second copy of the vanilla hotbar.
                ResetBars();

                _quickBar = clone.GetComponent<HotkeyBar>();

                var positioned = clone.gameObject.AddComponent<Common.ConfigPositionedElement>();
                positioned.AnchorConfig = ValConfig.QuickSlotsAnchor;
                positioned.PositionConfig = ValConfig.QuickSlotsPosition;
                // AddComponent ran Awake before the configs were assigned, so nothing has placed the
                // bar yet. Place it now rather than leaving it to the component's first Update:
                // BetterUI's own Hud.Awake postfix (which runs right after this one) removes this
                // component so its HUD editor can own the bar's position, and it takes the bar's
                // position at that moment as the starting point — see BetterUICompat. 2.x placed
                // the bar here as well.
                positioned.EnsureCorrectPosition();

                bars.Add(vanillaBar.GetComponent<HotkeyBar>());
                bars.Add(_quickBar);
                barGates.Add(new HotkeyBarRefreshGate());
                barGates.Add(new HotkeyBarRefreshGate());
                _currentBarIndex = 0;
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.OnDestroy))]
        private static class Hud_OnDestroy_ResetBars {
            private static void Postfix() => ResetBars();
        }

        // Controlled bars skip their vanilla Update; the controller below drives them. When the
        // quick bar is inactive the vanilla bar falls back to pure vanilla behavior — but the
        // quick bar itself must never run vanilla Update (it would render the vanilla hotbar's
        // items a second time), so it is suppressed and emptied instead.
        [HarmonyPatch(typeof(HotkeyBar), "Update")]
        private static class HotkeyBar_Update_PreventCall {
            [HarmonyPriority(Priority.First)]
            private static bool Prefix(HotkeyBar __instance) {
                if (__instance == _quickBar && _quickBar != null) {
                    if (NoBarsToControl())
                        ClearBarElements(__instance);

                    return false;
                }

                return !IsBarToControl(__instance) || NoBarsToControl();
            }
        }

        private static void ClearBarElements(HotkeyBar bar) {
            if (bar.m_elements.Count == 0)
                return;

            foreach (var element in bar.m_elements)
                UnityEngine.Object.Destroy(element.m_go);
            bar.m_elements.Clear();
        }

        [HarmonyPatch(typeof(Hud), "Update")]
        private static class Hud_Update_HotkeyBarsController {
            private static void Postfix() {
                if (NoBarsToControl())
                    return;

                Player player = Player.m_localPlayer;
                if (player == null)
                    return;

                for (int i = 0; i < bars.Count; i++) {
                    if (bars[i] == null) {
                        ResetBars();
                        return;
                    }
                }

                BetterUICompat.NoteQuickBarHandoff(_quickBar);

                if (ZInput.IsGamepadActive() && IsHotkeyBarsActive() && player.TakeInput()) {
                    bool joyHotbarLeft = ZInput.GetButtonDown("JoyHotbarLeft") && !ZInput.GetButton("JoyAltKeys");
                    bool joyHotbarRight = ZInput.GetButtonDown("JoyHotbarRight") && !ZInput.GetButton("JoyAltKeys");
                    bool joyHotbarUse = ZInput.GetButtonDown("JoyHotbarUse") && !ZInput.GetButton("JoyAltKeys");

                    HandleInput(player, joyHotbarLeft, joyHotbarRight, joyHotbarUse);
                }

                for (int i = 0; i < bars.Count; i++) {
                    HotkeyBar bar = bars[i];
                    bar.m_selected = _currentBarIndex == i
                        ? Mathf.Clamp(bar.m_selected, 0, Mathf.Max(0, bar.m_elements.Count - 1))
                        : -1;

                    HotkeyBarRefreshGate gate = barGates[i];
                    if (!gate.ShouldRefresh(bar, player))
                        continue;

                    bar.UpdateIcons(player);
                    gate.Resample(bar, player);
                }
            }
        }

        private static void HandleInput(Player player, bool joyHotbarLeft, bool joyHotbarRight, bool joyHotbarUse) {
            if (_currentBarIndex < 0 || _currentBarIndex >= bars.Count)
                _currentBarIndex = 0;

            HotkeyBar hotkeyBar = bars[_currentBarIndex];

            if (joyHotbarLeft && --hotkeyBar.m_selected < 0)
                ChangeActiveHotkeyBar(next: false);
            else if (joyHotbarRight && ++hotkeyBar.m_selected > hotkeyBar.m_elements.Count - 1)
                ChangeActiveHotkeyBar(next: true);
            else if (joyHotbarUse) {
                if (hotkeyBar == _quickBar)
                    player.UseItem(player.GetInventory(), GetItemInSlot(hotkeyBar.m_selected), fromInventoryGui: false);
                else
                    player.UseHotbarItem(hotkeyBar.m_selected + 1);
            }
        }

        private static void ChangeActiveHotkeyBar(bool next) {
            int[] activeBars = Enumerable.Range(0, bars.Count).Where(i => bars[i].m_elements.Count > 0).ToArray();
            if (activeBars.Length == 0)
                return;

            int index = System.Array.IndexOf(activeBars, _currentBarIndex);
            index = index == -1 ? 0 : index + (next ? 1 : -1);

            _currentBarIndex = activeBars[(index + activeBars.Length) % activeBars.Length];
            bars[_currentBarIndex].m_selected = next ? 0 : bars[_currentBarIndex].m_elements.Count - 1;
        }

        // While the quick bar refreshes its icons, Inventory.GetBoundItems supplies the quick
        // slot items instead of the hotbar row. Their grid x is the slot index (0-5), so the
        // vanilla element math needs no adjustment.
        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
        private static class HotkeyBar_UpdateIcons_QuickBarScope {
            internal static bool inCall;

            private static void Prefix(HotkeyBar __instance) {
                inCall = __instance == _quickBar && _quickBar != null;
            }

            private static void Postfix(HotkeyBar __instance) {
                if (!inCall)
                    return;

                inCall = false;

                for (int i = 0; i < __instance.m_elements.Count; i++) {
                    Transform binding = __instance.m_elements[i].m_go.transform.Find("binding");
                    if (binding && binding.GetComponent<TMP_Text>() is TMP_Text text)
                        text.text = !ZInput.IsGamepadActive() ? slots[QuickSlotStartIndex + i].GetShortcutText() : string.Empty;
                }
            }

            [HarmonyFinalizer]
            private static void Finalizer() => inCall = false;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetBoundItems))]
        private static class Inventory_GetBoundItems_QuickBarItems {
            private static bool Prefix(Inventory __instance, List<ItemDrop.ItemData> bound) {
                if (!HotkeyBar_UpdateIcons_QuickBarScope.inCall || __instance != PlayerInventory)
                    return true;

                GetQuickSlotItems(bound);
                return false;
            }
        }
    }
}
