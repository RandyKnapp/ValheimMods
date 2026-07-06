using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EpicLootAPI;

[Serializable]
[PublicAPI]
public class ItemAmount
{
    public string Item = "";
    public int Amount;

    public ItemAmount(string item, int amount = 1)
    {
        Item = item;
        Amount = amount;
    }
    
    public ItemAmount(){}
}

[Serializable]
[PublicAPI]
public class Sacrifice
{
    [Description("Conditional, checks item needs to be magic")]
    public bool IsMagic;

    [Description("Can be null")]
    public ItemRarity Rarity;

    [Description("Conditional, if empty, does not check if item is of correct type")]
    public List<string> ItemTypes = new List<string>();

    [Description("Conditional, if empty, does not check if item shared name is in list")]
    public List<string> ItemNames = new List<string>();

    [Description("Disenchant product entry")]
    public List<ItemAmount> Products = new List<ItemAmount>();
    
    public Sacrifice()
    {
        Sacrifices.Add(this);
    }

    public void AddRequiredItemType(params ItemDrop.ItemData.ItemType[] types)
    {
        foreach (ItemDrop.ItemData.ItemType type in types)
        {
            ItemTypes.Add(type.ToString());
        }
    }
    
    internal static readonly List<Sacrifice> Sacrifices = new();
    internal static readonly Method API_AddSacrifice = new("AddSacrifice");
    internal static readonly Method API_UpdateSacrifice = new("UpdateSacrifice");

    public static void RegisterAll()
    {
        foreach (Sacrifice sacrifice in new List<Sacrifice>(Sacrifices))
        {
            sacrifice.Register();
        }
    }

    public bool Register()
    {
        string json = JsonConvert.SerializeObject(this);
        object[] result = API_AddSacrifice.Invoke(json);

        if (result[0] is not string key)
        {
            return false;
        }

        Sacrifices.Remove(this);
        RunTimeRegistry.Register(this, key);
        EpicLoot.logger.LogDebug("Registered sacrifice");
        return true;
    }

    public bool Update()
    {
        if (!RunTimeRegistry.TryGetValue(this, out var key))
        {
            return false;
        }

        string json = JsonConvert.SerializeObject(this);
        object[] result = API_UpdateSacrifice.Invoke(key, json);
        bool output = (bool)(result[0] ?? false);
        EpicLoot.logger.LogDebug($"Updated sacrifice: {output}");
        return output;
    }
}