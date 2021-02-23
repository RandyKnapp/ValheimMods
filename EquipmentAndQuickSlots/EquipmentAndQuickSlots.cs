using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    [BepInPlugin("randyknapp.mods.equipmentandquickslots", "Equipment and Quick Slots", "1.0.1")]
    [BepInProcess("valheim.exe")]
    public class EquipmentAndQuickSlots : BaseUnityPlugin
    {
        public const int QuickUseSlotCount = 3;
        public const int QuickUseSlotIndexStart = 5;
        public const int EquipSlotCount = 5;
        public static ConfigEntry<string>[] KeyCodes = new ConfigEntry<string>[3];
        public static ConfigEntry<string>[] HotkeyLabels = new ConfigEntry<string>[3];
        public static List<ItemDrop.ItemData.ItemType> EquipSlotTypes = new List<ItemDrop.ItemData.ItemType> {
            ItemDrop.ItemData.ItemType.Helmet,
            ItemDrop.ItemData.ItemType.Chest,
            ItemDrop.ItemData.ItemType.Legs,
            ItemDrop.ItemData.ItemType.Shoulder,
            ItemDrop.ItemData.ItemType.Utility
        };

        public static ConfigEntry<bool> EquipmentSlotsEnabled;
        public static ConfigEntry<bool> QuickSlotsEnabled;

        private Harmony _harmony;

        private void Awake()
        {
            KeyCodes[0] = Config.Bind("Hotkeys", "Quick slot hotkey 1", "z", "Hotkey for Quick Slot 1.");
            KeyCodes[1] = Config.Bind("Hotkeys", "Quick slot hotkey 2", "v", "Hotkey for Quick Slot 2.");
            KeyCodes[2] = Config.Bind("Hotkeys", "Quick slot hotkey 3", "b", "Hotkey for Quick Slot 3.");
            HotkeyLabels[0] = Config.Bind("Hotkeys", "Quick slot hotkey label 1", "", "Hotkey Label for Quick Slot 1. Leave blank to use the hotkey itself.");
            HotkeyLabels[1] = Config.Bind("Hotkeys", "Quick slot hotkey label 2", "", "Hotkey Label for Quick Slot 2. Leave blank to use the hotkey itself.");
            HotkeyLabels[2] = Config.Bind("Hotkeys", "Quick slot hotkey label 3", "", "Hotkey Label for Quick Slot 3. Leave blank to use the hotkey itself.");
            EquipmentSlotsEnabled = Config.Bind("Toggles", "Enable Equipment Slots", true, "Enable the equipment slots. !!! WARNING !!! If you disable this while wearing equipment, you will LOSE IT!");
            QuickSlotsEnabled = Config.Bind("Toggles", "Enable Quick Slots", true, "Enable the quick slots. !!! WARNING !!! If you disable this while items are in the quickslots, you will LOSE THEM!");

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchAll();
        }

        private void Update()
        {
            if (QuickSlotsEnabled.Value)
            {
                var player = Player.m_localPlayer;
                if (player != null && player.TakeInput())
                {
                    for (int i = 0; i < QuickUseSlotCount; ++i)
                    {
                        CheckQuickUseInput(player, i);
                    }
                }
            }
        }

        public static string GetBindingLabel(int index)
        {
            index = Mathf.Clamp(index, 0, QuickUseSlotCount - 1);
            var keycode = GetBindingKeycode(index);
            var label = HotkeyLabels[index].Value;
            if (string.IsNullOrEmpty(label))
            {
                return keycode.ToUpperInvariant();
            }
            else
            {
                return label;
            }
        }

        public static string GetBindingKeycode(int index)
        {
            index = Mathf.Clamp(index, 0, QuickUseSlotCount - 1);
            return KeyCodes[index].Value.ToLowerInvariant();
        }

        public static int GetBonusInventoryRowIndex()
        {
            if (Player.m_localPlayer != null)
            {
                return Player.m_localPlayer.GetInventory().GetHeight() - 1;
            }

            return 0;
        }

        public static void CheckQuickUseInput(Player player, int index)
        {
            var keyCode = GetBindingKeycode(index);
            if (Input.GetKeyDown(keyCode))
            {
                var bonusInventoryRowIndex = GetBonusInventoryRowIndex();
                var item = player.GetInventory().GetItemAt(QuickUseSlotIndexStart + index, bonusInventoryRowIndex);
                if (item != null)
                {
                    player.UseItem(null, item, false);
                }
            }
        }

        public static bool IsQuickSlot(Vector2i pos)
        {
            var bonusInventoryRowIndex = GetBonusInventoryRowIndex();
            var startIndex = EquipmentAndQuickSlots.QuickUseSlotIndexStart;
            var endIndex = startIndex + EquipmentAndQuickSlots.QuickUseSlotCount;
            return pos.y == bonusInventoryRowIndex && pos.x >= startIndex && pos.x < endIndex;
        }

        public static Vector2i GetQuickSlotPosition(int index)
        {
            var bonusInventoryRowIndex = GetBonusInventoryRowIndex();
            var startIndex = EquipmentAndQuickSlots.QuickUseSlotIndexStart;
            return new Vector2i(startIndex + index, bonusInventoryRowIndex);
        }

        public static bool IsEquipmentSlot(Vector2i pos)
        {
            return IsEquipmentSlot(pos.x, pos.y);
        }

        public static bool IsEquipmentSlot(int x, int y)
        {
            var bonusInventoryRowIndex = GetBonusInventoryRowIndex();
            return y == bonusInventoryRowIndex && x >= 0 && x < EquipSlotCount;
        }

        public static ItemDrop.ItemData.ItemType GetEquipmentTypeForSlot(Vector2i pos)
        {
            if (IsEquipmentSlot(pos))
            {
                return EquipSlotTypes[pos.x];
            }
            return ItemDrop.ItemData.ItemType.None;
        }

        public static Vector2i GetEquipmentSlotForType(ItemDrop.ItemData.ItemType type)
        {
            int index = EquipSlotTypes.IndexOf(type);
            if (index >= 0)
            {
                var bonusInventoryRowIndex = GetBonusInventoryRowIndex();
                return new Vector2i(index, bonusInventoryRowIndex);
            }

            return new Vector2i();
        }

        public static bool IsSlotEquippable(ItemDrop.ItemData item)
        {
            return EquipSlotTypes.Contains(item.m_shared.m_itemType);
        }
    }
}
