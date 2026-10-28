using System.Collections.Generic;
using System.Text;
using EpicLoot.ShardStones;
using UnityEngine;

namespace EpicLoot;

// The magic sections: effects and sockets, sets, a shard's preview and a set rune. The effects and set sections
// are static so Auga's result dialogs can draw them on their own.
internal partial class TooltipRenderer
{
    /// <summary>The effect lines, the sockets and the rarity line.</summary>
    public static string RenderEffects(ItemTooltipData data)
    {
        string color = data.RarityColor;
        StringBuilder tooltip = new StringBuilder();
        tooltip.Append($"\n<color={color}>");
        foreach (TooltipEffect effect in data.Effects)
        {
            string pip = MagicItem.GetMagicEffectPip(effect.Marker);
            tooltip.AppendLine($"{pip} {MagicEffectRarity.Decorate(effect.EffectType, effect.Text, true)}");
            if (data.IncludeDetails)
            {
                // The range and other details go in the block below the line. Close/reopen the rarity color
                // around the dim detail block rather than nesting tags.
                tooltip.Append($"</color><color={DetailColor}>");
                tooltip.Append(effect.Detail);
                tooltip.Append($"</color><color={color}>");
            }
        }

        tooltip.Append($"</color>");

        TooltipSockets sockets = data.Sockets;
        if (sockets != null && sockets.Count > 0)
        {
            tooltip.AppendLine($"$mod_epicloot_sockets ({sockets.Used}/{sockets.Count}):");
            foreach (TooltipSocket socket in sockets.Entries)
            {
                string socketColor = EpicLoot.GetRarityColor((ItemRarity)socket.Rarity);
                // Inline the socketed item's own icon, resolved from its source prefab
                string iconTag = ShardTooltipSprites.GetSpriteTag(socket.SourcePrefab);
                string removalTag = GetRemovalTag(socket.Removal);
                switch (socket.State)
                {
                    case TooltipSocketState.InactiveRune:
                        tooltip.AppendLine($"  <color=#808080>{iconTag} {socket.Text} $mod_epicloot_socket_tip_runeinactive</color>");
                        break;
                    case TooltipSocketState.InertShard:
                        tooltip.AppendLine($"  <color={socketColor}>{iconTag} $mod_epicloot_shard_noeffect</color>{removalTag}");
                        break;
                    default:
                        tooltip.AppendLine($"  <color={socketColor}>{iconTag} {socket.Text}</color>" +
                            $"{GetSocketStackTag(socket.StackMultiplier)}{removalTag}");
                        if (data.IncludeDetails)
                        {
                            tooltip.Append($"<color={DetailColor}>");
                            tooltip.Append(socket.Detail);
                            tooltip.Append($"</color>");
                        }
                        break;
                }
            }

            for (int i = 0; i < sockets.Open; i++)
            {
                tooltip.AppendLine("  \u25CA<color=#808080> $mod_epicloot_empty_socket</color>");
            }
        }

        tooltip.AppendLine($"$mod_epicloot_itemtooltip_rarity: <color={color}>{data.RarityName}</color><pos=75%>" +
            $"$mod_epicloot_itemtooltip_effects: <color={color}>{data.Effects.Count}</color>");

        return tooltip.ToString();
    }

    /// <summary>
    /// The rune list's one-line-per-effect name for a runestone at the enchanting table, with each effect's
    /// roll range.
    /// </summary>
    public static string RenderCompactEffects(MagicItem magicItem)
    {
        StringBuilder tooltip = new StringBuilder();
        tooltip.Append($"<color={magicItem.GetColorString()}>");
        foreach (TooltipEffect effect in TooltipGatherer.GatherEffects(magicItem, false))
        {
            string range = effect.RollMin.HasValue && effect.RollMax.HasValue &&
                !Mathf.Approximately(effect.RollMin.Value, effect.RollMax.Value)
                    ? $" [{effect.RollMin.Value}-{effect.RollMax.Value}]"
                    : "";
            tooltip.AppendLine($"{MagicEffectRarity.Decorate(effect.EffectType, effect.Text + range, true)}");
        }

        tooltip.Append($"</color>");
        return tooltip.ToString();
    }

    // Marks sockets the current config won't let the player simply empty.
    private static string GetRemovalTag(string removal)
    {
        switch (removal)
        {
            case TooltipRemoval.BreakOnly:
                return " <color=#808080>$mod_epicloot_socket_tip_breakonly</color>";
            case TooltipRemoval.Locked:
                return " <color=#808080>$mod_epicloot_socket_tip_permanent</color>";
            default:
                return "";
        }
    }

    // Marks a socket whose value was cut by same-color stacking decay or rune diminishing returns, so
    // the reduced number on the line above reads as the rule working rather than as a bug. Nothing is
    // shown at full strength, which is every socket unless ShardStackMode.Diminishing or Rune
    // Diminishing Returns is on.
    private static string GetSocketStackTag(float stackMultiplier)
    {
        if (stackMultiplier >= 1f)
        {
            return "";
        }

        int percent = Mathf.RoundToInt(stackMultiplier * 100f);
        string label = string.Format(Localization.instance.Localize("$mod_epicloot_socket_tip_stacked"), percent);
        return $" <color=#808080>{label}</color>";
    }

