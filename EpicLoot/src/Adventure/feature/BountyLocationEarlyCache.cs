using System;
using System.Collections;
using UnityEngine;

namespace EpicLoot.Adventure.Feature
{
    /// <summary>
    /// Picks the world position a bounty target or treasure chest is sent to.
    ///
    /// The name is historical: this used to keep a cache of pre-vetted points because finding one was
    /// slow enough to need hiding. It no longer is. A request now samples <see cref="WorldBiomeIndex"/>
    /// -- a seed-derived map of where each biome is -- picks an anchor inside the biome's configured
    /// radius band, and refines to an exact point, all from WorldGenerator with nothing instantiated.
    /// That takes tens of microseconds, so there is nothing left worth caching, and the cache's own
    /// failure modes (a biome running dry, a stuck refill latch blocking every later request) are gone
    /// with it.
    ///
    /// Everything here is seed-only and therefore blind to terrain colliders, wards and player bases.
    /// <see cref="AdventureSpawnController.DeterminespawnPoint"/> checks all of those with an
    /// expanding band search once the player is actually near the point, and stays the authority on
    /// where the bounty or chest finally lands.
    /// </summary>
    internal static class BountyLocationEarlyCache
    {
        /// <summary>
        /// How long a request will wait for a world biome index that is still building. Generous
        /// because it has to cover a first build on a very large world; the realistic wait is a
        /// fraction of a second, and the merchant panel normally warms the index on open anyway.
        /// </summary>
        private const float IndexWaitTimeoutSeconds = 30f;

        /// <summary>
        /// Drops all world-derived state. Called on world change -- every point is a world position,
        /// so carrying anything over would hand out locations from the wrong map.
        /// </summary>
        public static void Reset()
        {
            // Stop first: a build still in flight would otherwise finish against the new world and
            // publish an index sampled from the old one.
            AdventureCacheDriver.StopAll();
            WorldBiomeIndex.Reset();
        }

        /// <summary>
        /// Finds a spawn point in <paramref name="biome"/> and hands it to
        /// <paramref name="onComplete"/>.
        ///
        /// <paramref name="saveData"/> is unused. It is kept because both callers pass it and because
        /// the radius ramp that once read it (widening the search band by how many bounties a player
        /// had taken) may come back; see the obsolete StartRadius/RadiusInterval config fields.
        ///
        /// <paramref name="onComplete"/> is invoked on every exit path, success or failure. That is
        /// the contract that matters here: the old implementation could give up silently, and the
        /// symptom players saw was a merchant button that did nothing at all.
        /// </summary>
        public static IEnumerator TryGetBiomePoint(
            Heightmap.Biome biome, AdventureSaveData saveData, Action<bool, Vector3> onComplete)
        {
            if (biome == Heightmap.Biome.None || biome == Heightmap.Biome.All)
            {
                // Not a real place. A caller or a config asked for something that cannot exist.
                EpicLoot.LogErrorForce($"Asked for an adventure spawn point in biome '{biome}', " +
                    "which is not a single biome. This is a configuration or caller error.");
                Fail(biome, "$mod_epicloot_adventure_locatefailed", onComplete);
                yield break;
            }

            WorldBiomeIndex.EnsureBuilt();

            // unscaledTime so a paused or time-scaled game still times out, and so the deadline is
            // not affected by the frame rate the way a frame counter would be.
            float deadline = Time.unscaledTime + IndexWaitTimeoutSeconds;
            while (WorldBiomeIndex.State == BiomeIndexState.Building && Time.unscaledTime < deadline)
            {
                yield return null;
            }

            if (WorldBiomeIndex.State != BiomeIndexState.Ready)
            {
                EpicLoot.LogWarningForce("Cannot pick an adventure spawn point: the world biome index " +
                    $"is {WorldBiomeIndex.State} after waiting {IndexWaitTimeoutSeconds:0}s.");
                Fail(biome, "$mod_epicloot_adventure_locateunavailable", onComplete);
                yield break;
            }

            GetRadiusBand(biome, out float minRadius, out float maxRadius);

            // Preferred: a point inside the configured band, which is what gates a biome's distance
            // from the world centre and therefore how far a player has to travel for it.
            if (WorldBiomeIndex.TryFindPoint(biome, minRadius, maxRadius, true,
                    out Vector3 point, out int candidates))
            {
                EpicLoot.Log($"Picked {biome} spawn at ({point.x:0}, {point.z:0}) " +
                    $"r={new Vector2(point.x, point.z).magnitude:0} ({candidates} candidates)");
                onComplete?.Invoke(true, point);
                yield break;
            }

            // The band and the biome do not overlap on this world. Take the nearest match anywhere
            // rather than refusing: a player who paid for this would rather travel oddly far than get
            // nothing. The warning is the signal that the bands need retuning for this world.
            if (WorldBiomeIndex.TryFindPoint(biome, minRadius, maxRadius, false, out point, out candidates))
            {
                float radius = new Vector2(point.x, point.z).magnitude;
                EpicLoot.LogWarning($"No usable {biome} point inside the configured " +
                    $"{minRadius:0}-{maxRadius:0}m band ({WorldExtent.Describe()}); " +
                    $"using the nearest at {radius:0}m instead ({candidates} candidates).");
                onComplete?.Invoke(true, point);
                yield break;
            }

            EpicLoot.LogWarningForce($"The world biome index has no usable {biome} location at a " +
                $"{WorldBiomeIndex.CellSize:0.#}m sample spacing " +
                $"({WorldBiomeIndex.CountCells(biome)} cells indexed). " +
                "This world may not contain that biome.");
            Fail(biome, "$mod_epicloot_adventure_locatefailed", onComplete);
        }

