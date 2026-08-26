using Common;
using EpicLoot.Crafting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot.Adventure.Feature
{
    public class SecretStashItemInfo
    {
        public ItemDrop.ItemData Item;
        public Currencies Cost;
        public string ItemID;
        public bool IsGamble;
        public bool GuaranteedRarity;
        public ItemRarity Rarity;

        public SecretStashItemInfo(string itemId, ItemDrop.ItemData item, Currencies cost, bool isGamble = false)
        {
            ItemID = itemId;
            Item = item;
            Cost = cost;
            IsGamble = isGamble;
        }
    }

    public class SecretStashAdventureFeature : AdventureFeature
    {
        public override AdventureFeatureType Type => AdventureFeatureType.SecretStash;
        public override int RefreshInterval => AdventureDataManager.Config.SecretStash.RefreshInterval;

        public List<SecretStashItemInfo> GetSecretStashItems()
        {
            var player = Player.m_localPlayer;
            if (player == null || AdventureDataManager.Config == null)
            {
                return new List<SecretStashItemInfo>();
            }

            var random = GetRandom();
            var results = new List<SecretStashItemInfo>();

            var availableMaterialsList = CollectItems(AdventureDataManager.Config.SecretStash.Materials,
                (x) => x.Item,
                (x) => x.IsMagic() || x.IsMagicCraftingMaterial() || x.IsRunestone());
            var availableMaterials = new MultiValueDictionary<ItemRarity, SecretStashItemInfo>();
            availableMaterialsList.ForEach(x => availableMaterials.Add(x.Item.GetRarity(), x));

            // Roll N times on each rarity list, ACCUMULATING across rarities -- the out-parameter
            // overload replaced the list on every pass (a 0.11.4 regression), so only the last
            // rarity's picks ever reached the stash.
            foreach (ItemRarity section in Enum.GetValues(typeof(ItemRarity)))
            {
                var items = availableMaterials.GetValues(section, true).ToList();
                var rollsPerRarity = AdventureDataManager.Config.SecretStash.RollsPerRarity;
                if ((int)section >= rollsPerRarity.Count)
                {
                    continue;
                }
                var rolls = rollsPerRarity[(int)section];
                RollOnListNTimesUnique(random, items, rolls, results);
            }

            // Remove the results that the player doesn't know about yet
            results.RemoveAll(result =>
            {
                if (result.Item.IsMagicCraftingMaterial() || result.Item.IsRunestone())
                {
                    return !player.m_knownMaterial.Contains(result.Item.m_shared.m_name);
                }
                return false;
            });

            var availableRandomItems = CollectItems(AdventureDataManager.Config.SecretStash.RandomItems,
                (x) => x.Item, (x) => player.m_knownMaterial.Contains(x.m_shared.m_name));

            var randomItems = new List<SecretStashItemInfo>();
            RollOnListNTimes(random, availableRandomItems,
                AdventureDataManager.Config.SecretStash.RandomItemsCount, out randomItems);
            results.AddRange(randomItems);

            var availableOtherItems = CollectItems(AdventureDataManager.Config.SecretStash.OtherItems);
            results.AddRange(availableOtherItems);
            results = SortListByRarity(results);

            return results;
        }

        public List<SecretStashItemInfo> GetForestTokenItems()
        {
            var player = Player.m_localPlayer;
            if (player == null || AdventureDataManager.Config == null)
            {
                return new List<SecretStashItemInfo>();
            }

            var results = CollectItems(AdventureDataManager.Config.TreasureMap.SaleItems,
                (x) => x.Item,
                (x) => player.m_knownMaterial.Contains(x.m_shared.m_name));

            return results;
        }
    }
}
