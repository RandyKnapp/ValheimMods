using System;
using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void SpawnMagicCraftingMaterials(Terminal.ConsoleEventArgs args)
    {
        foreach (string type in EpicLoot.MagicMaterials)
        {
            foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
            {
                string assetName = $"{type}{rarity}";
                GameObject itemPrefab = PrefabManager.Instance.GetPrefab(assetName);
                Transform transform = Player.m_localPlayer.transform;
                ItemDrop itemDrop = UnityEngine.Object.Instantiate(itemPrefab,
                    transform.position + transform.forward * 2f + Vector3.up,
                    Quaternion.identity).GetComponent<ItemDrop>();
                itemDrop.m_itemData.m_stack = itemDrop.m_itemData.m_shared.m_maxStackSize / 2;
            }
        }
    }
}