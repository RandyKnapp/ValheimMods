using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot.Adventure.Feature
{
    internal static class BountyLocationEarlyCache
    {
        // Per-client and deliberately unsynchronized: two players picking different spots for their own
        // bounties is fine, and the alternative (a shared list) would need an anchor to live on --
        // Haldor has none, since vanilla Trader carries no ZNetView and no networked state at all.
        //
        // Lifetime is the client's session, not the merchant panel's. Reset() clears it on world change
        // (points are world-specific); nothing else empties it, so reopening the merchant is free.
        public static Dictionary<Heightmap.Biome, List<Vector3>> PotentialBiomeLocations =
            new Dictionary<Heightmap.Biome, List<Vector3>> { };

        // Target and low-water mark are separate. The old single value of 3 was both the goal and the
        // cap, and SelectSpawnPoint consumes one per bounty or treasure map, so a biome ran dry after
        // two of them -- and every bounty after that paid for a full blocking search.
        private const int TargetPointsPerBiome = 12;
        private const int RefillThreshold = 4;

        private const int MaxTriesPerBiomeFill = 60;
        private const int MaxTriesOnDemand = 100;

        // A candidate whose zone will not come up is not worth waiting on forever; move to the next one.
        private const int MaxZoneWaitFrames = 120;

        // Only one top-up coroutine at a time, so repeated merchant visits cannot stack them.
        private static bool _refilling;

        private static Dictionary<Heightmap.Biome, Tuple<float, float>> GetRadiusRanges()
        {
            var adventureSave = Player.m_localPlayer.GetAdventureSaveData();
            Dictionary<Heightmap.Biome, Tuple<float, float>> radiusRanges = new();
            Heightmap.Biome[] biomeList = AdventureDataManager.Config.TreasureMap.GetBiomeList();

            for (int i = 0; i < biomeList.Length; i++)
            {
                if (!radiusRanges.ContainsKey(biomeList[i]))
                {
                    var biomeConfig = GetBiomeInfoConfig(biomeList[i]);
                    radiusRanges.Add(biomeList[i],
                        new Tuple<float, float>(biomeConfig.MinRadius, biomeConfig.MaxRadius));
                }
            }

            return radiusRanges;
        }

        /// <summary>
        /// Drops the cache. Called on world change -- every point is a world position, so carrying them
        /// into another world would hand out spawn points from the wrong map.
        /// </summary>
        public static void Reset()
        {
            // Stop first: a top-up still in flight would otherwise finish against the new world and file
            // points it picked from the old one.
            AdventureCacheDriver.StopAll();
            PotentialBiomeLocations.Clear();
            _refilling = false;
        }

        internal static int CachedPointCount(Heightmap.Biome biome)
        {
            return PotentialBiomeLocations.TryGetValue(biome, out var points) ? points.Count : 0;
        }

        internal static void TryAddBiomePoint(Heightmap.Biome biome, Vector3 point)
        {
            if (!PotentialBiomeLocations.TryGetValue(biome, out var points))
            {
                points = new List<Vector3>();
                PotentialBiomeLocations.Add(biome, points);
            }

            if (points.Count >= TargetPointsPerBiome)
            {
                return;
            }

            points.Add(point);
        }

        public static IEnumerator TryGetBiomePoint(
            Heightmap.Biome biome, AdventureSaveData saveData, Action<bool, Vector3> onComplete)
        {
            // Anything cached is served immediately -- the player is standing at the merchant waiting on
            // a bounty they have already paid for, so searching here is the one thing worth avoiding.
            if (CachedPointCount(biome) > 0)
            {
                SelectSpawnPoint(biome, onComplete);
                RequestRefill();
                yield break;
            }

            // Nothing banked for this biome, so there is no choice but to search now.
            yield return AddBiomePointLazyCache(GetRadiusRanges(), biome, true, onComplete);
            RequestRefill();
        }

        /// <summary>
        /// Tops the cache up to <see cref="TargetPointsPerBiome"/>. Called from MerchantPanel.Awake, so
        /// it runs again every time the panel is rebuilt -- it deliberately does not clear anything, and
        /// returns immediately when every biome is already stocked. Regenerating on each merchant visit
        /// is what made accepting a bounty pay for hundreds of world-point tests.
        /// </summary>
        public static IEnumerator PopulateCacheFromStart()
        {
            // Can't setup the cache without a player
            if (Player.m_localPlayer == null)
            {
                yield break;
            }

            yield return FillBiomesBelowTarget();
        }

        /// <summary>
        /// Kicks off a background top-up when any biome has fallen below the low-water mark. Fire and
        /// forget: the caller never waits on it, so consuming a point never blocks on a search.
        /// </summary>
        public static void RequestRefill(bool ignoreThreshold = false)
        {
            if (_refilling || Player.m_localPlayer == null)
            {
                return;
            }

            if (!ignoreThreshold && !AnyBiomeBelowThreshold())
            {
                return;
            }

            AdventureCacheDriver.Run(RefillCache());
        }

        private static bool AnyBiomeBelowThreshold()
        {
            foreach (var biome in AdventureDataManager.Config.TreasureMap.GetBiomeList())
            {
                if (biome == Heightmap.Biome.None || biome == Heightmap.Biome.All)
                {
                    continue;
                }

                if (CachedPointCount(biome) < RefillThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerator RefillCache()
        {
            _refilling = true;
            try
            {
                yield return FillBiomesBelowTarget();
            }
            finally
            {
                _refilling = false;
            }
        }

        private static IEnumerator FillBiomesBelowTarget()
        {
            Dictionary<Heightmap.Biome, Tuple<float, float>> radiusRanges = GetRadiusRanges();

            foreach (var biome in AdventureDataManager.Config.TreasureMap.GetBiomeList())
            {
                if (biome == Heightmap.Biome.None || biome == Heightmap.Biome.All)
                {
                    continue;
                }

                // Already stocked. Points do not go stale -- the world does not change -- so there is
                // nothing to refresh and this is the path a repeat merchant visit takes.
                if (CachedPointCount(biome) >= TargetPointsPerBiome)
                {
                    continue;
                }

                yield return AddBiomePointLazyCache(radiusRanges, biome);
            }
        }

        public static IEnumerator AddBiomePointLazyCache(Dictionary<Heightmap.Biome, Tuple<float, float>> radiusRanges,
            Heightmap.Biome biome, bool requireSelection = false,
            Action<bool, Vector3> onComplete = null)
        {
            int tries = 0;
            int maxTries = requireSelection ? MaxTriesOnDemand : MaxTriesPerBiomeFill;

            while (true)
            {
                // Fail safe, exit coroutine.
                if (tries > maxTries)
                {
                    onComplete?.Invoke(false, Vector3.zero);
                    yield break;
                }

                // Prevent locking main thread. Every path below increments tries, so this is guaranteed
                // to come around -- it previously could not, because the radius-reject path used to
                // `continue` without incrementing, leaving the loop spinning with nothing to yield on.
                if (tries % 10 == 0 && tries > 1)
                {
                    yield return new WaitForSeconds(1f);
                }

                // Stop filling the moment the biome is stocked, rather than burning the full try budget.
                if (!requireSelection && CachedPointCount(biome) >= TargetPointsPerBiome)
                {
                    yield break;
                }

                var range = radiusRanges.ContainsKey(biome) ? radiusRanges[biome] :
                    new Tuple<float, float>(0f, WorldGenerator.waterEdge);
                var spawnPoint = SelectWorldPoint(range, tries, biome);

                // Cheap rejection first. WorldGenerator answers biome and height from the world seed with
                // no instantiation at all, and it is the same data the Heightmap is built from -- so any
                // point it rejects would have failed the full check below too. This is what keeps the
                // zone spawn underneath from running on nearly every candidate.
                if (!PassesWorldGenPreFilter(spawnPoint))
                {
                    tries++;
                    continue;
                }

                // The full check needs a real Heightmap (GetGroundData raycasts terrain colliders, and
                // the lava test reads the vegetation mask), so the zone has to exist for it.
                var zoneId = ZoneSystem.GetZone(spawnPoint);
                GameObject zoneRoot = null;
                int zoneWaitFrames = 0;
                while (!ZoneSystem.instance.SpawnZone(zoneId, ZoneSystem.SpawnMode.Client, out zoneRoot))
                {
                    // A zone whose location prefab has not finished loading can stay unavailable for an
                    // open-ended time. Give up on this candidate rather than waiting on it forever.
                    if (++zoneWaitFrames > MaxZoneWaitFrames)
                    {
                        break;
                    }

                    yield return new WaitForEndOfFrame();
                }

                if (zoneRoot == null)
                {
                    tries++;
                    continue;
                }

                Heightmap.Biome spawnLocationBiome;
                bool valid;
                try
                {
                    valid = IsSpawnLocationValid(spawnPoint, out spawnLocationBiome);
                }
                finally
                {
                    // SpawnZone always instantiates a fresh root and, unlike PokeLocalZone, we never
                    // register it in ZoneSystem.m_zones -- so UpdateTTL will never destroy it. Left alone
                    // this leaked a zone and its terrain mesh on every single try.
                    UnityEngine.Object.Destroy(zoneRoot);
                }

                if (!valid)
                {
                    tries++;
                    continue;
                }

                if (requireSelection && spawnLocationBiome == biome)
                {
                    EpicLoot.Log($"Returning callback for Add Biome valid location: {biome} at {spawnPoint}");
                    spawnPoint.y += 100f;
                    onComplete?.Invoke(true, spawnPoint);
                    yield break;
                }

                if (radiusRanges.ContainsKey(spawnLocationBiome))
                {
                    var min = radiusRanges[spawnLocationBiome].Item1;
                    var max = radiusRanges[spawnLocationBiome].Item2;
                    var mag = new Vector2(spawnPoint.x, spawnPoint.z).magnitude;
                    if (mag < min || mag > max)
                    {
                        tries++;
                        continue;
                    }
                }

                TryAddBiomePoint(spawnLocationBiome, spawnPoint);
                tries++;
            }
        }

        /// <summary>
        /// Seed-level rejection of a candidate point, with nothing instantiated. Mirrors the biome and
        /// water tests in <see cref="IsSpawnLocationValid"/> using WorldGenerator, which is the source
        /// the Heightmap is generated from -- so this only ever rejects points the full check rejects.
        /// </summary>
        private static bool PassesWorldGenPreFilter(Vector3 point)
        {
            var worldGenerator = WorldGenerator.instance;
            if (worldGenerator == null)
            {
                // No generator to consult yet; let the full check decide.
                return true;
            }

            var biome = worldGenerator.GetBiome(point.x, point.z);
            if (biome == Heightmap.Biome.None)
            {
                return false;
            }

            if (biome != Heightmap.Biome.Ocean &&
                ZoneSystem.instance.m_waterLevel > worldGenerator.GetHeight(point.x, point.z) + 2f)
            {
                return false;
            }

            return true;
        }

        internal static void SelectSpawnPoint(Heightmap.Biome biome, Action<bool, Vector3> onComplete)
        {
            List<Vector3> locations = PotentialBiomeLocations[biome];
            Vector3 selectedLocation = locations.First();
            locations.RemoveAt(0);
            PotentialBiomeLocations[biome] = locations;

            ZoneSystem.instance.GetGroundData(
                ref selectedLocation, out var normal, out var foundBiome, out var biomeArea, out var hmap);
            selectedLocation.y += 100f;
            onComplete?.Invoke(true, selectedLocation);
        }

        internal static Vector3 SelectWorldPoint(Tuple<float, float> range, int intervalRange, Heightmap.Biome biome)
        {
            var minimumDistance = range.Item1;
            var maximumDistance = range.Item2;

            if (biome == Heightmap.Biome.AshLands || biome == Heightmap.Biome.DeepNorth)
            {
                // For biomes that are situated in specific areas (eg top/bottom of the world)
                float direction = 1f;
                if (biome == Heightmap.Biome.AshLands)
                {
                    direction = -1f;
                }

                float naturalY =  UnityEngine.Random.Range(minimumDistance + (intervalRange * 90),
                    minimumDistance + (intervalRange * 90) + 100f);
                float yDirection = naturalY * direction;
                float xDirection = UnityEngine.Random.Range(-1f * (minimumDistance / 2), (minimumDistance / 2));
                return new Vector3(xDirection, 0, yDirection);
            }
            else
            {
                // For biomes that are scattered throughout the world
                var randomPoint = UnityEngine.Random.insideUnitCircle;
                var magnitude = Mathf.Lerp(minimumDistance,
                    maximumDistance, randomPoint.magnitude);
                randomPoint = randomPoint * magnitude;
                return new Vector3(randomPoint.x, 0, randomPoint.y);
            }
        }

        internal static bool IsSpawnLocationValid(Vector3 location, out Heightmap.Biome biome)
        {
            ZoneSystem.instance.GetGroundData(
                ref location, out var normal, out biome, out var biomeArea, out var hmap);

            if (biome == Heightmap.Biome.None || hmap == null)
            {
                return false;
            }

            // Ashlands biome, and location is in lava | Try not to spawn in lava
            if (biome == Heightmap.Biome.AshLands && hmap.IsLava(location))
            {
                return false;
            }

            float groundHeight = location.y;
            var waterLevel = ZoneSystem.instance.m_waterLevel;
            // Small buffer allowing spawns in shallow water
            if (biome != Heightmap.Biome.Ocean && ZoneSystem.instance.m_waterLevel > groundHeight + 2f)
            {
                return false;
            }

            // Is too near to player base
            if (EffectArea.IsPointInsideArea(location, EffectArea.Type.PlayerBase,
                AdventureDataManager.Config.TreasureMap.MinimapAreaRadius))
            {
                return false;
            }

            // Is too near to player ward
            if (AdventureWardCheck.TryFindNearbyWard(location,
                    AdventureDataManager.Config.TreasureMap.MinimapAreaRadius, out _))
            {
                return false;
            }

            return true;
        }

        // TODO: Decide if we want to keep the RadiusInterval and IncreaseRadiusCount configs
        /*private static Tuple<float, float> GetTreasureMapSpawnRadiusRange(Heightmap.Biome biome, AdventureSaveData saveData)
        {
            var biomeInfoConfig = GetBiomeInfoConfig(biome);
            if (biomeInfoConfig == null)
            {
                EpicLoot.LogError($"Could not get biome info for biome: {biome}!");
                EpicLoot.LogWarning($"> Current BiomeInfo ({AdventureDataManager.Config.TreasureMap.BiomeInfo.Count}):");
                foreach (var biomeInfo in AdventureDataManager.Config.TreasureMap.BiomeInfo)
                {
                    EpicLoot.Log($"- {biomeInfo.Biome}: min:{biomeInfo.MinRadius}, max:{biomeInfo.MaxRadius}");
                }

                return new Tuple<float, float>(-1, -1);
            }

            var minSearchRange = biomeInfoConfig.MinRadius;
            var maxSearchRange = biomeInfoConfig.MaxRadius;
            var searchBandWidth = AdventureDataManager.Config.TreasureMap.StartRadiusMax -
                AdventureDataManager.Config.TreasureMap.StartRadiusMin;
            var numberOfBounties = AdventureDataManager.CheatNumberOfBounties >= 0 ?
                AdventureDataManager.CheatNumberOfBounties : saveData.NumberOfTreasureMapsOrBountiesStarted;
            var increments = (numberOfBounties / AdventureDataManager.Config.TreasureMap.IncreaseRadiusCount) % 20;
            var min1 = minSearchRange +
                (AdventureDataManager.Config.TreasureMap.StartRadiusMin +
                    increments * AdventureDataManager.Config.TreasureMap.RadiusInterval);
            var max1 = min1 + searchBandWidth;
            var min = Mathf.Clamp(min1, minSearchRange, maxSearchRange - searchBandWidth);
            var max = Mathf.Clamp(max1, minSearchRange + searchBandWidth, maxSearchRange);
            EpicLoot.Log($"Got biome info for biome ({biome}) - " +
                $"Overall search range: {minSearchRange}-{maxSearchRange}. " +
                $"Current increments: {increments}. " +
                $"Current search band: {min}-{max} (width={searchBandWidth})");
            return new Tuple<float, float>(min, max);
        }*/

        private static TreasureMapBiomeInfoConfig GetBiomeInfoConfig(Heightmap.Biome biome)
        {
            return AdventureDataManager.Config.TreasureMap.BiomeInfo.Find(x => x.Biome == biome);
        }
    }

    /// <summary>
    /// Host for the background cache top-up. The merchant panel cannot run it -- StoreGui deactivates
    /// that object on close, which would stop the coroutine partway -- and a refill is normally
    /// requested at the moment a point is consumed, i.e. while the panel is on its way out.
    /// </summary>
    internal class AdventureCacheDriver : MonoBehaviour
    {
        private static AdventureCacheDriver _instance;

        public static void Run(IEnumerator routine)
        {
            if (_instance == null)
            {
                var go = new GameObject("EpicLoot_AdventureCacheDriver");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<AdventureCacheDriver>();
            }

            _instance.StartCoroutine(routine);
        }

        public static void StopAll()
        {
            if (_instance != null)
            {
                _instance.StopAllCoroutines();
            }
        }
    }
}
