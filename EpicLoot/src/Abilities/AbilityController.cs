using EpicLoot.Config;
using EpicLoot.MagicItemEffects;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

namespace EpicLoot.Abilities
{
    [RequireComponent(typeof(Player))]
    public class AbilityController : MonoBehaviour
    {
        public const int AbilitySlotCount = 3;

        internal static AbilityController _local;

        internal Player _player;
        private readonly List<Ability> _currentAbilities = new List<Ability>();

        public IReadOnlyList<Ability> CurrentAbilities => _currentAbilities;

        public virtual void Awake()
        {
            _player = GetComponent<Player>();
            _local = this;
            UpdatePlayerAbilities();
        }

        public virtual void OnDestroy()
        {
            if (_local == this)
            {
                _local = null;
            }
        }

        public virtual void Update()
        {
            if (_player.TakeInput())
            {
                for (var i = 0; i < AbilitySlotCount; ++i)
                {
                    CheckAbilityInput(i);
                }
            }

            foreach (var ability in _currentAbilities)
            {
                ability.OnUpdate();
            }
        }

        public void CheckAbilityInput(int index)
        {
            if (!PlayerHasAbility(index))
            {
                return;
            }

            var keyCode = GetBindingKeycode(index);
            if (keyCode != null && Input.GetKeyDown(keyCode))
            {
                UseAbility(index);
            }
        }

        public static string GetBindingKeycode(int index)
        {
            index = Mathf.Clamp(index, 0, AbilitySlotCount - 1);
            return ELConfig.AbilityKeyCodes[index]?.Value.ToLowerInvariant();
        }

        public static KeyCode GetGamepadButton(int index)
        {
            index = Mathf.Clamp(index, 0, AbilitySlotCount - 1);
            return ELConfig.AbilityGamepadButtons[index]?.Value ?? KeyCode.None;
        }

        internal static bool UseGamepadModifier => ELConfig.AbilityGamepadUseAltKey?.Value ?? true;

        // Also gates the set hotkeys' gamepad buttons (SetAbilityKeys).
        internal static bool GamepadModifierHeld()
        {
            return !UseGamepadModifier || ZInput.GetButton("JoyAltKeys");
        }

        private static readonly Dictionary<string, string> ShortButtonNames = new Dictionary<string, string>
        {
            { "buttonSouth", "A" }, { "buttonEast", "B" }, { "buttonWest", "X" }, { "buttonNorth", "Y" },
            { "leftShoulder", "LB" }, { "rightShoulder", "RB" }, { "leftTrigger", "LT" }, { "rightTrigger", "RT" },
            { "leftStickPress", "L3" }, { "rightStickPress", "R3" }, { "start", "Start" }, { "select", "Select" },
        };

        private static readonly Dictionary<string, string> ShortDualShockNames = new Dictionary<string, string>
        {
            { "buttonSouth", "X" }, { "buttonEast", "O" }, { "buttonWest", "Sq" }, { "buttonNorth", "Tri" },
            { "leftShoulder", "L1" }, { "rightShoulder", "R1" }, { "leftTrigger", "L2" }, { "rightTrigger", "R2" },
            { "leftStickPress", "L3" }, { "rightStickPress", "R3" }, { "start", "Opt" }, { "select", "Share" },
        };

        private static readonly Dictionary<string, string> ShortDpadNames = new Dictionary<string, string>
        {
            { "up", "D-Up" }, { "down", "D-Dn" }, { "left", "D-L" }, { "right", "D-R" },
        };

        private static string ShortButtonName(string path)
        {
            var control = InputSystem.FindControl(path);
            if (control == null)
            {
                return null;
            }

            if (control.parent is DpadControl && ShortDpadNames.TryGetValue(control.name, out var dpad))
            {
                return dpad;
            }

            var names = control.device is DualShockGamepad ? ShortDualShockNames : ShortButtonNames;
            return names.TryGetValue(control.name, out var name) ? name : control.displayName;
        }

        // "LB+R3" for the ability bar; vanilla's own gamepad glyphs are TMP sprites, which the bar's legacy Text
        // cannot draw.
        public static string GamepadBindingLabel(int index)
        {
            return GamepadChordLabel(GetGamepadButton(index));
        }

        // The same label for any button pressed with the modifier rule above, such as a set hotkey's.
        public static string GamepadChordLabel(KeyCode button)
        {
            if (button == KeyCode.None)
            {
                return string.Empty;
            }

            string label = ShortButtonName(ZInput.KeyCodeToPath(button)) ?? button.ToString();
            if (!UseGamepadModifier)
            {
                return label;
            }

            string modifierPath = GamepadModifierPath();
            string modifier = modifierPath != null ? ShortButtonName(modifierPath) : null;
            return string.IsNullOrEmpty(modifier) ? label : $"{modifier}+{label}";
        }

        // The control the game's gamepad modifier is bound to now, or null when it has no binding.
        public static string GamepadModifierPath()
        {
            if (ZInput.instance == null || !ZInput.instance.m_buttons.TryGetValue("JoyAltKeys", out var def) ||
                def?.ButtonAction == null || def.ButtonAction.bindings.Count == 0)
            {
                return null;
            }

            return def.GetActionPath(effective: true);
        }

        private bool SlotTakesGamepadButton(int index, out KeyCode button)
        {
            button = GetGamepadButton(index);
            return button != KeyCode.None && PlayerHasAbility(index) && _currentAbilities[index].IsActivatedAbility();
        }

