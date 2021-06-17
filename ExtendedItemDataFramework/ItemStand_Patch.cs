using HarmonyLib;
using UnityEngine;

namespace ExtendedItemDataFramework
{
    [HarmonyPatch(typeof(ItemStand), "DropItem")]
    public static class ItemStand_Patch
    {
        public static bool Prefix(ItemStand __instance)
        {
            if (!__instance.HaveAttachment())
            {
                return false;
            }
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(__instance.m_nview.GetZDO().GetString("item"));
            if (itemPrefab != null)
            {
                GameObject gameObject = Object.Instantiate(itemPrefab, __instance.m_dropSpawnPoint.position, __instance.m_dropSpawnPoint.rotation);
                ItemDrop itemDrop = gameObject.GetComponent<ItemDrop>();
                ItemDrop.LoadFromZDO(itemDrop.m_itemData, __instance.m_nview.GetZDO());
                itemDrop.m_itemData = new ExtendedItemData(
                    itemDrop.m_itemData,
                    itemDrop.m_itemData.m_stack,
                    itemDrop.m_itemData.m_durability,
                    new Vector2i(),
                    false,
                    itemDrop.m_itemData.m_quality,
                    itemDrop.m_itemData.m_variant,
                    itemDrop.m_itemData.m_crafterID,
                    itemDrop.m_itemData.m_crafterName);
                itemDrop.Save();
                gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
                __instance.m_effects.Create(__instance.m_dropSpawnPoint.position, Quaternion.identity);
            }
            __instance.m_nview.GetZDO().Set("item", "");
            __instance.m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", "", 0);
            return false;
        }
    }
}
