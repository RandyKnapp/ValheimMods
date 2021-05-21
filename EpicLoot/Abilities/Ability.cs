using System.Linq;
using UnityEngine;

namespace EpicLoot.Abilities
{
    public class Ability
    {
        public virtual string CooldownEndKey => $"EpicLoot.{AbilityDef.ID}.CooldownEnd";

        public AbilityDefinition AbilityDef;

        protected Player _player;
        protected ZNetView _netView;

        public virtual void Initialize(AbilityDefinition abilityDef, Player player)
        {
            AbilityDef = abilityDef;
            _player = player;
            _netView = _player.GetComponent<ZNetView>();
        }

        public virtual void OnUpdate()
        {
            if (AbilityDef.ActivationMode == AbilityActivationMode.Triggerable && ShouldTrigger())
            {
                TryActivate();
            }
        }

        protected virtual bool ShouldTrigger()
        {
            return false;
        }

        protected static float GetTime()
        {
            return (float)ZNet.instance.GetTimeSeconds();
        }

        public virtual bool IsOnCooldown()
        {
            if (HasCooldown())
            {
                return GetTime() < GetCooldownEndTime();
            }
            return false;
        }

        public virtual float TimeUntilCooldownEnds()
        {
            var cooldownEndTime = GetCooldownEndTime();
            return Mathf.Max(0, cooldownEndTime - GetTime());
        }

        public virtual float PercentCooldownComplete()
        {
            if (HasCooldown() && IsOnCooldown())
            {
                return 1.0f - (TimeUntilCooldownEnds() / AbilityDef.Cooldown);
            }

            return 1.0f;
        }

        public virtual bool CanActivate()
        {
            return !IsOnCooldown();
        }

        public virtual void TryActivate()
        {
            Debug.LogWarning($"TryActivate Ability: {AbilityDef.ID}");
            if (CanActivate())
            {
                Activate();
            }
        }

        protected virtual void Activate()
        {
            if (HasCooldown())
            {
                var cooldownEndTime = GetTime() + AbilityDef.Cooldown;
                SetCooldownEndTime(cooldownEndTime);
            }

            switch (AbilityDef.Action)
            {
                case AbilityAction.Custom:
                    ActivateCustomAction();
                    break;

                case AbilityAction.StatusEffect:
                    ActivateStatusEffectAction();
                    break;
            }
        }

        protected virtual void ActivateCustomAction()
        {
        }

        protected virtual void ActivateStatusEffectAction()
        {
            if (AbilityDef.Action != AbilityAction.StatusEffect)
            {
                EpicLoot.LogError($"Tried to activate a status effect ability ({AbilityDef.ID}) that was not marked as Action=StatusEffect!");
                return;
            }

            var statusEffectName = AbilityDef.ActionParams.FirstOrDefault();
            if (string.IsNullOrEmpty(statusEffectName))
            {
                EpicLoot.LogError($"Tried to activate a status effect ability ({AbilityDef.ID}) but the status effect name param was missing!");
                return;
            }

            var statusEffect = EpicLoot.LoadAsset<StatusEffect>(statusEffectName);
            if (statusEffect == null)
            {
                EpicLoot.LogError($"Tried to activate a status effect ability ({AbilityDef.ID}) but the status effect asset could not be found ({statusEffectName})!");
                return;
            }

            _player.GetSEMan().AddStatusEffect(statusEffect);
        }

        protected virtual bool HasCooldown()
        {
            return AbilityDef.Cooldown > 0;
        }

        protected virtual void SetCooldownEndTime(float cooldownEndTime)
        {
            _netView.GetZDO().Set(CooldownEndKey, cooldownEndTime);
        }

        public virtual float GetCooldownEndTime()
        {
            return _netView.GetZDO().GetFloat(CooldownEndKey);
        }

        public bool IsActivatedAbility()
        {
            return AbilityDef.ActivationMode == AbilityActivationMode.Activated;
        }

        public void ResetCooldown()
        {
            SetCooldownEndTime(0);
        }
    }
}
