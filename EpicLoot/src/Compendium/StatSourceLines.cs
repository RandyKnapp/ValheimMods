using System;
using System.Collections.Generic;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;

namespace EpicLoot.Compendium;

public static class StatSourceLines
{
    public const string SourceColor = "#c0c0c0ff";

    public const string DormantColor = "#6f6f6fff";

    public static string Grey(string text) => $"<color={SourceColor}>{text}</color>";

    // Every effect type a section asks sources for during one page build. The Misc list shows the
    // effects on the player's gear that nothing asked for, so a row that was not built (no weapon
    // held, no blocker, no contributing effect) hands its effects back to Misc instead of hiding them.
    private static readonly HashSet<string> _shown = new();
    public static IReadOnlyCollection<string> Shown => _shown;
    public static void BeginBuild() => _shown.Clear();
    public static void MarkShown(string effectType)
    {
        if (!string.IsNullOrEmpty(effectType))
        {
            _shown.Add(effectType);
        }
    }

    public static bool IsDormant(string effectType) =>
        ModifyWithLowHealth.IsHealthCriticalEffect(effectType) && !ModifyWithLowHealth.PlayerHasLowHealth(Player.m_localPlayer);

    public static string Source(string effectType, string text)
    {
        return IsDormant(effectType)
            ? $"<color={DormantColor}><i>{text}</i></color>"
            : Grey(text);
    }

    public static IEnumerable<string> GetMagicSources(Player player, string effectType,
        Func<ItemDrop.ItemData, bool> include = null)
    {
        if (string.IsNullOrEmpty(effectType))
        {
            yield break;
        }

        MarkShown(effectType);
        foreach (ItemDrop.ItemData item in player.GetMagicEquipment())
        {
            if (include != null && !include(item))
            {
                continue;
            }

            foreach (MagicItemEffect effect in item.GetMagicItem().GetEffects(effectType, includeSocketed: true))
            {
                yield return Source(effectType, $"{MagicItem.GetEffectText(effect, item.GetRarity(), false)} ({item.GetDecoratedName()})");
            }
        }

        MagicItemEffectDefinition definition = MagicItemEffectDefinitions.Get(effectType);
        foreach (LegendarySetProgress progress in SetBonusEvaluator.GetEquippedSetProgress(player))
        {
            foreach (SetBonusInfo bonus in progress.Set.SetBonuses)
            {
                if (bonus.Effect == null || bonus.Effect.Type != effectType)
                {
                    continue;
                }

                ItemRarity? tier = progress.GetTier(bonus);
                if (tier.HasValue)
                {
                    float value = SetBonusEvaluator.GetBonusValue(bonus, tier.Value);
                    yield return Source(effectType, $"{MagicItem.GetEffectText(definition, value)} ({progress.Set.Name})");
                }
            }
        }
    }
}
