using EpicLoot;
using EpicLoot.Compatibility;
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

    // The read paths below also consult the inventory providers registered through
    // EpicLoot.API.RegisterInventoryProvider (nearby containers, backpacks, remote stashes), and all but
    // GetEditableItems the open table's storage. With neither, they read the player's own inventory alone.
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
        EpicLoot.EpicLoot.Log($"Attempting to give item {item} with amount {amount}");
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
        // (a stack of one) and still returns the item as added. The ItemData path adds what fits and drops
        // the rest at its real size.
        itemData.m_stack = amount;
        GiveItem(itemData);
    }

    public bool GiveItem(ItemDrop.ItemData item)
    {
        return GiveItem(item, announce: true);
    }

    /// <param name="announce">
    /// Whether each stack added is reported. Vanilla shows those one a second, so a caller giving many kinds at
    /// once passes false and reports the total itself. Anything dropped for lack of room is always reported.
    /// </param>
    public bool GiveItem(ItemDrop.ItemData item, bool announce)
    {
        EpicLoot.EpicLoot.Log($"Attempting to give itemdata {item.m_shared.m_name} with amount {item.m_stack}");
        Inventory inventory = GetInventory();

        do
        {
            ItemDrop.ItemData itemToAdd = item.Clone();
            itemToAdd.m_stack = Mathf.Min(item.m_stack, item.m_shared.m_maxStackSize);
            item.m_stack -= itemToAdd.m_stack;

            if (inventory != null)
            {
                AddItem(ref inventory, itemToAdd, announce);
            }
            else
            {
                DropItem(itemToAdd);
            }
        } while (item.m_stack > 0);

        return true;
    }

    private void AddItem(ref Inventory inventory, ItemDrop.ItemData item, bool announce)
    {
        if (inventory.AddItem(item))
        {
            if (announce)
            {
                SendMessage($"$msg_added {item.m_shared.m_name}", item.m_stack, item.GetIcon());
            }
        }
        else
        {
            DropItem(item);
        }
    }

    private void DropItem(ItemDrop.ItemData item)
    {
        EpicLoot.EpicLoot.Log($"Attempting to drop itemdata {item.m_shared.m_name} with amount {item.m_stack}");
        Player player = Player.m_localPlayer;
        ItemDrop itemDrop = ItemDrop.DropItem(item, item.m_stack,
            player.transform.position + player.transform.forward + player.transform.up,
            player.transform.rotation);
        itemDrop.GetComponent<Rigidbody>().linearVelocity = Vector3.up * 5f;

        SendMessage($"$msg_dropped {itemDrop.m_itemData.m_shared.m_name}",
            itemDrop.m_itemData.m_stack, itemDrop.m_itemData.GetIcon());
    }

    /// <summary>What a table action costs: materials by name, and exact items it consumes.</summary>
    public sealed class Payment
    {
        internal sealed class Line
        {
            public string Name;
            public ItemDrop.ItemData Exact;
            public int Amount;
        }

        internal readonly List<Line> Lines = new List<Line>();

        /// <summary>A cost list. Nothing is added under the no-cost cheat.</summary>
        public Payment Cost(IEnumerable<InventoryItemListElement> cost)
        {
            if (cost == null || (Player.m_localPlayer != null && Player.m_localPlayer.NoCostCheat()))
            {
                return this;
            }

            foreach (InventoryItemListElement element in cost)
            {
                ItemDrop.ItemData item = element.GetItem();
                if (item != null)
                {
                    ByName(item.m_shared.m_name, item.m_stack);
                }
            }

            return this;
        }

        public Payment ByName(string sharedName, int amount)
        {
            if (!string.IsNullOrEmpty(sharedName) && amount > 0)
            {
                Lines.Add(new Line { Name = sharedName, Amount = amount });
            }

            return this;
        }

        /// <summary>That item itself, as for an input the action uses up.</summary>
        public Payment Exact(ItemDrop.ItemData item, int amount)
        {
            if (item != null && amount > 0)
            {
                Lines.Add(new Line { Name = item.m_shared.m_name, Exact = item, Amount = amount });
            }

            return this;
        }
    }

    /// <summary>What a payment took, so an action that cannot go ahead after all can hand it back.</summary>
    public sealed class Receipt
    {
        internal EnchantingTableStash Stash;
        internal readonly Dictionary<string, int> FromStash = new Dictionary<string, int>();
        internal readonly List<ItemDrop.ItemData> FromOutside = new List<ItemDrop.ItemData>();

        public void Refund()
        {
            if (FromStash.Count > 0)
            {
                if (Stash != null)
                {
                    Stash.Return(FromStash);
                }
                else
                {
                    foreach (KeyValuePair<string, int> pair in FromStash)
                    {
                        if (StashCatalog.TryGetByPrefab(pair.Key, out StashItemKind kind))
                        {
                            Instance.GiveItem(kind.CreateItem(pair.Value));
                        }
                    }
                }
            }

            foreach (ItemDrop.ItemData item in FromOutside)
            {
                Instance.GiveItem(item);
            }

            FromStash.Clear();
            FromOutside.Clear();
        }
    }

    private sealed class Planned
    {
        public Payment.Line Line;
        public string StashPrefab;
        public int FromStash;
        public int Got;
        public int OutsideNeed => Line.Amount - Got;
    }

    /// <summary>
    /// Takes a payment, then reports what was taken: null when it could not all be paid, in which case nothing
    /// is kept. The open table's storage is spent first, and only through its owner, the one client that may
    /// change it: its share is asked for in one request and the rest of the action waits for the answer, so two
    /// players spending the same stored materials cannot both be paid for. What the owner could not supply is
    /// taken from the player's inventory, then from inventory providers. On the owner, and with nothing to take
    /// from storage, <paramref name="onDone"/> runs before this returns.
    /// </summary>
    public void Pay(Payment payment, System.Action<Receipt> onDone)
    {
        EnchantingTableStash stash = ActiveStash;
        Dictionary<string, int> stashWant = new Dictionary<string, int>();
        List<Planned> plan = new List<Planned>();
        foreach (Payment.Line line in payment.Lines)
        {
            Planned planned = new Planned { Line = line };
            if (stash != null)
            {
                if (line.Exact != null)
                {
                    if (stash.TryGetSnapshotPrefab(line.Exact, out string prefab))
                    {
                        planned.StashPrefab = prefab;
                        planned.FromStash = line.Amount;
                    }
                }
                else if (StashCatalog.TryGetBySharedName(line.Name, out StashItemKind kind))
                {
                    stashWant.TryGetValue(kind.PrefabName, out int already);
                    planned.StashPrefab = kind.PrefabName;
                    planned.FromStash = Mathf.Clamp(stash.GetCount(kind.PrefabName) - already, 0, line.Amount);
                }

                if (planned.FromStash > 0)
                {
                    stashWant.TryGetValue(planned.StashPrefab, out int wanted);
                    stashWant[planned.StashPrefab] = wanted + planned.FromStash;
                }
            }

            plan.Add(planned);
        }

        if (stashWant.Count == 0)
        {
            Settle(plan, stash, new Dictionary<string, int>(), onDone);
            return;
        }

        stash.Spend(stashWant, taken => Settle(plan, stash, taken, onDone), () =>
        {
            Debug.LogWarning("[EpicLoot] The enchanting table's storage never confirmed a payment; the action was not carried out.");
            onDone(null);
        });
    }

    private void Settle(List<Planned> plan, EnchantingTableStash stash, Dictionary<string, int> taken, System.Action<Receipt> onDone)
    {
        Receipt receipt = new Receipt { Stash = stash };
        foreach (KeyValuePair<string, int> pair in taken)
        {
            receipt.FromStash[pair.Key] = pair.Value;
        }

        Dictionary<string, int> left = new Dictionary<string, int>(taken);
        foreach (Planned planned in plan)
        {
            if (planned.FromStash <= 0)
            {
                continue;
            }

            left.TryGetValue(planned.StashPrefab, out int available);
            planned.Got = Mathf.Min(planned.FromStash, available);
            left[planned.StashPrefab] = available - planned.Got;
        }

        if (!CanCoverOutside(plan))
        {
            receipt.Refund();
            onDone(null);
            return;
        }

        foreach (Planned planned in plan)
        {
            int need = planned.OutsideNeed;
            if (need <= 0)
            {
                continue;
            }

            int removed = TakeOutside(planned, need, receipt);
            if (removed < need)
            {
                Debug.LogWarning($"[EpicLoot] Only {removed} of {need} {planned.Line.Name} could be taken; the action was not carried out.");
                receipt.Refund();
                onDone(null);
                return;
            }
        }

        onDone(receipt);
    }

    // Checked in full before anything outside the storage is taken, so a payment that cannot be made takes nothing.
    private bool CanCoverOutside(List<Planned> plan)
    {
        Inventory inventory = GetInventory();
        Dictionary<string, int> byName = new Dictionary<string, int>();
        Dictionary<string, int> byKind = new Dictionary<string, int>();
        foreach (Planned planned in plan)
        {
            int need = planned.OutsideNeed;
            if (need <= 0)
            {
                continue;
            }

            if (planned.Line.Exact == null)
            {
                byName.TryGetValue(planned.Line.Name, out int sum);
                byName[planned.Line.Name] = sum + need;
            }
            else if (planned.StashPrefab != null)
            {
                byKind.TryGetValue(planned.StashPrefab, out int sum);
                byKind[planned.StashPrefab] = sum + need;
            }
            else if (planned.Line.Exact.m_stack < need || !IsHeld(planned.Line.Exact, inventory))
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, int> pair in byName)
        {
            int held = (inventory != null ? inventory.CountItems(pair.Key) : 0) + API.CountProviderItems(pair.Key);
            if (held < pair.Value)
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, int> pair in byKind)
        {
            if (CountStashKindInInventory(pair.Key) < pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsHeld(ItemDrop.ItemData item, Inventory inventory)
    {
        if (inventory != null && inventory.ContainsItem(item))
        {
            return true;
        }

        if (!API.AnyInventoryProviders)
        {
            return false;
        }

        List<ItemDrop.ItemData> provided = new List<ItemDrop.ItemData>();
        API.AppendProviderItems(provided);
        return provided.Contains(item);
    }

    private int TakeOutside(Planned planned, int need, Receipt receipt)
    {
        Payment.Line line = planned.Line;
        if (line.Exact != null && planned.StashPrefab != null)
        {
            int removed = RemoveStashKindFromInventory(planned.StashPrefab, need);
            if (removed > 0 && StashCatalog.TryGetByPrefab(planned.StashPrefab, out StashItemKind kind))
            {
                receipt.FromOutside.Add(kind.CreateItem(removed));
            }

            return removed;
        }

        // Copied before it is taken: the copy is what a refund hands back.
        ItemDrop.ItemData template = line.Exact ?? FindByName(line.Name);
        ItemDrop.ItemData copy = template?.Clone();
        int taken = line.Exact != null ? RemoveExactOutside(line.Exact, need) : RemoveOutsideStash(line.Name, need);
        if (taken > 0 && copy != null)
        {
            copy.m_stack = taken;
            receipt.FromOutside.Add(copy);
        }

        return taken;
    }

    private ItemDrop.ItemData FindByName(string sharedName)
    {
        List<ItemDrop.ItemData> items = GetAllItems();
        return items?.Find(x => x?.m_shared != null && x.m_shared.m_name == sharedName);
    }

    private int RemoveExactOutside(ItemDrop.ItemData item, int amount)
    {
        Inventory inventory = GetInventory();
        int taken = 0;
        if (inventory != null && inventory.ContainsItem(item))
        {
            int before = item.m_stack;
            inventory.RemoveItem(item, amount);
            taken = before - (inventory.ContainsItem(item) ? item.m_stack : 0);
        }

        if (amount - taken > 0)
        {
            taken += API.RemoveExactProviderItem(item, amount - taken);
        }

        return taken;
    }

    // Pays at once from the inventory, then providers; never from the table storage, which pays through Pay.
    public int RemoveItem(string item, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        return RemoveOutsideStash(item, amount);
    }

    private int RemoveOutsideStash(string item, int amount)
    {
        int taken = 0;
        Inventory inventory = GetInventory();
        if (inventory != null)
        {
            int before = inventory.CountItems(item);
            inventory.RemoveItem(item, amount);
            taken = before - inventory.CountItems(item);
        }

        if (amount - taken > 0)
        {
            taken += API.RemoveProviderItems(item, amount - taken);
        }

        return taken;
    }

    private int CountStashKindInInventory(string prefabName)
    {
        Inventory inventory = GetInventory();
        int count = 0;
        if (inventory == null)
        {
            return 0;
        }

        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (StashCatalog.IsStashable(item) && StashCatalog.GetPrefabName(item) == prefabName)
            {
                count += item.m_stack;
            }
        }

        return count;
    }

    // An exact item from the storage stands for its kind, so a shortfall is made up from stacks of that same
    // kind the player carries, never from a same-named item the storage would not accept (a runestone
    // carrying an effect shares its name with the blank one).
    private int RemoveStashKindFromInventory(string prefabName, int amount)
    {
        Inventory inventory = GetInventory();
        if (inventory == null || amount <= 0)
        {
            return 0;
        }

        int taken = 0;
        foreach (ItemDrop.ItemData item in new List<ItemDrop.ItemData>(inventory.GetAllItems()))
        {
            if (taken >= amount)
            {
                break;
            }

            if (!StashCatalog.IsStashable(item) || StashCatalog.GetPrefabName(item) != prefabName)
            {
                continue;
            }

            int take = Mathf.Min(item.m_stack, amount - taken);
            inventory.RemoveItem(item, take);
            taken += take;
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
            EaqsQuickSlots.AppendItems(boundItems);
        }

        return boundItems;
    }
}
