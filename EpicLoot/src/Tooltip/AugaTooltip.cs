using System;
using EpicLoot.Crafting;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using EpicLoot.MagicItemEffects.Shards;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

/// <summary>
/// EpicLoot's part of Project Auga's item tooltip (AugaUnity.ComplexTooltip), registered from
/// <see cref="EpicLoot.InitializeAuga"/>.
///
/// Auga builds each stat line itself from the ItemData getters, and afterwards copies the tail of vanilla
/// item.GetTooltip() - everything after the stat block that follows the weight line - into a text box of its
/// own. EpicLoot's GetTooltip patch therefore already puts the magic effects, sockets and set text into
/// Auga's inventory and crafting tooltips. What is left for EpicLoot:
///  - <see cref="OnItemTooltipCreated"/>: the decorated name, the rarity subtitle, the rarity background
///    behind the icon, the rarity icon of crafting materials, legendary descriptions, and the effect text on
///    EpicLoot's own result dialogs (Auga's results panel has no box for the scraped text);
///  - <see cref="PreprocessTooltipStat"/>: the magic colour and the magic values on the stat lines. Auga
///    reads GetDamage, which only carries magic damage while the weapon is equipped.
/// </summary>
public static class AugaTooltip
{
    private const string MagicBackgroundName = "magicItem";

    private static bool _listenerErrorLogged;
    private static bool _preprocessorErrorLogged;

    public static void OnItemTooltipCreated(GameObject complexTooltip, ItemDrop.ItemData item)
    {
        if (complexTooltip == null || item == null)
            return;

        try
        {
            ExtendItemTooltip(complexTooltip, item);
        }
        catch (Exception e)
        {
            // Thrown from inside Auga's tooltip event, where it would break the tooltip for good.
            if (!_listenerErrorLogged)
            {
                _listenerErrorLogged = true;
                EpicLoot.LogErrorForce($"Auga item tooltip for {item.m_shared?.m_name}: {e}");
            }
        }
    }

    private static void ExtendItemTooltip(GameObject complexTooltip, ItemDrop.ItemData item)
    {
        bool isMagic = item.IsMagic(out MagicItem magicItem);

        // Auga has already localized everything it set; what is set here must arrive localized.
        Auga.API.ComplexTooltip_SetTopic(complexTooltip, Localization.instance.Localize(item.GetDecoratedName()));

        Image magicBG = GetOrCreateMagicBackground(complexTooltip);
        if (magicBG != null)
        {
            magicBG.gameObject.SetActive(isMagic);
        }

        if (item.IsMagicCraftingMaterial())
        {
            int iconIndex = EpicLoot.GetRarityIconIndex(item.GetCraftingMaterialRarity());
            if (item.m_shared.m_icons != null && iconIndex >= 0 && iconIndex < item.m_shared.m_icons.Length)
            {
                Auga.API.ComplexTooltip_SetIcon(complexTooltip, item.m_shared.m_icons[iconIndex]);
            }
        }

        if (!isMagic)
            return;

        if (magicBG != null)
        {
            magicBG.color = item.GetRarityColor();
        }

        Auga.API.ComplexTooltip_SetIcon(complexTooltip, item.GetIcon());

        string itemTypeName = magicItem.GetItemTypeName(item.Extended());
        string setLabel = GetSetLabel(item);
        string subtitle = setLabel != null
            ? $"<color={EpicLoot.GetSetItemColor()}>{setLabel}</color>, {itemTypeName}"
            : $"<color={magicItem.GetColorString()}>{magicItem.GetRarityDisplay()} {itemTypeName}</color>";
        Auga.API.ComplexTooltip_SetSubtitle(complexTooltip, Localization.instance.Localize(subtitle));

        // Differs from Auga's own only for unique legendaries, which carry their own lore.
        Auga.API.ComplexTooltip_SetDescription(complexTooltip, Localization.instance.Localize(item.GetDescription()));

        // Auga's inventory and crafting tooltips carry the scraped effect text already. EpicLoot's result
        // dialogs are Auga results panels, which do not, so the effects are added here - for those alone.
        if (complexTooltip.GetComponent<CraftSuccessDialog>() == null &&
            complexTooltip.GetComponent<AugmentChoiceDialog>() == null)
            return;

        Auga.API.ComplexTooltip_AddDivider(complexTooltip);
        GameObject textBox = Auga.API.ComplexTooltip_AddTwoColumnTextBox(complexTooltip);
        if (textBox != null)
        {
            Auga.API.TooltipTextBox_AddLine(textBox, magicItem.GetTooltip().Replace("\n\n", ""));
        }

        if (item.IsSetItem())
        {
            GameObject setTextBox = Auga.API.ComplexTooltip_AddTwoColumnTextBox(complexTooltip);
            if (setTextBox != null)
            {
                Auga.API.TooltipTextBox_AddLine(setTextBox, item.GetSetTooltip());
            }
        }
    }

