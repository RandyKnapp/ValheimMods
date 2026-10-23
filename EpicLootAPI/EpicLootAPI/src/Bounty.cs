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

        RunTimeRegistry.Register(this, key);
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

        string json = JsonConvert.SerializeObject(this);
        object[] result =  API_UpdateBountyTarget.Invoke(key, json);
        bool output = (bool)(result[0] ?? false);
        EpicLoot.logger.LogDebug($"Updated bounty target: {TargetID}, {output}");
        return output;
    }
}

/// <summary>
/// One of the local player's accepted bounties, as <see cref="EpicLoot.GetActiveBounties"/> reports it.
/// </summary>
[Serializable]
[PublicAPI]
public class ActiveBounty
{
    public string Id = "";
    /// <summary>The owner: the player who accepted it.</summary>
    public long PlayerId;
    public string MonsterId = "";
    /// <summary>Localized on this client.</summary>
    public string Name = "";
    public string Biome = "";
    /// <summary>Centre of the circle on the owner's map; the creatures spawn somewhere inside it.</summary>
    public float X;
    public float Y;
    public float Z;
    /// <summary>The circle's radius, in metres.</summary>
    public float Radius;
    /// <summary>"InProgress" or "Complete" (finished, reward not yet claimed).</summary>
    public string State = "";
    /// <summary>Whether the main target is dead.</summary>
    public bool Slain;
    /// <summary>The minions, with how many of each are still alive.</summary>
    public List<ActiveBountyAdd> Adds = new List<ActiveBountyAdd>();
    public int RewardIron;
    public int RewardGold;
    public int RewardCoins;
    public int Interval;

    public UnityEngine.Vector3 Position => new UnityEngine.Vector3(X, Y, Z);
}

[Serializable]
[PublicAPI]
public class ActiveBountyAdd
{
    public string MonsterId = "";
    /// <summary>How many are still alive.</summary>
    public int Count;
}

public static partial class EpicLoot
{
    private static readonly Method API_GetActiveBounties = new("GetActiveBounties");
    private static readonly Method API_GetBountyPinType = new("GetBountyPinType");

    /// <summary>
    /// The local player's bounties in this world that are being hunted or are finished but unclaimed.
    /// Requires API 2; empty on older versions.
    /// </summary>
    [PublicAPI]
    public static List<ActiveBounty> GetActiveBounties()
    {
        string json = (string)(API_GetActiveBounties.Invoke()[0] ?? "[]");
        try
        {
            return JsonConvert.DeserializeObject<List<ActiveBounty>>(json) ?? new List<ActiveBounty>();
        }
        catch
        {
            logger.LogWarning("Failed to parse active bounty list");
            return new List<ActiveBounty>();
        }
    }

    /// <summary>
    /// The minimap pin type Epic Loot draws bounty icons with, or -1 before the minimap has started (or
    /// before API 2). Pins of this type show the bounty icon and follow Epic Loot's bounty filter.
    /// </summary>
    [PublicAPI]
    public static int GetBountyPinType()
    {
        return (int)(API_GetBountyPinType.Invoke()[0] ?? -1);
    }
}