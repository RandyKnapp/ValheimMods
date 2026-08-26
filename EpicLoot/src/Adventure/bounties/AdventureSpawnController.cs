using EpicLoot.Data;
using System.Collections;
using System.Collections.Generic;
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

    internal class AdventureSpawnController : MonoBehaviour
    {
        protected ZNetView zNetView;
        private BountyInfoZNetProperty bounty { get; set; }
        private TreasureMapChestInfoZNetProperty treasure { get; set; }
        private BoolZNetProperty placed { get; set; }
        private BoolZNetProperty searchingForSpawn { get; set; }
        private Vector3ZNetProperty spawnPoint { get; set; }

        private BoolZNetProperty isBounty { get; set; }

        /// <summary>Frames to settle before the first search attempt.</summary>
        private const int InitialSearchDelayFrames = 300;

        /// <summary>
        /// Frames to wait before re-attempting a search that found nowhere to spawn. Longer than the
        /// initial delay because a blocked search usually stays blocked until a ward comes down.
        /// </summary>
        private const int RetrySearchDelayFrames = 1800;

        private const int SpawnAttemptsPerBand = 100;

        /// <summary>Set once a missing creature prefab has been reported, so it is logged one time.</summary>
        private bool reportedMissingPrefab = false;

        private int currentUpdates = 0;
        private int updatesRequired = InitialSearchDelayFrames;
        private bool startedPlacement = false;
        private Vector3 defaultSpawn = new(1, 1, 1);
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
                bounty = new BountyInfoZNetProperty("bount_spawn", zNetView, defaultBounty);
                treasure = new TreasureMapChestInfoZNetProperty("treasure_spawn", zNetView, defaultTreasure);
                isBounty = new BoolZNetProperty("isBounty", zNetView, false);
                placed = new BoolZNetProperty("placed", zNetView, false);
                searchingForSpawn = new BoolZNetProperty("searchingForSpawn", zNetView, false);
                spawnPoint = new Vector3ZNetProperty("spawnPoint", zNetView, defaultSpawn);
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

            if (currentUpdates < updatesRequired)
            {
                currentUpdates += 1;
                return;
            }

            if (startedPlacement == false)
            {
                EpicLoot.Log("Starting search for valid spawn location...");
                searchingForSpawn.Set(true);
                startedPlacement = true;
                if (bounty.Get().PlayerID != 0)
                {
                    StartCoroutine(DeterminespawnPoint(bounty.Get().Position, bounty.Get().Biome,
                        WaterPlacement.Surface));
                }

                if (treasure.Get().PlayerID != 0)
                {
                    StartCoroutine(DeterminespawnPoint(treasure.Get().Position, treasure.Get().Biome,
                        WaterPlacement.Seabed));
                }
            }

            if (searchingForSpawn.Get() == true && spawnPoint.Get() == defaultSpawn)
            {
                return;
            }

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

        private void SpawnBountyTargets(BountyInfo bounty)
        {
            Vector3 point = spawnPoint.Get();
            var mainPrefab = ZNetScene.instance.GetPrefab(bounty.Target.MonsterID);
            if (mainPrefab == null)
            {
                ReportMissingPrefab("target", bounty.ID, bounty.Target.MonsterID);
                return;
            }

            var prefabs = new List<GameObject>() { mainPrefab };
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
                    prefabs.Add(prefab);
                }
            }

            // An ocean bounty's targets swim, so they hold the water line the search settled on
            // instead of being dropped onto a seabed that is tens of metres further down.
            bool swimmingTargets = bounty.Biome == Heightmap.Biome.Ocean;
            float baseHeight = point.y;

            for (var index = 0; index < prefabs.Count; index++)
            {
                var prefab = prefabs[index];
                var isAdd = index > 0;

                // Character.UpdateSwimming holds a swimming creature at (water line - m_swimDepth),
                // so starting it there means it is already buoyant rather than dropping in from
                // above the surface.
                Vector3 spawnAt = point;
                if (swimmingTargets && prefab.TryGetComponent(out Character prefabCharacter))
                {
                    spawnAt.y = ZoneSystem.instance.m_waterLevel - prefabCharacter.m_swimDepth;
                }

                var creature = UnityEngine.Object.Instantiate(prefab, spawnAt, Quaternion.identity);
                var bountyTarget = creature.AddComponent<BountyTarget>();
                bountyTarget.Initialize(bounty, prefab.name, isAdd);

                var randomSpacing = UnityEngine.Random.insideUnitSphere * 4f;
                point += randomSpacing;

                // FindFloor reports 0 when its ray hits nothing at all, and the old code assigned
                // that unconditionally -- a miss teleported the next add down to y=0.
                if (!swimmingTargets && ZoneSystem.instance.FindFloor(point, out var floorHeight))
                {
                    point.y = floorHeight;
                }
                else
                {
                    point.y = baseHeight;
                }
            }

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
            placed.ForceSet(true);
        }

        internal IEnumerator DeterminespawnPoint(Vector3 startingSpawnPoint,
            Heightmap.Biome biome, WaterPlacement waterPlacement = WaterPlacement.Reject)
        {
            yield return new WaitForSeconds(5);

            while (!ZNetScene.instance.IsAreaReady(startingSpawnPoint))
            {
                yield return new WaitForSeconds(1f);
            }

            // TODO: If bounties get their own minimap area radius config this must choose the correct one
            float radius = AdventureDataManager.Config.TreasureMap.MinimapAreaRadius;
            float waterSurface = ZoneSystem.instance.m_waterLevel;

            // The Ocean biome is open water by definition -- its biome cutoff sits roughly 25m below
            // the water line, so every point inside it is under water. Rejecting submerged points
            // there rejected every candidate in every band, which is why no ocean bounty ever placed.
            bool spawnInOpenWater = biome == Heightmap.Biome.Ocean &&
                waterPlacement != WaterPlacement.Reject;
            int maxExpansions = Mathf.Max(0, AdventureDataManager.Config.TreasureMap.MaxSpawnSearchExpansions);
            Vector3 determinedSpawn = startingSpawnPoint;
            bool foundSpawn = false;
            PrivateArea blockingWard = null;

            // Band 0 is the original search disc. Every band after it is an annulus one MinimapAreaRadius
            // further out - the smallest step that can escape a ward, since a ward vetoes everything
            // within its own radius + MinimapAreaRadius.
            for (int band = 0; band <= maxExpansions && !foundSpawn; band++)
            {
                float innerRadius = band == 0 ? 0f : radius * 0.8f + (band - 1) * radius;
                float outerRadius = band == 0 ? radius * 0.8f : radius * 0.8f + band * radius;

                int spawnLocationAttempts = 0;

                // Attempt to find a spawn point, valid height must be selected
                while (spawnLocationAttempts < SpawnAttemptsPerBand)
                {
                    // Area-uniform sample of the ring, so points do not bunch up against its inner edge.
                    // For band 0 this is identical to the old Random.insideUnitCircle * (radius * 0.8f).
                    float sampleRadius = Mathf.Sqrt(Mathf.Lerp(innerRadius * innerRadius,
                        outerRadius * outerRadius, UnityEngine.Random.value));
                    float sampleAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    determinedSpawn = startingSpawnPoint + new Vector3(
                        Mathf.Cos(sampleAngle) * sampleRadius, 0, Mathf.Sin(sampleAngle) * sampleRadius);

                    if (spawnLocationAttempts > 1 && spawnLocationAttempts % 10 == 0)
                    {
                        // Sleep to avoid locking the thread
                        yield return new WaitForSeconds(1f);
                    }

                    ZoneSystem.instance.GetGroundData(
                        ref determinedSpawn, out var normal, out var foundBiome, out var biomeArea, out var hmap);

                    if (hmap == null || foundBiome != biome)
                    {
                        spawnLocationAttempts += 1;
                        continue;
                    }

                    float terrainHeight = determinedSpawn.y;
                    float solidHeight = StartingHeight;

                    if (ZoneSystem.instance.FindFloor(new Vector3(determinedSpawn.x, determinedSpawn.y + 100f, determinedSpawn.z), out solidHeight))
                    {
                        float terrainDiff = solidHeight - terrainHeight;

                        // Prevent spawns in objects and too high off the ground
                        if (terrainDiff > 0.5f)
                        {
                            spawnLocationAttempts += 1;
                            continue;
                        }

                        if (terrainDiff > 0f)
                        {
                            determinedSpawn.y = solidHeight;
                        }
                    }
                    else
                    {
                        spawnLocationAttempts += 1;
                        continue;
                    }

                    // Prevents spawning in a body of water. Open-water spawns are exempt: the
                    // seabed is the ground there, and a surface spawn is lifted to the water line
                    // once a point is settled on.
                    if (!spawnInOpenWater && determinedSpawn.y < waterSurface - 1f)
                    {
                        spawnLocationAttempts += 1;
                        continue;
                    }

                    // Prevent spawning in Lava unless a last resort
                    if (biome == Heightmap.Biome.AshLands &&
                        hmap.GetVegetationMask(determinedSpawn) > 0.45f)
                    {
                        spawnLocationAttempts += 1;
                        continue;
                    }

                    // Keep the spawn out of player bases. Unlike the check made when the world point was
                    // first picked, the wards around here are actually loaded by now.
                    if (AdventureWardCheck.TryFindNearbyWard(determinedSpawn, radius, out PrivateArea ward))
                    {
                        blockingWard = ward;
                        spawnLocationAttempts += 1;
                        continue;
                    }

                    foundSpawn = true;
                    break;
                }

                if (!foundSpawn && band < maxExpansions)
                {
                    EpicLoot.LogWarning(
                        $"No valid adventure spawn point in search band {band} " +
                        $"({innerRadius:0.##}-{outerRadius:0.##}m); expanding. " +
                        $"Start=({startingSpawnPoint.x:0.##}, {startingSpawnPoint.y:0.##}, {startingSpawnPoint.z:0.##}), " +
                        $"Biome={biome}, Ward={AdventureWardCheck.DescribeWard(blockingWard)}");
                }
            }

            if (!foundSpawn)
            {
                EpicLoot.LogWarning(
                    "Could not find a valid adventure spawn point after exhausting every search band. " +
                    $"Start=({startingSpawnPoint.x:0.##}, {startingSpawnPoint.y:0.##}, {startingSpawnPoint.z:0.##}), " +
                    $"Biome={biome}, SearchRadius={radius:0.##}, Expansions={maxExpansions}, " +
                    $"Ward={AdventureWardCheck.DescribeWard(blockingWard)}. " +
                    "Leaving the spawner in place to retry - it is not safe to discard a bounty or " +
                    "treasure map the player has already paid for.");
                ParkAndRetry();
                yield break;
            }

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

            // A point from an outer band no longer sits under the map marker, so the marker has to
            // follow it. Pins live in per-player local save data, so only the player who bought this
            // spawn can move theirs - anyone else parks and leaves it for the owner.
            if (RequiresPinRelocation(startingSpawnPoint, determinedSpawn) &&
                !TryRelocateOwnerPin(determinedSpawn))
            {
                EpicLoot.Log("Found an adventure spawn point outside the map circle, but the local " +
                    "player does not own this spawn; leaving it for the owner to place.");
                ParkAndRetry();
                yield break;
            }

            EpicLoot.Log($"Selected Spawn point X {determinedSpawn.x}, Y {determinedSpawn.y}, Z {determinedSpawn.z}");
            spawnPoint.ForceSet(determinedSpawn);
            yield break;
        }

        /// <summary>
        /// Stands the spawner back down without placing anything. Deliberately leaves both
        /// <c>placed</c> and <c>spawnPoint</c> untouched: Update's searchingForSpawn gate then keeps
        /// the component idle, the persistent ZDO survives, and the search runs again on the next
        /// visit (or after the retry delay for a player who stays in the zone). Marking it placed
        /// here would destroy a bounty or treasure map the player has already paid for.
        /// </summary>
        private void ParkAndRetry()
        {
            startedPlacement = false;
            currentUpdates = 0;
            updatesRequired = RetrySearchDelayFrames;
        }

        /// <summary>
        /// Handles a bounty creature prefab that ZNetScene does not have -- typically a creature from a
        /// mod that has since been removed.
        ///
        /// Spawning bails without setting <c>placed</c>, and Update re-enters it on the very next frame,
        /// so before this the spawner retried a lookup that cannot succeed **every frame for as long as
        /// the player stayed near the bounty**. Back off to the same delay a failed location search uses,
        /// and report it once at Error rather than per-frame at Warning -- Warning is invisible at the
        /// default Log Level, which is why this failed silently.
        ///
        /// The spawner is deliberately not marked placed: the player has already paid for this bounty,
        /// and re-adding the missing mod should let it spawn normally.
        /// </summary>
        private void ReportMissingPrefab(string role, string bountyId, string monsterId)
        {
            if (!reportedMissingPrefab)
            {
                reportedMissingPrefab = true;
                EpicLoot.LogError($"Could not find prefab for bounty {role}! BountyID: {bountyId}, " +
                    $"MonsterID: {monsterId}. This bounty cannot spawn until that prefab exists again " +
                    "(is the mod that adds it still installed?). Retrying occasionally.");
            }

            // Delay the next attempt without clearing startedPlacement -- the spawn point is fine, it is
            // only the prefab that is missing, so there is nothing to re-search for.
            currentUpdates = 0;
            updatesRequired = RetrySearchDelayFrames;
        }

        /// <summary>
        /// True when the chosen point falls outside the circle drawn on the map. The pin's
        /// <c>m_worldSize</c> is a diameter (vanilla sets it to range * 2), and MinimapController
        /// assigns MinimapAreaRadius * AreaScale, so the drawn radius is half of that.
        /// </summary>
        private static bool RequiresPinRelocation(Vector3 pinCentre, Vector3 spawn)
        {
            float drawnRadius = AdventureDataManager.Config.TreasureMap.MinimapAreaRadius *
                MinimapController.AreaScale * 0.5f;
            return Utils.DistanceXZ(pinCentre, spawn) > drawnRadius;
        }

        /// <summary>
        /// Moves the owning player's minimap pin to <paramref name="newPosition"/>.
        /// Returns false only when the local player is not the one who bought this spawn - a missing
        /// or already-resolved save record still counts as handled, since parking forever would be
        /// worse than a stale pin.
        /// </summary>
        private bool TryRelocateOwnerPin(Vector3 newPosition)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return false;
            }

            long localPlayerID = player.GetPlayerID();
            AdventureSaveData saveData = player.GetAdventureSaveData();

            bool relocated;
            string description;

            if (isBounty.Get() == true)
            {
                BountyInfo bountyInfo = bounty.Get();
                if (bountyInfo.PlayerID != localPlayerID)
                {
                    return false;
                }

                relocated = saveData != null && saveData.RelocateBounty(bountyInfo.ID, newPosition);
                description = $"bountyID={bountyInfo.ID}";
            }
            else
            {
                TreasureMapChestInfo treasureInfo = treasure.Get();
                if (treasureInfo.PlayerID != localPlayerID)
                {
                    return false;
                }

                relocated = saveData != null &&
                    saveData.RelocateTreasureMap(treasureInfo.Interval, treasureInfo.Biome, newPosition);
                description = $"interval={treasureInfo.Interval} biome={treasureInfo.Biome}";
            }

            if (relocated)
            {
                player.Message(MessageHud.MessageType.Center, "$mod_epicloot_adventure_spawnrelocated");
            }
            else
            {
                EpicLoot.LogWarning("Moved an adventure spawn outside its map circle but could not " +
                    $"update the minimap pin ({description}).");
            }

            return true;
        }
    }
}
