using System.Collections.Generic;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    public class ExtendedPlayerData : MonoBehaviour
    {
        public Inventory QuickSlotInventory = new Inventory(nameof(QuickSlotInventory), null, EquipmentAndQuickSlots.QuickSlotCount, 1);
        public Inventory EquipmentSlotInventory = new Inventory(nameof(EquipmentSlotInventory), null, EquipmentAndQuickSlots.EquipSlotCount, 1);

        private Player _player;
        private bool _isLoading;

        public const string Sentinel = "<|>";

        public void Awake()
        {
            QuickSlotInventory.m_onChanged += OnInventoryChanged;
            EquipmentSlotInventory.m_onChanged += OnInventoryChanged;
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
                EquipmentAndQuickSlots.LogError("Tried to save an ExtendedPlayerData without a player!");
                return;
            }

            //EquipmentAndQuickSlots.LogWarning("Saving ExtendedPlayerData");
            SaveValue(_player, "ExtendedPlayerData", "This player is using ExtendedPlayerData!");

            var pkg = new ZPackage();
            QuickSlotInventory.Save(pkg);
            SaveValue(_player, nameof(QuickSlotInventory), pkg.GetBase64());

            pkg = new ZPackage();
            EquipmentSlotInventory.Save(pkg);
            SaveValue(_player, nameof(EquipmentSlotInventory), pkg.GetBase64());
        }

        public void Load(Player fromPlayer)
        {
            if (fromPlayer == null)
            {
                EquipmentAndQuickSlots.LogError("Tried to load an ExtendedPlayerData with a null player!");
                return;
            }

            _player = fromPlayer;
            LoadValue(fromPlayer, "ExtendedPlayerData", out var init);
            //EquipmentAndQuickSlots.LogWarning("Loaded ExtendedPlayerData");

            if (LoadValue(fromPlayer, nameof(QuickSlotInventory), out var quickSlotData))
            {
                var pkg = new ZPackage(quickSlotData);
                _isLoading = true;
                QuickSlotInventory.Load(pkg);
                
                if (!EquipmentAndQuickSlots.QuickSlotsEnabled.Value)
                {
                    _player.m_inventory.MoveAll(QuickSlotInventory);

                    pkg = new ZPackage(quickSlotData);
                    QuickSlotInventory.Save(pkg);
                    SaveValue(_player, nameof(QuickSlotInventory), pkg.GetBase64());
                }

                _isLoading = false;
            }

            if (LoadValue(fromPlayer, nameof(EquipmentSlotInventory), out var equipSlotData))
            {
                var pkg = new ZPackage(equipSlotData);
                _isLoading = true;
                EquipmentSlotInventory.Load(pkg);

                if (!EquipmentAndQuickSlots.EquipmentSlotsEnabled.Value)
                {
                    _player.m_inventory.MoveAll(EquipmentSlotInventory);

                    pkg = new ZPackage(quickSlotData);
                    EquipmentSlotInventory.Save(pkg);
                    SaveValue(_player, nameof(EquipmentSlotInventory), pkg.GetBase64());
                }

                _isLoading = false;
            }
        }

        private static void SaveValue(Player player, string key, string value)
        {
            if (player.m_knownTexts.ContainsKey(key))
            {
                EquipmentAndQuickSlots.LogWarning("Found KnownText for save data, converting to customData");
                player.m_knownTexts.Remove(key);
            }

            if (player.m_customData.ContainsKey(key))
                player.m_customData[key] = value;
            else
                player.m_customData.Add(key, value);
        }

        private static bool LoadValue(Player player, string key, out string value)
        {
            if (player.m_customData.TryGetValue(key, out value))
                return true;

            var foundInKnownTexts = player.m_knownTexts.TryGetValue(key, out value);
            if (!foundInKnownTexts)
                key = Sentinel + key;
            foundInKnownTexts = player.m_knownTexts.TryGetValue(key, out value);
            if (foundInKnownTexts)
                EquipmentAndQuickSlots.LogWarning("Loaded data from knownTexts. Will be converted to customData on save.");

            return foundInKnownTexts;
        }
    }

    public static class PlayerExtensions
    {
        public static List<Inventory> GetAllInventories(this Player player)
        {
            var result = new List<Inventory>();
            result.Add(player.m_inventory);
            if (EquipmentAndQuickSlots.QuickSlotsEnabled.Value && player.IsExtended() && player.GetQuickSlotInventory() != null)
            {
                result.Add(player.GetQuickSlotInventory());
            }
            if (EquipmentAndQuickSlots.EquipmentSlotsEnabled.Value && player.IsExtended() && player.GetEquipmentSlotInventory() != null)
            {
                result.Add(player.GetEquipmentSlotInventory());
            }
            return result;
        }

        public static Inventory GetInventoryForItem(this Player player, ItemDrop.ItemData item)
        {
            player.m_inventory.Extended().CallBase = true;

            var inventories = player.GetAllInventories();
            foreach (var inventory in inventories)
            {
                if (inventory.ContainsItem(item))
                {
                    player.m_inventory.Extended().CallBase = false;
                    return inventory;
                }
            }

            player.m_inventory.Extended().CallBase = false;
            return null;
        }

        public static Inventory GetQuickSlotInventory(this Player player)
        {
            if (player != null)
            {
                var extendedPlayer = player.Extended();
                if (extendedPlayer != null)
                {
                    return extendedPlayer.QuickSlotInventory;
                }
            }

            return null;
        }

        public static Inventory GetEquipmentSlotInventory(this Player player)
        {
            if (player != null)
            {
                var extendedPlayer = player.Extended();
                if (extendedPlayer != null)
                {
                    return extendedPlayer.EquipmentSlotInventory;
                }
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

        public static ItemDrop.ItemData GetEquipmentSlotItem(this Player player, int index)
        {
            if (player == null)
            {
                return null;
            }

            if (index < 0 || index > EquipmentAndQuickSlots.EquipSlotCount)
            {
                return null;
            }

            var extendedPlayer = player.Extended();
            if (extendedPlayer != null)
            {
                return extendedPlayer.EquipmentSlotInventory.GetItemAt(index, 0);
            }

            return null;
        }

        public static void InitializeExtendedPlayer(this Player player)
        {
            if (player == null || player.IsExtended())
            {
                return;
            }

            var extendedPlayerData = player.gameObject.AddComponent<ExtendedPlayerData>();
            extendedPlayerData.Load(player);
        }

        public static bool IsExtended(this Player player)
        {
            return player?.gameObject.GetComponent<ExtendedPlayerData>() != null;
        }

        public static ExtendedPlayerData Extended(this Player player)
        {
            return player?.gameObject.GetComponent<ExtendedPlayerData>();
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
            player.EquipInventoryItems();
        }
    }
}
