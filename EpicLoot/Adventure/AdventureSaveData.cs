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
        Claimed,
        Abandoned
    }

    [Serializable]
    public class BountyTargetInfo
    {
        public string MonsterID = "";
        public int Count = 1;
        public int Level = 1;

        public void ToPackage(ZPackage pkg)
        {
            pkg.Write(MonsterID);
            pkg.Write(Count);
            pkg.Write(Level);
        }

        public static BountyTargetInfo FromPackage(ZPackage pkg)
        {
            var result = new BountyTargetInfo();
            result.MonsterID = pkg.ReadString();
            result.Count = pkg.ReadInt();
            result.Level = pkg.ReadInt();
            return result;
        }
    }

    [Serializable]
    public class BountyInfo
    {
        public const int Version = 1;

        public int Interval;
        public long PlayerID;
        public Heightmap.Biome Biome;
        public BountyState State;
        public BountyTargetInfo Target = new BountyTargetInfo();
        public string TargetName = "";
        public int RewardIron;
        public int RewardGold;
        public int RewardCoins;
        public SerializableVector3 Position;
        public SerializableVector3 MinimapCircleOffset;
        public List<BountyTargetInfo> Adds = new List<BountyTargetInfo>();
        public bool Slain;

        public string ID => $"Bounty.{PlayerID}.{Interval}.{Biome}.{Target.MonsterID}";

        public void ToPackage(ZPackage pkg)
        {
            pkg.Write(Version);
            pkg.Write(Interval);
            pkg.Write(PlayerID);
            pkg.Write((int)Biome);
            pkg.Write((int)State);
            Target.ToPackage(pkg);
            pkg.Write(TargetName);
            pkg.Write(RewardIron);
            pkg.Write(RewardGold);
            pkg.Write(RewardCoins);
            Position.ToPackage(pkg);
            MinimapCircleOffset.ToPackage(pkg);
            pkg.Write(Adds.Count);
            foreach (var targetInfo in Adds)
            {
                targetInfo.ToPackage(pkg);
            }
            pkg.Write(Slain);
        }

        public static BountyInfo FromPackage(ZPackage pkg)
        {
            var result = new BountyInfo();
            var version = pkg.ReadInt();
            result.Interval = pkg.ReadInt();
            result.PlayerID = pkg.ReadLong();
            result.Biome = (Heightmap.Biome)pkg.ReadInt();
            result.State = (BountyState)pkg.ReadInt();
            result.Target = BountyTargetInfo.FromPackage(pkg);
            result.TargetName = pkg.ReadString();
            result.RewardIron = pkg.ReadInt();
            result.RewardGold = pkg.ReadInt();
            result.RewardCoins = pkg.ReadInt();
            result.Position = SerializableVector3.FromPackage(pkg);
            result.MinimapCircleOffset = SerializableVector3.FromPackage(pkg);

            var addsCount = pkg.ReadInt();
            result.Adds = new List<BountyTargetInfo>();
            for (var index = 0; index < addsCount; index++)
            {
                result.Adds.Add(BountyTargetInfo.FromPackage(pkg));
            }

            result.Slain = pkg.ReadBool();

            return result;
        }

        public static BountyInfo FromBountyID(string ID)
        {
            try
            {
                return Player.m_localPlayer.GetAdventureSaveData().Bounties.Where(b => b.ID == ID).Single();
            }
            catch
            {
                EpicLoot.LogError($"Bounty {ID} not found");
                return null;
            }
        }
    }

    [Serializable]
    public class AdventureSaveDataList
    {
        public List<AdventureSaveData> AllSaveData = new List<AdventureSaveData>();
    }

    [Serializable]
    public class AdventureSaveData
    {
        public long WorldID;
        public int NumberOfTreasureMapsOrBountiesStarted;
        public List<TreasureMapChestInfo> TreasureMaps = new();
        public List<BountyInfo> Bounties = new();

        [NonSerialized] public bool DebugMode;
        [NonSerialized] public int IntervalOverride;

        public bool PurchasedTreasureMap(int interval, Heightmap.Biome biome, Vector3 position, Vector3 circleOffset)
        {
            if (!DebugMode)
            {
                if (HasPurchasedTreasureMap(interval, biome))
                {
                    EpicLoot.LogError($"Player has already purchased treasure map! (interval={interval} biome={biome})");
                    return false;
                }
            }
            else if (IntervalOverride != 0)
            {
                interval = IntervalOverride;
            }

            var chestInfo = new TreasureMapChestInfo()
            {
                Interval = interval,
                Biome = biome,
                State = TreasureMapState.Purchased,
                Position = position,
                MinimapCircleOffset = circleOffset,
            };

            TreasureMaps.Add(chestInfo);

            NumberOfTreasureMapsOrBountiesStarted++;

            var key = new Tuple<int, Heightmap.Biome>(chestInfo.Interval, chestInfo.Biome);
            if (!MinimapController.TreasureMapPins.ContainsKey(key))
            {
                var pinInfo = new AreaPinInfo
                {
                    Position = chestInfo.Position + chestInfo.MinimapCircleOffset,
                    Type = EpicLoot.TreasureMapPinType,
                    Name = Localization.instance.Localize("$mod_epicloot_treasurechest_minimappin", Localization.instance.Localize($"$biome_{chestInfo.Biome.ToString().ToLowerInvariant()}"), (chestInfo.Interval + 1).ToString())
                };

                var pinJob = new PinJob
                {
                    Task = MinimapPinQueueTask.AddTreasurePin,
                    DebugMode = DebugMode,
                    TreasurePin = new KeyValuePair<Tuple<int, Heightmap.Biome>, AreaPinInfo>(key, pinInfo)
                };

                MinimapController.AddPinJobToQueue(pinJob);
            }
            
            return true;
        }

        public bool FoundTreasureChest(int interval, Heightmap.Biome biome)
        {
            var treasureMap = GetTreasureMapChestInfo(interval, biome);
            if (treasureMap != null && treasureMap.State == TreasureMapState.Purchased)
            {
                treasureMap.State = TreasureMapState.Found;
                
                var key = new Tuple<int, Heightmap.Biome>(treasureMap.Interval, treasureMap.Biome);

                if (!MinimapController.TreasureMapPins.ContainsKey(key)) return true;
                
                var pinJob = new PinJob
                {
                    Task = MinimapPinQueueTask.RemoveTreasurePin,
                    DebugMode = DebugMode,
                    TreasurePin = new KeyValuePair<Tuple<int, Heightmap.Biome>, AreaPinInfo>(key, MinimapController.TreasureMapPins[key])
                };
                MinimapController.AddPinJobToQueue(pinJob);
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
            
            var key = bounty.ID;
            if (!MinimapController.BountyPins.ContainsKey(key))
            {
                var pinInfo = new AreaPinInfo
                {
                    Position = bounty.Position + bounty.MinimapCircleOffset,
                    Type = EpicLoot.BountyPinType,
                    Name = Localization.instance.Localize("$mod_epicloot_bounties_minimappin", AdventureDataManager.GetBountyName(bounty))
                };

                var pinJob = new PinJob
                {
                    Task = MinimapPinQueueTask.AddBountyPin,
                    DebugMode = DebugMode,
                    BountyPin = new KeyValuePair<string, AreaPinInfo>(key, pinInfo)
                };

                MinimapController.AddPinJobToQueue(pinJob);
            }


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

        public void AbandonedBounty(string bountyID)
        {
            var bounty = GetBountyInfoByID(bountyID);
            if (bounty != null && bounty.State == BountyState.InProgress)
            {
                bounty.State = BountyState.Abandoned;
            }
        }
    }
}
