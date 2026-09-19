using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void SpawnMagicCraftingMaterials(Terminal.ConsoleEventArgs args)
    {
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

                // Half a stack, but never zero. EtchedRunestone has m_maxStackSize 1, so the integer
                // division spawned it at stack 0 -- and nothing downstream normalizes that. Vanilla
                // Inventory.CanAddItem then answers true for such a drop (freeStackSpace 0 + free
                // cells 0 >= stack 0) while AddItem has nowhere to put it, so with a full inventory
                // Player.AutoPickup retries it every frame and the game logs "Trying to add item to
                // occupied slot -1, -1" forever. Picked up successfully it is just as bad: a stack-0
                // item counts as zero everywhere, so the material is there but unusable.
                //
                // SetStack rather than assigning m_stack: it writes the ZDO as well, so the count
                // survives the drop being saved or replicated instead of only living on this client.
                itemDrop.SetStack(Mathf.Max(1, itemDrop.m_itemData.m_shared.m_maxStackSize / 2));
            }
        }
    }
}