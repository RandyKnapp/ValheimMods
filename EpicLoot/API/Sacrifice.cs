using EpicLoot.Crafting;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace EpicLoot;

public static partial class API
{
    /// <param name="json">JSON serialized <see cref="DisenchantProductsConfig"/></param>
    /// <returns>True if added to <see cref="EnchantCostsHelper.Config"/></returns>
    [PublicAPI]
    public static string AddSacrifice(string json)
    {
        try
        {
            DisenchantProductsConfig sacrifice = JsonConvert.DeserializeObject<DisenchantProductsConfig>(json);

            if (sacrifice == null)
            {
                return null;
            }

            ExternalSacrifices.Add(sacrifice);
            EnchantCostsHelper.Config.DisenchantProducts.Add(sacrifice);
            return RuntimeRegistry.Register(sacrifice);
        }
        catch
        {
            OnError?.Invoke("Failed to parse sacrifice from external plugin");
            return null;
        }
    }

    /// <param name="key">unique identifier</param>
    /// <param name="json">JSON serialized <see cref="DisenchantProductsConfig"/></param>
    /// <returns>True if updated</returns>
    [PublicAPI]
    public static bool UpdateSacrifice(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out DisenchantProductsConfig disenchantProduct))
        {
            return false;
        }

        DisenchantProductsConfig sacrifice = JsonConvert.DeserializeObject<DisenchantProductsConfig>(json);
        disenchantProduct.CopyFieldsFrom(sacrifice);
        return true;
    }
}