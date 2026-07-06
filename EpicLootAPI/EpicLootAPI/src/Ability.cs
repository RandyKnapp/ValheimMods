using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EpicLootAPI;

[Serializable]
[PublicAPI]
public enum AbilityActivationMode
{
    Passive, // is not implemented
    Triggerable,
    Activated
}

[Serializable]
[PublicAPI]
public enum AbilityAction
{
    Custom,
    StatusEffect
}

[Serializable]
[PublicAPI]
public class AbilityDefinition
{
    public string ID = "";
    public string IconAsset = ""; // Will need to tweak EpicLoot class to allow for custom icons to be passed
    public AbilityActivationMode ActivationMode; // Only Activate works, since Triggerable is unique per Ability
    public float Cooldown;
    public AbilityAction Action; // Always Status Effect since custom is too complex behavior to pass through
    public List<string> ActionParams = new List<string>();

    [Description("Register a status effect ability which activates on player input")]
    public AbilityDefinition(string ID, string iconAsset, float cooldown, string statusEffectName)
    {
        this.ID = ID;
        ActivationMode = AbilityActivationMode.Activated;
        Cooldown = cooldown;
        Action = AbilityAction.StatusEffect;
        ActionParams.Add(statusEffectName);
        IconAsset = iconAsset;
        Abilities.Add(this);
    }
    
    internal AbilityDefinition(string ID, AbilityActivationMode mode)
    {
        this.ID = ID;
        ActivationMode = mode;
    }
    
    public AbilityDefinition(){}

    internal static readonly Method API_AddAbility = new("AddAbility");
    internal static readonly Method API_UpdateAbility = new ("UpdateAbility");
    internal static List<AbilityDefinition> Abilities = new();

    public static void RegisterAll()
    {
        foreach (AbilityDefinition ability in new List<AbilityDefinition>(Abilities))
        {
            ability.Register();
        }
    }
    
    /// <summary>
    /// Serialized to JSON and invokes <see cref="API_AddAbility"/>
    /// </summary>
    /// <returns>true if registered to runtime registry</returns>
    public bool Register()
    {
        string data = JsonConvert.SerializeObject(this);
        object[] result = API_AddAbility.Invoke(data);

        if (result[0] is not string key)
        {
            return false;
        }

        RunTimeRegistry.Register(this, key);
        Abilities.Remove(this);
        EpicLoot.logger.LogDebug("Registered ability: " + ID);
        return true;
    }
    
    /// <summary>
    /// Serialized to JSON and invokes <see cref="API_UpdateAbility"/> with unique identifier
    /// </summary>
    /// <returns>True if fields copied</returns>
    public bool Update()
    {
        if (!RunTimeRegistry.TryGetValue(this, out string key))
        {
            return false;
        }

        string data = JsonConvert.SerializeObject(this);
        object[] result = API_UpdateAbility.Invoke(key, data);
        bool output = (bool)(result[0] ?? false);
        EpicLoot.logger.LogDebug($"Updated ability {ID}: {output}");
        return output;
    }
}