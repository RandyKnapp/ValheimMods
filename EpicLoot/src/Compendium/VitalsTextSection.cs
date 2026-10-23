using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.MagicItemEffects;
using UnityEngine;

namespace EpicLoot.Compendium;

public static class VitalsTextSection
{
    private const string BetterColor = "#8aff8aff";
    private const string WorseColor = "#ff8a8aff";
    private const string NeutralColor = "#c0c0c0ff";
    private const string Dash = "<color=#808080ff>-</color>";

    private delegate void StatusRegen(StatusEffect effect, ref float multiplier);
    private delegate void ManagerRegen(SEMan manager, ref float multiplier);

    // Effects that reach a stat through their own patch rather than the flat effect the row is named
    // after. They are listed by their own effect text; only an unexplained remainder gets an "Other" line.
    private static readonly string[] HealthPoolExtras =
    {
        MagicEffectType.PercentHealth, MagicEffectType.BulkUp, MagicEffectType.BloodDrinker,
        MagicEffectType.ConvertHealthToEitr,
    };

    private static readonly string[] StaminaPoolExtras =
    {
        MagicEffectType.PercentStamina, MagicEffectType.GainMaxStaminaBasedOnPlayerMaxHealth,
        MagicEffectType.StaminaIncreaseForMovementPenalty, MagicEffectType.EveryXPointsOfEitrIncreasesStamina,
    };

    private static readonly string[] EitrPoolExtras =
    {
        MagicEffectType.PercentEitr, MagicEffectType.ConvertHealthToEitr, MagicEffectType.DeepWell,
        MagicEffectType.HeartyEitr, MagicEffectType.EnergeticEitr, MagicEffectType.DartingThoughts,
    };

    private static readonly string[] HealthRegenExtras =
    {
        MagicEffectType.ModifyHealthRegenLowHealth, MagicEffectType.DayHealthRegen, MagicEffectType.RestingHealthRegen,
        MagicEffectType.BloodMagicLevelIncreasesHealthRegen, MagicEffectType.BulkUp,
    };

    private static readonly string[] StaminaRegenExtras =
    {
        MagicEffectType.ModifyStaminaRegenLowHealth, MagicEffectType.DayStaminaRegen,
        MagicEffectType.NightStaminaRegenIncrease, MagicEffectType.StaminaRegenBonusFromPlayerWeight,
    };

    private static readonly string[] EitrRegenExtras =
    {
        MagicEffectType.ModifyEitrRegenLowHealth, MagicEffectType.DartingThoughts, MagicEffectType.DeepWell,
    };

    private static readonly string[] CarryWeightExtras =
    {
        MagicEffectType.NightCarryWeight, MagicEffectType.GainMaxCarryWeightFromRested,
        MagicEffectType.CarryWeightForMovementPenalty, MagicEffectType.TravelLight,
    };

    private static readonly string[] MovementExtras =
    {
        MagicEffectType.RemoveSpeedPenalty, MagicEffectType.ModifyMovementSpeedLowHealth,
    };

