using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EpicLootAPI;

[PublicAPI]
public enum SecretStashType
{
    Materials,
    RandomItems,
    OtherItems,
    Gamble,
    Sale
}

[Serializable][PublicAPI]
public class SecretStashItem
{
    public string Item = "";
    public int CoinsCost;
    public int ForestTokenCost;
    public int IronBountyTokenCost;
    public int GoldBountyTokenCost;

    public SecretStashItem(SecretStashType type, string item)
    {
        Item = item;
        this.type = type;
        SecretStashes.Add(this);
    }
    
    public SecretStashItem(){}

    private SecretStashType type;
    internal static readonly List<SecretStashItem> SecretStashes = new();
    internal static readonly Method API_AddSecretStashItem = new("AddSecretStashItem");
    internal static readonly Method API_UpdateSecretStashItem = new("UpdateSecretStashItem");
    
    public static void RegisterAll()
    {
        foreach (var item in new List<SecretStashItem>(SecretStashes))
        {
            item.Register();
        }
    }

    public bool Register()
    {
        string json = JsonConvert.SerializeObject(this);
        object[] result = API_AddSecretStashItem.Invoke(type.ToString(), json);

        if (result[0] is not string key)
        {
            return false;
        }

        SecretStashes.Remove(this);
        RunTimeRegistry.Register(type, key);
        EpicLoot.logger.LogDebug($"Registered secret stash {Item}");
        return true;
    }

    public bool Update()
    {
        if (!RunTimeRegistry.TryGetValue(this, out string key))
        {
            return false;
        }

        string json = JsonConvert.SerializeObject(this);
        object[] result = API_UpdateSecretStashItem.Invoke(key, json);
        bool output = (bool)(result[0] ?? false);
        EpicLoot.logger.LogDebug($"Updated secret stash {Item}, {output}");
        return output;
    }
}