    // The set label the vanilla-UI tooltip heads a magic set item with (MagicTooltip.AddMagicSetLabel).
    private static string GetSetLabel(ItemDrop.ItemData item)
    {
        // A set rune carries a SetID too, but it is not a piece of the set.
        if (item.IsSetRune())
        {
            return "$mod_epicloot_setrune_label";
        }

        return item.IsMagicSetItem() ? UniqueLegendaryHelper.GetSetLabelToken(item.GetRarity()) : null;
    }

    /// <summary>
    /// The rarity background behind the item icon: in front of the icon background in Auga's inventory
    /// tooltip, behind the icon on a results panel. Made once per tooltip object and found again after.
    /// </summary>
    private static Image GetOrCreateMagicBackground(GameObject complexTooltip)
    {
        Transform itemBG = complexTooltip.transform.Find("Tooltip/IconHeader/IconBkg/Item");
        bool inFront = itemBG != null;
        if (itemBG == null)
        {
            itemBG = complexTooltip.transform.Find("InventoryElement/icon");
        }

        if (itemBG == null)
            return null;

        Transform parent = inFront ? itemBG : itemBG.parent;
        Transform existing = parent.Find(MagicBackgroundName);
        if (existing != null)
            return existing.GetComponent<Image>();

        Image itemBGImage = itemBG.GetComponent<Image>();
        if (itemBGImage == null)
            return null;

        Image magicBG = UnityEngine.Object.Instantiate(itemBGImage, parent);
        magicBG.name = MagicBackgroundName;
        foreach (Transform child in magicBG.transform)
        {
            UnityEngine.Object.Destroy(child.gameObject);
        }

        RectTransform rt = magicBG.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        magicBG.color = Color.white;
        magicBG.raycastTarget = false;
        magicBG.sprite = EpicLoot.GetMagicItemBgSprite();
        magicBG.gameObject.SetActive(true);

        if (!inFront)
        {
            rt.SetSiblingIndex(itemBG.GetSiblingIndex());
        }

        return magicBG;
    }

    /// <summary>
    /// Registered with Auga.API.ComplexTooltip_AddItemStatPreprocessor. Auga passes every stat line it builds
    /// (upgrade comparisons excepted) through each registered preprocessor in turn, as (label, value); a line
    /// that is all label (the eitr and movement modifier lines) comes with a null value. Never returns null:
    /// Auga hands the result straight to the next mod's preprocessor, which would throw on it.
    /// </summary>
    public static Tuple<string, string> PreprocessTooltipStat(ItemDrop.ItemData item, string label, string value)
    {
        if (item == null || label == null || !item.IsMagic(out MagicItem magicItem))
            return new Tuple<string, string>(label, value);

        try
        {
            Preprocess(item, magicItem, ref label, ref value);
        }
        catch (Exception e)
        {
            if (!_preprocessorErrorLogged)
            {
                _preprocessorErrorLogged = true;
                EpicLoot.LogErrorForce($"Auga tooltip line '{label}' for {item.m_shared?.m_name}: {e}");
            }
        }

        return new Tuple<string, string>(label, value);
    }

