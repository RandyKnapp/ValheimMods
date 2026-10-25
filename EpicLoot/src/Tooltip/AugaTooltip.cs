using System;
using EpicLoot.Crafting;
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
/// Auga's inventory and crafting tooltips. What is left for EpicLoot, read from the same tooltip data
/// (<see cref="ItemTooltip.GetData"/>):
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
        ItemTooltipData data = ItemTooltip.GetData(item, item.m_quality, MagicItem.ShowEffectDetails);
        if (data == null)
            return;

        bool isMagic = data.IsMagic;

        // Auga has already localized everything it set; what is set here must arrive localized.
        Auga.API.ComplexTooltip_SetTopic(complexTooltip, Localization.instance.Localize(data.Title));

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

        string subtitle = data.SetLabel != null
            ? $"<color={data.SetColor}>{data.SetLabel}</color>, {data.ItemTypeName}"
            : $"<color={data.RarityColor}><color={data.RarityColor}>{data.RarityName}</color> {data.ItemTypeName}</color>";
        Auga.API.ComplexTooltip_SetSubtitle(complexTooltip, Localization.instance.Localize(subtitle));

        // Differs from Auga's own only for unique legendaries, which carry their own lore.
        Auga.API.ComplexTooltip_SetDescription(complexTooltip, Localization.instance.Localize(data.Description));

        // Auga's inventory and crafting tooltips carry the scraped effect text already. EpicLoot's result
        // dialogs are Auga results panels, which do not, so the effects are added here - for those alone.
        if (complexTooltip.GetComponent<CraftSuccessDialog>() == null &&
            complexTooltip.GetComponent<AugmentChoiceDialog>() == null)
            return;

        Auga.API.ComplexTooltip_AddDivider(complexTooltip);
        GameObject textBox = Auga.API.ComplexTooltip_AddTwoColumnTextBox(complexTooltip);
        if (textBox != null)
        {
            Auga.API.TooltipTextBox_AddLine(textBox, TooltipRenderer.RenderEffects(data).Replace("\n\n", ""));
        }

        if (data.Sets.Count > 0)
        {
            GameObject setTextBox = Auga.API.ComplexTooltip_AddTwoColumnTextBox(complexTooltip);
            if (setTextBox != null)
            {
                Auga.API.TooltipTextBox_AddLine(setTextBox, TooltipRenderer.RenderSets(data));
            }
        }
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
            ItemTooltipData data = ItemTooltip.GetData(item, item.m_quality, false);
            if (data != null)
                Preprocess(magicItem, data, ref label, ref value);
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

    // Every value comes from the tooltip data, so Auga's lines show exactly what the vanilla-UI tooltip does.
    private static void Preprocess(MagicItem magicItem, ItemTooltipData data, ref string label, ref string value)
    {
        string magicColor = data.RarityColor;

        switch (label)
        {
            case "$item_durability":
            case "$item_weight":
            case "$item_armor":
                if (data.GetStat(label)?.Modified == true)
                    value = Colored(value, magicColor);
                return;

            case "$item_staminause":
            {
                TooltipStat bloodlust = data.GetStat(label, TooltipStat.Bloodlust);
                TooltipStat stamina = data.GetStat(label);
                if (bloodlust != null)
                {
                    value = $"<color=red>{bloodlust.Value:0.#}</color>";
                }
                // Spellsword changes the cost without the vanilla-UI tooltip colouring it; Auga's line is
                // coloured either way, as it always was.
                else if (stamina != null && (stamina.Modified || magicItem.HasEffect(MagicEffectType.SpellSword)))
                {
                    value = Colored($"{stamina.Value:0.#}", magicColor);
                }
                return;
            }

            case "$item_backstab":
            {
                TooltipStat backstab = data.GetStat(label);
                if (backstab?.Modified == true)
                    value = Colored($"{backstab.Value:0.#}x", magicColor);
                return;
            }

            case "$item_blockarmor":
            {
                TooltipStat blockPower = data.GetStat(label);
                if (blockPower?.Modified == true)
                {
                    value = Colored($"{blockPower.Value:0.#}", magicColor) +
                        $" <color={Auga.API.Brown3}>({blockPower.Secondary.GetValueOrDefault():0})</color>";
                }
                return;
            }

            case "$item_blockforce":
            {
                TooltipStat deflection = data.GetStat(label);
                if (deflection?.Modified == true)
                    value = Colored($"{deflection.Value:0.#}", magicColor);
                return;
            }

            case "$item_parrybonus":
            {
                TooltipStat parry = data.GetStat(label);
                if (parry?.Modified == true)
                    value = Colored($"{parry.Value:0.#}x", magicColor);
                return;
            }
        }

        if (value != null && TryGetDamageLine(data, label, out string damageValue))
        {
            value = damageValue;
            return;
        }

        // Auga writes the modifier lines whole, as the label with a null value.
        if (value == null)
        {
            string id = label.StartsWith("$item_movement_modifier") ? "$item_movement_modifier"
                : label.StartsWith("$item_eitrregen_modifier") ? "$item_eitrregen_modifier"
                : null;
            TooltipStat modifier = id != null ? data.GetStat(id) : null;
            if (modifier?.Modified == true)
                label = ReplaceFirstColoredValue(label, $"{modifier.Value * 100f:+0;-0}%", magicColor);
        }
    }

    /// <summary>
    /// Auga's damage lines carry item.GetDamage(), which EpicLoot raises only while the weapon is equipped.
    /// The value is the tooltip data's magic damage, in Auga's "N (min-max)" layout, in the magic colour where
    /// an effect touches that damage type.
    /// </summary>
    private static bool TryGetDamageLine(ItemTooltipData data, string label, out string value)
    {
        value = null;
        TooltipStat damage = label.StartsWith("$inventory_") ? data.GetStat(label) : null;
        if (damage == null)
            return false;

        string amount = Mathf.RoundToInt(damage.Value).ToString();
        value = (damage.Modified ? Colored(amount, data.RarityColor) : amount) +
            $" <color={Auga.API.Brown3}>({Mathf.RoundToInt(damage.Min.GetValueOrDefault())}-" +
            $"{Mathf.RoundToInt(damage.Max.GetValueOrDefault())})</color>";
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
