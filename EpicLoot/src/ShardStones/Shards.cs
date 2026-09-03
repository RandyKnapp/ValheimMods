using EpicLoot.Crafting;
using EpicLoot.Data;
using EpicLoot.GatedItemType;
using EpicLoot.General;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.ShardStones {
    // A Shard's color determines which set of magical effects it has along with its icon
    public enum ShardType {
        // Core Shards
        Red = 0,  // Vitality
        Yellow = 1, // Stamina
        Cyan = 2,  // Eitr
        // Standard Shards
        Black = 3, // Night time effects
        Green = 4, // Movement
        Orange = 5, // Fire
        Pink = 6, // Dodge
        Purple = 7, // Eitr
        White = 8, // Daytime
        Grey = 9, // harvesting
        // Dark shards
        DarkGreen = 30,
        DarkPurple = 31,
        DarkRed = 32, // berserker
        DarkBlue = 33, // cold resistances
        Golden = 34, // luck
        // Light shards
        LightBlue = 40,
        LightGreen = 41,
        Peach = 42,
        //LightRed = 43,
        // Unique shards -- one signature effect granted on every slot, one worn at a time
        Firewalker = 70,
        Stormcaller = 71,
        // Boss shards
        Eikthyr = 90,
        Elder = 91,
        Bonemass = 92,
        Moder = 93,
        Yagluth = 94,
        Queen = 95,
        Fader = 96,

        // This is the error path
        None
    }

    // Groups shards into classes. A category may be flagged exclusive (see Shards.IsExclusive),
    // meaning a player may wear at most one socketed shard of that category at a time.
    public enum ShardCategory {
        Core,
        Dark,
        Light,
        Boss,
        Unique
    }

    // The equipment "slot" a shard resolves to when socketed. This mirrors EpicLoot's ItemInfo types
    // (config/iteminfo.json) so a shard can grant a different effect to each individual item type, while
    // still assigning one effect to a whole group (all melee weapons, all shields, all armor) as a
    // fallback. Shards.ResolveCategory maps a host item to the most specific fine type it can; a fine
    // effect overrides its group and a group effect covers every member that has no fine effect.
    public enum ShardSlotCategory {
        // -------- Groups (broad fallback keys; not resolution targets in the primary path) --------
        MeleeWeapon,
        RangedWeapon,
        MagicWeapon,
        Shield,
        Armor,

        // -------- Fine weapon types (mirror ItemInfo) --------
        Swords,
        Axes,
        TwoHandAxes,
        Knives,
        Fists,
        Clubs,
        Sledges,
        Polearms,
        Spears,
        Pickaxes,
        Tools,
        Torches,
        Bows,
        Staffs,

        // -------- Fine shield types --------
        Bucklers,
        RoundShields,
        TowerShields,

        // -------- Fine armor types --------
        Head,
        Chest,
        Legs,
        Shoulders,

        // -------- Standalone (no group) --------
        Trinket,
        Utility
    }

    public class ShardEffectDefinition {
        public string EffectType;
        public Dictionary<ItemRarity, float> ValuesPerRarity = new Dictionary<ItemRarity, float>();

        // Per-effect tunables, the same shape and semantics as the "Config" block on a
        // magiceffects.json entry: a flat string -> float bag read at runtime through EffectConfig
        // (backed by MagicItemEffectDefinitions.GetEffectConfig) and rendered one key per line in the
        // Shift-detail tooltip. This is where a shard effect's cooldowns, charge counts, durations,
        // radii and caps live, so they can be retuned without rebuilding the DLL.
        //
        // Left empty on purpose: Newtonsoft APPENDS to a pre-initialized collection, and an absent key
        // has to fall through to the effect class's code default rather than merge with it.
        // ShardEffectDefinitions.BuildDefinition owns that merge -- code defaults first, these keys
        // overlaid on top -- which is what lets a partial block tune one knob without blanking the rest.
        //
        // An effect assigned to several slots is authored once per slot, so the same effect type can
        // carry more than one Config. First occurrence wins, exactly as ValuesPerRarity already does;
        // a disagreeing later copy is warned about in ShardEffectDefinitions.CollectShardEffects.
        public Dictionary<string, float> Config = new Dictionary<string, float>();
    }

    public class ShardDefinition {
        public ShardCategory Category = ShardCategory.Core;

        // The rarities this shard can be created/dropped at. Each (color, rarity) is a distinct prefab and
        // a shard's rarity is stored per instance in its MagicItem metadata; this set constrains drop rolls
        // and debug spawns (see Shards.ClampToRaritySet). Left empty here on purpose: Newtonsoft APPENDS
        // to a pre-initialized collection, so a non-empty default would merge with the JSON list;
        // InitializeShardDefinitions backfills all five when the config omits it.
        public List<ItemRarity> Rarities = new List<ItemRarity>();

        // When non-null, the shard grants this single effect on ANY host item type it is allowed
        // into (a "uniform" shard), instead of the per-item-type TypeEffects mapping below.
        public ShardEffectDefinition UniformEffect = null;

        public Dictionary<ShardSlotCategory, ShardEffectDefinition> TypeEffects = new Dictionary<ShardSlotCategory, ShardEffectDefinition>();

        public float GetValue(ShardSlotCategory category, ItemRarity rarity) {
            if (UniformEffect != null) {
                return UniformEffect.ValuesPerRarity.TryGetValue(rarity, out var uniform) ? uniform : 0f;
            }
            if (TypeEffects.TryGetValue(category, out var effectDef)) {
                return effectDef.ValuesPerRarity.TryGetValue(rarity, out var value) ? value : 0f;
            }
            return 0f;
        }
    }

    // Tunables shared by several shard effects rather than owned by one, so they have no single
    // ShardEffectDefinition to hang off (the movement-penalty reference feeds seven shards, the
    // blood-block self-damage share feeds two). Read through EffectConfig.Global, but resolved once
    // per load into static fields on the classes that use them -- several sit on 50Hz vanilla methods
    // where a per-read dictionary lookup would not be free. Empty by default for the Newtonsoft reason
    // on ShardEffectDefinition.Config above.
    public class ShardGlobalConfig {
        public Dictionary<string, float> Values = new Dictionary<string, float>();
    }

    // Root of config/shardstones.json: the full shard effect/rarity grid keyed by color, plus the
    // cross-effect tunables that belong to no single shard.
    public class ShardStonesConfig {
        public Dictionary<ShardType, ShardDefinition> Shards = new Dictionary<ShardType, ShardDefinition>();
        public ShardGlobalConfig Global = new ShardGlobalConfig();
    }

    public static class Shards {
        public static readonly String ShardIndicator = "ShardStone";

        // Per-(color, rarity) prefab stack cap. Each (color, rarity) is a distinct prefab with a distinct
        // display name, so only identical-rarity shards of the same color merge -- up to this many.
        private const int ShardStackSize = 100;

        // Shard effect/rarity definitions, loaded from config/shardstones.json (registered in
        // ELConfig.InitializeConfig). Keyed by color; each carries its own rarity set.
        private static Dictionary<ShardType, ShardDefinition> _definitions =
            new Dictionary<ShardType, ShardDefinition>();

        // Cross-effect tunables from the "Global" block, kept whole so GetCFG can round-trip them to
        // clients. The live values are pushed into static fields by EffectConfig.ApplyGlobalConfig.
        private static ShardGlobalConfig _globalConfig = new ShardGlobalConfig();

        // Config setup hook (SychronizeConfig<ShardStonesConfig>). Backfills defaults so downstream
        // lookups never hit a null Rarities/TypeEffects/Config.
        public static void InitializeShardDefinitions(ShardStonesConfig config) {
            _definitions = config?.Shards ?? new Dictionary<ShardType, ShardDefinition>();
            foreach (var def in _definitions.Values) {
                if (def == null) {
                    continue;
                }
                if (def.Rarities == null || def.Rarities.Count == 0) {
                    def.Rarities = new List<ItemRarity> {
                        ItemRarity.Magic, ItemRarity.Rare, ItemRarity.Epic, ItemRarity.Legendary, ItemRarity.Mythic
                    };
                }
                if (def.TypeEffects == null) {
                    def.TypeEffects = new Dictionary<ShardSlotCategory, ShardEffectDefinition>();
                }

                // An explicit "Config": null in the file would otherwise reach BuildDefinition's merge.
                if (def.UniformEffect != null && def.UniformEffect.Config == null) {
                    def.UniformEffect.Config = new Dictionary<string, float>();
                }
                foreach (var effect in def.TypeEffects.Values) {
                    if (effect != null && effect.Config == null) {
                        effect.Config = new Dictionary<string, float>();
                    }
                }
            }

            _globalConfig = config?.Global ?? new ShardGlobalConfig();
            global::EpicLoot.src.Magic.MagicItemEffects.Helpers.EffectConfig.ApplyGlobalConfig(_globalConfig);

            // The synthesized MagicItemEffectDefinitions are built from this grid, so they go stale the
            // moment it changes. OnSetupMagicItemEffectDefinitions only fires when magiceffects.json
            // reloads -- a different file -- so a live edit here, or a server pushing its copy of
            // shardstones.json, has to rebuild them from this side. Same dual-trigger reason
            // ShardStoneConversions.Merge runs from both its own Initialize and the other file's event.
            // RegisterShardEffectDefinitions replaces its own previous output, so re-running is safe.
            global::EpicLoot.Magic.MagicItemEffects.Helpers.ShardEffectDefinitions.RegisterShardEffectDefinitions();
        }

        public static ShardStonesConfig GetCFG() {
            return new ShardStonesConfig { Shards = _definitions, Global = _globalConfig };
        }

        // Brings a shard's MagicItem in line with its identity. Color and rarity both come from
        // m_shared.m_ammoType, which Unity deep-copies on Instantiate and which is always rebuilt from
        // the prefab on load, so this takes no rarity argument and cannot be called with a wrong one.
        // The MagicItem it creates is purely cosmetic -- it is what makes a shard report IsMagic(),
        // giving it a rarity-colored name, magic background and magic tooltip. Idempotent and cheap:
        // no-ops for non-shards and for shards already carrying the right rarity, so it is safe on hot
        // paths. This is what makes every shard prefab directly safe to Instantiate -- no caller stamps.
        public static bool EnsureShardMetadata(ItemDrop.ItemData item) {
            if (!IsShard(item)) {
                return false;
            }

            var rarity = GetShardRarity(item);
            var mic = item.Data().GetOrCreate<MagicItemComponent>();
            if (mic.MagicItem != null && mic.MagicItem.Rarity == rarity) {
                return false;
            }

            var magicItem = mic.MagicItem ?? new MagicItem();
            magicItem.Rarity = rarity;
            mic.SetMagicItem(magicItem);
            return true;
        }

        // Snaps a rarity to the nearest one in a color's declared set (Rarities in shardstones.json).
        // Returns the input unchanged when the set is empty/undefined or already contains it.
        public static ItemRarity ClampToRaritySet(ShardType color, ItemRarity rarity) {
            var set = ShardDefinitions.Get(color)?.Rarities;
            if (set == null || set.Count == 0 || set.Contains(rarity)) {
                return rarity;
            }
            ItemRarity best = set[0];
            int bestDiff = int.MaxValue;
            foreach (var r in set) {
                int diff = Math.Abs((int)r - (int)rarity);
                if (diff < bestDiff) {
                    bestDiff = diff;
                    best = r;
                }
            }
            return best;
        }

        // Picks a uniformly random rarity from a color's declared set (Rarities in shardstones.json).
        // Falls back to Magic when the set is empty/undefined, mirroring GetShardRarity's default.
        public static ItemRarity RandomRarityFromSet(ShardType color) {
            var set = ShardDefinitions.Get(color)?.Rarities;
            if (set == null || set.Count == 0) {
                return ItemRarity.Magic;
            }
            return set[UnityEngine.Random.Range(0, set.Count)];
        }

        // Accessors kept under the ShardDefinitions name for existing call sites (MagicTooltipShard,
        // ShardEffectDefinitions). Backed by the config loaded above.
        public static class ShardDefinitions {
            public static Dictionary<ShardType, ShardDefinition> ShardEffects => _definitions;

            public static ShardDefinition Get(ShardType color) {
                return ShardEffects.TryGetValue(color, out var def) ? def : null;
            }
        }

        // A shard's identity -- color and rarity -- lives entirely in its m_shared.m_ammoType, formatted
        // as "{Color}|{Rarity}|ShardStone" (e.g. "Yellow|Epic|ShardStone"). m_shared is a serialized
        // field, so Unity deep-copies it on Instantiate and the game rebuilds it from the prefab on every
        // load; that is what makes identity impossible to lose on any spawn path. Custom data
        // (m_customData / MagicItem) is [NonSerialized] and must never be the source of truth here.
        private const int ShardAmmoTypeParts = 3;

        // Check if this is a shard item. Runs on every ItemDrop.Awake (via InitializeCustomData), so it
        // stays cheap and must not throw on a modded item with an unset ammoType.
        public static bool IsShard(ItemDrop.ItemData item) {
            return item?.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material &&
                !string.IsNullOrEmpty(item.m_shared.m_ammoType) &&
                item.m_shared.m_ammoType.EndsWith(ShardIndicator);
        }

        public static ShardType GetShardColor(ItemDrop.ItemData item) {
            if (!TryGetShardParts(item, out var parts)) {
                return ShardType.None;
            }
            return Enum.TryParse(parts[0], true, out ShardType color) ? color : ShardType.None;
        }

        public static ItemRarity GetShardRarity(ItemDrop.ItemData item) {
            if (!TryGetShardParts(item, out var parts)) {
                return ItemRarity.Magic;
            }
            return Enum.TryParse(parts[1], true, out ItemRarity rarity) ? rarity : ItemRarity.Magic;
        }

        private static bool TryGetShardParts(ItemDrop.ItemData item, out string[] parts) {
            parts = null;
            if (!IsShard(item)) {
                return false;
            }
            var split = item.m_shared.m_ammoType.Split('|');
            if (split.Length != ShardAmmoTypeParts) {
                return false;
            }
            parts = split;
            return true;
        }

        public static ShardEffectDefinition GetShardEffect(ItemDrop.ItemData item, ShardType color) {
            if (!ShardDefinitions.ShardEffects.TryGetValue(color, out var colorEffects)) {
                return null;
            }

            // A uniform shard (e.g. a boss shard) grants the same effect on any host item type.
            if (colorEffects.UniformEffect != null) {
                return colorEffects.UniformEffect;
            }

            if (colorEffects.TypeEffects == null) {
                return null;
            }

            // An item we cannot classify gets no effect -- the shard still occupies the socket, it just
            // sits inert (the tooltip says so). Substituting a slot here would hand the item an effect
            // authored for some other kind of gear.
            var slot = ResolveCategory(item);
            if (slot == null) {
                return null;
            }

            // A fine-type effect (e.g. Swords) overrides its group (MeleeWeapon); a group effect covers
            // every member with no fine effect of its own.
            if (colorEffects.TypeEffects.TryGetValue(slot.Value, out var fineEffect)) {
                return fineEffect;
            }
            if (GroupOf(slot.Value) is ShardSlotCategory group &&
                colorEffects.TypeEffects.TryGetValue(group, out var groupEffect)) {
                return groupEffect;
            }
            return null;
        }

        // Maps each EpicLoot ItemInfo type string (config/iteminfo.json "Type") to its fine slot. This is
        // the primary resolution path -- it distinguishes types that share a skill/ItemType (e.g. Axes vs
        // TwoHandAxes, the three shield subtypes) that the fallback heuristic below cannot.
        private static readonly Dictionary<string, ShardSlotCategory> ItemInfoTypeToSlot =
            new Dictionary<string, ShardSlotCategory>(StringComparer.OrdinalIgnoreCase)
            {
                { "Swords", ShardSlotCategory.Swords },
                { "Axes", ShardSlotCategory.Axes },
                { "TwoHandAxes", ShardSlotCategory.TwoHandAxes },
                { "Knives", ShardSlotCategory.Knives },
                { "Fists", ShardSlotCategory.Fists },
                { "Clubs", ShardSlotCategory.Clubs },
                { "Sledges", ShardSlotCategory.Sledges },
                { "Polearms", ShardSlotCategory.Polearms },
                { "Spears", ShardSlotCategory.Spears },
                { "Pickaxes", ShardSlotCategory.Pickaxes },
                { "Tools", ShardSlotCategory.Tools },
                { "Torches", ShardSlotCategory.Torches },
                { "Bows", ShardSlotCategory.Bows },
                { "Staffs", ShardSlotCategory.Staffs },
                { "Bucklers", ShardSlotCategory.Bucklers },
                { "RoundShields", ShardSlotCategory.RoundShields },
                { "TowerShields", ShardSlotCategory.TowerShields },
                { "HeadArmor", ShardSlotCategory.Head },
                { "ChestArmor", ShardSlotCategory.Chest },
                { "LegsArmor", ShardSlotCategory.Legs },
                { "ShouldersArmor", ShardSlotCategory.Shoulders },
                { "Trinket", ShardSlotCategory.Trinket },
                { "Utility", ShardSlotCategory.Utility }
            };

        // The broad group a fine slot falls back to when a shard defines no effect for the exact type.
        // Returns null for standalone slots (Trinket, Utility) and for group values themselves.
        public static ShardSlotCategory? GroupOf(ShardSlotCategory slot) {
            switch (slot) {
                case ShardSlotCategory.Swords:
                case ShardSlotCategory.Axes:
                case ShardSlotCategory.TwoHandAxes:
                case ShardSlotCategory.Knives:
                case ShardSlotCategory.Fists:
                case ShardSlotCategory.Clubs:
                case ShardSlotCategory.Sledges:
                case ShardSlotCategory.Polearms:
                case ShardSlotCategory.Spears:
                case ShardSlotCategory.Pickaxes:
                case ShardSlotCategory.Tools:
                case ShardSlotCategory.Torches:
                    return ShardSlotCategory.MeleeWeapon;
                case ShardSlotCategory.Bows:
                    return ShardSlotCategory.RangedWeapon;
                case ShardSlotCategory.Staffs:
                    return ShardSlotCategory.MagicWeapon;
                case ShardSlotCategory.Bucklers:
                case ShardSlotCategory.RoundShields:
                case ShardSlotCategory.TowerShields:
                    return ShardSlotCategory.Shield;
                case ShardSlotCategory.Head:
                case ShardSlotCategory.Chest:
                case ShardSlotCategory.Legs:
                case ShardSlotCategory.Shoulders:
                    return ShardSlotCategory.Armor;
                default:
                    return null;
            }
        }

        // Maps a host equipment item to the most specific fine slot a shard uses to pick its effect.
        // ItemTypeClassifier answers "which ItemInfo type is this?" for the whole mod -- the item's
        // iteminfo.json entry when it has one, else a raw-field heuristic -- and ItemInfoTypeToSlot
        // is the shard-specific layer on top of that vocabulary.
        //
        // Null means the item could not be classified at all. Callers must treat that as "no slot"
        // rather than substituting one: guessing here is what put weapon effects on armor.
        public static ShardSlotCategory? ResolveCategory(ItemDrop.ItemData item) {
            return ItemInfoTypeToSlot.TryGetValue(ItemTypeClassifier.GetItemInfoType(item), out var mapped)
                ? mapped
                : (ShardSlotCategory?)null;
        }

        // Human-readable label for a slot category, e.g. "$mod_epicloot_shardslot_meleeweapon" ->
        // "Melee Weapon". Falls back to the raw enum name when no localization key is defined.
        public static string GetCategoryDisplayName(ShardSlotCategory category) {
            var token = $"mod_epicloot_shardslot_{category.ToString().ToLowerInvariant()}";
            return Extensions.TryLocalize(token, out var localized) ? localized : category.ToString();
        }

        // Human-readable label for a shard category, e.g. "$mod_epicloot_shardcategory_boss" -> "Boss".
        // Falls back to the raw enum name when no localization key is defined.
        public static string GetCategoryDisplayName(ShardCategory category) {
            var token = $"mod_epicloot_shardcategory_{category.ToString().ToLowerInvariant()}";
            return Extensions.TryLocalize(token, out var localized) ? localized : category.ToString();
        }

        // Display color for each shard color's name text (Compendium/tooltips). One entry per
        // ShardType; anything unmapped renders white.
        private static readonly Dictionary<ShardType, string> ShardNameColors = new Dictionary<ShardType, string>
        {
            { ShardType.Red, "#e6484c" },
            { ShardType.Yellow, "#e8d24a" },
            { ShardType.Cyan, "#4ad9e0" },
            { ShardType.Black, "#9a8fa8" },
            { ShardType.Green, "#5fc65f" },
            { ShardType.Orange, "#f08a35" },
            { ShardType.Pink, "#f08ac0" },
            { ShardType.Purple, "#b070e0" },
            { ShardType.White, "#f2f0e6" },
            { ShardType.Grey, "#b0b0b0" },
            { ShardType.DarkGreen, "#3f9a5c" },
            { ShardType.DarkPurple, "#8a52c0" },
            { ShardType.DarkRed, "#c0392b" },
            { ShardType.DarkBlue, "#4a72d0" },
            { ShardType.Golden, "#d4af37" },
            { ShardType.LightBlue, "#8fd0f0" },
            { ShardType.LightGreen, "#a8e08a" },
            { ShardType.Peach, "#f5b98a" },
            { ShardType.Firewalker, "#ff7a3c" },
            { ShardType.Stormcaller, "#7ec8ff" },
            { ShardType.Eikthyr, "#c9e86a" },
            { ShardType.Elder, "#7fbf6a" },
            { ShardType.Bonemass, "#8fa85c" },
            { ShardType.Moder, "#9fd8f0" },
            { ShardType.Yagluth, "#e07a3c" },
            { ShardType.Queen, "#e0b84a" },
            { ShardType.Fader, "#d06ad0" },
        };

        public static string GetShardNameColor(ShardType color) =>
            ShardNameColors.TryGetValue(color, out var value) ? value : "#ffffff";

        // Prefab name of the lowest-rarity shard of this color -- the stand-in used when a UI needs
        // "an icon for this color" without a concrete shard in hand (e.g. the Compendium sprite
        // atlas). Null when the color has no usable definition.
        public static string GetIconPrefabName(ShardType color) {
            var def = ShardDefinitions.Get(color);
            if (def?.Rarities == null || def.Rarities.Count == 0) {
                return null;
            }

            ItemRarity lowest = def.Rarities[0];
            foreach (ItemRarity rarity in def.Rarities) {
                if (rarity < lowest) {
                    lowest = rarity;
                }
            }

            return $"{color}_{lowest}_ShardStone";
        }

        // Categories whose shards are mutually exclusive: a player may wear at most one socketed
        // shard of each such category at a time. Exclusivity is a property of the category, so it
        // applies uniformly to every color in it.
        private static readonly HashSet<ShardCategory> ExclusiveCategories = new HashSet<ShardCategory>
        {
            ShardCategory.Boss,
            ShardCategory.Unique
        };

        public static bool IsExclusive(ShardCategory category) => ExclusiveCategories.Contains(category);

        // Exclusivity is enforced per category, so a Boss shard and a Unique shard may be worn at the
        // same time -- one of each, not one in total. The player-facing messages therefore have to name
        // the category that actually blocked instead of always saying "boss". Callers build
        // $mod_epicloot_shard_{slug}exclusive, $mod_epicloot_socket_{slug}limit and
        // $mod_epicloot_equip_{slug}limit from this.
        public static string ExclusiveCategorySlug(ShardCategory category) =>
            category == ShardCategory.Unique ? "unique" : "boss";

        public static ShardCategory GetCategory(ShardType color) {
            var def = ShardDefinitions.Get(color);
            return def != null ? def.Category : ShardCategory.Core;
        }

        internal static void CreateAndLoadShardItems() {
            GameObject genericPrefab = EpicAssets.AssetBundle.LoadAsset<GameObject>("_ShardStone");
            CustomItem genericShard = new CustomItem(genericPrefab, false);
            ItemManager.Instance.AddItem(genericShard);
            genericPrefab.SetActive(false);

            var shardPrefabNames = new List<string>();

            foreach (string shardColor in Enum.GetNames(typeof(ShardType))) {
                if (shardColor == "None") {
                    continue;
                }

                Enum.TryParse(shardColor, true, out ShardType color);

                // A stale on-disk shardstones.json (missing this colour entirely, or carrying
                // "Rarities": null) used to NRE here -- inside LoadAssets(), which kills the whole
                // plugin in Awake. Skip the colour instead; its shards just don't exist this session.
                ShardDefinition shardDef = ShardDefinitions.Get(color);
                if (shardDef?.Rarities == null || shardDef.Rarities.Count == 0) {
                    EpicLoot.LogErrorForce($"shardstones.json has no usable definition for shard colour '{shardColor}' " +
                        "(missing entry or empty Rarities); skipping its shard items. Delete the on-disk " +
                        "baseconfig/shardstones.json (or enable AlwaysRefreshCoreConfigs) to regenerate it.");
                    continue;
                }

                foreach (ItemRarity rarity in shardDef.Rarities) {
                    var prefab = UnityEngine.Object.Instantiate(genericPrefab);
                    string PrefabName = $"{shardColor}_{rarity}_ShardStone";
                    prefab.name = PrefabName;
                    ItemDrop pid = prefab.GetComponent<ItemDrop>();
                    pid.m_itemData.m_dropPrefab = prefab;
                    // One sprite per color, by convention. A color added to ShardType before its art
                    // exists in the bundle still produces a working shard -- Unity renders a null sprite
                    // as an empty slot rather than throwing -- so say so once instead of leaving a blank
                    // icon to be diagnosed by eye.
                    var icon = EpicAssets.AssetBundle.LoadAsset<Sprite>($"Assets/EpicLoot/Sprites/Shardstones/{shardColor}.png");
                    if (icon == null) {
                        EpicLoot.LogWarning($"No shardstone sprite found for '{shardColor}' " +
                            $"(Assets/EpicLoot/Sprites/Shardstones/{shardColor}.png); it will have no icon.");
                    }
                    pid.m_itemData.m_shared.m_icons = new Sprite[] { icon };
                    // The ammoType is this shard's identity: color and rarity both live here, in shared
                    // data that survives Instantiate. Everything downstream reads it rather than the
                    // prefab name or any baked custom data.
                    pid.m_itemData.m_shared.m_ammoType = $"{shardColor}|{rarity}|ShardStone";
                    pid.m_itemData.m_shared.m_maxStackSize = ShardStackSize;

                    // Bake the cosmetic MagicItem onto the prefab too, so the paths that Clone() an
                    // ItemData straight off a prefab (conversions, socket reconstruction) get it without
                    // waiting for a hook. Instances created by Instantiate are healed on Awake instead.
                    EnsureShardMetadata(pid.m_itemData);

                    // Include the rarity in the display name so each (color, rarity) prefab is a
                    // distinct name -- that is what keeps different rarities in separate inventory
                    // stacks (vanilla merges by name), e.g. "$mod_epicloot_Rare Red Shardstone".
                    ItemConfig ShardItemConfig = new ItemConfig() {
                        Name = $"{EpicLoot.GetRarityDisplayName(rarity)} $mod_epicloot_shard_{shardColor} $mod_epicloot_assets_shardstone",
                        Description = "$mod_epicloot_assets_shardstone_introduce",
                    };

                    CustomItem custom = new CustomItem(prefab, false, ShardItemConfig);
                    ItemManager.Instance.AddItem(custom);

                    shardPrefabNames.Add(PrefabName);
                }
            }

            // Enable items once things are working so that ZNet issues don't happen.
            // A single idempotent handler activates every registered prefab; a null
            // lookup logs and continues so one missing prefab can't leave the rest inactive.
            void EnableShardItems() {
                foreach (string prefabName in shardPrefabNames) {
                    GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);
                    if (prefab == null) {
                        EpicLoot.LogError($"Could not find shardstone prefab '{prefabName}' to activate.");
                        continue;
                    }

                    prefab.SetActive(true);
                    prefab.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = prefab;
                }
            }

            ItemManager.OnItemsRegistered += EnableShardItems;
        }

    }

}
