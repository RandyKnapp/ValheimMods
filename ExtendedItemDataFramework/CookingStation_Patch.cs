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
            var itemPrefab = ObjectDB.instance.GetItemPrefab(name);
            var itemDrop = itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                itemDrop.m_itemData = new ExtendedItemData(itemDrop.m_itemData);
            }
            var pos = __instance.m_spawnPoint.position;
            var quaternion = Quaternion.Euler(0.0f, Random.Range(0, 360), 0.0f);
            var position = pos;
            var rotation = quaternion;
            var newObject = Object.Instantiate(itemPrefab, position, rotation);
            newObject.GetComponent<Rigidbody>().velocity = Vector3.up * __instance.m_spawnForce;
            __instance.m_pickEffector.Create(pos, Quaternion.identity);
            return false;
        }
    }
}
