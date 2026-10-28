using BepInEx;
using BepInEx.Configuration;
using Common;
using EpicLoot.Abilities;
using EpicLoot.Adventure;
using EpicLoot.Biomes;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using EpicLoot.GatedItemType;
using EpicLoot.LegendarySystem;
using EpicLoot.Magic;
using EpicLoot.Patching;
using EpicLoot.ShardStones;
using EpicLoot_UnityLib;
using Jotunn.Entities;
using Jotunn.Managers;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static EpicLoot.Magic.AutoAddEnchantableItems;

namespace EpicLoot.Config;

internal class ELConfig {
    public static ConfigFile cfg;

    public static ConfigEntry<string> _setItemColor;
    public static ConfigEntry<string> _magicRarityColor;
    public static ConfigEntry<string> _rareRarityColor;
    public static ConfigEntry<string> _epicRarityColor;
    public static ConfigEntry<string> _legendaryRarityColor;
    public static ConfigEntry<string> _mythicRarityColor;
    public static ConfigEntry<string> _ancientRarityColor;
    public static ConfigEntry<int> _magicMaterialIconColor;
    public static ConfigEntry<int> _rareMaterialIconColor;
    public static ConfigEntry<int> _epicMaterialIconColor;
    public static ConfigEntry<int> _legendaryMaterialIconColor;
    public static ConfigEntry<int> _mythicMaterialIconColor;
    public static ConfigEntry<int> _ancientMaterialIconColor;
    public static ConfigEntry<bool> UseScrollingCraftDescription;
    public static ConfigEntry<bool> ShowRarityInRecipeList;
    public static ConfigEntry<bool> ShowEnchantSelectionChance;
    public static ConfigEntry<EffectRarityFlare> EffectRarityFlareMode;
    public static ConfigEntry<bool> TransferMagicItemToCrafts;
    public static ConfigEntry<bool> _loggingEnabled;
    public static ConfigEntry<LogLevel> _logLevel;
    public static ConfigEntry<bool> UseGeneratedMagicItemNames;
    public static ConfigEntry<bool> KeepInventoryOpenOverItems;
    public static ConfigEntry<GatedItemTypeMode> _gatedItemTypeModeConfig;
    public static ConfigEntry<GatedBountyMode> BossBountyMode;
    public static ConfigEntry<GatedPieceTypeMode> GatedFreebuildMode;
    public static ConfigEntry<BossDropMode> _bossTrophyDropMode;
    public static ConfigEntry<float> _bossTrophyDropPlayerRange;
    public static ConfigEntry<int> _andvaranautRange;
    public static ConfigEntry<bool> ShowEquippedAndHotbarItemsInSacrificeTab;
    public static ConfigEntry<bool> ShowStorageCounts;
    public static ConfigEntry<bool> RespectItemFavorites;
    public static ConfigEntry<bool> ShowFavoriteStars;
    public static ConfigEntry<bool> RequestLootRolls;
    public static ConfigEntry<ItemRarity> LootRollMinimumRarity;
    public static ConfigEntry<bool> LootRollBossesOnly;
    public static ConfigEntry<float> LootRollPlayerRange;
    public static ConfigEntry<int> LootRollMinimumPlayers;
    public static ConfigEntry<bool> _adventureModeEnabled;
    public static readonly ConfigEntry<string>[] AbilityKeyCodes = new ConfigEntry<string>[AbilityController.AbilitySlotCount];
    public static readonly ConfigEntry<KeyCode>[] AbilityGamepadButtons = new ConfigEntry<KeyCode>[AbilityController.AbilitySlotCount];
    public static ConfigEntry<bool> AbilityGamepadUseAltKey;
    public static ConfigEntry<TextAnchor> AbilityBarAnchor;
    public static ConfigEntry<Vector2> AbilityBarPosition;
    public static ConfigEntry<TextAnchor> AbilityBarLayoutAlignment;
    public static ConfigEntry<float> AbilityBarIconSpacing;
    public static ConfigEntry<KeyCode> OverwhelmingLaunchKey;
    public static ConfigEntry<KeyCode> OverwhelmingLaunchGamepadButton;
    public static ConfigEntry<KeyCode> SpiritAnimalKey;
    public static ConfigEntry<KeyCode> SpiritAnimalGamepadButton;
    public static ConfigEntry<KeyCode> EarthshakerKey;
    public static ConfigEntry<KeyCode> EarthshakerGamepadButton;
    public static ConfigEntry<bool> ShowOwnFalconWings;
    public static ConfigEntry<KeyCode> EitrInfusionKey;
    public static ConfigEntry<KeyCode> EitrInfusionGamepadButton;
    public static ConfigEntry<KeyCode> ShieldThrowKey;
    public static ConfigEntry<KeyCode> ShieldThrowGamepadButton;
    public static ConfigEntry<float> SetItemDropChance;
    public static ConfigEntry<bool> HealthCriticalEffectsEnabled;
    public static ConfigEntry<bool> AllowRuneSocketing;
    public static ConfigEntry<bool> AllowDuplicateSocketedEffects;
    public static ConfigEntry<bool> AllowShardstoneDuplicateItemEffect;
    public static ConfigEntry<bool> AllowRunestoneDuplicateItemEffect;
    // Named ...RemovalMode rather than ...SocketMode: a static field sharing its type's name would
    // shadow the enum inside this class and break `ShardSocketMode.Free` in the bind call below.
    public static ConfigEntry<ShardSocketMode> ShardSocketRemovalMode;
    public static ConfigEntry<RuneSocketMode> RuneSocketRemovalMode;
    // Named ...StackingMode for the same shadowing reason as ShardSocketRemovalMode above.
    public static ConfigEntry<ShardStackMode> ShardStackingMode;
    public static ConfigEntry<float> ShardStackDecayFactor;
    public static ConfigEntry<bool> RuneDiminishingReturns;
    public static ConfigEntry<float> RuneStackDecayFactor;
    public static ConfigEntry<bool> AllowGiftOnItemsWithSlots;
    public static ConfigEntry<int> LegendaryGiftSlotsAdded;
    public static ConfigEntry<int> MythicGiftSlotsAdded;
    public static ConfigEntry<int> AncientGiftSlotsAdded;
    public static ConfigEntry<float> LegendaryGiftSuccessChance;
    public static ConfigEntry<float> MythicGiftSuccessChance;
    public static ConfigEntry<float> AncientGiftSuccessChance;
    public static ConfigEntry<KeyboardShortcut> SocketOverlayShortcut;
    public static ConfigEntry<SocketGamepadButton> SocketOverlayGamepadButton;
    public static ConfigEntry<SocketGamepadModifier> SocketOverlayGamepadModifier;
    public static ConfigEntry<Vector2> ShardSlotPanelOffset;
    private const string SocketOverlayShortcutKey = "Socket Overlay Shortcut";
    /// <summary>The settings Socket Overlay Shortcut replaced, and their old defaults; see MigrateSocketOverlayShortcut.</summary>
    private const string LegacySocketOverlayKey = "Socket Overlay Key";
    private const KeyCode LegacySocketOverlayKeyDefault = KeyCode.LeftControl;
    private const string LegacyAlwaysRequireSocketOverlayKey = "Always Require Socket Overlay Key";
    private const string LegacySocketOverlayModifierKey = "Socket Overlay Modifier";
    private const KeyCode LegacySocketOverlayModifierDefault = KeyCode.LeftAlt;
    public static ConfigEntry<float> GlobalDropRateModifier;
    public static ConfigEntry<float> StarLootScaling;
    public static ConfigEntry<bool> DeferChestLootRoll;
    public static ConfigEntry<bool> RemovePurchasedGambles;

    public static ConfigEntry<FirstRunMode> WelcomeWizardMode;
    /// <summary>The Welcome Wizard's line in the shared per-user first-run record. Never change it once shipped.</summary>
    internal const string WelcomeWizardKey = "EpicLoot";
    /// <summary>Raise to show a reworked Welcome Wizard to users who have seen this one.</summary>
    internal const int WelcomeWizardRevision = 1;
    /// <summary>The per-profile flag WelcomeWizardMode replaced; see MigrateWelcomeWizard.</summary>
    private const string LegacyWelcomeMessageKey = "Show Welcome Message, automatically set to false once config is viewed.";
    public static ConfigEntry<bool> OutputPatchedConfigFiles;
    public static ConfigEntry<bool> VerifyPenaltyScalingCache;
    public static ConfigEntry<bool> EnchantingTableUpgradesActive;
    public static ConfigEntry<bool> EnableLimitedBountiesInProgress;
    public static ConfigEntry<int> MaxInProgressBounties;
    public static ConfigEntry<EnchantingTabs> EnchantingTableActivatedTabs;
    public static ConfigEntry<bool> AutoDepositMaterials;
    public static ConfigEntry<BossDropMode> _bossCryptKeyDropMode;
    public static ConfigEntry<float> _bossCryptKeyDropPlayerRange;
    public static ConfigEntry<BossDropMode> _bossWishboneDropMode;
    public static ConfigEntry<float> _bossWishboneDropPlayerRange;
    public static ConfigEntry<BossExtraDropMode> ModerDropMode;
    public static ConfigEntry<float> ModerDropPlayerRange;
    public static ConfigEntry<BossExtraDropMode> YagluthDropMode;
    public static ConfigEntry<float> YagluthDropPlayerRange;
    public static ConfigEntry<BossExtraDropMode> QueenDropMode;
    public static ConfigEntry<float> QueenDropPlayerRange;
    public static ConfigEntry<BossExtraDropMode> FaderDropMode;
    public static ConfigEntry<float> FaderDropPlayerRange;
    public static ConfigEntry<BossExtraDropMode> FrozenKingDropMode;
    public static ConfigEntry<float> FrozenKingDropPlayerRange;
    public static ConfigEntry<bool> EliteRunestoneDrops;
    public static ConfigEntry<float> EliteRunestoneDropChance;
    public static ConfigEntry<string> BalanceConfigurationType;
    public static ConfigEntry<bool> AutoAddEquipment;
    public static ConfigEntry<bool> AutoRemoveEquipmentNotFound;
    public static ConfigEntry<bool> OnlyAddEquipmentWithRecipes;
    public static ConfigEntry<float> ItemDropRatio;
    public static ConfigEntry<float> MaterialsDropRatio;
    public static ConfigEntry<float> ItemsUnidentifiedDropRatio;
    public static ConfigEntry<float> ShardStoneDropRatio;
    public static ConfigEntry<float> UIAudioVolumeAdjustment;
    public static ConfigEntry<bool> AutoAddRemoveEquipmentFromVendor;
    public static ConfigEntry<bool> AutoAddRemoveEquipmentFromLootLists;
    public static ConfigEntry<bool> AutoAddCreaturesToLootTables;
    public static ConfigEntry<bool> EnableHotReloadPatches;
    public static ConfigEntry<bool> AlwaysRefreshCoreConfigs;
    public static ConfigEntry<int> TooltipMaxWidth;
    public static ConfigEntry<int> TooltipMaxHeight;
    public static ConfigEntry<float> TraderPanelPositionX;
    public static ConfigEntry<float> TraderPanelPositionY;
    public static ConfigEntry<float> TemperPanelPositionX;
    public static ConfigEntry<float> TemperPanelPositionY;
    public static ConfigEntry<KeyCode> TraderPanelDragKey;
    public static ConfigEntry<bool> ShowQuickConfigButton;
    public static ConfigEntry<bool> ShowAdvancedQuickConfigPages;

    public static ConfigEntry<RuneExtractMode> RuneExtractItemMode;
    public static ConfigEntry<RuneSetExtractMode> RuneSetExtractItemMode;
    public static ConfigEntry<string> SpendOnlyStorageMods;

    public static ConfigEntry<bool> TemperFailDowngrades;
    public static ConfigEntry<bool> TemperDestroysItem;
    public static ConfigEntry<float> TemperChanceToDestroy;
    public static ConfigEntry<float> TemperBaseChance;
    public static ConfigEntry<float> TemperDecrement;

    private static CustomRPC LootTablesRPC;
    private static CustomRPC MagicEffectsRPC;
    private static CustomRPC ItemConfigRPC;
    private static CustomRPC EnchantingCostsRPC;
    private static CustomRPC ItemNamesRPC;
    private static CustomRPC AdventureDataRPC;
    private static CustomRPC LegendariesRPC;
    private static CustomRPC AbilitiesRPC;
    private static CustomRPC MaterialConversionRPC;
    private static CustomRPC EnchantingUpgradesRPC;
    private static CustomRPC AutoSorterConfigurationRPC;
    private static CustomRPC ShardStonesRPC;
    private static CustomRPC ShardStoneConversionsRPC;
    private static CustomRPC BiomeDataRPC;

    private static string LocalizationDir = GetLocalizationDirectoryPath();
    private static readonly List<string> LocalizationLanguages = new List<string>() {
        "English",
        "Swedish",
        "French",
        "Italian",
        "German",
        "Spanish",
        "Russian",
        "Romanian",
        "Bulgarian",
        "Macedonian",
        "Finnish",
        "Danish",
        "Norwegian",
        "Icelandic",
        "Turkish",
        "Lithuanian",
        "Czech",
        "Hungarian",
        "Slovak",
        "Polish",
        "Dutch",
        "Portuguese_European",
        "Portuguese_Brazilian",
        "Chinese",
        "Chinese_Trad",
        "Japanese",
        "Korean",
        "Hindi",
        "Thai",
        "Abenaki",
        "Croatian",
        "Georgian",
        "Greek",
        "Serbian",
        "Ukrainian",
        "Latvian",
        "Vietnamese"
    };

