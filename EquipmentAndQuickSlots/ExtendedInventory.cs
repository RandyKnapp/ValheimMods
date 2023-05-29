using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    public static class InventoryExtensions
    {
        public static bool IsExtended(this Inventory inventory)
        {
            return inventory is ExtendedInventory;
        }

        public static ExtendedInventory Extended(this Inventory inventory)
        {
            return inventory is ExtendedInventory extended ? extended : null;
        }

        public static bool DoExtendedCall(this Inventory inventory)
        {
            return inventory.Extended() != null && !inventory.Extended().CallBase;
        }

        public static bool CountAsEmptySlots(this Inventory inventory, Player player)
        {
            return inventory != player.GetEquipmentSlotInventory();
        }
    }

    public class ExtendedInventory : Inventory
    {
        public bool CallBase { get; set; }
        private readonly Player _player;
        private List<Inventory> _inventories;

        public ExtendedInventory(Player player, string name, Sprite bkg, int w, int h) : base(name, bkg, w, h)
        {
            //EquipmentAndQuickSlots.LogWarning("New Extended Inventory for Player");
            _player = player;
        }

        public void OverrideAwake()
        {
            _player.InitializeExtendedPlayer();
            _inventories = _player.GetAllInventories();
            OverrideUpdateTotalWeight();
        }

        public bool OverrideCanAddItem(GameObject prefab, int stack)
        {
            CallBase = true;
            var result = _inventories.Any(x => x.CountAsEmptySlots(_player) && x.CanAddItem(prefab, stack));
            CallBase = false;
            return result;
        }

        public bool OverrideCanAddItem(ItemDrop.ItemData item, int stack)
        {
            CallBase = true;
            var result = _inventories.Any(x => x.CountAsEmptySlots(_player) && x.CanAddItem(item, stack));
            CallBase = false;
            return result;
        }

        public bool OverrideAddItem(ItemDrop.ItemData item)
        {
            CallBase = true;
            var result = false;
            foreach (var inventory in _inventories)
            {
                if (!inventory.CountAsEmptySlots(_player))
                {
                    continue;
                }

                if (inventory.AddItem(item))
                {
                    //EquipmentAndQuickSlots.LogWarning($"Added item ({item.m_shared.m_name}) to ({inventory.m_name}) at ({item.m_gridPos})");
                    OverrideUpdateTotalWeight();
                    result = true;
                    break;
                }
            }
            
            CallBase = false;
            return result;
        }

        public Vector2i OverrideFindEmptySlot(bool topFirst)
        {
            CallBase = true;
            Vector2i result = new Vector2i(-1, -1);
            foreach (var inventory in _inventories)
            {
                if (!inventory.CountAsEmptySlots(_player))
                {
                    continue;
                }

                result = inventory.FindEmptySlot(topFirst);
                if (result.x != -1)
                {
                    break;
                }
            }

            CallBase = false;
            return result;
        }

        public ItemDrop.ItemData OverrideFindFreeStackItem(string name, int quality)
        {
            ItemDrop.ItemData result = null;
            foreach (var inventory in _inventories)
            {
                if (!inventory.CountAsEmptySlots(_player))
                {
                    continue;
                }

                result = inventory.FindFreeStackItem(name, quality);
                if (result != null)
                {
                    break;
                }
            }

            return result;
        }

        public bool OverrideContainsItem(ItemDrop.ItemData item)
        {
            CallBase = true;
            var result = _inventories.Any(x => x.ContainsItem(item));
            CallBase = false;
            return result;
        }

        public List<ItemDrop.ItemData> OverrideGetEquipedItems()
        {
            CallBase = true;
            var result = new List<ItemDrop.ItemData>();
            _inventories.ForEach(x => result.AddRange(x.GetEquippedItems()));
            CallBase = false;
            return result;
        }

        public void OverrideGetWornItems(List<ItemDrop.ItemData> worn)
        {
            CallBase = true;
            _inventories.ForEach(x => x.GetWornItems(worn));
            CallBase = false;
        }

        public void OverrideGetValuableItems(List<ItemDrop.ItemData> items)
        {
            CallBase = true;
            _inventories.ForEach(x => x.GetValuableItems(items));
            CallBase = false;
        }

        public List<ItemDrop.ItemData> OverrideGetAllItems()
        {
            CallBase = true;
            var result = new List<ItemDrop.ItemData>();
            _inventories.ForEach(x => result.AddRange(x.m_inventory));
            CallBase = false;
            return result;
        }

        public void OverrideGetAllItems(string name, List<ItemDrop.ItemData> items)
        {
            CallBase = true;
            _inventories.ForEach(x => x.GetAllItems(name, items));
            CallBase = false;
        }

        public void OverrideGetAllItems(ItemDrop.ItemData.ItemType type, List<ItemDrop.ItemData> items)
        {
            CallBase = true;
            _inventories.ForEach(x => x.GetAllItems(type, items));
            CallBase = false;
        }

        public int OverrideGetEmptySlots()
        {
            CallBase = true;
            var result = _inventories.Sum(x => x.CountAsEmptySlots(_player) ? x.GetEmptySlots() : 0);
            CallBase = false;
            return result;
        }

        public bool OverrideHaveEmptySlot()
        {
            CallBase = true;
            var result = _inventories.Any(x => x.CountAsEmptySlots(_player) && x.HaveEmptySlot());
            CallBase = false;
            return result;
        }

        public bool OverrideRemoveOneItem(ItemDrop.ItemData item)
        {
            CallBase = true;
            var result = _inventories.Any(x => x.RemoveOneItem(item));
            OverrideUpdateTotalWeight();
            CallBase = false;
            return result;
        }

        public bool OverrideRemoveItem(ItemDrop.ItemData item)
        {
            CallBase = true;
            var result = _inventories.Any(x => x.RemoveItem(item));
            OverrideUpdateTotalWeight();
            CallBase = false;
            return result;
        }

        public bool OverrideRemoveItem(ItemDrop.ItemData item, int amount)
        {
            CallBase = true;
            var result = _inventories.Any(x => x.RemoveItem(item, amount));
            OverrideUpdateTotalWeight();
            CallBase = false;
            return result;
        }

        public void OverrideRemoveItem(string name, int amount, int itemQuality = -1)
        {
            CallBase = true;
            foreach (var inventory in _inventories)
            {
                foreach (var itemData in inventory.m_inventory)
                {
                    if (itemData.m_shared.m_name == name && (itemQuality < 0 || itemData.m_quality == itemQuality))
                    {
                        var num = Mathf.Min(itemData.m_stack, amount);
                        itemData.m_stack -= num;
                        amount -= num;
                        inventory.Changed();
                        if (amount <= 0)
                        {
                            break;
                        }
                    }
                }
                inventory.m_inventory.RemoveAll((x => x.m_stack <= 0));
                OverrideUpdateTotalWeight();
            }
            CallBase = false;
        }

        public bool OverrideHaveItem(string name)
        {
            CallBase = true;
            var result = _inventories.Any(x => x.HaveItem(name));
            CallBase = false;
            return result;
        }

        public void OverrideGetAllPieceTables(List<PieceTable> tables)
        {
            CallBase = true;
            _inventories.ForEach(x => x.GetAllPieceTables(tables));
            CallBase = false;
        }

        public int OverrideCountItems(string name)
        {
            CallBase = true;
            var result = _inventories.Sum(x => x.CountItems(name));
            CallBase = false;
            return result;
        }

        public ItemDrop.ItemData OverrideGetItem(string name, int quality = -1, bool isPrefabName = false)
        {
            CallBase = true;
            ItemDrop.ItemData result = null;
            foreach (var inventory in _inventories)
            {
                result = inventory.GetItem(name, quality, isPrefabName);
                if (result != null)
                {
                    break;
                }
            }
            CallBase = false;
            return result;
        }

        public ItemDrop.ItemData OverrideGetAmmoItem(string ammoName, string matchPrefabName = null)
        {
            CallBase = true;
            ItemDrop.ItemData result = null;
            foreach (var inventory in _inventories)
            {
                result = inventory.GetAmmoItem(ammoName, matchPrefabName);
                if (result != null)
                {
                    break;
                }
            }
            CallBase = false;
            return result;
        }

        public int OverrideFindFreeStackSpace(string name)
        {
            CallBase = true;
            var result = _inventories.Sum(x => x.FindFreeStackSpace(name));
            CallBase = false;
            return result;
        }

        public int OverrideNrOfItems()
        {
            CallBase = true;
            var result = _inventories.Sum(x => x.NrOfItems());
            CallBase = false;
            return result;
        }

        public float OverrideSlotsUsedPercentage()
        {
            var totalCount = (float)_inventories.Sum(x => x.m_inventory.Count);
            var totalSlots = (float)_inventories.Sum(x => x.m_width * x.m_height);
            return (totalCount / totalSlots * 100.0f);
        }

        public void OverrideUpdateTotalWeight()
        {
            CallBase = true;
            m_totalWeight = 0f;
            var iWeight = new float[_inventories.Count()];
            //EquipmentAndQuickSlots.LogWarning("Begin updating " + _inventories.Count() + " inventories of weights");

            for (var i = 0; i < _inventories.Count(); i++)
            {
                //EquipmentAndQuickSlots.LogWarning("InventoryName: " + _inventories[i].m_name + " has " + _inventories[i].m_inventory.Count() + " items");

                foreach (var itemData in _inventories[i].m_inventory)
                {
                    iWeight[i] += itemData.GetWeight();
                    //EquipmentAndQuickSlots.LogWarning("ItemName: " + itemData.m_shared.m_name + ", ItemWeight: " + itemData.GetWeight() + ", Total " + _inventories[i].m_name + " Weight: " + iWeight[i] );
                }
                //EquipmentAndQuickSlots.LogWarning(_inventories[i].m_name + " Weight:" + iWeight[i]);
                m_totalWeight += iWeight[i];
            }
            //EquipmentAndQuickSlots.LogWarning("Total Weight of all inventories: " + m_totalWeight);
            CallBase = false;
            //EquipmentAndQuickSlots.LogWarning("Done Updating Total Weight");
        }


        public bool OverrideIsTeleportable()
        {
            CallBase = true;
            var result = _inventories.All(x => x.IsTeleportable());
            CallBase = false;
            return result;
        }
    }
}
