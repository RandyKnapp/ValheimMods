using Common;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public long PlayerID;

        public void ToPackage(ZPackage pkg)
        {
            pkg.Write(Interval);
            pkg.Write((int)Biome);
            pkg.Write((int)State);
            Position.ToPackage(pkg);
            MinimapCircleOffset.ToPackage(pkg);
            pkg.Write(PlayerID);
        }

        public static TreasureMapChestInfo FromPackage(ZPackage pkg)
        {
            var result = new TreasureMapChestInfo();
            result.Interval = pkg.ReadInt();
            result.Biome = (Heightmap.Biome)pkg.ReadInt();
            result.State = (TreasureMapState)pkg.ReadInt();
            result.Position = SerializableVector3.FromPackage(pkg);
            result.MinimapCircleOffset = SerializableVector3.FromPackage(pkg);
            result.PlayerID = pkg.ReadLong();
            return result;
        }
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
    }

    

    [Serializable]
    public class AdventureSaveDataList
    {
        // Format version for the compact binary save. Bump when the layout changes.
        public const int Version = 1;

        public List<AdventureSaveData> AllSaveData = new List<AdventureSaveData>();

        public void ToPackage(ZPackage pkg)
        {
            pkg.Write(Version);
            pkg.Write(AllSaveData.Count);
            foreach (var saveData in AllSaveData)
            {
                saveData.ToPackage(pkg);
            }
        }

        public static AdventureSaveDataList FromPackage(ZPackage pkg)
        {
            var result = new AdventureSaveDataList();
            pkg.ReadInt(); // Version (reserved for future format branching)
            var count = pkg.ReadInt();
            result.AllSaveData = new List<AdventureSaveData>(count);
            for (var index = 0; index < count; index++)
            {
                result.AllSaveData.Add(AdventureSaveData.FromPackage(pkg));
            }
            return result;
        }
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

        public void ToPackage(ZPackage pkg)
        {
            pkg.Write(WorldID);
            pkg.Write(NumberOfTreasureMapsOrBountiesStarted);

            pkg.Write(TreasureMaps.Count);
            foreach (var treasureMap in TreasureMaps)
            {
                treasureMap.ToPackage(pkg);
            }

            pkg.Write(Bounties.Count);
            foreach (var bounty in Bounties)
            {
                bounty.ToPackage(pkg);
            }
        }

        public static AdventureSaveData FromPackage(ZPackage pkg)
        {
            var result = new AdventureSaveData();
            result.WorldID = pkg.ReadLong();
            result.NumberOfTreasureMapsOrBountiesStarted = pkg.ReadInt();

            var treasureMapCount = pkg.ReadInt();
            result.TreasureMaps = new List<TreasureMapChestInfo>(treasureMapCount);
            for (var index = 0; index < treasureMapCount; index++)
            {
                result.TreasureMaps.Add(TreasureMapChestInfo.FromPackage(pkg));
            }

            var bountyCount = pkg.ReadInt();
            result.Bounties = new List<BountyInfo>(bountyCount);
            for (var index = 0; index < bountyCount; index++)
            {
                result.Bounties.Add(BountyInfo.FromPackage(pkg));
            }

            return result;
        }

        /// <summary>
        /// Drops terminal-state records from intervals that have already elapsed. The board and
        /// shop regenerate deterministically per interval, so past-interval finished records are
        /// never read again. Current-interval records are kept: pruning them would let the same
        /// map/bounty reappear as available this interval.
        /// </summary>
        public int PruneStaleRecords(int currentBountyInterval, int currentTreasureInterval)
        {
            var removed = Bounties.RemoveAll(x =>
                (x.State == BountyState.Claimed || x.State == BountyState.Abandoned)
                && x.Interval < currentBountyInterval);

            removed += TreasureMaps.RemoveAll(x =>
                x.State == TreasureMapState.Found
                && x.Interval < currentTreasureInterval);

            return removed;
        }

        public bool PurchasedTreasureMap(TreasureMapChestInfo chestInfo)
        {
            if (!DebugMode)
            {
                if (HasPurchasedTreasureMap(chestInfo.Interval, chestInfo.Biome))
                {
                    EpicLoot.LogError($"Player has already purchased treasure map! (interval={chestInfo.Interval} biome={chestInfo.Biome})");
                    return false;
                }
            }
            else if (IntervalOverride != 0)
            {
                chestInfo.Interval = IntervalOverride;
            }

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

        /// <summary>
        /// Moves a purchased-but-unfound treasure map to a new world position and drags its minimap
        /// pin along with it. Called when the spawner had to search outside the original map circle
        /// (almost always because a ward covered it) - without this the pin would keep pointing at a
        /// spot the chest is not in.
        /// </summary>
        public bool RelocateTreasureMap(int interval, Heightmap.Biome biome, Vector3 newPosition)
        {
            var treasureMap = GetTreasureMapChestInfo(interval, biome);
            if (treasureMap == null || treasureMap.State != TreasureMapState.Purchased)
            {
                return false;
            }

            treasureMap.Position = newPosition;
            // The circle is re-centred on the chest, so any old offset would just skew it back.
            treasureMap.MinimapCircleOffset = Vector3.zero;

            var key = new Tuple<int, Heightmap.Biome>(interval, biome);
            if (MinimapController.TreasureMapPins.TryGetValue(key, out var existingPin))
            {
                // The queue is drained FIFO, so remove-then-add is a move.
                MinimapController.AddPinJobToQueue(new PinJob
                {
                    Task = MinimapPinQueueTask.RemoveTreasurePin,
                    DebugMode = DebugMode,
                    TreasurePin = new KeyValuePair<Tuple<int, Heightmap.Biome>, AreaPinInfo>(key, existingPin)
                });
            }

            var pinInfo = new AreaPinInfo
            {
                Position = treasureMap.Position + treasureMap.MinimapCircleOffset,
                Type = EpicLoot.TreasureMapPinType,
                Name = Localization.instance.Localize("$mod_epicloot_treasurechest_minimappin",
                    Localization.instance.Localize($"$biome_{biome.ToString().ToLowerInvariant()}"),
                    (interval + 1).ToString())
            };

            MinimapController.AddPinJobToQueue(new PinJob
            {
                Task = MinimapPinQueueTask.AddTreasurePin,
                DebugMode = DebugMode,
                TreasurePin = new KeyValuePair<Tuple<int, Heightmap.Biome>, AreaPinInfo>(key, pinInfo)
            });

            return true;
        }

        public TreasureMapChestInfo GetTreasureMapChestInfo(int interval, Heightmap.Biome biome)
        {
            return TreasureMaps.Find(x => x.Interval == interval && x.Biome == biome);
        }

        public bool HasPurchasedTreasureMap(int interval, Heightmap.Biome biome)
        {
            return TreasureMaps.Exists(x => x.Interval == interval && x.Biome == biome);
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

        /// <summary>
        /// Bounty counterpart to <see cref="RelocateTreasureMap"/>: moves an in-progress bounty's
        /// world position and its minimap pin when the spawner had to place the targets outside the
        /// original circle.
        /// </summary>
        public bool RelocateBounty(string bountyID, Vector3 newPosition)
        {
            var bounty = GetBountyInfoByID(bountyID);
            if (bounty == null || bounty.State != BountyState.InProgress)
            {
                return false;
            }

            bounty.Position = newPosition;
            bounty.MinimapCircleOffset = Vector3.zero;

            if (MinimapController.BountyPins.TryGetValue(bountyID, out var existingPin))
            {
                MinimapController.AddPinJobToQueue(new PinJob
                {
                    Task = MinimapPinQueueTask.RemoveBountyPin,
                    DebugMode = DebugMode,
                    BountyPin = new KeyValuePair<string, AreaPinInfo>(bountyID, existingPin)
                });
            }

            var pinInfo = new AreaPinInfo
            {
                Position = bounty.Position + bounty.MinimapCircleOffset,
                Type = EpicLoot.BountyPinType,
                Name = Localization.instance.Localize("$mod_epicloot_bounties_minimappin",
                    AdventureDataManager.GetBountyName(bounty))
            };

            MinimapController.AddPinJobToQueue(new PinJob
            {
                Task = MinimapPinQueueTask.AddBountyPin,
                DebugMode = DebugMode,
                BountyPin = new KeyValuePair<string, AreaPinInfo>(bountyID, pinInfo)
            });

            return true;
        }
    }
}
