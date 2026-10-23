using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using EpicLoot.LegendarySystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace EpicLoot.Compatibility;

/// <summary>
/// Epic Loot's conditions for Item Favorite Framework's wishlist: a minimum rarity, a unique item, a set,
/// and effects the item must carry. The framework draws the fields from <see cref="GetSchema"/> in its
/// wishlist editor, stores the chosen values as JSON with the entry, and asks <see cref="Matches"/>.
///
/// Matches is also run on other players' entries (to show who wants an item), so it depends only on the
/// item and the criteria, never on the local player or the time.
/// </summary>
internal static class ItemFavoritesWishlist
{
    internal const string ProviderId = "epicloot";

    private static readonly Regex Placeholder = new Regex(@"\{[^}]*\}");

    /// <summary>The fields offered for an item that can be magic; "" for anything else.</summary>
    internal static string GetSchema(string prefabName)
    {
        ItemDrop.ItemData item = Template(prefabName);
        if (item == null || !EpicLoot.CanBeMagicItem(item))
        {
            return "";
        }

        var rarities = new JArray(Rarities.All.Select(r => new JArray(((int)r).ToString(CultureInfo.InvariantCulture), EpicLoot.GetRarityDisplayName(r))));
        var uniques = new JArray(UniqueLegendaryHelper.AllUniques.Values
            .Where(u => u.Requirements == null || u.Requirements.AllowByItemType(item))
            .OrderBy(u => Localize(u.Name))
            .Select(u => new JArray(u.ID, Localize(u.Name))));
        var sets = new JArray(UniqueLegendaryHelper.AllSets.Values
            .OrderBy(s => Localize(s.Name))
            .Select(s => new JArray(s.ID, Localize(s.Name))));
        var effects = new JArray(MagicItemEffectDefinitions.AllDefinitions.Values
            .Where(d => d.Requirements == null || d.Requirements.AllowByItemType(item))
            .Select(d => new { d.Type, Label = EffectLabel(d) })
            .OrderBy(e => e.Label)
            .Select(e => new JArray(e.Type, e.Label)));

        var fields = new JArray
        {
            Field("rarity", "$mod_epicloot_wishlist_rarity", "Minimum rarity", "enum", rarities),
        };
        if (uniques.Count > 0)
        {
            fields.Add(Field("legendary", "$mod_epicloot_wishlist_unique", "Unique item", "pick", uniques));
        }

        if (sets.Count > 0)
        {
            fields.Add(Field("set", "$mod_epicloot_wishlist_set", "Set", "pick", sets));
        }

        fields.Add(Field("effects", "$mod_epicloot_wishlist_effects", "Must have effects", "multi", effects));
        return new JObject { ["title"] = "Epic Loot", ["fields"] = fields }.ToString(Formatting.None);
    }

