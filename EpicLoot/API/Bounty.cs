using EpicLoot.Adventure;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot;

public static partial class API
{
    [PublicAPI]
    public static string AddBountyTarget(string json)
    {
        try
        {
            var bounty = JsonConvert.DeserializeObject<BountyTargetConfig>(json);

            if (bounty == null)
            {
                return null;
            }

            ExternalBountyTargets.Add(bounty);
            AdventureDataManager.Config.Bounties.Targets.Add(bounty);
            return RuntimeRegistry.Register(bounty);
        }
        catch
        {
            OnError?.Invoke("Failed to parse bounty target passed in through external plugin.");
            return null;
        }
    }

    [PublicAPI]
    public static bool UpdateBountyTarget(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out BountyTargetConfig bountyTarget))
        {
            return false;
        }

        try
        {
            BountyTargetConfig config = JsonConvert.DeserializeObject<BountyTargetConfig>(json);
            if (config == null)
            {
                return false;
            }

            bountyTarget.CopyFieldsFrom(config);
            return true;
        }
        catch
        {
            OnError?.Invoke("Failed to parse bounty target from external plugin");
            return false;
        }
    }

    /// <summary>
    /// The local player's accepted bounties in the current world that are still being hunted
    /// (<c>InProgress</c>) or are finished but not yet claimed (<c>Complete</c>), as a JSON array of
    /// <c>{ Id, PlayerId, MonsterId, Name, Biome, X, Y, Z, Radius, State, Slain, Adds: [{ MonsterId, Count }],
    /// RewardIron, RewardGold, RewardCoins, Interval }</c>.
    /// </summary>
    /// <remarks>
    /// X/Y/Z is the centre of the circle the owner's map shows, and Radius its radius in metres. The
    /// creatures spawn somewhere inside that circle, chosen later by whichever machine is near it, so the
    /// exact spot is never recorded here. Name is localized on this client. An add's Count is how many
    /// are still alive. Bounties live in the character's save, so this only ever describes the local
    /// player; pair it with <see cref="AddAdventureDataChangedListener"/> to hear about changes.
    /// </remarks>
    /// <returns>A JSON array, "[]" with no local player or on error</returns>
    [PublicAPI]
    public static string GetActiveBounties()
    {
        try
        {
            Player player = Player.m_localPlayer;
            if (player == null || ZNet.m_world == null)
            {
                return "[]";
            }

            float radius = MinimapController.AreaRadius;
            List<ActiveBountyJson> active = new();
            foreach (BountyInfo bounty in player.GetAdventureSaveData().Bounties)
            {
                if (bounty?.Target == null ||
                    (bounty.State != BountyState.InProgress && bounty.State != BountyState.Complete))
                {
                    continue;
                }

                Vector3 centre = bounty.Position + bounty.MinimapCircleOffset;
                ActiveBountyJson entry = new()
                {
                    Id = bounty.ID,
                    PlayerId = bounty.PlayerID,
                    MonsterId = bounty.Target.MonsterID,
                    Name = AdventureDataManager.GetBountyName(bounty),
                    Biome = bounty.Biome.ToString(),
                    X = centre.x,
                    Y = centre.y,
                    Z = centre.z,
                    Radius = radius,
                    State = bounty.State.ToString(),
                    Slain = bounty.Slain,
                    RewardIron = bounty.RewardIron,
                    RewardGold = bounty.RewardGold,
                    RewardCoins = bounty.RewardCoins,
                    Interval = bounty.Interval
                };

                if (bounty.Adds != null)
                {
                    foreach (BountyTargetInfo add in bounty.Adds)
                    {
                        entry.Adds.Add(new ActiveBountyAddJson { MonsterId = add.MonsterID, Count = add.Count });
                    }
                }

                active.Add(entry);
            }

            return JsonConvert.SerializeObject(active);
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Failed to list active bounties: {ex.Message}");
            return "[]";
        }
    }

    /// <summary>
    /// The minimap pin type Epic Loot draws bounty icons with. Pins of this type show Epic Loot's bounty
    /// icon and follow its bounty filter button on the large map.
    /// </summary>
    /// <returns>The pin type as an int, or -1 before the minimap has started</returns>
    [PublicAPI]
    public static int GetBountyPinType()
    {
        return (int)EpicLoot.BountyPinType;
    }

    // The JSON shape of GetActiveBounties. Kept private: nothing but the JSON crosses the API boundary.
    private sealed class ActiveBountyJson
    {
        public string Id;
        public long PlayerId;
        public string MonsterId;
        public string Name;
        public string Biome;
        public float X;
        public float Y;
        public float Z;
        public float Radius;
        public string State;
        public bool Slain;
        public List<ActiveBountyAddJson> Adds = new();
        public int RewardIron;
        public int RewardGold;
        public int RewardCoins;
        public int Interval;
    }

    private sealed class ActiveBountyAddJson
    {
        public string MonsterId;
        public int Count;
    }
}