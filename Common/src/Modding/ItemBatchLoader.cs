using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Common {
    /// <summary>
    /// Batch-registers config-driven items from an asset bundle and keeps them reconciled to their config
    /// entries for the rest of the session (recipe, station, station level, craft amount, per-stat values
    /// and damage modifiers), including across server config syncs and ObjectDB rebuilds.
    ///
    /// Usage: add every <see cref="ItemDefinition"/> with <see cref="AddDefinition"/>, then call
    /// <see cref="BatchSetup"/> once, after <see cref="ModContext.Initialize"/>.
    ///
    /// State is static: one batch per mod assembly, which is all a shared-project consumer needs.
    /// </summary>
    public class ItemBatchLoader {
        internal static List<ItemDefinition> resourceDefinitions = new List<ItemDefinition>();
        internal static AssetBundle Assets;
        internal static Dictionary<string, string> AddedItems = new Dictionary<string, string>();
        internal static List<string> ArcheryAmmoToAdd = new List<string>();

        /// <summary>
        /// Bundle path for an item prefab; {0} is the category, {1} the prefab name. Set to null to look the
        /// prefab up by its bare name (flat bundles).
        /// </summary>
        public static string PrefabPathFormat = "Assets/Custom/Weapons/{0}/{1}.prefab";

        /// <summary>Bundle path for an item icon; {0} is the icon name. Null looks it up by bare name.</summary>
        public static string IconPathFormat = "Assets/Custom/Icons/{0}.png";

        /// <summary>
        /// Whether Jotunn should fix up JVLmock_ references on the registered prefabs. Set false for prefabs
        /// authored directly against the real game assets, which have no mocks to resolve.
        /// </summary>
        public static bool FixReferences = true;

        // Pending in-world item updates, drained once per frame so an entire burst of SettingChanged
        // handlers (e.g. a full server config sync) costs a single Resources.FindObjectsOfTypeAll scan
        // instead of one scan per changed setting.
        private static readonly List<KeyValuePair<string, Action<ItemDrop.ItemData>>> pendingWorldUpdates = new List<KeyValuePair<string, Action<ItemDrop.ItemData>>>();
        private static bool worldUpdateScheduled = false;

        internal static readonly AcceptableValueList<string> allowedModifiers = new AcceptableValueList<string>(new string[] {
            HitData.DamageModifier.Normal.ToString(),
            HitData.DamageModifier.VeryWeak.ToString(),
            HitData.DamageModifier.Weak.ToString(),
            HitData.DamageModifier.Resistant.ToString(),
            HitData.DamageModifier.VeryResistant.ToString(),
            HitData.DamageModifier.Immune.ToString()
        });

        public bool AddDefinition(ItemDefinition itemdef) {
            resourceDefinitions.Add(itemdef);
            return true;
        }

        /// <summary>
        /// Binds every definition's config entries, registers the items with Jotunn, and hooks up the
        /// change/sync handlers that keep them reconciled.
        /// </summary>
        /// <param name="assetBundle">Bundle to load prefabs and icons from; defaults to <see cref="ModContext.AssetBundle"/>.</param>
        /// <param name="reverse_order">Config entries are ordered by bind order; reversing makes them appear in declaration order.</param>
        public bool BatchSetup(AssetBundle assetBundle = null, bool reverse_order = true) {
            Assets = assetBundle ?? ModContext.AssetBundle;
            if (Assets == null) {
                ModLogger.LogError("No asset bundle available; items will not be registered.");
                return false;
            }
            // Since configs are ordered by when they are connected this allows us to add things in the order they are defined.
            if (reverse_order) {
                resourceDefinitions.Reverse();
            }
            WireConfigDefs();

            bool on_server = ZNet.instance != null && Common.Utils.IsServer();

            if (on_server == false) {
                // This is not needed on the server
                // The server does not actually do anything with prefabs, and is not responsible for modifying them
                BatchAddItems();
                SetupOnChange();
                ItemManager.OnItemsRegistered += AddAmmoItemsToArcheryTarget;
                // Re-apply config driven recipe values whenever the ObjectDB is (re)built. Jotunn re-adds the
                // cached (local-config) recipes on every ObjectDB.Awake, so this reconciles them to the current
                // (possibly server-synced) config values. Also re-apply when admin config arrives from the server.
                ItemManager.OnItemsRegistered += ReapplyAllRecipeConfig;
                SynchronizationManager.OnConfigurationSynchronized += OnModConfigsChanged;
            }

            // Flush to disk
            ModContext.SaveOnSet(true);
            return true;
        }

        private static bool WireConfigDefs() {
            foreach (ItemDefinition itemdef in resourceDefinitions) {
                // Build a compacted display name for reference, this primarily just needs spaces removed.
                itemdef.DisplayName = string.Join("", itemdef.Name.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
                // Blow up if adding a non-unique data control
                AddedItems.Add(itemdef.DisplayName, itemdef.Prefab);
                string section = $"{itemdef.Category} - {itemdef.Name}";
                itemdef.CraftableCfg = ConfigBinder.BindServerConfig(section, $"{itemdef.DisplayName}-craftable", itemdef.Craftable, $"Enable/Disable the crafting recipe for {itemdef.Name}.");
                itemdef.StationLVLCfg = ConfigBinder.BindServerConfig(section, $"{itemdef.DisplayName}-stationRequiredLevel", itemdef.ReqStationlevel, $"Sets the required minimum crafting station level to craft {itemdef.Name}", true, 1, 4);
                itemdef.CraftAmountCfg = ConfigBinder.BindServerConfig(section, $"{itemdef.DisplayName}-craftAmount", itemdef.CraftAmount, $"Sets the amount of {itemdef.Name} crafted per recipe.", true, 1, 50);
                itemdef.CraftedAtCfg = ConfigBinder.BindServerConfig(section, $"{itemdef.DisplayName}-craftedAt", itemdef.CraftedAt, $"Sets the crafting station for {itemdef.Name}.");
                // Setup the modifiable stats that this item has defined
                foreach (KeyValuePair<ItemStat, ItemStatConfig> stat in itemdef.ModifableStats) {
                    if (stat.Value.Configurable == false) { continue; }
                    if (stat.Value.IsInt) {
                        stat.Value.CfgInt = ConfigBinder.BindServerConfig(section, $"{itemdef.DisplayName}-{stat.Key}", (int)stat.Value.Default_value, $"Value for {stat.Key} on {itemdef.Name}", true, (int)stat.Value.Min, (int)stat.Value.Max);
                    } else {
                        stat.Value.Cfg = ConfigBinder.BindServerConfig(section, $"{itemdef.DisplayName}-{stat.Key}", stat.Value.Default_value, $"Value for {stat.Key} on {itemdef.Name}", true, stat.Value.Min, stat.Value.Max);
                    }
                }
                // Set the damage modifiers for this item
                if (itemdef.DamageMods != null) {
                    foreach (KeyValuePair<HitData.DamageType, HitCustomDamageMod> dmgmod in itemdef.DamageMods) {
                        dmgmod.Value.DmgModCfg = ConfigBinder.BindServerConfig(section, $"{itemdef.DisplayName}-{dmgmod.Key}-DamageModifier", dmgmod.Value.DamageModifier.ToString(), $"Damage modifier for {dmgmod.Key} on {itemdef.Name}", allowedModifiers, true);
                    }
                }

                // Build the item recipe
                itemdef.Recipe.RecipeConfig = ConfigBinder.BindServerConfig(section, $"{itemdef.DisplayName}-recipe", BuildStringRecipeFromItemDef(itemdef), $"Recipe for {itemdef.Name}. Should be in the format of Prefab,Amount,AmountPerLevel|Prefab,Amount,AmountPerLevel eg: Wood,12,2|Stone,2,0");
                if (ValidateRecipeConfig(itemdef) == false) {
                    BuildRecipeReqsFromDefault(itemdef);
                }

                // Collapse this item's entries into a single grouped custom drawer to keep the in-game
                // Configuration Manager responsive (one visible row per item instead of ~10-20).
                ItemConfigDrawer.Attach(itemdef);
            }
            return true;
        }

        // TODO: Change batch onchange actions to pass to a queue and execute queue from a couroutine.
        private bool SetupOnChange() {
            foreach (ItemDefinition itemdef in resourceDefinitions) {
                // Need to have config onchange settings available for items which are not enabled to ensure that we can enable them when joining a remote server with different items enabled
                // Craftable config toggle
                itemdef.CraftableCfg.SettingChanged += (_, __) => {
                    ConfigChangeDebouncer.Schedule(itemdef.CraftableCfg, () => EnableDisableItemInDB(itemdef, itemdef.CraftableCfg.Value));
                };
                // Station level config
                itemdef.StationLVLCfg.SettingChanged += (_, __) => {
                    ConfigChangeDebouncer.Schedule(itemdef.StationLVLCfg, () => ModifyItemRecipeLevel(itemdef, itemdef.StationLVLCfg.Value));
                };
                // Modify where the item is crafted
                itemdef.CraftedAtCfg.SettingChanged += (_, __) => {
                    ConfigChangeDebouncer.Schedule(itemdef.CraftedAtCfg, () => ModifyItemRecipeCraftedAt(itemdef));
                };
                // Modify how many of the item are crafted per recipe
                itemdef.CraftAmountCfg.SettingChanged += (_, __) => {
                    ConfigChangeDebouncer.Schedule(itemdef.CraftAmountCfg, () => ModifyItemRecipeCraftAmount(itemdef, itemdef.CraftAmountCfg.Value));
                };

                // All of the configurable stat variables
                foreach (KeyValuePair<ItemStat, ItemStatConfig> stat in itemdef.ModifableStats) {
                    if (stat.Value.Configurable == false) { continue; }
                    object statKey = stat.Value.IsInt ? (object)stat.Value.CfgInt : stat.Value.Cfg;
                    void UpdateFromConfig(object sender, EventArgs args) {
                        ConfigChangeDebouncer.Schedule(statKey, () => {
                            if (ZNet.instance == null || ZNet.instance.enabled == false) { return; }
                            stat.Value.Default_value = stat.Value.IsInt ? stat.Value.CfgInt.Value : stat.Value.Cfg.Value;
                            // Update player items
                            UpdateItemInPlayerInventory(itemdef.Prefab, (ItemDrop.ItemData item) => { ItemDataConfigModifier(stat.Key, stat.Value.Default_value, item); });
                            // Update in world items, batched into a single scan to prevent lag spikes (e.g. on server config sync).
                            EnqueueWorldUpdate(itemdef.Prefab, (ItemDrop.ItemData item) => { ItemDataConfigModifier(stat.Key, stat.Value.Default_value, item); });
                        });
                    }

                    if (stat.Value.IsInt) {
                        stat.Value.CfgInt.SettingChanged += UpdateFromConfig;
                    } else {
                        stat.Value.Cfg.SettingChanged += UpdateFromConfig;
                    }
                }

                // Modify the recipe in the object DB
                itemdef.Recipe.RecipeConfig.SettingChanged += (sender, args) => {
                    ConfigChangeDebouncer.Schedule(itemdef.Recipe.RecipeConfig, () => {
                        if (ValidateRecipeConfig(itemdef)) {
                            ModifyItemRecipeInODB(itemdef);
                        }
                    });
                };

                //Modify the damage modifiers
                if (itemdef.DamageMods == null) { continue; }
                foreach (KeyValuePair<HitData.DamageType, HitCustomDamageMod> dmgmod in itemdef.DamageMods) {
                    dmgmod.Value.DmgModCfg.SettingChanged += (_, __) => {
                        ConfigChangeDebouncer.Schedule(dmgmod.Value.DmgModCfg, () => {
                            if (ZNet.instance == null || ZNet.instance.enabled == false) { return; }
                            HitData.DamageModifier modifier = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), dmgmod.Value.DmgModCfg.Value);
                            // Update player items
                            UpdateItemInPlayerInventory(itemdef.Prefab, (ItemDrop.ItemData item) => { SetItemDamageModifier(modifier, dmgmod.Key, item); });
                            // Update world items, batched into a single scan to prevent lag spikes (e.g. on server config sync).
                            EnqueueWorldUpdate(itemdef.Prefab, (ItemDrop.ItemData item) => { SetItemDamageModifier(modifier, dmgmod.Key, item); });
                        });
                    };
                }
            }
            return true;
        }

        // Idempotently reconciles every item recipe in the live ObjectDB to the current config values.
        // Safe to call repeatedly and from multiple lifecycle events; self guards when no ObjectDB exists.
        private static void ReapplyAllRecipeConfig() {
            if (ObjectDB.instance == null || ObjectDB.instance.m_recipes == null) { return; }
            foreach (ItemDefinition itemdef in resourceDefinitions) {
                // Make sure the recipe is present before we try to modify it. A prior ObjectDB.CopyOtherDB
                // (server join) can drop our custom recipes, so re-add disabled items too - their config is
                // applied here and EnableDisableItemInDB sets the disabled flag last.
                EnsureRecipeInDB(itemdef);
                if (ValidateRecipeConfig(itemdef)) { ModifyItemRecipeInODB(itemdef); }
                ModifyItemRecipeLevel(itemdef, itemdef.StationLVLCfg.Value);
                ModifyItemRecipeCraftedAt(itemdef);
                ModifyItemRecipeCraftAmount(itemdef, itemdef.CraftAmountCfg.Value);
                EnableDisableItemInDB(itemdef, itemdef.CraftableCfg.Value);
            }
            // Refresh an open crafting panel so changed recipes/amounts/enabled state are reflected immediately.
            if (Player.m_localPlayer != null) { Player.m_localPlayer.UpdateKnownRecipesList(); }
        }

        // Fires when admin (server) config is synchronized to this client. Only re-apply when our plugin's
        // config was part of the sync payload to avoid needless work when other mods sync.
        private static void OnModConfigsChanged(object sender, ConfigurationSynchronizationEventArgs e) {
            if (e.UpdatedPluginGUIDs != null && e.UpdatedPluginGUIDs.Count > 0 && !e.UpdatedPluginGUIDs.Contains(ModContext.PluginGuid)) {
                return;
            }
            ReapplyAllRecipeConfig();
        }

        // Resolves a bundle asset, preferring the configured path convention and falling back to the bare
        // name so flat bundles (Jam, AdvancedPortals, EpicLoot) work with the same loader.
        private static T LoadBundleAsset<T>(string pathFormat, string bareName, params object[] formatArgs) where T : UnityEngine.Object {
            if (!string.IsNullOrEmpty(pathFormat)) {
                T viaPath = Assets.LoadAsset<T>(string.Format(pathFormat, formatArgs));
                if (viaPath != null) { return viaPath; }
            }
            return Assets.LoadAsset<T>(bareName);
        }

        private static bool BatchAddItems() {
            foreach (ItemDefinition itemdef in resourceDefinitions) {
                GameObject ItemPrefab = LoadBundleAsset<GameObject>(PrefabPathFormat, itemdef.Prefab, itemdef.Category, itemdef.Prefab);
                if (ItemPrefab == null) {
                    ModLogger.LogError($"Could not find prefab '{itemdef.Prefab}' for {itemdef.Name} in the asset bundle; skipping.");
                    continue;
                }
                // Icon is optional: a prefab authored with its own icon leaves it unset and Jotunn keeps
                // whatever the ItemDrop already carries.
                Sprite ItemSprite = string.IsNullOrEmpty(itemdef.Icon)
                    ? null
                    : LoadBundleAsset<Sprite>(IconPathFormat, itemdef.Icon, itemdef.Icon);
                ItemDrop ItemD = ItemPrefab.GetComponent<ItemDrop>();
                // Modify this items stats
                foreach (KeyValuePair<ItemStat, ItemStatConfig> modstat in itemdef.ModifableStats) {
                    if (modstat.Value.Configurable == false) {
                        ItemDataConfigModifier(modstat.Key, modstat.Value.Default_value, ItemD.m_itemData);
                    } else {
                        if (modstat.Value.IsInt) {
                            ItemDataConfigModifier(modstat.Key, modstat.Value.CfgInt.Value, ItemD.m_itemData);
                        } else {
                            ItemDataConfigModifier(modstat.Key, modstat.Value.Cfg.Value, ItemD.m_itemData);
                        }
                    }
                }
                // Modify this items resistances
                if (itemdef.DamageMods != null) {
                    foreach (KeyValuePair<HitData.DamageType, HitCustomDamageMod> dmgmod in itemdef.DamageMods) {
                        if (dmgmod.Value.Configurable == false || dmgmod.Value.DmgModCfg == null) { continue; }
                        HitData.DamageModifier modifier = (HitData.DamageModifier)Enum.Parse(typeof(HitData.DamageModifier), dmgmod.Value.DmgModCfg.Value);
                        SetItemDamageModifier(modifier, dmgmod.Key, ItemD.m_itemData);
                    }
                }
                ItemConfig itemcfg = new ItemConfig() {
                    Amount = itemdef.CraftAmountCfg.Value,
                    CraftingStation = $"{itemdef.CraftedAtCfg.Value}",
                    MinStationLevel = itemdef.StationLVLCfg.Value,
                    // Always register as enabled so the recipe is added to and retained in the ObjectDB (a
                    // recipe registered disabled never gets cached/retained). The real craftable state is
                    // applied immediately after by ReapplyAllRecipeConfig -> EnableDisableItemInDB, so a
                    // disabled item still lives in the DB (m_enabled=false), stays modifiable, and re-enables
                    // correctly - including after a server ObjectDB copy replaces the recipe list.
                    Enabled = true,
                    Icons = ItemSprite != null ? new[] { ItemSprite } : null,
                    Requirements = itemdef.Recipe.RecipeReqs.ToArray()
                };
                ItemManager.Instance.AddItem(new CustomItem(ItemPrefab, FixReferences, itemcfg));

                // This item needs to be included as a returnable arrow/bolt
                if (itemdef.Category == ItemCategory.Arrows) {
                    ArcheryAmmoToAdd.Add(itemdef.Prefab);
                }
            }
            return true;
        }

        private static void ItemDataConfigModifier(ItemStat target_attribute, float updatedValue, ItemDrop.ItemData itemData) {
            if (itemData == null) { return; }
            switch (target_attribute) {
                // Standard Dmg types
                case ItemStat.slash:
                    itemData.m_shared.m_damages.m_slash = updatedValue;
                    break;
                case ItemStat.slash_per_level:
                    itemData.m_shared.m_damagesPerLevel.m_slash = updatedValue;
                    break;
                case ItemStat.blunt:
                    itemData.m_shared.m_damages.m_blunt = updatedValue;
                    break;
                case ItemStat.blunt_per_level:
                    itemData.m_shared.m_damagesPerLevel.m_blunt = updatedValue;
                    break;
                case ItemStat.pierce:
                    itemData.m_shared.m_damages.m_pierce = updatedValue;
                    break;
                case ItemStat.pierce_per_level:
                    itemData.m_shared.m_damagesPerLevel.m_pierce = updatedValue;
                    break;
                // Special Damage Types
                case ItemStat.pickaxe:
                    itemData.m_shared.m_damages.m_pickaxe = updatedValue;
                    break;
                case ItemStat.pickaxe_per_level:
                    itemData.m_shared.m_damagesPerLevel.m_pickaxe = updatedValue;
                    break;
                case ItemStat.chop:
                    itemData.m_shared.m_damages.m_chop = updatedValue;
                    break;
                case ItemStat.chop_per_level:
                    itemData.m_shared.m_damagesPerLevel.m_chop = updatedValue;
                    break;
                case ItemStat.attack_force:
                    itemData.m_shared.m_attackForce = updatedValue;
                    break;
                case ItemStat.secondary_attack_force_multiply:
                    itemData.m_shared.m_secondaryAttack.m_forceMultiplier = updatedValue;
                    break;
                case ItemStat.primary_attack_force_multiply:
                    itemData.m_shared.m_attack.m_forceMultiplier = updatedValue;
                    break;
                // Elemental Damage Types
                case ItemStat.fire:
                    itemData.m_shared.m_damages.m_fire = updatedValue;
                    break;
                case ItemStat.fire_per_level:
                    itemData.m_shared.m_damagesPerLevel.m_fire = updatedValue;
                    break;
                case ItemStat.lightning:
                    itemData.m_shared.m_damages.m_lightning = updatedValue;
                    break;
                case ItemStat.lightning_per_level:
                    itemData.m_shared.m_damagesPerLevel.m_lightning = updatedValue;
                    break;
                case ItemStat.frost:
                    itemData.m_shared.m_damages.m_frost = updatedValue;
                    break;
                case ItemStat.frost_per_level:
                    itemData.m_shared.m_damagesPerLevel.m_frost = updatedValue;
                    break;
                case ItemStat.poison:
                    itemData.m_shared.m_damages.m_poison = updatedValue;
                    break;
                case ItemStat.poison_per_level:
                    itemData.m_shared.m_damagesPerLevel.m_poison = updatedValue;
                    break;
                case ItemStat.spirit:
                    itemData.m_shared.m_damages.m_spirit = updatedValue;
                    break;
                case ItemStat.spirit_per_level:
                    itemData.m_shared.m_damagesPerLevel.m_spirit = updatedValue;
                    break;
                // Block and parry
                case ItemStat.block_armor:
                    itemData.m_shared.m_blockPower = updatedValue;
                    break;
                case ItemStat.block_armor_per_level:
                    itemData.m_shared.m_blockPowerPerLevel = updatedValue;
                    break;
                case ItemStat.parry:
                    itemData.m_shared.m_timedBlockBonus = updatedValue;
                    break;
                case ItemStat.block_force:
                    itemData.m_shared.m_deflectionForce = updatedValue;
                    break;
                case ItemStat.block_force_per_level:
                    itemData.m_shared.m_deflectionForcePerLevel = updatedValue;
                    break;
                // Costs for attack types
                case ItemStat.primary_attack_stamina:
                    itemData.m_shared.m_attack.m_attackStamina = updatedValue;
                    break;
                case ItemStat.primary_attack_eitr:
                    itemData.m_shared.m_attack.m_attackEitr = updatedValue;
                    break;
                case ItemStat.primary_attack_flat_health_cost:
                    itemData.m_shared.m_attack.m_attackHealth = updatedValue;
                    break;
                case ItemStat.primary_attack_percent_health_cost:
                    itemData.m_shared.m_attack.m_attackHealthPercentage = updatedValue;
                    break;
                case ItemStat.primary_attack_health_returned:
                    itemData.m_shared.m_attack.m_attackHealthReturnHit = updatedValue;
                    break;
                case ItemStat.primary_attack_damage_bonus_per_missing_hp:
                    itemData.m_shared.m_attack.m_damageMultiplierPerMissingHP = updatedValue;
                    break;
                case ItemStat.primary_attack_projectile_count:
                    itemData.m_shared.m_attack.m_projectiles = (int)updatedValue;
                    break;
                case ItemStat.secondary_attack_stamina:
                    itemData.m_shared.m_secondaryAttack.m_attackStamina = updatedValue;
                    break;
                case ItemStat.secondary_attack_eitr:
                    itemData.m_shared.m_secondaryAttack.m_attackEitr = updatedValue;
                    break;
                case ItemStat.secondary_attack_flat_health_cost:
                    itemData.m_shared.m_secondaryAttack.m_attackHealth = updatedValue;
                    break;
                case ItemStat.secondary_attack_percent_health_cost:
                    itemData.m_shared.m_secondaryAttack.m_attackHealthPercentage = updatedValue;
                    break;
                // Speed Modifiers
                case ItemStat.movement_speed:
                    itemData.m_shared.m_movementModifier = updatedValue;
                    break;
                case ItemStat.bow_draw_speed:
                    itemData.m_shared.m_attack.m_drawDurationMin = updatedValue;
                    break;
                case ItemStat.crossbow_reload_speed:
                    itemData.m_shared.m_attack.m_reloadTime = updatedValue;
                    break;
                case ItemStat.crossbow_reload_stamina_drain:
                    itemData.m_shared.m_attack.m_reloadStaminaDrain = updatedValue;
                    break;
                case ItemStat.draw_stamina_drain:
                    itemData.m_shared.m_attack.m_drawStaminaDrain = updatedValue;
                    break;
                case ItemStat.projectile_velocity:
                    itemData.m_shared.m_attack.m_projectileVel = updatedValue;
                    break;
                case ItemStat.projectile_accuracy_max:
                    itemData.m_shared.m_attack.m_projectileAccuracy = (100f - updatedValue);
                    break;
                // Item Modifiers
                case ItemStat.durability:
                    itemData.m_shared.m_maxDurability = updatedValue;
                    break;
                case ItemStat.durability_per_level:
                    itemData.m_shared.m_durabilityPerLevel = updatedValue;
                    break;
                case ItemStat.max_item_level:
                    itemData.m_shared.m_maxQuality = (int)updatedValue;
                    break;
                // 'amount' is the inventory stack size; it is grouped with the other item modifiers in the
                // config drawer and previously fell through to the "unknown stat" warning below.
                case ItemStat.amount:
                    itemData.m_shared.m_maxStackSize = (int)updatedValue;
                    break;
                case ItemStat.tool_level:
                    itemData.m_shared.m_toolTier = (int)updatedValue;
                    break;
                default:
                    ModLogger.LogWarning($"Unknown item stat {target_attribute} for {itemData.m_shared.m_name}");
                    break;
            }
        }

        private static bool ValidateRecipeConfig(ItemDefinition itemdef) {
            List<RequirementConfig> requirements = new List<RequirementConfig>();
            try {
                string[] recipeConfig = itemdef.Recipe.RecipeConfig.Value.Split('|');
                foreach (string ingredient in recipeConfig) {
                    string[] ingredientConfig = ingredient.Split(',');
                    if (ingredientConfig.Length == 1) {
                        // This is the first run or deleted config entry scenario
                        return false;
                    }
                    if (ingredientConfig.Length != 3) {
                        ModLogger.LogWarning($"Invalid ({itemdef.Name}) recipe config detected: {ingredient}. Needs three entries eg: Wood,1,1");
                        return false;
                    }
                    requirements.Add(new RequirementConfig { Item = ingredientConfig[0], Amount = int.Parse(ingredientConfig[1]), AmountPerLevel = int.Parse(ingredientConfig[2]) });
                }
                // Only happens if the recipe is valid
                itemdef.Recipe.RecipeReqs = requirements;
                return true;
            } catch {
                ModLogger.LogWarning($"Recipe is Invalid. Should have the format of Wood,1,1|Stone,2,0 - Prefab,cost,upgrade.");
                return false;
            }
        }

        private static void BuildRecipeReqsFromDefault(ItemDefinition itemdef) {
            List<RequirementConfig> requirements = new List<RequirementConfig>();
            foreach (RecipeIngredient recipeIng in itemdef.Recipe.RecipeItems) {
                requirements.Add(new RequirementConfig { Item = recipeIng.Prefab, Amount = recipeIng.Amount, AmountPerLevel = recipeIng.UpgradeCost });
            }
            itemdef.Recipe.RecipeReqs = requirements;
        }

        private static string BuildStringRecipeFromItemDef(ItemDefinition itemdef) {
            List<string> recipe = new List<string>();
            foreach (RecipeIngredient req in itemdef.Recipe.RecipeItems) {
                recipe.Add($"{req.Prefab},{req.Amount},{req.UpgradeCost}");
            }
            return string.Join("|", recipe.ToArray());
        }

        private static bool ModifyItemRecipeCraftedAt(ItemDefinition itemdef) {
            if (ObjectDB.instance == null || ObjectDB.instance.m_recipes == null) { return false; }

            int index = GetRecipeIndexByPrefab(itemdef.Prefab);
            if (index == -1) {
                ModLogger.LogWarning($"Recipe of {itemdef.Prefab} not found in ObjectDB, recipe will not be modified.");
                return false;
            }
            CraftingStation craftable_at = PrefabManager.Instance.GetPrefab(itemdef.CraftedAtCfg.Value)?.GetComponent<CraftingStation>();
            if (craftable_at == null) {
                ModLogger.LogWarning($"Crafting Station {itemdef.CraftedAtCfg.Value} prefab not found, or does not have a crafting station componet.");
                return false;
            }
            ObjectDB.instance.m_recipes[index].m_craftingStation = craftable_at;
            // repair station should likely be split out into a seperate config
            ObjectDB.instance.m_recipes[index].m_repairStation = craftable_at;
            return true;
        }

        private static void ModifyItemRecipeInODB(ItemDefinition itemdef) {
            if (ObjectDB.instance == null || ObjectDB.instance.m_recipes == null) { return; }

            int recipe_index = GetRecipeIndexByPrefab(itemdef.Prefab);
            if (recipe_index == -1) {
                ModLogger.LogWarning($"Recipe of {itemdef.Prefab} not found in ObjectDB, Recipe will not be modified.");
                return;
            }
            Recipe current_recipe = ObjectDB.instance.m_recipes[recipe_index];
            Recipe newRecipe = current_recipe;
            List<Piece.Requirement> newRequirements = new List<Piece.Requirement>();
            foreach (RequirementConfig req in itemdef.Recipe.RecipeReqs) {
                GameObject resgo = ObjectDB.instance.GetItemPrefab(req.Item);
                if (resgo == null) {
                    ModLogger.LogWarning($"Recipe for {itemdef.Prefab} has an invalid requirement {req.Item}.");
                    return;
                }
                newRequirements.Add(new Piece.Requirement { m_resItem = resgo.GetComponent<ItemDrop>(), m_amount = req.Amount, m_amountPerLevel = req.AmountPerLevel });
            }
            newRecipe.m_resources = newRequirements.ToArray();

            int index = ObjectDB.instance.m_recipes.IndexOf(current_recipe);
            if (index > -1) {
                ObjectDB.instance.m_recipes[index] = newRecipe;
                itemdef.Recipe.ResolvedRecipe = newRecipe;
            } else {
                ModLogger.LogWarning($"Recipe {current_recipe.name} not found in ObjectDB.");
            }
        }

        // Re-add itemdef's cached recipe to the live ObjectDB if a prior ObjectDB.CopyOtherDB (server join)
        // replaced m_recipes and dropped it. Keeps even disabled items present so their recipe can still be
        // modified and correctly re-enabled later.
        private static void EnsureRecipeInDB(ItemDefinition itemdef) {
            if (ObjectDB.instance == null || ObjectDB.instance.m_recipes == null) { return; }
            if (GetRecipeIndexByPrefab(itemdef.Prefab) != -1) { return; }
            if (itemdef.Recipe.ResolvedRecipe != null) {
                ObjectDB.instance.m_recipes.Add(itemdef.Recipe.ResolvedRecipe);
            }
        }

        private static void EnableDisableItemInDB(ItemDefinition itemdef, bool enable) {
            if (ObjectDB.instance == null || ObjectDB.instance.m_recipes == null) { return; }

            int index = GetRecipeIndexByPrefab(itemdef.Prefab);
            if (index == -1) {
                // Recipe was dropped (e.g. server ObjectDB copy). Re-add our cached recipe so a disabled
                // item still lives in the DB and can be modified / re-enabled later.
                if (itemdef.Recipe.ResolvedRecipe != null) {
                    itemdef.Recipe.ResolvedRecipe.m_enabled = enable;
                    ObjectDB.instance.m_recipes.Add(itemdef.Recipe.ResolvedRecipe);
                } else {
                    ModLogger.LogWarning($"Recipe of {itemdef.Prefab} not found in ObjectDB and no cached recipe to re-add.");
                }
                return;
            }
            // recipe exists in the ODB
            ObjectDB.instance.m_recipes[index].m_enabled = enable;
            itemdef.Recipe.ResolvedRecipe = ObjectDB.instance.m_recipes[index];
        }

        private static void ModifyItemRecipeLevel(ItemDefinition itemdef, int level) {
            if (ObjectDB.instance == null || ObjectDB.instance.m_recipes == null) { return; }
            int index = GetRecipeIndexByPrefab(itemdef.Prefab);
            if (index == -1) {
                ModLogger.LogWarning($"Recipe of {itemdef.Prefab} not found in ObjectDB, required level will not be modified.");
                return;
            }
            ObjectDB.instance.m_recipes[index].m_minStationLevel = level;
            // Update the stored recipe so if we use it to target things again it will still be accurate
            itemdef.Recipe.ResolvedRecipe = ObjectDB.instance.m_recipes[index];
        }

        private static void ModifyItemRecipeCraftAmount(ItemDefinition itemdef, int amount) {
            if (ObjectDB.instance == null || ObjectDB.instance.m_recipes == null) { return; }
            int index = GetRecipeIndexByPrefab(itemdef.Prefab);
            if (index == -1) {
                ModLogger.LogWarning($"Recipe of {itemdef.Prefab} not found in ObjectDB, craft amount will not be modified.");
                return;
            }
            ObjectDB.instance.m_recipes[index].m_amount = amount;
            // Update the stored recipe so if we use it to target things again it will still be accurate
            itemdef.Recipe.ResolvedRecipe = ObjectDB.instance.m_recipes[index];
        }

        private static int GetRecipeIndexByPrefab(string prefab) {
            return ObjectDB.instance.m_recipes.FindIndex(m => m.m_item != null && m.m_item.name == prefab);
        }

        private static void SetItemDamageModifier(HitData.DamageModifier modifier, HitData.DamageType type, ItemDrop.ItemData itemData) {
            List<HitData.DamageModPair> temp = itemData.m_shared.m_damageModifiers.Where(entry => entry.m_type != type).ToList();
            if (temp.Count == 0) {
                itemData.m_shared.m_damageModifiers.Clear();
                itemData.m_shared.m_damageModifiers.Add(new HitData.DamageModPair() { m_modifier = modifier, m_type = type });
            } else {
                temp.Add(new HitData.DamageModPair() { m_modifier = modifier, m_type = type });
                itemData.m_shared.m_damageModifiers = temp;
            }
        }

        private static void UpdateItemInPlayerInventory(string prefab, Action<ItemDrop.ItemData> callback) {
            if (Player.m_localPlayer == null) { return; }
            foreach (ItemDrop.ItemData user_item in Player.m_localPlayer.m_inventory.GetAllItems()) {
                if (user_item == null || user_item.m_dropPrefab == null) { continue; }
                if (user_item.m_dropPrefab.name != prefab) { continue; }
                callback(user_item);
            }
        }

        // Queues an in-world item update. All updates enqueued within a frame are applied together in a single
        // scan (see DrainWorldUpdates), collapsing N Resources.FindObjectsOfTypeAll scans - one per changed
        // setting during a config sync - into one.
        private static void EnqueueWorldUpdate(string prefab, Action<ItemDrop.ItemData> callback) {
            // Skip during game shutdown: the ThreadingHelper's MonoBehaviour is destroyed (StartCoroutine
            // would throw) and there are no in-world items left to update. Unity's '==' treats it as null.
            BepInEx.ThreadingHelper host = BepInEx.ThreadingHelper.Instance;
            if (host == null) { return; }
            pendingWorldUpdates.Add(new KeyValuePair<string, Action<ItemDrop.ItemData>>(prefab, callback));
            if (worldUpdateScheduled) { return; }
            worldUpdateScheduled = true;
            host.StartCoroutine(DrainWorldUpdates());
        }

        // Applies all queued in-world item updates using a single Resources.FindObjectsOfTypeAll scan.
        private static IEnumerator DrainWorldUpdates() {
            // Wait a frame so the full burst of SettingChanged handlers (e.g. an entire config sync) enqueues first.
            yield return null;
            try {
                if (pendingWorldUpdates.Count > 0) {
                    foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()) {
                        if (go == null) { continue; }
                        if (!go.TryGetComponent<ItemDrop>(out ItemDrop id)) { continue; }
                        foreach (KeyValuePair<string, Action<ItemDrop.ItemData>> update in pendingWorldUpdates) {
                            if (go.name.StartsWith(update.Key)) {
                                update.Value(id.m_itemData);
                            }
                        }
                    }
                }
            } finally {
                pendingWorldUpdates.Clear();
                worldUpdateScheduled = false;
            }
        }

        private static void AddAmmoItemsToArcheryTarget() {
            if (ArcheryAmmoToAdd.Count == 0) { return; }
            GameObject archerTarget = PrefabManager.Instance.GetPrefab("piece_ArcheryTarget");
            if (archerTarget == null) { return; }
            ArcheryTarget ArcherAmmoManger = archerTarget.GetComponentInChildren<ArcheryTarget>(true);
            if (ArcherAmmoManger == null) { return; }

            foreach (string ammoPrefab in ArcheryAmmoToAdd) {
                ModLogger.LogDebug($"Adding {ammoPrefab} to Archery Target Ammo Return.");
                ItemDrop ammoID = PrefabManager.Instance.GetPrefab(ammoPrefab)?.GetComponent<ItemDrop>();
                if (ammoID != null && ArcherAmmoManger.m_returnAmmo.Contains(ammoID) == false) {
                    ArcherAmmoManger.m_returnAmmo.Add(ammoID);
                }
            }
        }
    }
}
