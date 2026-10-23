using System.Collections.Generic;
using HarmonyLib;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // Face buttons go by their Xbox names (A is the bottom one); ZInput maps them to the
    // connected pad's glyphs and honours the game's swap-face-buttons / swap-triggers options.
    public enum ControllerButton {
        None,
        DPadUp,
        DPadRight,
        DPadDown,
        DPadLeft,
        ButtonA,
        ButtonB,
        ButtonX,
        ButtonY,
        LeftBumper,
        RightBumper,
        LeftTrigger,
        RightTrigger,
        LeftStick,
        RightStick,
    }

    // Quick slots on a controller: hold the modifier (LB by default) and press a slot's button
    // (the D-pad by default). Every controller button already does something in some layout, so
    // a combo swallows its button until it is released: every vanilla action bound to that
    // control (hotbar select/use, forsaken power, sit, camera zoom, the quick bar's own
    // navigation) reads it as up for the whole press, release included. The modifier keeps its
    // held state (LB is Run on the default layout, Block on Alternative 1) but loses its release
    // and hold timer, so Alternative 2's LB tap-to-sheathe and hold-for-radial stay quiet. With
    // the slot empty nothing is swallowed and the press does what the game normally does, like
    // the keyboard hotkeys.
    //
    // Combos are read right after ZInput ticks its buttons (inside Game.Update), not in the
    // plugin's Update: ButtonDef.Pressed stays true until the next tick, so reading it from an
    // Update that runs on either side of Game.Update could see one press twice, and a vanilla
    // read later in the same frame must already find the button swallowed.
    internal static class GamepadQuickSlots {
        // A physical control a combo is holding, with every ZInput button bound to it
        private sealed class Control {
            public ZInput.ButtonDef Def;
            public readonly HashSet<string> Names = new HashSet<string>();
        }

        private static readonly List<Control> swallowed = new List<Control>();
        private static Control _heldModifier;
        private static readonly List<int> pendingUses = new List<int>();

        private static string GetButtonName(ControllerButton button) => button switch {
            ControllerButton.DPadUp => "JoyDPadUp",
            ControllerButton.DPadRight => "JoyDPadRight",
            ControllerButton.DPadDown => "JoyDPadDown",
            ControllerButton.DPadLeft => "JoyDPadLeft",
            ControllerButton.ButtonA => "JoyButtonA",
            ControllerButton.ButtonB => "JoyButtonB",
            ControllerButton.ButtonX => "JoyButtonX",
            ControllerButton.ButtonY => "JoyButtonY",
            ControllerButton.LeftBumper => "JoyLBumper",
            ControllerButton.RightBumper => "JoyRBumper",
            ControllerButton.LeftTrigger => "JoyLTrigger",
            ControllerButton.RightTrigger => "JoyRTrigger",
            ControllerButton.LeftStick => "JoyLStick",
            ControllerButton.RightStick => "JoyRStick",
            _ => null,
        };

        // The slot's button, or None when it has no usable binding
        private static ControllerButton GetSlotButton(int index) {
            ControllerButton button = ValConfig.QuickSlotControllerButtons[index].Value;
            return button == ValConfig.QuickSlotControllerModifier.Value ? ControllerButton.None : button;
        }

        // The combo as controller glyphs, e.g. LB then D-pad up; empty when the slot has none
        internal static string GetComboText(int index) {
            ControllerButton button = GetSlotButton(index);
            if (button == ControllerButton.None || ZInput.instance == null)
                return "";

            ControllerButton modifier = ValConfig.QuickSlotControllerModifier.Value;
            string text = ZInput.instance.GetBoundKeyString(GetButtonName(button), emptyStringOnMissing: true);
            return modifier == ControllerButton.None
                ? text
                : ZInput.instance.GetBoundKeyString(GetButtonName(modifier), emptyStringOnMissing: true) + text;
        }

        internal static void UsePending(Player player) {
            if (pendingUses.Count == 0)
                return;

            if (player != null) {
                foreach (int index in pendingUses) {
                    if (QuickSlotsHotBar.GetItemInSlot(index) is ItemDrop.ItemData item)
                        player.UseItem(null, item, false);
                }
            }

            pendingUses.Clear();
        }

        private static bool CanUseCombos() => Player.m_localPlayer != null && Slot.CanUseHotkeys() && !Hud.InRadial();

        private static void ReadCombos(ZInput input) {
            // Swallowing ends on the tick after the release, so the release itself stays
            // swallowed too. Plain loops: this runs every frame.
            for (int i = swallowed.Count - 1; i >= 0; i--) {
                if (IsLetGo(input, swallowed[i]))
                    swallowed.RemoveAt(i);
            }

            if (_heldModifier != null && IsLetGo(input, _heldModifier))
                _heldModifier = null;

            if (!CanUseCombos())
                return;

            ControllerButton modifier = ValConfig.QuickSlotControllerModifier.Value;
            ZInput.ButtonDef modifierDef = null;
            if (modifier != ControllerButton.None) {
                modifierDef = input.GetButtonDef(GetButtonName(modifier));
                if (modifierDef == null || !modifierDef.Held)
                    return;
            }

            for (int i = 0; i < ValConfig.MaxQuickSlots; i++) {
                ControllerButton button = GetSlotButton(i);
                if (button == ControllerButton.None)
                    continue;

                Slot slot = slots[QuickSlotStartIndex + i];
                if (!slot.IsActive || slot.Item == null)
                    continue;

                // The D-pad buttons repeat while held; a repeat lands on a control that is
                // already swallowed and is ignored.
                ZInput.ButtonDef def = input.GetButtonDef(GetButtonName(button));
                if (def == null || !def.Pressed || IsSwallowed(def))
                    continue;

                swallowed.Add(CreateControl(input, def));
                if (modifierDef != null && _heldModifier == null)
                    _heldModifier = CreateControl(input, modifierDef);

                pendingUses.Add(i);
            }
        }

        // A layout change rebuilds every gamepad ButtonDef; the old one never updates again and
        // would hold its control forever, so a replaced def counts as let go.
        private static bool IsLetGo(ZInput input, Control control) =>
            (!control.Def.Held && !control.Def.Released) || input.GetButtonDef(control.Def.Name) != control.Def;

        // Matched by effective path, so a control the game swapped (face buttons, triggers) is
        // matched under the button it now acts as.
        private static Control CreateControl(ZInput input, ZInput.ButtonDef def) {
            Control control = new Control { Def = def };
            control.Names.Add(def.Name);

            string path = GetPath(def);
            if (string.IsNullOrEmpty(path))
                return control;

            foreach (ZInput.ButtonDef button in input.m_buttons.Values) {
                if (GetPath(button) == path)
                    control.Names.Add(button.Name);
            }

            return control;
        }

        // ButtonDef.GetActionPath reads bindings[0] unchecked; unused buttons have no binding
        private static string GetPath(ZInput.ButtonDef def) =>
            def?.ButtonAction != null && def.ButtonAction.bindings.Count > 0 ? def.GetActionPath(effective: true) : null;

        private static bool IsSwallowed(ZInput.ButtonDef def) {
            for (int i = 0; i < swallowed.Count; i++) {
                if (swallowed[i].Def == def)
                    return true;
            }

            return false;
        }

        private static bool IsSwallowed(string name, bool released) {
            for (int i = 0; i < swallowed.Count; i++) {
                if (swallowed[i].Names.Contains(name))
                    return true;
            }

            return released && _heldModifier != null && _heldModifier.Names.Contains(name);
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.InternalUpdate))]
        private static class ZInput_InternalUpdate_ReadCombos {
            private static void Postfix(ZInput __instance) => ReadCombos(__instance);
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonDown))]
        private static class ZInput_GetButtonDown_Swallow {
            private static void Postfix(string name, ref bool __result) {
                if (__result && IsSwallowed(name, released: false))
                    __result = false;
            }
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButton))]
        private static class ZInput_GetButton_Swallow {
            private static void Postfix(string name, ref bool __result) {
                if (__result && IsSwallowed(name, released: false))
                    __result = false;
            }
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonUp))]
        private static class ZInput_GetButtonUp_Swallow {
            private static void Postfix(string name, ref bool __result) {
                if (__result && IsSwallowed(name, released: true))
                    __result = false;
            }
        }

        // Hold-to-act checks (Alternative 2's LB radial, crouch) read the timer, not the state
        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonPressedTimer))]
        private static class ZInput_GetButtonPressedTimer_Swallow {
            private static void Postfix(string name, ref float __result) {
                if (__result > 0f && IsSwallowed(name, released: true))
                    __result = 0f;
            }
        }
    }
}
