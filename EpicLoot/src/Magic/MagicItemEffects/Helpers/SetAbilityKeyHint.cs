using BepInEx.Configuration;
using EpicLoot.Abilities;
using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.MagicItemEffects
{
    // A "Label [Key]" entry in the combat key hints for a set ability's hotkey, cloned from vanilla's Block entry in
    // both the keyboard and the gamepad rows, so vanilla's own layout and keyboard/gamepad switching carry it. Each
    // ability holds one instance and refreshes it from a KeyHints.UpdateHints postfix.
    internal class SetAbilityKeyHint
    {
        private readonly string _entryName;
        private readonly string _labelToken;
        private readonly Func<bool> _available;
        private readonly Func<ConfigEntry<KeyCode>> _key;
        private readonly Func<ConfigEntry<KeyCode>> _button;

        private KeyHints _hints;
        private GameObject _keyboardEntry;
        private GameObject _gamepadEntry;
        private KeyCode _keyboardShownKey = KeyCode.None;
        private KeyCode _gamepadShownKey = KeyCode.None;
        private string _gamepadShownModifierPath = "";

        // The config entries are passed as getters: ELConfig binds them after the effect classes may have been
        // touched, so a captured reference could still be null.
        internal SetAbilityKeyHint(string entryName, string labelToken, Func<bool> available,
            Func<ConfigEntry<KeyCode>> key, Func<ConfigEntry<KeyCode>> button)
        {
            _entryName = entryName;
            _labelToken = labelToken;
            _available = available;
            _key = key;
            _button = button;
        }

        internal void Refresh(KeyHints hints)
        {
            if (hints.m_combatHints == null || !hints.m_combatHints.activeSelf)
            {
                return;
            }

            if (_hints != hints)
            {
                _hints = hints;
                _keyboardEntry = CloneEntry(hints.m_secondaryAttackKB, "Block");
                _gamepadEntry = CloneEntry(hints.m_secondaryAttackGP, "Text - Block");
                _keyboardShownKey = KeyCode.None;
                _gamepadShownKey = KeyCode.None;
                _gamepadShownModifierPath = "";
            }

            bool available = _available();
            var key = available ? _key()?.Value ?? KeyCode.None : KeyCode.None;
            var button = available ? _button()?.Value ?? KeyCode.None : KeyCode.None;

            if (_keyboardEntry != null && key != _keyboardShownKey)
            {
                _keyboardShownKey = key;
                if (key != KeyCode.None)
                {
                    SetKeyboardText(_keyboardEntry, ZInput.KeyCodeToDisplayName(key));
                }
                _keyboardEntry.SetActive(key != KeyCode.None);
            }

            // The set hotkeys' gamepad buttons take the ability bar's modifier (SetAbilityKeys), which moves between LT
            // and LB with the gamepad layout, so its binding is compared too.
            string modifierPath = button != KeyCode.None && AbilityController.UseGamepadModifier
                ? AbilityController.GamepadModifierPath() ?? ""
                : "";
            if (_gamepadEntry != null && (button != _gamepadShownKey || modifierPath != _gamepadShownModifierPath))
            {
                _gamepadShownKey = button;
                _gamepadShownModifierPath = modifierPath;
                if (button != KeyCode.None)
                {
                    var text = _gamepadEntry.GetComponentInChildren<TMP_Text>(true);
                    if (text != null)
                    {
                        string modifier = modifierPath.Length > 0
                            ? ZInput.instance.GetBoundKeyString("JoyAltKeys", emptyStringOnMissing: true)
                            : "";
                        string chord = modifier.Length > 0 ? $"{modifier} + {GamepadGlyph(button)}" : GamepadGlyph(button);
                        text.text = $"{Localization.instance.Localize(_labelToken)} <mspace=0.6em> {chord}</mspace>";
                    }
                }
                _gamepadEntry.SetActive(button != KeyCode.None);
            }
        }

        // The template is the Block entry beside vanilla's secondary-attack hint; a UI mod that restructured the
        // row simply gets no hint.
        private GameObject CloneEntry(GameObject secondaryAttackHint, string templateName)
        {
            var template = secondaryAttackHint != null ? secondaryAttackHint.transform.parent?.Find(templateName) : null;
            if (template == null)
            {
                return null;
            }

            var entry = Object.Instantiate(template.gameObject, template.parent, false);
            entry.name = _entryName;
            entry.transform.SetSiblingIndex(template.GetSiblingIndex());
            entry.SetActive(false);
            return entry;
        }

        // A keyboard entry is a "Text" label plus the key in "key_bkg/Key".
        private void SetKeyboardText(GameObject entry, string keyText)
        {
            foreach (var text in entry.GetComponentsInChildren<TMP_Text>(true))
            {
                text.text = text.name == "Key" ? keyText : Localization.instance.Localize(_labelToken);
            }
        }

        // Vanilla draws gamepad buttons as sprites; borrow the glyph of whichever vanilla gamepad button is bound to
        // the same control.
        private static string GamepadGlyph(KeyCode button)
        {
            var zinput = ZInput.instance;
            if (zinput != null)
            {
                string path = ZInput.KeyCodeToPath(button);
                foreach (var pair in zinput.m_buttons)
                {
                    var def = pair.Value;
                    if (def?.ButtonAction == null || def.ButtonAction.bindings.Count == 0 ||
                        def.Source != ZInput.InputSource.Gamepad || def.GetActionPath(effective: true) != path)
                    {
                        continue;
                    }

                    string glyph = zinput.GetBoundKeyString(pair.Key, emptyStringOnMissing: true);
                    if (!string.IsNullOrEmpty(glyph))
                    {
                        return glyph;
                    }
                }
            }

            return ZInput.KeyCodeToDisplayName(button);
        }
    }
}
