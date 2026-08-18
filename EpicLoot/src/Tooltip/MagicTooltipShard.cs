using EpicLoot.ShardStones;

namespace EpicLoot;

public partial class MagicTooltip
{
    // Loose shard preview: shows what the shard would grant if socketed, at its own rarity, listing
    // only the item types that have a defined effect for this shard color.
    private void AddShardPreview()
    {
        var color = Shards.GetShardColor(item);
        var def = Shards.ShardDefinitions.Get(color);
        var showDetails = MagicItem.ShowEffectDetails;
        // Read the rarity from the shard's own shared data rather than magicItem, keeping every shard
        // lookup on the one source of truth.
        var rarity = Shards.GetShardRarity(item);

        text.Append("\n");
        if (def == null || (def.UniformEffect == null && def.TypeEffects.Count == 0))
        {
            text.AppendLine($"<color={magicColor}>$mod_epicloot_shard_noeffect</color>");
            // Granting nothing is no protection under the blanket modes -- it would still occupy the
            // socket for good, so the commitment has to be stated here too.
            if (color != ShardType.None)
            {
                AppendBlanketRemovalWarning(ShardSocketManager.GetRemovalPolicy(color, null, rarity));
            }
            return;
        }

        text.AppendLine("$mod_epicloot_shard_ifsocketed:");

        // Probing the policy with a null effect gives the rule that applies no matter what the shard
        // ends up granting -- i.e. the BreakAll/Permanent modes. When one of those is on, say so once
        // up front instead of repeating a marker on every line, and note that it covers the slots not
        // even listed below (those that grant nothing).
        var blanket = ShardSocketManager.GetRemovalPolicy(color, null, rarity);

        // A uniform shard (e.g. a boss shard) grants one effect on every slot it is allowed into.
        if (def.UniformEffect != null)
        {
            if (def.UniformEffect.ValuesPerRarity.TryGetValue(rarity, out var uniformValue))
            {
                var uniformDef = MagicItemEffectDefinitions.Get(def.UniformEffect.EffectType);
                if (uniformDef != null)
                {
                    var allSlots = Localization.instance.Localize("$mod_epicloot_shard_allslots");
                    var uniformText = MagicItem.GetEffectText(uniformDef, uniformValue);
                    var removalTag = PreviewRemovalTag(blanket, color, uniformDef.Type, uniformValue, rarity);
                    text.AppendLine($"  <color={magicColor}>{allSlots}: {uniformText}</color>{removalTag}");
                    AppendShardEffectDetails(uniformDef.Type, uniformValue, showDetails);
                }
            }

            if (Shards.IsExclusive(def.Category))
            {
                text.AppendLine($"<color={magicColor}>" +
                    $"$mod_epicloot_shard_{Shards.ExclusiveCategorySlug(def.Category)}exclusive</color>");
            }
            AppendBlanketRemovalWarning(blanket);
            return;
        }

        foreach (var pair in def.TypeEffects)
        {
            var effectDef = pair.Value;
            if (!effectDef.ValuesPerRarity.TryGetValue(rarity, out var value))
            {
                continue;
            }

            var effectMagicDef = MagicItemEffectDefinitions.Get(effectDef.EffectType);
            if (effectMagicDef == null)
            {
                continue;
            }

            var typeName = Shards.GetCategoryDisplayName(pair.Key);
            var effectText = MagicItem.GetEffectText(effectMagicDef, value);
            var removalTag = PreviewRemovalTag(blanket, color, effectMagicDef.Type, value, rarity);
            text.AppendLine($"  <color={magicColor}>{typeName}: {effectText}</color>{removalTag}");
            AppendShardEffectDetails(effectMagicDef.Type, value, showDetails);
        }

        AppendBlanketRemovalWarning(blanket);
    }

    // Per-slot removal marker, used only when the mode is selective (BreakValueless). Under a blanket
    // mode the single warning line below says it once for the whole shard instead.
    private static string PreviewRemovalTag(SocketRemoval blanket, ShardType color, string effectType,
        float value, ItemRarity rarity)
    {
        if (blanket != SocketRemoval.Free)
        {
            return "";
        }

        var policy = ShardSocketManager.GetRemovalPolicy(color, new MagicItemEffect(effectType, value), rarity);
        switch (policy)
        {
            case SocketRemoval.BreakOnly:
                return " <color=#808080>$mod_epicloot_socket_tip_breakonly</color>";
            case SocketRemoval.Locked:
                return " <color=#808080>$mod_epicloot_socket_tip_permanent</color>";
            default:
                return "";
        }
    }

    // States the commitment up front when the mode restricts every slot, including the ones that grant
    // nothing and so never appear in the list above.
    private void AppendBlanketRemovalWarning(SocketRemoval blanket)
    {
        switch (blanket)
        {
            case SocketRemoval.BreakOnly:
                text.AppendLine("<color=#808080>$mod_epicloot_shard_warn_breakonly</color>");
                break;
            case SocketRemoval.Locked:
                text.AppendLine("<color=#808080>$mod_epicloot_shard_warn_permanent</color>");
                break;
        }
    }

    // Appends the dim, indented detail block (description + config) under a previewed shard effect
    // when Shift is held. A shard grants a fixed value per rarity, so the range line is suppressed by
    // passing a single-value override; only the description and any config lines are shown.
    private void AppendShardEffectDetails(string effectType, float value, bool showDetails)
    {
        if (!showDetails)
        {
            return;
        }

        var fixedValue = new MagicItemEffectDefinition.ValueDef { MinValue = value, MaxValue = value, Increment = 0 };
        var block = MagicItem.GetEffectDetailBlock(new MagicItemEffect(effectType, value),
            Shards.GetShardRarity(item), null, fixedValue, "     ");
        if (block.Length > 0)
        {
            text.Append($"<color=#c0c0c0ff>{block}</color>");
        }
    }
}
