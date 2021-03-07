using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    public class ExtendedPlayerData : MonoBehaviour
    {
        public Inventory QuickSlotInventory = new Inventory(nameof(QuickSlotInventory), null, EquipmentAndQuickSlots.QuickSlotCount, 1);

        private Player _player;
        private bool _isLoading;

        public void Awake()
        {
            QuickSlotInventory.m_onChanged += OnInventoryChanged;
        }

        private void OnInventoryChanged()
        {
            if (_isLoading)
            {
                return;
            }

            Save();
        }

        public void Save()
        {
            if (_player == null)
            {
                Debug.LogError("Tried to save an ExtendedPlayerData without a player!");
                return;
            }

            Debug.LogWarning("Saving ExtendedPlayerData");
            SaveValue(_player, "ExtendedPlayerData", "This player is using ExtendedPlayerData!");

            var pkg = new ZPackage();
            QuickSlotInventory.Save(pkg);
            SaveValue(_player, nameof(QuickSlotInventory), pkg.GetBase64());
        }

        public void Load(Player fromPlayer)
        {
            if (fromPlayer == null)
            {
                Debug.LogError("Tried to load an ExtendedPlayerData with a null player!");
                return;
            }

            _player = fromPlayer;
            LoadValue(fromPlayer, "ExtendedPlayerData", out var init);
            Debug.LogWarning("Loaded ExtendedPlayerData");

            if (LoadValue(fromPlayer, nameof(QuickSlotInventory), out var inventoryBase64String))
            {
                var pkg = new ZPackage(inventoryBase64String);
                _isLoading = true;
                QuickSlotInventory.Load(pkg);
                _isLoading = false;
            }
        }

        private static void SaveValue(Player player, string key, string value)
        {
            if (player.m_knownTexts.ContainsKey(key))
            {
                player.m_knownTexts[key] = value;
            }
            else
            {
                player.m_knownTexts.Add(key, value);
            }
        }

        private static bool LoadValue(Player player, string key, out string value)
        {
            return player.m_knownTexts.TryGetValue(key, out value);
        }
    }

    public static class PlayerExtensions
    {
        public static bool InventoryContainsItem(this Player player, ItemDrop.ItemData item)
        {
            var inventories = player.GetAllInventories();
            return inventories.Any(x => x.m_inventory.Contains(item));
        }

        public static List<Inventory> GetAllInventories(this Player player)
        {
            var result = new List<Inventory>();
            result.Add(player.m_inventory);
            if (player.IsExtended() && player.GetQuickSlotInventory() != null)
            {
                result.Add(player.GetQuickSlotInventory());
            }
            return result;
        }

        public static Inventory GetQuickSlotInventory(this Player player)
        {
            var extendedPlayer = player.Extended();
            if (extendedPlayer != null)
            {
                return extendedPlayer.QuickSlotInventory;
            }

            return null;
        }

        public static ItemDrop.ItemData GetQuickSlotItem(this Player player, int index)
        {
            if (index < 0 || index > EquipmentAndQuickSlots.QuickSlotCount)
            {
                return null;
            }

            var extendedPlayer = player.Extended();
            if (extendedPlayer != null)
            {
                return extendedPlayer.QuickSlotInventory.GetItemAt(index, 0);
            }

            return null;
        }

        /*public static Inventory GetQuickUseInventory(this Player player)
        {
            
        }*/

        public static void InitializeExtendedPlayer(this Player player)
        {
            if (player.IsExtended())
            {
                return;
            }

            var extendedPlayerData = player.gameObject.AddComponent<ExtendedPlayerData>();
            extendedPlayerData.Load(player);
        }

        public static bool IsExtended(this Player player)
        {
            return player.gameObject.GetComponent<ExtendedPlayerData>() != null;
        }

        public static ExtendedPlayerData Extended(this Player player)
        {
            return player.gameObject.GetComponent<ExtendedPlayerData>();
        }

        public static void BeforeSave(this Player player)
        {
            player.InitializeExtendedPlayer();
            player.Extended().Save();
        }

        public static void AfterLoad(this Player player)
        {
            player.InitializeExtendedPlayer();
            player.Extended().Load(player);
        }
    }
}
