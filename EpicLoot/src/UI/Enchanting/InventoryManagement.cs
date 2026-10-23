using EpicLoot;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot_UnityLib;

public class InventoryManagement
{
    static InventoryManagement() { }
    private InventoryManagement() { }
    private static readonly InventoryManagement _instance = new InventoryManagement();

    public static InventoryManagement Instance
    {
        get => _instance;
    }

    // The storage of the enchanting table whose window is open; null otherwise.
    internal static EnchantingTableStash ActiveStash;

    private void SendMessage(string message, int amount, Sprite icon)
    {
        Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,
            message, amount, icon);
    }

    private Inventory GetInventory()
    {
        Player player = Player.m_localPlayer;

        if (player != null)
        {
            return player.GetInventory();
        }

        return null;
    }

    // The four read paths below all consult external inventory providers registered through
    // EpicLoot.API.RegisterInventoryProvider (nearby containers, backpacks, remote stashes). With none
    // registered the provider calls short-circuit on a bool and behavior is identical to the player's
    // own inventory.
    public List<ItemDrop.ItemData> GetAllItems()
    {
        Inventory inventory = GetInventory();
        if (inventory == null)
        {
            return null;
        }

        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        if (!API.AnyInventoryProviders && ActiveStash == null)
        {
            return items;
        }

        // Inventory.GetAllItems hands back the live m_inventory list, so appending to it would inject
        // container items into the player's actual inventory. Copy first.
        List<ItemDrop.ItemData> combined = new List<ItemDrop.ItemData>(items);
        if (ActiveStash != null)
        {
            combined.AddRange(ActiveStash.GetSnapshotItems());
        }

        API.AppendProviderItems(combined);
        return combined;
    }

    // What an action that changes an item in place (enchant, augment, etch, a reducing extract, a set strip,
    // disenchant) may target: GetAllItems less the providers listed under Spend-Only Storage Mods. Those
    // never save such a change, so the item would revert when their storage reloads, after the materials
    // were spent. Consuming actions keep using GetAllItems, since a removal is saved.
    public List<ItemDrop.ItemData> GetEditableItems()
    {
        Inventory inventory = GetInventory();
        if (inventory == null)
        {
            return null;
        }

        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        if (!API.AnyInventoryProviders)
        {
            return items;
        }

        List<ItemDrop.ItemData> combined = new List<ItemDrop.ItemData>(items);
        API.AppendProviderItems(combined, forEditing: true);
        return combined;
    }

    // Re-checked right before an in-place change is paid for: the item must still be somewhere that change
    // can be saved. Providers are asked again, so a chest that reloaded since the list was built (replacing
    // the instance) fails here instead of taking the materials.
    public bool IsEditable(ItemDrop.ItemData item)
    {
        if (item == null)
        {
            return false;
        }

        Inventory inventory = GetInventory();
        if (inventory != null && inventory.ContainsItem(item))
        {
            return true;
        }

        List<ItemDrop.ItemData> editable = GetEditableItems();
        return editable != null && editable.Contains(item);
    }

    // Call after changing an item in place. The player's own inventory is saved with the character; an
    // item a provider offered is handed back to that provider to save where it lives.
    public void CommitItemEdit(ItemDrop.ItemData item)
    {
        if (item == null || !API.AnyInventoryProviders)
        {
            return;
        }

        Inventory inventory = GetInventory();
        if (inventory != null && inventory.ContainsItem(item))
        {
            return;
        }

        API.SaveProviderItem(item);
    }

    public bool HasItem(ItemDrop.ItemData item)
    {
        return CountItem(item.m_shared.m_name) >= item.m_stack;
    }

    public int CountItem(ItemDrop.ItemData item)
    {
        return CountItem(item.m_shared.m_name);
    }

    public int CountItem(string item)
    {
        Inventory inventory = GetInventory();

        int count = inventory == null ? 0 : inventory.CountItems(item);
        return count + CountStashItems(item) + API.CountProviderItems(item);
    }

    public int CountProviderItem(ItemDrop.ItemData item)
    {
        return CountStashItems(item.m_shared.m_name) + API.CountProviderItems(item.m_shared.m_name);
    }

    private static int CountStashItems(string item)
    {
        return ActiveStash != null ? ActiveStash.GetCountBySharedName(item) : 0;
    }

    public void GiveItem(string item, int amount)
    {
        Debug.Log($"Attempting to give item {item} with amount {amount}");
        if (amount <= 0)
        {
            return;
        }

        GameObject prefab = ObjectDB.instance.GetItemPrefab(item);
        ItemDrop.ItemData itemData = prefab != null ? LootRoller.CreateLootItemData(prefab) : null;
        if (itemData == null)
        {
            Debug.LogWarning($"Could not give item {item}: no such item prefab");
            return;
        }

        // Not vanilla's Inventory.AddItem(name, ...): when the inventory fills, that drops a bare prefab
        // (a stack of one) and still returns the item as added, so a full player claiming bounties got one
        // coin or token per reward. The ItemData path adds what fits and drops the rest at its real size.
        itemData.m_stack = amount;
        GiveItem(itemData);
    }

    public bool GiveItem(ItemDrop.ItemData item)
    {
        Debug.Log($"Attempting to give itemdata {item.m_shared.m_name} with amount {item.m_stack}");
        Inventory inventory = GetInventory();

        do
        {
            ItemDrop.ItemData itemToAdd = item.Clone();
            itemToAdd.m_stack = Mathf.Min(item.m_stack, item.m_shared.m_maxStackSize);
            item.m_stack -= itemToAdd.m_stack;

            if (inventory != null)
            {
                AddItem(ref inventory, itemToAdd);
            }
            else
            {
                DropItem(itemToAdd);
            }
        } while (item.m_stack > 0);

        return true;
    }

    private void AddItem(ref Inventory inventory, ItemDrop.ItemData item)
    {
        if (inventory.AddItem(item))
        {
            SendMessage($"$msg_added {item.m_shared.m_name}", item.m_stack, item.GetIcon());
        }
        else
        {
            DropItem(item);
        }
    }

    private void DropItem(ItemDrop.ItemData item)
    {
        Debug.Log($"Attempting to drop itemdata {item.m_shared.m_name} with amount {item.m_stack}");
        Player player = Player.m_localPlayer;
        ItemDrop itemDrop = ItemDrop.DropItem(item, item.m_stack,
            player.transform.position + player.transform.forward + player.transform.up,
            player.transform.rotation);
        itemDrop.GetComponent<Rigidbody>().linearVelocity = Vector3.up * 5f;

        SendMessage($"$msg_dropped {itemDrop.m_itemData.m_shared.m_name}",
            itemDrop.m_itemData.m_stack, itemDrop.m_itemData.GetIcon());
    }

    // Both removal paths spend the player's own inventory first and only charge the shortfall to
    // external providers, matching how the read paths above report availability.
    // Returns how many were actually removed, which is less than amount when the item (or part of its
    // stack) was no longer anywhere the table can reach. Callers that hand something out in exchange
    // must pay out for that number, not for what they asked for.
    public int RemoveExactItem(ItemDrop.ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return 0;
        }

        if (ActiveStash != null && ActiveStash.TryGetSnapshotPrefab(item, out string stashPrefab))
        {
            return ActiveStash.Take(stashPrefab, amount);
        }

        Inventory inventory = GetInventory();

        int taken = 0;
        if (inventory != null && inventory.ContainsItem(item))
        {
            int before = item.m_stack;
            inventory.RemoveItem(item, amount);
            taken = before - (inventory.ContainsItem(item) ? item.m_stack : 0);
        }

        int shortfall = amount - taken;
        if (shortfall > 0)
        {
            taken += API.RemoveExactProviderItem(item, shortfall);
        }

        return taken;
    }

    public int RemoveItem(ItemDrop.ItemData item)
    {
        return RemoveItem(item.m_shared.m_name, item.m_stack);
    }

    public int RemoveItem(string item, int amount)
    {
        Inventory inventory = GetInventory();

        int taken = 0;
        if (inventory != null)
        {
            int before = inventory.CountItems(item);
            inventory.RemoveItem(item, amount);
            taken = before - inventory.CountItems(item);
        }

        if (ActiveStash != null && amount - taken > 0)
        {
            taken += ActiveStash.TakeBySharedName(item, amount - taken);
        }

        int shortfall = amount - taken;
        if (shortfall > 0)
        {
            taken += API.RemoveProviderItems(item, shortfall);
        }

        return taken;
    }

    public List<ItemDrop.ItemData> GetBoundItems()
    {
        List<ItemDrop.ItemData> boundItems = new List<ItemDrop.ItemData>();

        if (Player.m_localPlayer != null)
        {
            Inventory inventory = Player.m_localPlayer.GetInventory();
            inventory.GetBoundItems(boundItems);
        }

        return boundItems;
    }
}
