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
            var results = new List<TreasureMapItemInfo>();

            var player = Player.m_localPlayer;
            var currentInterval = GetCurrentInterval();
            if (player != null)
            {
                var saveData = player.GetAdventureSaveData();
                foreach (var biome in player.m_knownBiome)
                {
                    var lootTableName = $"TreasureMapChest_{biome}";
                    var lootTableExists = LootRoller.GetLootTable(lootTableName).Count > 0;
                    if (lootTableExists)
                    {
                        var purchased = saveData.HasPurchasedTreasureMap(currentInterval, biome);
                        var cost = AdventureDataManager.Config.TreasureMap.BiomeInfo.Find(x => x.Biome == biome);
                        if (cost.Cost > 0)
                        {
                            results.Add(new TreasureMapItemInfo() {
                                Biome = biome,
                                Interval = currentInterval,
                                Cost = cost.Cost,
                                AlreadyPurchased = purchased
                            });
                        }
                    }
                }
            }

            return results.OrderBy(x => x.Cost).ToList();
        }

        public IEnumerator SpawnTreasureChest(Heightmap.Biome biome, Player player, Action<bool, Vector3> callback)
        {
            player.Message(MessageHud.MessageType.Center, "$mod_epicloot_treasuremap_locatingmsg");
            var saveData = player.GetAdventureSaveData();
            yield return GetRandomPointInBiome(biome, saveData, (success, spawnPoint, normal) =>
            {
                if (success)
                {
                    CreateTreasureChest(biome, player, spawnPoint, normal, saveData, callback);
                }
                else
                {
                    callback?.Invoke(false, Vector3.zero);
                }
            });
        }
        
        private void CreateTreasureChest(Heightmap.Biome biome, Player player, Vector3 spawnPoint, Vector3 normal, AdventureSaveData saveData, Action<bool, Vector3> callback)
        {
            const string treasureChestPrefabName = "piece_chest_wood";
            var treasureChestPrefab = ZNetScene.instance.GetPrefab(treasureChestPrefabName);
            var treasureChestObject = Object.Instantiate(treasureChestPrefab, spawnPoint, Quaternion.FromToRotation(Vector3.up, normal));
            var treasureChest = treasureChestObject.AddComponent<TreasureMapChest>();
            treasureChest.Setup(player, biome, GetCurrentInterval());

            var offset2 = UnityEngine.Random.insideUnitCircle * (AdventureDataManager.Config.TreasureMap.MinimapAreaRadius * 0.8f);
            var offset = new Vector3(offset2.x, 0, offset2.y);
            saveData.PurchasedTreasureMap(GetCurrentInterval(), biome, spawnPoint, offset);
            Minimap.instance.ShowPointOnMap(spawnPoint + offset);

            callback?.Invoke(true, spawnPoint);
        }
    }
}
