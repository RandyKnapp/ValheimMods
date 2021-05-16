using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Abilities
{
    [RequireComponent(typeof(Player))]
    public class AbilityController : MonoBehaviour
    {
        public const int AbilitySlotCount = 3;

        private Player _player;
        private readonly List<Ability> _currentAbilities = new List<Ability>();

        public IReadOnlyList<Ability> CurrentAbilities => _currentAbilities;

        public virtual void Awake()
        {
            _player = GetComponent<Player>();
            UpdatePlayerAbilities();
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
                Debug.LogWarning($"Key Down: {keyCode}");
                UseAbility(index);
            }
        }

        public static string GetBindingKeycode(int index)
        {
            index = Mathf.Clamp(index, 0, AbilitySlotCount - 1);
            return EpicLoot.AbilityKeyCodes[index]?.Value.ToLowerInvariant();
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
            var availableAbilities = GetAvailableAbilities();

            _currentAbilities.RemoveAll(x => !availableAbilities.Exists(y => y.ID == x.AbilityDef.ID));

            for (var i = 0; i < AbilitySlotCount && i < availableAbilities.Count; i++)
            {
                var abilityDef = availableAbilities[i];
                if (abilityDef.ActivationMode == AbilityActivationMode.Activated)
                {
                    if (!_currentAbilities.Exists(x => x.AbilityDef.ID == abilityDef.ID))
                    {
                        Debug.LogWarning($"Add Ability: {abilityDef.ID}");
                        var ability = new Ability();
                        ability.Initialize(abilityDef, _player);
                        _currentAbilities.Add(ability);
                    }
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
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
    public static class Player_SetLocalPlayer_Patch
    {
        public static void Postfix(Player __instance)
        {
            __instance.gameObject.AddComponent<AbilityController>();
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
