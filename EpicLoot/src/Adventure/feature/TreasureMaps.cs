using EpicLoot.Biomes;
using Jotunn.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.Adventure.Feature
{
    public class TreasureMapItemInfo
    {
        public Heightmap.Biome Biome;
        public int Interval;
        public int Cost;
        public bool AlreadyPurchased;
    }

    public class TreasureMapsAdventureFeature : AdventureFeature
    {
        public override AdventureFeatureType Type => AdventureFeatureType.TreasureMaps;
        public override int RefreshInterval => AdventureDataManager.Config.TreasureMap.RefreshInterval;

        public List<TreasureMapItemInfo> GetTreasureMaps()
        {
            List<TreasureMapItemInfo> results = new List<TreasureMapItemInfo>();
            if (Player.m_localPlayer == null)
            {
                return results;
            }

            int currentInterval = GetCurrentInterval();

            AdventureSaveData saveData = Player.m_localPlayer.GetAdventureSaveData();
            foreach (Heightmap.Biome biome in Player.m_localPlayer.m_knownBiome)
            {
                string lootTableName = $"TreasureMapChest_{BiomeDataManager.GetName(biome)}";
                bool lootTableExists = LootRoller.GetLootTable(lootTableName).Count > 0;

                if (!lootTableExists)
                {
                    continue;
                }

                bool purchased = saveData.HasPurchasedTreasureMap(currentInterval, biome);
                TreasureMapBiomeInfoConfig cost = AdventureDataManager.Config.TreasureMap.BiomeInfo.Find(x => x.GetBiome() == biome);
                if (cost != null && cost.Cost > 0)
                {
                    results.Add(new TreasureMapItemInfo()
                    {
                        Biome = biome,
                        Interval = currentInterval,
                        Cost = cost.Cost,
                        AlreadyPurchased = purchased
                    });
                }
            }

            return results.OrderBy(x => x.Cost).ToList();
        }

        public IEnumerator SpawnTreasureChest(Heightmap.Biome biome, Player player, int price, Action<int, bool, Vector3> callback)
        {
            player.Message(MessageHud.MessageType.Center, "$mod_epicloot_treasuremap_locatingmsg");
            AdventureSaveData saveData = player.GetAdventureSaveData();
            yield return BountyLocationEarlyCache.TryGetBiomePoint(biome, saveData, (success, spawnPoint) =>
            {
                // Only report success once the spawner actually exists. Reporting it unconditionally
                // charged the player for a map that CreateTreasureSpawner had refused to create.
                if (success && CreateTreasureSpawner(biome, spawnPoint, saveData))
                {
                    callback?.Invoke(price, true, spawnPoint);
                }
                else
                {
                    callback?.Invoke(0, false, Vector3.zero);
                }
            });
        }

        /// <summary>
        /// Records the purchase and spawns the chest's spawner. Returns false when nothing was
        /// created, so the caller can leave the player's coins alone.
        /// </summary>
        private bool CreateTreasureSpawner(Heightmap.Biome biome,  Vector3 spawnPoint, AdventureSaveData saveData)
        {
            TreasureMapChestInfo treasure_details = new TreasureMapChestInfo()
            {
                Biome = biome,
                Interval = GetCurrentInterval(),
                Position = spawnPoint,
                PlayerID = Player.m_localPlayer.GetPlayerID()
            };

            // Record the purchase FIRST and honor its refusal (duplicate interval/biome):
            // spawning regardless used to leave orphan treasure chests with no save record.
            if (!saveData.PurchasedTreasureMap(treasure_details))
            {
                EpicLoot.LogWarningForce($"Treasure map purchase for {biome} was refused by the save data; no chest spawned.");
                return false;
            }

            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            GameObject gameObject = PrefabManager.Instance.GetPrefab("EL_SpawnController");
            GameObject created_go = Object.Instantiate(gameObject, spawnPoint, rotation);
            AdventureSpawnController asc = created_go.GetComponent<AdventureSpawnController>();
            asc.SetTreasure(treasure_details);

            Vector2 offset2 = UnityEngine.Random.insideUnitCircle *
                (AdventureDataManager.Config.TreasureMap.MinimapAreaRadius * 0.8f);
            Vector3 offset = new Vector3(offset2.x, 0, offset2.y);

            Minimap.instance.ShowPointOnMap(spawnPoint + offset);
            return true;
        }
    }
}
