using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace EpicLoot.Adventure
{
    internal enum BiomeIndexState
    {
        NotBuilt,
        Building,
        Ready,
        Unavailable
    }

    /// <summary>
    /// A coarse map of where each biome is, sampled straight from the world seed.
    ///
    /// This exists because picking a bounty or treasure location used to be rejection sampling: throw
    /// a random dart at the world, force-load the zone it landed in to see what was there, throw it
    /// away, repeat. That cost a zone instantiate and a terrain build per dart, and for a biome the
    /// darts could not reach (AshLands sits in a polar cap, not scattered across the disc) it never
    /// terminated usefully at all.
    ///
    /// WorldGenerator.GetBiome is a pure function of the world seed and an x/z pair -- no zone, no
    /// Heightmap, no Random, and vanilla already calls it off the main thread from
    /// HeightmapBuilder.BuildThread. So the whole world can be sampled once, up front, for less than
    /// the cost of a handful of the old darts, and every later request becomes a lookup.
    ///
    /// Buckets are keyed by whatever Heightmap.Biome value GetBiome returned, never by a name or a
    /// hardcoded list. DeepNorth (which has no adventure config entry) and biomes another mod adds
    /// therefore work with no code change here.
    /// </summary>
    internal static class WorldBiomeIndex
    {
        /// <summary>
        /// Cells of one biome, sorted by distance from the world centre. The sort is what turns a
        /// "cells within this radius band" query into a binary-search slice instead of a full scan.
        /// </summary>
        private sealed class BiomeBucket
        {
            public float[] Radius;
            public float[] X;
            public float[] Z;
            public int Count => Radius.Length;
        }

        private static readonly Dictionary<Heightmap.Biome, BiomeBucket> Buckets = new();

        private static BiomeIndexState _state = BiomeIndexState.NotBuilt;

        // Validity stamp. The seed alone is not enough: Expand World Size can resize the world
        // mid-session (a server sync or a cfg hot reload, both of which regenerate the world) without
        // the seed ever changing, and every cached point would then be measured against the old map.
        private static int _seed;
        private static float _stampPlayable;
        private static float _stampTotal;
        private static float _stampStretch;

        private static float _cellSize = DefaultCellSize;
        private static int _cellCount;
        private static double _buildMs;
        private static int _buildFrames;

        /// <summary>
        /// Sample spacing on a vanilla world: two zones wide, and biome selection noise runs at a
        /// 1/1000 scale so real regions are hundreds of metres across.
        /// </summary>
        private const float DefaultCellSize = 128f;

        /// <summary>Ceiling on total samples, so an enormous world cannot make the build unbounded.</summary>
        private const int MaxCells = 400000;

        /// <summary>Main-thread time the build may take per frame.</summary>
        private const float FrameBudgetMs = 2f;

        private const int SamplesPerBudgetCheck = 128;

        /// <summary>Cells examined before a search gives up on one rung of the fallback ladder.</summary>
        private const int MaxCandidatesPerAttempt = 256;

        private const int MaxCellsPerAttempt = 4;
        private const int NearestCellCount = 8;

        /// <summary>
        /// Minimum spacing between consecutive points handed out for the same biome, when the K
        /// nearest cells offer a choice. Stops two bounties in a row landing in the same clearing.
        /// </summary>
        private const float PreferredSpacing = 200f;

        private static readonly Dictionary<Heightmap.Biome, Vector3> LastIssued = new();

        // Scratch for the nearest-cell search. Reused rather than allocated per query: searching is
        // main-thread only and never re-enters itself, so there is exactly one live search at a time.
        private static readonly int[] NearestScratch = new int[NearestCellCount];
        private static readonly float[] NearestDistanceScratch = new float[NearestCellCount];

        internal static BiomeIndexState State => _state;
        internal static float CellSize => _cellSize;
        internal static int CellCount => _cellCount;

        /// <summary>
        /// Builds the index if it is missing or stale. Safe and cheap to call repeatedly -- the
        /// merchant panel calls it on open and every spawn point request calls it again, which is
        /// what makes a mid-session world resize self-correcting without subscribing to anything.
        /// </summary>
        internal static void EnsureBuilt()
        {
            if (_state == BiomeIndexState.Building)
            {
                return;
            }

            WorldExtent.Refresh();

            if (!PreconditionsMet())
            {
                // Not in a world yet. Stay retryable rather than latching Unavailable.
                _state = BiomeIndexState.NotBuilt;
                return;
            }

            if (_state == BiomeIndexState.Ready && StampMatches())
            {
                return;
            }

            _state = BiomeIndexState.Building;
            AdventureCacheDriver.Run(BuildRoutine());
        }

        /// <summary>
        /// Drops the index. Called on world change -- every cell is a world position, and carrying
        /// them into another world would hand out locations from the wrong map.
        /// </summary>
        internal static void Reset()
        {
            WorldExtent.InvalidateProbe();
            Buckets.Clear();
            LastIssued.Clear();
            _state = BiomeIndexState.NotBuilt;
            _seed = 0;
            _stampPlayable = 0f;
            _stampTotal = 0f;
            _stampStretch = 0f;
            _cellCount = 0;
            _buildMs = 0;
            _buildFrames = 0;
        }

        private static bool PreconditionsMet()
        {
            // ZNet is the menu-world guard: FejdStartup initializes a WorldGenerator for the menu
            // world before ZNet exists, and that generator only ever reports Mountain or BlackForest.
            // Player gates out a dedicated server, which has no reason to build this.
            return WorldGenerator.instance != null &&
                ZNet.instance != null &&
                ZoneSystem.instance != null &&
                Player.m_localPlayer != null;
        }

        private static bool StampMatches()
        {
            return _seed == WorldGenerator.instance.GetSeed() &&
                Mathf.Approximately(_stampPlayable, WorldExtent.PlayableRadius) &&
                Mathf.Approximately(_stampTotal, WorldExtent.TotalRadius) &&
                Mathf.Approximately(_stampStretch, WorldExtent.Stretch);
        }

        /// <summary>
        /// Sample spacing for the current world. Feature size tracks stretch, not radius: doubling
        /// the world radius at stretch 1 produces twice as many patches of the same size, not bigger
        /// ones, so the grid must stay fine. The budget term is what stops that from turning a very
        /// large world into an unbounded build.
        /// </summary>
        private static float ResolveCellSize()
        {
            float byFeature = DefaultCellSize * Mathf.Clamp(WorldExtent.Stretch, 0.5f, 8f);
            float byBudget = Mathf.Sqrt(Mathf.PI * WorldExtent.TotalRadius * WorldExtent.TotalRadius / MaxCells);
            return Mathf.Max(byFeature, byBudget);
        }

        private static IEnumerator BuildRoutine()
        {
            var scratch = new Dictionary<Heightmap.Biome, List<Vector2>>();
            var stopwatch = Stopwatch.StartNew();
            double elapsedMs = 0;
            int frames = 0;
            int sampled = 0;
            bool failed = false;

            float radius = WorldExtent.TotalRadius;
            float cellSize = ResolveCellSize();
            var worldGenerator = WorldGenerator.instance;
            int steps = Mathf.CeilToInt(radius * 2f / cellSize);
            float radiusSq = radius * radius;

            for (int iz = 0; iz < steps; iz++)
            {
                float z = -radius + (iz + 0.5f) * cellSize;

                for (int ix = 0; ix < steps; ix++)
                {
                    float x = -radius + (ix + 0.5f) * cellSize;
                    if (x * x + z * z > radiusSq)
                    {
                        continue;
                    }

                    Heightmap.Biome biome;
                    try
                    {
                        biome = worldGenerator.GetBiome(x, z);
                    }
                    catch (Exception e)
                    {
                        EpicLoot.LogErrorForce("Failed while sampling the world biome index at " +
                            $"({x:0}, {z:0}): {e}");
                        failed = true;
                        break;
                    }

                    sampled++;

                    // Checked before the Biome.None skip below, so a long run of skipped samples
                    // cannot starve the yield and freeze the frame.
                    if (sampled % SamplesPerBudgetCheck == 0 && stopwatch.Elapsed.TotalMilliseconds >= FrameBudgetMs)
                    {
                        elapsedMs += stopwatch.Elapsed.TotalMilliseconds;
                        frames++;
                        // Deliberately not WaitForEndOfFrame: a minimised or occluded window can stop
                        // producing end-of-frame, which is how the old picker could hang forever.
                        yield return null;
                        stopwatch.Reset();
                        stopwatch.Start();
                    }

                    if (biome == Heightmap.Biome.None)
                    {
                        continue;
                    }

                    if (!scratch.TryGetValue(biome, out var cells))
                    {
                        cells = new List<Vector2>();
                        scratch.Add(biome, cells);
                    }

                    cells.Add(new Vector2(x, z));
                }

                if (failed)
                {
                    break;
                }
            }

            elapsedMs += stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Stop();

            if (failed)
            {
                Buckets.Clear();
                _state = BiomeIndexState.Unavailable;
                yield break;
            }

            yield return null;
            frames++;

            Buckets.Clear();
            foreach (var pair in scratch)
            {
                Buckets.Add(pair.Key, BuildBucket(pair.Value));
            }

            _cellSize = cellSize;
            _cellCount = sampled;
            _buildMs = elapsedMs;
            _buildFrames = frames;
            _seed = WorldGenerator.instance.GetSeed();
            _stampPlayable = WorldExtent.PlayableRadius;
            _stampTotal = WorldExtent.TotalRadius;
            _stampStretch = WorldExtent.Stretch;
            _state = BiomeIndexState.Ready;

            EpicLoot.LogForce($"World biome index built: {_cellCount} cells, {_cellSize:0.#}m grid, " +
                $"{_buildMs:0}ms over {_buildFrames} frames, {WorldExtent.Describe()}. {DescribeBuckets()}");
        }

        private static BiomeBucket BuildBucket(List<Vector2> cells)
        {
            int count = cells.Count;
            var radii = new float[count];
            for (int i = 0; i < count; i++)
            {
                radii[i] = cells[i].magnitude;
            }

            var order = new int[count];
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
            }

            var sortKeys = (float[])radii.Clone();
            Array.Sort(sortKeys, order);

            var bucket = new BiomeBucket
            {
                Radius = sortKeys,
                X = new float[count],
                Z = new float[count]
            };

            for (int i = 0; i < count; i++)
            {
                var cell = cells[order[i]];
                bucket.X[i] = cell.x;
                bucket.Z[i] = cell.y;
            }

            return bucket;
        }

        internal static int CountCells(Heightmap.Biome biome)
        {
            return Buckets.TryGetValue(biome, out var bucket) ? bucket.Count : 0;
        }

        internal static int CountCellsInBand(Heightmap.Biome biome, float minRadius, float maxRadius)
        {
            if (!Buckets.TryGetValue(biome, out var bucket))
            {
                return 0;
            }

            int lo = LowerBound(bucket.Radius, minRadius);
            int hi = UpperBound(bucket.Radius, maxRadius);
            return Mathf.Max(0, hi - lo);
        }

        internal static IEnumerable<Heightmap.Biome> IndexedBiomes => Buckets.Keys;

        private static string DescribeBuckets()
        {
            var sb = new StringBuilder();
            foreach (var pair in Buckets)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(pair.Key).Append('=').Append(pair.Value.Count);
            }

            return sb.Length == 0 ? "(no biomes indexed)" : sb.ToString();
        }

        internal static string Describe()
        {
            return $"state={_state} seed={_seed} cells={_cellCount} grid={_cellSize:0.#}m " +
                $"build={_buildMs:0}ms/{_buildFrames}frames";
        }

        /// <summary>
        /// Picks a world position in <paramref name="biome"/>. Chooses a random anchor inside the
        /// requested radius band, snaps to one of the nearest indexed cells of that biome, then
        /// refines within that cell to an exact point.
        ///
        /// Validation is seed-only on purpose: nothing here loads a zone, so nothing here can see
        /// terrain colliders, wards or player bases. AdventureSpawnController.DeterminespawnPoint
        /// does that check with an expanding band search once the player is actually near the point,
        /// and remains the authority on where a bounty or chest finally lands.
        /// </summary>
        internal static bool TryFindPoint(Heightmap.Biome biome, float minRadius, float maxRadius,
            bool requireBand, out Vector3 point, out int candidatesTried)
        {
            point = Vector3.zero;
            candidatesTried = 0;

            if (!Buckets.TryGetValue(biome, out var bucket) || bucket.Count == 0)
            {
                return false;
            }

            int lo;
            int hi;
            if (requireBand)
            {
                lo = LowerBound(bucket.Radius, minRadius);
                hi = UpperBound(bucket.Radius, maxRadius);
                if (hi - lo <= 0)
                {
                    return false;
                }
            }
            else
            {
                lo = 0;
                hi = bucket.Count;
            }

            Vector2 anchor = SampleAnchor(minRadius, maxRadius);
            int[] nearest = NearestScratch;
            int nearestCount = FindNearestCells(bucket, lo, hi, anchor, nearest);
            if (nearestCount == 0)
            {
                return false;
            }

            OrderByPreference(bucket, biome, nearest, nearestCount);

            int cellsToTry = Mathf.Min(nearestCount, MaxCellsPerAttempt);

            // Two passes. The first insists the point's whole zone reads as this biome, which is what
            // makes the placement search downstream agree with us; the second drops that so a biome
            // too fragmented to offer a clean zone still yields something.
            for (int pass = 0; pass < 2; pass++)
            {
                bool requireZoneAgreement = pass == 0;

                // Each pass gets its own budget, but the caller is told the running total -- a log
                // line reporting only the last pass would understate what the search really cost.
                int passCandidates = 0;
                int alreadyTried = candidatesTried;

                for (int i = 0; i < cellsToTry; i++)
                {
                    int index = nearest[i];
                    var cell = new Vector2(bucket.X[index], bucket.Z[index]);

                    bool found = TryRefineInCell(biome, cell, minRadius, maxRadius, requireBand,
                        requireZoneAgreement, ref passCandidates, out point);
                    candidatesTried = alreadyTried + passCandidates;

                    if (found)
                    {
                        LastIssued[biome] = point;
                        return true;
                    }

                    if (passCandidates >= MaxCandidatesPerAttempt)
                    {
                        break;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Area-uniform sample of the annulus, so points do not bunch against its inner edge. Same
        /// formula AdventureSpawnController uses for its search bands.
        /// </summary>
        private static Vector2 SampleAnchor(float minRadius, float maxRadius)
        {
            float radius = Mathf.Sqrt(Mathf.Lerp(minRadius * minRadius, maxRadius * maxRadius,
                UnityEngine.Random.value));
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        }

        /// <summary>
        /// Keeps the K cells closest to the anchor. K is tiny, so straight insertion into a fixed
        /// buffer beats any heap, and the scan is over one biome's in-band slice rather than the
        /// whole index.
        /// </summary>
        private static int FindNearestCells(BiomeBucket bucket, int lo, int hi, Vector2 anchor,
            int[] nearest)
        {
            float[] bestDistance = NearestDistanceScratch;
            int count = 0;

            for (int i = lo; i < hi; i++)
            {
                float dx = bucket.X[i] - anchor.x;
                float dz = bucket.Z[i] - anchor.y;
                float distance = dx * dx + dz * dz;

                if (count == NearestCellCount && distance >= bestDistance[count - 1])
                {
                    continue;
                }

                int insertAt = count < NearestCellCount ? count : NearestCellCount - 1;
                while (insertAt > 0 && bestDistance[insertAt - 1] > distance)
                {
                    bestDistance[insertAt] = bestDistance[insertAt - 1];
                    nearest[insertAt] = nearest[insertAt - 1];
                    insertAt--;
                }

                bestDistance[insertAt] = distance;
                nearest[insertAt] = i;

                if (count < NearestCellCount)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Shuffles the K nearest cells and floats one clear of the last point issued for this biome
        /// to the front. Without this, repeat requests keep resolving to the same nearest cell and
        /// consecutive bounties land on top of each other.
        /// </summary>
        private static void OrderByPreference(BiomeBucket bucket, Heightmap.Biome biome,
            int[] nearest, int count)
        {
            for (int i = count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (nearest[i], nearest[j]) = (nearest[j], nearest[i]);
            }

            if (!LastIssued.TryGetValue(biome, out var last))
            {
                return;
            }

            float spacingSq = PreferredSpacing * PreferredSpacing;
            for (int i = 0; i < count; i++)
            {
                float dx = bucket.X[nearest[i]] - last.x;
                float dz = bucket.Z[nearest[i]] - last.z;
                if (dx * dx + dz * dz >= spacingSq)
                {
                    (nearest[0], nearest[i]) = (nearest[i], nearest[0]);
                    return;
                }
            }
        }

        /// <summary>
        /// Turns a grid node into a real position. The cell centre is one candidate; the rest are
        /// jitter and rings within the cell, so repeat hits on the same cell do not return the same
        /// point and nothing ends up snapped to a visible 128m lattice.
        /// </summary>
        private static bool TryRefineInCell(Heightmap.Biome biome, Vector2 cell,
            float minRadius, float maxRadius, bool requireBand, bool requireZoneAgreement,
            ref int candidatesTried, out Vector3 point)
        {
            point = Vector3.zero;

            if (TryCandidate(biome, cell, minRadius, maxRadius, requireBand, requireZoneAgreement,
                        ref candidatesTried, out point))
            {
                return true;
            }

            float half = _cellSize * 0.5f;

            for (int i = 0; i < 11 && candidatesTried < MaxCandidatesPerAttempt; i++)
            {
                Vector2 candidate = cell + UnityEngine.Random.insideUnitCircle * half;
                if (TryCandidate(biome, candidate, minRadius, maxRadius, requireBand, requireZoneAgreement,
                        ref candidatesTried, out point))
                {
                    return true;
                }
            }

            float phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            for (int ring = 1; ring <= 3 && candidatesTried < MaxCandidatesPerAttempt; ring++)
            {
                float ringRadius = _cellSize * (0.375f * ring);
                for (int step = 0; step < 8 && candidatesTried < MaxCandidatesPerAttempt; step++)
                {
                    float angle = phase + step / 8f * Mathf.PI * 2f;
                    Vector2 candidate = cell + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius;
                    if (TryCandidate(biome, candidate, minRadius, maxRadius, requireBand, requireZoneAgreement,
                        ref candidatesTried, out point))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The predicate chain, cheapest test first. Only candidates that already match the biome pay
        /// for a height sample -- the AshLands height is cellular noise and roughly 20-30x the cost
        /// of a Meadows one.
        /// </summary>
        private static bool TryCandidate(Heightmap.Biome biome, Vector2 candidate,
            float minRadius, float maxRadius, bool requireBand, bool requireZoneAgreement,
            ref int candidatesTried, out Vector3 point)
        {
            point = Vector3.zero;
            candidatesTried++;

            float radius = candidate.magnitude;

            // Stay clear of the world edge, where height collapses to -400 regardless of biome.
            if (radius > WorldExtent.TotalRadius - _cellSize)
            {
                return false;
            }

            if (requireBand && (radius < minRadius || radius > maxRadius))
            {
                return false;
            }

            var worldGenerator = WorldGenerator.instance;
            if (worldGenerator == null)
            {
                return false;
            }

            if (worldGenerator.GetBiome(candidate.x, candidate.y) != biome)
            {
                return false;
            }

            float height = worldGenerator.GetBiomeHeight(biome, candidate.x, candidate.y, out Color mask);

            // The Ocean biome is open water by definition, so every point in it is submerged and the
            // water test would reject all of them. Everywhere else, water means a lake or a river.
            // This is also what rejects the trench that CreateAshlandsGap/CreateDeepNorthGap carve
            // around the polar caps, where a biome match sits under tens of metres of water.
            if (biome != Heightmap.Biome.Ocean &&
                height < ZoneSystem.instance.m_waterLevel - 2f)
            {
                return false;
            }

            // mask.a is the Ashlands lava mask, the same value ZoneSystem.IsLavaPreHeightmap tests --
            // already in hand here, where that method would re-derive the biome and the height.
            if (biome == Heightmap.Biome.AshLands && mask.a > 0.6f)
            {
                return false;
            }

            if (requireZoneAgreement && !ZoneAgreesOnBiome(worldGenerator, candidate, biome))
            {
                return false;
            }

            // Y only has to put the spawner's downward ground raycast (which starts 5000m above the
            // given point) over real terrain. Height plus the long-standing 100m offset does that
            // everywhere, and unlike the old picker it is an actual terrain height rather than a
            // constant.
            point = new Vector3(candidate.x, height + 100f, candidate.y);
            return true;
        }

        /// <summary>
        /// True when every corner of the point's zone reads as <paramref name="biome"/>.
        ///
        /// This is what makes the seed-level pick agree with the loaded world. A built Heightmap does
        /// not evaluate the biome per point: HeightmapBuilder samples GetBiome at the four corners of
        /// the 64m zone, and Heightmap.GetBiome returns the nearest-corner-weighted winner among
        /// those four -- short-circuiting to one answer for the whole zone when they agree. So a
        /// point can sit well inside a swamp and still be reported as meadows by the GetGroundData
        /// check AdventureSpawnController runs before it places anything, which would send the
        /// placement search off expanding bands for no reason. Four extra GetBiome calls make it a
        /// guarantee instead.
        /// </summary>
        private static bool ZoneAgreesOnBiome(WorldGenerator worldGenerator, Vector2 point,
            Heightmap.Biome biome)
        {
            Vector3 zonePos = ZoneSystem.GetZonePos(ZoneSystem.GetZone(new Vector3(point.x, 0f, point.y)));
            const float half = ZoneSystem.c_ZoneHalfSize;

            return worldGenerator.GetBiome(zonePos.x - half, zonePos.z - half) == biome &&
                worldGenerator.GetBiome(zonePos.x + half, zonePos.z - half) == biome &&
                worldGenerator.GetBiome(zonePos.x - half, zonePos.z + half) == biome &&
                worldGenerator.GetBiome(zonePos.x + half, zonePos.z + half) == biome;
        }

        private static int LowerBound(float[] sorted, float value)
        {
            int lo = 0;
            int hi = sorted.Length;
            while (lo < hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (sorted[mid] < value)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }

        private static int UpperBound(float[] sorted, float value)
        {
            int lo = 0;
            int hi = sorted.Length;
            while (lo < hi)
            {
                int mid = lo + (hi - lo) / 2;
                if (sorted[mid] <= value)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }
    }
}
