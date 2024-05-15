using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.Crafting;

public static class TransferMagicalEffects
{

    private static bool IsDoCraft;
    private static ItemDrop.ItemData CraftedItem = null;
    private static List<ItemDrop.ItemData> ConsumedMagicItems = new ();

    private static void TransferMagicToCraftedItem(Player player)
    {
        if (!IsDoCraft || !EpicLoot.TransferMagicItemToCrafts.Value || CraftedItem == null || ConsumedMagicItems.Count == 0) return;

        MagicItem highestTierMagicItem =  null;
        
        foreach (var itemData in ConsumedMagicItems)
        {
            var magicItem = itemData.GetMagicItem();
            if (magicItem == null)
                continue;
            if (highestTierMagicItem == null)
                highestTierMagicItem = magicItem;
            else
            {
                if (magicItem.Rarity > highestTierMagicItem.Rarity)
                    highestTierMagicItem = magicItem;
                else if (magicItem.Rarity == highestTierMagicItem.Rarity)
                    if (magicItem.Effects.Count > highestTierMagicItem.Effects.Count)
                        highestTierMagicItem = magicItem;
            }
        }

        if (highestTierMagicItem == null)
            return;

        IsDoCraft = false;
        CraftedItem.SaveMagicItem(highestTierMagicItem);
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    static class InventoryGuiDoCraftingPatch
    {
        [UsedImplicitly]
        static void Prefix(InventoryGui __instance)
        {
            IsDoCraft = true;
        }

        [UsedImplicitly]
        static void Postfix(InventoryGui __instance, Player player)
        {
            TransferMagicToCraftedItem(player);
            IsDoCraft = false;
            CraftedItem = null;
            ConsumedMagicItems = new List<ItemDrop.ItemData>();
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new []{typeof(string), typeof(int), typeof(int), typeof(int), typeof(long),typeof(string),typeof(bool)})]
    static class InventoryAddItemPatch
    {
        [UsedImplicitly]
        static void Postfix(Inventory __instance, ref ItemDrop.ItemData __result)
        {
            if (!IsDoCraft) return;
            
            CraftedItem = __result;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(string), typeof(int), typeof(int), typeof(bool) })]
    static class InventoryRemoveItemPatch
    {
        private static void CheckConsumedResource(ItemDrop.ItemData itemData)
        {
            if (!IsDoCraft) return;
            
            if (itemData.IsMagic())
                ConsumedMagicItems.Add(itemData);
        }

        [UsedImplicitly]
        public static void Prefix(Inventory __instance, string name, int amount, int itemQuality, bool worldLevelBased)
        {
            if (!IsDoCraft || __instance == null) return;

            foreach (var itemData in __instance.m_inventory)
            {
                if (itemData.m_shared.m_name == name 
                    && (itemQuality < 0 || itemData.m_quality == itemQuality) 
                    && (!worldLevelBased || itemData.m_worldLevel >= Game.m_worldLevel))
                {
                    var num = Mathf.Min(itemData.m_stack, amount);
                    amount -= num;
                    if (amount > 0) continue;
                    CheckConsumedResource(itemData);
                    break;
                }
            }
        }
    }
}