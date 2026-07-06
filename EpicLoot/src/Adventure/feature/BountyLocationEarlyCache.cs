using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot.Adventure.Feature
{
    internal static class BountyLocationEarlyCache
    {
        // This could be shifted to multiple variable zsynced lists to preserve the generated values for future use.
        public static Dictionary<Heightmap.Biome, List<Vector3>> PotentialBiomeLocations =
            new Dictionary<Heightmap.Biome, List<Vector3>> { };
        private static int _minimumLocationKeys = 3;

        private static int _cacheTriesPerBiome = 10;
        private static int _maximumTries = 100;

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

        internal static void TryAddBiomePoint(Heightmap.Biome biome, Vector3 point)
        {
            if (!PotentialBiomeLocations.ContainsKey(biome))
            {
                PotentialBiomeLocations.Add(biome, new List<Vector3>() { });
            }
            else if (PotentialBiomeLocations[biome].Count >= _minimumLocationKeys)
            {
                return;
            }

            PotentialBiomeLocations[biome].Add(point);
        }

        public static IEnumerator TryGetBiomePoint(
            Heightmap.Biome biome, AdventureSaveData saveData, Action<bool, Vector3> onComplete)
        {
            Dictionary<Heightmap.Biome, Tuple<float, float>> radiusRanges = GetRadiusRanges();

            // Check if any valid cached
            if (PotentialBiomeLocations.ContainsKey(biome) && PotentialBiomeLocations[biome].Count > 1)
            {
                SelectSpawnPoint(biome, onComplete);
                yield break;
            }

            yield return AddBiomePointLazyCache(radiusRanges, biome, true, onComplete);
        }

        public static IEnumerator PopulateCacheFromStart()
        {
            // Can't setup the cache without a player
            if (Player.m_localPlayer == null)
            {
                yield break;
            }

            // Clear old data
            PotentialBiomeLocations = new Dictionary<Heightmap.Biome, List<Vector3>> { };
            Dictionary<Heightmap.Biome, Tuple<float, float>> radiusRanges = GetRadiusRanges();

            int index = 0;
            Heightmap.Biome[] biomeList = AdventureDataManager.Config.TreasureMap.GetBiomeList();

            while (index < biomeList.Length)
            {
                Heightmap.Biome targetBiome = biomeList[index];

                Tuple<float, float> radiusRange = radiusRanges[targetBiome];

                yield return AddBiomePointLazyCache(radiusRanges, targetBiome);

                index++;

                // Check if all biomes have the required number of keys
                bool isReady = true;
                foreach (var biome in biomeList)
                {
                    if (biome == Heightmap.Biome.None || biome == Heightmap.Biome.All)
                    {
                        continue;
                    }

                    if (!PotentialBiomeLocations.ContainsKey(biome) ||
                        PotentialBiomeLocations[biome].Count < _minimumLocationKeys)
                    {
                        isReady = false;
                        break;
                    }
                }

                if (isReady)
                {
                    break;
                }
            }
        }

        public static IEnumerator AddBiomePointLazyCache(Dictionary<Heightmap.Biome, Tuple<float, float>> radiusRanges,
            Heightmap.Biome biome, bool requireSelection = false,
            Action<bool, Vector3> onComplete = null)
        {
            int tries = 0;

            while (true)
            {
                // Fail safe, exit coroutine.
                if ((!requireSelection && tries > _cacheTriesPerBiome) || tries > _maximumTries)
                {
                    onComplete?.Invoke(false, Vector3.zero);
                    yield break;
                }

                // Prevent locking main thread.
                if (tries % 20 == 0 && tries > 1)
                {
                    yield return new WaitForSeconds(1f);
                }
                var range = radiusRanges.ContainsKey(biome) ? radiusRanges[biome] :
                    new Tuple<float, float>(0f, WorldGenerator.waterEdge);
                var spawnPoint = SelectWorldPoint(range, tries, biome);
                var zoneId = ZoneSystem.GetZone(spawnPoint);
                while (!ZoneSystem.instance.SpawnZone(zoneId, ZoneSystem.SpawnMode.Client, out _))
                {
                    // Wait until the zone is spawned.
                    yield return new WaitForEndOfFrame();
                }

                if (!IsSpawnLocationValid(spawnPoint, out Heightmap.Biome spawnLocationBiome))
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
                else
                {
                    if (radiusRanges.ContainsKey(spawnLocationBiome))
                    {
                        var min = radiusRanges[spawnLocationBiome].Item1;
                        var max = radiusRanges[spawnLocationBiome].Item2;
                        var mag = new Vector2(spawnPoint.x, spawnPoint.z).magnitude;
                        if (mag < min || mag > max)
                        {
                            continue;
                        }
                    }

                    TryAddBiomePoint(spawnLocationBiome, spawnPoint);
                }

                tries++;
            }
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
            var tooCloseToWard = PrivateArea.m_allAreas.Any(
                x => x.IsInside(location, AdventureDataManager.Config.TreasureMap.MinimapAreaRadius));
            if (tooCloseToWard)
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
}
