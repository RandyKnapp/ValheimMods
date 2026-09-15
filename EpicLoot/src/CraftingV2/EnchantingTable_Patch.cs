using EpicLoot.Config;
using EpicLoot_UnityLib;
using HarmonyLib;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.CraftingV2
{
    /// <summary>
    /// Refunds the feature unlock and upgrade costs paid into an enchanting table when the piece is
    /// destroyed, the same way vanilla refunds the piece's own build resources.
    /// </summary>
    [HarmonyPatch(typeof(Piece), nameof(Piece.DropResources))]
    public static class Piece_DropResources_Patch
    {
        public static void Postfix(Piece __instance, HitData hitData)
        {
            var table = __instance.GetComponent<EnchantingTable>();
            if (table == null)
            {
                return;
            }

            if (!ELConfig.EnchantingTableUpgradesActive.Value)
            {
                // Do not drop upgrade costs when disabled
                return;
            }

            // Vanilla flags every drop of a piece that was placed with the no-cost cheat.
            var nview = __instance.GetComponent<ZNetView>();
            bool cheated = nview != null && nview.IsValid() &&
                nview.GetZDO().GetBool(ZDOVars.s_cheated) && !PlayerProfile.s_bypassCheatChecks;
            Vector3 dropPosition = __instance.transform.position +
                Vector3.up * __instance.m_returnResourceHeightOffset;

            foreach (EnchantingFeature feature in Enum.GetValues(typeof(EnchantingFeature)))
            {
                if (!table.IsFeatureAvailable(feature) || table.IsFeatureLocked(feature))
                {
                    continue;
                }

                // A feature starts at its configured default level for free, so only the unlock and
                // upgrade steps above that level were ever paid for.
                int firstPaidLevel = Mathf.Max(0, EnchantingTable.GetDefaultFeatureLevel(feature) + 1);
                int currentLevel = table.GetFeatureLevel(feature);
                for (int level = firstPaidLevel; level <= currentLevel; ++level)
                {
                    foreach (InventoryItemListElement cost in EnchantingTableUpgrades.GetUpgradeCost(feature, level))
                    {
                        DropCostItem(cost.Item, dropPosition, hitData, cheated);
                    }
                }
            }
        }

        // Mirrors the per-requirement loop in Piece.DropResources. ItemDrop.SetStack clamps to the
        // item's max stack size, so dropping each cost as a single ItemDrop silently lost everything
        // above that limit: 200 Cloudberries came back as one stack of 50, 36 Surtling Cores as 10.
        private static void DropCostItem(ItemDrop.ItemData costItem, Vector3 position, HitData hitData, bool cheated)
        {
            GameObject dropPrefab = costItem.m_dropPrefab;
            if (dropPrefab == null)
            {
                return;
            }

            int dropCount = costItem.m_stack;
            var prefabItemDrop = dropPrefab.GetComponent<ItemDrop>();
            if (Game.instance != null && prefabItemDrop != null)
            {
                // Same conversion vanilla applies to the piece's own resources, e.g. wood burned by
                // cinder fire drops as coal.
                dropPrefab = Game.instance.CheckDropConversion(hitData, prefabItemDrop, dropPrefab, ref dropCount);
            }

            while (dropCount > 0)
            {
                var itemDrop = Object.Instantiate(dropPrefab, position, Quaternion.identity).GetComponent<ItemDrop>();
                itemDrop.SetStack(Mathf.Min(dropCount, itemDrop.m_itemData.m_shared.m_maxStackSize));
                ItemDrop.OnCreateNew(itemDrop, cheated);
                // SetStack is a no-op on a drop this client does not own, leaving the prefab's stack of
                // one behind; never let a zero-stack prefab turn this into an endless loop.
                dropCount -= Mathf.Max(1, itemDrop.m_itemData.m_stack);
            }
        }
    }
}
