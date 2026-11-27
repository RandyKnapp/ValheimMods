using EpicLoot.LegendarySystem;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
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

    // While set, totals for this one effect type read as absent. The compendium measures what each effect
    // contributes to a weapon by re-running GetDamage with the effect suppressed; checked before the cache so
    // nothing cached is ever the suppressed value.
    public static string SuppressedEffectType;

    public static List<MagicItemEffect> GetAllActiveMagicEffects(this Player player, string effectType = null)
    {
        if (effectType != null && effectType == SuppressedEffectType)
        {
            return new List<MagicItemEffect>();
        }

        IEnumerable<MagicItemEffect> equipEffects = player.GetMagicEquipment()
            .SelectMany(x => x.GetMagicItem().GetEffects(effectType, includeSocketed: true));
        List<MagicItemEffect> setEffects = player.GetAllActiveSetMagicEffects(effectType);
        return equipEffects.Concat(setEffects).ToList();
    }

    public static List<MagicItemEffect> GetAllActiveSetMagicEffects(this Player player, string effectType = null)
    {
        List<MagicItemEffect> activeSetEffects = new List<MagicItemEffect>();
        foreach (LegendarySetProgress progress in SetBonusEvaluator.GetEquippedSetProgress(player))
        {
            foreach (SetBonusInfo setBonusInfo in progress.Set.SetBonuses)
            {
                if (setBonusInfo.Effect == null || (effectType != null && setBonusInfo.Effect.Type != effectType))
                {
                    continue;
                }

                // Each bonus applies at its own tier, so mixed-rarity pieces still count toward the set.
                ItemRarity? tier = progress.GetTier(setBonusInfo);
                if (tier.HasValue)
                {
                    activeSetEffects.Add(new MagicItemEffect(setBonusInfo.Effect.Type,
                        SetBonusEvaluator.GetBonusValue(setBonusInfo, tier.Value)));
                }
            }
        }

        return activeSetEffects;
    }

    public static float GetTotalActiveMagicEffectValue(this Player player, string effectType,
        float scale = 1.0f, ItemDrop.ItemData ignoreThisItem = null)
    {
        if (effectType == SuppressedEffectType)
        {
            return 0f;
        }

        float totalValue = scale * (GetCachedTotal(player, effectType) ?? 0);

        if (ignoreThisItem != null && player.IsItemEquiped(ignoreThisItem) && ignoreThisItem.IsMagic(out MagicItem magicItem))
        {
            totalValue -= magicItem.GetTotalEffectValue(effectType, scale, includeSocketed: true);
        }
        else if (ignoreThisItem == null)
        {
            // Only non-zero while a thrown weapon's projectile is landing (see HitSource.FiringWeapon).
            totalValue += MagicEffectsHelper.GetFiringWeaponAdjustment(player, effectType, scale);
        }

        return totalValue;
    }

    public static bool HasActiveMagicEffect(this Player player, string effectType, out float effectValue,
        float scale = 1.0f, ItemDrop.ItemData ignoreThisItem = null)
    {
        effectValue = GetTotalActiveMagicEffectValue(player, effectType, scale, ignoreThisItem);
        return effectValue != 0f;
    }

    // Presence rather than a non-zero total: an effect that rolled 0 still counts as present. Memoized like
    // the total, so it is safe on per-tick paths (GiantsBlood's GetDamageModifiers postfix, the status
    // effect indicators), where rebuilding the active-effect list on every call cost an equipment walk each.
    public static bool HasActiveMagicEffect(this Player player, string effectType)
    {
        if (player == null || effectType == SuppressedEffectType)
        {
            return false;
        }

        return GetCachedTotal(player, effectType).HasValue;
    }

    // The memoized sum of one effect type's active values: null when nothing provides it, a float (which
    // can be 0) when something does. TryGetValue/Store with no closure, since this runs several times per
    // fixed tick from GetMaxCarryWeight, ModifyStaminaRegen and GetArmor. Callers check
    // SuppressedEffectType first, so the suppressed value is never cached.
    private static float? GetCachedTotal(Player player, string effectType)
    {
        if (!EquipmentEffectCache.TryGetValue(player, effectType, out float? cached))
        {
            List<MagicItemEffect> allEffects = player.GetAllActiveMagicEffects(effectType);
            cached = allEffects.Count > 0 ? allEffects.Select(x => x.EffectValue).Sum() : (float?)null;
            EquipmentEffectCache.Store(player, effectType, cached);
        }

        return cached;
    }

    public static List<ItemDrop.ItemData> GetEquippedSetPieces(this Player player, string setName)
    {
        return player.GetInventory().GetEquippedItems().Where(x => x.IsPartOfSet(setName)).ToList();
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
        // Hot path (ModifyArmor, on every GetArmor): a loop rather than a capturing LINQ closure.
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
