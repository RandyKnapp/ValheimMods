using System.Collections.Generic;
using System.Linq;
using EpicLoot.PlayerKnown;

namespace EpicLoot.GatedItemType
{
    public enum GatedItemTypeMode
    {
        Unlimited,
        MustKnowRecipe,
        MustHaveCrafted
    }

    public static class GatedItemTypeHelper
    {
        public static readonly List<ItemTypeInfo> ItemInfos = new List<ItemTypeInfo>();
        public static readonly Dictionary<string, ItemTypeInfo> ItemInfoByID = new Dictionary<string, ItemTypeInfo>();

        public static void Initialize(ItemInfoConfig config)
        {
            ItemInfos.Clear();
            ItemInfoByID.Clear();
            foreach (var info in config.ItemInfo)
            {
                ItemInfos.Add(info);
                //EpicLoot.Log($"Adding ItemTypeInfo: {info.Type}, fallback={info.Fallback}, items={string.Join(",", info.Items)}");
                foreach (var itemID in info.Items)
                {
                    ItemInfoByID.Add(itemID, info);
                }
            }
        }

        public static string GetGatedItemID(string itemID)
        {
            return GetGatedItemID(itemID, EpicLoot.GetGatedItemTypeMode());
        }

        public static string GetGatedItemID(string itemID, GatedItemTypeMode mode)
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

            if (!ItemInfoByID.TryGetValue(itemID, out var info))
            {
                return itemID;
            }

            var itemName = GetItemName(itemID);
            if (string.IsNullOrEmpty(itemName))
            {
                return null;
            }

            while (CheckIfItemNeedsGate(mode, itemName))
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
                    //EpicLoot.Log($"Reached end of gated list. Fallback is ({info.Fallback}), returning ({(string.IsNullOrEmpty(info.Fallback) ? itemID : info.Fallback)}){(string.IsNullOrEmpty(info.Fallback) ? "" : " (fallback)")}");
                    return string.IsNullOrEmpty(info.Fallback) ? itemID : info.Fallback;
                }

                itemID = info.Items[index - 1];
                itemName = GetItemName(itemID);
                //EpicLoot.Log($"Next lower tier item is ({itemID})");
            }

            //EpicLoot.Log($"No, return ({itemID})");
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

        private static bool CheckIfItemNeedsGate(GatedItemTypeMode mode, string itemName)
        {
            //EpicLoot.Log($"Checking if item ({itemName}) needs gating...");
            switch (mode)
            {
                case GatedItemTypeMode.MustKnowRecipe: return !PlayerKnownManager.IsRecipeKnown(itemName);
                case GatedItemTypeMode.MustHaveCrafted: return !PlayerKnownManager.IsItemKnown(itemName);
                default: return false;
            }
        }

        public static string GetItemFromCategory(string itemCategory, GatedItemTypeMode mode)
        {
            var itemInfo = ItemInfos.FirstOrDefault(x => x.Type == itemCategory);
            if (itemInfo == null)
            {
                EpicLoot.LogWarning($"Could not find item info category: {itemCategory}");
                return "";
            }

            return GetGatedItemID(itemInfo.Items[itemInfo.Items.Count - 1], mode);
        }
    }
}
