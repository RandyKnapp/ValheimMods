using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EpicLootAPI;

[Serializable]
[PublicAPI]
public class BountyMinion
{
    public string ID = "";
    public int Count;

    public BountyMinion(string ID, int count)
    {
        this.ID = ID;
        Count = count;
    }
    
    public BountyMinion(){}
}

[Serializable]
[PublicAPI]
public class BountyTarget
{
    public Heightmap.Biome Biome = Heightmap.Biome.None;
    public string TargetID = "";
    public int RewardGold;
    public int RewardIron;
    public int RewardCoins;
    public List<BountyMinion> Adds = new List<BountyMinion>();

    public BountyTarget(Heightmap.Biome biome, string targetID)
    {
        Biome = biome;
        TargetID = targetID;
            
        BountyTargets.Add(this);
    }
    
    public BountyTarget(){}
    
    internal static readonly List<BountyTarget> BountyTargets = new();
    internal static readonly Method API_AddBountyTarget = new("AddBountyTarget");
    internal static readonly Method API_UpdateBountyTarget = new("UpdateBountyTarget");

    public static void RegisterAll()
    {
        foreach (BountyTarget bounty in new List<BountyTarget>(BountyTargets))
        {
            bounty.Register();
        }
    }

    public bool Register()
    {
        string json = JsonConvert.SerializeObject(this);
        object[] result = API_AddBountyTarget.Invoke(json);
        if (result[0] is not string key)
        {
            return false;
        }

        RunTimeRegistry.Register(BountyTargets, key);
        BountyTargets.Remove(this);
        EpicLoot.logger.LogDebug($"Registered bounty: {TargetID}");
        return true;
    }

    public bool Update()
    {
        if (!RunTimeRegistry.TryGetValue(this, out var key))
        {
            return false;
        }

        string json = JsonConvert.SerializeObject(BountyTargets);
        object[] result =  API_UpdateBountyTarget.Invoke(key, json);
        bool output = (bool)(result[0] ?? false);
        EpicLoot.logger.LogDebug($"Updated bounty target: {TargetID}, {output}");
        return output;
    }
}