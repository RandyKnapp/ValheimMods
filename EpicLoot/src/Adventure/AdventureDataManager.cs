using EpicLoot.Adventure.Feature;
using EpicLoot.Biomes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot.Adventure
{
    public static class AdventureDataManager
    {
        public static AdventureDataConfig Config;
        private static readonly Dictionary<string, Sprite> _cachedTrophySprites = new Dictionary<string, Sprite>();

        public static SecretStashAdventureFeature SecretStash;
        public static GambleAdventureFeature Gamble;
        public static TreasureMapsAdventureFeature TreasureMaps;
        public static BountiesAdventureFeature Bounties;
        public static int CheatNumberOfBounties = -1;
        #nullable enable
        public static event Action? OnSetupAdventureData;
        #nullable disable
        public static void Initialize(AdventureDataConfig config)
        {
            Config = config;

            // Code-side default for a config that omits the block (the POCO default is left empty
            // because Newtonsoft appends to pre-initialized collections).
            if (Config?.SecretStash != null && Config.SecretStash.RollsPerRarity.Count == 0)
            {
                Config.SecretStash.RollsPerRarity = new List<int> { 1, 1, 1, 1, 1 };
            }

            // Every load path - first load, embedded-default fallback, file-watcher hot reload and the
            // server->client RPC - routes through here, so this is the only hook tempering costs need.
            TemperMan.ApplyConfig(Config?.Tempering);

            // Surface unresolvable biome names once per load, then hand the deprecated Bounties.Bosses
            // list to the registry, which appends any biome it does not already define.
            ReportUnresolvedBiomes();
            BiomeDataManager.SetLegacyBosses(Config?.Bounties?.Bosses);

            OnSetupAdventureData?.Invoke();

            SecretStash = new SecretStashAdventureFeature();
            Gamble = new GambleAdventureFeature();
            TreasureMaps = new TreasureMapsAdventureFeature();
            Bounties = new BountiesAdventureFeature();

            Config.TreasureMap.UpdateBiomeList();
            EpicLoot.Log($"Updated/setup Adventure Data");
        }

        public static AdventureDataConfig GetCFG()
        {
            return Config;
        }

        /// <summary>
        /// biomedata.json was (re)loaded: the adventure config's biome names may resolve differently
        /// now, so re-check them and rebuild the treasure map biome list. Must not re-send the legacy
        /// Bosses list; the registry re-applies what it was last given on its own rebuild.
        /// </summary>
        public static void OnBiomeDataChanged()
        {
            if (Config == null)
            {
                return;
            }

            ReportUnresolvedBiomes();
            Config.TreasureMap?.UpdateBiomeList();
        }

        private static void ReportUnresolvedBiomes()
        {
            // Resolved purely for the side effect: the registry warns once per unknown name per load.
            foreach (TreasureMapBiomeInfoConfig info in Config?.TreasureMap?.BiomeInfo ?? new List<TreasureMapBiomeInfoConfig>())
            {
                info.GetBiome();
            }

            foreach (BountyTargetConfig target in Config?.Bounties?.Targets ?? new List<BountyTargetConfig>())
            {
                target.GetBiome();
            }
        }

        public static Sprite GetTrophyIconForMonster(string monsterID, bool isGold)
        {
            if (_cachedTrophySprites.TryGetValue(monsterID, out var sprite))
            {
                return sprite;
            }

            if (ZNetScene.instance != null)
            {
                var prefab = ZNetScene.instance.GetPrefab(monsterID);
                if (prefab != null)
                {
                    var characterDrop = prefab.GetComponent<CharacterDrop>();
                    if (characterDrop != null)
                    {
                        // A drop entry can have no prefab assigned (mod-added creatures do this), and a
                        // prefab need not carry an ItemDrop.
                        var drops = characterDrop.m_drops
                            .Where(x => x.m_prefab != null)
                            .Select(x => x.m_prefab.GetComponent<ItemDrop>())
                            .Where(x => x != null);
                        var trophyPrefab = drops.FirstOrDefault(x => x.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy);
                        if (trophyPrefab != null)
                        {
                            sprite = trophyPrefab.m_itemData.GetIcon();
                            if (sprite != null)
                            {
                                _cachedTrophySprites.Add(monsterID, sprite);
                            }
                            return sprite;
                        }
                    }
                }
            }

            var noTrophySpriteName = $"NoTrophy{(isGold ? "Gold" : "Iron")}Sprite";
            if (_cachedTrophySprites.TryGetValue(noTrophySpriteName, out sprite))
            {
                return sprite;
            }

            if (ObjectDB.instance != null)
            {
                var tokenItem = ObjectDB.instance.GetItemPrefab(isGold ? "GoldBountyToken" : "IronBountyToken");
                if (tokenItem != null)
                {
                    sprite = tokenItem.GetComponent<ItemDrop>().m_itemData.GetIcon();
                    if (sprite != null)
                    {
                        _cachedTrophySprites.Add(noTrophySpriteName, sprite);
                    }
                    return sprite;
                }
            }

            return null;
        }

        public static string GetBountyName(BountyInfo bountyInfo)
        {
            return Localization.instance.Localize(string.IsNullOrEmpty(bountyInfo.TargetName) ?
                GetMonsterName(bountyInfo.Target.MonsterID) :
                bountyInfo.TargetName);
        }

        public static string GetMonsterName(string monsterID)
        {
            var monsterPrefab = ZNetScene.instance.GetPrefab(monsterID);
            return monsterPrefab?.GetComponent<Character>()?.m_name ?? monsterID;
        }

        public static void OnZNetStart()
        {
            SecretStash.OnZNetStart();
            Gamble.OnZNetStart();
            TreasureMaps.OnZNetStart();
            Bounties.OnZNetStart();
        }

        public static void OnZNetDestroyed()
        {
            SecretStash.OnZNetDestroyed();
            Gamble.OnZNetDestroyed();
            TreasureMaps.OnZNetDestroyed();
            Bounties.OnZNetDestroyed();

            // The spawn-point cache lives for the client's session rather than the merchant panel's, so
            // this is the only thing that empties it. Its contents are world positions -- carrying them
            // into the next world would hand out points from the wrong map.
            BountyLocationEarlyCache.Reset();
        }

        public static void OnWorldSave()
        {
            SecretStash.OnWorldSave();
            Gamble.OnWorldSave();
            TreasureMaps.OnWorldSave();
            Bounties.OnWorldSave();
        }
    }
}
