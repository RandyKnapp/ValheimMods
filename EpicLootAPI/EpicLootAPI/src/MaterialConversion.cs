using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EpicLootAPI;

[Serializable][PublicAPI]
public enum MaterialConversionType
{
    Upgrade,
    Convert,
    Junk
}

[Serializable][PublicAPI]
public class MaterialConversionRequirement
{
    public string Item = "";
    public int Amount;

    public MaterialConversionRequirement(string item, int amount = 1)
    {
        Item = item;
        Amount = amount;
    }
    
    public MaterialConversionRequirement(){}
}

[Serializable][PublicAPI]
public class MaterialConversion
{
    public string Name = "";
    public string Product = "";
    public int Amount;
    public MaterialConversionType Type;
    public List<MaterialConversionRequirement> Resources = new();
    [Description("Creates a new material conversion definition.")]

    public MaterialConversion(MaterialConversionType type, string name, string product, int amount = 1)
    {
        Name = name;
        Product = product;
        Amount = amount;
        Type = type;

        MaterialConversions.Add(this);
    }
    
    public MaterialConversion(){}
    
    internal static readonly Method API_AddMaterialConversion = new("AddMaterialConversion");
    internal static readonly Method API_UpdateMaterialConversion = new ("UpdateMaterialConversion");
    internal static readonly List<MaterialConversion> MaterialConversions = new();

    public static void RegisterAll()
    {
        foreach (MaterialConversion conversion in new List<MaterialConversion>(MaterialConversions))
        {
            conversion.Register();
        }
    }

    /// <summary>
    /// Register material conversion to EpicLoot MaterialConversions.Conversions
    /// </summary>
    /// <returns>true if added to MaterialConversions.Conversions</returns>
    [Description("serializes to json and sends to EpicLoot")]
    public bool Register()
    {
        string data = JsonConvert.SerializeObject(this);
        object[] result = API_AddMaterialConversion.Invoke(data);

        if (result[0] is not string key)
        {
            return false;
        }

        MaterialConversions.Remove(this);
        RunTimeRegistry.Register(this, key);
        EpicLoot.logger.LogDebug($"Registered material conversion: {Name}");
        return true;
    }

    /// <summary>
    /// Invokes UpdateMaterialConversions with unique key and serialized MaterialConversion
    /// </summary>
    /// <returns>true if updated</returns>
    public bool Update()
    {
        if (!RunTimeRegistry.TryGetValue(this, out var key))
        {
            return false;
        }

        string json = JsonConvert.SerializeObject(this);
        object[] result =  API_UpdateMaterialConversion.Invoke(key, json);
        bool output = (bool)(result[0] ?? false);
        EpicLoot.logger.LogDebug($"Updated material conversion: {Name}, {output}");
        return output;
    }
}