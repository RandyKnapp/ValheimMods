using EpicLoot.Abilities;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EpicLoot;

public static partial class API
{
    /// <param name="abilityID"></param>
    /// <param name="proxy"></param>
    /// <returns>true, if callback functions found using unique ability ID</returns>
    private static bool TryGetProxyAbility(string abilityID, out Dictionary<string, Delegate> proxy)
    {
        return AbilityProxies.TryGetValue(abilityID, out proxy);
    }

    /// <param name="json">JSON serialized <see cref="AbilityDefinition"/></param>
    /// <param name="delegates">callback functions</param>
    /// <returns>unique identifier if registered</returns>
    [PublicAPI]
    public static string RegisterProxyAbility(string json, Dictionary<string, Delegate> delegates)
    {
        try
        {
            AbilityDefinition ability = JsonConvert.DeserializeObject<AbilityDefinition>(json);
            if (ability == null)
            {
                return null;
            }

            AbilityFactory.Register(ability.ID, typeof(AbilityProxy));
            AbilityProxies[ability.ID] = delegates;
            AbilityProxyDefinition def = new AbilityProxyDefinition(ability, delegates);
            AbilityDefinitions.Config.Abilities.Add(ability);
            AbilityDefinitions.Abilities[ability.ID] = ability;
            return RuntimeRegistry.Register(def);
        }
        catch
        {
            OnError?.Invoke("Failed to parse ability definition from external plugin");
            return null;
        }
    }

    /// <param name="key">unique identifier</param>
    /// <param name="json">JSON serialized <see cref="AbilityDefinition"/></param>
    /// <param name="proxy">callback functions</param>
    /// <returns></returns>
    [PublicAPI]
    public static bool UpdateProxyAbility(string key, string json, Dictionary<string, Delegate> proxy)
    {
        if (!RuntimeRegistry.TryGetValue(key, out AbilityProxyDefinition kvp))
        {
            return false;
        }

        AbilityDefinition def = JsonConvert.DeserializeObject<AbilityDefinition>(json);
        kvp.Ability.CopyFieldsFrom(def);
        kvp.Delegates.CopyFieldsFrom(proxy);
        return true;
    }

    /// <summary>
    /// Custom class to contain proxy definition, to register as a unit
    /// </summary>
    private class AbilityProxyDefinition
    {
        public readonly AbilityDefinition Ability;
        public readonly Dictionary<string, Delegate> Delegates;
        public AbilityProxyDefinition(AbilityDefinition ability, Dictionary<string, Delegate> delegates)
        {
            Ability = ability;
            Delegates = delegates;
        }
    }
    
    /// <param name="proxy"><see cref="AbilityProxy"/></param>
    /// <param name="abilityID">unique ability ID</param>
    /// <returns>true if dictionary of callbacks injected into new instance of proxy</returns>
    public static bool InjectCallbacks(this AbilityProxy proxy, string abilityID)
    {
        if (!TryGetProxyAbility(abilityID, out Dictionary<string, Delegate> delegates))
        {
            return false;
        }

        proxy._callbacks = delegates;
        return true;
    }
    
    /// <summary>
    /// Ability wrapper to handle callbacks
    /// </summary>
    public class AbilityProxy : Ability
    {
        public Dictionary<string, Delegate> _callbacks = new Dictionary<string, Delegate>();
        private T GetCallback<T>(string name) where T : Delegate
        {
            if (_callbacks.TryGetValue(name, out Delegate del) && del is T typed)
            {
                return typed;
            }

            return null;
        }

        public override void Initialize(AbilityDefinition abilityDef, Player player)
        {
            base.Initialize(abilityDef, player);
            if (GetCallback<Action<Player, string, float>>(nameof(Initialize)) is not { } callback)
            {
                return;
            }

            callback(player, abilityDef.ID, abilityDef.Cooldown);
        }

        public override void OnUpdate()
        {
            if (GetCallback<Action>(nameof(OnUpdate)) is not { } callback)
            {
                base.OnUpdate();
            }
            else
            {
                callback();
            }
        }

        protected override bool ShouldTrigger()
        {
            return GetCallback<Func<bool>>(nameof(ShouldTrigger)) is not { } callback
                ? base.ShouldTrigger()
                : callback();
        }

        public override bool IsOnCooldown() => GetCallback<Func<bool>>(nameof(IsOnCooldown)) is not { } callback
            ? base.IsOnCooldown()
            : callback();

        public override float TimeUntilCooldownEnds() =>
            GetCallback<Func<float>>(nameof(TimeUntilCooldownEnds)) is not { } callback
                ? base.TimeUntilCooldownEnds()
                : callback();

        public override float PercentCooldownComplete() =>
            GetCallback<Func<float>>(nameof(PercentCooldownComplete)) is not { } callback
                ? base.PercentCooldownComplete()
                : callback();

        public override bool CanActivate() => GetCallback<Func<bool>>(nameof(CanActivate)) is not { } callback
            ? base.CanActivate()
            : callback();

        public override void TryActivate()
        {
            if (GetCallback<Action>(nameof(TryActivate)) is not { } callback) base.TryActivate();
            else callback();
        }

        protected override void Activate()
        {
            if (GetCallback<Action>(nameof(Activate)) is not { } callback) base.Activate();
            else callback();
        }

        protected override void ActivateCustomAction()
        {
            if (GetCallback<Action>(nameof(ActivateCustomAction)) is not { } callback) base.ActivateCustomAction();
            else callback();
        }

        protected override void ActivateStatusEffectAction()
        {
            if (GetCallback<Action>(nameof(ActivateStatusEffectAction)) is not { } callback)
                base.ActivateStatusEffectAction();
            else callback();
        }

        protected override bool HasCooldown() => GetCallback<Func<bool>>(nameof(HasCooldown)) is not { } callback
            ? base.HasCooldown()
            : callback();

        protected override void SetCooldownEndTime(float cooldownEndTime)
        {
            if (GetCallback<Action<float>>(nameof(SetCooldownEndTime)) is not { } callback)
                base.SetCooldownEndTime(cooldownEndTime);
            else callback(cooldownEndTime);
        }
        public override float GetCooldownEndTime() => GetCallback<Func<float>>(nameof(GetCooldownEndTime)) is not { } callback 
            ? base.GetCooldownEndTime() : callback();
        
        public override void OnRemoved()
        {
            if (GetCallback<Action>(nameof(OnRemoved)) is not { } callback) base.OnRemoved();
            else callback();
        }
    }
}