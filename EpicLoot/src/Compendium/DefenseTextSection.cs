using System.Collections.Generic;
using EpicLoot.MagicItemEffects;
using UnityEngine;

namespace EpicLoot.Compendium;

public static class DefenseTextSection
{
    private const string MoreDamageColor = "#ff8a8aff";
    private const string LessDamageColor = "#8aff8aff";
    private const string NeutralColor = "#c0c0c0ff";
    private const string Dash = "<color=#808080ff>-</color>";

    private static readonly HitData.DamageType[] DamageTypes =
    {
        HitData.DamageType.Blunt,
        HitData.DamageType.Slash,
        HitData.DamageType.Pierce,
        HitData.DamageType.Fire,
        HitData.DamageType.Frost,
        HitData.DamageType.Lightning,
        HitData.DamageType.Poison,
        HitData.DamageType.Spirit,
    };

    public static void Build(MagicPages instance)
    {
        Player player = Player.m_localPlayer;
        if (!player)
        {
            return;
        }

        instance.MagicPagesTextArea.Add("");
        instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>$mod_epicloot_resistances</size>");
        MagicTextTable table = instance.MagicPagesTextArea.AddTable(
            new MagicTableColumn { Header = "$mod_epicloot_defense_col_type", Weight = 0.26f, Outline = true },
            new MagicTableColumn { Header = "$mod_epicloot_defense_col_modifier", Weight = 0.30f, Alignment = TextAnchor.MiddleCenter },
            new MagicTableColumn { Header = "$mod_epicloot_defense_col_resistance", Weight = 0.22f, Alignment = TextAnchor.MiddleCenter },
            new MagicTableColumn { Header = "$mod_epicloot_defense_col_taken", Weight = 0.22f, Alignment = TextAnchor.MiddleRight });

        HitData.DamageModifiers modifiers = player.GetDamageModifiers();
        HitData unitHit = new HitData
        {
            m_damage = new HitData.DamageTypes
            {
                m_blunt = 1f, m_slash = 1f, m_pierce = 1f, m_chop = 1f, m_pickaxe = 1f,
                m_fire = 1f, m_frost = 1f, m_lightning = 1f, m_poison = 1f, m_spirit = 1f,
            },
        };
        unitHit.ApplyResistance(modifiers, out _);
        HitData.DamageTypes unit = unitHit.m_damage;

        foreach (HitData.DamageType type in DamageTypes)
        {
            float vanillaFactor = GetDamage(unit, type);
            string specificEffect = ModifyResistance.GetResistanceEffect(type);
            string groupEffect = ModifyResistance.GetGroupResistanceEffect(type);
            float groupFraction = player.GetTotalActiveMagicEffectValue(groupEffect, 0.01f);
            float uncapped = ModifyResistance.GetUncappedResistanceFraction(player, specificEffect, groupFraction);
            float capped = ModifyResistance.GetResistanceFraction(player, specificEffect, groupFraction);
            float cap = ModifyResistance.GetResistanceCap(specificEffect);
            float factor = vanillaFactor * (1f - capped);

            List<string> detail = new();
            foreach (KeyValuePair<HitData.DamageModifier, string> source in GetVanillaSources(player, type))
            {
                detail.Add(Grey($"{GetModifierText(source.Key)} ({source.Value})"));
            }

            HitData.DamageModifier combined = modifiers.GetModifier(type);
            if (combined != HitData.DamageModifier.Normal && detail.Count == 0)
            {
                detail.Add(Grey(GetModifierText(combined)));
            }

            detail.AddRange(StatSourceLines.GetMagicSources(player, specificEffect));
            detail.AddRange(StatSourceLines.GetMagicSources(player, groupEffect));
            if (uncapped > cap)
            {
                detail.Add(Grey(string.Format(Localization.instance.Localize("$mod_epicloot_defense_capped"),
                    Mathf.RoundToInt(cap * 100f), Mathf.RoundToInt(uncapped * 100f))));
            }

            string color = factor > 1.001f ? MoreDamageColor : factor < 0.999f ? LessDamageColor : NeutralColor;
            string[] cells =
            {
                GetDamageTypeText(type),
                combined == HitData.DamageModifier.Normal ? Dash : GetModifierText(combined),
                capped > 0f ? $"{Mathf.RoundToInt(capped * 100f)}%" : Dash,
                $"<color={color}>{Mathf.RoundToInt(factor * 100f)}%</color>",
            };
            table.AddRow(cells, detail, true);
        }

        table.Finish();
    }

