using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // Pressing a quick slot hotkey must not also trigger whatever vanilla action shares that
    // key (Z = sit, V = toggle walk, ...). ZInput's button table is reverse-mapped from key
    // paths to button names, and the low-level state getters return false for those buttons on
    // the frames a quick slot hotkey (with an item in the slot) fires. Ported from ExtraSlots'
    // PreventSimilarHotkeys.
    public static class PreventSimilarHotkeys {
        private static readonly HashSet<string> similarName = new HashSet<string>();
        private static readonly HashSet<KeyCode> similarKeyCode = new HashSet<KeyCode>();
        private static bool _anyHotkeyDown;
        private static bool _anyHotkeyHeld;

        private static int _cacheUpdatedToken = -1;
        private static int _heldCacheUpdatedToken = -1;

        private static int _skipPreventionDepth;
        private static bool SkipPrevention => _skipPreventionDepth > 0;

        private static bool IsDedicated => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;

        public static bool IsShortcutDown(KeyboardShortcut shortcut) => IsShortcutActive(shortcut, checkForHeld: false);

        public static bool IsShortcutPressed(KeyboardShortcut shortcut) => IsShortcutActive(shortcut, checkForHeld: true);

        private static bool IsShortcutActive(KeyboardShortcut shortcut, bool checkForHeld) {
            if (shortcut.MainKey == KeyCode.None)
                return false;

            _skipPreventionDepth++;

            try {
                bool mainKeyActive = checkForHeld
                    ? ZInput.GetKey(shortcut.MainKey)
                    : ZInput.GetKeyDown(shortcut.MainKey);

                if (!mainKeyActive)
                    return false;

                foreach (KeyCode modifier in shortcut.Modifiers) {
                    if (!ZInput.GetKey(modifier))
                        return false;
                }

                return true;
            } finally {
                _skipPreventionDepth--;
            }
        }

        private static int GetCacheToken() => (Time.frameCount << 1) | (Time.inFixedTimeStep ? 1 : 0);

        private static void ResetHotkeyState() {
            _cacheUpdatedToken = -1;
            _heldCacheUpdatedToken = -1;

            _anyHotkeyDown = false;
            _anyHotkeyHeld = false;

            ZInput_TryGetButtonState_PreventSimilarHotkeys.checkForHeld = false;
            ZInput_TryGetButtonState_PreventSimilarHotkeys.skipCheck = false;

            ZInput_TryGetKeyStateLowLevel_PreventSimilarHotkeys.checkForHeld = false;
            ZInput_TryGetKeyStateLowLevel_PreventSimilarHotkeys.skipCheck = false;
        }

        public static void FillSimilarHotkey() => FillSimilarHotkey(ZInput.instance);

        internal static void FillSimilarHotkey(ZInput __instance) {
            if (IsDedicated)
                return;

            SanitizeShortcutsKeys();

            similarName.Clear();
            similarKeyCode.Clear();
            ResetHotkeyState();

            if (__instance?.m_buttons == null)
                return;

            Dictionary<string, HashSet<string>> pathToButtonNames = new Dictionary<string, HashSet<string>>();

            foreach (KeyValuePair<string, ZInput.ButtonDef> button in __instance.m_buttons) {
                AddButtonPath(button.Value.GetActionPath(effective: true), button.Key);
                AddButtonPath(button.Value.GetActionPath(effective: false), button.Key);
            }

            foreach (Slot slot in slots) {
                if (!slot.IsHotkeySlot)
                    continue;

                KeyCode mainKey = slot.GetShortcut().MainKey;

                if (mainKey == KeyCode.None)
                    continue;

                similarKeyCode.Add(mainKey);

                string keyPath = ZInput.KeyCodeToPath(mainKey);
                if (!pathToButtonNames.TryGetValue(keyPath, out HashSet<string> buttonNames))
                    continue;

                similarName.UnionWith(buttonNames);
            }

            void AddButtonPath(string path, string buttonName) {
                if (string.IsNullOrEmpty(path))
                    return;

                if (!pathToButtonNames.TryGetValue(path, out HashSet<string> buttonNames)) {
                    buttonNames = new HashSet<string>();
                    pathToButtonNames[path] = buttonNames;
                }

                buttonNames.Add(buttonName);
            }
        }

        private static void SanitizeShortcutsKeys() {
            foreach (ConfigEntry<KeyboardShortcut> hotkeyConfig in ValConfig.QuickSlotKeys) {
                if (hotkeyConfig == null)
                    continue;

                KeyCode key = hotkeyConfig.Value.MainKey;
                if (key != KeyCode.None && !ZInput.IsKeyCodeValid(key)) {
                    EquipmentAndQuickSlots.LogWarning($"Wrong bind data on {hotkeyConfig.Definition}: {hotkeyConfig.Value}. Hotkey cleared.");
                    hotkeyConfig.Value = KeyboardShortcut.Empty;
                }

                ReorderKeys(hotkeyConfig);
            }
        }

        // A shortcut stored as "LeftAlt + Z" with the modifier in the main-key position never
        // fires; swap it back into shape.
        private static void ReorderKeys(ConfigEntry<KeyboardShortcut> keyboardShortcut) {
            if (!IsModifier(keyboardShortcut.Value.MainKey))
                return;

            KeyCode key = keyboardShortcut.Value.Modifiers.FirstOrDefault(k => !IsModifier(k));
            if (key == KeyCode.None)
                return;

            keyboardShortcut.Value = new KeyboardShortcut(key, keyboardShortcut.Value.Modifiers.Where(k => k != key).AddItem(keyboardShortcut.Value.MainKey).ToArray());
            EquipmentAndQuickSlots.LogWarning($"Reordered bind data on {keyboardShortcut.Definition}: {keyboardShortcut.Value}.");
        }

        private static bool IsModifier(KeyCode key) {
            return key == KeyCode.AltGr ||
                   key == KeyCode.LeftAlt ||
                   key == KeyCode.RightAlt ||
                   key == KeyCode.LeftShift ||
                   key == KeyCode.RightShift ||
                   key == KeyCode.LeftControl ||
                   key == KeyCode.RightControl ||
                   key == KeyCode.LeftApple ||
                   key == KeyCode.RightApple ||
                   key == KeyCode.LeftCommand ||
                   key == KeyCode.RightCommand ||
                   key == KeyCode.LeftWindows ||
                   key == KeyCode.RightWindows;
        }

        internal static bool IsAnyQuickSlotHotkeyDown(bool checkForHeld = false) {
            int token = GetCacheToken();

            if (checkForHeld) {
                if (_heldCacheUpdatedToken == token)
                    return _anyHotkeyHeld;

                _heldCacheUpdatedToken = token;
                _anyHotkeyHeld = slots.Any(slot => slot.IsHotkeySlot && slot.IsShortcutPressedWithItem());
                return _anyHotkeyHeld;
            }

            if (_cacheUpdatedToken == token)
                return _anyHotkeyDown;

            _cacheUpdatedToken = token;
            _anyHotkeyDown = slots.Any(slot => slot.IsHotkeySlot && slot.IsShortcutDownWithItem());
            return _anyHotkeyDown;
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButtonUp))]
        private static class ZInput_GetButtonUp_PreventSimilarHotkeys {
            private static void Prefix() => ZInput_TryGetButtonState_PreventSimilarHotkeys.skipCheck = true;
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetButton))]
        private static class ZInput_GetButton_PreventSimilarHotkeys {
            private static void Prefix() => ZInput_TryGetButtonState_PreventSimilarHotkeys.checkForHeld = true;
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseButton))]
        private static class ZInput_GetMouseButton_PreventSimilarHotkeys {
            private static void Prefix() => ZInput_TryGetButtonState_PreventSimilarHotkeys.checkForHeld = true;
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetMouseButtonUp))]
        private static class ZInput_GetMouseButtonUp_PreventSimilarHotkeys {
            private static void Prefix() => ZInput_TryGetButtonState_PreventSimilarHotkeys.skipCheck = true;
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.TryGetButtonState))]
        private static class ZInput_TryGetButtonState_PreventSimilarHotkeys {
            internal static bool checkForHeld = false;
            internal static bool skipCheck = false;

            private static void Postfix(string name, ref bool __result) {
                bool held = checkForHeld;
                bool skip = skipCheck;

                checkForHeld = false;
                skipCheck = false;

                if (!skip && __result && similarName.Contains(name))
                    __result = !IsAnyQuickSlotHotkeyDown(held);
            }
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetKey))]
        private static class ZInput_GetKey_PreventSimilarHotkeys {
            private static void Prefix() => ZInput_TryGetKeyStateLowLevel_PreventSimilarHotkeys.checkForHeld = true;
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.GetKeyUp))]
        private static class ZInput_GetKeyUp_PreventSimilarHotkeys {
            private static void Prefix() => ZInput_TryGetKeyStateLowLevel_PreventSimilarHotkeys.skipCheck = true;
        }

        [HarmonyPatch(typeof(ZInput), nameof(ZInput.TryGetKeyStateLowLevel))]
        private static class ZInput_TryGetKeyStateLowLevel_PreventSimilarHotkeys {
            internal static bool checkForHeld = false;
            internal static bool skipCheck = false;

            private static void Postfix(KeyCode keyCode, ref bool __result) {
                bool held = checkForHeld;
                bool skip = skipCheck;

                checkForHeld = false;
                skipCheck = false;

                if (!SkipPrevention && !skip && __result && similarKeyCode.Contains(keyCode))
                    __result = !IsAnyQuickSlotHotkeyDown(held);
            }
        }

        [HarmonyPatch]
        private static class ZInput_InternalUpdate_ResetSimilarHotkeyState {
            private static IEnumerable<MethodBase> TargetMethods() {
                yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.InternalUpdate));
                yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.InternalUpdateFixed));
            }

            private static void Finalizer() => ResetHotkeyState();
        }

        [HarmonyPatch]
        private static class ZInput_SimilarHotkeyOnBind {
            private static IEnumerable<MethodBase> TargetMethods() {
                yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.ResetKBMButtons));
                yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.ResetGamepadButtonsGeneric));
                yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.ResetGamepadToClassic));
                yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.ResetGamepadToAlt1));
                yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.ResetGamepadToAlt2));
                yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.OnRebindComplete));
                yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.ResetToDefault));
                yield return AccessTools.Method(typeof(ZInput), nameof(ZInput.Load));
            }

            private static void Finalizer(ZInput __instance) => FillSimilarHotkey(__instance);
        }
    }
}