        /// <summary>
        /// Reports the failure to the player and completes the request. Never leave a caller waiting
        /// on a callback that does not come.
        /// </summary>
        private static void Fail(Heightmap.Biome biome, string token, Action<bool, Vector3> onComplete)
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                string biomeName = Localization.instance.Localize($"$biome_{biome.ToString().ToLowerInvariant()}");
                player.Message(MessageHud.MessageType.Center,
                    Localization.instance.Localize(token, biomeName));
            }

            onComplete?.Invoke(false, Vector3.zero);
        }

        /// <summary>
        /// The distance-from-world-centre band a biome's spawn points must fall inside.
        ///
        /// The configured values are absolute metres authored against a vanilla 10km world. On a world
        /// resized by Expand World Size they no longer describe where anything is -- AshLands' shipped
        /// 8000-10500 band sits in the inner Meadows of a 40km map -- so they are scaled by the real
        /// world radius unless the pack author has already retuned them.
        /// </summary>
        internal static void GetRadiusBand(Heightmap.Biome biome, out float min, out float max)
        {
            float limit = WorldExtent.TotalRadius;
            var biomeConfig = GetBiomeInfoConfig(biome);

            if (biomeConfig == null)
            {
                // Legitimate for a biome nothing sells a map for -- DeepNorth has no BiomeInfo entry
                // but does have bounty targets -- so search the whole world rather than refusing.
                EpicLoot.LogWarning($"No adventure BiomeInfo entry for {biome}; " +
                    "searching the whole world for a spawn point.");
                min = 0f;
                max = WorldExtent.PlayableRadius;
                return;
            }

            float scale = AdventureDataManager.Config.TreasureMap.ScaleRadiiToWorldSize
                ? WorldExtent.RadiusScale
                : 1f;

            min = Mathf.Clamp(Mathf.Min(biomeConfig.MinRadius, biomeConfig.MaxRadius) * scale, 0f, limit);
            max = Mathf.Clamp(Mathf.Max(biomeConfig.MinRadius, biomeConfig.MaxRadius) * scale, 0f, limit);

            if (max <= min)
            {
                // A band that clamped to nothing, usually a hand-edited config or a MaxRadius left at
                // zero. Fall back to the whole world instead of searching an empty annulus.
                EpicLoot.LogWarning($"Adventure radius band for {biome} is empty after scaling " +
                    $"({biomeConfig.MinRadius}-{biomeConfig.MaxRadius} x {scale:0.###}); " +
                    "searching the whole world instead.");
                min = 0f;
                max = WorldExtent.PlayableRadius;
            }
        }

        private static TreasureMapBiomeInfoConfig GetBiomeInfoConfig(Heightmap.Biome biome)
        {
            return AdventureDataManager.Config.TreasureMap.BiomeInfo.Find(x => x.GetBiome() == biome);
        }
    }
}