    // Sections, in the order they are bound and therefore the order the player sees them. The ordered
    // binder numbers each section as it is first bound ("2 - Balance"), and that number is part of the
    // name on disk, so:
    //  - there can be at most nine sections; a tenth sorts as "10 - " between "1 - " and "2 - ".
    //  - moving a section renames it, which orphans the player's values. RelocatedSettings below carries
    //    them across, and StripOrderPrefix makes a pure renumbering (a section inserted in the middle)
    //    survive on its own.
    private const string SectionGeneral = "General";
    private const string SectionBalance = "Balance";
    private const string SectionSockets = "Shardstones & Runes";
    private const string SectionEnchanting = "Enchanting Table";
    private const string SectionAdventure = "Adventure";
    private const string SectionInterface = "Interface";
    private const string SectionItemColors = "Item Colors";
    private const string SectionAbilities = "Abilities";
    private const string SectionDebug = "Debug";

    // Migration dictionary for historic settings
    // TODO: Delete after a few releases, once the player base has had time to migrate their configs.
    private static readonly Dictionary<string, string> RelocatedSettings = new Dictionary<string, string> {
        // Rune extraction is a socket operation, not a drop-rate one.
        { $"{SectionSockets}::Rune Extract Mode", "Balance::Rune Extract Mode" },
        // Tempering folded into the enchanting table section; its keys were only unambiguous while they
        // had a section of their own.
        { $"{SectionEnchanting}::Temper Base Chance", "Tempering::Base Chance" },
        { $"{SectionEnchanting}::Temper Decrement Amount", "Tempering::Decrement Amount" },
        { $"{SectionEnchanting}::Temper Fail Destroys Item", "Tempering::Fail Destroys Item" },
        { $"{SectionEnchanting}::Temper Destroy Chance", "Tempering::Destroy Chance" },
        // Adventure mode gathered out of Balance and the old Bounty Management section.
        { $"{SectionAdventure}::Adventure Mode Enabled", "Balance::Adventure Mode Enabled" },
        { $"{SectionAdventure}::Andvaranaut Range", "Balance::Andvaranaut Range" },
        { $"{SectionAdventure}::Gated Bounty Mode", "Balance::Gated Bounty Mode" },
        { $"{SectionAdventure}::Enable Bounty Limit", "Bounty Management::Enable Bounty Limit" },
        { $"{SectionAdventure}::Max Bounties Per Player", "Bounty Management::Max Bounties Per Player" },
        // Interface absorbed Crafting UI, Tooltips and the panel positions that sat under General.
        { $"{SectionInterface}::Use Scrolling Craft Description", "Crafting UI::Use Scrolling Craft Description" },
        { $"{SectionInterface}::Show Enchant Selection Chance", "Crafting UI::Show Enchant Selection Chance" },
        { $"{SectionInterface}::ShowEquippedAndHotbarItemsInSacrificeTab", "Crafting UI::ShowEquippedAndHotbarItemsInSacrificeTab" },
        { $"{SectionInterface}::AudioVolumeAdjustment", "Crafting UI::AudioVolumeAdjustment" },
        { $"{SectionInterface}::Tooltip Max Width", "Tooltips::Max Width" },
        { $"{SectionInterface}::Tooltip Max Height", "Tooltips::Max Height" },
        { $"{SectionInterface}::Trader Panel X Position", "General::Trader Panel X Position" },
        { $"{SectionInterface}::Trader Panel Y Position", "General::Trader Panel Y Position" },
        { $"{SectionInterface}::Temper Panel X Position", "General::Temper Panel X Position" },
        { $"{SectionInterface}::Temper Panel Y Position", "General::Temper Panel Y Position" },
        // Logging is diagnostics; it belongs with the rest of them.
        { $"{SectionDebug}::Logging Enabled", "Logging::Logging Enabled" },
        { $"{SectionDebug}::Log Level", "Logging::Log Level" }
    };

    /// <summary>Raw values of the config file as it was before this run bound anything, keyed "Section::Key".</summary>
    private static readonly Dictionary<string, string> PreviousConfigValues = new Dictionary<string, string>();

    /// <summary>
    /// The section names each "Section::Key" of <see cref="PreviousConfigValues"/> was found under, order prefix
    /// and all ("9 - Debug"): what a retired key has to be addressed by to take it out of the file.
    /// </summary>
    private static readonly Dictionary<string, List<string>> PreviousRawSections = new Dictionary<string, List<string>>();

    /// <summary>Everything bound this run, with the section name minus its order prefix.</summary>
    private static readonly List<(ConfigEntryBase Entry, string Location)> BoundEntries =
        new List<(ConfigEntryBase, string)>();

    /// <summary>
    /// One "re-read this baseconfig file into the live config" callback per file, in the order
    /// <see cref="InitializeConfig"/> registered them. See <see cref="ReloadBaseConfigsFromDisk"/>.
    /// </summary>
    private static readonly List<(string FileName, Func<bool> ReloadFromDisk)> BaseConfigReloaders =
        new List<(string, Func<bool>)>();

    public ELConfig(ConfigFile Config) {
        // ensure all the config values are created
        cfg = Config;
        // Bind first, save once. Every Set with SaveOnConfigSet on rewrites the whole file, and the
        // migration below sets a good fraction of the entries.
        cfg.SaveOnConfigSet = false;
        ReadPreviousConfigValues();
        CreateConfigValues();
        MigrateWelcomeWizard();
        MigrateSocketOverlayShortcut();
        ApplyPreviousConfigValues();
        // After the migration, so restoring a saved value does not fire a handler into a UI that Awake
        // has not built yet.
        RegisterSettingChangedHandlers();
        cfg.SaveOnConfigSet = true;
        cfg.Save();
        SetupConfigRPCs();
        FilePatching.LoadAllPatches();
        // Bring any config the player has not edited up to date first, so InitializeConfig reads the
        // refreshed contents rather than relying on a file watcher to fire part way through Awake.
        ConfigVersionManager.RefreshUnmodifiedConfigs();
        // InitializeConfig applies patches per file via SychronizeConfig -> LoadPatchedJSON, so a separate
        // ApplyAllPatches() pass here would be redundant (and cause extra file-watcher reloads).
        InitializeConfig();
        ConfigVersionManager.StampInitializedConfigs();
        FilePatching.LogAppliedPatchSummary();
    }

    public void SetupConfigRPCs() {
        LootTablesRPC = NetworkManager.Instance.AddRPC("epicloot_loottables_RPC",
            OnServerRecieveConfigs, OnClientRecieveLootConfigs);
        MagicEffectsRPC = NetworkManager.Instance.AddRPC("epicloot_magiceffect_RPC",
            OnServerRecieveConfigs, OnClientRecieveMagicConfigs);
        ItemConfigRPC = NetworkManager.Instance.AddRPC("epicloot_itemconfig_RPC",
            OnServerRecieveConfigs, OnClientRecieveItemInfoConfigs);
        EnchantingCostsRPC = NetworkManager.Instance.AddRPC("epicloot_enchantingcosts_RPC",
            OnServerRecieveConfigs, OnClientRecieveEnchantingCostsConfigs);
        ItemNamesRPC = NetworkManager.Instance.AddRPC("ItemNamesRPC",
            OnServerRecieveConfigs, OnClientRecieveItemNameConfigs);
        AdventureDataRPC = NetworkManager.Instance.AddRPC("AdventureDataRPC",
            OnServerRecieveConfigs, OnClientRecieveAdventureDataConfigs);
        LegendariesRPC = NetworkManager.Instance.AddRPC("LegendariesRPC",
            OnServerRecieveConfigs, OnClientRecieveLegendaryItemConfigs);
        AbilitiesRPC = NetworkManager.Instance.AddRPC("AbilitiesRPC",
            OnServerRecieveConfigs, OnClientRecieveAbilityConfigs);
        MaterialConversionRPC = NetworkManager.Instance.AddRPC("MaterialConversionRPC",
            OnServerRecieveConfigs, OnClientRecieveMaterialConversionConfigs);
        EnchantingUpgradesRPC = NetworkManager.Instance.AddRPC("EnchantingUpgradesRPC",
            OnServerRecieveConfigs, OnClientRecieveEnchantingUpgradesConfigs);
        AutoSorterConfigurationRPC = NetworkManager.Instance.AddRPC("AutoSorterConfigurationRPC",
            OnServerRecieveConfigs, OnClientRecieveAutoSorterConfigs);
        ShardStonesRPC = NetworkManager.Instance.AddRPC("epicloot_shardstones_RPC",
            OnServerRecieveConfigs, OnClientRecieveShardStonesConfigs);
        ShardStoneConversionsRPC = NetworkManager.Instance.AddRPC("epicloot_shardstoneconversions_RPC",
            OnServerRecieveConfigs, OnClientRecieveShardStoneConversionsConfigs);
        BiomeDataRPC = NetworkManager.Instance.AddRPC("epicloot_biomedata_RPC",
            OnServerRecieveConfigs, OnClientRecieveBiomeDataConfigs);
    }

