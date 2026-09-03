using BepInEx;
using BepInEx.Configuration;
using Common;
using EpicLoot.Abilities;
using EpicLoot.Adventure;
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
    public static ConfigEntry<int> _magicMaterialIconColor;
    public static ConfigEntry<int> _rareMaterialIconColor;
    public static ConfigEntry<int> _epicMaterialIconColor;
    public static ConfigEntry<int> _legendaryMaterialIconColor;
    public static ConfigEntry<int> _mythicMaterialIconColor;
    public static ConfigEntry<bool> UseScrollingCraftDescription;
    public static ConfigEntry<bool> ShowRarityInRecipeList;
    public static ConfigEntry<bool> ShowEnchantSelectionChance;
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
    public static ConfigEntry<bool> _adventureModeEnabled;
    public static readonly ConfigEntry<string>[] AbilityKeyCodes = new ConfigEntry<string>[AbilityController.AbilitySlotCount];
    public static ConfigEntry<TextAnchor> AbilityBarAnchor;
    public static ConfigEntry<Vector2> AbilityBarPosition;
    public static ConfigEntry<TextAnchor> AbilityBarLayoutAlignment;
    public static ConfigEntry<float> AbilityBarIconSpacing;
    public static ConfigEntry<float> SetItemDropChance;
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
    public static ConfigEntry<bool> AllowGiftOnItemsWithSlots;
    public static ConfigEntry<int> LegendaryGiftSlotsAdded;
    public static ConfigEntry<int> MythicGiftSlotsAdded;
    public static ConfigEntry<float> LegendaryGiftSuccessChance;
    public static ConfigEntry<float> MythicGiftSuccessChance;
    public static ConfigEntry<float> GlobalDropRateModifier;
    public static ConfigEntry<bool> DeferChestLootRoll;

    public static ConfigEntry<bool> AlwaysShowWelcomeMessage;
    public static ConfigEntry<bool> OutputPatchedConfigFiles;
    public static ConfigEntry<bool> VerifyPenaltyScalingCache;
    public static ConfigEntry<bool> EnchantingTableUpgradesActive;
    public static ConfigEntry<bool> EnableLimitedBountiesInProgress;
    public static ConfigEntry<int> MaxInProgressBounties;
    public static ConfigEntry<EnchantingTabs> EnchantingTableActivatedTabs;
    public static ConfigEntry<BossDropMode> _bossCryptKeyDropMode;
    public static ConfigEntry<float> _bossCryptKeyDropPlayerRange;
    public static ConfigEntry<BossDropMode> _bossWishboneDropMode;
    public static ConfigEntry<float> _bossWishboneDropPlayerRange;
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
    public static ConfigEntry<bool> EnableHotReloadPatches;
    public static ConfigEntry<bool> AlwaysRefreshCoreConfigs;
    public static ConfigEntry<int> TooltipMaxWidth;
    public static ConfigEntry<int> TooltipMaxHeight;
    public static ConfigEntry<float> TraderPanelPositionX;
    public static ConfigEntry<float> TraderPanelPositionY;
    public static ConfigEntry<float> TemperPanelPositionX;
    public static ConfigEntry<float> TemperPanelPositionY;

    public static ConfigEntry<RuneExtractMode> RuneExtractItemMode;

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
        "Latvian"
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
    }

    private static void CreateConfigValues() {
        // 1 - General
        UseGeneratedMagicItemNames = BindClient(SectionGeneral, "Use Generated Magic Item Names", true,
            "If true, magic items uses special, randomly generated names based on their rarity, type, and magic effects.");
        KeepInventoryOpenOverItems = BindClient(SectionGeneral, "Keep Inventory Open Over Items", true,
            "When true, pressing Use while the cursor is over an inventory item never closes the " +
            "inventory, so a shard slot press that misses its target does nothing instead of " +
            "shutting everything. Use over an empty slot still closes the inventory as normal.");
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
            "0 = no materials drop.\n" +
            "Min = 0, Max = 1", new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        SetItemDropChance = BindServer(SectionBalance, "Set Item Drop Chance", 0.15f,
            "The percent chance that a legendary or mythic special item will be dropped, enchanted, " +
            "or identified as a set item from the legendaries configuration file.\n" +
            "Min = 0, Max = 1",
            new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
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
            "Sets bosses to drop a number of trophies equal to the number of players. " +
            "Optionally set it to only include players within a certain distance, " +
            "use 'Boss Trophy Drop Player Range' to set the range.");
        _bossTrophyDropPlayerRange = BindServer(SectionBalance, "Boss Trophy Drop Player Range", 100.0f,
            "Sets the range that bosses check when dropping multiple trophies using the OnePerPlayerNearBoss drop mode.");
        _bossCryptKeyDropMode = BindServer(SectionBalance, "Crypt Key Drop Mode", BossDropMode.OnePerPlayerNearBoss,
            "Sets bosses to drop a number of crypt keys equal to the number of players. " +
            "Optionally set it to only include players within a certain distance, " +
            "use 'Crypt Key Drop Player Range' to set the range.");
        _bossCryptKeyDropPlayerRange = BindServer(SectionBalance, "Crypt Key Drop Player Range", 100.0f,
            "Sets the range that bosses check when dropping multiple crypt keys using the OnePerPlayerNearBoss drop mode.");
        _bossWishboneDropMode = BindServer(SectionBalance, "Wishbone Drop Mode", BossDropMode.OnePerPlayerNearBoss,
            "Sets bosses to drop a number of wishbones equal to the number of players. " +
            "Optionally set it to only include players within a certain distance, " +
            "use 'Crypt Key Drop Player Range' to set the range.");
        _bossWishboneDropPlayerRange = BindServer(SectionBalance, "Wishbone Drop Player Range", 100.0f,
            "Sets the range that bosses check when dropping multiple wishbones using the OnePerPlayerNearBoss drop mode.");
        // 3 - Sockets
        AllowDuplicateSocketedEffects = BindServer(SectionSockets, "Allow Duplicate Socketed Effects", false,
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
            "Runes can always be inserted; this only affects removal.\n" +
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
        RuneExtractItemMode = BindServer(SectionSockets, "Rune Extract Mode", RuneExtractMode.ReduceEnchants,
            "Controls what happens to the source item when a rune is extracted from it.\n" +
            "KeepItem = the item is returned untouched.\n" +
            "ReduceEnchants = the extracted enchantment is removed from the item (item kept).\n" +
            "ReduceEnchantsAndRarity = the extracted enchantment is removed, the item's rarity is reduced " +
            "one tier, and remaining effect values are clamped to the new rarity's max.\n" +
            "DestroyItem = the item is consumed.\n" +
            "If the extracted enchantment is the item's only enchantment, the item reverts to a normal item.\n" +
            "Default: ReduceEnchants.");
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
        LegendaryGiftSuccessChance = BindServer(SectionSockets, "Legendary Brokkr Gift Success Chance", 100f,
            "Percent chance that a Legendary Brokkr's Gift adds its slots. On a failed roll the gift is " +
            "still consumed and nothing is added.\n" +
            "Min = 0, Max = 100", new AcceptableValueRange<float>(0f, 100f));
        MythicGiftSuccessChance = BindServer(SectionSockets, "Mythic Brokkr Gift Success Chance", 100f,
            "Percent chance that a Mythic Brokkr's Gift adds its slots. On a failed roll the gift is " +
            "still consumed and nothing is added.\n" +
            "Min = 0, Max = 100", new AcceptableValueRange<float>(0f, 100f));

        // 4 - Enchanting Table
        EnchantingTableUpgradesActive = BindServer(SectionEnchanting, "Upgrades Active", true,
            "Toggles Enchanting Table Upgrade Capabilities. If false, enchanting table features will be unlocked set to Level 1");
        EnchantingTableActivatedTabs = BindServer(SectionEnchanting, "Table Features Active",
            EnchantingTabs.Sacrifice | EnchantingTabs.Augment | EnchantingTabs.Enchant | EnchantingTabs.Disenchant |
            EnchantingTabs.Upgrade | EnchantingTabs.ConvertMaterials | EnchantingTabs.Rune,
            "Toggles Enchanting Table Feature on and off completely.");
        TemperBaseChance = BindServer(SectionEnchanting, "Temper Base Chance", 0.5f,
            "Base chance to temper item when below max value. When effect value is at max, the chance is at the base. If value is above max value, the chance is reduces by the decrement chance. Default value: 0.5");
        TemperDecrement = BindServer(SectionEnchanting, "Temper Decrement Amount", 0.15f,
            "Decrement amount when effect value is above max value. Does not apply if value is below max value. Decrement amount is multiplied by the increment amount the value is above max value. Default value: 0.15");
        TemperDestroysItem = BindServer(SectionEnchanting, "Temper Fail Destroys Item", false,
            "When tempering fails, the item will be destroyed. If False, the item will be returned intact. Default value: False");
        TemperChanceToDestroy = BindServer(SectionEnchanting, "Temper Destroy Chance", 0.5f,
            "If Fail Destroys Item is enabled, Destroy Chance rolls if item should be destroyed. Default value: 0.5");

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
        ShowEquippedAndHotbarItemsInSacrificeTab = BindClient(SectionInterface,
            "ShowEquippedAndHotbarItemsInSacrificeTab", false,
            "If set to false, hides the items that are equipped or on your hotbar in the Sacrifice items list.");
        UIAudioVolumeAdjustment = BindClient(SectionInterface, "AudioVolumeAdjustment", 1.0f,
            "Multiplies the crafting UI sound volume by this percentage [0.0-1.0].\n" +
            "1 = full UI sounds\n" +
            "0 = no UI sounds",
            new AcceptableValueRange<float>(0, 1));
        TooltipMaxWidth = BindClient(SectionInterface, "Tooltip Max Width", 350,
            "Maximum width of the item tooltip box, in pixels.", new AcceptableValueRange<int>(150, 1200));
        TooltipMaxHeight = BindClient(SectionInterface, "Tooltip Max Height", 650,
            "Maximum height of the item tooltip box, in pixels. Content taller than this scrolls.",
            new AcceptableValueRange<int>(350, 4000));
        TraderPanelPositionX = BindClient(SectionInterface, "Trader Panel X Position", -200f,
            "The horizontal on-screen position (RectTransform anchoredPosition X, anchored to the " +
            "top-right of the trader window) of the EpicLoot adventure trader panel. Dragging the " +
            "panel in-game updates this automatically. Default: -200. More negative moves it left, " +
            "toward 0 (and positive) moves it right.");
        TraderPanelPositionY = BindClient(SectionInterface, "Trader Panel Y Position", -155f,
            "The vertical on-screen position (RectTransform anchoredPosition Y, anchored to the " +
            "top-right of the trader window) of the EpicLoot adventure trader panel. Dragging the " +
            "panel in-game updates this automatically. Default: -155.");
        TemperPanelPositionX = BindClient(SectionInterface, "Temper Panel X Position", -200f,
            "The horizontal on-screen position (RectTransform anchoredPosition X, anchored to the " +
            "top-right of the trader window) of the EpicLoot tempering panel. Dragging the panel " +
            "in-game updates this automatically. Default: -200.");
        TemperPanelPositionY = BindClient(SectionInterface, "Temper Panel Y Position", -155f,
            "The vertical on-screen position (RectTransform anchoredPosition Y, anchored to the " +
            "top-right of the trader window) of the EpicLoot tempering panel. Dragging the panel " +
            "in-game updates this automatically. Default: -155.");

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
            "The color of Mythic rarity items, the highest magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _mythicMaterialIconColor = BindClient(SectionItemColors, "Mythic Crafting Material Icon Index", 1,
            "Indicates the color of the icon used for legendary crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _setItemColor = BindClient(SectionItemColors, "Set Item Color", "#26ffff",
            "The color of set item text and the set item icon. Use a hex color, default is cyan");

        // 8 - Abilities
        AbilityKeyCodes[0] = BindClient(SectionAbilities, "Ability Hotkey 1", "g", "Hotkey for Ability Slot 1.");
        AbilityKeyCodes[1] = BindClient(SectionAbilities, "Ability Hotkey 2", "h", "Hotkey for Ability Slot 2.");
        AbilityKeyCodes[2] = BindClient(SectionAbilities, "Ability Hotkey 3", "j", "Hotkey for Ability Slot 3.");
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

        // 9 - Debug
        _loggingEnabled = BindClient(SectionDebug, "Logging Enabled", true, "Enable logging");
        _logLevel = BindClient(SectionDebug, "Log Level", LogLevel.Error,
            "Only log messages of the selected level or higher");
        AlwaysShowWelcomeMessage = BindClient(SectionDebug, "Show Welcome Message, automatically set to false once config is viewed.", true,
            "Sets whether or not the welcome message is displayed on startup, this is automatically set to false once the player has viewed the message.");
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
        _adventureModeEnabled.SettingChanged += (_, _) => MinimapController.RefreshAdventureToggleContainer();
        EnchantingTableUpgradesActive.SettingChanged += (_, _) => EnchantingTableUI.UpdateUpgradeActivation();
        EnchantingTableActivatedTabs.SettingChanged += (_, _) => EnchantingTableUI.UpdateTabActivation();
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
        if (!File.Exists(cfg.ConfigFilePath)) {
            return;
        }

        string section = string.Empty;
        bool sectionIsOrdered = false;
        foreach (string rawLine in File.ReadAllLines(cfg.ConfigFilePath)) {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#') {
                continue;
            }

            if (line[0] == '[' && line[line.Length - 1] == ']') {
                section = line.Substring(1, line.Length - 2).Trim();
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
        }
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

        // Reads the file back into the live config. Shared by the file watcher and by the hot-reload
        // pass, which cannot wait for the watcher (see ReloadBaseConfigsFromDisk).
        bool ReloadFromDisk() {
            if (!File.Exists(baseCfgLocation)) {
                return false;
            }

            try {
                T contents = JsonConvert.DeserializeObject<T>(File.ReadAllText(baseCfgLocation));
                if (contents == null) {
                    throw new InvalidDataException("file deserialized to null");
                }

                EpicLoot.Log($"Config file {baseCfgLocation} has been modified, updating config.");
                setupMethod(contents);
            } catch (Exception ex) {
                EpicLoot.LogWarningForce($"Config file {baseCfgLocation} is invalid and config will not be updated." + ex);
                return false;
            }

            if (GUIManager.IsHeadless()) {
                try {
                    targetRPC.SendPackage(ZNet.instance.m_peers, SendConfig(JsonConvert.SerializeObject(getConfig())));
                } catch {
                    // TODO check
                    EpicLoot.LogError($"Error while server syncing {filename} configs");
                }
            }

            return true;
        }

        // Registered in call order, so the load-order dependencies InitializeConfig encodes
        // (adventuredata before iteminfo, shardstones before shardstoneconversions) still hold on a
        // hot reload. Thirteen independent watchers fire in whatever order the OS delivers them.
        BaseConfigReloaders.RemoveAll(reloader => reloader.FileName == filename);
        BaseConfigReloaders.Add((filename, ReloadFromDisk));

        // Encapsulated file watcher modification method for the config file
        void FileModified(object sender, FileSystemEventArgs e) {
            if (e.FullPath != baseCfgLocation || !File.Exists(baseCfgLocation)) {
                return;
            }

            EpicLoot.Log($"Config file {baseCfgLocation} {e.FullPath} has been modified, attempting to update config.");
            ReloadFromDisk();
        }

        // Setup the file watcher for the config file. NotifyFilter must include FileName:
        // LastWrite alone never reports create/delete/rename actions, which left the
        // Created/Deleted/Renamed handlers dead and missed editors that save via
        // write-temp-then-rename.
        FileSystemWatcher fsw = new FileSystemWatcher(ELConfig.GetOverhaulDirectoryPath());
        fsw.Created += new FileSystemEventHandler(FileModified);
        fsw.Changed += new FileSystemEventHandler(FileModified);
        fsw.Renamed += new RenamedEventHandler(FileModified);
        fsw.Deleted += new FileSystemEventHandler(FileModified);
        fsw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
        fsw.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        fsw.EnableRaisingEvents = true;
        fsw.Filter = filename;
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
    /// </summary>
    /// <param name="fileNames">
    /// The files to reload, with extension ("loottables.json"). Null reloads every registered file;
    /// an empty collection reloads none.
    /// </param>
    internal static void ReloadBaseConfigsFromDisk(ICollection<string> fileNames) {
        foreach ((string fileName, Func<bool> reloadFromDisk) in BaseConfigReloaders) {
            if (fileNames != null && !fileNames.Contains(fileName)) {
                continue;
            }

            reloadFromDisk();
        }
    }

    /// <summary>
    /// Rebuilds the configs from the patch files on disk and puts the result into the running game.
    ///
    /// The reload has to be driven from here rather than left to the per-file FileSystemWatchers.
    /// Those events are asynchronous and are marshalled onto the main thread, so they cannot be
    /// delivered until this callback returns -- which used to mean the auto-add pass below ran
    /// against the pre-patch config still in memory and wrote it straight back over the files
    /// FilePatching had just rebuilt. The patch survived on neither disk nor in memory, and only
    /// took effect after a restart.
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

        EpicLoot.LogForce($"Patch files changed; rebuilt and reloaded {string.Join(", ", rebuiltTargets)}.");
        HashSet<string> rebuiltFiles = new HashSet<string>(rebuiltTargets.Select(target => $"{target}.json"));
        ReloadBaseConfigsFromDisk(rebuiltFiles);

        if (AutoAddEquipment.Value == false && AutoRemoveEquipmentNotFound.Value == false) {
            return;
        }

        // The scan classifies the ItemDrops currently loaded, so outside a world it finds nothing and
        // writes configs stripped of every item. It runs again on the next world load regardless.
        if (ZNetScene.instance == null) {
            EpicLoot.LogForce("Patches were rebuilt, but the equipment auto-add pass needs a loaded " +
                "world and was skipped; it runs again when you load one.");
            return;
        }

        AutoAddEnchantableItems.CheckAndAddAllEnchantableItems(false);
        // The auto-add pass merges onto the live config and writes the result back out, so re-read
        // the files it rewrote instead of waiting on their watchers.
        ReloadBaseConfigsFromDisk(AutoAddEnchantableItems.RewrittenConfigFiles);
    }

    private static void IngestPatchFilesFromDisk(object s, FileSystemEventArgs e) {
        if (EnableHotReloadPatches.Value == false) {
            return;
        }

        if (SynchronizationManager.Instance.PlayerIsAdmin == false) {
            EpicLoot.Log("Player is not an admin, and not allowed to change local configuration. Local config change will not be loaded.");
            return;
        }

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

    private static FileSystemWatcher _patchWatcher;

    public static void SetupPatchConfigFileWatch(string path) {
        // Replacing rather than stacking: a second watcher on the same tree would just double every
        // event. The field also keeps the watcher rooted -- a collected one stops raising events.
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

    private static IEnumerator OnClientRecieveAdventureDataConfigs(long sender, ZPackage package) {
        // Full Initialize (not UpdateAventureData): the RPC path must fire OnSetupAdventureData and
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
        return ApplyClientConfig<AutoSorterConfiguration>("autosorter config", package, AutoAddEnchantableItems.InitializeConfig);
    }

    private static IEnumerator OnClientRecieveShardStonesConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<ShardStonesConfig>("shardstones.json", package, Shards.InitializeShardDefinitions);
    }

    private static IEnumerator OnClientRecieveShardStoneConversionsConfigs(long sender, ZPackage package) {
        return ApplyClientConfig<MaterialConversionsConfig>("shardstoneconversions.json", package, ShardStoneConversions.Initialize);
    }

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
