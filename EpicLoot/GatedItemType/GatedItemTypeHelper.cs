using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using BepInEx;
using Common;
using EpicLoot.Adventure.Feature;

namespace EpicLoot.GatedItemType
{
    public enum GatedItemTypeMode
    {
        Unlimited,
        BossKillUnlocksCurrentBiomeItems,
        BossKillUnlocksNextBiomeItems,
        PlayerMustKnowRecipe,
        PlayerMustHaveCraftedItem
    }

    public static class GatedItemTypeHelper
    {
        public static readonly List<ItemTypeInfo> ItemInfos = new List<ItemTypeInfo>();
        public static readonly Dictionary<string, ItemTypeInfo> ItemInfoByID = new Dictionary<string, ItemTypeInfo>();
        public static readonly Dictionary<string, List<string>> ItemsPerBoss = new Dictionary<string, List<string>>();
        public static readonly Dictionary<string, string> BossPerItem = new Dictionary<string, string>();

        public static void Initialize(ItemInfoConfig config)
        {
            ItemInfos.Clear();
            ItemInfoByID.Clear();
            ItemsPerBoss.Clear();
            BossPerItem.Clear();
            foreach (var info in config.ItemInfo)
            {
                ItemInfos.Add(info);
               
                foreach (var itemID in info.Items)
                {
                    ItemInfoByID.Add(itemID, info);
                }

                foreach (var entry in info.ItemsByBoss)
                {
                    if (ItemsPerBoss.ContainsKey(entry.Key))
                        ItemsPerBoss[entry.Key].AddRange(entry.Value);
                    else
                        ItemsPerBoss.Add(entry.Key, entry.Value.ToList());

                    foreach (var itemID in entry.Value)
                    {
                        BossPerItem.Add(itemID, entry.Key);
                    }
                }
            }
        }

        public static string GetGatedItemID(string itemID)
        {
            return GetGatedItemID(itemID, EpicLoot.GetGatedItemTypeMode());
        }

        public static string GetGatedFallbackItem(string infoType, GatedItemTypeMode mode, string originalItemID, List<string> usedTypes = null)
        {
            ItemInfos.TryFind(x => x.Type.Equals(originalItemID), out var originalInfo);
            var returnItem = originalItemID;

            if (usedTypes == null)
            {
                usedTypes = new List<string>();
            }

            if (!ItemInfos.TryFind(x => x.Type.Equals(infoType), out var info)) return returnItem;
            if (usedTypes.Contains(info.Type)) return returnItem;
            
            usedTypes.Add(info.Type);
            var fallbackItem = GetItemFromCategory(info.Type, mode, usedTypes);
            if (!fallbackItem.IsNullOrWhiteSpace())
            {
                returnItem = fallbackItem;
            }

            return returnItem;
        }

        public static string GetGatedItemID(string itemID, GatedItemTypeMode mode, List<string> usedTypes = null)
        {
            if (string.IsNullOrEmpty(itemID))
            {
                EpicLoot.LogError($"Tried to get gated itemID with null or empty itemID!");
                return null;
            }

            if (mode == GatedItemTypeMode.Unlimited)
            {
                return itemID;
            }

            if (!EpicLoot.IsObjectDBReady())
            {
                EpicLoot.LogError($"Tried to get gated itemID ({itemID}) but ObjectDB is not initialized!");
                return null;
            }

            //Gets Info Item for specific itemId
            if (!ItemInfoByID.TryGetValue(itemID, out var info))
            {
                return itemID;
            }
            
            var itemName = GetItemName(itemID);
            if (string.IsNullOrEmpty(itemName))
            {
                return null;
            }

            while (CheckIfItemNeedsGate(mode, itemID, itemName))
            {
                //EpicLoot.Log("Yes...");

                var index = info.Items.IndexOf(itemID);
                if (index < 0)
                {
                    // Items list is empty, no need to gate any items from of this type
                    return itemID;
                }
                if (index == 0)
                {
                    return string.IsNullOrEmpty(info.Fallback) ? itemID : GetGatedFallbackItem(info.Fallback, mode, itemID, usedTypes);
                }

                itemID = info.Items[index - 1];
                itemName = GetItemName(itemID);
                //EpicLoot.Log($"Next lower tier item is ({itemID} - {itemName})");
            }

            return itemID;
        }

        private static string GetItemName(string itemID)
        {
            var itemPrefab = ObjectDB.instance.GetItemPrefab(itemID);
            if (itemPrefab == null)
            {
                EpicLoot.LogError($"Tried to get gated itemID ({itemID}) but there is no prefab with that ID!");
                return null;
            }

            var itemDrop = itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                EpicLoot.LogError($"Tried to get gated itemID ({itemID}) but its prefab has no ItemDrop component!");
                return null;
            }

            var item = itemDrop.m_itemData;
            return item.m_shared.m_name;
        }

        private static bool CheckIfItemNeedsGate(GatedItemTypeMode mode, string itemID, string itemName)
        {
            if (!BossPerItem.ContainsKey(itemID))
            {
                EpicLoot.LogWarning($"Item ({itemID}) was not registered in iteminfo.json with any particular boss");
                return false;
            }

            var bossKeyForItem = BossPerItem[itemID];
            var prevBossKey = Bosses.GetPrevBossKey(bossKeyForItem);
            //EpicLoot.Log($"Checking if item ({itemID}) needs gating (boss: {bossKeyForItem}, prev boss: {prevBossKey}");
            switch (mode)
            {
                case GatedItemTypeMode.BossKillUnlocksCurrentBiomeItems:    return !ZoneSystem.instance.GetGlobalKey(bossKeyForItem);
                case GatedItemTypeMode.BossKillUnlocksNextBiomeItems:       return !(string.IsNullOrEmpty(prevBossKey) || ZoneSystem.instance.GetGlobalKey(prevBossKey));
                case GatedItemTypeMode.PlayerMustKnowRecipe:                return Player.m_localPlayer != null && !Player.m_localPlayer.IsRecipeKnown(itemName);
                case GatedItemTypeMode.PlayerMustHaveCraftedItem:           return Player.m_localPlayer != null && !Player.m_localPlayer.m_knownMaterial.Contains(itemName);
                default: return false;
            }
        }

        public static string GetItemFromCategory(string itemCategory, GatedItemTypeMode mode, List<string> usedTypes = null)
        {
            var itemInfo = ItemInfos.FirstOrDefault(x => x.Type == itemCategory);

            return itemInfo == null ? "" : GetGatedItemID(itemInfo.Items[itemInfo.Items.Count - 1], mode, usedTypes);
        }
    }
}
