using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace EpicLoot
{
    [Serializable]
    public class LootDrop
    {
        public string Item;
        public float Weight = 1;

        // Added to Weight once per effective star past the level this entry's list was authored at, and
        // floored at 0. It is how one anchor level shifts a template's item mix toward the next tier's
        // gear as stars rise, which used to take a separate LeveledLoot entry per level. Only read on
        // loot table lists -- an ItemSet member's weight never scales.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? WeightPerStar;

        public float[] Rarity;

        // Per-rarity override of Item: the rarity rolled from Rarity[] picks the entry, and that name
        // replaces Item before anything looks the prefab up. A value may name a prefab, an ItemSet or a
        // "Object.Level" loot table reference -- LootRoller.ResolveLootDrop keeps resolving whatever it
        // substitutes. Item stays the default for a rarity the map does not cover.
        //
        // Left null rather than empty on purpose. Newtonsoft APPENDS to a pre-initialized collection, and
        // null is what tells ResolveLootDrop this entry does not vary by rarity at all. Ignoring nulls on
        // write keeps AutoAddEnchantableItems' rewritten loottables.json from sprouting "RarityItems": null
        // on every one of its ~2000 entries.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<ItemRarity, string> RarityItems;

        // Fields this version does not know (another mod's, or a newer EpicLoot's) ride along untouched
        // through the legacy conversion and the auto-add rewrite instead of being dropped from disk.
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraData;
    }

    [Serializable]
    public class LeveledLootDef
    {
        public int Level;

        // Either may be left out: a level that defines only Drops keeps rolling the Loot of the level
        // below it, and the other way round. Each is an independent anchor for star scaling.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float[][] Drops;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public LootDrop[] Loot;

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraData;
    }

    /// <summary>
    /// How a loot table's rolls grow per effective star past the level they were authored at. Every
    /// field is optional; a missing one falls through to the table's template and then to
    /// <see cref="LootConfig.DefaultStarScaling"/> (see LootScalingMath.MergeScaling).
    /// </summary>
    [Serializable]
    public class LootScaling
    {
        // Added to the chance of dropping anything at all, per star (0.05 = five percentage points).
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? DropChance;

        // Extra items per star on a roll that drops something: the whole part is guaranteed, the
        // fraction is the chance of one more.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? BonusDrops;

        // Fraction of each rarity's weight promoted one tier per star, at the lowest tier the table
        // rolls; RarityFalloff scales it for every tier above that one.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? RarityShift;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? RarityFalloff;

        // Promotion never moves weight into a tier above this one.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemRarity? MaxRarity;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxDrops;

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraData;
    }

    /// <summary>
    /// Star-independent adjustments on top of whatever level a table resolves to. Written on a creature's
    /// RefObject entry they tune that one creature without cloning its template.
    /// </summary>
    [Serializable]
    public class LootModifiers
    {
        // Multiplies the odds of dropping anything, with the same maths as Global Drop Rate Modifier.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? DropRate;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? BonusDrops;

        // One extra promotion step at this rate, applied after the star-driven ones.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? RarityShift;

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraData;
    }

    [Serializable]
    public class LootTable
    {
        public string Object;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RefObject;

        // The old flat form: one Drops/Loot pair that only ever answered creature levels 1-3.
        // LootTableMigration folds it into LeveledLoot level 1 wherever a table enters the game, so
        // nothing past that point reads these. Kept so older files, patches and API callers still load.
        [Obsolete("Flat loot tables are deprecated; use LeveledLoot. LootTableMigration converts these on load.")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float[][] Drops;

        [Obsolete("Flat loot tables are deprecated; use LeveledLoot. LootTableMigration converts these on load.")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public LootDrop[] Loot;

        // Anchor levels. A creature rolls the highest level at or below its own and StarScaling
        // extrapolates the rest, so one entry per level is no longer needed.
        public List<LeveledLootDef> LeveledLoot = new List<LeveledLootDef>();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public LootScaling StarScaling;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public LootModifiers Modifiers;

        // How aggressively stars count for this table (on a template) or this one creature (on its
        // RefObject entry): 1.5 makes a 2-star creature loot like a 3-star one.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? StarMultiplier;

        // Written by the creature sorter on the entries it generated. The sorter re-sorts or removes
        // only these; delete the flag (or pin the creature in itemsorter.json) to keep an entry as is.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? Auto;

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraData;

        // A RefObject stub has no levels of its own; do not write an empty list onto all of them.
        public bool ShouldSerializeLeveledLoot() => LeveledLoot != null && LeveledLoot.Count > 0;

        /// <summary>A single-level table: the shape code builds for console spawns, gambles and identify.</summary>
        public static LootTable Simple(string objectName, float[][] drops, LootDrop[] loot)
        {
            return new LootTable
            {
                Object = objectName,
                LeveledLoot = new List<LeveledLootDef>
                {
                    new LeveledLootDef { Level = 1, Drops = drops, Loot = loot }
                }
            };
        }
    }

    [Serializable]
    public class LootItemSet
    {
        public string Name;
        public LootDrop[] Loot;
    }

    [Serializable]
    public class MagicEffectsCountConfig
    {
        public float[][] Magic;
        public float[][] Rare;
        public float[][] Epic;
        public float[][] Legendary;
        public float[][] Mythic;
        public float[][] Ancient;
    }

    [Serializable]
    public class SocketCountsConfig
    {
        public float[][] Magic;
        public float[][] Rare;
        public float[][] Epic;
        public float[][] Legendary;
        public float[][] Mythic;
        public float[][] Ancient;
    }

    [Serializable]
    public class LootConfig
    {
        public MagicEffectsCountConfig MagicEffectsCount;
        public SocketCountsConfig SocketCounts;

        // Star scaling for any table that does not say otherwise: third-party and API tables, and
        // anything converted from the flat form. LootScalingMath.CodeDefaultStarScaling stands in when a
        // file predates this field.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public LootScaling DefaultStarScaling;

        public LootItemSet[] ItemSets;
        public LootTable[] LootTables;
        public List<string> RestrictedItems = new List<string>();

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraData;

        /// <summary>
        /// A copy sharing every member, for writers that swap out ItemSets/LootTables. Building a new
        /// LootConfig field by field is how a root field silently went missing from the rewritten file.
        /// </summary>
        public LootConfig ShallowCopy()
        {
            return (LootConfig)MemberwiseClone();
        }
    }
}