    public static void Build(MagicPages instance)
    {
        Player player = Player.m_localPlayer;
        if (!player)
        {
            return;
        }

        instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>$mod_epicloot_vitals</size>");
        MagicTextTable table = instance.MagicPagesTextArea.AddTable(
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_stat", Weight = 0.34f, Outline = true },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_base", Weight = 0.22f, Alignment = TextAnchor.MiddleCenter },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_epicloot", Weight = 0.22f, Alignment = TextAnchor.MiddleCenter },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_total", Weight = 0.22f, Alignment = TextAnchor.MiddleRight });

        AddPools(player, table);
        AddHealthRegen(player, table);
        AddRegenMultiplier(player, table, "$mod_epicloot_vitals_staminaregen", MagicEffectType.ModifyStaminaRegen,
            StaminaRegenExtras,
            (StatusEffect effect, ref float m) => effect.ModifyStaminaRegen(ref m),
            (SEMan manager, ref float m) => manager.ModifyStaminaRegen(ref m));
        AddRegenMultiplier(player, table, "$mod_epicloot_vitals_eitrregen", MagicEffectType.ModifyEitrRegen,
            EitrRegenExtras,
            (StatusEffect effect, ref float m) => effect.ModifyEitrRegen(ref m),
            (SEMan manager, ref float m) => manager.ModifyEitrRegen(ref m),
            item => item.m_shared.m_eitrRegenModifier);
        AddCarryWeight(player, table);
        AddMovement(player, table);
        table.Finish();
    }

    private static void AddPools(Player player, MagicTextTable table)
    {
        List<Player.Food> foods = player.GetFoods();
        float baseHealth = player.m_baseHP + foods.Sum(x => x.m_health);
        float baseStamina = player.m_baseStamina + foods.Sum(x => x.m_stamina);
        float baseEitr = foods.Sum(x => x.m_eitr);
        player.GetTotalFoodValue(out float health, out float stamina, out float eitr);

        AddPoolRow(player, table, "$mod_epicloot_vitals_health", player.m_baseHP, foods, x => x.m_health,
            baseHealth, health, MagicEffectType.IncreaseHealth, HealthPoolExtras);
        AddPoolRow(player, table, "$mod_epicloot_vitals_stamina", player.m_baseStamina, foods, x => x.m_stamina,
            baseStamina, stamina, MagicEffectType.IncreaseStamina, StaminaPoolExtras);
        AddPoolRow(player, table, "$mod_epicloot_vitals_eitr", 0f, foods, x => x.m_eitr,
            baseEitr, eitr, MagicEffectType.IncreaseEitr, EitrPoolExtras);
    }

    private static void AddPoolRow(Player player, MagicTextTable table, string name, float characterBase,
        List<Player.Food> foods, System.Func<Player.Food, float> foodValue, float baseValue, float total,
        string flatEffect, string[] extras)
    {
        List<string> detail = new();
        if (characterBase > 0f)
        {
            detail.Add(Grey($"$mod_epicloot_vitals_basevalue {Signed(characterBase)}"));
        }

        foreach (Player.Food food in foods)
        {
            float value = foodValue(food);
            if (value >= 0.5f)
            {
                detail.Add(Grey($"{food.m_item.m_shared.m_name} {Signed(value)}"));
            }
        }

        float remainder = total - baseValue - player.GetTotalActiveMagicEffectValue(flatEffect);
        AddMagicDetail(player, detail, new[] { flatEffect }, extras, remainder, 0.5f, x => Signed(x));
        AddRow(table, name, Whole(baseValue), total - baseValue, Whole(total), detail, x => Signed(x));
    }

    private static void AddHealthRegen(Player player, MagicTextTable table)
    {
        List<Player.Food> foods = player.GetFoods();
        float foodRegen = foods.Sum(x => x.m_item.m_shared.m_foodRegen);
        List<string> detail = new();
        foreach (Player.Food food in foods)
        {
            if (food.m_item.m_shared.m_foodRegen > 0f)
            {
                detail.Add(Grey($"{food.m_item.m_shared.m_name} {PerTick(food.m_item.m_shared.m_foodRegen)}"));
            }
        }

        float vanillaMultiplier = AddStatusLines(player, detail,
            (StatusEffect effect, ref float m) => effect.ModifyHealthRegen(ref m));
        float totalMultiplier = 1f;
        player.GetSEMan().ModifyHealthRegen(ref totalMultiplier);
        float flat = player.GetTotalActiveMagicEffectValue(MagicEffectType.AddHealthRegen);
        float baseRegen = foodRegen * vanillaMultiplier;
        float totalRegen = foodRegen > 0f ? (foodRegen + flat) * totalMultiplier : 0f;

        float remainder = totalMultiplier - vanillaMultiplier -
            ModifyPlayerRegen.GetModifyRegenValue(player, MagicEffectType.ModifyHealthRegen);
        AddMagicDetail(player, detail, new[] { MagicEffectType.AddHealthRegen, MagicEffectType.ModifyHealthRegen },
            HealthRegenExtras, remainder, 0.005f, SignedPercent);
        AddRow(table, "$mod_epicloot_vitals_healthregen", PerTick(baseRegen), totalRegen - baseRegen,
            PerTick(totalRegen), detail, x => Signed(x, "0.#"));
    }

    private static void AddRegenMultiplier(Player player, MagicTextTable table, string name, string effectType,
        string[] extras, StatusRegen statusRegen, ManagerRegen managerRegen, Func<ItemDrop.ItemData, float> gearRegen = null)
    {
        List<string> detail = new();
        float vanillaMultiplier = AddStatusLines(player, detail, statusRegen);
        float totalMultiplier = 1f;
        managerRegen(player.GetSEMan(), ref totalMultiplier);

        float remainder = totalMultiplier - vanillaMultiplier - ModifyPlayerRegen.GetModifyRegenValue(player, effectType);

        // Vanilla adds the worn gear's own regen modifier after the status effects (eitr only, in
        // Player.UpdateStats), so it is part of the base.
        if (gearRegen != null)
        {
            float gear = AddGearLines(player, detail, gearRegen);
            vanillaMultiplier += gear;
            totalMultiplier += gear;
        }

        AddMagicDetail(player, detail, new[] { effectType }, extras, remainder, 0.005f, SignedPercent);
        AddRow(table, name, Percent(vanillaMultiplier), totalMultiplier - vanillaMultiplier, Percent(totalMultiplier),
            detail, SignedPercent);
    }

    private static float AddStatusLines(Player player, List<string> detail, StatusRegen statusRegen)
    {
        float vanillaMultiplier = 1f;
        foreach (StatusEffect effect in player.GetSEMan().GetStatusEffects())
        {
            float multiplier = 1f;
            statusRegen(effect, ref multiplier);
            vanillaMultiplier += multiplier - 1f;
            if (!Mathf.Approximately(multiplier, 1f))
            {
                detail.Add(Grey($"{effect.m_name} {SignedPercent(multiplier - 1f)}"));
            }
        }

        return vanillaMultiplier;
    }

    // The slots Player.GetEquipmentEitrRegenModifier sums.
    private static float AddGearLines(Player player, List<string> detail, Func<ItemDrop.ItemData, float> gearRegen)
    {
        float total = 0f;
        foreach (ItemDrop.ItemData item in new[] { player.m_chestItem, player.m_legItem, player.m_helmetItem,
            player.m_shoulderItem, player.m_leftItem, player.m_rightItem, player.m_utilityItem })
        {
            float value = item != null ? gearRegen(item) : 0f;
            if (Mathf.Approximately(value, 0f))
            {
                continue;
            }

            total += value;
            detail.Add(Grey($"{item.m_shared.m_name} {SignedPercent(value)}"));
        }

        return total;
    }

    private static void AddCarryWeight(Player player, MagicTextTable table)
    {
        float baseLimit = player.m_maxCarryWeight;
        float vanillaLimit = baseLimit;
        List<string> detail = new() { Grey($"$mod_epicloot_vitals_basevalue {Whole(baseLimit)}") };
        foreach (StatusEffect effect in player.GetSEMan().GetStatusEffects())
        {
            float before = vanillaLimit;
            effect.ModifyMaxCarryWeight(baseLimit, ref vanillaLimit);
            if (!Mathf.Approximately(before, vanillaLimit))
            {
                detail.Add(Grey($"{effect.m_name} {Signed(vanillaLimit - before)}"));
            }
        }

        float total = player.GetMaxCarryWeight();
        float remainder = total - vanillaLimit - player.GetTotalActiveMagicEffectValue(MagicEffectType.AddCarryWeight);
        AddMagicDetail(player, detail, new[] { MagicEffectType.AddCarryWeight }, CarryWeightExtras, remainder, 0.5f,
            x => Signed(x));
        AddRow(table, "$mod_epicloot_vitals_carryweight", Whole(vanillaLimit), total - vanillaLimit, Whole(total),
            detail, x => Signed(x));
    }

    private static void AddMovement(Player player, MagicTextTable table)
    {
        float vanilla = 0f;
        List<string> detail = new();
        foreach (ItemDrop.ItemData item in player.GetInventory().GetEquippedItems())
        {
            float modifier = item.m_shared.m_movementModifier;
            if (!Mathf.Approximately(modifier, 0f))
            {
                vanilla += modifier;
                detail.Add(Grey($"{item.m_shared.m_name} {SignedPercent(modifier)}"));
            }
        }

        float total = player.GetEquipmentMovementModifier();
        float remainder = total - vanilla - player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyMovementSpeed, 0.01f);
        AddMagicDetail(player, detail, new[] { MagicEffectType.ModifyMovementSpeed }, MovementExtras, remainder, 0.005f,
            SignedPercent);
        AddRow(table, "$mod_epicloot_vitals_movement", SignedPercent(vanilla), total - vanilla, SignedPercent(total),
            detail, SignedPercent);
    }

    private static void AddMagicDetail(Player player, List<string> detail, string[] primaryEffects, string[] extras,
        float remainder, float threshold, System.Func<float, string> formatOther)
    {
        foreach (string effectType in primaryEffects)
        {
            detail.AddRange(StatSourceLines.GetMagicSources(player, effectType));
        }

        int before = detail.Count;
        foreach (string effectType in extras)
        {
            detail.AddRange(StatSourceLines.GetMagicSources(player, effectType));
        }

        if (detail.Count == before && Mathf.Abs(remainder) >= threshold)
        {
            detail.Add(Grey($"$mod_epicloot_vitals_other {formatOther(remainder)}"));
        }
    }

    private static void AddRow(MagicTextTable table, string name, string baseText, float magicDelta, string totalText,
        List<string> detail, System.Func<float, string> formatDelta)
    {
        bool neutral = Mathf.Abs(magicDelta) < 0.005f;
        string color = neutral ? NeutralColor : magicDelta > 0f ? BetterColor : WorseColor;
        string[] cells =
        {
            name,
            baseText,
            neutral ? Dash : formatDelta(magicDelta),
            $"<color={color}>{totalText}</color>",
        };
        table.AddRow(cells, detail, true);
    }

    private static string Grey(string text) => StatSourceLines.Grey(text);
    private static string Whole(float value) => Mathf.RoundToInt(value).ToString();
    private static string Percent(float fraction) => $"{Mathf.RoundToInt(fraction * 100f)}%";
    private static string SignedPercent(float fraction) => Signed(fraction * 100f) + "%";

    private static string Signed(float value, string format = "0")
    {
        string text = Mathf.Abs(value).ToString(format);
        return value < 0f ? $"-{text}" : $"+{text}";
    }

    private static string PerTick(float value) =>
        string.Format(Localization.instance.Localize("$mod_epicloot_vitals_pertick"), value.ToString("0.#"));
}