    /// <summary>Every set section: the pieces, which are worn, and the bonuses.</summary>
    public static string RenderSets(ItemTooltipData data)
    {
        if (data.Sets.Count == 0)
        {
            return "";
        }

        string setColor = data.SetColor;
        StringBuilder text = new StringBuilder();
        foreach (TooltipSet set in data.Sets)
        {
            text.Append($"\n\n<color={setColor}> $mod_epicloot_set: {set.Name} ({set.Equipped}/{set.Full}):</color>");

            foreach (TooltipSetPiece piece in set.Pieces)
            {
                // An equipped Epic Loot piece shows in the rarity it is worn at, which is what its bonuses tier by.
                string color = !piece.Equipped ? InactiveColor
                    : piece.Rarity >= 0 ? EpicLoot.GetRarityColor((ItemRarity)piece.Rarity)
                    : "white";
                text.Append($"\n  <color={color}>{piece.Name}</color>");
            }

            foreach (TooltipSetBonus bonus in set.Bonuses)
            {
                string tierTag = bonus.Active && bonus.TierVaries && bonus.Tier >= 0
                    ? $" <color={EpicLoot.GetRarityColor((ItemRarity)bonus.Tier)}>" +
                      $"({EpicLoot.GetRarityDisplayName((ItemRarity)bonus.Tier)})</color>"
                    : "";

                text.Append($"\n<color={(bonus.Active ? setColor : InactiveColor)}>" +
                    $"({bonus.Count}) \u2023 {bonus.Text}</color>{tierTag}");
                AppendBonusDetails(text, bonus, bonus.Active ? DetailColor : InactiveColor);
            }
        }

        return text.ToString();
    }

    private static void AppendBonusDetails(StringBuilder text, TooltipSetBonus bonus, string color)
    {
        if (!string.IsNullOrEmpty(bonus.Detail))
        {
            text.Append($"\n<color={color}>    {bonus.Detail}</color>");
        }

        if (!string.IsNullOrEmpty(bonus.ActivationKey))
        {
            text.Append($"\n<color={color}>    {bonus.ActivationKey}</color>");
        }
    }

    private void AddShardPreview()
    {
        TooltipShardPreview preview = data.ShardPreview;
        if (preview == null)
        {
            return;
        }

        text.Append("\n");
        if (preview.NoEffect)
        {
            text.AppendLine($"<color={magicColor}>$mod_epicloot_shard_noeffect</color>");
            return;
        }

        text.AppendLine("$mod_epicloot_shard_ifsocketed:");
        if (!preview.Uniform)
        {
            text.AppendLine($"<color={DetailColor}>$mod_epicloot_shard_supportedtypes</color>");
        }

        foreach (TooltipShardSlot slot in preview.Slots)
        {
            // Per slot only when the mode is selective (BreakValueless); a blanket mode is said once below.
            string removalTag = preview.BlanketRemoval == TooltipRemoval.Free ? GetRemovalTag(slot.Removal) : "";
            text.AppendLine($"  <color={magicColor}>{slot.Name}: {slot.Text}</color>{removalTag}");
            if (!string.IsNullOrEmpty(slot.Detail))
            {
                text.Append($"<color={DetailColor}>{slot.Detail}</color>");
            }
        }

        if (preview.Exclusive != null)
        {
            text.AppendLine($"<color={magicColor}>{preview.Exclusive}</color>");
        }

        // States the commitment when the removal mode restricts every supported placement.
        switch (preview.BlanketRemoval)
        {
            case TooltipRemoval.BreakOnly:
                text.AppendLine("<color=#808080>$mod_epicloot_shard_warn_breakonly</color>");
                break;
            case TooltipRemoval.Locked:
                text.AppendLine("<color=#808080>$mod_epicloot_shard_warn_permanent</color>");
                break;
        }
    }

    private void AddSetRuneTooltip()
    {
        string setColor = data.SetColor;
        AddSetLabel();
        AddDescription();
        AddExtra(TooltipAnchor.AfterHeader);
        text.Append("\n");

        AddTeleportable();
        AddValuable();
        AddWeight();

        TooltipSetRune rune = data.SetRune;
        if (rune != null && !rune.Known)
        {
            text.Append($"\n\n<color={setColor}>$mod_epicloot_set: {rune.SetId}</color>");
            text.Append("\n<color=#ff5555ff>$mod_epicloot_setrune_unknownset</color>");
        }
        else if (rune != null)
        {
            text.Append($"\n\n<color={setColor}>$mod_epicloot_set: {rune.Name}</color>");
            text.Append($"\n$mod_epicloot_setrune_rarities: {rune.Rarities}");

            text.Append("\n$mod_epicloot_setrune_etchonto:");
            bool anyFurtherRestricted = false;
            foreach (TooltipSetRunePiece piece in rune.Pieces)
            {
                anyFurtherRestricted |= piece.FurtherRestricted;
                text.Append($"\n  {piece.Name} <color={DetailColor}>({piece.Requirements}{(piece.FurtherRestricted ? "*" : "")})</color>");
            }

            text.Append("\n$mod_epicloot_set_bonuses:");
            foreach (TooltipSetBonus bonus in rune.Bonuses)
            {
                text.Append($"\n<color={setColor}>({bonus.Count}) \u2023 {bonus.Text}</color>");
                AppendBonusDetails(text, bonus, DetailColor);
            }

            if (anyFurtherRestricted)
            {
                text.Append($"\n<color={DetailColor}>* $mod_epicloot_set_restricted</color>");
            }
        }

        AddExtra(TooltipAnchor.AfterStats, TooltipAnchor.AfterEffects, TooltipAnchor.AfterSet, TooltipAnchor.End);
    }
}
