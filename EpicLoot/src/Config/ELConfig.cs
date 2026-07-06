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

internal class ELConfig
{
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
    public static ConfigEntry<bool> TransferMagicItemToCrafts;
    public static ConfigEntry<bool> _loggingEnabled;
    public static ConfigEntry<LogLevel> _logLevel;
    public static ConfigEntry<bool> UseGeneratedMagicItemNames;
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
    public static ConfigEntry<float> GlobalDropRateModifier;
    public static ConfigEntry<float> ItemsToMaterialsDropRatio;
    public static ConfigEntry<bool> AlwaysShowWelcomeMessage;
    public static ConfigEntry<bool> OutputPatchedConfigFiles;
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
    public static ConfigEntry<float> ItemsUnidentifiedDropRatio;
    public static ConfigEntry<float> UIAudioVolumeAdjustment;
    public static ConfigEntry<bool> AutoAddRemoveEquipmentFromVendor;
    public static ConfigEntry<bool> AutoAddRemoveEquipmentFromLootLists;
    public static ConfigEntry<bool> EnableHotReloadPatches;
    public static ConfigEntry<bool> AlwaysRefreshCoreConfigs;

    public static ConfigEntry<bool> RuneExtractDestroysItem;

    private static CustomRPC LootTablesRPC;
    private static CustomRPC MagicEffectsRPC;
    private static CustomRPC ItemConfigRPC;
    private static CustomRPC RecipesRPC;
    private static CustomRPC EnchantingCostsRPC;
    private static CustomRPC ItemNamesRPC;
    private static CustomRPC AdventureDataRPC;
    private static CustomRPC LegendariesRPC;
    private static CustomRPC AbilitiesRPC;
    private static CustomRPC MaterialConversionRPC;
    private static CustomRPC EnchantingUpgradesRPC;
    private static CustomRPC AutoSorterConfigurationRPC;

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

    public ELConfig(ConfigFile Config)
    {
        // ensure all the config values are created
        cfg = Config;
        cfg.SaveOnConfigSet = true;
        CreateConfigValues(Config);
        SetupConfigRPCs();
        FilePatching.LoadAllPatches();
        InitializeConfig();
        FilePatching.ApplyAllPatches();
    }

    public void SetupConfigRPCs()
    {
        LootTablesRPC = NetworkManager.Instance.AddRPC("epicloot_loottables_RPC",
            OnServerRecieveConfigs, OnClientRecieveLootConfigs);
        MagicEffectsRPC = NetworkManager.Instance.AddRPC("epicloot_magiceffect_RPC",
            OnServerRecieveConfigs, OnClientRecieveMagicConfigs);
        ItemConfigRPC = NetworkManager.Instance.AddRPC("epicloot_itemconfig_RPC",
            OnServerRecieveConfigs, OnClientRecieveItemInfoConfigs);
        RecipesRPC = NetworkManager.Instance.AddRPC("epicloot_recipes_RPC",
            OnServerRecieveConfigs, OnClientRecieveRecipesConfigs);
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
    }

