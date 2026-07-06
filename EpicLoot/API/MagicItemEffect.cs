using JetBrains.Annotations;
using Newtonsoft.Json;

namespace EpicLoot;

public static partial class API
{
    /// <param name="json">JSON serialized <see cref="MagicItemEffectDefinition"/></param>
    /// <returns>unique identifier if registered</returns>
    [PublicAPI]
    public static string AddMagicEffect(string json)
    {
        try
        {
            MagicItemEffectDefinition def = JsonConvert.DeserializeObject<MagicItemEffectDefinition>(json);

            if (def == null)
            {
                return null;
            }

            MagicItemEffectDefinitions.Add(def);
            ExternalMagicItemEffectDefinitions[def.Type] = def;
            return RuntimeRegistry.Register(def);
        }
        catch
        {
            OnError?.Invoke("Failed to parse magic effect from external plugin");
            return null;
        }
    }

    /// <param name="key">unique identifier</param>
    /// <param name="json">JSON serialized <see cref="MagicItemEffectDefinition"/></param>
    /// <returns>true if updated</returns>

    [PublicAPI]
    public static bool UpdateMagicEffect(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out MagicItemEffectDefinition original))
        {
            return false;
        }

        MagicItemEffectDefinition def = JsonConvert.DeserializeObject<MagicItemEffectDefinition>(json);
        if (def == null)
        {
            return false;
        }

        original.CopyFieldsFrom(def);
        return true;
    }
}