    private static void CreateConfigValues() {
        // 1 - General
        UseGeneratedMagicItemNames = BindClient(SectionGeneral, "Use Generated Magic Item Names", true,
            "If true, magic items uses special, randomly generated names based on their rarity, type, and magic effects.");
        KeepInventoryOpenOverItems = BindClient(SectionGeneral, "Keep Inventory Open Over Items", true,
            "When true, pressing Use while the cursor is over an inventory item never closes the " +
            "inventory, so a shard slot shortcut that misses its target does nothing instead of " +
            "shutting everything. Use over an empty slot, or over an item another mod has turned into " +
            "a container, still behaves as normal.");
        AutoAddEquipment = BindServer(SectionGeneral, "Auto Add Equipment", true,
            "Automatically adds equipment types that can be enchanted to possible drops and gates them" +
            "behind their respective bosses. Disabling this also disables automatic removal of items not found.");
        AutoRemoveEquipmentNotFound = BindServer(SectionGeneral, "Auto Remove Equipment Not Found", true,
            "Automatically removes equipment types that is not found when loading the game.");
        OnlyAddEquipmentWithRecipes = BindServer(SectionGeneral, "Only Add Equipment With Recipes", true,
            "Equipment must be able to be created by a recipe in order to automatically get selected. " +
            "If this is disabled enemy weapons can be added to drops, they are not always valid.");
        AutoAddRemoveEquipmentFromVendor = BindServer(SectionGeneral, "Auto Add Remove Equipment From Vendor", true,
            "Automatically adds/removes equipment from the vendor when it is added/removed from the game. ");
        AutoAddRemoveEquipmentFromLootLists = BindServer(SectionGeneral, "Auto Add Remove Equipment From Loot Lists", true,
            "Automatically adds/removes equipment from the tier based loot lists, and validates other loot lists only contain valid items.");
        AutoAddCreaturesToLootTables = BindServer(SectionGeneral, "Auto Add Creatures To Loot Tables", true,
            "Gives creatures that have no loot table (most often ones other mods add) a tier loot table, chosen from " +
            "their biome and how tough they are by the CreatureSorter rules in itemsorter.json. The entries it " +
            "writes into loottables.json are marked \"Auto\": true and are re-sorted every time a world loads; " +
            "entries without that mark are never touched. Runs on the server (or in single player) only.");
        RequestLootRolls = BindServer(SectionGeneral, "Request Loot Rolls", false,
            "When Item Favorite Framework runs on the server with its loot rolls turned on, magic items a creature " +
            "drops go to a need/greed/pass roll among the players nearby instead of to whoever grabs them first.");
        LootRollMinimumRarity = BindServer(SectionGeneral, "Loot Roll Minimum Rarity", ItemRarity.Epic,
            "Only magic items of this rarity or better are rolled for.");
        LootRollBossesOnly = BindServer(SectionGeneral, "Loot Rolls Bosses Only", false,
            "Only roll for items bosses drop.");
        LootRollPlayerRange = BindServer(SectionGeneral, "Loot Roll Player Range", 50f,
            "Players this close to the drop, in metres, take part in its roll.",
            new AcceptableValueRange<float>(5f, 200f));
        LootRollMinimumPlayers = BindServer(SectionGeneral, "Loot Roll Minimum Players", 2,
            "A roll only happens when at least this many players are in range; otherwise the drop is free as usual.",
            new AcceptableValueRange<int>(1, 10));

        // 2 - Balance
        BalanceConfigurationType = BindServer(SectionBalance, "Balance Template", "Default",
            "Sets the type of balance configuration to use. " +
            "When initially set can change the value of other configurations in this file.\n" +
            "balanced: the recommended balancing, enchantments are powerful but stronger enemies can be a threat.\n" +
            "minimal: reduced enchantment power to be used with vanilla difficulty options.\n" +
            "legendary: legacy balancing that can make players godlike.",
            new AcceptableValueList<string>("balanced", "legendary", "minimal"));
        _gatedItemTypeModeConfig = BindServer(SectionBalance, "Item Drop Limits",
            GatedItemTypeMode.BossKillUnlocksCurrentBiomeItems,
            "Sets how the drop system limits what item types can drop. " +
            "Unlimited: no limits, exactly what's in the loot table will drop.\n" +
            "BossKillUnlocksCurrentBiomeItems: items will drop for the current biome if the that biome's boss has been killed " +
            "(Leather gear will drop once Eikthyr is killed).\n" +
            "BossKillUnlocksNextBiomeItems: items will only drop for the current biome if the previous biome's boss is killed " +
            "(Bronze gear will drop once Eikthyr is killed).\n" +
            "PlayerMustKnowRecipe: (local world only) the item can drop if the player can craft it.\n" +
            "PlayerMustHaveCraftedItem: (local world only) the item can drop if the player has already crafted it " +
            "or otherwise picked it up. If an item type cannot drop, it will downgrade to an item of the same type and " +
            "skill that the player has unlocked (i.e. swords will stay swords) according to iteminfo.json.");
        GatedFreebuildMode = BindServer(SectionBalance, "Gated Freebuild Mode", GatedPieceTypeMode.BossKillUnlocksCurrentBiomePieces,
            "Sets whether available pieces for the Freebuild effect are ungated or gated by boss kills.");
        GlobalDropRateModifier = BindServer(SectionBalance, "Global Drop Rate Modifier", 1.0f,
            "A global percentage that modifies how likely loot is to drop.\n" +
            "1 = Exactly what is in the loot tables will drop.\n" +
            "0 = Nothing will drop.\n" +
            "2 = The number of items in the drop table are twice as likely to drop " +
            "(note, this doesn't double the number of loot dropped, just doubles the relative chance for it to drop).\n" +
            "Min = 0, Max = 4", new AcceptableValueRange<float>(minValue: 0, maxValue: 4));
        StarLootScaling = BindServer(SectionBalance, "Star Loot Scaling", 1.0f,
            "How much a creature's stars improve its loot, on top of what loottables.json authors for each level.\n" +
            "Every star counts as this many stars when a loot table extrapolates past its authored levels " +
            "(StarScaling: drop chance, extra drops, item mix and rarity).\n" +
            "1 = as configured.\n" +
            "0 = stars never affect loot; every creature drops its level 1 loot.\n" +
            "2 = each star counts double.\n" +
            "Min = 0, Max = 3", new AcceptableValueRange<float>(minValue: 0, maxValue: 3));
        // The four ratios below are RELATIVE WEIGHTS, not independent chances. Every drop the loot
        // tables produce rolls exactly one of these four categories, weighted against each other, so
        // they need not sum to 1 -- only their proportions matter. Setting one to 0 removes that
        // category. A drop no other category is eligible for (and the case where every weight is 0)
        // falls back to a normal item, so loot never silently vanishes.
        ItemDropRatio = BindServer(SectionBalance, "Item Drop Ratio", 0.7f,
            "Relative weight for a loot drop being a normal (possibly enchanted) item.\n" +
            "Weighed against Shard Stone Drop Ratio, Items Unidentified Drop Ratio and Materials " +
            "Drop Ratio; the four need not add up to 1, only their proportions matter.\n" +
            "0 = never drop plain items, except where no other category is possible for that drop.\n" +
            "Min = 0, Max = 1", new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        ShardStoneDropRatio = BindServer(SectionBalance, "Shard Stone Drop Ratio", 0.2f,
            "Relative weight for a loot drop being a shard stone, chosen from the shard set assigned " +
            "to the biome at the drop point (the ShardStone_{Biome} item sets in loottables.json).\n" +
            "Weighed against Item Drop Ratio, Items Unidentified Drop Ratio and Materials Drop Ratio.\n" +
            "0 = no shard stones drop from normal loot. Elite creature and boss shard drops come " +
            "from the loot tables directly and are not affected by this setting.\n" +
            "Shard stones, runestones and enchanting materials a loot table names always drop as themselves.\n" +
            "Min = 0, Max = 1", new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        ItemsUnidentifiedDropRatio = BindServer(SectionBalance, "Items Unidentified Drop Ratio", 0.1f,
            "Relative weight for a loot drop being an unidentified item.\n" +
            "Weighed against Item Drop Ratio, Shard Stone Drop Ratio and Materials Drop Ratio.\n" +
            "Only equippable loot can become unidentified, so drops of materials and other " +
            "non-equipment ignore this weight.\n" +
            "0 = no unidentified items drop.\n" +
            "Min = 0, Max = 1", new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        MaterialsDropRatio = BindServer(SectionBalance, "Materials Drop Ratio", 0.1f,
            "Relative weight for a loot drop being magic crafting materials instead of the item " +
            "itself, as though that item had been sacrificed.\n" +
            "Weighed against Item Drop Ratio, Shard Stone Drop Ratio and Items Unidentified Drop Ratio.\n" +
            "Shard stones, runestones and enchanting materials a loot table names always drop as themselves.\n" +
            "0 = no materials drop.\n" +
            "Min = 0, Max = 1", new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        SetItemDropChance = BindServer(SectionBalance, "Set Item Drop Chance", 0.15f,
            "The percent chance that an item of a rarity with item sets enabled (Legendary, Mythic and " +
            "Ancient by default, see each set's Rarities in legendaries.json) is dropped, enchanted " +
            "or identified as a set item. If no set piece fits the item it rolls a regular unique instead.\n" +
            "Min = 0, Max = 1",
            new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        HealthCriticalEffectsEnabled = BindServer(SectionBalance, "Health Critical Enchantments", true,
            "When false, the enchantments that only act at critical health stop rolling: every " +
            "'(Health Critical)' effect, Instant Mead, Automatic Mead Consumption and Health Critical Threshold " +
            "no longer come up from loot, identifying, enchanting or augmenting.\n" +
            "Items that already carry one keep it and it keeps working, and a runestone holding one can still " +
            "be etched. Default: True.");
        TransferMagicItemToCrafts = BindServer(SectionBalance, "Transfer Enchants to Crafted Items", true,
            "When enchanted items are used as ingredients in recipes, transfer every enchantment from the " +
            "consumed items that is valid on the newly crafted item, along with the highest socket count. " +
            "Default: True.");
        DeferChestLootRoll = BindServer(SectionBalance, "Defer Chest Loot Roll", true,
            "When true, a loot chest's EpicLoot contents are rolled the first time a player reads them " +
            "(hovers it, opens it, quick-loots it, or breaks it) rather than when the chest spawns.\n" +
            "This matters because a dungeon's interior is created as soon as a player enters the zone " +
            "above it, so with this off a crypt passed by early in a playthrough keeps low-tier fallback " +
            "loot forever. Deferring means the roll sees the boss keys and Item Drop Limits in effect " +
            "when the chest is actually reached.\n" +
            "Only applies to chests generated from now on; chests that already rolled keep their contents.");
        _bossTrophyDropMode = BindServer(SectionBalance, "Boss Trophy Drop Mode", BossDropMode.OnePerPlayerNearBoss,
            "Bosses drop one extra trophy for each player after the first, on top of their normal drop. " +
            "OnePerPlayerOnServer counts every player on the server; OnePerPlayerNearBoss counts only players " +
            "within 'Boss Trophy Drop Player Range' of the boss. Default leaves the drop unchanged.");
        _bossTrophyDropPlayerRange = BindServer(SectionBalance, "Boss Trophy Drop Player Range", 100.0f,
            "Sets the range that bosses check when dropping multiple trophies using the OnePerPlayerNearBoss drop mode.");
        _bossCryptKeyDropMode = BindServer(SectionBalance, "Crypt Key Drop Mode", BossDropMode.OnePerPlayerNearBoss,
            "Bosses drop one extra crypt key for each player after the first, on top of their normal drop. " +
            "OnePerPlayerOnServer counts every player on the server; OnePerPlayerNearBoss counts only players " +
            "within 'Crypt Key Drop Player Range' of the boss. Default keeps vanilla's one key per player on the server.");
        _bossCryptKeyDropPlayerRange = BindServer(SectionBalance, "Crypt Key Drop Player Range", 100.0f,
            "Sets the range that bosses check when dropping multiple crypt keys using the OnePerPlayerNearBoss drop mode.");
        _bossWishboneDropMode = BindServer(SectionBalance, "Wishbone Drop Mode", BossDropMode.OnePerPlayerNearBoss,
            "Bosses drop one extra wishbone for each player after the first, on top of their normal drop. " +
            "OnePerPlayerOnServer counts every player on the server; OnePerPlayerNearBoss counts only players " +
            "within 'Wishbone Drop Player Range' of the boss. Default keeps vanilla's one wishbone per player on the server.");
        _bossWishboneDropPlayerRange = BindServer(SectionBalance, "Wishbone Drop Player Range", 100.0f,
            "Sets the range that bosses check when dropping multiple wishbones using the OnePerPlayerNearBoss drop mode.");
        BindBossExtraDrop("Moder", "Dragon Tears", 10, out ModerDropMode, out ModerDropPlayerRange);
        BindBossExtraDrop("Yagluth", "Torn Spirits", 3, out YagluthDropMode, out YagluthDropPlayerRange);
        BindBossExtraDrop("Queen", "Majestic Carapaces", 5, out QueenDropMode, out QueenDropPlayerRange);
        BindBossExtraDrop("Fader", "Fader Relics", 5, out FaderDropMode, out FaderDropPlayerRange);
        BindBossExtraDrop("Frozen King", "Sacrificial Blood", 1, out FrozenKingDropMode, out FrozenKingDropPlayerRange);
        EliteRunestoneDrops = BindServer(SectionBalance, "Elite Runestone Drops", false,
            "When true, elite creatures (those on the Tier3EliteMob to Tier9EliteMob loot tables, such as " +
            "trolls, abominations, fenrings and fuling brutes) also have a chance to drop a blank runestone of " +
            "their biome's rarity, on top of their normal loot: Magic in the Black Forest, Rare in the Swamp, " +
            "Epic in the Mountains, Legendary in the Plains and Mistlands, Mythic in the Ashlands, and Mythic " +
            "or Ancient in the Deep North. Default: False.");
        EliteRunestoneDropChance = BindServer(SectionBalance, "Elite Runestone Drop Chance", 0.1f,
            "With Elite Runestone Drops on, the chance that an elite creature drops a runestone. " +
            "It is the same at every star level, and Global Drop Rate Modifier applies to it as to all loot.\n" +
            "Min = 0, Max = 1",
            new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        // 3 - Sockets
        AllowRuneSocketing = BindServer(SectionSockets, "Allow Rune Socketing", true,
            "When true, etched runestones can be slotted into shard slots.\n" +
            "When false, no runestone can be slotted. Runestones already in a slot stay there but grant " +
            "nothing, and can always be taken out (whatever Rune Removal Mode says), but not put back. " +
            "Turning this back on restores their effects.\n" +
            "Default: true.");
        AllowDuplicateSocketedEffects =BindServer(SectionSockets, "Allow Duplicate Socketed Effects", false,
            "When false, an effect that is already socketed on an item cannot be socketed again.");
        AllowShardstoneDuplicateItemEffect = BindServer(SectionSockets, "Allow Shardstone On Matching Item Effect", false,
            "When true, a shardstone may be socketed even when the item already has that same effect from being enchanted/rolled.");
        AllowRunestoneDuplicateItemEffect = BindServer(SectionSockets, "Allow Runestone On Matching Item Effect", false,
            "When true, a runestone may be socketed even when the item already has that same effect from being enchanted/rolled.");
        ShardSocketRemovalMode = BindServer(SectionSockets, "Shard Removal Mode", ShardSocketMode.BreakValueless,
            "Controls whether a shardstone can be taken back out of a socket once it has been placed. " +
            "Shards can always be inserted; this only affects removal.\n" +
            "Free = shards can be freely removed and moved to another item.\n" +
            "BreakValueless = a shard granting an effect that has no rarity-scaled value (Warmth, for " +
            "example) must be broken to be removed, destroying it. A shard granting a rarity-scaled " +
            "value, or granting nothing at all on that item, can still be freely removed.\n" +
            "BreakAll = every shard must be broken to be removed, destroying it.\n" +
            "Permanent = every shard is permanent; it can be neither removed nor broken.\n" +
            "Default: BreakValueless.");
        RuneSocketRemovalMode = BindServer(SectionSockets, "Rune Removal Mode", RuneSocketMode.Free,
            "Controls whether a runestone can be taken back out of a socket once it has been placed. " +
            "This only affects removal, and only while Allow Rune Socketing is on; while it is off, " +
            "every slotted rune can be freely removed.\n" +
            "Free = runes can be freely removed and moved to another item.\n" +
            "Break = a socketed rune must be broken to be removed, destroying it.\n" +
            "Permanent = a socketed rune is permanent; it can be neither removed nor broken, and no " +
            "other rune can be swapped into its socket.\n" +
            "Default: Free.");
        ShardStackingMode = BindServer(SectionSockets, "Shard Stack Mode", ShardStackMode.Diminishing,
            "Controls whether more than one shardstone of the same color may sit on a single item. " +
            "A shard's effect comes from its color and the item's type, so two shards of one color " +
            "always grant the same effect.\n" +
            "Blocked = a color already socketed on an item cannot be socketed into it again.\n" +
            "Diminishing = allowed, with each further shard of that color contributing a decayed " +
            "fraction of its value (see Shard Stack Decay Factor).\n" +
            "Full = allowed at full value; every shard of the color contributes its whole effect.\n" +
            "Shards whose effect has no rarity-scaled value (Warmth, for example) can never be " +
            "stacked -- halving a yes/no effect would leave a dead socket. Boss and other exclusive " +
            "shards keep their own one-per-item / one-per-worn-set rule regardless of this setting.\n" +
            "Default: Diminishing.");
        ShardStackDecayFactor = BindServer(SectionSockets, "Shard Stack Decay Factor", 0.5f,
            "Under Shard Stack Mode = Diminishing, the share of its value each further shard of the " +
            "same color contributes. The shards of a color are ranked strongest first, and the " +
            "shard at rank R is worth (factor ^ R) of its normal value -- so 0.5 gives full, half, " +
            "a quarter, an eighth, and so on down the stack.\n" +
            "0 = additional shards of a color contribute nothing.\n" +
            "1 = no decay (equivalent to Shard Stack Mode = Full).\n" +
            "Min = 0, Max = 1", new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        RuneDiminishingReturns = BindServer(SectionSockets, "Rune Diminishing Returns", true,
            "When true, a slotted runestone whose effect the item already has -- rolled on the item, from a " +
            "shardstone, or from another runestone -- contributes only a decayed fraction of its value " +
            "(see Rune Stack Decay Factor). Such a rune can only be slotted while Allow Duplicate Socketed " +
            "Effects or Allow Runestone On Matching Item Effect allows it. The runestone itself is not " +
            "changed: taken back out, it has its full value again.\n" +
            "Runes whose effect has no rarity-scaled value (Warmth, for example) are never reduced.\n" +
            "Default: true.");
        RuneStackDecayFactor = BindServer(SectionSockets, "Rune Stack Decay Factor", 0.5f,
            "Under Rune Diminishing Returns, the share of its value each repeated runestone contributes. " +
            "The item's own rolled effect and any shardstone granting the same effect count first, then " +
            "the item's runestones of that effect, strongest first; the rune at rank R is worth " +
            "(factor ^ R) of its normal value. With 0.5, a rune repeating an effect the item already rolled " +
            "is worth half, a second such rune a quarter, and so on.\n" +
            "0 = repeated runes contribute nothing.\n" +
            "1 = no decay (equivalent to Rune Diminishing Returns off).\n" +
            "Min = 0, Max = 1", new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        RuneExtractItemMode = BindServer(SectionSockets, "Rune Extract Mode", RuneExtractMode.ReduceEnchants,
            "Controls what happens to the source item when a rune is extracted from it.\n" +
            "KeepItem = the item is returned untouched.\n" +
            "ReduceEnchants = the extracted enchantment is removed from the item (item kept).\n" +
            "ReduceEnchantsAndRarity = the extracted enchantment is removed, the item's rarity is reduced " +
            "one tier, and remaining effect values are clamped to the new rarity's max.\n" +
            "DestroyItem = the item is consumed.\n" +
            "If the extracted enchantment is the item's only enchantment, the item reverts to a normal item.\n" +
            "Default: ReduceEnchants.");
        RuneSetExtractItemMode = BindServer(SectionSockets, "Set Rune Extract Mode", RuneSetExtractMode.StripSet,
            "Controls what happens to the source item when its set is extracted into a set rune " +
            "(Rune page, Set Extract).\n" +
            "StripSet = the item keeps its rarity, enchantments and shard slots but is no longer a set " +
            "piece, and gets a generated name.\n" +
            "DestroyItem = the item is consumed; socketed stones that are not permanent are returned.\n" +
            "Default: StripSet.");
        AllowGiftOnItemsWithSlots = BindServer(SectionSockets, "Allow Brokkr Gift On Items With Slots", true,
            "When true, Brokkr's Gift can extend an item that already has shard slots, up to the most " +
            "its rarity allows. When false, it only works on an item with no shard slots at all -- an " +
            "item with slots is refused even when every slot is still empty.\n" +
            "Default: true.");
        LegendaryGiftSlotsAdded = BindServer(SectionSockets, "Legendary Brokkr Gift Slots Added", 1,
            "How many shard slots a Legendary Brokkr's Gift adds. The item's rarity cap still applies on " +
            "top of this: if fewer slots are free than this grants, the item gains only what fits and the " +
            "gift is still consumed.\n" +
            $"Min = 1, Max = {LootRoller.MaxSocketCount}", new AcceptableValueRange<int>(1, LootRoller.MaxSocketCount));
        MythicGiftSlotsAdded = BindServer(SectionSockets, "Mythic Brokkr Gift Slots Added", 2,
            "How many shard slots a Mythic Brokkr's Gift adds. The item's rarity cap still applies on " +
            "top of this: if fewer slots are free than this grants, the item gains only what fits and the " +
            "gift is still consumed.\n" +
            $"Min = 1, Max = {LootRoller.MaxSocketCount}", new AcceptableValueRange<int>(1, LootRoller.MaxSocketCount));
        AncientGiftSlotsAdded = BindServer(SectionSockets, "Ancient Brokkr Gift Slots Added", 3,
            "How many shard slots an Ancient Brokkr's Gift adds. The item's rarity cap still applies on " +
            "top of this: if fewer slots are free than this grants, the item gains only what fits and the " +
            "gift is still consumed.\n" +
            $"Min = 1, Max = {LootRoller.MaxSocketCount}", new AcceptableValueRange<int>(1, LootRoller.MaxSocketCount));
        LegendaryGiftSuccessChance = BindServer(SectionSockets, "Legendary Brokkr Gift Success Chance", 100f,
            "Percent chance that a Legendary Brokkr's Gift adds its slots. On a failed roll the gift is " +
            "still consumed and nothing is added.\n" +
            "Min = 0, Max = 100", new AcceptableValueRange<float>(0f, 100f));
        MythicGiftSuccessChance = BindServer(SectionSockets, "Mythic Brokkr Gift Success Chance", 100f,
            "Percent chance that a Mythic Brokkr's Gift adds its slots. On a failed roll the gift is " +
            "still consumed and nothing is added.\n" +
            "Min = 0, Max = 100", new AcceptableValueRange<float>(0f, 100f));
        AncientGiftSuccessChance = BindServer(SectionSockets, "Ancient Brokkr Gift Success Chance", 100f,
            "Percent chance that an Ancient Brokkr's Gift adds its slots. On a failed roll the gift is " +
            "still consumed and nothing is added.\n" +
            "Min = 0, Max = 100", new AcceptableValueRange<float>(0f, 100f));
        SocketOverlayShortcut = BindClient(SectionSockets, SocketOverlayShortcutKey,
            new KeyboardShortcut(KeyCode.E, KeyCode.LeftControl),
            "The keys that open the shard slots of the inventory item under the cursor, written as the key " +
            "that is pressed, then the keys held with it: 'E + LeftControl'. When that key is the one bound to " +
            "Use, a press another mod has already taken is left to it. A shortcut that is only the Use key never " +
            "opens the shard slots of an item another mod has turned into a container (a backpack, a quiver), " +
            "because plain Use opens that container.");
        SocketOverlayGamepadButton = BindClient(SectionSockets, "Socket Overlay Gamepad Button",
            SocketGamepadButton.ButtonX,
            "The gamepad button that opens the shard slots of the selected inventory item, pressed with the " +
            "Socket Overlay Gamepad Modifier. Over an item that has shard slots this takes the press from what " +
            "the game does with it: X uses an item, A picks it up, Y closes the inventory, and with a trigger " +
            "held they move, split or drop items. None turns the gamepad gesture off.");
        SocketOverlayGamepadModifier = BindClient(SectionSockets, "Socket Overlay Gamepad Modifier",
            SocketGamepadModifier.RTrigger,
            "The gamepad button held with the Socket Overlay Gamepad Button. None means the button alone, " +
            "with neither trigger held.");
        ShardSlotPanelOffset = BindClient(SectionSockets, "Shard Slot Panel Offset", new Vector2(10f, -40f),
            "Where the shard slot panel sits: the distance from the bottom-right corner of the inventory panel " +
            "to the panel's own top-left corner, in interface units, x to the right and y downwards (negative). " +
            "The default puts it beside the chest panel, below where Equipment and Quick Slots places its " +
            "equipment panel. Takes effect while the panel is open.");

        // 4 - Enchanting Table
        EnchantingTableUpgradesActive = BindServer(SectionEnchanting, "Upgrades Active", true,
            "Toggles Enchanting Table Upgrade Capabilities. If false, enchanting table features will be unlocked set to Level 1");
        EnchantingTableActivatedTabs = BindServer(SectionEnchanting, "Table Features Active",
            EnchantingTabs.Sacrifice | EnchantingTabs.Augment | EnchantingTabs.Enchant | EnchantingTabs.Disenchant |
            EnchantingTabs.Upgrade | EnchantingTabs.ConvertMaterials | EnchantingTabs.Rune | EnchantingTabs.Storage,
            "Toggles Enchanting Table Feature on and off completely.");
        AutoDepositMaterials = BindClient(SectionEnchanting, "Auto Deposit Materials", false,
            "Opening an enchanting table with its Storage feature unlocked first moves every enchanting material, " +
            "blank runestone, shardstone, chisel and bounty token from your inventory into the table's storage.");
        TemperBaseChance = BindServer(SectionEnchanting, "Temper Base Chance", 0.5f,
            "Base chance to temper item when below max value. When effect value is at max, the chance is at the base. If value is above max value, the chance is reduces by the decrement chance. Default value: 0.5");
        TemperDecrement = BindServer(SectionEnchanting, "Temper Decrement Amount", 0.15f,
            "Decrement amount when effect value is above max value. Does not apply if value is below max value. Decrement amount is multiplied by the increment amount the value is above max value. Default value: 0.15");
        TemperFailDowngrades = BindServer(SectionEnchanting, "Temper Fail Downgrades Effect", false,
            "When tempering fails, the enchantment being tempered drops by one increment, never below the " +
            "lowest roll for the item's rarity. No other enchantment on the item is touched. If False, a " +
            "failed temper only costs the payment. Default value: False");
        TemperDestroysItem = BindServer(SectionEnchanting, "Temper Fail Destroys Item", false,
            "When tempering fails, the item may be destroyed (see Temper Destroy Chance). If False, a failed temper never destroys the item. Default value: False");
        TemperChanceToDestroy = BindServer(SectionEnchanting, "Temper Destroy Chance", 0.5f,
            "If Fail Destroys Item is enabled, Destroy Chance rolls if item should be destroyed. Default value: 0.5");
        SpendOnlyStorageMods = BindServer(SectionEnchanting, "Spend-Only Storage Mods", "Azumatt.AzuCraftyBoxes",
            "Comma-separated plugin ids of mods that let the enchanting table use items stored outside your " +
            "inventory, such as nearby chests. The table still spends those items as materials and runes, and " +
            "can sacrifice or identify them, but it will not enchant, augment, etch, disenchant, reduce or strip " +
            "one in place: the change would only reach the mod's copy in memory, and the chest reloading would " +
            "undo it after the materials were spent. Take the item out of the chest to work on it.\n" +
            "Remove a mod from the list once it saves these changes through Epic Loot's API (DvergerAutomation " +
            "does, so it is not listed). Default: Azumatt.AzuCraftyBoxes");

        // 5 - Adventure
        _adventureModeEnabled = BindServer(SectionAdventure, "Adventure Mode Enabled", true,
            "Set to true to enable all the adventure mode features: secret stash, gambling, treasure maps, and bounties. " +
            "Set to false to disable. This will not actually remove active treasure maps or bounties from your save.");
        _andvaranautRange = BindServer(SectionAdventure, "Andvaranaut Range", 20,
            "Sets the range that Andvaranaut will activate to locate a treasure chest.");
        BossBountyMode = BindServer(SectionAdventure, "Gated Bounty Mode", GatedBountyMode.Unlimited,
            "Sets whether available bounties are ungated or gated by boss kills.");
        EnableLimitedBountiesInProgress = BindServer(SectionAdventure, "Enable Bounty Limit", false,
            "Toggles limiting bounties. Players unable to purchase if enabled and maximum bounty in-progress count is met");
        MaxInProgressBounties = BindServer(SectionAdventure, "Max Bounties Per Player", 5,
            "Max amount of in-progress bounties allowed per player.");
        RemovePurchasedGambles = BindServer(SectionAdventure, "Remove Purchased Gambles", false,
            "When true, buying a gamble item from Haldor takes that offer off the Gamble list for the rest of " +
            "the gamble refresh interval, so each offer can be taken once; the offer comes back next interval.\n" +
            "When false the offer stays on the list and can be bought over and over. Default: false.");

        // 6 - Interface
        UseScrollingCraftDescription = BindClient(SectionInterface, "Use Scrolling Craft Description", true,
            "Changes the item description in the crafting panel to scroll instead of scale when it gets too " +
            "long for the space.");
        ShowRarityInRecipeList = BindClient(SectionInterface, "Show Rarity In Recipe List", true,
            "Shows the magic item rarity background behind item icons in the crafting panel's recipe " +
            "list and detail panel, the same way it is shown in your inventory.");
        ShowEnchantSelectionChance = BindServer(SectionInterface, "Show Enchant Selection Chance", false,
            "When true, the Enchant and Augment panels show the weighted chance that each available effect " +
            "is selected on a single roll, displayed right after the bullet for each effect.");
        EffectRarityFlareMode = BindClient(SectionInterface, "Effect Rarity Flare", EffectRarityFlare.Animated,
            "Marks magic effects that are rare to roll, derived from each effect's SelectionWeight relative " +
            "to the median weight of every loaded effect -- no effect is singled out by name.\n" +
            "Off = no marker.\n" +
            "Static = a coloured star rating after the effect, one to three stars by how rare it is.\n" +
            "Animated = as Static, and in the item tooltip and the enchanting table those lines also drift and shimmer.");
        ShowEquippedAndHotbarItemsInSacrificeTab = BindClient(SectionInterface,
            "ShowEquippedAndHotbarItemsInSacrificeTab", false,
            "If set to false, hides the items that are equipped or on your hotbar in the Sacrifice items list.");
        ShowStorageCounts = BindClient(SectionInterface, "Show Storage Counts", true,
            "Shows, after each material cost at the enchanting table, how many of it are available from " +
            "storage outside your inventory, such as a craft-from-containers mod (as a green +N). Hover " +
            "the material for the full line.");
        RespectItemFavorites = BindClient(SectionInterface, "Respect Item Favorites", true,
            "When Item Favorite Framework is installed, items you favorited there, and anything in a favorited " +
            "inventory slot, are left out of every list that destroys or consumes the item: Sacrifice, Identify, " +
            "Disenchant, Set Extract, Rune Extract (unless the extract mode keeps the item) and the runes offered " +
            "for etching.");
        ShowFavoriteStars = BindClient(SectionInterface, "Show Favorite Stars", true,
            "When Item Favorite Framework is installed, items you favorited there show a star on their icon in the " +
            "enchanting table and temper lists.");
        UIAudioVolumeAdjustment = BindClient(SectionInterface, "AudioVolumeAdjustment", 1.0f,
            "Multiplies the crafting UI sound volume by this percentage [0.0-1.0].\n" +
            "1 = full UI sounds\n" +
            "0 = no UI sounds",
            new AcceptableValueRange<float>(0, 1));
        TooltipMaxWidth = BindClient(SectionInterface, "Tooltip Max Width", 400,
            "Maximum width of the item tooltip box, in pixels. The box narrows to fit shorter text, and longer " +
            "lines wrap at this width.", new AcceptableValueRange<int>(150, 1200));
        TooltipMaxHeight = BindClient(SectionInterface, "Tooltip Max Height", 850,
            "Maximum height of the item tooltip box, in pixels. The box shrinks to fit its content, and " +
            "content taller than this scrolls. Never taller than the screen.",
            new AcceptableValueRange<int>(350, 4000));
        TraderPanelPositionX = BindClient(SectionInterface, "Trader Panel X Position", -200f,
            "The horizontal on-screen position (RectTransform anchoredPosition X, anchored to the " +
            "top-right of the trader window) of the EpicLoot adventure trader panel. Dragging the " +
            "panel in-game (by its corner handle, or anywhere while holding the Trader Panel Drag " +
            "Key) updates this automatically. Default: -200. More negative moves it left, toward 0 " +
            "(and positive) moves it right.");
        TraderPanelPositionY = BindClient(SectionInterface, "Trader Panel Y Position", -155f,
            "The vertical on-screen position (RectTransform anchoredPosition Y, anchored to the " +
            "top-right of the trader window) of the EpicLoot adventure trader panel. Dragging the " +
            "panel in-game (by its corner handle, or anywhere while holding the Trader Panel Drag " +
            "Key) updates this automatically. Default: -155.");
        TemperPanelPositionX = BindClient(SectionInterface, "Temper Panel X Position", -200f,
            "The horizontal on-screen position (RectTransform anchoredPosition X, anchored to the " +
            "top-right of the trader window) of the EpicLoot tempering panel. Dragging the panel " +
            "in-game (by its corner handle, or anywhere while holding the Trader Panel Drag Key) " +
            "updates this automatically. Default: -200.");
        TemperPanelPositionY = BindClient(SectionInterface, "Temper Panel Y Position", -155f,
            "The vertical on-screen position (RectTransform anchoredPosition Y, anchored to the " +
            "top-right of the trader window) of the EpicLoot tempering panel. Dragging the panel " +
            "in-game (by its corner handle, or anywhere while holding the Trader Panel Drag Key) " +
            "updates this automatically. Default: -155.");
        TraderPanelDragKey = BindClient(SectionInterface, "Trader Panel Drag Key", KeyCode.LeftAlt,
            "Hold this key to drag the adventure trader and tempering panels from anywhere on them, their " +
            "lists and buttons included. Without it the panels stay put, so clicking them never moves " +
            "them. The arrow handle in each panel's top-right corner drags it without the key. Set to None " +
            "to move the panels by the handle only.");
        ShowQuickConfigButton = BindClient(SectionInterface, "Show Quick Configure Button", true,
            "Lists Epic Loot in the shared Mod Config button on the main menu and the pause menu, which " +
            "opens the Quick Configure panel: a paged editor over the settings most worlds change, " +
            "including a few values from the JSON configs. Turn off to hide the entry; the panel can " +
            "still be reached by other mods' launchers if they offer it. Applies without a restart.");
        ShowAdvancedQuickConfigPages = BindClient(SectionInterface, "Show Advanced Quick Configure Pages", false,
            "Whether the Quick Configure panel shows every page or only the Balance page (after the welcome " +
            "page in first-time setup). The Show Advanced Configs and Hide Advanced Configs buttons on the " +
            "Balance page set this, and the panel remembers it the next time it opens.");

        // 7 - Item Colors
        _magicRarityColor = BindClient(SectionItemColors, "Magic Rarity Color", "Blue",
            "The color of Magic rarity items, the lowest magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _magicMaterialIconColor = BindClient(SectionItemColors, "Magic Crafting Material Icon Index", 5,
            "Indicates the color of the icon used for magic crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _rareRarityColor = BindClient(SectionItemColors, "Rare Rarity Color", "Yellow",
            "The color of Rare rarity items, the second magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _rareMaterialIconColor = BindClient(SectionItemColors, "Rare Crafting Material Icon Index", 2,
            "Indicates the color of the icon used for rare crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _epicRarityColor = BindClient(SectionItemColors, "Epic Rarity Color", "Purple",
            "The color of Epic rarity items, the third magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _epicMaterialIconColor = BindClient(SectionItemColors, "Epic Crafting Material Icon Index", 7,
            "Indicates the color of the icon used for epic crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _legendaryRarityColor = BindClient(SectionItemColors, "Legendary Rarity Color", "Teal",
            "The color of Legendary rarity items, the fourth magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _legendaryMaterialIconColor = BindClient(SectionItemColors, "Legendary Crafting Material Icon Index", 4,
            "Indicates the color of the icon used for legendary crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _mythicRarityColor = BindClient(SectionItemColors, "Mythic Rarity Color", "Orange",
            "The color of Mythic rarity items, the fifth magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _mythicMaterialIconColor = BindClient(SectionItemColors, "Mythic Crafting Material Icon Index", 1,
            "Indicates the color of the icon used for mythic crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _ancientRarityColor = BindClient(SectionItemColors, "Ancient Rarity Color", "Red",
            "The color of Ancient rarity items, the highest magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _ancientMaterialIconColor = BindClient(SectionItemColors, "Ancient Crafting Material Icon Index", 0,
            "Indicates the color of the icon used for ancient crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _setItemColor = BindClient(SectionItemColors, "Set Item Color", "#26ffff",
            "The color of set item text and the set item icon. Use a hex color, default is cyan");

        // 8 - Abilities
        AbilityKeyCodes[0] = BindClient(SectionAbilities, "Ability Hotkey 1", "g", "Hotkey for Ability Slot 1.");
        AbilityKeyCodes[1] = BindClient(SectionAbilities, "Ability Hotkey 2", "h", "Hotkey for Ability Slot 2.");
        AbilityKeyCodes[2] = BindClient(SectionAbilities, "Ability Hotkey 3", "j", "Hotkey for Ability Slot 3.");
        AbilityGamepadUseAltKey = BindClient(SectionAbilities, "Ability Gamepad Buttons Use Modifier", true,
            "Ability gamepad buttons, and the set hotkey gamepad buttons below, only fire while the game's gamepad " +
            "modifier is held: LT on the Default and Alternative 2 layouts (where it is also block), LB on " +
            "Alternative 1. The same modifier the game uses for dodge and camera zoom. These buttons do nothing in " +
            "build mode. Off makes each button fire on its own, which takes it away from its vanilla action while " +
            "its ability can be used.");
        // One default chord per slot, so an activated ability in any slot has a button (the stock sets fill slot 1,
        // but API abilities from other mods can take the others), with what the game does with that button while the
        // modifier is held.
        var abilityGamepadDefaults = new (KeyCode Button, string Note)[AbilityController.AbilitySlotCount]
        {
            (KeyCode.JoystickButton9, "JoystickButton9 is the right stick click (Start on macOS). With the modifier " +
                "held, the game uses it only for crouch on the Alternative layouts"),
            (KeyCode.JoystickButton8, "JoystickButton8 is the left stick click. With the modifier held, the game uses " +
                "it to toggle auto pickup, and for crouch (Default layout) or run (Alternative layouts)"),
            (KeyCode.JoystickButton2, "JoystickButton2 is the X button (Square). With the modifier held, the game " +
                "uses it only for interact on the Alternative layouts"),
        };
        for (int i = 0; i < AbilityController.AbilitySlotCount; i++)
        {
            AbilityGamepadButtons[i] = BindClient(SectionAbilities, $"Ability Gamepad Button {i + 1}",
                abilityGamepadDefaults[i].Button,
                $"Gamepad button (JoystickButton0-19) for Ability Slot {i + 1}, pressed with the modifier above. " +
                $"{abilityGamepadDefaults[i].Note}, which the ability takes over while the slot holds an activated " +
                "ability. None unbinds it.");
        }
        AbilityBarAnchor = BindClient(SectionAbilities, "Ability Bar Anchor", TextAnchor.LowerLeft,
            "The point on the HUD to anchor the ability bar. Changing this also changes the pivot of the ability bar to that corner. " +
            "For reference: the ability bar size is 208 by 64.");
        AbilityBarPosition = BindClient(SectionAbilities, "Ability Bar Position", new Vector2(150, 170),
            "The position offset from the Ability Bar Anchor at which to place the ability bar.");
        AbilityBarLayoutAlignment = BindClient(SectionAbilities, "Ability Bar Layout Alignment", TextAnchor.LowerLeft,
            "The Ability Bar is a Horizontal Layout Group. This value indicates how the elements inside are aligned. " +
            "Choices with 'Center' in them will keep the items centered on the bar, even if there are fewer than the maximum allowed. " +
            "'Left' will be left aligned, and similar for 'Right'.");
        AbilityBarIconSpacing = BindClient(SectionAbilities, "Ability Bar Icon Spacing", 8.0f,
            "The number of units between the icons on the ability bar.");
        OverwhelmingLaunchKey = BindClient(SectionAbilities, "Overwhelming Launch Hotkey", KeyCode.T,
            "Key that hurls your weapon while the Overwhelming Launch set bonus is active and a melee weapon " +
            "is held. While that is true the key does only this: whatever vanilla action shares it (T is the " +
            "emote wheel) is suppressed. None disables the keyboard binding.");
        OverwhelmingLaunchGamepadButton = BindClient(SectionAbilities, "Overwhelming Launch Gamepad Button", KeyCode.JoystickButton9,
            "Gamepad button (JoystickButton0-19) for Overwhelming Launch, pressed with the modifier of Ability Gamepad " +
            "Buttons Use Modifier. JoystickButton9 is the right stick click, the same chord as Ability Slot 1 and " +
            "the other set hotkeys: no one can wear two sets that need a press. While a launchable weapon is held, " +
            "the button's vanilla action is suppressed. None unbinds it.");
        SpiritAnimalKey = BindClient(SectionAbilities, "Spirit Animal Hotkey", KeyCode.Y,
            "Key that binds the tamed creature under the crosshair as your spirit animal while the Spirit Animal " +
            "set bonus is active. A confirmation is shown first. None disables the keyboard binding.");
        SpiritAnimalGamepadButton = BindClient(SectionAbilities, "Spirit Animal Gamepad Button", KeyCode.JoystickButton9,
            "Gamepad button (JoystickButton0-19) for binding a spirit animal, pressed with the modifier of Ability " +
            "Gamepad Buttons Use Modifier. JoystickButton9 is the right stick click, the same chord as Ability Slot 1 " +
            "and the other set hotkeys: no one can wear two sets that need a press. While a tamed creature that can be " +
            "bound is under the crosshair, the button's vanilla action is suppressed. None unbinds it.");
        EarthshakerKey = BindClient(SectionAbilities, "Earthshaker Hotkey", KeyCode.T,
            "Key that dives and slams the ground while the Earthshaker set bonus is active and you are in the air. " +
            "Shares T with Overwhelming Launch, since no one can wear both full sets. While airborne with the set, " +
            "the key does only this: whatever vanilla action shares it (T is the emote wheel) is suppressed. " +
            "None disables the keyboard binding.");
        EarthshakerGamepadButton = BindClient(SectionAbilities, "Earthshaker Gamepad Button", KeyCode.JoystickButton9,
            "Gamepad button (JoystickButton0-19) for Earthshaker, pressed with the modifier of Ability Gamepad " +
            "Buttons Use Modifier. JoystickButton9 is the right stick click, the same chord as Ability Slot 1 and " +
            "the other set hotkeys: no one can wear two sets that need a press. While you are airborne with the set, " +
            "the button's vanilla action is suppressed. None unbinds it.");
        ShowOwnFalconWings = BindClient(SectionAbilities, "Show Own Falcon Wings", true,
            "Show the dark wings of the Freyja's Falcon set on your own character. Turn off if they block your view; " +
            "other players still see them.");
        EitrInfusionKey = BindClient(SectionAbilities, "Eitr Infusion Hotkey", KeyCode.T,
            "Key that pours your eitr into your weapon while the Eitr Infusion set bonus is active. Shares T with " +
            "Overwhelming Launch and Earthshaker, since no one can wear two of those full sets. While the full set is " +
            "worn, the key does only this: whatever vanilla action shares it (T is the emote wheel) is suppressed. " +
            "None disables the keyboard binding.");
        EitrInfusionGamepadButton = BindClient(SectionAbilities, "Eitr Infusion Gamepad Button", KeyCode.JoystickButton9,
            "Gamepad button (JoystickButton0-19) for Eitr Infusion, pressed with the modifier of Ability Gamepad " +
            "Buttons Use Modifier. JoystickButton9 is the right stick click, the same chord as Ability Slot 1 and " +
            "the other set hotkeys: no one can wear two sets that need a press. While the full set is worn, " +
            "the button's vanilla action is suppressed. None unbinds it.");
        ShieldThrowKey = BindClient(SectionAbilities, "Shield Throw Hotkey", KeyCode.T,
            "Key that throws your shield while the Shield Throw set bonus is active and a shield is in your hand. " +
            "Shares T with Overwhelming Launch, Earthshaker and Eitr Infusion, since no one can wear two of those full " +
            "sets. While that is true the key does only this: whatever vanilla action shares it (T is the emote wheel) " +
            "is suppressed. None disables the keyboard binding.");
        ShieldThrowGamepadButton = BindClient(SectionAbilities, "Shield Throw Gamepad Button", KeyCode.JoystickButton9,
            "Gamepad button (JoystickButton0-19) for Shield Throw, pressed with the modifier of Ability Gamepad " +
            "Buttons Use Modifier. JoystickButton9 is the right stick click, the same chord as Ability Slot 1 and " +
            "the other set hotkeys: no one can wear two sets that need a press. While the full set is worn with a " +
            "shield in hand, the button's vanilla action is suppressed. None unbinds it.");

        // 9 - Debug
        _loggingEnabled = BindClient(SectionDebug, "Logging Enabled", true, "Enable logging");
        _logLevel = BindClient(SectionDebug, "Log Level", LogLevel.Error,
            "Only log messages of the selected level or higher");
        WelcomeWizardMode = BindClient(SectionDebug, "Welcome Wizard", FirstRunMode.Auto,
            "Whether the Quick Configure setup wizard opens by itself on the main menu, with a Welcome page " +
            "(balance preset, drop mix, features, and every other page). Auto opens it once per user: whether " +
            "you have seen it is shared by every mod manager profile, in ModQuickConfig/FirstRun.cfg next to " +
            "your Valheim saves. ShowNextLaunch opens it on the next launch and then goes back to Auto. Never " +
            "stops it opening by itself in this profile. The panel can be reopened any time from the Mod Config " +
            "button on the main or pause menu.");
        OutputPatchedConfigFiles = BindClient(SectionDebug, "OutputPatchedConfigFiles", false,
            "Just a debug flag for testing the patching system, do not use.");
        VerifyPenaltyScalingCache = BindClient(SectionDebug, "Verify Penalty Scaling Cache", false,
            "Recomputes the movement-penalty measurement on every read and logs any disagreement with the " +
            "per-step cached value. Costs a full status-effect speed pass per read -- for diagnosing a " +
            "suspected stale scaling factor only, do not leave on.");
        EnableHotReloadPatches = BindServer(SectionDebug, "Enable Hot Reloading Patches", true,
            "Controls whether or not patch edits can be live-reloaded. Can cause lag when recompiling patches.");
        AlwaysRefreshCoreConfigs = BindServer(SectionDebug, "Always Refresh Core Configs", false,
            "Overwrites your core configuration with the mod default values on startup. THIS WILL DELETE ANY MODIFICATIONS TO THE CORE CONFIG.");
    }

    /// <summary>
    /// Subscribed after <see cref="ApplyPreviousConfigValues"/> so restoring a migrated value cannot push
    /// a UI refresh through before Awake has built the UI.
    /// </summary>
    private static void RegisterSettingChangedHandlers() {
        TraderPanelPositionX.SettingChanged += (_, _) =>
            MerchantPanel.Instance?.ApplyConfiguredPosition();
        TraderPanelPositionY.SettingChanged += (_, _) =>
            MerchantPanel.Instance?.ApplyConfiguredPosition();
        TemperPanelPositionX.SettingChanged += (_, _) =>
            global::EpicLoot.TemperPanel.Instance?.ApplyConfiguredPosition();
        TemperPanelPositionY.SettingChanged += (_, _) =>
            global::EpicLoot.TemperPanel.Instance?.ApplyConfiguredPosition();
        _adventureModeEnabled.SettingChanged += (_, _) => MinimapController.RefreshAdventurePinFilters();
        RemovePurchasedGambles.SettingChanged += (_, _) => {
            // Only while the merchant is actually open -- rebuilding a hidden panel would
            // instantiate every candidate ItemDrop for nothing, and it re-rolls on show anyway.
            if (MerchantPanel.Instance != null && MerchantPanel.Instance.gameObject.activeInHierarchy) {
                MerchantPanel.Instance.RefreshAll();
            }
        };
        UIAudioVolumeAdjustment.SettingChanged += (_, _) => EnchantingUIController.RefreshUIAudioLevels();
        EnchantingTableUpgradesActive.SettingChanged += (_, _) => EnchantingTableUI.UpdateUpgradeActivation();
        EnchantingTableActivatedTabs.SettingChanged += (_, _) => EnchantingTableUI.UpdateTabActivation();
        ShowQuickConfigButton.SettingChanged += (_, _) => QuickConfig.QuickConfigureTool.ApplyRegistration();
        EliteRunestoneDrops.SettingChanged += (_, _) => EliteRunestoneTables.Refresh();
        EliteRunestoneDropChance.SettingChanged += (_, _) => EliteRunestoneTables.Refresh();
        AllowRuneSocketing.SettingChanged += (_, _) => ShardSocketManager.OnRuneSocketRulesChanged();
        RuneDiminishingReturns.SettingChanged += (_, _) => ShardSocketManager.OnRuneSocketRulesChanged();
        RuneStackDecayFactor.SettingChanged += (_, _) => ShardSocketManager.OnRuneSocketRulesChanged();
        // A shortcut written held keys first ("LeftControl + E", in the file or ConfigurationManager) would fire
        // on the held key. Put it right once now and again whenever it is edited; the second set is a no-op.
        NormalizeSocketOverlayShortcut();
        SocketOverlayShortcut.SettingChanged += (_, _) => NormalizeSocketOverlayShortcut();
        foreach (ConfigEntry<string> rarityColor in new[] {
            _magicRarityColor, _rareRarityColor, _epicRarityColor,
            _legendaryRarityColor, _mythicRarityColor, _ancientRarityColor }) {
            rarityColor.SettingChanged += (_, _) => SetRarityColor.RefreshAll();
        }
    }

    private static void NormalizeSocketOverlayShortcut() {
        KeyboardShortcut normalized = SocketOverlayInput.Normalize(SocketOverlayShortcut.Value);
        if (!normalized.Equals(SocketOverlayShortcut.Value)) {
            SocketOverlayShortcut.Value = normalized;
        }
    }

    /// <summary>Binds a client-local entry in declaration order, recording where it landed for the migration.</summary>
    private static ConfigEntry<T> BindClient<T>(string section, string key, T value, string description,
                                               AcceptableValueBase acceptableValues = null) {
        return Track(ConfigBinder.BindClientConfigInOrder(section, key, value, description, acceptableValues), section, key);
    }

    /// <summary>Binds a server-authoritative (admin only) entry in declaration order.</summary>
    private static ConfigEntry<T> BindServer<T>(string section, string key, T value, string description,
                                               AcceptableValueBase acceptableValues = null) {
        return Track(ConfigBinder.BindServerConfigInOrder(section, key, value, description, acceptableValues), section, key);
    }

    // The mode and range pair of one late boss's own drop (EpicLootDropsHelper), in the Balance section.
    private static void BindBossExtraDrop(string boss, string drop, int amount,
                                          out ConfigEntry<BossExtraDropMode> mode, out ConfigEntry<float> range) {
        mode = BindServer(SectionBalance, $"{boss} Drop Mode", BossExtraDropMode.OneExtraPerNearbyPlayer,
            $"How many {drop} {boss} drops ({amount} normally) when several players take part.\n" +
            $"OneExtraPerNearbyPlayer: one more for each player after the first within '{boss} Drop Player Range'.\n" +
            $"MultiplyByNearbyPlayers: the whole drop once for each of those players ({amount * 4} for four players).\n" +
            "Default leaves the drop unchanged.");
        range = BindServer(SectionBalance, $"{boss} Drop Player Range", 100.0f,
            $"How far from {boss} a player may be to count for '{boss} Drop Mode'.");
    }

    private static ConfigEntry<T> Track<T>(ConfigEntry<T> entry, string section, string key) {
        BoundEntries.Add((entry, $"{section}::{key}"));
        return entry;
    }

    /// <summary>
    /// Snapshots the config file as it stands on disk, keyed by section-without-its-order-prefix. Reading
    /// the file rather than the bound entries is what lets a setting be found under a name BepInEx no
    /// longer binds; unbound entries survive in the file (BepInEx writes its orphans back out) but are
    /// not reachable through the ConfigFile API.
    /// </summary>
    private static void ReadPreviousConfigValues() {
        PreviousConfigValues.Clear();
        PreviousRawSections.Clear();
        if (!File.Exists(cfg.ConfigFilePath)) {
            return;
        }

        string section = string.Empty;
        string rawSection = string.Empty;
        bool sectionIsOrdered = false;
        foreach (string rawLine in File.ReadAllLines(cfg.ConfigFilePath)) {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#') {
                continue;
            }

            if (line[0] == '[' && line[line.Length - 1] == ']') {
                section = line.Substring(1, line.Length - 2).Trim();
                rawSection = section;
                Match orderPrefix = Regex.Match(section, @"^\d+\s*-\s*");
                sectionIsOrdered = orderPrefix.Success;
                section = sectionIsOrdered ? section.Substring(orderPrefix.Length) : section;
                continue;
            }

            int separator = line.IndexOf('=');
            if (separator <= 0) {
                continue;
            }

            // Other binders write sections this file does not own - PieceLoader names one after each
            // piece, so "[Enchanting Table]" holds the table's build cost while "[4 - Enchanting Table]"
            // holds its features. Stripping the prefix makes those two collide, so an ordered section
            // always wins; anything else only fills a gap.
            string location = $"{section}::{line.Substring(0, separator).Trim()}";
            if (sectionIsOrdered || !PreviousConfigValues.ContainsKey(location)) {
                PreviousConfigValues[location] = line.Substring(separator + 1).Trim();
            }
            if (!PreviousRawSections.TryGetValue(location, out List<string> rawSections)) {
                rawSections = new List<string>();
                PreviousRawSections[location] = rawSections;
            }
            if (!rawSections.Contains(rawSection)) {
                rawSections.Add(rawSection);
            }
        }
    }

    /// <summary>
    /// Retires the old per-profile "Show Welcome Message" flag in favour of <see cref="WelcomeWizardMode"/> and the
    /// per-user first-run record every mod manager profile shares (Common/Config/UI, ConfigUIFirstRun). false meant
    /// the wizard had been closed, so this user has seen it; true was both the default and "run it again", which is
    /// Auto. The line is taken out of every section it was found under, so it is not written back. Must run before
    /// <see cref="ApplyPreviousConfigValues"/>, which clears the snapshot this reads.
    /// </summary>
    private static void MigrateWelcomeWizard() {
        string location = $"{SectionDebug}::{LegacyWelcomeMessageKey}";
        if (PreviousConfigValues.TryGetValue(location, out string previous)
            && bool.TryParse(previous, out bool show) && !show
            // A dedicated server has no wizard, and must not write the record on its user's behalf.
            && !GUIManager.IsHeadless()) {
            ConfigUIFirstRun.MarkSeen(WelcomeWizardKey, WelcomeWizardRevision);
            EpicLoot.Log("The Welcome Wizard had already been closed in this profile; recorded it as seen for every profile.");
        }

        if (PreviousRawSections.TryGetValue(location, out List<string> sections)) {
            foreach (string section in sections) {
                ConfigUIFirstRun.TakeLegacyEntry(cfg, section, LegacyWelcomeMessageKey, out _);
            }
        }
    }

    /// <summary>
    /// Folds the settings <see cref="SocketOverlayShortcut"/> replaced into it. The key held with Use comes from
    /// "Socket Overlay Key" or, in an older file, "Socket Overlay Modifier", whose default LeftAlt is part of other
    /// mods' inventory hotkeys (NewtCraftHub eats food on Alt+E) and is not carried over. A key the player chose
    /// becomes the shortcut's held key on E, and None, which held nothing, becomes plain E. "Always Require Socket
    /// Overlay Key" has nothing to carry, since the shortcut is the same for every item. A file that already has
    /// the shortcut keeps it. The old lines are taken out of every section they were found under, so they are not
    /// written back and this runs once. Must run before <see cref="ApplyPreviousConfigValues"/>, which clears the
    /// snapshot this reads.
    /// </summary>
    private static void MigrateSocketOverlayShortcut() {
        KeyCode? held = null;
        if (TryReadPreviousSocketKey(LegacySocketOverlayKey, out KeyCode key)) {
            if (key != LegacySocketOverlayKeyDefault) {
                held = key;
            }
        } else if (TryReadPreviousSocketKey(LegacySocketOverlayModifierKey, out KeyCode modifier)
            && modifier != LegacySocketOverlayModifierDefault) {
            held = modifier;
        }

        if (held.HasValue && !PreviousConfigValues.ContainsKey($"{SectionSockets}::{SocketOverlayShortcutKey}")) {
            SocketOverlayShortcut.Value = held.Value == KeyCode.None
                ? new KeyboardShortcut(KeyCode.E)
                : new KeyboardShortcut(KeyCode.E, held.Value);
        }

        foreach (string legacyKey in new[] {
            LegacySocketOverlayKey, LegacyAlwaysRequireSocketOverlayKey, LegacySocketOverlayModifierKey }) {
            if (PreviousRawSections.TryGetValue($"{SectionSockets}::{legacyKey}", out List<string> sections)) {
                foreach (string section in sections) {
                    ConfigUIFirstRun.TakeLegacyEntry(cfg, section, legacyKey, out _);
                }
            }
        }
    }

    private static bool TryReadPreviousSocketKey(string key, out KeyCode value) {
        value = KeyCode.None;
        return PreviousConfigValues.TryGetValue($"{SectionSockets}::{key}", out string previous)
            && Enum.TryParse(previous, true, out value);
    }

    /// <summary>
    /// Restores values the player had set before this run, for entries BepInEx could not match itself.
    /// That covers two cases: a section whose order prefix changed (handled by the stripped keys), and a
    /// setting that moved or was renamed (handled by <see cref="RelocatedSettings"/>).
    /// </summary>
    private static void ApplyPreviousConfigValues() {
        foreach ((ConfigEntryBase entry, string location) in BoundEntries) {
            if (!PreviousConfigValues.TryGetValue(location, out string previousValue)
                && (!RelocatedSettings.TryGetValue(location, out string previousLocation)
                    || !PreviousConfigValues.TryGetValue(previousLocation, out previousValue))) {
                continue;
            }

            // Setting the value it already holds is a no-op, so this only bites when something moved.
            entry.SetSerializedValue(previousValue);
        }

        BoundEntries.Clear();
        PreviousConfigValues.Clear();
        PreviousRawSections.Clear();
    }

    public static void InitializeConfig() {


        SychronizeConfig<LootConfig>("loottables.json", LootRoller.Initialize,
            LootTablesRPC, LootRoller.GetCFG);
        SychronizeConfig<MagicItemEffectsList>("magiceffects.json", MagicItemEffectDefinitions.Initialize,
            MagicEffectsRPC, MagicItemEffectDefinitions.GetMagicItemEffectDefinitions);
        SychronizeConfig<ShardStonesConfig>("shardstones.json", Shards.InitializeShardDefinitions,
            ShardStonesRPC, Shards.GetCFG);
        // Must precede materialconversions, whose Initialize fires the event that merges these in.
        SychronizeConfig<MaterialConversionsConfig>("shardstoneconversions.json", ShardStoneConversions.Initialize,
            ShardStoneConversionsRPC, ShardStoneConversions.GetCFG);
        // Biome data first: adventuredata resolves its biome names against it (and merges any legacy
        // Bounties.Bosses list into it), and iteminfo takes its biome progression order from it.
        SychronizeConfig<BiomeDataConfig>("biomedata.json", BiomeDataManager.Initialize,
            BiomeDataRPC, BiomeDataManager.GetCFG);
        // Adventure data has to be loaded before iteminfo, as iteminfo uses the adventure data to determine what items can drop
        SychronizeConfig<AdventureDataConfig>("adventuredata.json", AdventureDataManager.Initialize,
            AdventureDataRPC, AdventureDataManager.GetCFG);
        SychronizeConfig<ItemInfoConfig>("iteminfo.json", GatedItemTypeHelper.Initialize,
            ItemConfigRPC, GatedItemTypeHelper.GetCFG);
        SychronizeConfig<EnchantingCostsConfig>("enchantcosts.json", EnchantCostsHelper.Initialize,
            EnchantingCostsRPC, EnchantCostsHelper.GetCFG);
        SychronizeConfig<ItemNameConfig>("itemnames.json", MagicItemNames.Initialize, ItemNamesRPC, MagicItemNames.GetCFG);
        SychronizeConfig<LegendaryItemConfig>("legendaries.json", UniqueLegendaryHelper.Initialize,
            LegendariesRPC, UniqueLegendaryHelper.GetCFG);
        SychronizeConfig<AbilityConfig>("abilities.json", AbilityDefinitions.Initialize, AbilitiesRPC, AbilityDefinitions.GetCFG);
        SychronizeConfig<MaterialConversionsConfig>("materialconversions.json", MaterialConversions.Initialize,
            MaterialConversionRPC, MaterialConversions.GetCFG);
        SychronizeConfig<EnchantingUpgradesConfig>("enchantingupgrades.json", EnchantingTableUpgrades.InitializeConfig,
            EnchantingUpgradesRPC, EnchantingTableUpgrades.GetCFG);
        SychronizeConfig<AutoSorterConfiguration>("itemsorter.json", AutoAddEnchantableItems.InitializeConfig,
            AutoSorterConfigurationRPC, AutoAddEnchantableItems.GetCFG);
        SetupBaseConfigFileWatch(GetOverhaulDirectoryPath());
        SetupPatchConfigFileWatch(FilePatching.PatchesDirPath);

        ItemManager.OnItemsRegistered += InitializeRecipeOnReady;
    }

    /// <summary>
    /// Recipes cannot be created until the game is launched. Epic Loot no longer ships any recipes of its
    /// own, so this only applies recipes other mods registered through <see cref="API.AddRecipe"/>.
    /// Watch for issues, this can potentially trigger after client config synchronization and break.
    /// </summary>
    private static void InitializeRecipeOnReady() {
        RecipesHelper.Initialize(RecipesHelper.Config);
        ItemManager.OnItemsRegistered -= InitializeRecipeOnReady;
    }

    public static string GetLocalizationDirectoryPath() {
        string localizationFolder = Path.Combine(Paths.ConfigPath, "EpicLoot", "localizations");
        DirectoryInfo dirInfo = Directory.CreateDirectory(localizationFolder);
        return dirInfo.FullName;
    }

    public static string GetOverhaulDirectoryPath() {
        string overhaulfolder = Path.Combine(Paths.ConfigPath, "EpicLoot", "baseconfig");
        DirectoryInfo dirInfo = Directory.CreateDirectory(overhaulfolder);
        return dirInfo.FullName;
    }

    public static string GetDefaultEmbeddedFileLocation(string configName) {
        // Callers may pass the config name with or without extension; the embedded resource names
        // (and the magiceffects overhaul check below) require the ".json" suffix.
        if (!configName.EndsWith(".json")) {
            configName += ".json";
        }

        string embeddedcfgpath = "EpicLoot.config." + configName;
        if (configName == "magiceffects.json") {
            embeddedcfgpath = "EpicLoot.config.overhauls." + BalanceConfigurationType.Value + "." + configName;
        }

        return embeddedcfgpath;
    }

    public static void CreateBaseConfigurations(string baseCfgLocation, string filename) {
        EpicLoot.Log($"Base config file {baseCfgLocation} being created from embedded default config.");
        string overhaulFileData = EpicLoot.ReadEmbeddedResourceFile(GetDefaultEmbeddedFileLocation(filename));
        File.WriteAllText(baseCfgLocation, overhaulFileData);
    }

    public static void SychronizeConfig<T>(string filename, Action<T> setupMethod, CustomRPC targetRPC, Func<T> getConfig) where T : class {
        string baseCfgLocation = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), filename);

        // Ensure the base config file exists and reflects the current patches before we read it.
        // LoadPatchedJSON handles the missing-file / AlwaysRefreshCoreConfigs / has-patches cases internally.
        FilePatching.LoadPatchedJSON(filename.Split('.')[0]);

        // Attempt to parse the core config, if its not valid use the embedded default config
        try {
            string fileContents = File.ReadAllText(baseCfgLocation);
            T contents = JsonConvert.DeserializeObject<T>(fileContents);
            if (contents == null) {
                // A file containing just "null" (or empty) deserializes to null without throwing.
                throw new InvalidDataException("file deserialized to null");
            }
            setupMethod(contents);
        } catch (Exception e) {
            EpicLoot.LogWarningForce($"The existing baseconfig file {filename} is invalid! Defaults will be used." +
                $"\n{e.Message}");
            try {
                string defaultConfig = EpicLoot.ReadEmbeddedResourceFile(GetDefaultEmbeddedFileLocation(filename));
                setupMethod(JsonConvert.DeserializeObject<T>(defaultConfig));
            } catch (Exception fallbackException) {
                // The fallback path must never escape: this runs inside the ELConfig constructor,
                // and an unhandled throw here kills Awake before any Harmony patch is applied.
                EpicLoot.LogErrorForce($"Failed to load even the embedded default for {filename}: {fallbackException}");
            }
        }

        EpicLoot.Log($"Finished loading and applying patches for baseconfig file {filename}.");

        ZPackage SendInitialConfig() {
            string cfgs = JsonConvert.SerializeObject(getConfig());
            return SendConfig(cfgs);
        }

        // Setup the initial synchronization for network connection
        SynchronizationManager.Instance.AddInitialSynchronization(targetRPC, SendInitialConfig);

        // Reads the file back into the live config. Shared by the reload scheduler and by the
        // hot-reload pass, which cannot wait for the poll (see ReloadBaseConfigsFromDisk).
        bool ReloadFromDisk() {
            if (!File.Exists(baseCfgLocation)) {
                return false;
            }

            try {
                T contents = JsonConvert.DeserializeObject<T>(File.ReadAllText(baseCfgLocation));
                if (contents == null) {
                    throw new InvalidDataException("file deserialized to null");
                }

                setupMethod(contents);
            } catch (Exception ex) {
                EpicLoot.LogWarningForce($"Config file {baseCfgLocation} is invalid and config will not be updated." + ex);
                return false;
            }

            // Forced: at the default log level this is the only evidence an edit was picked up.
            // IsServer rather than IsHeadless: a player hosting the game is a server too, and its
            // peers were otherwise left on the definitions they were handed at connect.
            if (ZNet.instance != null && ZNet.instance.IsServer() && ZNet.instance.m_peers.Count > 0) {
                List<ZNetPeer> peers = ZNet.instance.m_peers;
                try {
                    targetRPC.SendPackage(peers, SendConfig(JsonConvert.SerializeObject(getConfig())));
                    EpicLoot.LogForce($"Reloaded {filename} from disk and pushed it to {peers.Count} connected peer(s).");
                } catch (Exception ex) {
                    EpicLoot.LogErrorForce($"Reloaded {filename} from disk but pushing it to peers failed: {ex}");
                }
            } else {
                EpicLoot.LogForce($"Reloaded {filename} from disk.");
            }

            return true;
        }

        // Registered in call order, so the load-order dependencies InitializeConfig encodes
        // (biomedata before adventuredata before iteminfo, shardstones before shardstoneconversions)
        // hold on a hot reload too: the reload scheduler applies changed files in this same order.
        BaseConfigReloaders.RemoveAll(reloader => reloader.FileName == filename);
        BaseConfigReloaders.Add((filename, ReloadFromDisk));

        // Registered after the initial read (and after LoadPatchedJSON's write above), so the file as
        // it stands now is the baseline and startup writes are never mistaken for edits. The scheduler
        // polls the file's timestamp and size; the watcher SetupBaseConfigFileWatch installs only asks
        // it to look sooner. See ConfigFileReloader for why events on their own are not enough on a
        // server, and why a connected client does not reload.
        ConfigFileReloader.Watch(baseCfgLocation, ReloadFromDisk);
    }

    public static void StartupProcessModifiedLocalizations() {
        string[] files = Directory.GetFiles(LocalizationDir, "*", SearchOption.AllDirectories);
        EpicLoot.Log($"Processing localization startup file patches: {string.Join(",", files)}");
        foreach (string file in files) {
            if (!file.Contains(".json")) {
                EpicLoot.Log($"File: {file} is not a supported format, ignoring.");
                continue;
            }

            FileInfo fileInfo = new FileInfo(file);
            string language = file.Trim().Split(Path.DirectorySeparatorChar).Last().Split('.').First().Trim();
            if (!LocalizationLanguages.Contains(language)) {
                EpicLoot.LogWarning($"{language} is not a supported language [{string.Join(", ", LocalizationLanguages.ToArray())}]");
                continue;
            }

            // Per-file isolation: these are user-edited files, and a stray comma used to throw out
            // of AddLocalizations -> Awake BEFORE any Harmony patch was applied, leaving the whole
            // mod silently inert. Comments are stripped by the JSON reader itself instead of a
            // regex, which used to truncate any line containing "//" inside a value (URLs).
            try {
                string contents = File.ReadAllText(file);
                // Newtonsoft skips // and /* */ comments natively during parsing, so no regex
                // pre-pass is needed (the old one also broke URLs inside values).
                Dictionary<string, string> localizationUpdates =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(contents);

                if (localizationUpdates == null) {
                    EpicLoot.LogErrorForce($"Localization override {fileInfo.Name} is empty or not a JSON object; skipping it.");
                    continue;
                }

                CheckAndUpdateLocalization(localizationUpdates, language);
            } catch (Exception ex) {
                EpicLoot.LogErrorForce($"Could not parse localization override {fileInfo.Name}: {ex.Message}. Skipping it.");
            }
        }
    }


    /// <summary>
    /// Re-reads baseconfig files into the live config, in registration order.
    ///
    /// Only where this machine owns its config files, the rule the reload scheduler applies to every
    /// edit. On a client connected to someone else's server the live config is the server's push: it is
    /// left in place and the files are left unapplied, so the scheduler reads them after disconnecting.
    /// </summary>
    /// <param name="fileNames">
    /// The files to reload, with extension ("loottables.json"). Null reloads every registered file;
    /// an empty collection reloads none.
    /// </param>
    /// <returns>The files that could not be read back, whose previous contents stay in effect.</returns>
    internal static List<string> ReloadBaseConfigsFromDisk(ICollection<string> fileNames) {
        List<string> failed = new List<string>();
        if (!ConfigFileReloader.OwnsConfigOnDisk()) {
            EpicLoot.Log($"Not reloading {(fileNames == null ? "the baseconfig files" : string.Join(", ", fileNames))}: " +
                "this client is connected to a server whose configs are in effect. They are read after disconnecting.");
            return failed;
        }

        string baseConfigDir = GetOverhaulDirectoryPath();
        foreach ((string fileName, Func<bool> reloadFromDisk) in BaseConfigReloaders) {
            if (fileNames != null && !fileNames.Contains(fileName)) {
                continue;
            }

            if (!reloadFromDisk()) {
                failed.Add(fileName);
            }
            // What is on disk is now what is in memory, so the scheduler must not apply it a second
            // time. Recorded even when the read failed, matching the scheduler's own once-per-edit rule.
            ConfigFileReloader.MarkApplied(Path.Combine(baseConfigDir, fileName));
        }
        return failed;
    }

    /// <summary>
    /// Rebuilds the configs from the patch files on disk and puts the result into the running game.
    ///
    /// The reload has to be driven from here rather than left to the reload scheduler. That polls
    /// the files every couple of seconds, so the auto-add pass below would otherwise run against the
    /// pre-patch config still in memory and write it straight back over the files FilePatching had
    /// just rebuilt. The patch survived on neither disk nor in memory, and only took effect after a
    /// restart.
    /// </summary>
    internal static void RunPatchHotReload() {
        List<string> rebuiltTargets = FilePatching.ReloadAndApplyAllPatches();
        if (rebuiltTargets.Count == 0) {
            // Nothing on disk changed -- a stray json in patches/, or a patch file that failed to
            // parse. Re-deriving the auto-added items would just rewrite the configs unchanged.
            // Forced: this is the "my patch did nothing" case, and every other breadcrumb on this
            // path is Info, which the default Error log level hides.
            EpicLoot.LogForce("Patch files changed, but no config file needed rebuilding. " +
                "Check the log above for a patch file that failed to parse.");
            return;
        }

        // The rebuilt files are this player's own. Connected to someone else's server, its configs stay in
        // effect, as they do for a baseconfig file edited by hand: the reload scheduler reads the rebuilt
        // files after disconnecting. They are rebuilt from the embedded defaults, without the items the
        // equipment pass adds, so the pass runs again on the next world this player loads.
        if (!ConfigFileReloader.OwnsConfigOnDisk()) {
            AutoAddEnchantableItems.RunOnNextWorldLoad();
            EpicLoot.LogForce($"Patch files changed; rebuilt {string.Join(", ", rebuiltTargets)}. This client is " +
                "connected to a server whose configs are in effect, so the change applies after disconnecting.");
            return;
        }

        EpicLoot.LogForce($"Patch files changed; rebuilt and reloaded {string.Join(", ", rebuiltTargets)}.");
        HashSet<string> rebuiltFiles = new HashSet<string>(rebuiltTargets.Select(target => $"{target}.json"));
        ReloadBaseConfigsFromDisk(rebuiltFiles);

        // A rebuilt loottables.json comes back without the creature sorter's "Auto" entries; put them
        // back before the equipment pass below rewrites the file from the live config.
        if (rebuiltFiles.Contains("loottables.json")) {
            CreatureSorterRunner.RunIfWorldLoaded("patch reload");
        }

        if (AutoAddEquipment.Value == false && AutoRemoveEquipmentNotFound.Value == false) {
            return;
        }

        // The scan classifies the ItemDrops currently loaded, so outside a world it finds nothing and
        // writes configs stripped of every item. Re-armed for the next world load instead, since it may
        // already have run this session.
        if (ZNetScene.instance == null) {
            AutoAddEnchantableItems.RunOnNextWorldLoad();
            EpicLoot.LogForce("Patches were rebuilt, but the equipment auto-add pass needs a loaded " +
                "world and was skipped; it runs again when you load one.");
            return;
        }

        // The auto-add pass merges onto the live config, writes the result back out and re-reads the
        // files it rewrote itself, so nothing here waits on the scheduler.
        AutoAddEnchantableItems.CheckAndAddAllEnchantableItems(false);
    }

    private static void IngestPatchFilesFromDisk(object s, FileSystemEventArgs e) {
        if (EnableHotReloadPatches.Value == false) {
            return;
        }

        // No admin check: a rebuild writes only this player's own files, and on a client connected to
        // someone else's server RunPatchHotReload leaves the server's configs in effect until it
        // disconnects. Being that server's admin does not make the local files the server's.

        // Directory events carry no patch content, and the watcher spans subdirectories itself.
        // Directory.Exists instead of File.GetAttributes: a Deleted event (or a Changed event racing
        // an atomic-save rename) arrives for a path that no longer exists, and the GetAttributes
        // throw was silently swallowed upstream -- the reload below then never ran.
        if (Directory.Exists(e.FullPath)) {
            return;
        }

        // Match what ProcessPatchDirectory ingests (*.json). EndsWith, not Contains: the temp files
        // an editor writes alongside an atomic save ("foo.json~", "foo.json.tmp") are not patches,
        // and the rename to the real name raises its own event.
        if (!e.FullPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
            return;
        }

        EpicLoot.Log($"Processing patch file update: {e.FullPath}");
        PatchReloadDebouncer.Schedule();
    }

    private static FileSystemWatcher _baseConfigWatcher;

    /// <summary>
    /// One watcher for the whole baseconfig folder. It reloads nothing itself: an event only asks the
    /// reload scheduler to check the files now instead of on its next poll, so what happens to a file
    /// is decided by its timestamp and size on the main thread, not by which of an editor's burst of
    /// events happened to arrive first.
    /// </summary>
    public static void SetupBaseConfigFileWatch(string path) {
        if (_baseConfigWatcher != null) {
            _baseConfigWatcher.EnableRaisingEvents = false;
            _baseConfigWatcher.Dispose();
            _baseConfigWatcher = null;
        }

        // Filter set through the constructor: Mono's Windows backend copies it to the native watcher
        // only when EnableRaisingEvents is switched on, so a filter assigned afterwards never applies.
        FileSystemWatcher watcher = new FileSystemWatcher(path, "*.json");
        watcher.Created += OnBaseConfigFileEvent;
        watcher.Changed += OnBaseConfigFileEvent;
        watcher.Renamed += OnBaseConfigFileEvent;
        watcher.Deleted += OnBaseConfigFileEvent;
        // FileName included so an editor that saves by writing a temp file and renaming it over the
        // config still reports; LastWrite alone only covers writes made in place.
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
        // baseconfig-backup/ is a sibling of this folder, not a child, so there is nothing below it.
        watcher.IncludeSubdirectories = false;
        // No SynchronizingObject: the handler only sets a flag the scheduler reads on the main thread.
        watcher.EnableRaisingEvents = true;
        _baseConfigWatcher = watcher;
    }

    private static void OnBaseConfigFileEvent(object sender, FileSystemEventArgs e) {
        ConfigFileReloader.CheckSoon();
    }

    private static FileSystemWatcher _patchWatcher;

    public static void SetupPatchConfigFileWatch(string path) {
        // Replacing rather than stacking: a second watcher on the same tree would just double every
        // event. The field is what lets the previous one be disposed; on this Mono runtime the watcher
        // backends hold every instance statically until Dispose, so it is not what keeps events flowing.
        if (_patchWatcher != null) {
            _patchWatcher.EnableRaisingEvents = false;
            _patchWatcher.Dispose();
            _patchWatcher = null;
        }

        FileSystemWatcher newPatchWatcher = new FileSystemWatcher(path);
        newPatchWatcher.Created += new FileSystemEventHandler(IngestPatchFilesFromDisk);
        newPatchWatcher.Changed += new FileSystemEventHandler(IngestPatchFilesFromDisk);
        newPatchWatcher.Renamed += new RenamedEventHandler(IngestPatchFilesFromDisk);
        newPatchWatcher.Deleted += new FileSystemEventHandler(IngestPatchFilesFromDisk);
        // FileName included so dropping in / deleting / renaming a patch file actually fires
        // (LastWrite alone only reports in-place content writes).
        newPatchWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
        // ProcessPatchDirectory recurses, so patches shipped in patches/<ModName>/ are loaded at
        // startup; without this they were loaded but never watched, and only a subdirectory created
        // while the game ran ever got a watcher of its own.
        newPatchWatcher.IncludeSubdirectories = true;
        newPatchWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        newPatchWatcher.EnableRaisingEvents = true;
        _patchWatcher = newPatchWatcher;
    }


    internal static void CheckAndUpdateLocalization(Dictionary<string, string> localizationUpdates, string language) {
        foreach (KeyValuePair<string, string> localization in localizationUpdates) {
            EpicLoot.Log($"Updating localization: {localization.Key} - {localization.Value}");
            LocalizationManager.Instance.GetLocalization().ClearToken(language, localization.Key);
            LocalizationManager.Instance.GetLocalization().AddTranslation(language, localization.Key, localization.Value);
        }
    }

    private static IEnumerator OnClientRecieveLootConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<LootConfig>("loottables.json", package, LootRoller.Initialize);
    }

    private static IEnumerator OnClientRecieveMagicConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<MagicItemEffectsList>("magiceffects.json", package, MagicItemEffectDefinitions.Initialize);
    }

    private static IEnumerator OnClientRecieveItemInfoConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<ItemInfoConfig>("iteminfo.json", package, GatedItemTypeHelper.Initialize);
    }

    private static IEnumerator OnClientRecieveEnchantingCostsConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<EnchantingCostsConfig>("enchantcosts.json", package, EnchantCostsHelper.Initialize);
    }

    private static IEnumerator OnClientRecieveItemNameConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<ItemNameConfig>("itemnames.json", package, MagicItemNames.Initialize);
    }