    private static void Preprocess(ItemDrop.ItemData item, MagicItem magicItem, ref string label, ref string value)
    {
        string magicColor = magicItem.GetColorString();

        switch (label)
        {
            case "$item_durability":
                if (magicItem.HasEffect(MagicEffectType.ModifyDurability))
                    value = Colored(value, magicColor);
                return;

            case "$item_weight":
                if (magicItem.HasEffect(MagicEffectType.ReduceWeight) || magicItem.HasEffect(MagicEffectType.Weightless))
                    value = Colored(value, magicColor);
                return;

            case "$item_armor":
                if (magicItem.HasEffect(MagicEffectType.ModifyArmor))
                    value = Colored(value, magicColor);
                return;

            case "$item_staminause":
                if (magicItem.HasEffect(MagicEffectType.Bloodlust))
                {
                    value = $"<color=red>{Bloodlust.GetBloodlustStamina():0.#}</color>";
                }
                else if (magicItem.HasEffect(MagicEffectType.ModifyAttackStaminaUse) || magicItem.HasEffect(MagicEffectType.SpellSword))
                {
                    float stamina = (1 - magicItem.GetTotalEffectValue(MagicEffectType.ModifyAttackStaminaUse, 0.01f)) *
                        item.m_shared.m_attack.m_attackStamina;
                    if (magicItem.HasEffect(MagicEffectType.SpellSword))
                        stamina = Spellsword.GetSpellswordAttackStamina(stamina);
                    value = Colored($"{stamina:0.#}", magicColor);
                }
                return;

            case "$item_backstab":
                if (magicItem.HasEffect(MagicEffectType.ModifyBackstab))
                {
                    float backstab = item.m_shared.m_backstabBonus *
                        (1.0f + magicItem.GetTotalEffectValue(MagicEffectType.ModifyBackstab, 0.01f));
                    value = Colored($"{backstab:0.#}x", magicColor);
                }
                return;

            case "$item_blockarmor":
            {
                float blockPower = MagicTooltip.GetBlockPowerValue(item, magicItem, item.m_quality, out bool hasModifiers);
                if (hasModifiers)
                {
                    value = Colored($"{blockPower:0.#}", magicColor) +
                        $" <color={Auga.API.Brown3}>({item.GetBlockPowerTooltip(item.m_quality):0})</color>";
                }
                return;
            }

            case "$item_blockforce":
            {
                float deflection = MagicTooltip.GetDeflectionForceValue(item, magicItem, item.m_quality, out bool hasModifiers);
                if (hasModifiers)
                    value = Colored($"{deflection:0.#}", magicColor);
                return;
            }

            case "$item_parrybonus":
            {
                float parry = MagicTooltip.GetParryBonusValue(item, magicItem, item.m_quality, out bool hasModifiers);
                if (hasModifiers)
                    value = Colored($"{parry:0.#}x", magicColor);
                return;
            }
        }

        if (value != null && TryGetDamageLine(item, magicItem, label, magicColor, out string damageValue))
        {
            value = damageValue;
            return;
        }

        if (value == null && label.StartsWith("$item_movement_modifier"))
        {
            float movement = MagicTooltip.GetMovementModifierValue(item, magicItem, out bool hasModifiers);
            if (hasModifiers)
                label = ReplaceFirstColoredValue(label, $"{movement * 100f:+0;-0}%", magicColor);
            return;
        }

        if (value == null && label.StartsWith("$item_eitrregen_modifier") && magicItem.HasEffect(MagicEffectType.ModifyEitrRegen))
        {
            float eitrRegen = ModifyPlayerRegen.GetModifiedRegenValue(item, MagicEffectType.ModifyEitrRegen, item.m_shared.m_eitrRegenModifier);
            label = ReplaceFirstColoredValue(label, $"{eitrRegen * 100f:+0;-0}%", magicColor);
        }
    }