    // Criteria can come from another player's shared wishlist, so nothing about their shape is trusted: a
    // value of the wrong kind reads as unset, and anything else that goes wrong is no match.
    internal static bool Matches(ItemDrop.ItemData item, string criteriaJson)
    {
        try
        {
            return MatchesUnchecked(item, criteriaJson);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool MatchesUnchecked(ItemDrop.ItemData item, string criteriaJson)
    {
        JObject criteria = Parse(criteriaJson);
        if (criteria == null || item == null)
        {
            return false;
        }

        MagicItem magicItem = item.GetMagicItem();
        string rarity = Text(criteria["rarity"]);
        if (!string.IsNullOrEmpty(rarity))
        {
            if (magicItem == null || !int.TryParse(rarity, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minimum) ||
                (int)magicItem.Rarity < minimum)
            {
                return false;
            }
        }

        string legendary = Text(criteria["legendary"]);
        if (!string.IsNullOrEmpty(legendary) && (magicItem == null || magicItem.LegendaryID != legendary))
        {
            return false;
        }

        string set = Text(criteria["set"]);
        if (!string.IsNullOrEmpty(set) && (magicItem == null || magicItem.SetID != set))
        {
            return false;
        }

        foreach (string effect in Strings(criteria["effects"]))
        {
            if (magicItem == null || !item.HasMagicEffect(effect))
            {
                return false;
            }
        }

        return true;
    }

    internal static string Describe(string criteriaJson)
    {
        try
        {
            return DescribeUnchecked(criteriaJson);
        }
        catch (Exception)
        {
            return "";
        }
    }

    private static string DescribeUnchecked(string criteriaJson)
    {
        JObject criteria = Parse(criteriaJson);
        if (criteria == null)
        {
            return "";
        }

        var parts = new List<string>();
        string rarity = Text(criteria["rarity"]);
        if (int.TryParse(rarity, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minimum) &&
            Enum.IsDefined(typeof(ItemRarity), minimum))
        {
            ItemRarity value = (ItemRarity)minimum;
            parts.Add(value == Rarities.Highest ? EpicLoot.GetRarityDisplayName(value) : EpicLoot.GetRarityDisplayName(value) + "+");
        }

        string legendary = Text(criteria["legendary"]);
        if (!string.IsNullOrEmpty(legendary))
        {
            parts.Add(UniqueLegendaryHelper.AllUniques.TryGetValue(legendary, out LegendaryInfo info) ? Localize(info.Name) : legendary);
        }

        string set = Text(criteria["set"]);
        if (!string.IsNullOrEmpty(set))
        {
            parts.Add(UniqueLegendaryHelper.AllSets.TryGetValue(set, out LegendarySetInfo info) ? Localize(info.Name) : set);
        }

        List<string> effects = Strings(criteria["effects"]).ToList();
        if (effects.Count > 0)
        {
            parts.Add(string.Join(", ", effects.Select(e =>
                MagicItemEffectDefinitions.AllDefinitions.TryGetValue(e, out MagicItemEffectDefinition definition) ? EffectLabel(definition) : e)));
        }

        return string.Join("; ", parts);
    }

    /// <summary>A magic item's own rarity, and its unique or set when it has one.</summary>
    internal static string Suggest(ItemDrop.ItemData item)
    {
        if (item == null || !item.IsMagic(out MagicItem magicItem) || magicItem.IsUnidentified)
        {
            return "";
        }

        var criteria = new JObject { ["v"] = 1, ["rarity"] = ((int)magicItem.Rarity).ToString(CultureInfo.InvariantCulture) };
        if (!string.IsNullOrEmpty(magicItem.LegendaryID))
        {
            criteria["legendary"] = magicItem.LegendaryID;
        }
        else if (!string.IsNullOrEmpty(magicItem.SetID))
        {
            criteria["set"] = magicItem.SetID;
        }

        return criteria.ToString(Formatting.None);
    }

    /// <summary>A magic item's full, rarity-coloured name for roll windows and loot messages.</summary>
    internal static string DisplayName(ItemDrop.ItemData item)
    {
        return item != null && item.IsMagic() ? item.GetDecoratedName() : null;
    }

    private static ItemDrop.ItemData Template(string prefabName)
    {
        GameObject prefab = ObjectDB.instance != null && !string.IsNullOrEmpty(prefabName) ? ObjectDB.instance.GetItemPrefab(prefabName) : null;
        ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
        if (drop == null)
        {
            return null;
        }

        ItemDrop.ItemData item = drop.m_itemData.Clone();
        item.m_dropPrefab = prefab;
        return item;
    }

    private static JObject Field(string key, string token, string english, string type, JArray options)
    {
        string label = Localize(token);
        return new JObject
        {
            ["key"] = key,
            ["label"] = string.IsNullOrEmpty(label) || label.Contains(token.TrimStart('$')) ? english : label,
            ["type"] = type,
            ["options"] = options
        };
    }

    // "+{0}% Physical Damage" -> "+X% Physical Damage".
    private static string EffectLabel(MagicItemEffectDefinition definition)
    {
        string text = Localize(definition.DisplayText);
        text = Placeholder.Replace(text, "X").Trim();
        return string.IsNullOrEmpty(text) ? definition.Type : text;
    }

    private static string Localize(string text)
    {
        if (string.IsNullOrEmpty(text) || Localization.instance == null)
        {
            return text ?? "";
        }

        return Localization.instance.Localize(text);
    }

    private static JObject Parse(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JObject.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<string> Strings(JToken token)
    {
        if (token == null)
        {
            yield break;
        }

        if (token.Type == JTokenType.Array)
        {
            foreach (JToken element in token)
            {
                string value = Text(element);
                if (!string.IsNullOrEmpty(value))
                {
                    yield return value;
                }
            }
        }
        else
        {
            string value = Text(token);
            if (!string.IsNullOrEmpty(value))
            {
                yield return value;
            }
        }
    }

    // A scalar as text; null for anything else (an array or object where a single value belongs).
    private static string Text(JToken token)
    {
        if (token == null)
        {
            return null;
        }

        switch (token.Type)
        {
            case JTokenType.String:
            case JTokenType.Integer:
            case JTokenType.Float:
            case JTokenType.Boolean:
                return Convert.ToString(((JValue)token).Value, CultureInfo.InvariantCulture);
            default:
                return null;
        }
    }
}