    private static IEnumerator OnClientRecieveBiomeDataConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<BiomeDataConfig>("biomedata.json", package, BiomeDataManager.Initialize);
    }

    private static IEnumerator OnClientRecieveAdventureDataConfigs(long sender, ZPackage package) {
        // Full Initialize: the RPC path must fire OnSetupAdventureData and
        // rebuild the features exactly like a local reload does, or every API-registered bounty
        // target / stash item / treasure map silently vanishes the moment a client joins a server.
        return ApplyClientConfig<AdventureDataConfig>("adventuredata.json", package, AdventureDataManager.Initialize);
    }

    private static IEnumerator OnClientRecieveLegendaryItemConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<LegendaryItemConfig>("legendaries.json", package, UniqueLegendaryHelper.Initialize);
    }

    private static IEnumerator OnClientRecieveAbilityConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<AbilityConfig>("abilities.json", package, AbilityDefinitions.Initialize);
    }

    private static IEnumerator OnClientRecieveMaterialConversionConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<MaterialConversionsConfig>("materialconversions.json", package, MaterialConversions.Initialize);
    }

    private static IEnumerator OnClientRecieveEnchantingUpgradesConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<EnchantingUpgradesConfig>("enchantingupgrades.json", package, EnchantingTableUpgrades.InitializeConfig);
    }

    private static IEnumerator OnClientRecieveAutoSorterConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<AutoSorterConfiguration>("itemsorter.json", package, AutoAddEnchantableItems.InitializeConfig);
    }

    private static IEnumerator OnClientRecieveShardStonesConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<ShardStonesConfig>("shardstones.json", package, Shards.InitializeShardDefinitions);
    }

    private static IEnumerator OnClientRecieveShardStoneConversionsConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<MaterialConversionsConfig>("shardstoneconversions.json", package, ShardStoneConversions.Initialize);
    }

    /// <summary>
    /// Raised on a client after a server-pushed baseconfig file ("loottables.json") is applied to the live
    /// config. Quick Configure waits on it after a server admin's save, since the push, not the server's
    /// reply, is what brings the saved values into this game.
    /// </summary>
    internal static event Action<string> ServerConfigApplied;

    // One guard for every server-pushed config: a payload that fails to parse (or deserializes to
    // null -- JsonConvert returns null for the literal "null" WITHOUT throwing) is ignored with an
    // error, keeping whatever config is currently loaded. Passing the null through used to clear
    // the live tables and then NRE inside the Initialize chain, leaving the client without that
    // whole subsystem for the session.
    private static IEnumerator ApplyClientConfig<T>(string name, ZPackage package, Action<T> initialize) where T : class {
        T parsed = ClientRecieveParseJsonConfig<T>(package.ReadString());
        if (parsed == null) {
            EpicLoot.LogErrorForce($"Server-pushed {name} could not be parsed; keeping the currently loaded config.");
        } else {
            initialize(parsed);
            // Memory now holds the server's copy, not the local file: forget the file's applied stamp
            // so the scheduler restores the local copy on its first check after disconnecting.
            ConfigFileReloader.MarkDirty(Path.Combine(GetOverhaulDirectoryPath(), name));
            // Forced: at the default log level this is the only sign on a client that a push arrived.
            EpicLoot.LogForce($"Applied server-pushed {name}.");
            try {
                ServerConfigApplied?.Invoke(name);
            } catch (Exception e) {
                EpicLoot.LogErrorForce($"A handler of the server-pushed {name} failed: {e}");
            }
        }
        yield return null;
    }

    private static T ClientRecieveParseJsonConfig<T>(string json) {
        try {
            return JsonConvert.DeserializeObject<T>(json);
        } catch (Exception e) {
            EpicLoot.LogError($"There was an error syncing client configs: {e}");
        }
        return default;
    }

    public static ZPackage SendConfig(string zpackage_content) {
        ZPackage package = new ZPackage();
        package.Write(zpackage_content);
        return package;
    }

    public static IEnumerator OnServerRecieveConfigs(long sender, ZPackage package) {
        EpicLoot.Log("Server received config from client, rejecting due to being the server.");
        yield return null;
    }

}