    /// <summary>
    /// Auga's damage lines carry item.GetDamage(), which EpicLoot raises only while the weapon is equipped.
    /// The value is rebuilt from the same magic damage the vanilla-UI tooltip shows (MagicTooltip.AddDamages),
    /// in Auga's "N (min-max)" layout, in the magic colour where an effect touches that damage type.
    /// </summary>
    private static bool TryGetDamageLine(ItemDrop.ItemData item, MagicItem magicItem, string label, string magicColor, out string value)
    {
        value = null;
        if (!label.StartsWith("$inventory_") || Player.m_localPlayer == null)
            return false;

        HitData.DamageTypes damages = ModifyDamage.GetDamageWithMagicEffects(item);
        float eitrImbueSpirit = EitrImbueAttack.GetSpiritBonus(magicItem, damages);
        damages.m_spirit += eitrImbueSpirit;

        bool all = magicItem.HasEffect(MagicEffectType.ModifyDamage, true) ||
            CoinHoarder.GetDamageBonus(Player.m_localPlayer) > 0f ||
            magicItem.HasEffect(MagicEffectType.SpellSword);
        bool physical = all || magicItem.HasEffect(MagicEffectType.ModifyPhysicalDamage);
        bool elemental = all || magicItem.HasEffect(MagicEffectType.ModifyElementalDamage);

        float damage;
        bool isMagic;
        switch (label)
        {
            case "$inventory_damage": damage = damages.m_damage; isMagic = all; break;
            case "$inventory_blunt": damage = damages.m_blunt; isMagic = physical || magicItem.HasEffect(MagicEffectType.AddBluntDamage); break;
            case "$inventory_slash": damage = damages.m_slash; isMagic = physical || magicItem.HasEffect(MagicEffectType.AddSlashingDamage); break;
            case "$inventory_pierce": damage = damages.m_pierce; isMagic = physical || magicItem.HasEffect(MagicEffectType.AddPiercingDamage); break;
            case "$inventory_fire": damage = damages.m_fire; isMagic = elemental || magicItem.HasEffect(MagicEffectType.AddFireDamage); break;
            case "$inventory_frost": damage = damages.m_frost; isMagic = elemental || magicItem.HasEffect(MagicEffectType.AddFrostDamage); break;
            case "$inventory_lightning": damage = damages.m_lightning; isMagic = elemental || magicItem.HasEffect(MagicEffectType.AddLightningDamage); break;
            case "$inventory_poison": damage = damages.m_poison; isMagic = elemental || magicItem.HasEffect(MagicEffectType.AddPoisonDamage); break;
            case "$inventory_spirit":
                damage = damages.m_spirit;
                isMagic = elemental || eitrImbueSpirit > 0f || magicItem.HasEffect(MagicEffectType.AddSpiritDamage);
                break;
            default:
                return false;
        }

        Player.m_localPlayer.GetSkills().GetRandomSkillRange(out float min, out float max, item.m_shared.m_skillType);
        string amount = Mathf.RoundToInt(damage).ToString();
        value = (isMagic ? Colored(amount, magicColor) : amount) +
            $" <color={Auga.API.Brown3}>({Mathf.RoundToInt(damage * min)}-{Mathf.RoundToInt(damage * max)})</color>";
        return true;
    }

    private static string Colored(string text, string color) => $"<color={color}>{text}</color>";

    /// <summary>
    /// Auga writes the modifier lines as "$label: &lt;color=#XXXXXX&gt;+N%&lt;/color&gt; ($item_total: ...)";
    /// swaps the first coloured value for <paramref name="newValue"/> in the magic colour.
    /// </summary>
    private static string ReplaceFirstColoredValue(string line, string newValue, string color)
    {
        int open = line.IndexOf("<color=", StringComparison.Ordinal);
        if (open < 0)
            return line;
        int openEnd = line.IndexOf('>', open);
        int close = openEnd < 0 ? -1 : line.IndexOf("</color>", openEnd, StringComparison.Ordinal);
        if (close < 0)
            return line;

        return line.Substring(0, open) + Colored(newValue, color) + line.Substring(close + "</color>".Length);
    }
}
