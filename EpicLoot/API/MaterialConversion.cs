using EpicLoot.CraftingV2;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace EpicLoot;

public static partial class API
{
    /// <param name="json">JSON serialized <see cref="MaterialConversion"/></param>
    /// <returns>unique key if added to <see cref="MaterialConversions.Conversions"/></returns>
    [PublicAPI]
    public static string AddMaterialConversion(string json)
    {
        try
        {
            MaterialConversion conversion = JsonConvert.DeserializeObject<MaterialConversion>(json);
            if (conversion == null)
            {
                return null;
            }

            ExternalMaterialConversions.Add(conversion);
            MaterialConversions.Config.MaterialConversions.Add(conversion);
            MaterialConversions.Conversions.Add(conversion.Type, conversion);
            return RuntimeRegistry.Register(conversion);
        }
        catch
        {
            OnError?.Invoke("Failed to parse material conversion passed in through external plugin.");
            return null;
        }
    }
    
    /// <param name="key">unique identifier</param>
    /// <param name="json">JSON serialized <see cref="MaterialConversion"/></param>
    /// <returns>True if updated</returns>
    [PublicAPI]
    public static bool UpdateMaterialConversion(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out MaterialConversion original))
        {
            return false;
        }

        MaterialConversion conversion = JsonConvert.DeserializeObject<MaterialConversion>(json);
        if (conversion == null)
        {
            return false;
        }

        original.CopyFieldsFrom(conversion);
        return true;
    }
}