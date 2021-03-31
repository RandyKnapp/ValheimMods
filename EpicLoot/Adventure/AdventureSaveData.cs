using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [Serializable]
    public enum TreasureMapState
    {
        Purchased,
        Found
    }

    [Serializable]
    public class TreasureMapChestInfo
    {
        public int Interval;
        public Heightmap.Biome Biome;
        public TreasureMapState State;
        public SerializableVector3 Position;
        public SerializableVector3 MinimapCircleOffset;
    }

    [Serializable]
    public enum BountyState
    {
        Available,
        InProgress,
        Complete,
        Claimed
    }

    [Serializable]
    public class BountyTargetInfo
    {
        public string MonsterID;
        public int Count;
        public int Level;
    }

    [Serializable]
    public class BountyInfo
    {
        public int Interval;
        public Heightmap.Biome Biome;
        public BountyState State;
        public BountyTargetInfo Target;
        public string TargetName;
        public int RewardIron;
        public int RewardGold;
        public SerializableVector3 Position;
        public SerializableVector3 MinimapCircleOffset;
        public List<BountyTargetInfo> Adds = new List<BountyTargetInfo>();
        public bool Slain;

        public string ID => $"Bounty.{Interval}.{Biome}.{Target.MonsterID}";
    }

    [Serializable]
    public class AdventureSaveData
    {
        public int NumberOfTreasureMapsOrBountiesStarted;
        public List<TreasureMapChestInfo> TreasureMaps = new List<TreasureMapChestInfo>();
        public List<BountyInfo> Bounties = new List<BountyInfo>();

        public bool PurchasedTreasureMap(int interval, Heightmap.Biome biome, Vector3 position, Vector3 circleOffset)
        {
            if (HasPurchasedTreasureMap(interval, biome))
            {
                EpicLoot.LogError($"Player has already purchased treasure map! (interval={interval} biome={biome})");
                return false;
            }

            TreasureMaps.Add(new TreasureMapChestInfo()
            {
                Interval = interval,
                Biome = biome,
                State = TreasureMapState.Purchased,
                Position = position,
                MinimapCircleOffset = circleOffset,
            });

            NumberOfTreasureMapsOrBountiesStarted++;

            return true;
        }

        public bool FoundTreasureChest(int interval, Heightmap.Biome biome)
        {
            var treasureMap = GetTreasureMapChestInfo(interval, biome);
            if (treasureMap != null && treasureMap.State == TreasureMapState.Purchased)
            {
                treasureMap.State = TreasureMapState.Found;
                return true;
            }

            return false;
        }

        public TreasureMapChestInfo GetTreasureMapChestInfo(int interval, Heightmap.Biome biome)
        {
            return TreasureMaps.Find(x => x.Interval == interval && x.Biome == biome);
        }

        public bool HasPurchasedTreasureMap(int interval, Heightmap.Biome biome)
        {
            return TreasureMaps.Exists(x => x.Interval == interval && x.Biome == biome);
        }

        public bool HasFoundTreasureMapChest(int interval, Heightmap.Biome biome)
        {
            return TreasureMaps.Exists(x => x.Interval == interval && x.Biome == biome && x.State == TreasureMapState.Found);
        }

        public List<TreasureMapChestInfo> GetUnfoundTreasureChests()
        {
            return TreasureMaps.Where(x => x.State == TreasureMapState.Purchased).ToList();
        }

        public bool AcceptedBounty(BountyInfo bounty, Vector3 spawnPoint, Vector3 offset)
        {
            if (HasAcceptedBounty(bounty.Interval, bounty.ID))
            {
                EpicLoot.LogError($"Player has already accepted bounty! (interval={bounty.Interval} bountyID={bounty.ID})");
                return false;
            }

            if (bounty.State != BountyState.Available)
            {
                EpicLoot.LogError($"Can only accept available bounties! (interval={bounty.Interval} bountyID={bounty.ID})");
                return false;
            }

            bounty.State = BountyState.InProgress;
            bounty.Position = spawnPoint;
            bounty.MinimapCircleOffset = offset;
            Bounties.Add(bounty);

            return true;
        }

        public bool KilledBountyTarget(int interval, string bountyID)
        {
            var bounty = GetBountyInfo(interval, bountyID);
            if (bounty != null && bounty.State == BountyState.InProgress)
            {
                bounty.State = BountyState.Complete;
                return true;
            }

            return false;
        }

        public bool ClaimedBountyReward(int interval, string bountyID)
        {
            var bounty = GetBountyInfo(interval, bountyID);
            if (bounty != null && bounty.State == BountyState.Complete)
            {
                bounty.State = BountyState.Claimed;
                return true;
            }

            return false;
        }

        private BountyInfo GetBountyInfo(int interval, string bountyID)
        {
            return Bounties.Find(x => x.Interval == interval && x.ID == bountyID);
        }

        public BountyInfo GetBountyInfoByID(string bountyID)
        {
            return Bounties.Find(x => x.ID == bountyID);
        }

        public bool HasAcceptedBounty(int interval, string bountyID)
        {
            return Bounties.Exists(x => x.Interval == interval && x.ID == bountyID);
        }

        public bool BountyIsInProgress(int interval, string bountyID)
        {
            return Bounties.Exists(x => x.State == BountyState.InProgress && x.Interval == interval && x.ID == bountyID);
        }

        public List<BountyInfo> GetInProgressBounties()
        {
            return Bounties.Where(x => x.State == BountyState.InProgress).ToList();
        }

        public List<BountyInfo> GetClaimableBounties()
        {
            return Bounties.Where(x => x.State == BountyState.Complete).ToList();
        }
    }
}
