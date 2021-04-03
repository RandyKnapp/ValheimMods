using System.Collections.Generic;
using System.Linq;
using EpicLoot.Adventure.Feature;
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

        public static void Initialize(AdventureDataConfig config)
        {
            Config = config;

            SecretStash = new SecretStashAdventureFeature();
            Gamble = new GambleAdventureFeature();
            TreasureMaps = new TreasureMapsAdventureFeature();
            Bounties = new BountiesAdventureFeature();
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
                        var drops = characterDrop.m_drops.Select(x => x.m_prefab.GetComponent<ItemDrop>());
                        var trophyPrefab = drops.FirstOrDefault(x => x.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie);
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
            return string.IsNullOrEmpty(bountyInfo.TargetName) ? GetMonsterName(bountyInfo.Target.MonsterID) : bountyInfo.TargetName;
        }

        public static string GetMonsterName(string monsterID)
        {
            var monsterPrefab = ZNetScene.instance.GetPrefab(monsterID);
            return monsterPrefab?.GetComponent<Character>()?.m_name ?? monsterID;
        }
    }
}
