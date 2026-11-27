using System;
using System.Collections.Generic;
using EpicLoot.MagicItemEffects;
using UnityEngine;
using static EpicLoot.Compendium.CompendiumFormat;

namespace EpicLoot.Compendium;

public static class ArmorTextSection
{
    private const float ParryWindowSeconds = 0.25f;

    public static void Build(MagicPages instance)
    {
        Player player = Player.m_localPlayer;
        if (!player)
        {
            return;
        }

        instance.MagicPagesTextArea.Add("");
        instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>$mod_epicloot_armor</size>");
        MagicTextTable table = AddStatTable(instance);

        AddArmor(player, table);
        AddBlock(player, table);
        AddParryWindow(player, table);
        AddBlockStamina(player, table);
        AddChance(player, table, "$mod_epicloot_armor_avoid", MagicEffectType.AvoidDamageTaken, true);
        AddChance(player, table, "$mod_epicloot_armor_reflect", MagicEffectType.ReflectDamage, false);
        AddChance(player, table, "$mod_epicloot_armor_staggerattacker", MagicEffectType.StaggerOnDamageTaken, false);
        table.Finish();
    }

    private static void AddArmor(Player player, MagicTextTable table)
    {
        float baseArmor = 0f;
        List<string> detail = new();
        float worldBase = Game.instance != null ? Game.instance.m_worldLevelGearBaseAC : 0f;
        foreach (ItemDrop.ItemData item in new[] { player.m_chestItem, player.m_legItem, player.m_helmetItem, player.m_shoulderItem })
        {
            if (item == null)
            {
                continue;
            }

            float armor = item.m_shared.m_armor + Mathf.Max(0, item.m_quality - 1) * item.m_shared.m_armorPerLevel +
                item.m_worldLevel * worldBase;
            if (armor <= 0f)
            {
                continue;
            }

            baseArmor += armor;
            detail.Add(StatSourceLines.Grey($"{item.m_shared.m_name} {Signed(armor, "0.#")}"));
        }

        float total = player.GetBodyArmor();
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyArmor));
        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyArmorLowHealth));
        AddRow(table, "$item_armor", OneDecimal(baseArmor), total - baseArmor, OneDecimal(total), detail, x => Signed(x, "0.#"), true);
    }

    private static void AddBlock(Player player, MagicTextTable table)
    {
        ItemDrop.ItemData blocker = player.GetCurrentBlocker();
        if (blocker == null)
        {
            return;
        }

        ItemDrop.ItemData otherWeapon = blocker == player.m_rightItem ? player.m_leftItem : player.m_rightItem;
        Func<ItemDrop.ItemData, bool> include = x => x != otherWeapon || !OffenseTextSection.IsWeapon(otherWeapon);
        int quality = blocker.m_quality;

        float baseBlock = blocker.m_shared.m_blockPower + Mathf.Max(0, quality - 1) * blocker.m_shared.m_blockPowerPerLevel;
        if (baseBlock > 0f)
        {
            float total = blocker.GetBaseBlockPower(quality);
            List<string> detail = new();
            detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyBlockPower, include));
            detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyBlockPowerLowHealth, include));
            detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.Duelist, include));
            AddRow(table, "$item_blockarmor", OneDecimal(baseBlock), total - baseBlock, OneDecimal(total), detail,
                x => Signed(x, "0.#"), true);

            float parryBase = blocker.m_shared.m_timedBlockBonus;
            if (parryBase > 1f)
            {
                float normal = ModifyBlock.GetBlockPowerValue(player, blocker, baseBlock, false);
                float parry = ModifyBlock.GetBlockPowerValue(player, blocker, baseBlock, true);
                float parryTotal = normal > 0f ? parryBase * parry / normal : parryBase;
                List<string> parryDetail = new();
                parryDetail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyParry, include));
                parryDetail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyParryLowHealth, include));
                AddRow(table, "$item_parrybonus", Times(parryBase), parryTotal - parryBase, Times(parryTotal), parryDetail,
                    x => Signed(x, "0.#") + "x", true);
            }
        }

        float baseForce = blocker.m_shared.m_deflectionForce + Mathf.Max(0, quality - 1) * blocker.m_shared.m_deflectionForcePerLevel;
        if (baseForce > 0f)
        {
            float total = blocker.GetDeflectionForce(quality);
            List<string> detail = new();
            detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyBlockForce, include));
            detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.Duelist, include));
            AddRow(table, "$item_blockforce", OneDecimal(baseForce), total - baseForce, OneDecimal(total), detail,
                x => Signed(x, "0.#"), true);
        }
    }

    private static void AddParryWindow(Player player, MagicTextTable table)
    {
        List<string> detail = new(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyParryWindow));
        if (detail.Count == 0)
        {
            return;
        }

        float extraMs = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyParryWindow);
        float baseMs = ParryWindowSeconds * 1000f;
        AddRow(table, "$mod_epicloot_armor_parrywindow", Millis(baseMs), extraMs, Millis(baseMs + extraMs), detail,
            x => string.Format(Localization.instance.Localize("$mod_epicloot_armor_ms"), Signed(x)), true);
    }

    private static void AddBlockStamina(Player player, MagicTextTable table)
    {
        float total = player.GetEquipmentBlockStaminaModifier();
        float magic = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyBlockStaminaUse, 0.01f);
        float baseValue = total + magic;
        List<string> detail = new();
        foreach (ItemDrop.ItemData item in player.GetInventory().GetEquippedItems())
        {
            if (!Mathf.Approximately(item.m_shared.m_blockStaminaModifier, 0f))
            {
                detail.Add(StatSourceLines.Grey($"{item.m_shared.m_name} {SignedPercent(item.m_shared.m_blockStaminaModifier)}"));
            }
        }

        detail.AddRange(StatSourceLines.GetMagicSources(player, MagicEffectType.ModifyBlockStaminaUse));
        if (detail.Count == 0 && Mathf.Approximately(total, 0f))
        {
            return;
        }

        AddRow(table, "$mod_epicloot_armor_blockstamina", SignedPercent(baseValue), -magic, SignedPercent(total), detail,
            SignedPercent, false);
    }

    private static void AddChance(Player player, MagicTextTable table, string name, string effectType, bool lowHealthVariant)
    {
        List<string> detail = new(StatSourceLines.GetMagicSources(player, effectType));
        float chance = player.GetTotalActiveMagicEffectValue(effectType, 0.01f);
        if (lowHealthVariant)
        {
            string lowHealth = ModifyWithLowHealth.EffectNameWithLowHealth(effectType);
            detail.AddRange(StatSourceLines.GetMagicSources(player, lowHealth));
            ModifyWithLowHealth.ApplyOnlyForLowHealth(player, effectType,
                effect => chance += player.GetTotalActiveMagicEffectValue(effect, 0.01f));
        }

        if (detail.Count == 0)
        {
            return;
        }

        AddRow(table, name, Percent(0f), chance, Percent(chance), detail, SignedPercent, true);
    }

    private static string Millis(float value) => string.Format(Localization.instance.Localize("$mod_epicloot_armor_ms"), Mathf.RoundToInt(value));
}
