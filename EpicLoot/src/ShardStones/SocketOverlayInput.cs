using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using EpicLoot.Compatibility;
using EpicLoot.Config;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EpicLoot.ShardStones {
    /// <summary>
    /// The gamepad button that opens shard slots. "Joy" + the name is its ZInput button and "$KEY_" + the name
    /// its glyph, so the names must stay as they are.
    /// </summary>
    public enum SocketGamepadButton {
        None,
        ButtonX,
        ButtonY,
        ButtonA,
        RStick,
        LStick
    }

    /// <summary>The gamepad button held with <see cref="SocketGamepadButton"/>, named the same way.</summary>
    public enum SocketGamepadModifier {
        None,
        RTrigger,
        LTrigger
    }

    // The one answer to "is this press the one that opens shard slots". The inventory opener, the tooltip hint,
    // the BowsBeforeHoes patch and API.IsShardSlotPress all ask here, so they cannot disagree. Everything is a
    // read of this frame's input and nothing is consumed, so code that runs ahead of the opener in
    // InventoryGui.Update (another mod's prefix) gets the same answer the opener will.
    internal static class SocketOverlayInput {
        private const string UseButton = "Use";

        private static KeyCode _cachedMainKey = KeyCode.None;
        private static string _cachedUsePath;
        private static bool _cachedMainKeyIsUse;

        private static KeyboardShortcut Shortcut => ELConfig.SocketOverlayShortcut?.Value ?? KeyboardShortcut.Empty;

        /// <summary>
        /// Whether the shortcut's main key is the key bound to Use. The main key is then read through the Use
        /// button rather than the keyboard: a press another mod has already taken is left to it (BowsBeforeHoes
        /// opens its quiver on Use ahead of us), and the button and the raw key can never be a frame apart.
        /// </summary>
        internal static bool MainKeyIsUse() {
            KeyCode mainKey = Shortcut.MainKey;
            ZInput.ButtonDef use = ZInput.instance?.GetButtonDef(UseButton);
            if (mainKey == KeyCode.None || use == null) {
                return false;
            }

            string usePath = use.ButtonAction.bindings.Count > 0 ? use.ButtonAction.bindings[0].effectivePath : null;
            if (mainKey == _cachedMainKey && usePath == _cachedUsePath) {
                return _cachedMainKeyIsUse;
            }

            // ZInput only turns a KeyCode into a control through that control's display name, so the display
            // name is what identifies the key here.
            string mainName = ZInput.KeyCodeToDisplayName(mainKey);
            bool isUse = false;
            foreach (InputControl control in use.ButtonAction.controls) {
                if (control.device is Keyboard && control.displayName == mainName) {
                    isUse = true;
                    break;
                }
            }

            _cachedMainKey = mainKey;
            _cachedUsePath = usePath;
            _cachedMainKeyIsUse = isUse;
            return isUse;
        }

        /// <summary>True when the keyboard shortcut went down this frame.</summary>
        internal static bool KeyboardPressed() {
            KeyboardShortcut shortcut = Shortcut;
            KeyCode mainKey = shortcut.MainKey;
            if (mainKey == KeyCode.None) {
                return false;
            }

            // Both reads are cheap and one of them is true on the frame the shortcut fires, which keeps the
            // binding comparison and the modifier list off every other frame.
            bool useDown = ZInput.GetButtonDown(UseButton);
            if (!useDown && !ZInput.GetKeyDown(mainKey, logWarning: false)) {
                return false;
            }

            if (MainKeyIsUse() ? !useDown : !ZInput.GetKeyDown(mainKey, logWarning: false)) {
                return false;
            }

            // Modifiers are only ever held, so reading them raw is never out of step with the main key.
            foreach (KeyCode modifier in shortcut.Modifiers) {
                if (!ZInput.GetKey(modifier, logWarning: false)) {
                    return false;
                }
            }

            return true;
        }

        /// <summary>A shortcut that is only the Use key: the gesture item container mods open on.</summary>
        internal static bool IsBareUseShortcut() {
            KeyboardShortcut shortcut = Shortcut;
            return shortcut.MainKey != KeyCode.None && !shortcut.Modifiers.Any() && MainKeyIsUse();
        }

        /// <summary>The ZInput button the gamepad gesture is pressed on, or null when it is turned off.</summary>
        internal static string GamepadButtonName {
            get {
                switch (ELConfig.SocketOverlayGamepadButton?.Value ?? SocketGamepadButton.None) {
                    case SocketGamepadButton.ButtonX: return "JoyButtonX";
                    case SocketGamepadButton.ButtonY: return "JoyButtonY";
                    case SocketGamepadButton.ButtonA: return "JoyButtonA";
                    case SocketGamepadButton.RStick: return "JoyRStick";
                    case SocketGamepadButton.LStick: return "JoyLStick";
                    default: return null;
                }
            }
        }

        private static string GamepadModifierName {
            get {
                switch (ELConfig.SocketOverlayGamepadModifier?.Value ?? SocketGamepadModifier.None) {
                    case SocketGamepadModifier.RTrigger: return "JoyRTrigger";
                    case SocketGamepadModifier.LTrigger: return "JoyLTrigger";
                    default: return null;
                }
            }
        }

        /// <summary>
        /// True when the gamepad gesture went down this frame. The button is read by the same name InventoryGrid
        /// reads it (JoyButtonX, never JoyUse, which is A on the classic layout and X on the others), because
        /// ZInput.ResetButtonStatus clears only the named ButtonDef: consuming it under that name is what stops the
        /// grid from also using or picking up the item. With no modifier, neither trigger may be held, so vanilla's
        /// trigger gestures on the same button (split, drop, move) keep working.
        /// </summary>
        internal static bool GamepadPressed() {
            string button = GamepadButtonName;
            if (button == null || !ZInput.IsGamepadActive() || !ZInput.GetButtonDown(button)) {
                return false;
            }

            string modifier = GamepadModifierName;
            return modifier != null
                ? ZInput.GetButton(modifier)
                : !ZInput.GetButton("JoyLTrigger") && !ZInput.GetButton("JoyRTrigger");
        }

        /// <summary>
        /// Whether the shard slot gesture went down this frame, and the player grid item it was aimed at: the
        /// one under the cursor for the shortcut, the selected one for the gamepad. The item is null over an
        /// empty slot.
        /// </summary>
        internal static bool TryGetPressTarget(InventoryGui gui, out ItemDrop.ItemData item, out bool gamepad) {
            item = null;
            gamepad = false;
            if (gui == null || gui.m_playerGrid == null) {
                return false;
            }

            if (KeyboardPressed()) {
                Vector3 pos = Input.mousePosition;
                item = gui.m_playerGrid.GetItem(new Vector2i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)));
                return true;
            }

            if (gui.m_playerGrid.m_uiGroup != null && gui.m_playerGrid.m_uiGroup.IsActive && GamepadPressed()) {
                item = gui.m_playerGrid.GetGamepadSelectedItem();
                gamepad = true;
                return true;
            }

            return false;
        }

        internal static bool HasSockets(ItemDrop.ItemData item) {
            return item != null && item.IsMagic(out MagicItem magicItem) && magicItem.HasSockets();
        }

        /// <summary>
        /// The container rule. Plain Use is the gesture another mod's container opens on, so a shortcut that is
        /// only the Use key never opens the shard slots of such an item: the press stays with that mod.
        /// </summary>
        internal static bool IsBlockedFor(ItemDrop.ItemData item, bool gamepad) {
            return !gamepad && IsBareUseShortcut() && ForeignItemContainers.HasContainer(item);
        }

        /// <summary>True when this frame's press is the shard slot gesture and the opener will act on it.</summary>
        internal static bool IsShardSlotPress(InventoryGui gui) {
            return TryGetPressTarget(gui, out ItemDrop.ItemData item, out bool gamepad) &&
                HasSockets(item) && !IsBlockedFor(item, gamepad);
        }

        /// <summary>
        /// The same question about an item the caller has already found under the cursor or selection, for
        /// <see cref="API.IsShardSlotPress"/>.
        /// </summary>
        internal static bool IsShardSlotPress(ItemDrop.ItemData item) {
            if (!HasSockets(item)) {
                return false;
            }

            return (KeyboardPressed() && !IsBlockedFor(item, gamepad: false)) || GamepadPressed();
        }

        /// <summary>
        /// The shortcut as players say it, held keys first ("Left Control + E"), although the config writes the
        /// main key first. Null when no shortcut is set.
        /// </summary>
        internal static string DescribeShortcut() {
            KeyboardShortcut shortcut = Shortcut;
            if (shortcut.MainKey == KeyCode.None) {
                return null;
            }

            List<string> keys = shortcut.Modifiers.Select(KeyName).ToList();
            keys.Add(KeyName(shortcut.MainKey));
            return string.Join(" + ", keys);
        }

        /// <summary>
        /// The gamepad gesture as $KEY_ glyph tokens ("$KEY_RTrigger + $KEY_ButtonX"), or null when it is
        /// turned off. The tokens must sit in the hint's own text, never in a translation value, since
        /// Localization.Localize is single-pass.
        /// </summary>
        internal static string DescribeGamepad() {
            string button = GamepadButtonName;
            if (button == null) {
                return null;
            }

            string modifier = GamepadModifierName;
            string buttonToken = "$KEY_" + button.Substring("Joy".Length);
            return modifier == null ? buttonToken : $"$KEY_{modifier.Substring("Joy".Length)} + {buttonToken}";
        }

        // The keyboard's own name for the key, or the KeyCode name when the key has no control (ZInput then
        // returns an error string rather than a name).
        private static string KeyName(KeyCode key) {
            string name = ZInput.KeyCodeToDisplayName(key);
            return string.IsNullOrEmpty(name) || name.StartsWith("$KeyCode") ? key.ToString() : name;
        }

        /// <summary>
        /// KeyboardShortcut takes its first key as the one that fires, so "LeftControl + E", typed the way players
        /// say it, would fire on Control. This moves the first key that is not a modifier into that place, as
        /// EAQS does for its quick slot keys.
        /// </summary>
        internal static KeyboardShortcut Normalize(KeyboardShortcut shortcut) {
            if (!IsModifier(shortcut.MainKey)) {
                return shortcut;
            }

            KeyCode mainKey = shortcut.Modifiers.FirstOrDefault(key => !IsModifier(key));
            if (mainKey == KeyCode.None) {
                return shortcut;
            }

            return new KeyboardShortcut(mainKey,
                shortcut.Modifiers.Where(key => key != mainKey).Append(shortcut.MainKey).ToArray());
        }

        private static bool IsModifier(KeyCode key) {
            switch (key) {
                case KeyCode.AltGr:
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                case KeyCode.LeftCommand:
                case KeyCode.RightCommand:
                case KeyCode.LeftWindows:
                case KeyCode.RightWindows:
                    return true;
                default:
                    return false;
            }
        }
    }
}