    private void CreateConfigValues(ConfigFile Config)
    {
        // Item Colors
        _magicRarityColor = Config.Bind("Item Colors", "Magic Rarity Color", "Blue",
            "The color of Magic rarity items, the lowest magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _magicMaterialIconColor = Config.Bind("Item Colors", "Magic Crafting Material Icon Index", 5,
            "Indicates the color of the icon used for magic crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _rareRarityColor = Config.Bind("Item Colors", "Rare Rarity Color", "Yellow",
            "The color of Rare rarity items, the second magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _rareMaterialIconColor = Config.Bind("Item Colors", "Rare Crafting Material Icon Index", 2,
            "Indicates the color of the icon used for rare crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _epicRarityColor = Config.Bind("Item Colors", "Epic Rarity Color", "Purple",
            "The color of Epic rarity items, the third magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _epicMaterialIconColor = Config.Bind("Item Colors", "Epic Crafting Material Icon Index", 7,
            "Indicates the color of the icon used for epic crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _legendaryRarityColor = Config.Bind("Item Colors", "Legendary Rarity Color", "Teal",
            "The color of Legendary rarity items, the fourth magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _legendaryMaterialIconColor = Config.Bind("Item Colors", "Legendary Crafting Material Icon Index", 4,
            "Indicates the color of the icon used for legendary crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _mythicRarityColor = Config.Bind("Item Colors", "Mythic Rarity Color", "Orange",
            "The color of Mythic rarity items, the highest magic item tier. " +
            "(Optional, use an HTML hex color starting with # to have a custom color.)\n" +
            "Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
        _mythicMaterialIconColor = Config.Bind("Item Colors", "Mythic Crafting Material Icon Index", 1,
            "Indicates the color of the icon used for legendary crafting materials. A number between 0 and 9.\n" +
            "Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
        _setItemColor = Config.Bind("Item Colors", "Set Item Color", "#26ffff",
            "The color of set item text and the set item icon. Use a hex color, default is cyan");

        // Crafting UI
        UseScrollingCraftDescription = Config.Bind("Crafting UI", "Use Scrolling Craft Description", true,
            "Changes the item description in the crafting panel to scroll instead of scale when it gets too " +
            "long for the space.");
        ShowEquippedAndHotbarItemsInSacrificeTab = Config.Bind("Crafting UI",
            "ShowEquippedAndHotbarItemsInSacrificeTab", false,
            "If set to false, hides the items that are equipped or on your hotbar in the Sacrifice items list.");
        UIAudioVolumeAdjustment = Config.Bind("Crafting UI", "AudioVolumeAdjustment", 1.0f,
            new ConfigDescription("Multiplies the crafting UI sound volume by this percentage [0.0-1.0].\n" +
            "1 = full UI sounds\n" +
            "0 = no UI sounds",
            new AcceptableValueRange<float>(0, 1)));

        // Logging
        _loggingEnabled = Config.Bind("Logging", "Logging Enabled", true, "Enable logging");
        _logLevel = Config.Bind("Logging", "Log Level", LogLevel.Error,
            "Only log messages of the selected level or higher");

        // General
        UseGeneratedMagicItemNames = Config.Bind("General", "Use Generated Magic Item Names", true,
            "If true, magic items uses special, randomly generated names based on their rarity, type, and magic effects.");
        AutoAddEquipment = BindServerConfig("General", "Auto Add Equipment", true,
            "Automatically adds equipment types that can be enchanted to possible drops and gates them" +
            "behind their respective bosses. Disabling this also disables automatic removal of items not found.");
        AutoRemoveEquipmentNotFound = BindServerConfig("General", "Auto Remove Equipment Not Found", true,
            "Automatically removes equipment types that is not found when loading the game.");
        OnlyAddEquipmentWithRecipes = BindServerConfig("General", "Only Add Equipment With Recipes", true,
            "Equipment must be able to be created by a recipe in order to automatically get selected. " +
            "If this is disabled enemy weapons can be added to drops, they are not always valid.");
        AutoAddRemoveEquipmentFromVendor = BindServerConfig("General", "Auto Add Remove Equipment From Vendor", true,
            "Automatically adds/removes equipment from the vendor when it is added/removed from the game. ");
        AutoAddRemoveEquipmentFromLootLists = BindServerConfig("General", "Auto Add Remove Equipment From Loot Lists", true,
            "Automatically adds/removes equipment from the tier based loot lists, and validates other loot lists only contain valid items.");

        // Balance
        BalanceConfigurationType = BindServerConfig("Balance", "Balance Template", "Default",
            "Sets the type of balance configuration to use. " +
            "When initially set can change the value of other configurations in this file.\n" +
            "balanced: the recommended balancing, enchantments are powerful but stronger enemies can be a threat.\n" +
            "minimal: reduced enchantment power to be used with vanilla difficulty options.\n" +
            "legendary: legacy balancing that can make players godlike.",
            new AcceptableValueList<string>("balanced", "legendary", "minimal"));
        _gatedItemTypeModeConfig = BindServerConfig("Balance", "Item Drop Limits",
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
        BossBountyMode = BindServerConfig("Balance", "Gated Bounty Mode", GatedBountyMode.Unlimited,
            "Sets whether available bounties are ungated or gated by boss kills.");
        GatedFreebuildMode = Config.Bind("Balance", "Gated Freebuild Mode", GatedPieceTypeMode.BossKillUnlocksCurrentBiomePieces,
            "Sets whether available pieces for the Freebuild effect are ungated or gated by boss kills.");
        _bossTrophyDropMode = BindServerConfig("Balance", "Boss Trophy Drop Mode", BossDropMode.OnePerPlayerNearBoss,
            "Sets bosses to drop a number of trophies equal to the number of players. " +
            "Optionally set it to only include players within a certain distance, " +
            "use 'Boss Trophy Drop Player Range' to set the range.");
        _bossTrophyDropPlayerRange = BindServerConfig("Balance", "Boss Trophy Drop Player Range", 100.0f,
            "Sets the range that bosses check when dropping multiple trophies using the OnePerPlayerNearBoss drop mode.");
        _bossCryptKeyDropMode = BindServerConfig("Balance", "Crypt Key Drop Mode", BossDropMode.OnePerPlayerNearBoss,
            "Sets bosses to drop a number of crypt keys equal to the number of players. " +
            "Optionally set it to only include players within a certain distance, " +
            "use 'Crypt Key Drop Player Range' to set the range.");
        _bossCryptKeyDropPlayerRange = BindServerConfig("Balance", "Crypt Key Drop Player Range", 100.0f,
            "Sets the range that bosses check when dropping multiple crypt keys using the OnePerPlayerNearBoss drop mode.");
        _bossWishboneDropMode = BindServerConfig("Balance", "Wishbone Drop Mode", BossDropMode.OnePerPlayerNearBoss,
            "Sets bosses to drop a number of wishbones equal to the number of players. " +
            "Optionally set it to only include players within a certain distance, " +
            "use 'Crypt Key Drop Player Range' to set the range.");
        _bossWishboneDropPlayerRange = BindServerConfig("Balance", "Wishbone Drop Player Range", 100.0f,
            "Sets the range that bosses check when dropping multiple wishbones using the OnePerPlayerNearBoss drop mode.");

        _adventureModeEnabled = BindServerConfig("Balance", "Adventure Mode Enabled", true,
            "Set to true to enable all the adventure mode features: secret stash, gambling, treasure maps, and bounties. " +
            "Set to false to disable. This will not actually remove active treasure maps or bounties from your save.");
        _adventureModeEnabled.SettingChanged += (_, _) => MinimapController.RefreshAdventureToggleContainer();

        _andvaranautRange = BindServerConfig("Balance", "Andvaranaut Range", 20,
            "Sets the range that Andvaranaut will activate to locate a treasure chest.");
        SetItemDropChance = BindServerConfig("Balance", "Set Item Drop Chance", 0.15f,
            "The percent chance that a legendary or mythic special item will be dropped, enchanted, " +
            "or identified as a set item from the legendaries configuration file.\n" +
            "Min = 0, Max = 1",
            new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        GlobalDropRateModifier = BindServerConfig("Balance", "Global Drop Rate Modifier", 1.0f,
            "A global percentage that modifies how likely loot is to drop.\n" +
            "1 = Exactly what is in the loot tables will drop.\n" +
            "0 = Nothing will drop.\n" +
            "2 = The number of items in the drop table are twice as likely to drop " +
            "(note, this doesn't double the number of loot dropped, just doubles the relative chance for it to drop).\n" +
            "Min = 0, Max = 4", new AcceptableValueRange<float>(minValue: 0, maxValue: 4));
        ItemsUnidentifiedDropRatio = BindServerConfig("Balance", "Items Unidentified Drop Ratio", 0.0f,
            "Sets the chance that loot is dropped as unidentified items. " +
            "This value is set first, " +
            "Items To Materials Drop Ratio uses the remaining value from this configuration for ratio calculation.\n" +
            "0 = no unidentified items drop, uses only the Items To Materials Drop Ratio.\n" +
            "1 = only unidentified items drop.",
            new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        ItemsToMaterialsDropRatio = BindServerConfig("Balance", "Items To Materials Drop Ratio", 0.0f,
            "Sets the chance, using the remaining value from Items Unidentified Drop Ratio, " +
            "that loot drops are instead dropped as magic crafting materials.\n" +
            "0 = all items, no materials.\n" +
            "1 = all materials, no items. Values between 0 and 1 change the ratio of items to materials that drop.\n" +
            "At 0.5, half of everything that drops would be items and the other half would be materials.\n" +
            "Min = 0, Max = 1", new AcceptableValueRange<float>(minValue: 0, maxValue: 1));
        TransferMagicItemToCrafts = BindServerConfig("Balance", "Transfer Enchants to Crafted Items", false,
            "When enchanted items are used as ingredients in recipes, transfer the highest enchant to the " +
            "newly crafted item. Default: False.");
        RuneExtractDestroysItem = BindServerConfig("Balance", "Rune Extract Destroys Item", true,
            "When extracting a rune from an item, the item will be destroyed. If false, the item will be returned intact. " +
            "Default: True.");

        // Debug
        AlwaysShowWelcomeMessage = Config.Bind("Debug", "Show Welcome Message, automatically set to false once config is viewed.", true,
            "Sets whether or not the welcome message is displayed on startup, this is automatically set to false once the player has viewed the message.");
        OutputPatchedConfigFiles = Config.Bind("Debug", "OutputPatchedConfigFiles", false,
            "Just a debug flag for testing the patching system, do not use.");
        EnableHotReloadPatches = BindServerConfig("Debug", "Enable Hot Reloading Patches", true,
            "Controls whether or not patch edits can be live-reloaded. Can cause lag when recompiling patches.");
        AlwaysRefreshCoreConfigs = BindServerConfig("Debug", "Always Refresh Core Configs", false,
            "Overwrites your core configuration with the mod default values on startup. THIS WILL DELETE ANY MODIFICATIONS TO THE CORE CONFIG.");

        // Abilities
        AbilityKeyCodes[0] = Config.Bind("Abilities", "Ability Hotkey 1", "g", "Hotkey for Ability Slot 1.");
        AbilityKeyCodes[1] = Config.Bind("Abilities", "Ability Hotkey 2", "h", "Hotkey for Ability Slot 2.");
        AbilityKeyCodes[2] = Config.Bind("Abilities", "Ability Hotkey 3", "j", "Hotkey for Ability Slot 3.");
        AbilityBarAnchor = Config.Bind("Abilities", "Ability Bar Anchor", TextAnchor.LowerLeft,
            "The point on the HUD to anchor the ability bar. Changing this also changes the pivot of the ability bar to that corner. " +
            "For reference: the ability bar size is 208 by 64.");
        AbilityBarPosition = Config.Bind("Abilities", "Ability Bar Position", new Vector2(150, 170),
            "The position offset from the Ability Bar Anchor at which to place the ability bar.");
        AbilityBarLayoutAlignment = Config.Bind("Abilities", "Ability Bar Layout Alignment", TextAnchor.LowerLeft,
            "The Ability Bar is a Horizontal Layout Group. This value indicates how the elements inside are aligned. " +
            "Choices with 'Center' in them will keep the items centered on the bar, even if there are fewer than the maximum allowed. " +
            "'Left' will be left aligned, and similar for 'Right'.");
        AbilityBarIconSpacing = Config.Bind("Abilities", "Ability Bar Icon Spacing", 8.0f,
            "The number of units between the icons on the ability bar.");

        // Enchanting Table
        EnchantingTableUpgradesActive = BindServerConfig("Enchanting Table", "Upgrades Active", true,
            "Toggles Enchanting Table Upgrade Capabilities. If false, enchanting table features will be unlocked set to Level 1");
        EnchantingTableActivatedTabs = BindServerConfig("Enchanting Table", $"Table Features Active",
            EnchantingTabs.Sacrifice | EnchantingTabs.Augment | EnchantingTabs.Enchant | EnchantingTabs.Disenchant |
            EnchantingTabs.Upgrade | EnchantingTabs.ConvertMaterials | EnchantingTabs.Rune,
            $"Toggles Enchanting Table Feature on and off completely.");
        EnchantingTableUpgradesActive.SettingChanged += (_, _) => EnchantingTableUI.UpdateUpgradeActivation();
        EnchantingTableActivatedTabs.SettingChanged += (_, _) => EnchantingTableUI.UpdateTabActivation();

        // Bounty Management
        EnableLimitedBountiesInProgress = BindServerConfig("Bounty Management", "Enable Bounty Limit", false,
            "Toggles limiting bounties. Players unable to purchase if enabled and maximum bounty in-progress count is met");
        MaxInProgressBounties = BindServerConfig("Bounty Management", "Max Bounties Per Player", 5,
            "Max amount of in-progress bounties allowed per player.");
    }

    public static void InitializeConfig()
    {


        SychronizeConfig<LootConfig>("loottables.json", LootRoller.Initialize,
            LootTablesRPC, LootRoller.GetCFG);
        SychronizeConfig<MagicItemEffectsList>("magiceffects.json", MagicItemEffectDefinitions.Initialize,
            MagicEffectsRPC, MagicItemEffectDefinitions.GetMagicItemEffectDefinitions);
        // Adventure data has to be loaded before iteminfo, as iteminfo uses the adventure data to determine what items can drop
        SychronizeConfig<AdventureDataConfig>("adventuredata.json", AdventureDataManager.Initialize,
            AdventureDataRPC, AdventureDataManager.GetCFG);
        SychronizeConfig<ItemInfoConfig>("iteminfo.json", GatedItemTypeHelper.Initialize,
            ItemConfigRPC, GatedItemTypeHelper.GetCFG);
        SychronizeConfig<RecipesConfig>("recipes.json", RecipesHelper.Initialize, RecipesRPC, RecipesHelper.GetCFG);
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
    /// Recipes cannot be created until the game is launched.
    /// Watch for issues, this can potentially trigger after client config synchronization and break.
    /// </summary>
    private static void InitializeRecipeOnReady()
    {
        string jsonFile = EpicLoot.ReadEmbeddedResourceFile("EpicLoot.config.recipes.json");
        RecipesConfig result = JsonConvert.DeserializeObject<RecipesConfig>(jsonFile);

        if (RecipesHelper.Config == null)
        {
            RecipesHelper.Initialize(result);
        }
        else
        {
            RecipesHelper.Initialize(RecipesHelper.Config);
        }
        ItemManager.OnItemsRegistered -= InitializeRecipeOnReady;
    }

    public static string GetLocalizationDirectoryPath()
    {
        string localizationFolder = Path.Combine(Paths.ConfigPath, "EpicLoot", "localizations");
        DirectoryInfo dirInfo = Directory.CreateDirectory(localizationFolder);
        return dirInfo.FullName;
    }

    public static string GetOverhaulDirectoryPath()
    {
        string overhaulfolder = Path.Combine(Paths.ConfigPath, "EpicLoot", "baseconfig");
        DirectoryInfo dirInfo = Directory.CreateDirectory(overhaulfolder);
        return dirInfo.FullName;
    }

    public static string GetDefaultEmbeddedFileLocation(string configName)
    {
        string embeddedcfgpath = "EpicLoot.config." + configName;
        if (configName == "magiceffects.json")
        {
            embeddedcfgpath = "EpicLoot.config.overhauls." + BalanceConfigurationType.Value + "." + configName;
        }

        return embeddedcfgpath;
    }

    public static void CreateBaseConfigurations(string baseCfgLocation, string filename)
    {
        EpicLoot.Log($"Base config file {baseCfgLocation} being created from embedded default config.");
        string overhaulFileData = EpicLoot.ReadEmbeddedResourceFile(GetDefaultEmbeddedFileLocation(filename));
        File.WriteAllText(baseCfgLocation, overhaulFileData);
    }

    public static void SychronizeConfig<T>(string filename, Action<T> setupMethod, CustomRPC targetRPC, Func<T> getConfig) where T : class
    {
        string baseCfgLocation = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), filename);

        // Ensure that the core config file exists
        if (File.Exists(baseCfgLocation) == false || AlwaysRefreshCoreConfigs.Value)
        {
            CreateBaseConfigurations(baseCfgLocation, filename);
            FilePatching.LoadPatchedJSON(filename.Split('.')[0], true);
        }

        // Attempt to parse the core config, if its not valid use the embedded default config
        try
        {
            string fileContents = File.ReadAllText(baseCfgLocation);
            T contents = JsonConvert.DeserializeObject<T>(fileContents);
            setupMethod(contents);
        }
        catch (Exception e)
        {
            EpicLoot.LogWarningForce($"The existing baseconfig file {filename} is invalid! Defaults will be used." +
                $"\n{e.Message}");
            string defaultConfig = EpicLoot.ReadEmbeddedResourceFile(GetDefaultEmbeddedFileLocation(filename));
            setupMethod(JsonConvert.DeserializeObject<T>(defaultConfig));
        }

        EpicLoot.Log($"Finished loading and applying patches for baseconfig file {filename}.");

        ZPackage SendInitialConfig()
        {
            string cfgs = JsonConvert.SerializeObject(getConfig());
            return SendConfig(cfgs);
        }

        // Setup the initial synchronization for network connection
        SynchronizationManager.Instance.AddInitialSynchronization(targetRPC, SendInitialConfig);

        // Encapsulated file watcher modification method for the config file
        void FileModified(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath != baseCfgLocation || !File.Exists(baseCfgLocation))
            {
                return;
            }

            EpicLoot.Log($"Config file {baseCfgLocation} {e.FullPath} has been modified, attempting to update config.");

            bool validUpdate = false;
            try
            {
                T contents = JsonConvert.DeserializeObject<T>(File.ReadAllText(baseCfgLocation));
                EpicLoot.Log($"Config file {baseCfgLocation} has been modified, updating config.");
                setupMethod(contents);
                validUpdate = true;
            }
            catch (Exception ex)
            {
                EpicLoot.LogWarningForce($"Config file {baseCfgLocation} is invalid and config will not be updated." + ex);
            }

            if (validUpdate == false)
            {
                return;
            }

            if (GUIManager.IsHeadless())
            {
                try
                {
                    targetRPC.SendPackage(ZNet.instance.m_peers, SendConfig(JsonConvert.SerializeObject(getConfig())));
                }
                catch
                {
                    // TODO check
                    EpicLoot.LogError($"Error while server syncing {filename} configs");
                }
            }
        }

        // Setup the file watcher for the config file
        FileSystemWatcher fsw = new FileSystemWatcher(ELConfig.GetOverhaulDirectoryPath());
        fsw.Created += new FileSystemEventHandler(FileModified);
        fsw.Changed += new FileSystemEventHandler(FileModified);
        fsw.Renamed += new RenamedEventHandler(FileModified);
        fsw.Deleted += new FileSystemEventHandler(FileModified);
        fsw.NotifyFilter = NotifyFilters.LastWrite;
        fsw.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        fsw.EnableRaisingEvents = true;
        fsw.Filter = filename;
    }

    public static void StartupProcessModifiedLocalizations()
    {
        string[] files = Directory.GetFiles(LocalizationDir, "*", SearchOption.AllDirectories);
        EpicLoot.Log($"Processing localization startup file patches: {string.Join(",", files)}");
        foreach (string file in files)
        {
            if (!file.Contains(".json"))
            {
                EpicLoot.Log($"File: {file} is not a supported format, ignoring.");
                continue;
            }

            FileInfo fileInfo = new FileInfo(file);
            string language = file.Trim().Split(Path.DirectorySeparatorChar).Last().Split('.').First().Trim();
            if (!LocalizationLanguages.Contains(language))
            {
                EpicLoot.LogWarning($"{language} is not a supported language [{string.Join(", ", LocalizationLanguages.ToArray())}]");
                continue;
            }

            Dictionary<string, string> localizationUpdates = new Dictionary<string, string>();
            string contents = File.ReadAllText(file);
            string cleanedLocalization = Regex.Replace(contents, @"\/\/.*\n", "");
            localizationUpdates = JsonConvert.DeserializeObject<Dictionary<string, string>>(cleanedLocalization);

            CheckAndUpdateLocalization(localizationUpdates, language);
        }
    }


    private static void IngestPatchFilesFromDisk(object s, FileSystemEventArgs e)
    {
        if (EnableHotReloadPatches.Value == false)
        {
            return;
        }

        if (SynchronizationManager.Instance.PlayerIsAdmin == false)
        {
            EpicLoot.Log("Player is not an admin, and not allowed to change local configuration. Local config change will not be loaded.");
            return;
        }

        // Do not process directories, setup a new watcher- otherwise they get ingored even with subdirectory watching.
        if (File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory))
        {
            SetupPatchConfigFileWatch(e.FullPath);
            EpicLoot.Log($"Adding subdirectory filewatcher: {e.FullPath}");
            return;
        }

        FileInfo fileInfo = new FileInfo(e.FullPath);
        if (!fileInfo.FullName.Contains(".json"))
        {
            return;
        }

        EpicLoot.Log($"Processing patch file update: {fileInfo}");
        FilePatching.ReloadAndApplyAllPatches();

        if (AutoAddEquipment.Value == true || AutoRemoveEquipmentNotFound.Value == true)
        {
            AutoAddEnchantableItems.CheckAndAddAllEnchantableItems(false);
        }
    }

    public static void SetupPatchConfigFileWatch(string path)
    {
        FileSystemWatcher newPatchWatcher = new FileSystemWatcher(path);
        newPatchWatcher.Created += new FileSystemEventHandler(IngestPatchFilesFromDisk);
        newPatchWatcher.Changed += new FileSystemEventHandler(IngestPatchFilesFromDisk);
        newPatchWatcher.Renamed += new RenamedEventHandler(IngestPatchFilesFromDisk);
        newPatchWatcher.Deleted += new FileSystemEventHandler(IngestPatchFilesFromDisk);
        newPatchWatcher.NotifyFilter = NotifyFilters.LastWrite;
        // newPatchWatcher.IncludeSubdirectories = true;
        newPatchWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        newPatchWatcher.EnableRaisingEvents = true;
        // newPatchWatcher.Filter = "*.json";
    }


    internal static void CheckAndUpdateLocalization(Dictionary<string, string> localizationUpdates, string language)
    {
        foreach (KeyValuePair<string, string> localization in localizationUpdates)
        {
            EpicLoot.Log($"Updating localization: {localization.Key} - {localization.Value}");
            LocalizationManager.Instance.GetLocalization().ClearToken(language, localization.Key);
            LocalizationManager.Instance.GetLocalization().AddTranslation(language, localization.Key, localization.Value);
        }
    }

    private static IEnumerator OnClientRecieveLootConfigs(long sender, ZPackage package)
    {
        LootRoller.Initialize(ClientRecieveParseJsonConfig<LootConfig>(package.ReadString()));
        yield return null;
    }

    private static IEnumerator OnClientRecieveMagicConfigs(long sender, ZPackage package)
    {
        MagicItemEffectDefinitions.Initialize(ClientRecieveParseJsonConfig<MagicItemEffectsList>(package.ReadString()));
        yield return null;
    }

    private static IEnumerator OnClientRecieveItemInfoConfigs(long sender, ZPackage package)
    {
        GatedItemTypeHelper.Initialize(ClientRecieveParseJsonConfig<ItemInfoConfig>(package.ReadString()));
        yield return null;
    }

    private static IEnumerator OnClientRecieveRecipesConfigs(long sender, ZPackage package)
    {
        RecipesHelper.Initialize(ClientRecieveParseJsonConfig<RecipesConfig>(package.ReadString()));
        yield return null;
    }

    private static IEnumerator OnClientRecieveEnchantingCostsConfigs(long sender, ZPackage package)
    {
        EnchantCostsHelper.Initialize(ClientRecieveParseJsonConfig<EnchantingCostsConfig>(package.ReadString()));
        yield return null;
    }

    private static IEnumerator OnClientRecieveItemNameConfigs(long sender, ZPackage package)
    {
        MagicItemNames.Initialize(ClientRecieveParseJsonConfig<ItemNameConfig>(package.ReadString()));
        yield return null;
    }

    private static IEnumerator OnClientRecieveAdventureDataConfigs(long sender, ZPackage package)
    {
        AdventureDataManager.UpdateAventureData(ClientRecieveParseJsonConfig<AdventureDataConfig>(package.ReadString()));
        yield return null;
    }

    private static IEnumerator OnClientRecieveLegendaryItemConfigs(long sender, ZPackage package)
    {
        UniqueLegendaryHelper.Initialize(ClientRecieveParseJsonConfig<LegendaryItemConfig>(package.ReadString()));
        yield return null;
    }

    private static IEnumerator OnClientRecieveAbilityConfigs(long sender, ZPackage package)
    {
        AbilityDefinitions.Initialize(ClientRecieveParseJsonConfig<AbilityConfig>(package.ReadString()));
        yield return null;
    }

    private static IEnumerator OnClientRecieveMaterialConversionConfigs(long sender, ZPackage package)
    {
        MaterialConversions.Initialize(ClientRecieveParseJsonConfig<MaterialConversionsConfig>(package.ReadString()));
        yield return null;
    }

    private static IEnumerator OnClientRecieveEnchantingUpgradesConfigs(long sender, ZPackage package)
    {
        EnchantingTableUpgrades.InitializeConfig(ClientRecieveParseJsonConfig<EnchantingUpgradesConfig>(package.ReadString()));
        yield return null;
    }

    private static IEnumerator OnClientRecieveAutoSorterConfigs(long sender, ZPackage package)
    {
        AutoAddEnchantableItems.InitializeConfig(ClientRecieveParseJsonConfig<AutoSorterConfiguration>(package.ReadString()));
        yield return null;
    }

    private static T ClientRecieveParseJsonConfig<T>(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception e)
        {
            EpicLoot.LogError($"There was an error syncing client configs: {e}");
        }
        return default;
    }

    public static ZPackage SendConfig(string zpackage_content)
    {
        ZPackage package = new ZPackage();
        package.Write(zpackage_content);
        return package;
    }

    public static IEnumerator OnServerRecieveConfigs(long sender, ZPackage package)
    {
        EpicLoot.Log("Server received config from client, rejecting due to being the server.");
        yield return null;
    }

    /// <summary>
    /// Helper to bind configs for <TYPE>
    /// </summary>
    /// IsAdminOnly ensures this is a server authoratative value
    /// <returns></returns>
    public static ConfigEntry<T> BindServerConfig<T>(string category, string key, T value, string description,
        AcceptableValueList<string> acceptableValues = null, bool advanced = false)
    {
        return cfg.Bind(category, key, value,
            new ConfigDescription(
                description,
                acceptableValues,
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
        );
    }

    public static ConfigEntry<T> BindServerConfig<T>(string category, string key, T value, string description,
        AcceptableValueRange<float> acceptableValues, bool advanced = false)
    {
        return cfg.Bind(category, key, value,
            new ConfigDescription(
                description,
                acceptableValues,
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
        );
    }

    public static ConfigEntry<T> BindServerConfig<T>(string category, string key, T value, string description,
        AcceptableValueRange<int> acceptableValues, bool advanced = false)
    {
        return cfg.Bind(category, key, value,
            new ConfigDescription(
                description,
                acceptableValues,
            new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced })
        );
    }
}
