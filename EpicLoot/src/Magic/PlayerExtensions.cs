using EpicLoot.LegendarySystem;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot;

public static class PlayerExtensions
{
    /// <summary>
    /// Every magic item the player currently has equipped. This is the single source of equipped magic
    /// gear for effect totals, set bonuses, tooltips and shard socketing, so it is also where external
    /// equipment providers (extra slots, quick slots, equipped backpacks) are merged in --
    /// see <see cref="API.RegisterEquipmentProvider"/>.
    /// </summary>
    public static List<ItemDrop.ItemData> GetMagicEquipment(this Player player)
    {
        List<ItemDrop.ItemData> items = player.GetInventory().GetEquippedItems()
            .Where(x => x.IsMagic()).ToList();
        API.AppendProviderEquipment(player, items);
        return items;
    }

    /// <summary>
    /// DEPRECATED, DO NOT USE. Kept only because external mods patch it; nothing inside Epic Loot calls
    /// it, so patching this does not affect effect resolution -- use
    /// <see cref="API.RegisterEquipmentProvider"/> instead.
    /// </summary>
    public static List<ItemDrop.ItemData> GetEquipment(this Player player)
    {
        return player.GetMagicEquipment();
    }

    public static List<MagicItemEffect> GetAllActiveMagicEffects(this Player player, string effectType = null)
    {
        IEnumerable<MagicItemEffect> equipEffects = player.GetMagicEquipment()
            .Where(x => x.IsMagic())
            .SelectMany(x => x.GetMagicItem().GetEffects(effectType, includeSocketed: true));
        List<MagicItemEffect> setEffects = player.GetAllActiveSetMagicEffects(effectType);
        return equipEffects.Concat(setEffects).ToList();
    }

    public static List<MagicItemEffect> GetAllActiveSetMagicEffects(this Player player, string effectType = null)
    {
        List<MagicItemEffect> activeSetEffects = new List<MagicItemEffect>();
        HashSet<LegendarySetInfo> equippedSets = player.GetEquippedSets();
        foreach (LegendarySetInfo setInfo in equippedSets)
        {
            int count = player.GetMagicEquippedSetPieces(setInfo.ID).Count;
            foreach (SetBonusInfo setBonusInfo in setInfo.SetBonuses)
            {
                if (count >= setBonusInfo.Count && (effectType == null || setBonusInfo.Effect.Type == effectType))
                {
                    MagicItemEffect effect = new MagicItemEffect(setBonusInfo.Effect.Type, setBonusInfo.Effect.Values?.MinValue ?? MagicItemEffect.DefaultValue);
                    activeSetEffects.Add(effect);
                }
            }
        }

        return activeSetEffects;
    }

    public static HashSet<LegendarySetInfo> GetEquippedSets(this Player player)
    {
        HashSet<LegendarySetInfo> sets = new HashSet<LegendarySetInfo>();
        foreach (ItemDrop.ItemData itemData in player.GetMagicEquipment())
        {
            if (itemData.IsMagic(out MagicItem magicItem) && magicItem.IsLegendarySetItem())
            {
                if (UniqueLegendaryHelper.TryGetLegendarySetInfo(magicItem.SetID, out LegendarySetInfo setInfo, out ItemRarity rarity))
                {
                    sets.Add(setInfo);
                }
            }
        }

        return sets;
    }

    public static float GetTotalActiveMagicEffectValue(this Player player, string effectType,
        float scale = 1.0f, ItemDrop.ItemData ignoreThisItem = null)
    {
        // TryGetValue/Store rather than the Func overload: this runs several times per fixed tick from
        // GetMaxCarryWeight, ModifyStaminaRegen and GetArmor, and the closure that overload's delegate
        // captures would be allocated on every one of those calls even though nearly all of them hit.
        if (!EquipmentEffectCache.TryGetValue(player, effectType, out float? cached))
        {
            List<MagicItemEffect> allEffects = player.GetAllActiveMagicEffects(effectType);
            cached = allEffects.Count > 0 ? allEffects.Select(x => x.EffectValue).Sum() : (float?)null;
            EquipmentEffectCache.Store(player, effectType, cached);
        }

        float totalValue = scale * (cached ?? 0);

        if (ignoreThisItem != null && player.IsItemEquiped(ignoreThisItem) && ignoreThisItem.IsMagic(out MagicItem magicItem))
        {
            totalValue -= magicItem.GetTotalEffectValue(effectType, scale, includeSocketed: true);
        }

        return totalValue;
    }

    public static bool HasActiveMagicEffect(this Player player, string effectType, out float effectValue,
        float scale = 1.0f, ItemDrop.ItemData ignoreThisItem = null)
    {
        effectValue = GetTotalActiveMagicEffectValue(player, effectType, scale, ignoreThisItem);
        return effectValue != 0f;
    }

    public static bool HasActiveMagicEffect(this Player player, string effectType)
    {
        if (player == null) return false;
        List<MagicItemEffect> effects = player.GetAllActiveMagicEffects(effectType.ToString());

        return effects.Count > 0;
    }

    public static List<ItemDrop.ItemData> GetEquippedSetPieces(this Player player, string setName)
    {
        return player.GetInventory().GetEquippedItems().Where(x => x.IsPartOfSet(setName)).ToList();
    }

    public static List<ItemDrop.ItemData> GetMagicEquippedSetPieces(this Player player, string setName)
    {
        return player.GetMagicEquipment().Where(x => x.IsPartOfSet(setName)).ToList();
    }

    public static bool HasEquipmentOfType(this Player player, ItemDrop.ItemData.ItemType type)
    {
        return player.GetMagicEquipment().Exists(x => x != null && x.m_shared.m_itemType == type);
    }

    public static ItemDrop.ItemData GetEquipmentOfType(this Player player, ItemDrop.ItemData.ItemType type)
    {
        return player.GetMagicEquipment().FirstOrDefault(x => x != null && x.m_shared.m_itemType == type);
    }

    public static Player GetPlayerWithEquippedItem(ItemDrop.ItemData itemData)
    {
        // TODO: evaluate if this returns magic items of other players correctly
        // Hot path (called from ModifyArmor on every GetArmor): manual loop instead of a LINQ
        // FirstOrDefault(closure) so we don't allocate a capturing closure on every call.
        List<Player> players = Player.s_players;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].IsItemEquiped(itemData))
            {
                return players[i];
            }
        }

        return null;
    }
}
