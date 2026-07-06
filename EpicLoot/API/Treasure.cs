using EpicLoot.Adventure;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace EpicLoot;

public static partial class API
{
    /// <param name="json">JSON serialized <see cref="TreasureMapBiomeInfoConfig"/></param>
    /// <returns>unique identifier</returns>
    [PublicAPI]
    public static string AddTreasureMap(string json)
    {
        try
        {
            var map = JsonConvert.DeserializeObject<TreasureMapBiomeInfoConfig>(json);
            if (map == null)
            {
                return null;
            }

            ExternalTreasureMaps.Add(map);
            AdventureDataManager.Config.TreasureMap.BiomeInfo.Add(map);
            return RuntimeRegistry.Register(map);
        }
        catch
        {
            OnError?.Invoke("Failed to parse treasure map from external plugin");
            return null;
        }
    }

    /// <param name="key">unique identifier</param>
    /// <param name="json">JSON serialized <see cref="TreasureMapBiomeInfoConfig"/></param>
    /// <returns></returns>
    [PublicAPI]
    public static bool UpdateTreasureMap(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out TreasureMapBiomeInfoConfig original))
        {
            return false;
        }

        try
        {
            var map = JsonConvert.DeserializeObject<TreasureMapBiomeInfoConfig>(json);
            original.CopyFieldsFrom(map);
            return true;
        }
        catch
        {
            OnError?.Invoke("Failed to parse treasure map from external plugin");
            return false;
        }
    }
}