using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.ShardStones;

namespace EpicLoot.Compendium;

// The only place shard effects show outside a loose stone's tooltip: ExplainTextInfo filters on
// !NoRoll && CanBeAugmented, and every definition ShardEffectDefinitions synthesizes is NoRoll and not augmentable.
public class ShardStoneTextInfo(string topic) : MagicTextInfo(topic)
{
    private const string DimColor = "#c0c0c0ff";
    private const string FaintColor = "#808080";

    // The fine slot types each broad group covers, as the inverse of Shards.GroupOf. The shipped grid
    // only ever keys on the broad groups, so without this a player searching "sword" or "buckler" finds
    // nothing -- naming the members is what makes those terms match.
    private static readonly Dictionary<ShardSlotCategory, List<ShardSlotCategory>> GroupMembers =
        Enum.GetValues(typeof(ShardSlotCategory))
            .Cast<ShardSlotCategory>()
            .Select(slot => new { slot, group = Shards.GroupOf(slot) })
            .Where(x => x.group.HasValue)
            .GroupBy(x => x.group.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.slot).ToList());

    public override void Build(MagicPages instance)
    {
        List<KeyValuePair<ShardType, ShardDefinition>> shards = Shards.ShardDefinitions.ShardEffects
            .Where(x => x.Key != ShardType.None && x.Value != null)
            // Explicit ordering rather than dictionary insertion order: a file patch can append colours.
            .OrderBy(x => (int)x.Value.Category)
            .ThenBy(x => (int)x.Key)
            .ToList();

        if (shards.Count == 0)
        {
            instance.MagicPagesTextArea.Add("$mod_epicloot_shardstones_none");
            return;
        }

        foreach (KeyValuePair<ShardType, ShardDefinition> pair in shards)
        {
            FormatShard(instance, pair.Key, pair.Value);
        }
    }

    private static void FormatShard(MagicPages instance, ShardType color, ShardDefinition def)
    {
        List<ItemRarity> rarities = def.Rarities.OrderBy(x => (int)x).ToList();
        List<string> content = [];

        // Effect names carry the shard's rarity colour, keyed off the LOWEST rarity it exists at: every
        // colour reaches the top rarity, so the tier it becomes obtainable at is the only axis that varies.
        string rarityColor = EpicLoot.GetRarityColor(rarities.Count > 0 ? rarities[0] : ItemRarity.Magic);

        if (rarities.Count > 0)
        {
            IEnumerable<string> names = rarities.Select(r =>
                $"<color={EpicLoot.GetRarityColor(r)}>{EpicLoot.GetRarityDisplayName(r)}</color>");
            content.Add($" <color={DimColor}>$mod_epicloot_shardstones_rarities:</color> {string.Join(", ", names)}");
        }

        if (def.UniformEffect != null)
        {
            if (TryFormatEffect(def.UniformEffect, rarities, out string uniform, out string uniformDesc))
            {
                content.Add($" <color={DimColor}>$mod_epicloot_shard_allslots:</color> " +
                            $"<color={rarityColor}>{uniform}</color>");
                AddDescription(content, uniformDesc);
            }
        }
        else
        {
            content.Add($" <color={DimColor}>$mod_epicloot_shard_supportedtypes</color>");
            // Ordered by the enum so the broad groups lead and the fine types follow, matching how a
            // shard actually resolves (fine type first, group as the fallback).
            foreach (KeyValuePair<ShardSlotCategory, ShardEffectDefinition> slot in
                     def.TypeEffects.OrderBy(x => (int)x.Key))
            {
                if (!TryFormatEffect(slot.Value, rarities, out string effectName, out string description))
                {
                    continue;
                }

                content.Add($" <color={DimColor}>{Shards.GetCategoryDisplayName(slot.Key)}" +
                            $"{GroupMemberSuffix(slot.Key)}:</color> " +
                            $"<color={rarityColor}>{effectName}</color>");
                AddDescription(content, description);
            }
        }

        if (Shards.IsExclusive(def.Category))
        {
            content.Add($" <color={DimColor}>" +
                        $"$mod_epicloot_shard_{Shards.ExclusiveCategorySlug(def.Category)}exclusive</color>");
        }

        content.Add("");

        // Empty when the prefab or its icon is not registered yet, which leaves the icon out.
        string icon = ShardTooltipSprites.GetSpriteTag(Shards.GetIconPrefabName(color));
        if (!string.IsNullOrEmpty(icon))
        {
            icon += " ";
        }

        instance.MagicPagesTextArea.Add(
            $"<size={MagicPages.LARGE_FONT_SIZE}>{icon}" +
            $"<color={Shards.GetShardNameColor(color)}>" +
            $"$mod_epicloot_shard_{color} $mod_epicloot_assets_shardstone</color></size> " +
            $"<color={DimColor}>({Shards.GetCategoryDisplayName(def.Category)})</color>",
            content.ToArray());
    }

    // "Melee Weapon" -> " (Swords, Axes, Battleaxes, ...)". Only the broad groups get one; Trinket,
    // Utility and the fine types name themselves already.
    private static string GroupMemberSuffix(ShardSlotCategory slot)
    {
        if (!GroupMembers.TryGetValue(slot, out List<ShardSlotCategory> members))
        {
            return string.Empty;
        }

        string names = string.Join(", ", members.Select(m => Shards.GetCategoryDisplayName(m)));
        return $" <color={FaintColor}>({names})</color>";
    }

    // The indented explanation under an effect line. Skipped when the definition carries no Description,
    // and when the lookup missed so the localized text came back as a bare "[token]" -- printing that
    // would be worse than printing nothing.
    private static void AddDescription(List<string> content, string description)
    {
        if (string.IsNullOrEmpty(description) || description.StartsWith("["))
        {
            return;
        }

        content.Add($"    <color={DimColor}>{description}</color>");
    }

    // The description fills each value slot with the range across the rarities this shard drops at.
    // False when the effect grants nothing at any of them.
    private static bool TryFormatEffect(ShardEffectDefinition effect, List<ItemRarity> rarities,
        out string effectName, out string description)
    {
        effectName = null;
        description = string.Empty;

        if (effect == null || string.IsNullOrEmpty(effect.EffectType) || effect.ValuesPerRarity == null)
        {
            return false;
        }

        // Use AllDefinitions rather than Get(): Get() synthesizes a blank fallback and logs a "definition
        // missing" warning, which would be one warning per slot per shard every time the page is opened.
        if (!MagicItemEffectDefinitions.AllDefinitions.TryGetValue(effect.EffectType,
                out MagicItemEffectDefinition definition))
        {
            return false;
        }

        // Intersect with the shard's declared rarity set. Boss and unique shards list every rarity in
        // ValuesPerRarity even where Rarities allows fewer, so reading the raw map would show Yagluth as
        // (4-12) instead of the (10-12) it can actually be found at.
        List<float> values = rarities
            .Where(effect.ValuesPerRarity.ContainsKey)
            .Select(r => effect.ValuesPerRarity[r])
            .ToList();

        if (values.Count == 0)
        {
            return false;
        }

        MagicItemEffectDefinition.ValueDef range = new MagicItemEffectDefinition.ValueDef
        {
            MinValue = values.Min(),
            MaxValue = values.Max(),
            Increment = 0
        };

        effectName = definition.GetName();
        description = string.IsNullOrEmpty(definition.Description)
            ? string.Empty
            : MagicItem.GetEffectDescriptionRange(definition, range);
        return true;
    }
}
