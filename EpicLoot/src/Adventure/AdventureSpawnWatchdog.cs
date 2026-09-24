using EpicLoot.Biomes;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.Adventure
{
    /// <summary>
    /// Checks, from the buyer's side, that a bounty or treasure map actually appears once the buyer is
    /// standing at its map circle, logs what it can see when it does not, and re-creates the spawner
    /// when there is none left to place it.
    ///
    /// The placement itself runs on whichever machine owns the spawner (see
    /// <see cref="AdventureSpawnController"/>), which is often not the buyer's. That machine logs its own
    /// reason when it is slow, but that line lands in someone else's log - or the server's - while the
    /// report comes from the buyer. So the buyer's log says who owns the spawner and what state it is
    /// in, which is enough to tell "another machine is stuck placing it" from "nobody is placing it"
    /// from "it was placed and is gone".
    ///
    /// Only the last case is repaired. With no spawner and nothing it placed anywhere near the circle,
    /// the bounty or map could never be finished, so the buyer drops a new spawner carrying the record
    /// from their own save data - the biome, interval and bounty ID it was bought with, and a bounty's
    /// progress so far. A spawner that still exists is left alone: another machine may be placing it.
    ///
    /// Everything here reads ZDOs the client already holds: the buyer is at the circle, so the server
    /// has synced the objects around it. Recovery additionally requires that the whole circle is inside
    /// the area the server syncs to this client (<see cref="CanSeeWholeCircle"/>), since an original
    /// that is merely out of sight would otherwise be duplicated.
    /// </summary>
    internal class AdventureSpawnWatchdog : MonoBehaviour
    {
        private const float PulseSeconds = 5f;

        /// <summary>How far outside the drawn circle still counts as having reached it.</summary>
        private const float ArrivalMargin = 16f;

        private static readonly int BountyIdHash = BountyTargetComponent.BountyIDKey.GetStableHashCode();
        private static readonly int ChestBiomeHash = $"{nameof(TreasureMapChest)}.{nameof(TreasureMapChest.Biome)}".GetStableHashCode();
        private static readonly int ChestIntervalHash = $"{nameof(TreasureMapChest)}.{nameof(TreasureMapChest.Interval)}".GetStableHashCode();
        private static readonly int SpawnPointHash = AdventureSpawnController.SpawnPointKey.GetStableHashCode();

        private static AdventureSpawnWatchdog _instance;

        private sealed class Watch
        {
            /// <summary>Time.time the player last came within reach of the circle, or -1 while away.</summary>
            public float ArrivedAt = -1f;

            /// <summary>Time.time the spawn was reported missing, or -1 if it has not been.</summary>
            public float ReportedAt = -1f;

            /// <summary>Seen in place; nothing more to check this session.</summary>
            public bool Resolved;

            /// <summary>A replacement spawner was dropped; never a second one this session.</summary>
            public bool Recovered;

            /// <summary>Said once that recovery is waiting for the whole circle to be in sight.</summary>
            public bool DeferralLogged;
        }

        private struct Snapshot
        {
            public ZDO Spawner;
            public int Creatures;
            public bool ChestFound;

            public bool Placed => Creatures > 0 || ChestFound;
        }

        private readonly Dictionary<string, Watch> _watches = new();
        private readonly HashSet<string> _activeKeys = new();
        private readonly List<string> _staleKeys = new();
        private readonly List<ZDO> _zdoScratch = new();
        private ZNet _trackedNet;

        internal static void Create()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("EL_AdventureSpawnWatchdog");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<AdventureSpawnWatchdog>();
        }

        [UsedImplicitly]
        private void Awake()
        {
            _instance = this;
            InvokeRepeating(nameof(Pulse), PulseSeconds, PulseSeconds);
        }

        [UsedImplicitly]
        private void Pulse()
        {
            // Keys are only unique within a world.
            if (ZNet.instance != _trackedNet)
            {
                _trackedNet = ZNet.instance;
                _watches.Clear();
            }

            Player player = Player.m_localPlayer;
            if (player == null || ZNet.instance == null || ZDOMan.instance == null || ZoneSystem.instance == null ||
                ZNetScene.instance == null || !EpicLoot.IsAdventureModeEnabled())
            {
                return;
            }

            AdventureSaveData saveData = player.GetAdventureSaveData();
            if (saveData == null)
            {
                return;
            }

            long playerID = player.GetPlayerID();
            Vector3 playerPosition = player.transform.position;
            float arrivalRadius = MinimapController.AreaRadius + ArrivalMargin;
            _activeKeys.Clear();

            foreach (BountyInfo bounty in saveData.Bounties)
            {
                if (bounty.State != BountyState.InProgress)
                {
                    continue;
                }

                string key = "bounty:" + bounty.ID;
                _activeKeys.Add(key);
                Vector3 centre = AdventureSpawnController.GetCircleCentre(bounty.Position, bounty.MinimapCircleOffset);
                Check(key, centre, playerPosition, arrivalRadius, bounty, null, playerID);
            }

            foreach (TreasureMapChestInfo map in saveData.TreasureMaps)
            {
                if (map.State != TreasureMapState.Purchased)
                {
                    continue;
                }

                string key = $"treasure:{map.Interval}:{map.Biome}";
                _activeKeys.Add(key);
                Vector3 centre = AdventureSpawnController.GetCircleCentre(map.Position, map.MinimapCircleOffset);
                Check(key, centre, playerPosition, arrivalRadius, null, map, playerID);
            }

            // Completed, claimed, abandoned and found entries drop out of the lists above.
            if (_watches.Count > _activeKeys.Count)
            {
                _staleKeys.Clear();
                foreach (string key in _watches.Keys)
                {
                    if (!_activeKeys.Contains(key))
                    {
                        _staleKeys.Add(key);
                    }
                }

                foreach (string key in _staleKeys)
                {
                    _watches.Remove(key);
                }
            }
        }

        private void Check(string key, Vector3 centre, Vector3 playerPosition, float arrivalRadius,
            BountyInfo bounty, TreasureMapChestInfo map, long playerID)
        {
            if (!_watches.TryGetValue(key, out Watch watch))
            {
                watch = new Watch();
                _watches[key] = watch;
            }

            if (watch.Resolved)
            {
                return;
            }

            if (Utils.DistanceXZ(playerPosition, centre) > arrivalRadius)
            {
                // Leaving restarts the clock. A report already made stands, and is followed up if the
                // spawn turns up on a later visit.
                watch.ArrivedAt = -1f;
                return;
            }

            if (watch.ArrivedAt < 0f)
            {
                watch.ArrivedAt = Time.time;
            }

            float waited = Time.time - watch.ArrivedAt;
            bool reported = watch.ReportedAt >= 0f;
            if (!reported && waited < AdventureSpawnController.OverdueSeconds)
            {
                return;
            }

            Snapshot snapshot = TakeSnapshot(centre, bounty, map, playerID);
            if (snapshot.Placed)
            {
                watch.Resolved = true;
                if (reported)
                {
                    EpicLoot.LogForce($"{DescribeSpawn(bounty, map)} is now here, " +
                        $"{Time.time - watch.ReportedAt:0}s after it was reported missing.");
                }

                return;
            }

            if (!reported)
            {
                watch.ReportedAt = Time.time;
                EpicLoot.LogWarningForce($"{DescribeSpawn(bounty, map)} has not appeared {waited:0}s after you reached " +
                    $"its map circle at ({centre.x:0}, {centre.z:0}). {DescribeSpawner(snapshot)}");
            }

            // A report made on an earlier visit snapshots as soon as the player is back, before the ZDOs
            // around the circle have had time to arrive, so recovery waits out the full delay on this
            // visit whatever was reported before.
            if (snapshot.Spawner != null || watch.Recovered || waited < AdventureSpawnController.OverdueSeconds)
            {
                return;
            }

            Vector3 spawnerPosition = bounty != null ? bounty.Position : map.Position;
            if (!CanSeeWholeCircle(centre, spawnerPosition))
            {
                if (!watch.DeferralLogged)
                {
                    watch.DeferralLogged = true;
                    EpicLoot.LogForce($"{DescribeSpawn(bounty, map)} will be re-created once its whole map circle is " +
                        "inside this client's simulation area, so that an original out of sight is not duplicated. " +
                        "Move towards the circle's centre.");
                }

                return;
            }

            watch.Recovered = true;
            Recover(bounty, map, centre);
        }

        /// <summary>
        /// Drops a new spawner at the circle, carrying the record from the buyer's save data. The
        /// spawner copies the record into its ZDO, so the save data itself is untouched. It belongs to
        /// this machine, and the buyer is standing right there, so it places straight away.
        /// </summary>
        private static void Recover(BountyInfo bounty, TreasureMapChestInfo map, Vector3 centre)
        {
            string remaining;
            if (bounty != null)
            {
                AdventureSpawnController.CreateForBounty(bounty, centre);
                int adds = bounty.Adds.Sum(x => x.Count);
                remaining = bounty.Slain ? $"its {adds} remaining minion(s)" : $"the target and {adds} minion(s)";
            }
            else
            {
                AdventureSpawnController.CreateForTreasure(map, centre);
                remaining = "a new chest";
            }

            EpicLoot.LogForce($"{DescribeSpawn(bounty, map)}: re-created its spawner at the map circle " +
                $"({centre.x:0}, {centre.z:0}) from your save data. It will place {remaining}.");
        }

        /// <summary>
        /// Whether this machine would hold the ZDOs of the original spawner and anything it placed, had
        /// they still existed: every zone the circle touches, and the zone the original spawner was
        /// dropped in. A client is only sent the objects in its near simulation area - the same area
        /// <see cref="AdventureSpawnController"/> places in - so on the lowest Simulation Distance a
        /// player at the edge of the circle cannot see its far side. The server holds every ZDO.
        /// </summary>
        private static bool CanSeeWholeCircle(Vector3 centre, Vector3 spawnerPosition)
        {
            if (ZNet.instance.IsServer())
            {
                return true;
            }

            float radius = MinimapController.AreaRadius;
            Vector2s min = ZoneSystem.GetZone(centre - new Vector3(radius, 0f, radius));
            Vector2s max = ZoneSystem.GetZone(centre + new Vector3(radius, 0f, radius));
            for (int y = min.y; y <= max.y; y++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    if (!IsZoneInSight(new Vector2s(x, y)))
                    {
                        return false;
                    }
                }
            }

            return IsZoneInSight(ZoneSystem.GetZone(spawnerPosition));
        }

        private static bool IsZoneInSight(Vector2s zone)
        {
            return AdventureSpawnController.IsZoneInLocalNearArea(zone) && AdventureSpawnController.IsZoneInstantiated(zone);
        }

        /// <summary>
        /// The spawner for this bounty or map and whatever it has spawned, from the objects around the
        /// circle. Two zones out, so creatures that have wandered a little are still counted.
        /// </summary>
        private Snapshot TakeSnapshot(Vector3 centre, BountyInfo bounty, TreasureMapChestInfo map, long playerID)
        {
            var snapshot = new Snapshot();
            string chestBiome = map?.Biome.ToString();

            _zdoScratch.Clear();
            ZDOMan.instance.FindSectorObjects(ZoneSystem.GetZone(centre), new SimulationDistance(2, 0), _zdoScratch);

            foreach (ZDO zdo in _zdoScratch)
            {
                if (zdo.GetPrefab() == AdventureSpawnController.PrefabHash)
                {
                    if (snapshot.Spawner == null && IsSpawnerFor(zdo, bounty, map, playerID))
                    {
                        snapshot.Spawner = zdo;
                    }

                    continue;
                }

                if (bounty != null)
                {
                    if (zdo.GetString(BountyIdHash) == bounty.ID)
                    {
                        snapshot.Creatures++;
                    }
                }
                else if (zdo.GetString(ChestBiomeHash) == chestBiome &&
                    zdo.GetInt(ChestIntervalHash, -1) == map.Interval &&
                    zdo.GetLong(ZDOVars.s_creator) == playerID)
                {
                    snapshot.ChestFound = true;
                }
            }

            _zdoScratch.Clear();
            return snapshot;
        }

        private static bool IsSpawnerFor(ZDO zdo, BountyInfo bounty, TreasureMapChestInfo map, long playerID)
        {
            if (bounty != null)
            {
                return AdventureSpawnController.TryReadBounty(zdo, out BountyInfo spawnerBounty) &&
                    spawnerBounty.Target != null && spawnerBounty.ID == bounty.ID;
            }

            return AdventureSpawnController.TryReadTreasure(zdo, out TreasureMapChestInfo spawnerMap) &&
                spawnerMap.PlayerID == playerID && spawnerMap.Interval == map.Interval && spawnerMap.Biome == map.Biome;
        }

        private static string DescribeSpawn(BountyInfo bounty, TreasureMapChestInfo map)
        {
            if (bounty != null)
            {
                return $"Adventure bounty target '{bounty.Target.MonsterID}' ({BiomeDataManager.GetName(bounty.Biome)}, {bounty.ID})";
            }

            return $"Adventure treasure chest ({BiomeDataManager.GetName(map.Biome)}, interval {map.Interval})";
        }

        private static string DescribeSpawner(Snapshot snapshot)
        {
            if (snapshot.Spawner == null)
            {
                return "No spawner for it is near the circle and nothing it spawns is here, so either it placed " +
                    "earlier and what it spawned has since died or moved away, or the spawner was lost.";
            }

            long owner = snapshot.Spawner.GetOwner();
            Vector3 spawnPoint = snapshot.Spawner.GetVec3(SpawnPointHash, AdventureSpawnController.UnsetSpawnPoint);
            string progress = spawnPoint != AdventureSpawnController.UnsetSpawnPoint
                ? $"has chosen a spot at ({spawnPoint.x:0}, {spawnPoint.z:0})"
                : "has not chosen a spot yet";

            string placer;
            if (owner == 0L)
            {
                placer = "Nobody owns it, so no machine is placing it.";
            }
            else if (owner == ZDOMan.GetSessionID())
            {
                placer = "This machine is placing it; the 'has not been placed' line in this log says what it is waiting on.";
            }
            else
            {
                placer = "That machine is placing it, and its log has the reason.";
            }

            return $"Its spawner {snapshot.Spawner.m_uid} is here, owned by {DescribeOwner(owner)}, and {progress}. {placer}";
        }

        /// <summary>
        /// Names the session that owns an object. A client only has the server as a peer, so other
        /// players are matched through the player list, whose character ids carry each player's session.
        /// </summary>
        private static string DescribeOwner(long owner)
        {
            if (owner == 0L)
            {
                return "nobody";
            }

            if (owner == ZDOMan.GetSessionID())
            {
                return "you";
            }

            ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
            if (serverPeer != null && serverPeer.m_uid == owner)
            {
                return "the server";
            }

            foreach (ZNet.PlayerInfo info in ZNet.instance.GetPlayerList())
            {
                if (!info.m_characterID.IsNone() && info.m_characterID.UserID == owner)
                {
                    return $"player '{info.m_name}'";
                }
            }

            // A host sees every other player as a peer of its own.
            ZNetPeer peer = ZNet.instance.GetPeer(owner);
            if (peer != null)
            {
                return $"player '{peer.m_playerName}'";
            }

            return $"session {owner}, which matches no connected player (disconnected, or dead or respawning)";
        }
    }
}
