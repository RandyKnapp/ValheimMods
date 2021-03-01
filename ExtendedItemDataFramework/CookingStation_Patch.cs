using HarmonyLib;
using UnityEngine;

namespace ExtendedItemDataFramework
{
    //public void SpawnItem(string name)
    [HarmonyPatch(typeof(CookingStation), "SpawnItem")]
    class CookingStation_SpawnItem_Patch
    {
        public static bool Prefix(CookingStation __instance, string name)
        {
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
            var itemDrop = itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                itemDrop.m_itemData = new ExtendedItemData(itemDrop.m_itemData);
            }
            Vector3 pos = __instance.transform.position + Vector3.up * __instance.m_spawnOffset;
            Quaternion quaternion = Quaternion.Euler(0.0f, Random.Range(0, 360), 0.0f);
            Vector3 position = pos;
            Quaternion rotation = quaternion;
            Object.Instantiate<GameObject>(itemPrefab, position, rotation).GetComponent<Rigidbody>().velocity = Vector3.up * __instance.m_spawnForce;
            __instance.m_pickEffector.Create(pos, Quaternion.identity);
            return true;
        }
    }
}
