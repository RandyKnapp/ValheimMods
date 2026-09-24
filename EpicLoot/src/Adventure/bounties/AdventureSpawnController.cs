using EpicLoot.Biomes;
using EpicLoot.Data;
using Jotunn.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace EpicLoot.Adventure
{
    /// <summary>
    /// How a spawn point that lands in open water is resolved. Only the Ocean biome ever reaches
    /// anything but <see cref="Reject"/> -- everywhere else, water means a lake or a river and the
    /// point is thrown away.
    /// </summary>
    internal enum WaterPlacement
    {
        /// <summary>Submerged points are rejected outright.</summary>
        Reject,

        /// <summary>The point stays on the seabed, under the water. Treasure chests sit there.</summary>
        Seabed,

        /// <summary>The point is lifted to the water line. Swimming bounty targets belong there.</summary>
        Surface
    }

    /// <summary>
    /// The persistent object a bounty or treasure map leaves at its map circle. It places the bounty
    /// targets or the chest once someone is close enough for the area to load, then destroys itself.
    ///
    /// Placement runs on whichever machine owns this object, and that is decided by vanilla, not by
    /// us: every couple of seconds the server hands a persistent object to any player within about a
    /// zone of it, unless its current owner is that close too. So the machine placing a bounty may be
    /// another player who got there first, or the dedicated server itself near the world centre.
    /// Nothing about placement may therefore depend on being the buyer - which is why the spawn never
    /// leaves the map circle. The circle lives in the buyer's save data, and no other machine can
    /// move it.
    /// </summary>
    internal class AdventureSpawnController : MonoBehaviour
    {
        internal const string PrefabName = "EL_SpawnController";
        internal static readonly int PrefabHash = PrefabName.GetStableHashCode();

        // ZDO keys. AdventureSpawnWatchdog reads them off spawners it does not own.
        internal const string BountyKey = "bount_spawn";
        internal const string TreasureKey = "treasure_spawn";
        internal const string PlacedKey = "placed";
        internal const string SpawnPointKey = "spawnPoint";
        private const string IsBountyKey = "isBounty";
        private const string SearchingKey = "searchingForSpawn";

        /// <summary>The spawn point a spawner holds before one has been chosen.</summary>
        internal static readonly Vector3 UnsetSpawnPoint = new(1, 1, 1);

        /// <summary>
        /// Seconds a spawner may spend owned by this machine without placing before it logs what it is
        /// waiting on. The buyer-side watchdog uses the same threshold.
        /// </summary>
        internal const float OverdueSeconds = 30f;

        protected ZNetView zNetView;
        private BountyInfoZNetProperty bounty { get; set; }
        private TreasureMapChestInfoZNetProperty treasure { get; set; }
        private BoolZNetProperty placed { get; set; }
        private BoolZNetProperty searchingForSpawn { get; set; }
        private Vector3ZNetProperty spawnPoint { get; set; }

        private BoolZNetProperty isBounty { get; set; }

        /// <summary>
        /// Seconds between lookups of a creature prefab ZNetScene does not have. Only re-installing the
        /// mod that adds it can fix that, so there is no point asking often.
        /// </summary>
        private const float MissingPrefabRetrySeconds = 30f;

        /// <summary>How long the search waits for a world biome index that is still building.</summary>
        private const float IndexWaitTimeoutSeconds = 10f;

        /// <summary>How often the search re-checks whether the area around the circle has loaded.</summary>
        private const float AreaPollSeconds = 0.25f;

        /// <summary>
        /// Spots sampled inside the circle before settling for the best one seen. The search stops at
        /// the first spot that meets every rule, so this budget is only spent when something - usually
        /// a ward - rules out part of the circle.
        /// </summary>
        private const int MaxCandidates = 200;

        /// <summary>
        /// Candidates evaluated before yielding a frame. Each one is two raycasts and a walk over the
        /// loaded wards, so this keeps a search that rejects everything to a few milliseconds a frame.
        /// </summary>
        private const int CandidatesPerFrame = 10;

        /// <summary>
        /// The share of the drawn circle's radius that spots are sampled from. The rest is margin, so
        /// minions placed around the target and a creature that has taken a step or two still read as
        /// inside the circle.
        /// </summary>
        private const float SearchRadiusFraction = 0.8f;

        /// <summary>
        /// The furthest a minion is placed from its bounty target, before it is clamped to the margin
        /// <see cref="SearchRadiusFraction"/> leaves inside the circle.
        /// </summary>
        private const float MaxMinionSpread = 4f;

        /// <summary>How closely a placement met the rules, best first. Every tier is inside the circle.</summary>
        private enum PlacementTier
        {
            /// <summary>Every rule met, including staying a full buffer clear of wards.</summary>
            Clear,

            /// <summary>Every terrain rule met, inside a ward's buffer but outside the area it protects.</summary>
            NearWard,

            /// <summary>Every terrain rule met, inside the area a ward protects.</summary>
            InsideWard,

            /// <summary>No spot met the terrain rules; the least-bad spot seen, or the circle's centre.</summary>
            Fallback
        }

        /// <summary>The first terrain rule a candidate failed, counted for the overdue and placement logs.</summary>
        private enum Rejection
        {
            ZoneNotLoaded,
            WrongBiome,
            NoFloor,
            Obstructed,
            Underwater,
            Lava,
            Count
        }

        /// <summary>What the placement on this machine is currently doing, for the overdue log.</summary>
        private enum PlacementStage
        {
            NotStarted,
            WaitingForIndex,
            WaitingForArea,
            Searching,
            Spawning
        }

        private struct Candidate
        {
            public Vector3 Position;

            /// <summary>Met every terrain rule, so it can be placed at one of the ward tiers.</summary>
            public bool Usable;

            /// <summary>The ward tier, when <see cref="Usable"/>.</summary>
            public PlacementTier Tier;

            public PrivateArea Ward;

            /// <summary>How badly it broke the terrain rules; lower is better, and MaxValue cannot be used.</summary>
            public int Penalty;
        }

        /// <summary>Set once a missing creature prefab has been reported, so it is logged one time.</summary>
        private bool reportedMissingPrefab = false;
        private string missingPrefab;

        /// <summary>Time.time before which Update will not start (or restart) a spawn.</summary>
        private float nextSearchTime = 0f;
        private bool startedPlacement = false;

        // Overdue tracking. Only time this machine actually owns the spawner counts: ownership can move
        // away and back while the player walks the edge of the area, and neither resetting the clock on
        // each move (it would never reach the threshold) nor counting the time away (it would report a
        // search that was never running here) tells the truth.
        private float ownedSeconds;
        private bool reportedOverdue;
        private PlacementStage stage = PlacementStage.NotStarted;

        // Timing and outcome for the placement log line, measured from when this machine started searching.
        private float searchStartTime;
        private float areaReadyTime;
        private Vector3 searchCentre;
        private int candidatesTried;
        private readonly int[] rejections = new int[(int)Rejection.Count];
        private int nearWardCandidates;
        private int insideWardCandidates;
        private PlacementTier placedTier;
        private PrivateArea placedWard;
        private float placedDistance;

        private static readonly List<ZDO> ZoneObjectsScratch = new();
        private readonly Dictionary<Vector2s, bool> zoneReadyCache = new();

        private BountyInfo defaultBounty = new();
        private TreasureMapChestInfo defaultTreasure = new();

        public float StartingHeight = 1000f;

        public void Awake()
        {
            if (gameObject.TryGetComponent<ZNetView>(out zNetView) == false)
            {
                gameObject.AddComponent<ZNetView>();
                zNetView = gameObject.GetComponent<ZNetView>();
                zNetView.m_persistent = true;
            }

            if ((bool)zNetView)
            {
                bounty = new BountyInfoZNetProperty(BountyKey, zNetView, defaultBounty);
                treasure = new TreasureMapChestInfoZNetProperty(TreasureKey, zNetView, defaultTreasure);
                isBounty = new BoolZNetProperty(IsBountyKey, zNetView, false);
                placed = new BoolZNetProperty(PlacedKey, zNetView, false);
                searchingForSpawn = new BoolZNetProperty(SearchingKey, zNetView, false);
                spawnPoint = new Vector3ZNetProperty(SpawnPointKey, zNetView, UnsetSpawnPoint);
            }
        }

        public void Update()
        {
            if (!(bool)zNetView || !zNetView.IsValid() || !zNetView.IsOwner())
            {
                return;
            }

            // A spawner whose ZDO already records a successful placement must never run again. If the
            // owner logged out (or the zone unloaded) between placing and the Destroy at the bottom of
            // this method, the object comes back with spawnPoint already set, falls straight through
            // the searchingForSpawn gate below, and would spawn its contents a second time.
            if (placed.Get() == true)
            {
                ZNetScene.instance.Destroy(this.gameObject);
                return;
            }

            CheckOverdue();

            if (Time.time < nextSearchTime)
            {
                return;
            }

            if (startedPlacement == false)
            {
                EpicLoot.Log("Starting search for valid spawn location...");
                searchingForSpawn.Set(true);
                startedPlacement = true;

                BountyInfo bountyInfo = bounty.Get();
                if (bountyInfo.PlayerID != 0)
                {
                    StartCoroutine(DeterminespawnPoint(GetCircleCentre(bountyInfo.Position, bountyInfo.MinimapCircleOffset),
                        bountyInfo.Biome, WaterPlacement.Surface));
                }

                TreasureMapChestInfo treasureInfo = treasure.Get();
                if (treasureInfo.PlayerID != 0)
                {
                    StartCoroutine(DeterminespawnPoint(GetCircleCentre(treasureInfo.Position, treasureInfo.MinimapCircleOffset),
                        treasureInfo.Biome, WaterPlacement.Seabed));
                }
            }

            if (searchingForSpawn.Get() == true && spawnPoint.Get() == UnsetSpawnPoint)
            {
                return;
            }

            stage = PlacementStage.Spawning;

            if (isBounty.Get() == true)
            {
                SpawnBountyTargets(bounty.Get());
            }
            else
            {
                SpawnChest(treasure.Get());
            }

            if (placed.Get() == true)
            {
                ZNetScene.instance.Destroy(this.gameObject);
            }
        }

        /// <summary>
        /// Drops a spawner for <paramref name="bountyInfo"/> at <paramref name="position"/>. Buying a
        /// bounty and the buyer-side recovery of a lost one (<see cref="AdventureSpawnWatchdog"/>) both
        /// come through here, so a recovered spawner is built exactly like a bought one.
        /// </summary>
        internal static void CreateForBounty(BountyInfo bountyInfo, Vector3 position)
        {
            AdventureSpawnController spawner = Create(position);
            spawner.SetBounty(bountyInfo);
            spawner.SetIsBounty();
        }

        /// <summary>Treasure-map counterpart to <see cref="CreateForBounty"/>.</summary>
        internal static void CreateForTreasure(TreasureMapChestInfo treasureInfo, Vector3 position)
        {
            Create(position).SetTreasure(treasureInfo);
        }

        private static AdventureSpawnController Create(Vector3 position)
        {
            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            GameObject prefab = PrefabManager.Instance.GetPrefab(PrefabName);
            return UnityEngine.Object.Instantiate(prefab, position, rotation).GetComponent<AdventureSpawnController>();
        }

        public void SetBounty(BountyInfo bountyInfo)
        {
            bounty.ForceSet(bountyInfo);
        }

        public void SetIsBounty()
        {
            isBounty.ForceSet(true);
        }

        public void SetTreasure(TreasureMapChestInfo treasureInfo)
        {
            treasure.ForceSet(treasureInfo);
        }

        /// <summary>
        /// The centre of the circle the buyer's map draws: MinimapController and AdventureSaveData both
        /// add the offset, and a spawn searched around the bare position would drift out of a circle
        /// that carries one (older saves do).
        /// </summary>
        internal static Vector3 GetCircleCentre(Vector3 position, Vector3 minimapCircleOffset)
        {
            return position + minimapCircleOffset;
        }

        private void SpawnBountyTargets(BountyInfo bounty)
        {
            Vector3 point = spawnPoint.Get();

            // Only what the bounty still needs. A bought bounty always needs everything; one the
            // watchdog re-created carries the buyer's progress, where Slain means the target is dead
            // and each add's Count is how many of it are left.
            var prefabs = new List<(GameObject Prefab, bool IsAdd)>();
            if (!bounty.Slain)
            {
                var mainPrefab = ZNetScene.instance.GetPrefab(bounty.Target.MonsterID);
                if (mainPrefab == null)
                {
                    ReportMissingPrefab("target", bounty.ID, bounty.Target.MonsterID);
                    return;
                }

                prefabs.Add((mainPrefab, false));
            }

            foreach (var addConfig in bounty.Adds)
            {
                for (var i = 0; i < addConfig.Count; i++)
                {
                    var prefab = ZNetScene.instance.GetPrefab(addConfig.MonsterID);
                    if (prefab == null)
                    {
                        ReportMissingPrefab("add", bounty.ID, addConfig.MonsterID);
                        return;
                    }
                    prefabs.Add((prefab, true));
                }
            }

            string what = bounty.Slain
                ? $"bounty minions of '{bounty.Target.MonsterID}'"
                : $"bounty target '{bounty.Target.MonsterID}'";

            // A bounty with nothing left is complete and never reaches a spawner; this is only a guard.
            if (prefabs.Count == 0)
            {
                EpicLoot.LogForce($"Adventure {what} ({BiomeDataManager.GetName(bounty.Biome)}) had nothing left to spawn.");
                placed.ForceSet(true);
                return;
            }

            // An open-water bounty's targets swim, so they hold the water line the search settled on
            // instead of being dropped onto a seabed that is tens of metres further down. Which
            // biomes count as open water is measured from the world, so a custom ocean-like biome
            // gets swimmers too rather than a pile of drowned creatures on the seabed.
            bool swimmingTargets = WorldBiomeIndex.IsOpenWater(bounty.Biome);

            // Minions go around the target rather than in a chain from one to the next, which drifted
            // a few metres per minion; the spread is held to the margin the search left inside the circle.
            float circleRadius = MinimapController.AreaRadius;
            float minionSpread = Mathf.Clamp(circleRadius * (1f - SearchRadiusFraction), 0f, MaxMinionSpread);
            var placedZdos = new List<ZDO>(prefabs.Count);

            foreach ((GameObject prefab, bool isAdd) in prefabs)
            {
                Vector3 spawnAt = point;
                if (isAdd && minionSpread > 0f)
                {
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * minionSpread;
                    spawnAt.x += offset.x;
                    spawnAt.z += offset.y;

                    // Cast from a little above the target so a minion on a slope finds ground that is
                    // higher than the target's. FindFloor reports 0 when its ray hits nothing at all, so a
                    // miss keeps the target's height instead of dropping the minion to y=0.
                    if (!swimmingTargets && ZoneSystem.instance.FindFloor(spawnAt + Vector3.up * 2f, out float floorHeight))
                    {
                        spawnAt.y = floorHeight;
                    }
                }

                // Character.UpdateSwimming holds a swimming creature at (water line - m_swimDepth),
                // so starting it there means it is already buoyant rather than dropping in from
                // above the surface.
                if (swimmingTargets && prefab.TryGetComponent(out Character prefabCharacter))
                {
                    spawnAt.y = ZoneSystem.instance.m_waterLevel - prefabCharacter.m_swimDepth;
                }

                var creature = UnityEngine.Object.Instantiate(prefab, spawnAt, Quaternion.identity);
                var bountyTarget = creature.AddComponent<BountyTarget>();
                bountyTarget.Initialize(bounty, prefab.name, isAdd);
                AddPlacedZdo(creature, placedZdos);
            }

            AdventureSpawnSaveMarker.MarkPlaced(placedZdos);
            LogPlacement(what, bounty.Biome);
            placed.ForceSet(true);
        }

        private void SpawnChest(TreasureMapChestInfo treasure)
        {
            Vector3 point = spawnPoint.Get();

            const string treasureChestPrefabName = "loot_chest_stone";
            var treasureChestPrefab = ZNetScene.instance.GetPrefab(treasureChestPrefabName);
            ZoneSystem.instance.GetGroundData(
                ref point, out var normal, out var foundBiome, out var biomeArea, out var hmap);
            var treasureChestObject = UnityEngine.Object.Instantiate(
                treasureChestPrefab, point, Quaternion.FromToRotation(Vector3.up, normal));
            var treasureChest = treasureChestObject.AddComponent<TreasureMapChest>();

            // Dungeon loot chests are not player-built pieces, so Piece may legitimately be absent -
            // TreasureMapChest.Reinitialize guards the same way.
            Piece tpiece = treasureChestObject.GetComponent<Piece>();
            if (tpiece != null)
            {
                // Prevent the wildlife from attacking the chest and giving away its location
                tpiece.m_primaryTarget = false;
                tpiece.m_randomTarget = false;
                tpiece.m_targetNonPlayerBuilt = false;
            }

            treasureChest.Setup(treasure.PlayerID, treasure.Biome, treasure.Interval);

            var placedZdos = new List<ZDO>(1);
            AddPlacedZdo(treasureChestObject, placedZdos);
            AdventureSpawnSaveMarker.MarkPlaced(placedZdos);

            LogPlacement("treasure chest", treasure.Biome);
            placed.ForceSet(true);
        }

        /// <summary>
        /// Collects the ZDO of an object this spawner just placed, for <see cref="AdventureSpawnSaveMarker"/>.
        /// </summary>
        private static void AddPlacedZdo(GameObject placedObject, List<ZDO> placedZdos)
        {
            if (placedObject.TryGetComponent(out ZNetView view) && view.GetZDO() != null)
            {
                placedZdos.Add(view.GetZDO());
            }
        }

        /// <summary>
        /// One always-visible line per placement. Every other line on this path is gated, which is why
        /// "my bounty never spawned" reports used to arrive with logs that said nothing at all. When
        /// the spawn point was chosen by another machine (or before this instance was loaded), there is
        /// no local search to time and only the fact of placement is reported.
        /// </summary>
        private void LogPlacement(string what, Heightmap.Biome biome)
        {
            string biomeName = BiomeDataManager.GetName(biome);
            if (searchStartTime <= 0f)
            {
                EpicLoot.LogForce($"Adventure {what} ({biomeName}) placed at a spawn point chosen earlier.");
                return;
            }

            EpicLoot.LogForce($"Adventure {what} ({biomeName}) placed after {Time.time - searchStartTime:0.0}s: " +
                $"area wait {areaReadyTime - searchStartTime:0.0}s, {candidatesTried} spots tried " +
                $"({DescribeRejections()}), {placedDistance:0}m from the circle's centre, {DescribeTier()}.");
        }

        private string DescribeTier()
        {
            string ward = AdventureWardCheck.DescribeWard(placedWard);
            switch (placedTier)
            {
                case PlacementTier.Clear:
                    return "clear of wards";
                case PlacementTier.NearWard:
                    return $"near a ward but outside its protected area, as nothing clear of wards was available (ward {ward})";
                case PlacementTier.InsideWard:
                    return $"inside a ward's protected area, as nothing outside one was available (ward {ward})";
                default:
                    return "on the least-bad spot in the circle, as none met the placement rules";
            }
        }

        private string DescribeRejections()
        {
            var parts = new List<string>();
            void Add(int count, string label)
            {
                if (count > 0)
                {
                    parts.Add($"{count} {label}");
                }
            }

            Add(rejections[(int)Rejection.ZoneNotLoaded], "in unloaded zones");
            Add(rejections[(int)Rejection.WrongBiome], "outside the biome");
            Add(rejections[(int)Rejection.NoFloor], "with no floor");
            Add(rejections[(int)Rejection.Obstructed], "obstructed");
            Add(rejections[(int)Rejection.Underwater], "underwater");
            Add(rejections[(int)Rejection.Lava], "on lava");
            Add(nearWardCandidates, "near a ward");
            Add(insideWardCandidates, "inside a ward");
            return parts.Count == 0 ? "none rejected" : string.Join(", ", parts);
        }

        /// <summary>
        /// Logs, once, why a spawner this machine has owned for <see cref="OverdueSeconds"/> has still
        /// not placed. Always visible: this is the line that says which step a stuck bounty is on, and
        /// the machine running it is often not the buyer's.
        /// </summary>
        private void CheckOverdue()
        {
            ownedSeconds += Time.deltaTime;
            if (reportedOverdue || ownedSeconds < OverdueSeconds)
            {
                return;
            }

            reportedOverdue = true;
            string machine = ZNet.instance.IsDedicated() ? "dedicated server" : ZNet.instance.IsServer() ? "host" : "client";
            EpicLoot.LogWarningForce($"{DescribeSpawn()} has not been placed after this {machine} owned its spawner " +
                $"for {ownedSeconds:0}s: {DescribeStage()}.");
        }

        private string DescribeSpawn()
        {
            if (isBounty.Get())
            {
                BountyInfo bountyInfo = bounty.Get();
                return $"Adventure bounty target '{bountyInfo.Target.MonsterID}' " +
                    $"({BiomeDataManager.GetName(bountyInfo.Biome)}, {bountyInfo.ID})";
            }

            TreasureMapChestInfo treasureInfo = treasure.Get();
            return $"Adventure treasure chest ({BiomeDataManager.GetName(treasureInfo.Biome)}, interval {treasureInfo.Interval})";
        }

        private string DescribeStage()
        {
            switch (stage)
            {
                case PlacementStage.WaitingForIndex:
                    return $"waiting for the world biome index (state {WorldBiomeIndex.State})";
                case PlacementStage.WaitingForArea:
                    return "waiting for the area around the map circle to load - " + DescribeUnsettledArea(searchCentre);
                case PlacementStage.Searching:
                    return $"still searching the circle, {candidatesTried} spots tried ({DescribeRejections()})";
                case PlacementStage.Spawning:
                    return missingPrefab != null
                        ? $"a spot was chosen, but the creature prefab '{missingPrefab}' does not exist"
                        : "a spot was chosen, but the spawn has not completed";
                default:
                    return "the search has not started";
            }
        }

        internal IEnumerator DeterminespawnPoint(Vector3 circleCentre,
            Heightmap.Biome biome, WaterPlacement waterPlacement = WaterPlacement.Reject)
        {
            searchStartTime = Time.time;
            searchCentre = circleCentre;
            candidatesTried = 0;
            Array.Clear(rejections, 0, rejections.Length);
            nearWardCandidates = 0;
            insideWardCandidates = 0;
            zoneReadyCache.Clear();

            // The owner of this spawner is not necessarily the player who bought it, so the biome
            // index may never have been built on this client. A build takes a fraction of a second;
            // IsOpenWater falls back safely if it is somehow still not ready when the wait gives up.
            stage = PlacementStage.WaitingForIndex;
            WorldBiomeIndex.EnsureBuilt();

            float indexDeadline = Time.unscaledTime + IndexWaitTimeoutSeconds;
            while (WorldBiomeIndex.State == BiomeIndexState.Building && Time.unscaledTime < indexDeadline)
            {
                yield return null;
                if (!StillOwner())
                {
                    AbandonSearch();
                    yield break;
                }
            }

            // This used to be a fixed 300 frames plus five seconds, then vanilla's IsAreaReady polled
            // once a second -- 10-15s of nothing after arriving, before the search had even begun.
            // IsAreaSettled waits exactly as long as loading actually takes.
            stage = PlacementStage.WaitingForArea;
            while (!IsAreaSettled(circleCentre))
            {
                yield return new WaitForSeconds(AreaPollSeconds);
                if (!StillOwner())
                {
                    AbandonSearch();
                    yield break;
                }
            }

            // One frame so colliders created this frame are in the physics scene before the raycasts.
            yield return null;
            if (!StillOwner())
            {
                AbandonSearch();
                yield break;
            }

            areaReadyTime = Time.time;
            zoneReadyCache.Clear();
            stage = PlacementStage.Searching;

            float searchRadius = MinimapController.AreaRadius * SearchRadiusFraction;
            // Tier 1's buffer. A ward vetoes every spot within its own radius plus this much.
            float wardBuffer = Mathf.Max(0f, AdventureDataManager.Config.TreasureMap.MinimapAreaRadius);
            float waterSurface = ZoneSystem.instance.m_waterLevel;

            // An open-water biome sits below the water line everywhere -- Ocean's biome cutoff is
            // roughly 25m under it. Rejecting submerged points there rejected every candidate, which is
            // why no ocean bounty ever placed. Asking the biome index rather than testing for Ocean by
            // name extends that fix to any ocean-like biome another mod adds.
            bool spawnInOpenWater = WorldBiomeIndex.IsOpenWater(biome) &&
                waterPlacement != WaterPlacement.Reject;

            bool haveUsable = false;
            Candidate best = default;
            bool haveFallback = false;
            Candidate fallback = default;
            int candidatesThisFrame = 0;

            // Sample the circle, keeping the best spot by tier and stopping at the first that meets every
            // rule. Nothing here ever looks outside the circle: see the class summary for why.
            for (int attempt = 0; attempt < MaxCandidates; attempt++)
            {
                // Spread the work over frames. The search used to sleep a whole second after every ten
                // rejected candidates, so a cluttered or partly flooded area cost many seconds.
                if (++candidatesThisFrame >= CandidatesPerFrame)
                {
                    candidatesThisFrame = 0;
                    yield return null;
                    if (!StillOwner())
                    {
                        AbandonSearch();
                        yield break;
                    }
                }

                // Area-uniform sample of the disc, so points do not bunch up at its centre.
                float sampleRadius = Mathf.Sqrt(UnityEngine.Random.value) * searchRadius;
                float sampleAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector3 sample = circleCentre + new Vector3(
                    Mathf.Cos(sampleAngle) * sampleRadius, 0, Mathf.Sin(sampleAngle) * sampleRadius);

                Candidate candidate = EvaluateCandidate(sample, biome, spawnInOpenWater, waterSurface, wardBuffer);
                if (candidate.Usable)
                {
                    if (!haveUsable || candidate.Tier < best.Tier)
                    {
                        best = candidate;
                        haveUsable = true;
                    }

                    if (candidate.Tier == PlacementTier.Clear)
                    {
                        break;
                    }
                }
                else if (!haveUsable && (!haveFallback || candidate.Penalty < fallback.Penalty))
                {
                    fallback = candidate;
                    haveFallback = true;
                }
            }

            Candidate chosen;
            if (haveUsable)
            {
                chosen = best;
            }
            else
            {
                // Nothing in the circle met the terrain rules. The centre is always a candidate here:
                // its zone is the one IsAreaSettled waited on, so this cannot come up empty, and a
                // centre that does meet the rules beats a sampled spot that does not.
                Candidate centre = EvaluateCandidate(circleCentre, biome, spawnInOpenWater, waterSurface, wardBuffer);
                if (centre.Usable || !haveFallback || centre.Penalty <= fallback.Penalty)
                {
                    chosen = centre;
                }
                else
                {
                    chosen = fallback;
                }

                if (!chosen.Usable)
                {
                    chosen.Tier = PlacementTier.Fallback;
                    chosen.Ward = null;
                }
            }

            Vector3 determinedSpawn = chosen.Position;

            // Bounty targets that belong in the Ocean are swimming creatures, so put them at the
            // surface rather than on the seabed tens of metres below it.
            if (spawnInOpenWater && waterPlacement == WaterPlacement.Surface)
            {
                determinedSpawn.y = waterSurface;
            }

            if (determinedSpawn.y >= StartingHeight - 1f)
            {
                determinedSpawn.y = 400f;
            }

            placedTier = chosen.Tier;
            placedWard = chosen.Ward;
            placedDistance = Utils.DistanceXZ(circleCentre, determinedSpawn);

            EpicLoot.Log($"Selected Spawn point X {determinedSpawn.x}, Y {determinedSpawn.y}, Z {determinedSpawn.z}");
            stage = PlacementStage.Spawning;
            spawnPoint.ForceSet(determinedSpawn);
        }

        /// <summary>
        /// Grounds <paramref name="sample"/> and grades it: <see cref="Candidate.Usable"/> with a ward tier
        /// when it meets every terrain rule, otherwise a penalty for choosing a fallback. A spot in a zone
        /// that has not loaded cannot be used at all - it would pass the floor test straight through a
        /// rock that does not exist yet.
        /// </summary>
        private Candidate EvaluateCandidate(Vector3 sample, Heightmap.Biome biome, bool spawnInOpenWater,
            float waterSurface, float wardBuffer)
        {
            candidatesTried += 1;
            var candidate = new Candidate { Position = sample, Penalty = int.MaxValue };

            if (!IsCandidateZoneReady(ZoneSystem.GetZone(sample)))
            {
                rejections[(int)Rejection.ZoneNotLoaded]++;
                return candidate;
            }

            ZoneSystem.instance.GetGroundData(ref sample, out _, out Heightmap.Biome foundBiome, out _, out Heightmap hmap);
            if (hmap == null)
            {
                rejections[(int)Rejection.ZoneNotLoaded]++;
                return candidate;
            }

            candidate.Position = sample;
            float terrainHeight = sample.y;
            Rejection? firstFailure = null;
            int penalty = 0;

            void Fail(Rejection reason, int cost)
            {
                firstFailure ??= reason;
                penalty += cost;
            }

            if (foundBiome != biome)
            {
                Fail(Rejection.WrongBiome, 2);
            }

            if (ZoneSystem.instance.FindFloor(new Vector3(sample.x, terrainHeight + 100f, sample.z), out float solidHeight))
            {
                float terrainDiff = solidHeight - terrainHeight;

                // Something solid more than half a metre above the ground: a rock, a tree, a building.
                if (terrainDiff > 0.5f)
                {
                    Fail(Rejection.Obstructed, 4);
                }
                else if (terrainDiff > 0f)
                {
                    candidate.Position.y = solidHeight;
                }
            }
            else
            {
                Fail(Rejection.NoFloor, 4);
            }

            // Prevents spawning in a body of water. Open-water spawns are exempt: the seabed is the
            // ground there, and a surface spawn is lifted to the water line once a point is chosen.
            if (!spawnInOpenWater && candidate.Position.y < waterSurface - 1f)
            {
                Fail(Rejection.Underwater, 3);
            }

            // The AshLands gate stays: the vegetation mask is a shared channel with a different meaning
            // per biome, and vanilla's own Heightmap.IsLava checks for AshLands before reading it, so lava
            // is an AshLands-only concept to the engine rather than a trait a custom biome could carry.
            if (biome == Heightmap.Biome.AshLands && hmap.GetVegetationMask(candidate.Position) > 0.45f)
            {
                Fail(Rejection.Lava, 8);
            }

            if (firstFailure.HasValue)
            {
                rejections[(int)firstFailure.Value]++;
                candidate.Penalty = penalty;
                return candidate;
            }

            // Keep the spawn out of player bases where the circle allows it. The wards around here are
            // actually loaded by now, unlike when the world point was first picked.
            candidate.Usable = true;
            candidate.Penalty = 0;
            switch (AdventureWardCheck.GetWardProximity(candidate.Position, wardBuffer, out candidate.Ward))
            {
                case WardProximity.InsideWard:
                    candidate.Tier = PlacementTier.InsideWard;
                    insideWardCandidates++;
                    break;
                case WardProximity.NearWard:
                    candidate.Tier = PlacementTier.NearWard;
                    nearWardCandidates++;
                    break;
                default:
                    candidate.Tier = PlacementTier.Clear;
                    break;
            }

            return candidate;
        }

        /// <summary>
        /// Whether this client still owns the spawner. Checked after every yield in the search, since
        /// ownership moves to another player when the current owner leaves the area.
        /// </summary>
        private bool StillOwner()
        {
            return zNetView != null && zNetView.IsValid() && zNetView.IsOwner();
        }

        /// <summary>
        /// Drops a search this client no longer owns. Without this the coroutine ran to the end
        /// regardless, and its <c>spawnPoint.ForceSet</c> claimed ownership back from whoever had taken
        /// over -- two clients searching and both reaching the spawn is how a bounty spawns twice.
        /// No retry delay: if ownership comes back, searching again right away is correct.
        /// </summary>
        private void AbandonSearch()
        {
            EpicLoot.Log("Adventure spawner changed owner mid-search; leaving the search to the new owner.");
            startedPlacement = false;
            stage = PlacementStage.NotStarted;
        }

        /// <summary>
        /// Whether the area around <paramref name="point"/> has finished loading, as far as this client
        /// can load it. Vanilla <see cref="ZNetScene.IsAreaReady"/> requires the point's zone and all
        /// eight around it, but a client only creates objects inside its own near simulation area. On a
        /// low Simulation Distance setting that area is too small to contain all nine zones unless the
        /// player stands in the point's own zone, and on the non-classic setting it never covers them
        /// from a diagonal zone, so a player waiting inside the map circle could wait forever. Zones this
        /// client cannot load are skipped rather than waited on.
        /// </summary>
        private bool IsAreaSettled(Vector3 point)
        {
            // Nothing is "near" a dedicated server's reference position; keep vanilla's rule there. A
            // dedicated server owns a spawner near the world centre, where its own reference position is,
            // or under a serverside simulation mod.
            if (ZNet.instance.IsDedicated())
            {
                return ZNetScene.instance.IsAreaReady(point);
            }

            Vector2s centre = ZoneSystem.GetZone(point);
            if (!IsZoneInLocalNearArea(centre))
            {
                // The owner has to come closer before the point itself can load.
                return false;
            }

            for (int y = centre.y - 1; y <= centre.y + 1; y++)
            {
                for (int x = centre.x - 1; x <= centre.x + 1; x++)
                {
                    var zone = new Vector2s(x, y);
                    if (IsZoneInLocalNearArea(zone) && !IsZoneInstantiated(zone))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Names what <see cref="IsAreaSettled"/> is still waiting on, for the overdue log: the zones
        /// not yet loaded, and the first object in each loaded zone that has not been created.
        /// </summary>
        private static string DescribeUnsettledArea(Vector3 point)
        {
            Vector2s centre = ZoneSystem.GetZone(point);
            bool dedicated = ZNet.instance.IsDedicated();

            if (!dedicated && !IsZoneInLocalNearArea(centre))
            {
                float distance = Utils.DistanceXZ(ZNet.instance.GetReferencePosition(), point);
                return $"the circle's zone {DescribeZone(centre)} is outside this client's simulation area, {distance:0}m away";
            }

            var waiting = new List<string>();
            for (int y = centre.y - 1; y <= centre.y + 1; y++)
            {
                for (int x = centre.x - 1; x <= centre.x + 1; x++)
                {
                    var zone = new Vector2s(x, y);
                    if (!dedicated && !IsZoneInLocalNearArea(zone))
                    {
                        continue;
                    }

                    if (!ZoneSystem.instance.IsZoneLoaded(zone))
                    {
                        waiting.Add($"zone {DescribeZone(zone)} not loaded");
                    }
                    else if (TryFindUninstantiatedObject(zone, out ZDO waitingOn))
                    {
                        GameObject prefab = ZNetScene.instance.GetPrefab(waitingOn.GetPrefab());
                        string name = prefab != null ? prefab.name : waitingOn.GetPrefab().ToString();
                        waiting.Add($"zone {DescribeZone(zone)} still creating '{name}' ({waitingOn.m_uid})");
                    }
                }
            }

            return waiting.Count == 0 ? "it has just finished loading" : string.Join(", ", waiting);
        }

        private static string DescribeZone(Vector2s zone)
        {
            return $"({zone.x}, {zone.y})";
        }

        /// <summary>
        /// Whether a search candidate's zone is loaded and populated, cached for the rest of the search.
        /// A zone outside the near area is rejected outright, as GetGroundData would do for it anyway.
        /// </summary>
        private bool IsCandidateZoneReady(Vector2s zone)
        {
            // A dedicated server has no near area of its own (see IsAreaSettled); leave candidates to
            // the GetGroundData check, as before.
            if (ZNet.instance.IsDedicated())
            {
                return true;
            }

            if (!zoneReadyCache.TryGetValue(zone, out bool ready))
            {
                ready = IsZoneInLocalNearArea(zone) && IsZoneInstantiated(zone);
                zoneReadyCache[zone] = ready;
            }

            return ready;
        }

        /// <summary>
        /// Whether ZNetScene creates the objects of <paramref name="zone"/> on this client. The same
        /// shape ZDOMan.FindSectorObjects uses for the near area: a square of NearSimulationDistance
        /// zones, trimmed to a circle unless the setting is classic.
        /// </summary>
        internal static bool IsZoneInLocalNearArea(Vector2s zone)
        {
            Vector2s centre = ZoneSystem.GetZone(ZNet.instance.GetReferencePosition());
            SimulationDistance distance = ZNet.instance.GetSyncedSimulationDistance();
            int near = distance.NearSimulationDistance;
            int ring = Mathf.Max(Mathf.Abs(zone.x - centre.x), Mathf.Abs(zone.y - centre.y));

            if (ring == 0)
            {
                return true;
            }

            return ring <= near &&
                (distance.IsClassic || ZoneSystem.instance.ZonesWithinRadius(centre, zone, near));
        }

        /// <summary>
        /// Vanilla <see cref="ZNetScene.IsAreaReady"/> narrowed to a single zone: the zone is loaded and
        /// every object in it with a known prefab has been created.
        /// </summary>
        internal static bool IsZoneInstantiated(Vector2s zone)
        {
            return ZoneSystem.instance.IsZoneLoaded(zone) && !TryFindUninstantiatedObject(zone, out _);
        }

        /// <summary>
        /// Finds the first object in <paramref name="zone"/> with a known prefab that ZNetScene has not
        /// created yet.
        /// </summary>
        private static bool TryFindUninstantiatedObject(Vector2s zone, out ZDO waitingOn)
        {
            waitingOn = null;
            ZoneObjectsScratch.Clear();
            ZDOMan.instance.FindSectorObjects(zone, new SimulationDistance(0, 0), ZoneObjectsScratch);

            foreach (ZDO zdo in ZoneObjectsScratch)
            {
                if (ZNetScene.instance.IsPrefabZDOValid(zdo) && !ZNetScene.instance.HaveInstance(zdo))
                {
                    waitingOn = zdo;
                    break;
                }
            }

            ZoneObjectsScratch.Clear();
            return waitingOn != null;
        }

        /// <summary>
        /// Handles a bounty creature prefab that ZNetScene does not have -- typically a creature from a
        /// mod that has since been removed.
        ///
        /// Spawning bails without setting <c>placed</c>, and Update re-enters it on the very next frame,
        /// so before this the spawner retried a lookup that cannot succeed **every frame for as long as
        /// the player stayed near the bounty**. Back off for <see cref="MissingPrefabRetrySeconds"/>,
        /// and report it once at Error rather than per-frame at Warning -- Warning is invisible at the
        /// default Log Level, which is why this failed silently.
        ///
        /// The spawner is deliberately not marked placed: the player has already paid for this bounty,
        /// and re-adding the missing mod should let it spawn normally.
        /// </summary>
        private void ReportMissingPrefab(string role, string bountyId, string monsterId)
        {
            missingPrefab = monsterId;
            if (!reportedMissingPrefab)
            {
                reportedMissingPrefab = true;
                EpicLoot.LogError($"Could not find prefab for bounty {role}! BountyID: {bountyId}, " +
                    $"MonsterID: {monsterId}. This bounty cannot spawn until that prefab exists again " +
                    "(is the mod that adds it still installed?). Retrying occasionally.");
            }

            // Delay the next attempt without clearing startedPlacement -- the spawn point is fine, it is
            // only the prefab that is missing, so there is nothing to re-search for.
            nextSearchTime = Time.time + MissingPrefabRetrySeconds;
        }

        /// <summary>
        /// Reads the bounty a spawner carries straight off its ZDO, for code that holds the ZDO but not an
        /// instance (the buyer-side watchdog). False when the spawner carries no bounty.
        /// </summary>
        internal static bool TryReadBounty(ZDO zdo, out BountyInfo bountyInfo)
        {
            return TryReadBinary(zdo, BountyKey, out bountyInfo);
        }

        /// <summary>Treasure-map counterpart to <see cref="TryReadBounty"/>.</summary>
        internal static bool TryReadTreasure(ZDO zdo, out TreasureMapChestInfo treasureInfo)
        {
            return TryReadBinary(zdo, TreasureKey, out treasureInfo);
        }

        /// <summary>The same BinaryFormatter decode the ZNetProperty wrappers in CustomZNet use.</summary>
        private static bool TryReadBinary<T>(ZDO zdo, string key, out T value) where T : class
        {
            value = null;
            byte[] stored = zdo.GetByteArray(key);
            if (stored == null)
            {
                return false;
            }

            try
            {
                using var stream = new MemoryStream(stored);
                value = new BinaryFormatter().Deserialize(stream) as T;
            }
            catch (Exception)
            {
                value = null;
            }

            return value != null;
        }
    }
}
