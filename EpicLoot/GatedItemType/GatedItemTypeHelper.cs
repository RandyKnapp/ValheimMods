using System.Collections.Generic;
using UnityEngine;

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
            foreach (var info in config.ItemInfo)
            {
                ItemInfos.Add(info);
                //Debug.Log($"Adding ItemTypeInfo: {info.Type}, fallback={info.Fallback}, items={string.Join(",", info.Items)}");
                foreach (var itemID in info.Items)
                {
                    ItemInfoByID.Add(itemID, info);
                }
            }
        }

        public static string GetGatedItemID(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
            {
                Debug.LogError($"Tried to get gated itemID with null or empty itemID!");
                return null;
            }

            var mode = EpicLoot.GetGatedItemTypeMode();
            if (mode == GatedItemTypeMode.Unlimited)
            {
                return itemID;
            }

            var player = Player.m_localPlayer;
            if (player == null)
            {
                Debug.LogError($"Tried to get gated itemID ({itemID}) with null player!");
                return null;
            }

            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
            {
                Debug.LogError($"Tried to get gated itemID ({itemID}) but ObjectDB is not initialized!");
                return null;
            }

            if (!ItemInfoByID.TryGetValue(itemID, out var info))
            {
                Debug.LogWarning($"Tried to get gated itemID from itemID ({itemID}), but no data exists for it in iteminfo.json. Returning itemID.");
                return itemID;
            }

            var itemName = GetItemName(itemID);
            if (string.IsNullOrEmpty(itemName))
            {
                return null;
            }

            while (CheckIfItemNeedsGate(player, mode, itemName))
            {
                Debug.Log("Yes...");
                var index = info.Items.IndexOf(itemID);
                if (index < 0)
                {
                    Debug.LogError($"Something has gone completely wrong, the ItemInfo ({info.Type}) did not contain the itemID ({itemID}).");
                    return null;
                }
                if (index == 0)
                {
                    Debug.Log($"Reached end of gated list. Fallback is ({info.Fallback}), returning ({(string.IsNullOrEmpty(info.Fallback) ? itemID : info.Fallback)}){(string.IsNullOrEmpty(info.Fallback) ? "" : " (fallback)")}");
                    return string.IsNullOrEmpty(info.Fallback) ? itemID : info.Fallback;
                }

                itemID = info.Items[index - 1];
                itemName = GetItemName(itemID);
                Debug.Log($"Next lower tier item is ({itemID})");
            }

            Debug.Log($"No, return ({itemID})");
            return itemID;
        }

        private static string GetItemName(string itemID)
        {
            var itemPrefab = ObjectDB.instance.GetItemPrefab(itemID);
            if (itemPrefab == null)
            {
                Debug.LogError($"Tried to get gated itemID ({itemID}) but there is no prefab with that ID!");
                return null;
            }

            var itemDrop = itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                Debug.LogError($"Tried to get gated itemID ({itemID}) but its prefab has no ItemDrop component!");
                return null;
            }

            var item = itemDrop.m_itemData;
            return item.m_shared.m_name;
        }

        private static bool CheckIfItemNeedsGate(Player player, GatedItemTypeMode mode, string itemName)
        {
            Debug.Log($"Checking if item ({itemName}) needs gating...");
            switch (mode)
            {
                case GatedItemTypeMode.MustKnowRecipe: return !player.IsRecipeKnown(itemName);
                case GatedItemTypeMode.MustHaveCrafted: return !player.m_knownMaterial.Contains(itemName);
                default: return false;
            }
        }
    }
}
