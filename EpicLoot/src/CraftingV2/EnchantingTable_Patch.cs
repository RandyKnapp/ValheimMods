using System;
using EpicLoot_UnityLib;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.CraftingV2
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
    public static class Piece_DropResources_Patch
    {
        public static void Postfix(Piece __instance)
        {
            var table = __instance.GetComponent<EnchantingTable>();
            if (table == null)
                return;

            foreach (EnchantingFeature feature in Enum.GetValues(typeof(EnchantingFeature)))
            {
                if (!table.IsFeatureAvailable(feature) || table.IsFeatureLocked(feature))
                    continue;

                var currentLevel = table.GetFeatureLevel(feature);
                for (var i = 0; i <= currentLevel; ++i)
                {
                    var cost = EnchantingTableUpgrades.GetUpgradeCost(feature, i);
                    foreach (var item in cost)
                    {
                        var itemDrop = Object.Instantiate(item.Item.m_dropPrefab, __instance.transform.position + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>();
                        itemDrop.SetStack(item.Item.m_stack);
                    }
                }
            }
        }
    }
}
