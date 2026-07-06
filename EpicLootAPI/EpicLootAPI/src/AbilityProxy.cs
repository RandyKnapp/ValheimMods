using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace EpicLootAPI;

[PublicAPI]
public class AbilityProxyDefinition
{
    public readonly AbilityDefinition Ability;
    public readonly Dictionary<string, Delegate> Callbacks = new();
    
    [Description("Register a complex ability behavior using Proxy class")]
    public AbilityProxyDefinition(string ID, AbilityActivationMode mode,  Proxy definition)
    {
        Ability = new AbilityDefinition(ID, mode);
        RegisterCallbacks(definition);
        ProxyAbilities.Add(this);
    }
    
    [Description("Register a complex ability behavior using Proxy class")]
    public AbilityProxyDefinition(string ID, AbilityActivationMode mode, Type type)
    {
        Ability = new  AbilityDefinition(ID, mode);
        if (!typeof(Proxy).IsAssignableFrom(type))
        {
            EpicLoot.logger.LogError($"Ability Proxy {ID} Type {type.Name} does not implement Proxy class");
            return;
        }

        try
        {
            object proxy = Activator.CreateInstance(type);
            RegisterCallbacks((Proxy)proxy);
            ProxyAbilities.Add(this);
        }
        catch (Exception ex)
        {
            EpicLoot.logger.LogError($"Failed to create instance of {type.Name}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets the method names and constructs delegates
    /// </summary>
    /// <param name="implementation"></param>
    private void RegisterCallbacks(Proxy implementation)
    {
        Type implementationType = implementation.GetType();
        Type interfaceType = typeof(Proxy);
        MethodInfo[] interfaceMethods = interfaceType.GetMethods();
        
        foreach (MethodInfo interfaceMethod in interfaceMethods)
        {
            try
            {
                // Find the corresponding implementation method
                Type[] parameterTypes = interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray();
                MethodInfo implementationMethod = implementationType.GetMethod(interfaceMethod.Name, parameterTypes);
                
                if (implementationMethod == null)
                {
                    EpicLoot.logger.LogWarning($"Method {interfaceMethod.Name} not found in implementation");
                    continue;
                }
                
                // Create proper delegate types based on method signature
                Type delegateType;
                
                if (interfaceMethod.ReturnType == typeof(void))
                {
                    // Action delegate
                    if (parameterTypes.Length == 0)
                    {
                        delegateType = typeof(Action);
                    }
                    else
                    {
                        delegateType = Expression.GetActionType(parameterTypes);
                    }
                }
                else
                {
                    // Func delegate
                    Type[] allTypes = parameterTypes.Concat(new[] { interfaceMethod.ReturnType }).ToArray();
                    delegateType = Expression.GetFuncType(allTypes);
                }
                
                Delegate del = Delegate.CreateDelegate(delegateType, implementation, implementationMethod);
                Callbacks[interfaceMethod.Name] = del;
            }
            catch (Exception ex)
            {
                EpicLoot.logger.LogError($"Failed to register callback for method {interfaceMethod.Name}: {ex.Message}");
            }
        }
    }

    internal static readonly List<AbilityProxyDefinition> ProxyAbilities = new();
    internal static readonly Method API_RegisterProxyAbility = new("RegisterProxyAbility");
    internal static readonly Method API_UpdateProxyAbility = new("UpdateProxyAbility");

    public static void RegisterAll()
    {
        foreach (AbilityProxyDefinition proxy in new List<AbilityProxyDefinition>(ProxyAbilities))
        {
            proxy.Register();
        }
    }

    public bool Register()
    {
        string json = JsonConvert.SerializeObject(Ability);
        object[] result = API_RegisterProxyAbility.Invoke(json, Callbacks);

        if (result[0] is not string key)
        {
            return false;
        }

        RunTimeRegistry.Register(this, key);
        ProxyAbilities.Remove(this);
        EpicLoot.logger.LogDebug("Registered proxy ability: " + Ability.ID);
        return true;
    }

    public bool Update()
    {
        if (!RunTimeRegistry.TryGetValue(this, out string key))
        {
            return false;
        }

        string json = JsonConvert.SerializeObject(Ability);
        object[] result = API_UpdateProxyAbility.Invoke(key, json,  Callbacks);
        bool output = (bool)(result[0] ?? false);
        EpicLoot.logger.LogDebug($"Updated proxy ability: {Ability.ID}, {output}");
        return output;
    }
}

[Description("Functions are converted into delegates when registered to epic loot")]
[PublicAPI]
public class Proxy
{
    public virtual string CooldownEndKey => $"EpicLoot.{AbilityID}.CooldownEnd";
    protected Player Player;
    public string AbilityID = "";
    public float Cooldown;
    
    public virtual void Initialize(Player player, string id, float cooldown)
    {
        Player = player;
        AbilityID = id;
        Cooldown = cooldown;
    }

    public virtual void OnUpdate() { }

    public virtual bool ShouldTrigger()
    {
        return false;
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
        return Mathf.Max(0, GetCooldownEndTime() - GetTime());
    }

    public virtual float PercentCooldownComplete()
    {
        if (HasCooldown() && IsOnCooldown())
        {
            return 1.0f - TimeUntilCooldownEnds() / Cooldown;
        }

        return 1.0f;
    }

    public virtual bool CanActivate()
    {
        return !IsOnCooldown();
    }

    public virtual void TryActivate()
    {
        if (CanActivate())
        {
            Activate();
        }
    }

    public virtual void Activate()
    {
        if (HasCooldown())
        {
            var cooldownEndtime = GetTime() + Cooldown;
            SetCooldownEndTime(cooldownEndtime);
        }
    }

    public virtual void ActivateCustomAction() { }

    public virtual void ActivateStatusEffectAction() { }

    public virtual bool HasCooldown()
    {
        return Cooldown > 0;
    }

    public virtual void SetCooldownEndTime(float cooldownEndTime)
    {
        if (Player == null)
        {
            return;
        }

        Player.m_nview.GetZDO().Set(CooldownEndKey, cooldownEndTime);
    }

    public virtual float GetCooldownEndTime()
    {
        if (Player == null)
        {
            return 0f;
        }

        return Player.m_nview.GetZDO().GetFloat(CooldownEndKey, 0);
    }

    public virtual void OnRemoved()
    {
        
    }

    protected static float GetTime() => (float)ZNet.instance.GetTimeSeconds();
}