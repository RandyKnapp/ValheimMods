using BepInEx.Configuration;
using EpicLoot.Abilities;
using EpicLoot.Config;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EpicLoot.MagicItemEffects
{
    // The keys of the set bonuses that need a press: the set hotkeys listed here and the ability bar's activated
    // abilities. A set hotkey's gamepad button is pressed with the game's gamepad modifier, as the ability bar's are
    // (Ability Gamepad Buttons Use Modifier), which is what lets every one of them default to the right stick click
    // without taking it from crouch or hiding weapons. No two of these sets can be worn whole at once, so sharing
    // that chord, as the keyboard sets share T, never fires two.
    internal static class SetAbilityKeys
    {
        // Getters, as in SetAbilityKeyHint: ELConfig binds the entries after this class may have been touched.
        private static readonly Dictionary<string, (Func<ConfigEntry<KeyCode>> Key, Func<ConfigEntry<KeyCode>> Button)> Hotkeys =
            new Dictionary<string, (Func<ConfigEntry<KeyCode>>, Func<ConfigEntry<KeyCode>>)>
            {
                { MagicEffectType.OverwhelmingLaunch, (() => ELConfig.OverwhelmingLaunchKey, () => ELConfig.OverwhelmingLaunchGamepadButton) },
                { MagicEffectType.Earthshaker, (() => ELConfig.EarthshakerKey, () => ELConfig.EarthshakerGamepadButton) },
                { MagicEffectType.EitrInfusion, (() => ELConfig.EitrInfusionKey, () => ELConfig.EitrInfusionGamepadButton) },
                { MagicEffectType.ShieldThrow, (() => ELConfig.ShieldThrowKey, () => ELConfig.ShieldThrowGamepadButton) },
                { MagicEffectType.SpiritAnimal, (() => ELConfig.SpiritAnimalKey, () => ELConfig.SpiritAnimalGamepadButton) },
            };

        // Never in build mode, where the modifier with the right stick click toggles alternate placement on the
        // Alternative layouts (AbilityController skips it for the same reason).
        internal static bool GamepadButtonDown(Player player, KeyCode button)
        {
            return button != KeyCode.None && !player.InPlaceMode() && AbilityController.GamepadModifierHeld() &&
                ZInput.GetKeyDown(button, logWarning: false);
        }

        internal static bool GamepadButtonHeld(Player player, KeyCode button)
        {
            return button != KeyCode.None && !player.InPlaceMode() && AbilityController.GamepadModifierHeld() &&
                ZInput.GetKey(button, logWarning: false);
        }

        // "Spirit Animal [Y]" style labels for hover text: the gamepad chord while a gamepad is in use.
        internal static string ActiveLabel(KeyCode key, KeyCode button)
        {
            return (ZInput.IsGamepadActive() && button != KeyCode.None) || key == KeyCode.None
                ? AbilityController.GamepadChordLabel(button)
                : ZInput.KeyCodeToDisplayName(key);
        }

        /// <summary>
        /// "Activation key: T (gamepad: LT+R3)", localized, for a set bonus that needs a press; empty for any other.
        /// The gamepad chord is left out while no gamepad is connected, since its button names come from the device.
        /// </summary>
        internal static string DescribeActivationKey(MagicItemEffectDefinition definition)
        {
            if (definition == null || !TryGetBindings(definition, out string key, out KeyCode button, out int slot))
            {
                return "";
            }

            string gamepad = Gamepad.current == null ? "" : slot >= 0
                ? AbilityController.GamepadBindingLabel(slot)
                : AbilityController.GamepadChordLabel(button);

            var localization = Localization.instance;
            if (key.Length > 0 && gamepad.Length > 0)
            {
                return localization.Localize("$mod_epicloot_set_activationkey_gamepad", key, gamepad);
            }

            if (key.Length > 0 || gamepad.Length > 0)
            {
                return localization.Localize("$mod_epicloot_set_activationkey", key.Length > 0 ? key : gamepad);
            }

            return localization.Localize("$mod_epicloot_set_activationkey_none");
        }

        // A set hotkey reports its button; an ability reports its slot instead, whose button is the slot's.
        private static bool TryGetBindings(MagicItemEffectDefinition definition, out string key, out KeyCode button, out int slot)
        {
            key = "";
            button = KeyCode.None;
            slot = -1;
            if (Hotkeys.TryGetValue(definition.Type, out var entries))
            {
                var keyCode = entries.Key()?.Value ?? KeyCode.None;
                key = keyCode == KeyCode.None ? "" : ZInput.KeyCodeToDisplayName(keyCode);
                button = entries.Button()?.Value ?? KeyCode.None;
                return true;
            }

            if (string.IsNullOrEmpty(definition.Ability) ||
                !AbilityDefinitions.TryGetAbilityDef(definition.Ability, out var abilityDef) ||
                abilityDef.ActivationMode != AbilityActivationMode.Activated)
            {
                return false;
            }

            slot = AbilitySlot(definition.Ability);
            key = AbilityController.GetBindingKeycode(slot)?.ToUpperInvariant() ?? "";
            return true;
        }

        // The slot the local player's ability is in, or the one it would take: abilities keep their slots and a new
        // one is added after them.
        private static int AbilitySlot(string abilityId)
        {
            var abilities = AbilityController._local?.CurrentAbilities;
            if (abilities == null)
            {
                return 0;
            }

            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i].AbilityDef.ID == abilityId)
                {
                    return i;
                }
            }

            return Mathf.Min(abilities.Count, AbilityController.AbilitySlotCount - 1);
        }
    }
}
