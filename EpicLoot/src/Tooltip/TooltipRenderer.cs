using System;
using System.Text;

namespace EpicLoot;

/// <summary>
/// Draws <see cref="ItemTooltipData"/> as tooltip text. It computes nothing Epic Loot changes: every value magic
/// touches comes from the data, so what a tooltip data editor changed is what is drawn. The plain vanilla lines
/// (crafter, teleport, food, ...) are still read off the item here, since they are not part of the data.
/// </summary>
/// <remarks>
/// Text is built with AppendLine where the old builders used it: under Mono on Windows that is "\r\n", and other
/// mods parse this text, so the line endings are part of what must not change.
/// </remarks>
public partial class TooltipRenderer
{
    private const string InactiveColor = "#808080ff";
    private const string DetailColor = "#c0c0c0ff";

    private readonly ItemDrop.ItemData item;
    private readonly ItemTooltipData data;
    private readonly int qualityLevel;
    private readonly string magicColor;
    private readonly Player localPlayer;
    private readonly StringBuilder text = new StringBuilder(1024);

    private TooltipRenderer(ItemDrop.ItemData item, ItemTooltipData data)
    {
        this.item = item;
        this.data = data;
        qualityLevel = data.Quality;
        magicColor = data.RarityColor;
        localPlayer = Player.m_localPlayer;
    }

    /// <summary>The whole tooltip of an item with magic data, before <see cref="Finish"/>.</summary>
    public static string RenderMagicItem(ItemDrop.ItemData item, ItemTooltipData data)
    {
        return new TooltipRenderer(item, data).RenderMagicItem();
    }

    private string RenderMagicItem()
    {
        if (data.Kind == TooltipKind.SetRune)
        {
            AddSetRuneTooltip();
            return text.ToString();
        }

        AddSetLabel();
        AddDescription();
        AddExtra(TooltipAnchor.AfterHeader);
        text.Append("\n");

        AddDLC();
        AddNewGamePlus();
        AddSubtitle();
        ItemDrop.ItemData.AddHandedTip(item, text);
        AddCrafterName();
        AddTeleportable();
        AddValuable();
        AddWeight();
        AddQuality();
        AddDurability();

        switch (item.m_shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.Consumable:
                if (item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f || item.m_shared.m_foodEitr > 0f)
                {
                    AddFoodHealth();
                    AddFoodStamina();
                    AddFoodEitr();
                    AddFoodBurn();
                    AddFoodRegen();
                }
                break;
            case ItemDrop.ItemData.ItemType.OneHandedWeapon:
            case ItemDrop.ItemData.ItemType.Bow:
            case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
            case ItemDrop.ItemData.ItemType.Torch:
            case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                AddDamages();
                AddDamageMultiplierByTotalHealthMissing();
                AddDamageMultiplierPerMissingHP();
                AddAttackStaminaUse();
                AddEitrUse();
                AddHealthUse();
                AddHealthHitReturn();
                AddHealthUsePercentage();
                AddDrawStaminaUse();

                AddBlockArmor();
                AddBlockForceAndParry();
                AddParryAdrenaline();

                // weapons typically do not have damage modifiers, but perhaps other mods utilize this
                AddDamageModifiers();

                AddKnockback();
                AddBackstab();
                AddTameOnly();
                AddProjectileTooltip();
                break;
            case ItemDrop.ItemData.ItemType.Shield:
                AddBlockArmor();
                AddBlockForceAndParry();
                AddParryAdrenaline();
                AddDamageModifiers();
                break;
            case ItemDrop.ItemData.ItemType.Helmet:
            case ItemDrop.ItemData.ItemType.Chest:
            case ItemDrop.ItemData.ItemType.Legs:
            case ItemDrop.ItemData.ItemType.Shoulder:
                AddArmor();
                AddDamageModifiers();
                break;
            case ItemDrop.ItemData.ItemType.Ammo:
            case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                AddDamages();
                AddKnockback();
                break;
        }

        AddStatusEffectTooltip();
        AddChainTooltip();
        AddEquipmentModifiers();
        AddExtra(TooltipAnchor.AfterStats);

        if (data.Kind == TooltipKind.Shard)
        {
            AddShardPreview();
        }
        else
        {
            text.AppendLine(RenderEffects(data));
        }

        AddExtra(TooltipAnchor.AfterEffects);
        text.Append(RenderSets(data));
        AddExtra(TooltipAnchor.AfterSet);
        AddAdrenalineStatusEffectTooltip();
        AddExtra(TooltipAnchor.End);

        return text.ToString();
    }

    /// <summary>
    /// Epic Loot's part of every item tooltip, run after vanilla or <see cref="RenderMagicItem"/> built it: the
    /// crafting material line, the set section and added lines of an item Epic Loot does not draw itself, and
    /// the colour pass.
    /// </summary>
    /// <param name="data">may be null (no local player), which leaves only the colour pass</param>
    public static string Finish(ItemDrop.ItemData item, string tooltip, ItemTooltipData data)
    {
        if (data != null)
        {
            if (data.MaterialRarity >= 0)
            {
                ItemRarity rarity = (ItemRarity)data.MaterialRarity;
                tooltip = $"<color={EpicLoot.GetRarityColor(rarity)}>{EpicLoot.GetRarityDisplayName(rarity)} " +
                    $"$mod_epicloot_craftingmaterial\n</color>" + tooltip;
            }

            if (!data.IsMagic)
            {
                if (item.IsSetItem())
                {
                    // Vanilla's own set lines are replaced by Epic Loot's set section.
                    int index = tooltip.IndexOf("\n\n$item_seteffect", StringComparison.InvariantCulture);
                    if (index >= 0)
                    {
                        tooltip = tooltip.Remove(index);
                    }

                    tooltip += RenderSets(data);
                }

                tooltip += RenderExtra(data, TooltipAnchor.AfterHeader, TooltipAnchor.AfterStats,
                    TooltipAnchor.AfterEffects, TooltipAnchor.AfterSet, TooltipAnchor.End);
            }
        }

        // Applied to the whole text, not to Epic Loot's own lines: the vanilla parts (status effect, damage
        // modifiers, projectile) and the localized effect texts carry these colours too.
        tooltip = tooltip.Replace("<color=orange>", "<color=#add8e6ff>");
        tooltip = tooltip.Replace("<color=yellow>", "<color=#add8e6ff>");
        return tooltip.Replace("\n\n\n", "\n\n");
    }

    /// <summary>The key hints the inventory grid draws under its tooltip.</summary>
    public static string RenderHints(ItemTooltipData data)
    {
        if (data == null || data.Hints.Count == 0)
        {
            return "";
        }

        StringBuilder hints = new StringBuilder();
        foreach (TooltipHint hint in data.Hints)
        {
            hints.Append("\n").Append(hint.Text);
        }

        return hints.ToString();
    }

    private void AddExtra(params string[] anchors)
    {
        text.Append(RenderExtra(data, anchors));
    }

    private static string RenderExtra(ItemTooltipData data, params string[] anchors)
    {
        if (data.Extra.Count == 0)
        {
            return "";
        }

        StringBuilder extra = new StringBuilder();
        foreach (string anchor in anchors)
        {
            foreach (TooltipLine line in data.Extra)
            {
                if (line.Anchor == anchor || (anchor == TooltipAnchor.End && !TooltipAnchor.IsKnown(line.Anchor)))
                {
                    extra.Append("\n").Append(line.Text);
                }
            }
        }

        return extra.ToString();
    }
}
