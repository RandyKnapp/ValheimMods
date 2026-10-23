using System;
using System.Collections.Generic;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using EpicLoot.MagicItemEffects.Shards;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using UnityEngine;

namespace EpicLoot.Compendium;

public static class OffenseTextSection
{
    private const string BetterColor = "#8aff8aff";
    private const string WorseColor = "#ff8a8aff";
    private const string NeutralColor = "#c0c0c0ff";
    private const string Dash = "<color=#808080ff>-</color>";

    private class DamageRow
    {
        public string Key;
        public Func<HitData.DamageTypes, float> Get;
        public string AddEffect;
        public bool Physical;
        public bool Combat;
    }

    private static readonly DamageRow[] DamageRows =
    {
        new DamageRow { Key = "$inventory_damage", Get = x => x.m_damage, Combat = true },
        new DamageRow { Key = "$inventory_blunt", Get = x => x.m_blunt, AddEffect = MagicEffectType.AddBluntDamage, Physical = true, Combat = true },
        new DamageRow { Key = "$inventory_slash", Get = x => x.m_slash, AddEffect = MagicEffectType.AddSlashingDamage, Physical = true, Combat = true },
        new DamageRow { Key = "$inventory_pierce", Get = x => x.m_pierce, AddEffect = MagicEffectType.AddPiercingDamage, Physical = true, Combat = true },
        new DamageRow { Key = "$inventory_fire", Get = x => x.m_fire, AddEffect = MagicEffectType.AddFireDamage, Combat = true },
        new DamageRow { Key = "$inventory_frost", Get = x => x.m_frost, AddEffect = MagicEffectType.AddFrostDamage, Combat = true },
        new DamageRow { Key = "$inventory_lightning", Get = x => x.m_lightning, AddEffect = MagicEffectType.AddLightningDamage, Combat = true },
        new DamageRow { Key = "$inventory_poison", Get = x => x.m_poison, AddEffect = MagicEffectType.AddPoisonDamage, Combat = true },
        new DamageRow { Key = "$inventory_spirit", Get = x => x.m_spirit, AddEffect = MagicEffectType.AddSpiritDamage, Combat = true },
        new DamageRow { Key = "$inventory_chop", Get = x => x.m_chop, AddEffect = MagicEffectType.AddSlashingDamage, Physical = true },
        new DamageRow { Key = "$inventory_pickaxe", Get = x => x.m_pickaxe, AddEffect = MagicEffectType.AddPiercingDamage, Physical = true },
    };

    public static void Build(MagicPages instance)
    {
        Player player = Player.m_localPlayer;
        if (!player)
        {
            return;
        }

        List<ItemDrop.ItemData> weapons = new();
        if (IsWeapon(player.m_rightItem))
        {
            weapons.Add(player.m_rightItem);
        }

        if (IsWeapon(player.m_leftItem))
        {
            weapons.Add(player.m_leftItem);
        }

        if (weapons.Count == 0)
        {
            return;
        }

        instance.MagicPagesTextArea.Add("");
        instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>$mod_epicloot_offense</size>");
        foreach (ItemDrop.ItemData weapon in weapons)
        {
            BuildWeapon(instance, player, weapon);
        }
    }

    public static bool IsWeapon(ItemDrop.ItemData item)
    {
        if (item == null)
        {
            return false;
        }

        switch (item.m_shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
            case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
            case ItemDrop.ItemData.ItemType.Bow:
            case ItemDrop.ItemData.ItemType.Torch:
                return true;
            default:
                return false;
        }
    }

