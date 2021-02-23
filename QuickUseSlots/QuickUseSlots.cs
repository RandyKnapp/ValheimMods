using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace QuickUseSlots
{
    [BepInPlugin("randyknapp.mods.quickuseslots", "Quick Use Slots", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class QuickUseSlots : BaseUnityPlugin
    {
        public const int QuickUseSlotCount = 3;
        public const int QuickUseSlotIndexStart = 5;
        public const int EquipSlotCount = 5;
        public const string QuickUseAction = "QuickUse";
        public static KeyCode[] KeyCodes = { KeyCode.Z, KeyCode.X, KeyCode.C };
        public static List<ItemDrop.ItemData.ItemType> EquipSlotTypes = new List<ItemDrop.ItemData.ItemType> {
            ItemDrop.ItemData.ItemType.Helmet,
            ItemDrop.ItemData.ItemType.Chest,
            ItemDrop.ItemData.ItemType.Legs,
            ItemDrop.ItemData.ItemType.Shoulder,
            ItemDrop.ItemData.ItemType.Utility
        };

        private Harmony _harmony;

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            for (int i = 0; i < QuickUseSlotCount; ++i)
            {
                SetBinding(i);
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchAll();
        }

        private void Update()
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                for (int i = 0; i < QuickUseSlotCount; ++i)
                {
                    CheckQuickUseInput(player, i);
                }
            }
        }

        public static string GetActionName(int index)
        {
            return $"{QuickUseAction}{index + 1}";
        }

        public static KeyCode GetBindingKeycode(int index)
        {
            index = Mathf.Clamp(index, 0, QuickUseSlotCount - 1);
            return KeyCodes[index];
        }

        public static void SetBinding(int index)
        {
            if (ZInput.instance == null)
            {
                return;
            }

            var action = GetActionName(index);
            var key = GetBindingKeycode(index);
            if (ZInput.instance.GetBoundKeyString(action).StartsWith("MISS"))
            {
                ZInput.instance.AddButton(action, key);
            }
            else
            {
                ZInput.instance.Setbutton(action, key);
            }
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
            var action = GetActionName(index);
            if (ZInput.GetButtonDown(action))
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
            var startIndex = QuickUseSlots.QuickUseSlotIndexStart;
            var endIndex = startIndex + QuickUseSlots.QuickUseSlotCount;
            return pos.y == bonusInventoryRowIndex && pos.x >= startIndex && pos.x < endIndex;
        }

        public static Vector2i GetQuickSlotPosition(int index)
        {
            var bonusInventoryRowIndex = GetBonusInventoryRowIndex();
            var startIndex = QuickUseSlots.QuickUseSlotIndexStart;
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
