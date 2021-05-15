using System.Collections.Generic;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public static class Indestructible
    {
        public static bool Override;
        public static bool OriginalValue = true;

        public static bool SetValue(ItemDrop.ItemData item)
        {
            if (item != null && item.HasMagicEffect(MagicEffectType.Indestructible))
            {
                Override = true;
                OriginalValue = item.m_shared.m_useDurability;
                item.m_shared.m_useDurability = false;
            }

            return true;
        }

        public static void ResetValue(ItemDrop.ItemData item)
        {
            if (Override && item != null)
            {
                item.m_shared.m_useDurability = OriginalValue;
            }

            Override = false;
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.DoAreaAttack))]
        public static class Attack_DoAreaAttack_Patch
        {
            public static bool Prefix(Attack __instance) { return SetValue(__instance.m_weapon); }
            public static void Postfix(Attack __instance) { ResetValue(__instance.m_weapon); }
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        public static class Attack_DoMeleeAttack_Patch
        {
            public static bool Prefix(Attack __instance) { return SetValue(__instance.m_weapon); }
            public static void Postfix(Attack __instance) { ResetValue(__instance.m_weapon); }
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.DoNonAttack))]
        public static class Attack_DoNonAttack_Patch
        {
            public static bool Prefix(Attack __instance) { return SetValue(__instance.m_weapon); }
            public static void Postfix(Attack __instance) { ResetValue(__instance.m_weapon); }
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.ProjectileAttackTriggered))]
        public static class Attack_ProjectileAttackTriggered_Patch
        {
            public static bool Prefix(Attack __instance) { return SetValue(__instance.m_weapon); }
            public static void Postfix(Attack __instance) { ResetValue(__instance.m_weapon); }
        }

        //public void UpdateIcons(Player player)
        [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons))]
        public static class HotkeyBar_UpdateIcons_Patch
        {
            public static void Postfix(HotkeyBar __instance)
            {
                foreach (var itemData in __instance.m_items)
                {
                    var elementIndex = itemData.m_gridPos.x;
                    if (elementIndex >= 0 && elementIndex < __instance.m_elements.Count)
                    {
                        var element = __instance.m_elements[elementIndex];

                        if (itemData.HasMagicEffect(MagicEffectType.Indestructible))
                        {
                            element.m_durability.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
        public static class Humanoid_BlockAttack_Patch
        {
            public static bool Prefix(Humanoid __instance) { return SetValue(__instance.GetCurrentBlocker()); }
            public static void Postfix(Humanoid __instance) { ResetValue(__instance.GetCurrentBlocker()); }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        public static class Humanoid_EquipItem_Patch
        {
            public static bool Prefix(ItemDrop.ItemData item) { return SetValue(item); }
            public static void Postfix(ItemDrop.ItemData item) { ResetValue(item); }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipment))]
        public static class Humanoid_UpdateEquipment_Patch
        {
            public static bool Prefix(Humanoid __instance, ref List<KeyValuePair<ItemDrop.ItemData, bool>> __state)
            {
                if (!__instance.IsPlayer())
                {
                    return true;
                }

                var player = __instance as Player;
                __state = new List<KeyValuePair<ItemDrop.ItemData, bool>>();

                var equipment = player.GetEquipment();
                foreach (var itemData in equipment)
                {
                    if (itemData.HasMagicEffect(MagicEffectType.Indestructible))
                    {
                        __state.Add(new KeyValuePair<ItemDrop.ItemData, bool>(itemData, itemData.m_shared.m_useDurability));
                        itemData.m_shared.m_useDurability = false;
                    }
                }

                return true;
            }

            public static void Postfix(Humanoid __instance, List<KeyValuePair<ItemDrop.ItemData, bool>> __state)
            {
                if (!__instance.IsPlayer())
                {
                    return;
                }

                foreach (var pair in __state)
                {
                    pair.Key.m_shared.m_useDurability = pair.Value;
                }
            }
        }

        //public void GetWornItems(List<ItemDrop.ItemData> worn)
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.GetWornItems))]
        public static class Inventory_GetWornItems_Patch
        {
            public static void Postfix(List<ItemDrop.ItemData> worn)
            {
                worn.RemoveAll(x => x.HasMagicEffect(MagicEffectType.Indestructible));
            }
        }

        //public void UpdateIcons(Player player)
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        public static class InventoryGrid_UpdateGui_Patch
        {
            public static void Postfix(InventoryGrid __instance)
            {
                foreach (var item in __instance.m_inventory.m_inventory)
                {
                    var element = __instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, __instance.m_width);
                    if (element != null && item.HasMagicEffect(MagicEffectType.Indestructible))
                    {
                        element.m_durability.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.AddRecipeToList))]
        public static class InventoryGui_AddRecipeToList_Patch
        {
            public static bool Prefix(ItemDrop.ItemData item) { return SetValue(item); }
            public static void Postfix(ItemDrop.ItemData item) { ResetValue(item); }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupUpgradeItem))]
        public static class InventoryGui_SetupUpgradeItem_Patch
        {
            public static bool Prefix(ItemDrop.ItemData item) { return SetValue(item); }
            public static void Postfix(ItemDrop.ItemData item) { ResetValue(item); }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Repair))]
        public static class Player_Repair_Patch
        {
            public static bool Prefix(ItemDrop.ItemData toolItem) { return SetValue(toolItem); }
            public static void Postfix(ItemDrop.ItemData toolItem) { ResetValue(toolItem); }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
        public static class Player_UpdatePlacement_Patch
        {
            public static bool Prefix(Player __instance) { return SetValue(__instance.m_rightItem); }
            public static void Postfix(Player __instance) { ResetValue(__instance.m_rightItem); }
        }
    }
}