    private static void BuildWeapon(MagicPages instance, Player player, ItemDrop.ItemData weapon)
    {
        MagicItem magicItem = weapon.GetMagicItem();
        ItemDrop.ItemData otherWeapon = weapon == player.m_rightItem ? player.m_leftItem : player.m_rightItem;
        Func<ItemDrop.ItemData, bool> include = x => x != otherWeapon || !IsWeapon(otherWeapon);

        instance.MagicPagesTextArea.Add($"<size={MagicPages.MEDIUM_FONT_SIZE}>{weapon.GetDecoratedName()}</size>");
        MagicTextTable table = instance.MagicPagesTextArea.AddTable(
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_stat", Weight = 0.34f, Outline = true },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_base", Weight = 0.22f, Alignment = TextAnchor.MiddleCenter },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_epicloot", Weight = 0.22f, Alignment = TextAnchor.MiddleCenter },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_total", Weight = 0.22f, Alignment = TextAnchor.MiddleRight });

        AddDamageRows(player, table, weapon, magicItem, include);
        AddAttackSpeed(player, table);
        AddStagger(player, table, weapon, otherWeapon);
        AddLifeSteal(player, table, otherWeapon);
        AddLifeAfterKill(player, table, otherWeapon);
        AddBackstab(player, table, weapon, include);
        AddStamina(player, table, weapon, include);
        AddEitr(player, table, weapon, include);
        AddHealth(player, table, weapon, include);
        AddDrawStamina(player, table, weapon, magicItem);
        AddKnockback(player, table, weapon, include);
        table.Finish();
    }

    private class EffectSource
    {
        public float Value;
        public readonly List<string> Names = new();
    }

    private static void AddDamageRows(Player player, MagicTextTable table, ItemDrop.ItemData weapon, MagicItem magicItem,
        Func<ItemDrop.ItemData, bool> include)
    {
        HitData.DamageTypes baseDamage = weapon.Clone().GetDamage();
        HitData.DamageTypes total = Measure(weapon, magicItem, null);

        // Each effect's share of every damage type is the difference GetDamage shows with that one effect
        // suppressed, so conversions and multipliers are attributed without knowing how they work.
        Dictionary<string, EffectSource> sources = CollectSources(player, include);
        Dictionary<string, HitData.DamageTypes> contributions = new();
        foreach (string effectType in sources.Keys)
        {
            HitData.DamageTypes without = Measure(weapon, magicItem, effectType);
            contributions[effectType] = Diff(total, without);
        }

        List<DamageRow> shown = new();
        foreach (DamageRow row in DamageRows)
        {
            if (row.Get(baseDamage) > 0.05f || row.Get(total) > 0.05f)
            {
                shown.Add(row);
            }
        }

        int combatRows = 0;
        float baseSum = 0f;
        float totalSum = 0f;
        foreach (DamageRow row in shown)
        {
            if (!row.Combat)
            {
                continue;
            }

            combatRows++;
            baseSum += row.Get(baseDamage);
            totalSum += row.Get(total);
        }

        foreach (DamageRow row in shown)
        {
            float baseValue = row.Get(baseDamage);
            float totalValue = row.Get(total);
            List<string> detail = ContributionLines(sources, contributions, row.Get);
            AddRow(table, row.Key, Whole(baseValue), totalValue - baseValue, Whole(totalValue), detail, x => Signed(x), true);
        }

        if (combatRows > 1)
        {
            List<string> detail = ContributionLines(sources, contributions, x => CombatSum(x, shown));
            AddRow(table, "$mod_epicloot_offense_totaldamage", Whole(baseSum), totalSum - baseSum, Whole(totalSum), detail,
                x => Signed(x), true);
        }
    }

    private static HitData.DamageTypes Measure(ItemDrop.ItemData weapon, MagicItem magicItem, string suppressed)
    {
        PlayerExtensions.SuppressedEffectType = suppressed;
        try
        {
            HitData.DamageTypes damage = weapon.GetDamage();
            if (magicItem != null && suppressed != MagicEffectType.EitrImbueAttack)
            {
                damage.m_spirit += EitrImbueAttack.GetSpiritBonus(magicItem, damage);
            }

            return damage;
        }
        finally
        {
            PlayerExtensions.SuppressedEffectType = null;
        }
    }

    private static Dictionary<string, EffectSource> CollectSources(Player player, Func<ItemDrop.ItemData, bool> include)
    {
        Dictionary<string, EffectSource> sources = new();
        void Add(string effectType, float value, string name)
        {
            if (!sources.TryGetValue(effectType, out EffectSource source))
            {
                source = new EffectSource();
                sources.Add(effectType, source);
            }

            source.Value += value;
            if (!source.Names.Contains(name))
            {
                source.Names.Add(name);
            }
        }

        foreach (ItemDrop.ItemData item in player.GetMagicEquipment())
        {
            if (!include(item))
            {
                continue;
            }

            foreach (MagicItemEffect effect in item.GetMagicItem().GetEffects(null, includeSocketed: true))
            {
                Add(effect.EffectType, effect.EffectValue, item.GetDecoratedName());
            }
        }

        foreach (LegendarySetProgress progress in SetBonusEvaluator.GetEquippedSetProgress(player))
        {
            foreach (SetBonusInfo bonus in progress.Set.SetBonuses)
            {
                ItemRarity? tier = bonus.Effect == null ? null : progress.GetTier(bonus);
                if (tier.HasValue)
                {
                    Add(bonus.Effect.Type, SetBonusEvaluator.GetBonusValue(bonus, tier.Value), progress.Set.Name);
                }
            }
        }

        return sources;
    }

    private static List<string> ContributionLines(Dictionary<string, EffectSource> sources,
        Dictionary<string, HitData.DamageTypes> contributions, Func<HitData.DamageTypes, float> get)
    {
        List<(float delta, string line)> lines = new();
        foreach (KeyValuePair<string, HitData.DamageTypes> entry in contributions)
        {
            float delta = get(entry.Value);
            if (Mathf.Abs(delta) < 0.05f)
            {
                continue;
            }

            StatSourceLines.MarkShown(entry.Key);
            EffectSource source = sources[entry.Key];
            MagicItemEffectDefinition definition = MagicItemEffectDefinitions.Get(entry.Key);
            string text = definition != null ? MagicItem.GetEffectText(definition, source.Value) : entry.Key;
            lines.Add((delta, StatSourceLines.Grey($"{Signed(delta, "0.#")}  {text} ({string.Join(", ", source.Names)})")));
        }

        lines.Sort((a, b) => Mathf.Abs(b.delta).CompareTo(Mathf.Abs(a.delta)));
        return lines.ConvertAll(x => x.line);
    }

    private static float CombatSum(HitData.DamageTypes damage, List<DamageRow> rows)
    {
        float sum = 0f;
        foreach (DamageRow row in rows)
        {
            if (row.Combat)
            {
                sum += row.Get(damage);
            }
        }

        return sum;
    }

    private static HitData.DamageTypes Diff(HitData.DamageTypes a, HitData.DamageTypes b)
    {
        return new HitData.DamageTypes
        {
            m_damage = a.m_damage - b.m_damage,
            m_blunt = a.m_blunt - b.m_blunt,
            m_slash = a.m_slash - b.m_slash,
            m_pierce = a.m_pierce - b.m_pierce,
            m_chop = a.m_chop - b.m_chop,
            m_pickaxe = a.m_pickaxe - b.m_pickaxe,
            m_fire = a.m_fire - b.m_fire,
            m_frost = a.m_frost - b.m_frost,
            m_lightning = a.m_lightning - b.m_lightning,
            m_poison = a.m_poison - b.m_poison,
            m_spirit = a.m_spirit - b.m_spirit,
        };
    }

    private static void AddAttackSpeed(Player player, MagicTextTable table)
    {
        double speed = 1.0;
        ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyAttackSpeed, effect =>
        {
            speed *= 1.0 + player.GetTotalActiveMagicEffectValue(effect, 0.01f);
        });

        List<string> detail = new();
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyAttackSpeed));
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyAttackSpeedLowHealth));
        float total = (float)speed;
        AddRow(table, "$mod_epicloot_offense_attackspeed", Percent(1f), total - 1f, Percent(total), detail, SignedPercent, true);
    }

    private static void AddStagger(Player player, MagicTextTable table, ItemDrop.ItemData weapon, ItemDrop.ItemData otherWeapon)
    {
        ItemDrop.ItemData ignore = IsWeapon(otherWeapon) ? otherWeapon : null;
        Func<ItemDrop.ItemData, bool> include = x => x != ignore;

        float staggerDamage = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyStaggerDamage, 0.01f, ignore);
        List<string> damageDetail = new(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyStaggerDamage, include));
        AddRow(table, "$mod_epicloot_offense_staggerdamage", Percent(1f), staggerDamage, Percent(1f + staggerDamage),
            damageDetail, SignedPercent, true);

        float staggerDuration = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyStaggerDuration, 0.01f);
        List<string> durationDetail = new(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyStaggerDuration));
        AddRow(table, "$mod_epicloot_offense_staggerduration", Percent(1f), staggerDuration, Percent(1f + staggerDuration),
            durationDetail, SignedPercent, true);
    }

    private static void AddLifeSteal(Player player, MagicTextTable table, ItemDrop.ItemData otherWeapon)
    {
        ItemDrop.ItemData ignore = IsWeapon(otherWeapon) ? otherWeapon : null;
        Func<ItemDrop.ItemData, bool> include = x => x != ignore;
        List<string> detail = new();
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.LifeSteal, include));
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.LifeStealLowHealth, include));
        if (detail.Count == 0)
        {
            return;
        }

        float total = 0f;
        ModifyWithLowHealth.Apply(player, MagicEffectType.LifeSteal,
            effect => total += player.GetTotalActiveMagicEffectValue(effect, 0.01f, ignore));
        AddRow(table, "$mod_epicloot_offense_lifesteal", Percent(0f), total, Percent(total), detail, SignedPercent, true);
    }

    private static void AddLifeAfterKill(Player player, MagicTextTable table, ItemDrop.ItemData otherWeapon)
    {
        ItemDrop.ItemData ignore = IsWeapon(otherWeapon) ? otherWeapon : null;
        List<string> detail = new(StatSourceLines.GetMagicSources(player, MagicEffectType.LifeAfterKill, x => x != ignore));
        if (detail.Count == 0)
        {
            return;
        }

        float total = player.GetTotalActiveMagicEffectValue(MagicEffectType.LifeAfterKill, 1f, ignore);
        AddRow(table, "$mod_epicloot_offense_lifeafterkill", Decimal(0f), total, Decimal(total), detail, x => Signed(x, "0.#"), true);
    }

    private static void AddBackstab(Player player, MagicTextTable table, ItemDrop.ItemData weapon,
        Func<ItemDrop.ItemData, bool> include)
    {
        float baseValue = weapon.m_shared.m_backstabBonus;
        float total = baseValue * (1f + WeaponEffect(player, weapon, MagicEffectType.ModifyBackstab));
        List<string> detail = new();
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyBackstab, include));
        AddRow(table, "$item_backstab", Times(baseValue), total - baseValue, Times(total), detail, x => Signed(x, "0.#") + "x", true);
    }

    private static void AddStamina(Player player, MagicTextTable table, ItemDrop.ItemData weapon,
        Func<ItemDrop.ItemData, bool> include)
    {
        float baseValue = weapon.m_shared.m_attack.m_attackStamina;
        bool bloodlust = Bloodlust.HasBloodlust(player, weapon, weapon.m_shared.m_attack);
        if (baseValue <= 0f && !bloodlust)
        {
            return;
        }

        float total;
        if (bloodlust)
        {
            total = Bloodlust.GetBloodlustStamina();
        }
        else
        {
            total = (1f - WeaponEffect(player, weapon, MagicEffectType.ModifyAttackStaminaUse)) * baseValue;
            if (HasSpellsword(player, weapon))
            {
                total = Spellsword.GetSpellswordAttackStamina(total);
            }
        }

        List<string> detail = new();
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyAttackStaminaUse, include));
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.Bloodlust, include));
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.SpellSword, include));
        AddRow(table, "$item_staminause", Decimal(baseValue), total - baseValue, Decimal(total), detail, x => Signed(x, "0.#"), false);
    }

    private static void AddEitr(Player player, MagicTextTable table, ItemDrop.ItemData weapon,
        Func<ItemDrop.ItemData, bool> include)
    {
        float baseValue = weapon.m_shared.m_attack.m_attackEitr;
        // An Eitr Infusion waives Spellsword's extra eitr while it lasts, as it does in game.
        bool spellsword = HasSpellsword(player, weapon) && !EitrInfusion.IsInfused;
        List<string> detail = new();
        float total;
        if (ConjuredArrows.TryGetShotCost(weapon, out float conjuredCost))
        {
            total = conjuredCost;
            detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ConjuredArrows));
        }
        else
        {
            if (baseValue <= 0f && !spellsword)
            {
                return;
            }

            float cost = baseValue;
            if (spellsword)
            {
                cost += Spellsword.GetAdditionalSpellswordAttackEitr(weapon.m_shared.m_attack.m_attackStamina);
            }

            total = (1f - WeaponEffect(player, weapon, MagicEffectType.ModifyAttackEitrUse)) * cost;
            detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyAttackEitrUse, include));
            detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.SpellSword, include));
        }

        AddRow(table, "$item_eitruse", Decimal(baseValue), total - baseValue, Decimal(total), detail, x => Signed(x, "0.#"), false);
    }

    private static void AddHealth(Player player, MagicTextTable table, ItemDrop.ItemData weapon,
        Func<ItemDrop.ItemData, bool> include)
    {
        float baseValue = weapon.m_shared.m_attack.m_attackHealth;
        bool bloodlust = Bloodlust.HasBloodlust(player, weapon, weapon.m_shared.m_attack);
        if (baseValue <= 0f && !bloodlust)
        {
            return;
        }

        float cost = bloodlust ? Bloodlust.GetBloodlustHealth(baseValue, weapon.m_shared.m_attack.m_attackStamina) : baseValue;
        float total = ModifyAttackCosts.GetEffectPercentage(WeaponEffect(player, weapon, MagicEffectType.ModifyAttackHealthUse)) * cost;
        List<string> detail = new();
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyAttackHealthUse, include));
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.Bloodlust, include));
        AddRow(table, "$item_healthuse", Decimal(baseValue), total - baseValue, Decimal(total), detail, x => Signed(x, "0.#"), false);
    }

    // Matches ModifyDrawStamina: the bow's own enchantments, plus set bonuses on the bow the player holds.
    // Other worn items do not count here, unlike the attack costs.
    private static void AddDrawStamina(Player player, MagicTextTable table, ItemDrop.ItemData weapon, MagicItem magicItem)
    {
        float baseValue = weapon.m_shared.m_attack.m_drawStaminaDrain;
        if (baseValue <= 0f)
        {
            return;
        }

        float modifier = (magicItem?.GetTotalEffectValue(MagicEffectType.ModifyDrawStaminaUse, 0.01f) ?? 0f) +
            MagicEffectsHelper.GetTotalActiveSetEffectValue(player, MagicEffectType.ModifyDrawStaminaUse, 0.01f);
        float total = (1f - Mathf.Clamp(modifier, 0f, 1f)) * baseValue;
        List<string> detail = new();
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyDrawStaminaUse,
            x => x == weapon));
        AddRow(table, "$item_staminahold", PerSecond(baseValue), total - baseValue, PerSecond(total), detail,
            x => Signed(x, "0.#"), false);
    }

    private static void AddKnockback(Player player, MagicTextTable table, ItemDrop.ItemData weapon,
        Func<ItemDrop.ItemData, bool> include)
    {
        float baseValue = weapon.m_shared.m_attackForce;
        if (baseValue <= 0f)
        {
            return;
        }

        float total = baseValue * (1f - Mathf.Clamp01(WeaponEffect(player, weapon, MagicEffectType.CloseQuarter)));
        List<string> detail = new();
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.CloseQuarter, include));
        AddRow(table, "$item_knockback", Decimal(baseValue), total - baseValue, Decimal(total), detail, x => Signed(x, "0.#"), true);
    }

    // What the game charges or grants this weapon: every worn item and active set bonus, less the weapon in the
    // other hand, as the cost, backstab and Close Quarter patches read it.
    private static float WeaponEffect(Player player, ItemDrop.ItemData weapon, string effectType) =>
        MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, weapon, effectType, 0.01f);

    private static bool HasSpellsword(Player player, ItemDrop.ItemData weapon) =>
        Spellsword.IsSpellswordWeapon(weapon) &&
        MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, weapon, MagicEffectType.SpellSword, out _);

    private static void AddRow(MagicTextTable table, string name, string baseText, float magicDelta, string totalText,
        List<string> detail, Func<float, string> formatDelta, bool higherIsBetter)
    {
        bool neutral = Mathf.Abs(magicDelta) < 0.005f;
        bool better = higherIsBetter ? magicDelta > 0f : magicDelta < 0f;
        string color = neutral ? NeutralColor : better ? BetterColor : WorseColor;
        string[] cells =
        {
            name,
            baseText,
            neutral ? Dash : formatDelta(magicDelta),
            $"<color={color}>{totalText}</color>",
        };
        table.AddRow(cells, detail, true);
    }

    private static string Whole(float value) => Mathf.RoundToInt(value).ToString();
    private static string Decimal(float value) => value.ToString("0.#");
    private static string Times(float value) => value.ToString("0.#") + "x";
    private static string Percent(float fraction) => $"{Mathf.RoundToInt(fraction * 100f)}%";
    private static string SignedPercent(float fraction) => Signed(fraction * 100f) + "%";

    private static string Signed(float value, string format = "0")
    {
        string text = Mathf.Abs(value).ToString(format);
        return value < 0f ? $"-{text}" : $"+{text}";
    }

    private static string PerSecond(float value) =>
        string.Format(Localization.instance.Localize("$mod_epicloot_vitals_persecond"), value.ToString("0.#"));
}
