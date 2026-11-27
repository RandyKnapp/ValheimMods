using EpicLoot.Crafting;
using EpicLoot.GatedItemType;
using EpicLoot.General;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.ShardStones {
    // Serialized by ordinal on socketed gear: never renumber or insert into the existing ranges.
    public enum ShardType {
        Red = 0,
        Yellow = 1,
        Cyan = 2,
        Black = 3,
        Green = 4,
        Orange = 5,
        Pink = 6,
        Purple = 7,
        White = 8,
        Grey = 9,
        DarkGreen = 30,
        DarkPurple = 31,
        DarkRed = 32,
        DarkBlue = 33,
        Golden = 34,
        LightBlue = 40,
        LightGreen = 41,
        Peach = 42,
        Firewalker = 70,
        Stormcaller = 71,
        Eikthyr = 90,
        Elder = 91,
        Bonemass = 92,
        Moder = 93,
        Yagluth = 94,
        Queen = 95,
        Fader = 96,

        // Pinned so new shards can follow Fader. Saves before 0.16.0 hold it as 97 (SocketedEffect.LegacyNone).
        None = 404
    }

    public enum ShardCategory {
        Core,
        Dark,
        Light,
        Boss,
        Unique
    }

    // Groups first (fallback keys only, never a resolution result), then the fine types, which mirror
    // the iteminfo.json types.
    public enum ShardSlotCategory {
        MeleeWeapon,
        RangedWeapon,
        MagicWeapon,
        Shield,
        Armor,

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

        Bucklers,
        RoundShields,
        TowerShields,

        Head,
        Chest,
        Legs,
        Shoulders,

        Trinket,
        Utility
    }

    public class ShardEffectDefinition {
        public string EffectType;
        public Dictionary<ItemRarity, float> ValuesPerRarity = new Dictionary<ItemRarity, float>();

        // Left empty: Newtonsoft appends to a pre-initialized collection, and an absent key has to fall
        // through to the effect's code default (merged in ShardEffectDefinitions.BuildDefinition).
        public Dictionary<string, float> Config = new Dictionary<string, float>();
    }

    public class ShardDefinition {
        public ShardCategory Category = ShardCategory.Core;

        // Left empty for the same Newtonsoft reason; InitializeShardDefinitions backfills every rarity.
        public List<ItemRarity> Rarities = new List<ItemRarity>();

        // When set, the shard grants this one effect on any host item type instead of TypeEffects.
        public ShardEffectDefinition UniformEffect = null;

        // Existing stones stay stored if a mapping is removed; new sockets need a matching effect.
        public Dictionary<ShardSlotCategory, ShardEffectDefinition> TypeEffects = new Dictionary<ShardSlotCategory, ShardEffectDefinition>();
    }

    // Tunables shared by several shard effects. Resolved once per load into static fields on their
    // owners (EffectConfig.ApplyGlobalConfig), since several sit on 50Hz vanilla methods.
    public class ShardGlobalConfig {
        public Dictionary<string, float> Values = new Dictionary<string, float>();
    }

    public class ShardStonesConfig {
        public Dictionary<ShardType, ShardDefinition> Shards = new Dictionary<ShardType, ShardDefinition>();
        public ShardGlobalConfig Global = new ShardGlobalConfig();
    }

    public static class Shards {
        public static readonly String ShardIndicator = "ShardStone";

        private const int ShardStackSize = 100;

        private static Dictionary<ShardType, ShardDefinition> _definitions =
            new Dictionary<ShardType, ShardDefinition>();

        // Kept whole so GetCFG can round-trip it to clients.
        private static ShardGlobalConfig _globalConfig = new ShardGlobalConfig();

        // Counts the grid's loads. Socketed stones carry values baked from the grid, so an item worked out
        // under another count re-derives them on its next read (MagicItemComponent.RefreshSocketValues).
        public static int GridVersion { get; private set; }

        public static void InitializeShardDefinitions(ShardStonesConfig config) {
            _definitions = config?.Shards ?? new Dictionary<ShardType, ShardDefinition>();
            foreach (var def in _definitions.Values) {
                if (def == null) {
                    continue;
                }
                if (def.Rarities == null || def.Rarities.Count == 0) {
                    def.Rarities = new List<ItemRarity>(Rarities.All);
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

            // The synthesized definitions are built from this grid, and OnSetupMagicItemEffectDefinitions
            // only fires when magiceffects.json reloads, so a live edit or server push of this file has to
            // rebuild them from here. Re-running is safe: it replaces its own previous output.
            global::EpicLoot.Magic.MagicItemEffects.Helpers.ShardEffectDefinitions.RegisterShardEffectDefinitions();

            GridVersion++;
            ShardSocketManager.OnRuneSocketRulesChanged();
        }

        // Shard Stack Mode and Shard Stack Decay Factor are baked into socket values like the grid is, so a change to
        // either (an edit or the server's push) re-derives every loaded item's sockets: the player's inventory, items on
        // the ground and the inventories of loaded containers. Anything else catches up on its next read.
        public static void ReapplySocketStacking() {
            GridVersion++;

            Inventory inventory = Player.m_localPlayer != null ? Player.m_localPlayer.GetInventory() : null;
            if (inventory != null) {
                RefreshSocketValues(inventory);
            }
            foreach (ItemDrop drop in ItemDrop.s_instances) {
                if (drop != null) {
                    MagicItemData.RefreshSocketValues(drop.m_itemData);
                }
            }
            foreach (Container container in UnityEngine.Object.FindObjectsByType<Container>(FindObjectsSortMode.None)) {
                Inventory containerInventory = container.GetInventory();
                if (containerInventory != null) {
                    RefreshSocketValues(containerInventory);
                }
            }

            ShardSocketManager.OnRuneSocketRulesChanged();
            ItemTooltip.InvalidateCache();
        }

        private static void RefreshSocketValues(Inventory inventory) {
            foreach (ItemDrop.ItemData item in inventory.GetAllItems()) {
                MagicItemData.RefreshSocketValues(item);
            }
        }

        public static ShardStonesConfig GetCFG() {
            return new ShardStonesConfig { Shards = _definitions, Global = _globalConfig };
        }

        // The MagicItem is cosmetic only (rarity-colored name, magic background and tooltip); identity
        // comes from m_ammoType. Idempotent and cheap, so safe on hot paths.
        public static bool EnsureShardMetadata(ItemDrop.ItemData item) {
            if (!IsShard(item)) {
                return false;
            }

            var rarity = GetShardRarity(item);
            var existing = item.GetMagicItem();
            if (existing != null && existing.Rarity == rarity) {
                return false;
            }

            var magicItem = existing ?? new MagicItem();
            magicItem.Rarity = rarity;
            item.SaveMagicItem(magicItem);
            return true;
        }

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

        public static ItemRarity RandomRarityFromSet(ShardType color) {
            var set = ShardDefinitions.Get(color)?.Rarities;
            if (set == null || set.Count == 0) {
                return ItemRarity.Magic;
            }
            return set[UnityEngine.Random.Range(0, set.Count)];
        }

        public static class ShardDefinitions {
            public static Dictionary<ShardType, ShardDefinition> ShardEffects => _definitions;

            public static ShardDefinition Get(ShardType color) {
                return ShardEffects.TryGetValue(color, out var def) ? def : null;
            }
        }

        // A shard's identity is its m_shared.m_ammoType, "{Color}|{Rarity}|ShardStone". m_shared is
        // serialized, so Unity deep-copies it on Instantiate and the game rebuilds it from the prefab on
        // every load; m_customData is [NonSerialized] and must never be the source of truth here.
        private const int ShardAmmoTypeParts = 3;

        // Runs on every ItemDrop.Awake, so it stays cheap and must not throw on an unset ammoType.
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

            if (colorEffects.UniformEffect != null) {
                return colorEffects.UniformEffect;
            }

            if (colorEffects.TypeEffects == null) {
                return null;
            }

            // An unclassifiable item leaves the shard inert; substituting a slot would hand it an effect
            // authored for some other kind of gear.
            var slot = ResolveCategory(item);
            if (slot == null) {
                return null;
            }

            if (colorEffects.TypeEffects.TryGetValue(slot.Value, out var fineEffect)) {
                return fineEffect;
            }
            if (GroupOf(slot.Value) is ShardSlotCategory group &&
                colorEffects.TypeEffects.TryGetValue(group, out var groupEffect)) {
                return groupEffect;
            }
            return null;
        }

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

        // Null means unclassifiable. Callers must treat that as "no slot" rather than substituting one:
        // guessing here is what put weapon effects on armor.
        public static ShardSlotCategory? ResolveCategory(ItemDrop.ItemData item) {
            return ItemInfoTypeToSlot.TryGetValue(ItemTypeClassifier.GetItemInfoType(item), out var mapped)
                ? mapped
                : (ShardSlotCategory?)null;
        }

        public static string GetCategoryDisplayName(ShardSlotCategory category) {
            var token = $"mod_epicloot_shardslot_{category.ToString().ToLowerInvariant()}";
            return Extensions.TryLocalize(token, out var localized) ? localized : category.ToString();
        }

        public static string GetCategoryDisplayName(ShardCategory category) {
            var token = $"mod_epicloot_shardcategory_{category.ToString().ToLowerInvariant()}";
            return Extensions.TryLocalize(token, out var localized) ? localized : category.ToString();
        }

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

        internal static readonly HashSet<ShardCategory> ExclusiveCategories = new HashSet<ShardCategory>
        {
            ShardCategory.Boss,
            ShardCategory.Unique
        };

        public static bool IsExclusive(ShardCategory category) => ExclusiveCategories.Contains(category);

        // Callers build $mod_epicloot_shard_{slug}exclusive, $mod_epicloot_socket_{slug}limit and
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

                // A stale on-disk shardstones.json can lack a colour; this runs inside LoadAssets, where
                // a throw kills the whole plugin, so skip it instead.
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
                    // Unity renders a null sprite as an empty slot rather than throwing, so a missing
                    // icon would otherwise go unnoticed.
                    var icon = EpicAssets.AssetBundle.LoadAsset<Sprite>($"Assets/EpicLoot/Sprites/Shardstones/{shardColor}.png");
                    if (icon == null) {
                        EpicLoot.LogWarning($"No shardstone sprite found for '{shardColor}' " +
                            $"(Assets/EpicLoot/Sprites/Shardstones/{shardColor}.png); it will have no icon.");
                    }
                    pid.m_itemData.m_shared.m_icons = new Sprite[] { icon };
                    // _ShardStone is a variant of CraftingMaterialBase and still carries its m_variant of
                    // 1, which with one icon makes vanilla's ItemDrop.EnsureValid log an error on every
                    // world spawn of a shard.
                    pid.m_itemData.m_variant = 0;
                    pid.m_itemData.m_shared.m_ammoType = $"{shardColor}|{rarity}|ShardStone";
                    pid.m_itemData.m_shared.m_maxStackSize = ShardStackSize;

                    // On the prefab too, for the paths that Clone() an ItemData straight off a prefab
                    // (conversions, socket reconstruction) and so never run Awake.
                    EnsureShardMetadata(pid.m_itemData);

                    // Vanilla merges stacks by name, so the rarity in the name keeps rarities apart.
                    ItemConfig ShardItemConfig = new ItemConfig() {
                        Name = $"{EpicLoot.GetRarityDisplayName(rarity)} $mod_epicloot_shard_{shardColor} $mod_epicloot_assets_shardstone",
                        Description = "$mod_epicloot_assets_shardstone_introduce",
                    };

                    CustomItem custom = new CustomItem(prefab, false, ShardItemConfig);
                    ItemManager.Instance.AddItem(custom);

                    shardPrefabNames.Add(PrefabName);
                }
            }

            // Activated only once items are registered, to avoid ZNet issues.
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
