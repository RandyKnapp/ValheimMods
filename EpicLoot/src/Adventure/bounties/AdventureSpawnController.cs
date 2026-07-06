using EpicLoot.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.Adventure
{
    internal class AdventureSpawnController : MonoBehaviour
    {
        protected ZNetView zNetView;
        private BountyInfoZNetProperty bounty { get; set; }
        private TreasureMapChestInfoZNetProperty treasure { get; set; }
        private BoolZNetProperty placed { get; set; }
        private BoolZNetProperty searchingForSpawn { get; set; }
        private Vector3ZNetProperty spawnPoint { get; set; }

        private BoolZNetProperty isBounty { get; set; }

        private int currentUpdates = 0;
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

            if (currentUpdates < 300)
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
                    StartCoroutine(DeterminespawnPoint(bounty.Get().Position, bounty.Get().Biome));
                }

                if (treasure.Get().PlayerID != 0)
                {
                    StartCoroutine(DeterminespawnPoint(treasure.Get().Position, treasure.Get().Biome, true));
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
                EpicLoot.LogWarning($"Could not find prefab for bounty target! BountyID: " +
                    $"{bounty.ID}, MonsterID: {bounty.Target.MonsterID}");
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
                        EpicLoot.LogError($"Could not find prefab for bounty add! BountyID: " +
                            $"{bounty.ID}, MonsterID: {addConfig.MonsterID}");
                        return;
                    }
                    prefabs.Add(prefab);
                }
            }

            for (var index = 0; index < prefabs.Count; index++)
            {
                var prefab = prefabs[index];
                var isAdd = index > 0;

                var creature = UnityEngine.Object.Instantiate(prefab, point, Quaternion.identity);
                var bountyTarget = creature.AddComponent<BountyTarget>();
                bountyTarget.Initialize(bounty, prefab.name, isAdd);

                var randomSpacing = UnityEngine.Random.insideUnitSphere * 4f;
                point += randomSpacing;
                ZoneSystem.instance.FindFloor(point, out var floorHeight);
                point.y = floorHeight;
            }

            placed.ForceSet(true);
        }

        private void SpawnChest(TreasureMapChestInfo treasure)
        {
            Vector3 point = spawnPoint.Get();

            const string treasureChestPrefabName = "piece_chest_wood";
            var treasureChestPrefab = ZNetScene.instance.GetPrefab(treasureChestPrefabName);
            ZoneSystem.instance.GetGroundData(
                ref point, out var normal, out var foundBiome, out var biomeArea, out var hmap);
            var treasureChestObject = UnityEngine.Object.Instantiate(
                treasureChestPrefab, point, Quaternion.FromToRotation(Vector3.up, normal));
            var treasureChest = treasureChestObject.AddComponent<TreasureMapChest>();
            Piece tpiece = treasureChestObject.GetComponent<Piece>();

            // Prevent the wildlife from attacking the chest and giving away its location
            tpiece.m_primaryTarget = false;
            tpiece.m_randomTarget = false;
            tpiece.m_targetNonPlayerBuilt = false;
            treasureChest.Setup(treasure.PlayerID, treasure.Biome, treasure.Interval);
            placed.ForceSet(true);
        }

        internal IEnumerator DeterminespawnPoint(Vector3 startingSpawnPoint,
            Heightmap.Biome biome, bool allowWaterSpawn = false)
        {
            yield return new WaitForSeconds(5);

            while (!ZNetScene.instance.IsAreaReady(startingSpawnPoint))
            {
                yield return new WaitForSeconds(1f);
            }

            // TODO: If bounties get their own minimap area radius config this must choose the correct one
            float radius = AdventureDataManager.Config.TreasureMap.MinimapAreaRadius;
            Vector3 determinedSpawn = startingSpawnPoint;
            int spawnLocationAttempts = 0;

            // Attempt to find a spawn point, valid height must be selected
            while (spawnLocationAttempts < 100)
            {
                var offset = UnityEngine.Random.insideUnitCircle * (radius * 0.8f);
                determinedSpawn = startingSpawnPoint + new Vector3(offset.x, 0, offset.y);

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

                // Prevents spawning in a body of water
                if ((biome != Heightmap.Biome.Ocean || !allowWaterSpawn) &&
                    determinedSpawn.y < 29)
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

                break;
            }

            if (determinedSpawn.y >= StartingHeight - 1f)
            {
                determinedSpawn.y = 400f;
            }

            EpicLoot.Log($"Selected Spawn point X {determinedSpawn.x}, Y {determinedSpawn.y}, Z {determinedSpawn.z}");
            spawnPoint.ForceSet(determinedSpawn);
            yield break;
        }
    }
}
