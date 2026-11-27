using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void SpawnMagicCraftingMaterials(Terminal.ConsoleEventArgs args)
    {
        if (Player.m_localPlayer == null) return;

        Transform transform = Player.m_localPlayer.transform;

        foreach (string type in EpicLoot.MagicMaterials)
        {
            foreach (ItemRarity rarity in Rarities.All)
            {
                string assetName = $"{type}{rarity}";
                GameObject itemPrefab = PrefabManager.Instance.GetPrefab(assetName);
                if (itemPrefab == null)
                {
                    EpicLoot.LogWarning($"magicmats: no prefab named {assetName}, skipping it.");
                    continue;
                }

                ItemDrop itemDrop = UnityEngine.Object.Instantiate(itemPrefab,
                    transform.position + transform.forward * 2f + Vector3.up,
                    Quaternion.identity).GetComponent<ItemDrop>();

                // Never zero (EtchedRunestone stacks to 1): vanilla Inventory.CanAddItem accepts a stack-0
                // drop that AddItem cannot place, so with a full inventory Player.AutoPickup retries it
                // every frame. SetStack rather than m_stack, so the ZDO carries the count too.
                itemDrop.SetStack(Mathf.Max(1, itemDrop.m_itemData.m_shared.m_maxStackSize / 2));
            }
        }
    }
}