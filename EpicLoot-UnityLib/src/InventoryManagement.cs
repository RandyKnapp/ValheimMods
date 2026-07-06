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

    public List<ItemDrop.ItemData> GetAllItems()
    {
        Inventory inventory = GetInventory();
        if (inventory != null)
        {
            return inventory.GetAllItems();
        }

        return null;
    }

    public bool HasItem(ItemDrop.ItemData item)
    {
        Inventory inventory = GetInventory();

        if (inventory == null || inventory.CountItems(item.m_shared.m_name) < item.m_stack)
        {
            return false;
        }

        return true;
    }

    public int CountItem(ItemDrop.ItemData item)
    {
        return CountItem(item.m_shared.m_name);
    }

    public int CountItem(string item)
    {
        Inventory inventory = GetInventory();

        if (inventory == null)
        {
            return 0;
        }

        return inventory.CountItems(item);
    }

    public void GiveItem(string item, int amount)
    {
        Debug.Log($"Attempting to give item {item} with amount {amount}");
        Inventory inventory = GetInventory();
        if (inventory != null)
        {
            AddItem(ref inventory, item, amount);
        }
        else
        {
            DropItem(item, amount);
        }
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

    private void AddItem(ref Inventory inventory, string item, int amount)
    {
        ItemDrop.ItemData result = inventory.AddItem(item, amount, 1, 0, 0, string.Empty);

        if (result == null)
        {
            DropItem(item, amount);
        }
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

    private void DropItem(string item, int amount)
    {
        Debug.Log($"Attempting to drop item {item} with amount {amount}");
        Player player = Player.m_localPlayer;
        GameObject prefab = ObjectDB.instance.GetItemPrefab(item);

        if (prefab != null)
        {
            GameObject go = GameObject.Instantiate(prefab,
                player.transform.position + player.transform.forward + player.transform.up,
                player.transform.rotation);

            ItemDrop itemdrop = go.GetComponent<ItemDrop>();
            itemdrop.SetStack(amount);
            itemdrop.GetComponent<Rigidbody>().linearVelocity = Vector3.up * 5f;

            SendMessage($"$msg_dropped {itemdrop.m_itemData.m_shared.m_name}",
                itemdrop.m_itemData.m_stack, itemdrop.m_itemData.GetIcon());
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

    public void RemoveExactItem(ItemDrop.ItemData item, int amount)
    {
        Inventory inventory = GetInventory();

        inventory.RemoveItem(item, amount);
    }

    public void RemoveItem(ItemDrop.ItemData item)
    {
        RemoveItem(item.m_shared.m_name, item.m_stack);
    }

    public void RemoveItem(string item, int amount)
    {
        Inventory inventory = GetInventory();

        inventory.RemoveItem(item, amount);
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
