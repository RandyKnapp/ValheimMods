using BepInEx.Configuration;
using Jotunn.Configs;
using System.Collections.Generic;

namespace Common {
    /// <summary>Item fields that <see cref="ItemBatchLoader"/> knows how to expose as config entries.</summary>
    public enum ItemStat {
        slash,
        slash_per_level,
        blunt,
        blunt_per_level,
        pierce,
        pierce_per_level,
        pickaxe,
        pickaxe_per_level,
        chop,
        chop_per_level,
        attack_force,
        fire,
        fire_per_level,
        lightning,
        lightning_per_level,
        frost,
        frost_per_level,
        poison,
        poison_per_level,
        spirit,
        spirit_per_level,
        block_armor,
        block_armor_per_level,
        parry,
        block_force,
        block_force_per_level,
        primary_attack_stamina,
        primary_attack_eitr,
        primary_attack_flat_health_cost,
        primary_attack_percent_health_cost,
        primary_attack_health_returned,
        primary_attack_damage_bonus_per_missing_hp,
        primary_attack_projectile_count,
        primary_attack_force_multiply,
        secondary_attack_stamina,
        secondary_attack_eitr,
        secondary_attack_force_multiply,
        secondary_attack_flat_health_cost,
        secondary_attack_percent_health_cost,
        movement_speed,
        bow_draw_speed,
        crossbow_reload_speed,
        crossbow_reload_stamina_drain,
        draw_stamina_drain,
        projectile_velocity,
        projectile_accuracy_max,
        durability,
        durability_per_level,
        max_item_level,
        amount,
        tool_level
    }

    /// <summary>
    /// Groups items in the config file and, by default, in the asset-bundle prefab path.
    /// </summary>
    public enum ItemCategory {
        Arrows,
        Atgeirs,
        Axes,
        Hammers,
        Shields,
        Swords,
        Bows,
        Spears,
        Knives,
        Maces,
        Fists,
        Pickaxes,
        Magics,
        /// <summary>Catch-all for non-weapon craftables (jewellery, belts, trinkets).</summary>
        Misc
    }

    /// <summary>
    /// Authored definition of one config-driven item. Everything above the "Populated at runtime" line is
    /// what a mod fills in; the ConfigEntry handles are wired up by <see cref="ItemBatchLoader"/>.
    /// </summary>
    public class ItemDefinition {
        // Metadata
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public ItemCategory Category { get; set; }
        public string Prefab { get; set; }
        public string Icon { get; set; }

        // Authored defaults, each exposed as a config entry
        public string CraftedAt { get; set; }
        public bool Craftable { get; set; } = true;
        public int ReqStationlevel { get; set; }
        public int CraftAmount { get; set; }
        public Dictionary<ItemStat, ItemStatConfig> ModifableStats { get; set; } = new Dictionary<ItemStat, ItemStatConfig>();
        public Dictionary<HitData.DamageType, HitCustomDamageMod> DamageMods { get; set; }
        public RecipeDefinition Recipe { get; set; }

        // -- Populated at runtime by ItemBatchLoader ------------------------------------------------
        public ConfigEntry<string> CraftedAtCfg { get; set; }
        public ConfigEntry<bool> CraftableCfg { get; set; }
        public ConfigEntry<int> StationLVLCfg { get; set; }
        public ConfigEntry<int> CraftAmountCfg { get; set; }
    }

    public class HitCustomDamageMod {
        public bool Configurable { get; set; } = true;
        public HitData.DamageModifier DamageModifier { get; set; }
        public ConfigEntry<string> DmgModCfg { get; set; }
    }

    public class ItemStatConfig {
        public bool Configurable { get; set; } = true;
        public bool IsInt { get; set; } = false;
        public float Default_value { get; set; }
        public float Min { get; set; } = 0f;
        public float Max { get; set; } = 400f;

        // Only one of these is bound, selected by IsInt.
        public ConfigEntry<float> Cfg { get; set; }
        public ConfigEntry<int> CfgInt { get; set; }
    }

    public class RecipeDefinition {
        /// <summary>Raw CSV form: <c>Prefab,Amount,AmountPerLevel|Prefab,Amount,AmountPerLevel</c>.</summary>
        public ConfigEntry<string> RecipeConfig { get; set; }
        public List<RecipeIngredient> RecipeItems { get; set; }
        public List<RequirementConfig> RecipeReqs { get; set; }
        public Recipe ResolvedRecipe { get; set; }
    }

    public class RecipeIngredient {
        public string Prefab { get; set; }
        public int Amount { get; set; }
        public int UpgradeCost { get; set; } = 0;
    }
}