        // Runs before the body of Player.Update, i.e. before vanilla reads this frame's buttons, so the press can be
        // taken away from whatever vanilla action shares the button. Never in build mode, where the modifier with
        // the right stick click toggles alternate placement on the Alternative layouts.
        internal void CheckGamepadInput()
        {
            if (!_player.TakeInput() || _player.InPlaceMode() || Hud.InRadial() || !GamepadModifierHeld())
            {
                return;
            }

            for (var i = 0; i < AbilitySlotCount; ++i)
            {
                if (!SlotTakesGamepadButton(i, out var button) || !ZInput.GetKeyDown(button, logWarning: false))
                {
                    continue;
                }

                OverwhelmingLaunch.ConsumeKey(button, refresh: true);
                UseAbility(i);
            }
        }

        // Buttons whose vanilla action is read in the fixed step (attack, block, jump) have already been read by the
        // time Player.Update runs, so they are held down here too.
        internal void ConsumeHeldGamepadButtons()
        {
            if (!_player.TakeInput() || _player.InPlaceMode() || !GamepadModifierHeld())
            {
                return;
            }

            for (var i = 0; i < AbilitySlotCount; ++i)
            {
                if (SlotTakesGamepadButton(i, out var button) && ZInput.GetKey(button, logWarning: false))
                {
                    OverwhelmingLaunch.ConsumeKey(button, refresh: false);
                }
            }
        }

        private void UseAbility(int index)
        {
            var ability = _currentAbilities[index];
            if (!ability.IsActivatedAbility() || ability.IsOnCooldown())
            {
                return;
            }

            ability.TryActivate();
        }

        private bool PlayerHasAbility(int index)
        {
            return index >= 0 && index < _currentAbilities.Count;
        }

        public void OnEquipItem()
        {
            UpdatePlayerAbilities();
        }

        public void OnUnequipItem()
        {
            UpdatePlayerAbilities();
        }

        public void UpdatePlayerAbilities()
        {
            List<AbilityDefinition> availableAbilities = GetAvailableAbilities();
            HashSet<string> availableAbilityIds = new HashSet<string>(
                availableAbilities
                    .Take(AbilitySlotCount)
                    .Where(x => x.ActivationMode is AbilityActivationMode.Activated or AbilityActivationMode.Triggerable)
                    .Select(x => x.ID)
            );

            // Single pass: remove old abilities and track which ones we still have
            HashSet<string> existingAbilityIds = new HashSet<string>();
            for (var i = _currentAbilities.Count - 1; i >= 0; --i)
            {
                Ability ability = _currentAbilities[i];
                if (!availableAbilityIds.Contains(ability.AbilityDef.ID))
                {
                    ability.OnRemoved();
                    _currentAbilities.RemoveAt(i);
                }
                else
                {
                    existingAbilityIds.Add(ability.AbilityDef.ID);
                }
            }

            // Add new abilities
            foreach (AbilityDefinition abilityDef in availableAbilities.Take(AbilitySlotCount))
            {
                if (abilityDef.ActivationMode is AbilityActivationMode.Activated or AbilityActivationMode.Triggerable && !existingAbilityIds.Contains(abilityDef.ID))
                {
                    Ability ability = AbilityFactory.Create(abilityDef.ID);
                    ability.Initialize(abilityDef, _player);
                    _currentAbilities.Add(ability);
                }
            }
        }

        private List<AbilityDefinition> GetAvailableAbilities()
        {
            var effectsWithAbilities = _player.GetAllActiveMagicEffects()
                .Select(x => MagicItemEffectDefinitions.Get(x.EffectType))
                .Where(x => !string.IsNullOrEmpty(x.Ability));

            var availableAbilities = new HashSet<AbilityDefinition>();
            foreach (var effectDef in effectsWithAbilities)
            {
                if (AbilityDefinitions.TryGetAbilityDef(effectDef.Ability, out var abilityDef) && !availableAbilities.Contains(abilityDef))
                {
                    availableAbilities.Add(abilityDef);
                }
            }

            return availableAbilities.ToList();
        }

        public virtual Ability GetCurrentAbility(int index)
        {
            return PlayerHasAbility(index) ? _currentAbilities[index] : null;
        }

        public virtual Ability GetCurrentAbility(string abilityID)
        {
            return _currentAbilities.Find(x => x.AbilityDef.ID == abilityID);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
    public static class Player_SetLocalPlayer_Patch
    {
        public static void Postfix(Player __instance)
        {
            __instance.gameObject.AddComponent<AbilityController>();
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    public static class Player_Update_Patch
    {
        public static void Prefix(Player __instance)
        {
            if (AbilityController._local != null && AbilityController._local._player == __instance)
            {
                AbilityController._local.CheckGamepadInput();
            }
        }
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FixedUpdate))]
    public static class PlayerController_FixedUpdate_Patch
    {
        public static void Prefix(PlayerController __instance)
        {
            if (AbilityController._local != null && AbilityController._local._player == __instance.m_character)
            {
                AbilityController._local.ConsumeHeldGamepadButtons();
            }
        }
    }
    
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    public static class Player_EquipItem_Patch
    {
        public static void Postfix(Humanoid __instance)
        {
            var abilityController = __instance.GetComponent<AbilityController>();
            if (abilityController != null)
            {
                abilityController.OnEquipItem();
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    public static class Player_UnequipItem_Patch
    {
        public static void Postfix(Humanoid __instance)
        {
            var abilityController = __instance.GetComponent<AbilityController>();
            if (abilityController != null)
            {
                abilityController.OnUnequipItem();
            }
        }
    }
}
