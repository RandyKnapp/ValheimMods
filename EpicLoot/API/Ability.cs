using EpicLoot.Abilities;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace EpicLoot;

public static partial class API
{
    /// <param name="json">JSON serialized <see cref="AbilityDefinition"/></param>
    /// <returns>unique identifier if registered</returns>
    [PublicAPI]
    public static string AddAbility(string json)
    {
        try
        {
            AbilityDefinition def = JsonConvert.DeserializeObject<AbilityDefinition>(json);
            if (def == null)
            {
                return null;
            }

            if (AbilityDefinitions.Abilities.ContainsKey(def.ID))
            {
                OnError?.Invoke($"Duplicate entry found for Abilities: {def.ID} when adding from external plugin.");
                return null;
            }

            ExternalAbilities[def.ID] = def;
            AbilityDefinitions.Config.Abilities.Add(def);
            AbilityDefinitions.Abilities[def.ID] = def;
            return RuntimeRegistry.Register(def);
        }
        catch
        {
            OnError?.Invoke("Failed to parse ability definition passed in through external plugin.");
            return null;
        }
    }
    
    /// <param name="key">unique identifier</param>
    /// <param name="json">JSON serialized <see cref="AbilityDefinition"/></param>
    /// <returns>true if updated</returns>
    [PublicAPI]
    public static bool UpdateAbility(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out AbilityDefinition original))
        {
            return false;
        }

        AbilityDefinition def = JsonConvert.DeserializeObject<AbilityDefinition>(json);
        if (def == null)
        {
            return false;
        }
        
        original.CopyFieldsFrom(def);
        return true;
    }
    
    public static bool HasCurrentAbility(Player player, string key)
    {
        if (!RuntimeRegistry.TryGetValue(key, out AbilityDefinition definition))
        {
            return false;
        }

        if (!player.TryGetComponent(out AbilityController controller))
        {
            return false;
        }
        return controller.GetCurrentAbility(definition.ID) is not null;
    }
}