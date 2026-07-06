using EpicLoot.Adventure;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;

namespace EpicLoot;

public static partial class API
{
    private enum SecretStashType
    {
        Materials,
        RandomItems,
        OtherItems,
        Gamble,
        Sale
    }
    
    /// <param name="type"><see cref="SecretStashType"/></param>
    /// <param name="json">JSON serialized <see cref="SecretStashItemConfig"/></param>
    /// <returns>unique identifier if added</returns>
    [PublicAPI]
    public static string AddSecretStashItem(string type, string json)
    {
        try
        {
            if (!Enum.TryParse(type, true, out SecretStashType stashType))
            {
                return null;
            }

            SecretStashItemConfig secretStash = JsonConvert.DeserializeObject<SecretStashItemConfig>(json);

            if (secretStash == null)
            {
                return null;
            }

            ExternalSecretStashItems.AddOrSet(stashType, secretStash);
            switch (stashType)
            {
                case SecretStashType.Materials:
                    AdventureDataManager.Config.SecretStash.Materials.Add(secretStash);
                    break;
                case SecretStashType.OtherItems:
                    AdventureDataManager.Config.SecretStash.OtherItems.Add(secretStash);
                    break;
                case SecretStashType.RandomItems:
                    AdventureDataManager.Config.SecretStash.RandomItems.Add(secretStash);
                    break;
                case SecretStashType.Gamble:
                    AdventureDataManager.Config.Gamble.GambleCosts.Add(secretStash);
                    break;
                case SecretStashType.Sale:
                    AdventureDataManager.Config.TreasureMap.SaleItems.Add(secretStash);
                    break;
            }
            return RuntimeRegistry.Register(secretStash);
        }
        catch
        {
            OnError?.Invoke("Failed to parse secret stash from external plugin");
            return null;
        }
    }
    
    /// <param name="key">unique identifier</param>
    /// <param name="json">JSON serialized <see cref="SecretStashItemConfig"/></param>
    /// <returns>True if fields copied</returns>
    [PublicAPI]
    public static bool UpdateSecretStashItem(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out SecretStashItemConfig original))
        {
            return false;
        }

        SecretStashItemConfig secretStash = JsonConvert.DeserializeObject<SecretStashItemConfig>(json);

        if (secretStash == null)
        {
            return false;
        }

        original.CopyFieldsFrom(secretStash);
        return true;
    }
}