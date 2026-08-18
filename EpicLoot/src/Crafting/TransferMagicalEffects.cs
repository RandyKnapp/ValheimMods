using EpicLoot.Config;
using EpicLoot.ShardStones;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EpicLoot.Crafting;

public static class TransferMagicalEffects
{
    private static bool IsDoCraft;
    private static ItemDrop.ItemData CraftedItem = null;
    private static List<ItemDrop.ItemData> ConsumedMagicItems = new ();

    private static void TransferMagicToCraftedItem(Player player)
    {
        if (!IsDoCraft || !ELConfig.TransferMagicItemToCrafts.Value || CraftedItem == null || ConsumedMagicItems.Count == 0) return;

        // Never clobber a crafted item that already carries magic. Fresh crafts are never pre-enchanted,
        // so this only skips items that already have enchants — e.g. upgrading a magic item, whose data
        // ItemDataManager (CopyCustomDataFromUpgradedItem) re-applies during AddItem, before this postfix.
        if (CraftedItem.GetMagicItem() != null)
            return;

        // Gather the magic items from every consumed ingredient, strongest first so that when two
        // items share an effect type the higher-rarity item's effect wins the exclusivity check.
        var sources = ConsumedMagicItems
            .Select(x => x.GetMagicItem())
            .Where(m => m != null)
            .OrderByDescending(m => m.Rarity)
            .ToList();
        if (sources.Count == 0)
            return;

        // Rarity is the highest among the sources (affects value-range display and background color;
        // each effect keeps its own stored value).
        var newMagicItem = new MagicItem { Rarity = sources[0].Rarity };

        // Bake the valid rolled effects from every source. Socketed effects are handled below with the
        // sockets themselves, so they are not folded in here (no double-dipping).
        foreach (var source in sources)
        {
            foreach (var effect in source.Effects)
            {
                var def = MagicItemEffectDefinitions.Get(effect.EffectType);
                if (def == null)
                    continue;

                // Same legality check the socket/rune system uses: allowed on this item type/rarity and
                // not violating exclusivity with what has already been added. checklootroll:false lets
                // no-roll (e.g. legendary) effects through when the item type allows them.
                if (!def.Requirements.CheckRequirements(CraftedItem, newMagicItem, effect.EffectType, checklootroll: false))
                    continue;

                newMagicItem.Effects.Add(new MagicItemEffect(effect.EffectType, effect.EffectValue));
            }
        }

        // Transfer sockets from the consumed item with the most sockets, keeping its shards/runestones
        // in place but re-resolved for the crafted item's type.
        var socketSource = sources
            .OrderByDescending(m => m.SocketCount)
            .ThenByDescending(m => m.Rarity)
            .FirstOrDefault();
        if (socketSource != null && socketSource.SocketCount > 0)
        {
            newMagicItem.SocketCount = socketSource.SocketCount; // capacity, including any open slots
            foreach (var socket in socketSource.Sockets)
            {
                if (socket == null)
                    continue;

                var carried = new SocketedEffect(null, socket.SourcePrefab, socket.SourceRarity)
                {
                    ShardType = socket.ShardType
                };

                // Shard sockets are left effect-less here: a shard's effect depends on the host item
                // type AND on what else the crafted item ends up carrying (same-color stacking decay),
                // so every shard is resolved together once the set is complete, below.
                if (socket.ShardType == ShardType.None && socket.Effect != null)
                {
                    // A runestone carries a fixed effect; keep it only if valid on the crafted item.
                    var def = MagicItemEffectDefinitions.Get(socket.Effect.EffectType);
                    if (def != null && def.Requirements.CheckRequirements(CraftedItem, newMagicItem, socket.Effect.EffectType, checklootroll: false))
                        carried.Effect = new MagicItemEffect(socket.Effect.EffectType, socket.Effect.EffectValue);
                }

                newMagicItem.Sockets.Add(carried);
            }

            ShardSocketManager.RecomputeSocketValues(CraftedItem, newMagicItem);
        }

        if (newMagicItem.Effects.Count == 0 && newMagicItem.SocketCount == 0)
            return; // nothing valid carried over; leave the crafted item mundane

        newMagicItem.DisplayName = MagicItemNames.GetNameForItem(CraftedItem, newMagicItem);
        API.WithChangeReason(API.ChangeReason.Transfer, () => CraftedItem.SaveMagicItem(newMagicItem));
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

    // DoCrafting adds the crafted item through the Vector2i overload; the bool overload merely delegates
    // to this one, so patching here is the single choke point that reliably captures the crafted item.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new []{typeof(string), typeof(int), typeof(int),
        typeof(int), typeof(long), typeof(string), typeof(Vector2i), typeof(bool)})]
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

            // Mirror vanilla's stack-decrement loop exactly, but record every item drawn from (not just
            // the one that finishes the requirement) so multi-quantity requirements spread across several
            // magic stacks carry over all of their effects.
            foreach (var itemData in __instance.m_inventory)
            {
                if (itemData.m_shared.m_name == name
                    && (itemQuality < 0 || itemData.m_quality == itemQuality)
                    && (!worldLevelBased || itemData.m_worldLevel >= Game.m_worldLevel))
                {
                    var num = Mathf.Min(itemData.m_stack, amount);
                    if (num <= 0) continue;
                    amount -= num;
                    CheckConsumedResource(itemData);
                    if (amount <= 0) break;
                }
            }
        }
    }
}