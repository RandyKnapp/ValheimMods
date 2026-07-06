using EpicLoot.LegendarySystem;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot;

public static class PlayerExtensions
{
    public static List<ItemDrop.ItemData> GetMagicEquipment(this Player player)
    {
        List<ItemDrop.ItemData> items = player.GetInventory().GetEquippedItems()
            .Where(x => x.IsMagic()).ToList();
        return items;
    }

    /// <summary>
    /// DEPRECATED, DO NOT USE
    /// </summary>
    public static List<ItemDrop.ItemData> GetEquipment(this Player player)
    {
        return player.GetMagicEquipment();
    }

    public static List<MagicItemEffect> GetAllActiveMagicEffects(this Player player, string effectType = null)
    {
        IEnumerable<MagicItemEffect> equipEffects = player.GetMagicEquipment()
            .Where(x => x.IsMagic())
            .SelectMany(x => x.GetMagicItem().GetEffects(effectType));
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
        float totalValue = scale * (EquipmentEffectCache.Get(player, effectType, () =>
        {
            List<MagicItemEffect> allEffects = player.GetAllActiveMagicEffects(effectType);
            return allEffects.Count > 0 ? allEffects.Select(x => x.EffectValue).Sum() : null;
        }) ?? 0);

        if (ignoreThisItem != null && player.IsItemEquiped(ignoreThisItem) && ignoreThisItem.IsMagic(out MagicItem magicItem))
        {
            totalValue -= magicItem.GetTotalEffectValue(effectType, scale);
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
        return Player.s_players.FirstOrDefault(player => player.IsItemEquiped(itemData));
    }
}
