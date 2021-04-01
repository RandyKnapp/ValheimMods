using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using EpicLoot.Crafting;

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

            // Roll N times on each rarity list
            foreach (ItemRarity section in Enum.GetValues(typeof(ItemRarity)))
            {
                var items = availableMaterials.GetValues(section, true).ToList();
                var rolls = AdventureDataManager.Config.SecretStash.RollsPerRarity[(int)section];
                RollOnListNTimes(random, items, rolls, results);
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

            var availableOtherItems = CollectItems(AdventureDataManager.Config.SecretStash.OtherItems);
            results.AddRange(availableOtherItems);

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
