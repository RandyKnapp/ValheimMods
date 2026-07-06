using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EpicLootAPI;

[Serializable]
[PublicAPI]
public class TreasureMap
{
    public Heightmap.Biome Biome = Heightmap.Biome.None;
    public int Cost;
    public int ForestTokens;
    public int GoldTokens;
    public int IronTokens;
    public int Coins;
    public float MinRadius;
    public float MaxRadius;

    public TreasureMap(Heightmap.Biome biome, int cost, float minRadius, float maxRadius)
    {
        Biome = biome;
        Cost = cost;
        MinRadius = minRadius;
        MaxRadius = maxRadius;

        Treasures.Add(this);
    }
    
    public TreasureMap(){}
    
    internal static readonly List<TreasureMap> Treasures = new();
    private static readonly Method API_AddTreasureMap = new("AddTreasureMap");
    private static readonly Method API_UpdateTreasureMap = new("UpdateTreasureMap");

    public static void RegisterAll()
    {
        foreach (TreasureMap treasure in new List<TreasureMap>(Treasures))
        {
            treasure.Register();
        }
    }

    public bool Register()
    {
        string json = JsonConvert.SerializeObject(this);
        object[] result = API_AddTreasureMap.Invoke(json);
        if (result[0] is not string key) return false;
        RunTimeRegistry.Register(this, key);
        Treasures.Remove(this);
        EpicLoot.logger.LogDebug($"Added treasure map: {Biome}");
        return true;
    }

    public bool Update()
    {
        if (!RunTimeRegistry.TryGetValue(this, out string key)) return false;
        string json = JsonConvert.SerializeObject(this);
        object[] result = API_UpdateTreasureMap.Invoke(key, json);
        bool output = (bool)(result[0] ?? false);
        EpicLoot.logger.LogDebug($"Updated treasure map: {Biome}, {output}");
        return output;
    }
    
}