    private static string Grey(string text) => StatSourceLines.Grey(text);

    private static IEnumerable<KeyValuePair<HitData.DamageModifier, string>> GetVanillaSources(Player player,
        HitData.DamageType type)
    {
        foreach (ItemDrop.ItemData item in player.GetInventory().GetEquippedItems())
        {
            foreach (HitData.DamageModPair pair in item.m_shared.m_damageModifiers)
            {
                if (pair.m_type == type && pair.m_modifier != HitData.DamageModifier.Normal)
                {
                    yield return new KeyValuePair<HitData.DamageModifier, string>(pair.m_modifier, item.m_shared.m_name);
                }
            }
        }

        foreach (StatusEffect statusEffect in player.GetSEMan().GetStatusEffects())
        {
            if (statusEffect is not SE_Stats stats)
            {
                continue;
            }

            foreach (HitData.DamageModPair pair in stats.m_mods)
            {
                if (pair.m_type == type && pair.m_modifier != HitData.DamageModifier.Normal)
                {
                    yield return new KeyValuePair<HitData.DamageModifier, string>(pair.m_modifier, statusEffect.m_name);
                }
            }
        }
    }

    private static float GetDamage(HitData.DamageTypes damage, HitData.DamageType type)
    {
        switch (type)
        {
            case HitData.DamageType.Blunt: return damage.m_blunt;
            case HitData.DamageType.Slash: return damage.m_slash;
            case HitData.DamageType.Pierce: return damage.m_pierce;
            case HitData.DamageType.Chop: return damage.m_chop;
            case HitData.DamageType.Pickaxe: return damage.m_pickaxe;
            case HitData.DamageType.Fire: return damage.m_fire;
            case HitData.DamageType.Frost: return damage.m_frost;
            case HitData.DamageType.Lightning: return damage.m_lightning;
            case HitData.DamageType.Poison: return damage.m_poison;
            case HitData.DamageType.Spirit: return damage.m_spirit;
            default: return 1f;
        }
    }

    private static string GetDamageTypeText(HitData.DamageType type)
    {
        switch (type)
        {
            case HitData.DamageType.Blunt: return "$inventory_blunt";
            case HitData.DamageType.Slash: return "$inventory_slash";
            case HitData.DamageType.Pierce: return "$inventory_pierce";
            case HitData.DamageType.Chop: return "$inventory_chop";
            case HitData.DamageType.Pickaxe: return "$inventory_pickaxe";
            case HitData.DamageType.Fire: return "$inventory_fire";
            case HitData.DamageType.Frost: return "$inventory_frost";
            case HitData.DamageType.Lightning: return "$inventory_lightning";
            case HitData.DamageType.Poison: return "$inventory_poison";
            case HitData.DamageType.Spirit: return "$inventory_spirit";
            default: return type.ToString();
        }
    }

    private static string GetModifierText(HitData.DamageModifier modifier)
    {
        switch (modifier)
        {
            case HitData.DamageModifier.SlightlyResistant: return "$inventory_slightlyresistant";
            case HitData.DamageModifier.Resistant: return "$inventory_resistant";
            case HitData.DamageModifier.VeryResistant: return "$inventory_veryresistant";
            case HitData.DamageModifier.SlightlyWeak: return "$inventory_slightlyweak";
            case HitData.DamageModifier.Weak: return "$inventory_weak";
            case HitData.DamageModifier.VeryWeak: return "$inventory_veryweak";
            case HitData.DamageModifier.Immune: return "$inventory_immune";
            default: return modifier.ToString();
        }
    }
}
