using EpicLoot.ShardStones;

namespace EpicLoot;

// A loose shard's preview: what it would grant if socketed, at its own rarity, listing only the item types
// that have a defined effect for this shard color.
public partial class TooltipGatherer
{
    private void GatherShardPreview()
    {
        ShardType color = Shards.GetShardColor(item);
        var def = Shards.ShardDefinitions.Get(color);
        // Read the rarity from the shard's own shared data rather than magicItem, keeping every shard
        // lookup on the one source of truth.
        ItemRarity rarity = Shards.GetShardRarity(item);

        TooltipShardPreview preview = new TooltipShardPreview();
        data.ShardPreview = preview;

        if (def == null || (def.UniformEffect == null && def.TypeEffects.Count == 0))
        {
            preview.NoEffect = true;
            return;
        }

        // Probing the policy with a null effect gives the rule that applies no matter what the shard
        // ends up granting -- i.e. the BreakAll/Permanent modes. When one of those is on, it is said once
        // up front instead of repeating a marker on every supported type's line.
        SocketRemoval blanket = ShardSocketManager.GetRemovalPolicy(color, null);
        preview.BlanketRemoval = ToRemoval(blanket);

        if (Shards.IsExclusive(def.Category))
        {
            preview.Exclusive = $"$mod_epicloot_shard_{Shards.ExclusiveCategorySlug(def.Category)}exclusive";
        }

        // A uniform shard (e.g. a boss shard) grants one effect on every slot it is allowed into.
        if (def.UniformEffect != null)
        {
            preview.Uniform = true;
            if (def.UniformEffect.ValuesPerRarity.TryGetValue(rarity, out float uniformValue) &&
                MagicItemEffectDefinitions.TryGet(def.UniformEffect.EffectType, out MagicItemEffectDefinition uniformDef))
            {
                preview.Slots.Add(GetShardSlot(Localization.instance.Localize("$mod_epicloot_shard_allslots"),
                    uniformDef, uniformValue, blanket, color));
            }

            return;
        }

        foreach (var pair in def.TypeEffects)
        {
            var effectDef = pair.Value;
            if (effectDef == null || !effectDef.ValuesPerRarity.TryGetValue(rarity, out float value))
            {
                continue;
            }

            if (!MagicItemEffectDefinitions.TryGet(effectDef.EffectType, out MagicItemEffectDefinition effectMagicDef))
            {
                continue;
            }

            preview.Slots.Add(GetShardSlot(Shards.GetCategoryDisplayName(pair.Key), effectMagicDef, value, blanket, color));
        }
    }

    private TooltipShardSlot GetShardSlot(string name, MagicItemEffectDefinition effectDef, float value,
        SocketRemoval blanket, ShardType color)
    {
        TooltipShardSlot slot = new TooltipShardSlot
        {
            Name = name,
            EffectType = effectDef.Type,
            Value = value,
            Text = MagicItem.GetEffectText(effectDef, value),
            // Per-slot only when the mode is selective (BreakValueless); under a blanket mode the one warning
            // line says it for the whole shard.
            Removal = blanket != SocketRemoval.Free
                ? ToRemoval(blanket)
                : ToRemoval(ShardSocketManager.GetRemovalPolicy(color, new MagicItemEffect(effectDef.Type, value)))
        };

        if (includeDetails)
        {
            // A shard grants a fixed value per rarity, so the range line is suppressed by passing a
            // single-value override; only the description and any config lines are shown.
            MagicItemEffectDefinition.ValueDef fixedValue =
                new MagicItemEffectDefinition.ValueDef { MinValue = value, MaxValue = value, Increment = 0 };
            slot.Detail = MagicItem.GetEffectDetailBlock(new MagicItemEffect(effectDef.Type, value),
                Shards.GetShardRarity(item), null, fixedValue, "     ");
        }

        return slot;
    }
}
