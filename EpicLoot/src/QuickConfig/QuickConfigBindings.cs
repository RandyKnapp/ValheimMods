using BepInEx.Configuration;
using Common;
using EpicLoot.Adventure;
using EpicLoot.Biomes;
using EpicLoot.Config;
using EpicLoot.CraftingV2;
using EpicLoot.GatedItemType;
using EpicLoot.ShardStones;
using EpicLoot_UnityLib;
using Jotunn.Managers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace EpicLoot.QuickConfig;

internal enum BindingKind { Bool, Float, Int, Enum, Flags, String, Key, Color, RarityCounts, BiomeCosts, ItemCategories, BiomeDrops, EffectConfigs, Bounties, UpgradeCosts, Action, Readout }

internal enum BindingScope { Client, Server, Json, Action }

/// <summary>
/// What a row key means: how to show it, how to read and write it on the staged snapshot, and when it
/// is visible or enabled. The UI-facing value convention per Kind:
/// Bool = bool, Float = float, Int = int, Enum/Key/String/Color = string (an enum member
/// name, a key name, free text), Flags = HashSet&lt;string&gt; of set member names,
/// BiomeCosts = List&lt;BiomeCostEntry&gt;, ItemCategories = ItemCategoriesValue, BiomeDrops = List&lt;BiomeDropRow&gt;,
/// EffectConfigs = EffectConfigsValue, Bounties = BountiesValue, RarityCounts = RarityCountsValue,
/// UpgradeCosts = UpgradeCostsValue. Action rows run <see cref="Act"/>, Readout rows show <see cref="Readout"/>.
/// </summary>
internal sealed class Binding {
    internal string Key;
    internal string DisplayName;
    internal BindingKind Kind;
    internal BindingScope Scope;
    internal float Min;
    internal float Max;
    internal float Step = 1f;
    /// <summary>Enum member names, key names, colour names or item prefab names; null for free text.</summary>
    internal Func<IList<string>> Options;
    /// <summary>ItemCategories rows: the boss keys an item may be gated behind, for the staged values.</summary>
    internal Func<StagedConfig, IList<string>> KeyOptions;
    internal Func<string> Tooltip;
    internal Func<StagedConfig, bool> VisibleWhen;
    internal Func<StagedConfig, bool> EnabledWhen;
    internal Func<StagedConfig, object> Get;
    internal Action<StagedConfig, object> Set;
    /// <summary>Returns an error message, or null when the staged value can be saved.</summary>
    internal Func<StagedConfig, string> Validate;
    /// <summary>Action rows: runs the action over the staged values and returns a status line, or null.</summary>
    internal Func<StagedConfig, string> Act;
    /// <summary>Readout rows: the text to show for the staged values.</summary>
    internal Func<StagedConfig, string> Readout;
    /// <summary>Colour rows: the key of the Int binding that holds the crafting-material icon index.</summary>
    internal string IconKey;
    /// <summary>
    /// Writes baseconfig files (JSON rows and the preset buttons): read-only for a player who may not
    /// change the files in effect, see <see cref="QuickConfigBindings.CanEditFiles"/>.
    /// </summary>
    internal bool EditsFiles;

    internal string TooltipText() {
        try { return Tooltip?.Invoke() ?? ""; } catch (Exception) { return ""; }
    }

    internal bool IsVisible(StagedConfig staged) {
        try { return VisibleWhen == null || VisibleWhen(staged); } catch (Exception) { return true; }
    }

    internal bool IsEnabled(StagedConfig staged) {
        try { return EnabledWhen == null || EnabledWhen(staged); } catch (Exception) { return true; }
    }
}

/// <summary>
/// The registry behind the row key rule (see README.md in this folder): every key a row GameObject
/// may be named, with the slot that stages its value. Built once, lazily, after ELConfig has bound
/// its entries.
/// </summary>
internal static class QuickConfigBindings {
    internal const string PresetActionPrefix = "action:preset:";
    internal const string DiscordUrl = "https://discord.gg/ZNhYeavv3C";
    internal const string PatchNotesUrl = "https://thunderstore.io/c/valheim/p/RandyKnapp/EpicLoot/changelog/";

    private static readonly Dictionary<string, Binding> bindings = new Dictionary<string, Binding>(StringComparer.Ordinal);
    private static readonly List<ConfigSlot> slots = new List<ConfigSlot>();
    private static bool built;

    internal static IEnumerable<Binding> All {
        get { Ensure(); return bindings.Values; }
    }

    internal static IReadOnlyList<ConfigSlot> Slots {
        get { Ensure(); return slots; }
    }

    internal static Binding Get(string key) {
        Ensure();
        return string.IsNullOrEmpty(key) == false && bindings.TryGetValue(key, out Binding binding) ? binding : null;
    }

    internal static bool IsHost() {
        return ZNet.instance == null || ZNet.instance.IsServer();
    }

    /// <summary>True on a client connected to someone else's server whose admin list holds this player.</summary>
    internal static bool IsRemoteAdmin() {
        return ZNet.instance != null && ZNet.instance.IsServer() == false
            && SynchronizationManager.Instance != null && SynchronizationManager.Instance.PlayerIsAdmin;
    }

    /// <summary>
    /// Whether this player may change the baseconfig files in effect (the JSON rows and the balance
    /// presets): the host writes its own, a server admin's are written on the server
    /// (<see cref="QuickConfigServerSave"/>).
    /// </summary>
    internal static bool CanEditFiles() {
        return IsHost() || IsRemoteAdmin();
    }

    private static void Ensure() {
        if (built) { return; }
        // Not bound yet: ELConfig runs from Awake, and nothing opens the panel before that.
        if (ELConfig.cfg == null || ELConfig.GlobalDropRateModifier == null) { return; }
        built = true;
        try {
            Build();
        } catch (Exception e) {
            EpicLoot.LogErrorForce($"Quick Configure could not build its binding registry: {e}");
        }
        AuditTooltips();
    }

    // Every row must explain itself on hover. A binding with nothing to say is a registration bug,
    // reported once so it is fixed rather than shipped.
    private static void AuditTooltips() {
        List<string> silent = new List<string>();
        foreach (Binding binding in bindings.Values) {
            if (string.IsNullOrWhiteSpace(binding.TooltipText())) { silent.Add(binding.Key); }
        }
        if (silent.Count > 0) {
            EpicLoot.LogWarning($"Quick Configure: {silent.Count} binding(s) have no tooltip: {string.Join(", ", silent)}");
        } else {
            EpicLoot.Log($"Quick Configure: {bindings.Count} bindings registered, every one with a tooltip.");
        }
    }

    // ------------------------------------------------------------------------------------------------
    //  The registry, page by page
    // ------------------------------------------------------------------------------------------------

    private static void Build() {
        // --- Welcome ---
        UrlAction("action:url:discord", "$mod_epicloot_cfg_welcome_discord", DiscordUrl,
            "Opens the Epic Loot Discord in your browser.");
        UrlAction("action:url:patchnotes", "$mod_epicloot_cfg_welcome_patchnotes", PatchNotesUrl,
            "Opens the Thunderstore changelog in your browser.");

        // --- 1. Balance ---
        foreach (string preset in BalancePreset.Names) {
            string name = preset;
            Add(new Binding {
                Key = PresetActionPrefix + name,
                DisplayName = "$mod_epicloot_cfg_preset_" + name,
                Kind = BindingKind.Action,
                Scope = BindingScope.Action,
                EditsFiles = true,
                VisibleWhen = _ => CanEditFiles(),
                Tooltip = () => QuickConfigTooltip.Text($"Balance preset: {name}",
                    "Sets the Balance Template and the four drop ratios to this preset. On Save, " +
                    "magiceffects.json is rewritten from the preset's template and the Effect Tuning " +
                    "page is reset to it."),
                Act = staged => BalancePreset.ApplyToStaged(staged, name)
            });
        }
        Readout("readout:template", staged => $"Current template: {staged.Get("BalanceConfigurationType", "balanced")}",
            "Balance Template", "The overhaul variant of magiceffects.json in use (balanced, minimal or legendary). " +
            "Pick a preset above to change it; the file is rewritten from that template on Save.");
        TextChoice("BalanceConfigurationType", ELConfig.BalanceConfigurationType, "Balance Template", BindingScope.Server,
            () => BalancePreset.Names, BalancePreset.Normalize, visible: _ => CanEditFiles() == false);
        FloatSlider("GlobalDropRateModifier", ELConfig.GlobalDropRateModifier, "Global Drop Rate Modifier", BindingScope.Server, 0f, 4f, 0.05f);
        EnumCycle("_gatedItemTypeModeConfig", ELConfig._gatedItemTypeModeConfig, "Item Drop Limits", BindingScope.Server);
        FloatSlider("SetItemDropChance", ELConfig.SetItemDropChance, "Set Item Drop Chance", BindingScope.Server, 0f, 1f, 0.01f);
        Toggle("HealthCriticalEffectsEnabled", ELConfig.HealthCriticalEffectsEnabled, "Health Critical Enchantments", BindingScope.Server);
        FloatSlider("ItemDropRatio", ELConfig.ItemDropRatio, "Item Drop Ratio", BindingScope.Server, 0f, 1f, 0.01f);
        FloatSlider("ShardStoneDropRatio", ELConfig.ShardStoneDropRatio, "Shard Stone Drop Ratio", BindingScope.Server, 0f, 1f, 0.01f);
        FloatSlider("ItemsUnidentifiedDropRatio", ELConfig.ItemsUnidentifiedDropRatio, "Items Unidentified Drop Ratio", BindingScope.Server, 0f, 1f, 0.01f);
        FloatSlider("MaterialsDropRatio", ELConfig.MaterialsDropRatio, "Materials Drop Ratio", BindingScope.Server, 0f, 1f, 0.01f);
        Readout("readout:dropmix", DropMixReadout, "Drop mix",
            "How the four ratios above split each loot drop. They are relative weights, so only their " +
            "proportions matter; this line shows the resulting share of each category.");
        RarityCounts();
        AdvancedPagesAction(QuickConfigureTool.ShowAdvancedKey, "$mod_epicloot_cfg_show_advanced", true,
            "Adds the rest of the pages after this one: loot drops, shardstones and runes, the enchanting " +
            "table, the merchant and bounties, interface, item colors, rarity, effect tuning and advanced. " +
            "Remembered the next time the panel opens.");
        AdvancedPagesAction(QuickConfigureTool.HideAdvancedKey, "$mod_epicloot_cfg_hide_advanced", false,
            "Goes back to this page only. Edits already made on the other pages are kept, and Save still " +
            "writes them. Remembered the next time the panel opens.");

        // --- 2. Features ---
        Toggle("_adventureModeEnabled", ELConfig._adventureModeEnabled, "Adventure Mode Enabled", BindingScope.Server);
        Toggle("EnchantingTableUpgradesActive", ELConfig.EnchantingTableUpgradesActive, "Enchanting Table Upgrades Active", BindingScope.Server);
        Flags("EnchantingTableActivatedTabs", ELConfig.EnchantingTableActivatedTabs, "Table Features Active", BindingScope.Server);

        // --- 3. Loot Drops ---
        Toggle("AutoAddEquipment", ELConfig.AutoAddEquipment, "Auto Add Equipment", BindingScope.Server);
        Func<StagedConfig, bool> autoAddOn = staged => staged.Get("AutoAddEquipment", true);
        Toggle("AutoRemoveEquipmentNotFound", ELConfig.AutoRemoveEquipmentNotFound, "Auto Remove Equipment Not Found", BindingScope.Server, enabled: autoAddOn);
        Toggle("OnlyAddEquipmentWithRecipes", ELConfig.OnlyAddEquipmentWithRecipes, "Only Add Equipment With Recipes", BindingScope.Server, enabled: autoAddOn);
        Toggle("AutoAddRemoveEquipmentFromVendor", ELConfig.AutoAddRemoveEquipmentFromVendor, "Auto Add/Remove Equipment From Vendor", BindingScope.Server, enabled: autoAddOn);
        Toggle("AutoAddRemoveEquipmentFromLootLists", ELConfig.AutoAddRemoveEquipmentFromLootLists, "Auto Add/Remove Equipment From Loot Lists", BindingScope.Server, enabled: autoAddOn);
        Toggle("AutoAddCreaturesToLootTables", ELConfig.AutoAddCreaturesToLootTables, "Auto Add Creatures To Loot Tables", BindingScope.Server);
        Toggle("TransferMagicItemToCrafts", ELConfig.TransferMagicItemToCrafts, "Transfer Enchants to Crafted Items", BindingScope.Server);
        BossDrop("_bossTrophyDropMode", ELConfig._bossTrophyDropMode, "Boss Trophy Drop Mode",
            "_bossTrophyDropPlayerRange", ELConfig._bossTrophyDropPlayerRange, "Boss Trophy Drop Player Range");
        BossDrop("_bossCryptKeyDropMode", ELConfig._bossCryptKeyDropMode, "Crypt Key Drop Mode",
            "_bossCryptKeyDropPlayerRange", ELConfig._bossCryptKeyDropPlayerRange, "Crypt Key Drop Player Range");
        BossDrop("_bossWishboneDropMode", ELConfig._bossWishboneDropMode, "Wishbone Drop Mode",
            "_bossWishboneDropPlayerRange", ELConfig._bossWishboneDropPlayerRange, "Wishbone Drop Player Range");
        BossExtraDrop("ModerDropMode", ELConfig.ModerDropMode, "ModerDropPlayerRange", ELConfig.ModerDropPlayerRange, "Moder");
        BossExtraDrop("YagluthDropMode", ELConfig.YagluthDropMode, "YagluthDropPlayerRange", ELConfig.YagluthDropPlayerRange, "Yagluth");
        BossExtraDrop("QueenDropMode", ELConfig.QueenDropMode, "QueenDropPlayerRange", ELConfig.QueenDropPlayerRange, "Queen");
        BossExtraDrop("FaderDropMode", ELConfig.FaderDropMode, "FaderDropPlayerRange", ELConfig.FaderDropPlayerRange, "Fader");
        BossExtraDrop("FrozenKingDropMode", ELConfig.FrozenKingDropMode, "FrozenKingDropPlayerRange", ELConfig.FrozenKingDropPlayerRange, "Frozen King");
        Toggle("EliteRunestoneDrops", ELConfig.EliteRunestoneDrops, "Elite Runestone Drops", BindingScope.Server);
        FloatSlider("EliteRunestoneDropChance", ELConfig.EliteRunestoneDropChance, "Elite Runestone Drop Chance", BindingScope.Server, 0f, 1f, 0.01f,
            enabled: staged => staged.Get("EliteRunestoneDrops", ELConfig.EliteRunestoneDrops.Value));

        // --- 4. Shardstones & Runes ---
        // The rune rows below only mean anything while runes can be slotted.
        Func<StagedConfig, bool> runeSocketing = staged => staged.Get("AllowRuneSocketing", ELConfig.AllowRuneSocketing.Value);
        Toggle("AllowRuneSocketing", ELConfig.AllowRuneSocketing, "Allow Rune Socketing", BindingScope.Server);
        Toggle("AllowDuplicateSocketedEffects", ELConfig.AllowDuplicateSocketedEffects, "Allow Duplicate Socketed Effects", BindingScope.Server);
        Toggle("AllowShardstoneDuplicateItemEffect", ELConfig.AllowShardstoneDuplicateItemEffect, "Allow Shardstone On Matching Item Effect", BindingScope.Server);
        Toggle("AllowRunestoneDuplicateItemEffect", ELConfig.AllowRunestoneDuplicateItemEffect, "Allow Runestone On Matching Item Effect", BindingScope.Server,
            enabled: runeSocketing);
        EnumCycle("ShardSocketRemovalMode", ELConfig.ShardSocketRemovalMode, "Shard Removal Mode", BindingScope.Server);
        EnumCycle("RuneSocketRemovalMode", ELConfig.RuneSocketRemovalMode, "Rune Removal Mode", BindingScope.Server,
            enabled: runeSocketing);
        EnumCycle("ShardStackingMode", ELConfig.ShardStackingMode, "Shard Stack Mode", BindingScope.Server);
        FloatSlider("ShardStackDecayFactor", ELConfig.ShardStackDecayFactor, "Shard Stack Decay Factor", BindingScope.Server, 0f, 1f, 0.01f,
            visible: staged => staged.Get("ShardStackingMode", ShardStackMode.Diminishing) == ShardStackMode.Diminishing);
        Toggle("RuneDiminishingReturns", ELConfig.RuneDiminishingReturns, "Rune Diminishing Returns", BindingScope.Server,
            enabled: runeSocketing);
        FloatSlider("RuneStackDecayFactor", ELConfig.RuneStackDecayFactor, "Rune Stack Decay Factor", BindingScope.Server, 0f, 1f, 0.01f,
            visible: staged => staged.Get("RuneDiminishingReturns", ELConfig.RuneDiminishingReturns.Value), enabled: runeSocketing);
        EnumCycle("RuneExtractItemMode", ELConfig.RuneExtractItemMode, "Rune Extract Mode", BindingScope.Server);
        EnumCycle("RuneSetExtractItemMode", ELConfig.RuneSetExtractItemMode, "Set Rune Extract Mode", BindingScope.Server);
        Toggle("AllowGiftOnItemsWithSlots", ELConfig.AllowGiftOnItemsWithSlots, "Allow Brokkr Gift On Items With Slots", BindingScope.Server);
        IntSlider("LegendaryGiftSlotsAdded", ELConfig.LegendaryGiftSlotsAdded, "Legendary Gift Slots Added", BindingScope.Server, 1, LootRoller.MaxSocketCount);
        IntSlider("MythicGiftSlotsAdded", ELConfig.MythicGiftSlotsAdded, "Mythic Gift Slots Added", BindingScope.Server, 1, LootRoller.MaxSocketCount);
        IntSlider("AncientGiftSlotsAdded", ELConfig.AncientGiftSlotsAdded, "Ancient Gift Slots Added", BindingScope.Server, 1, LootRoller.MaxSocketCount);
        FloatSlider("LegendaryGiftSuccessChance", ELConfig.LegendaryGiftSuccessChance, "Legendary Gift Success Chance", BindingScope.Server, 0f, 100f, 1f);
        FloatSlider("MythicGiftSuccessChance", ELConfig.MythicGiftSuccessChance, "Mythic Gift Success Chance", BindingScope.Server, 0f, 100f, 1f);
        FloatSlider("AncientGiftSuccessChance", ELConfig.AncientGiftSuccessChance, "Ancient Gift Success Chance", BindingScope.Server, 0f, 100f, 1f);
        JsonFloat("json:shardstones:Global.Values.MovementPenaltyReference", "shardstones.json", "Movement Penalty Reference", 0f, 1f, 0.01f,
            () => ShardGlobal("MovementPenaltyReference"),
            (root, value) => JsonConfigEdits.SetPath(root, "Global.Values.MovementPenaltyReference", value),
            "The movement-speed penalty (0-1) that counts as 'full' for the shard effects that scale " +
            "with how encumbered your gear makes you. Lower values reach full effect on lighter armour.");
        JsonFloat("json:shardstones:Global.Values.BloodBlockSelfDamagePercent", "shardstones.json", "Blood Block Self Damage %", 0f, 100f, 1f,
            () => ShardGlobal("BloodBlockSelfDamagePercent"),
            (root, value) => JsonConfigEdits.SetPath(root, "Global.Values.BloodBlockSelfDamagePercent", value),
            "Percent of the blocked damage the blood-block shard effects turn back on the wearer.");

        // --- 5. Enchanting Table ---
        FloatSlider("TemperBaseChance", ELConfig.TemperBaseChance, "Temper Base Chance", BindingScope.Server, 0f, 1f, 0.01f);
        FloatSlider("TemperDecrement", ELConfig.TemperDecrement, "Temper Decrement Amount", BindingScope.Server, 0f, 1f, 0.01f);
        Toggle("TemperFailDowngrades", ELConfig.TemperFailDowngrades, "Temper Fail Downgrades Effect", BindingScope.Server);
        Toggle("TemperDestroysItem", ELConfig.TemperDestroysItem, "Temper Fail Destroys Item", BindingScope.Server);
        FloatSlider("TemperChanceToDestroy", ELConfig.TemperChanceToDestroy, "Temper Destroy Chance", BindingScope.Server, 0f, 1f, 0.01f,
            visible: staged => staged.Get("TemperDestroysItem", false));
        Toggle("ShowEnchantSelectionChance", ELConfig.ShowEnchantSelectionChance, "Show Enchant Selection Chance", BindingScope.Server);
        Toggle("ShowEquippedAndHotbarItemsInSacrificeTab", ELConfig.ShowEquippedAndHotbarItemsInSacrificeTab, "Show Equipped & Hotbar Items In Sacrifice Tab", BindingScope.Client);
        Toggle("AutoDepositMaterials", ELConfig.AutoDepositMaterials, "Auto Deposit Materials", BindingScope.Client);
        foreach (EnchantingFeature feature in (EnchantingFeature[])Enum.GetValues(typeof(EnchantingFeature))) {
            EnchantingFeature f = feature;
            string defaultKey = $"json:enchantingupgrades:DefaultFeatureLevels.{f}";
            string maxKey = MaxFeatureLevelKey(f);
            JsonInt(defaultKey, "enchantingupgrades.json", $"{f} default level", -1, 12,
                () => FeatureLevel(EnchantingTableUpgrades.Config?.DefaultFeatureLevels, f),
                (root, value) => JsonConfigEdits.SetPath(root, $"DefaultFeatureLevels.{f}", value),
                $"The level the {f} feature starts at on a new enchanting table. -1 = locked until upgraded.",
                staged => FeatureLevelError(staged, f, defaultKey, maxKey));
            // Every level up to the max needs a cost and an UpgradeValues entry, so a raised max pads both.
            JsonInt(maxKey, "enchantingupgrades.json", $"{f} max level", -1, 12,
                () => FeatureLevel(EnchantingTableUpgrades.Config?.MaximumFeatureLevels, f),
                (root, value) => {
                    JsonConfigEdits.SetPath(root, $"MaximumFeatureLevels.{f}", value);
                    JsonConfigEdits.EnsureUpgradeLevels(root, f, value + 1);
                },
                $"The highest level the {f} feature can be upgraded to. The upgrade costs below list one level " +
                "per step up to it: raising it adds levels that start as a copy of the level below, and their " +
                "bonus repeats the top level's until UpgradeValues is edited in the file. Lowering it hides the " +
                "levels above, which stay in the file.",
                staged => FeatureLevelError(staged, f, defaultKey, maxKey));
        }
        UpgradeCosts();

        // --- 6. Adventure: Merchant ---
        IntSlider("_andvaranautRange", ELConfig._andvaranautRange, "Andvaranaut Range", BindingScope.Server, 5, 100);
        Toggle("RemovePurchasedGambles", ELConfig.RemovePurchasedGambles, "Remove Purchased Gambles", BindingScope.Server);
        JsonInt("json:adventuredata:SecretStash.RefreshInterval", "adventuredata.json", "Secret Stash Refresh (days)", 1, 30,
            () => AdventureDataManager.Config?.SecretStash?.RefreshInterval,
            (root, value) => JsonConfigEdits.SetPath(root, "SecretStash.RefreshInterval", value),
            "In-game days between refreshes of Haldor's secret stash.");
        JsonInt("json:adventuredata:Gamble.RefreshInterval", "adventuredata.json", "Gamble Refresh (days)", 1, 30,
            () => AdventureDataManager.Config?.Gamble?.RefreshInterval,
            (root, value) => JsonConfigEdits.SetPath(root, "Gamble.RefreshInterval", value),
            "In-game days between refreshes of the gamble list.");
        JsonInt("json:adventuredata:Gamble.GamblesCount", "adventuredata.json", "Gambles Offered", 0, 12,
            () => AdventureDataManager.Config?.Gamble?.GamblesCount,
            (root, value) => JsonConfigEdits.SetPath(root, "Gamble.GamblesCount", value),
            "How many coin gambles the merchant offers per refresh.");
        JsonInt("json:adventuredata:Gamble.ForestTokenGamblesCount", "adventuredata.json", "Forest Token Gambles", 0, 6,
            () => AdventureDataManager.Config?.Gamble?.ForestTokenGamblesCount,
            (root, value) => JsonConfigEdits.SetPath(root, "Gamble.ForestTokenGamblesCount", value),
            "How many gambles paid in Forest Tokens the merchant offers per refresh.");
        JsonInt("json:adventuredata:Gamble.IronBountyGamblesCount", "adventuredata.json", "Iron Token Gambles", 0, 6,
            () => AdventureDataManager.Config?.Gamble?.IronBountyGamblesCount,
            (root, value) => JsonConfigEdits.SetPath(root, "Gamble.IronBountyGamblesCount", value),
            "How many gambles paid in Iron Bounty Tokens the merchant offers per refresh.");
        JsonInt("json:adventuredata:Gamble.GoldBountyGamblesCount", "adventuredata.json", "Gold Token Gambles", 0, 6,
            () => AdventureDataManager.Config?.Gamble?.GoldBountyGamblesCount,
            (root, value) => JsonConfigEdits.SetPath(root, "Gamble.GoldBountyGamblesCount", value),
            "How many gambles paid in Gold Bounty Tokens the merchant offers per refresh.");
        JsonInt("json:adventuredata:TreasureMap.RefreshInterval", "adventuredata.json", "Treasure Map Refresh (days)", 1, 30,
            () => AdventureDataManager.Config?.TreasureMap?.RefreshInterval,
            (root, value) => JsonConfigEdits.SetPath(root, "TreasureMap.RefreshInterval", value),
            "In-game days between refreshes of the treasure maps on offer.");
        JsonFloat("json:adventuredata:TreasureMap.StartRadiusMin", "adventuredata.json", "Start Radius Min", 0f, 10000f, 50f,
            () => AdventureDataManager.Config?.TreasureMap?.StartRadiusMin,
            (root, value) => JsonConfigEdits.SetPath(root, "TreasureMap.StartRadiusMin", value),
            "Closest distance from the world centre a treasure map may be placed, in metres.");
        JsonFloat("json:adventuredata:TreasureMap.StartRadiusMax", "adventuredata.json", "Start Radius Max", 0f, 10000f, 50f,
            () => AdventureDataManager.Config?.TreasureMap?.StartRadiusMax,
            (root, value) => JsonConfigEdits.SetPath(root, "TreasureMap.StartRadiusMax", value),
            "Farthest distance from the world centre a treasure map may be placed, in metres.",
            staged => staged.Get("json:adventuredata:TreasureMap.StartRadiusMin", 0f) > staged.Get("json:adventuredata:TreasureMap.StartRadiusMax", 0f)
                ? "Treasure map Start Radius Min must not exceed Start Radius Max." : null);
        JsonFloat("json:adventuredata:TreasureMap.MinimapAreaRadius", "adventuredata.json", "Map Circle Radius", 25f, 500f, 5f,
            () => AdventureDataManager.Config?.TreasureMap?.MinimapAreaRadius,
            (root, value) => JsonConfigEdits.SetPath(root, "TreasureMap.MinimapAreaRadius", value),
            "Radius of the search circle a treasure map (and a bounty) draws on the map, in metres.");
        JsonBool("json:adventuredata:TreasureMap.ScaleRadiiToWorldSize", "adventuredata.json", "Scale Radii To World Size",
            () => AdventureDataManager.Config?.TreasureMap?.ScaleRadiiToWorldSize,
            (root, value) => JsonConfigEdits.SetPath(root, "TreasureMap.ScaleRadiiToWorldSize", value),
            "Scale every biome's treasure-map radius band by the real world radius / 10000, so the shipped " +
            "bands keep their meaning on a resized world. Turn off if the bands were retuned by hand.",
            new TreasureMapConfig().ScaleRadiiToWorldSize);
        BiomeCosts();

        // --- 7. Adventure: Bounties ---
        EnumCycle("BossBountyMode", ELConfig.BossBountyMode, "Gated Bounty Mode", BindingScope.Server);
        Toggle("EnableLimitedBountiesInProgress", ELConfig.EnableLimitedBountiesInProgress, "Enable Bounty Limit", BindingScope.Server);
        IntSlider("MaxInProgressBounties", ELConfig.MaxInProgressBounties, "Max Bounties Per Player", BindingScope.Server, 1, 20,
            visible: staged => staged.Get("EnableLimitedBountiesInProgress", false));
        JsonInt("json:adventuredata:Bounties.RefreshInterval", "adventuredata.json", "Bounty Refresh (days)", 1, 30,
            () => AdventureDataManager.Config?.Bounties?.RefreshInterval,
            (root, value) => JsonConfigEdits.SetPath(root, "Bounties.RefreshInterval", value),
            "In-game days between refreshes of the bounty board.");
        BountyTier("Iron", () => AdventureDataManager.Config?.Bounties?.IronMinLevel, () => AdventureDataManager.Config?.Bounties?.IronMaxLevel,
            () => AdventureDataManager.Config?.Bounties?.IronHealthMultiplier);
        BountyTier("Gold", () => AdventureDataManager.Config?.Bounties?.GoldMinLevel, () => AdventureDataManager.Config?.Bounties?.GoldMaxLevel,
            () => AdventureDataManager.Config?.Bounties?.GoldHealthMultiplier);
        BountyTier("Adds", () => AdventureDataManager.Config?.Bounties?.AddsMinLevel, () => AdventureDataManager.Config?.Bounties?.AddsMaxLevel,
            () => AdventureDataManager.Config?.Bounties?.AddsHealthMultiplier);
        Bounties();

        // --- 8. Interface ---
        Toggle("UseGeneratedMagicItemNames", ELConfig.UseGeneratedMagicItemNames, "Use Generated Magic Item Names", BindingScope.Client);
        Toggle("KeepInventoryOpenOverItems", ELConfig.KeepInventoryOpenOverItems, "Keep Inventory Open Over Items", BindingScope.Client);
        Toggle("ShowRarityInRecipeList", ELConfig.ShowRarityInRecipeList, "Show Rarity In Recipe List", BindingScope.Client);
        FloatSlider("UIAudioVolumeAdjustment", ELConfig.UIAudioVolumeAdjustment, "UI Audio Volume", BindingScope.Client, 0f, 1f, 0.05f);
        IntSlider("TooltipMaxWidth", ELConfig.TooltipMaxWidth, "Tooltip Max Width", BindingScope.Client, 150, 1200);
        IntSlider("TooltipMaxHeight", ELConfig.TooltipMaxHeight, "Tooltip Max Height", BindingScope.Client, 350, 4000);
        KeyCodeChoice("SocketOverlayKey", ELConfig.SocketOverlayKey, "Socket Overlay Key", BindingScope.Client);
        Toggle("AlwaysRequireSocketOverlayKey", ELConfig.AlwaysRequireSocketOverlayKey, "Always Require Socket Overlay Key", BindingScope.Client);
        FloatSlider("TraderPanelPositionX", ELConfig.TraderPanelPositionX, "Trader Panel X Position", BindingScope.Client, -4000f, 4000f, 1f);
        FloatSlider("TraderPanelPositionY", ELConfig.TraderPanelPositionY, "Trader Panel Y Position", BindingScope.Client, -4000f, 4000f, 1f);
        FloatSlider("TemperPanelPositionX", ELConfig.TemperPanelPositionX, "Temper Panel X Position", BindingScope.Client, -4000f, 4000f, 1f);
        FloatSlider("TemperPanelPositionY", ELConfig.TemperPanelPositionY, "Temper Panel Y Position", BindingScope.Client, -4000f, 4000f, 1f);
        KeyCodeChoice("TraderPanelDragKey", ELConfig.TraderPanelDragKey, "Trader Panel Drag Key", BindingScope.Client);
        ResetAction("action:reset:TraderPanelPosition", "Reset trader panel position", "TraderPanelPositionX", "TraderPanelPositionY",
            ELConfig.TraderPanelPositionX, ELConfig.TraderPanelPositionY, "Puts the adventure trader panel back where it was originally.");
        ResetAction("action:reset:TemperPanelPosition", "Reset temper panel position", "TemperPanelPositionX", "TemperPanelPositionY",
            ELConfig.TemperPanelPositionX, ELConfig.TemperPanelPositionY, "Puts the tempering panel back where it was originally.");
        Toggle("ShowQuickConfigButton", ELConfig.ShowQuickConfigButton, "Show in Mod Config launcher", BindingScope.Client);
        for (int i = 0; i < ELConfig.AbilityKeyCodes.Length; i++) {
            Hotkey(i);
        }
        EnumChoice("AbilityBarAnchor", ELConfig.AbilityBarAnchor, "Ability Bar Anchor", BindingScope.Client);
        EnumChoice("AbilityBarLayoutAlignment", ELConfig.AbilityBarLayoutAlignment, "Ability Bar Layout Alignment", BindingScope.Client);
        Vector2Part("AbilityBarPosition.x", ELConfig.AbilityBarPosition, "Ability Bar Position X", true);
        Vector2Part("AbilityBarPosition.y", ELConfig.AbilityBarPosition, "Ability Bar Position Y", false);
        FloatSlider("AbilityBarIconSpacing", ELConfig.AbilityBarIconSpacing, "Ability Bar Icon Spacing", BindingScope.Client, 0f, 40f, 1f);

        // --- 9. Item Colors ---
        RarityColor("_magicRarityColor", ELConfig._magicRarityColor, "_magicMaterialIconColor", ELConfig._magicMaterialIconColor, "Magic");
        RarityColor("_rareRarityColor", ELConfig._rareRarityColor, "_rareMaterialIconColor", ELConfig._rareMaterialIconColor, "Rare");
        RarityColor("_epicRarityColor", ELConfig._epicRarityColor, "_epicMaterialIconColor", ELConfig._epicMaterialIconColor, "Epic");
        RarityColor("_legendaryRarityColor", ELConfig._legendaryRarityColor, "_legendaryMaterialIconColor", ELConfig._legendaryMaterialIconColor, "Legendary");
        RarityColor("_mythicRarityColor", ELConfig._mythicRarityColor, "_mythicMaterialIconColor", ELConfig._mythicMaterialIconColor, "Mythic");
        RarityColor("_ancientRarityColor", ELConfig._ancientRarityColor, "_ancientMaterialIconColor", ELConfig._ancientMaterialIconColor, "Ancient");
        HexColor("_setItemColor", ELConfig._setItemColor, "Set Item Color");

        // --- 10. Rarity (loottables.json) ---
        BiomeDrops();

        // --- 11. Effect Tuning ---
        EnumCycle("GatedFreebuildMode", ELConfig.GatedFreebuildMode, "Gated Freebuild Mode", BindingScope.Server);
        EffectConfigs();

        // --- 12. Advanced ---
        Toggle("_loggingEnabled", ELConfig._loggingEnabled, "Logging Enabled", BindingScope.Client);
        EnumChoice("_logLevel", ELConfig._logLevel, "Log Level", BindingScope.Client);
        Toggle("EnableHotReloadPatches", ELConfig.EnableHotReloadPatches, "Enable Hot Reloading Patches", BindingScope.Server);
        Toggle("AlwaysRefreshCoreConfigs", ELConfig.AlwaysRefreshCoreConfigs, "Always Refresh Core Configs", BindingScope.Server);
        if (ModContext.EnableDebugMode != null) {
            Toggle("Common.EnableDebugMode", ModContext.EnableDebugMode, "Debug Mode", BindingScope.Client);
        }
        if (ModContext.ConfigApplyDelay != null) {
            FloatSlider("Common.ConfigApplyDelay", ModContext.ConfigApplyDelay, "Config Apply Delay", BindingScope.Server, 0f, 10f, 0.5f);
        }
        WelcomeWizardNextLaunch();
        ItemCategories();
    }

    // ------------------------------------------------------------------------------------------------
    //  .cfg helpers
    // ------------------------------------------------------------------------------------------------

    private static void Add(Binding binding) {
        bindings[binding.Key] = binding;
    }

    private static CfgSlot CfgSlotFor<T>(string key, ConfigEntry<T> entry) {
        CfgSlot slot = new CfgSlot {
            Key = key,
            Entry = entry,
            Read = () => entry.Value,
            Write = value => entry.Value = (T)value
        };
        slots.Add(slot);
        return slot;
    }

    private static void Toggle(string key, ConfigEntry<bool> entry, string name, BindingScope scope,
        Func<StagedConfig, bool> visible = null, Func<StagedConfig, bool> enabled = null) {
        CfgSlotFor(key, entry);
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Bool, Scope = scope,
            Tooltip = () => QuickConfigTooltip.Of(entry),
            VisibleWhen = visible, EnabledWhen = enabled,
            Get = staged => staged.Get(key, entry.Value),
            Set = (staged, value) => staged.Set(key, value is bool b && b)
        });
    }

    private static void FloatSlider(string key, ConfigEntry<float> entry, string name, BindingScope scope,
        float min, float max, float step, Func<StagedConfig, bool> visible = null, Func<StagedConfig, bool> enabled = null) {
        CfgSlotFor(key, entry);
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Float, Scope = scope, Min = min, Max = max, Step = step,
            Tooltip = () => QuickConfigTooltip.Of(entry),
            VisibleWhen = visible, EnabledWhen = enabled,
            Get = staged => staged.Get(key, entry.Value),
            Set = (staged, value) => staged.Set(key, Snap(ToFloat(value, entry.Value), min, max, step))
        });
    }

    private static void IntSlider(string key, ConfigEntry<int> entry, string name, BindingScope scope,
        int min, int max, Func<StagedConfig, bool> visible = null) {
        CfgSlotFor(key, entry);
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Int, Scope = scope, Min = min, Max = max, Step = 1f,
            Tooltip = () => QuickConfigTooltip.Of(entry),
            VisibleWhen = visible,
            Get = staged => staged.Get(key, entry.Value),
            Set = (staged, value) => staged.Set(key, Mathf.Clamp(ToInt(value, entry.Value), min, max))
        });
    }

    // An enum shown as a cycle button (six members or fewer) or a picker; the widget decides.
    private static void EnumCycle<T>(string key, ConfigEntry<T> entry, string name, BindingScope scope,
        Func<StagedConfig, bool> enabled = null) where T : struct, Enum {
        EnumChoice(key, entry, name, scope, enabled);
    }

    private static void EnumChoice<T>(string key, ConfigEntry<T> entry, string name, BindingScope scope,
        Func<StagedConfig, bool> enabled = null) where T : struct, Enum {
        CfgSlotFor(key, entry);
        string[] names = Enum.GetNames(typeof(T));
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Enum, Scope = scope,
            Options = () => names,
            Tooltip = () => QuickConfigTooltip.Of(entry),
            EnabledWhen = enabled,
            Get = staged => staged.Get(key, entry.Value).ToString(),
            Set = (staged, value) => {
                if (value is string text && Enum.TryParse(text.Trim(), true, out T parsed)) { staged.Set(key, parsed); }
            }
        });
    }

    private static void Flags<T>(string key, ConfigEntry<T> entry, string name, BindingScope scope) where T : struct, Enum {
        CfgSlotFor(key, entry);
        List<string> members = new List<string>();
        foreach (T member in (T[])Enum.GetValues(typeof(T))) {
            if (Convert.ToUInt64(member) != 0) { members.Add(member.ToString()); }
        }
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Flags, Scope = scope,
            Options = () => members,
            Tooltip = () => QuickConfigTooltip.Of(entry),
            Get = staged => {
                ulong bits = Convert.ToUInt64(staged.Get(key, entry.Value));
                HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);
                foreach (string member in members) {
                    ulong bit = Convert.ToUInt64(Enum.Parse(typeof(T), member));
                    if ((bits & bit) == bit) { set.Add(member); }
                }
                return set;
            },
            Set = (staged, value) => {
                if (value is HashSet<string> set == false) { return; }
                ulong bits = 0;
                foreach (string member in set) {
                    if (members.Contains(member)) { bits |= Convert.ToUInt64(Enum.Parse(typeof(T), member)); }
                }
                staged.Set(key, (T)Enum.ToObject(typeof(T), bits));
            }
        });
    }

    private static void KeyCodeChoice(string key, ConfigEntry<KeyCode> entry, string name, BindingScope scope) {
        CfgSlotFor(key, entry);
        List<string> names = Enum.GetNames(typeof(KeyCode)).Distinct().ToList();
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Key, Scope = scope,
            Options = () => names,
            Tooltip = () => QuickConfigTooltip.Of(entry),
            Get = staged => staged.Get(key, entry.Value).ToString(),
            Set = (staged, value) => {
                if (value is string text && Enum.TryParse(text.Trim(), true, out KeyCode parsed)) { staged.Set(key, parsed); }
            }
        });
    }

    // A string entry restricted to a fixed set of values (the balance template).
    private static void TextChoice(string key, ConfigEntry<string> entry, string name, BindingScope scope,
        Func<IList<string>> options, Func<string, string> normalize, Func<StagedConfig, bool> visible = null) {
        CfgSlot slot = CfgSlotFor(key, entry);
        slot.Read = () => normalize(entry.Value);
        slot.ReadDefault = () => normalize((string)entry.DefaultValue);
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.String, Scope = scope,
            Options = options,
            Tooltip = () => QuickConfigTooltip.Of(entry),
            VisibleWhen = visible,
            Get = staged => staged.Get(key, normalize(entry.Value)),
            Set = (staged, value) => { if (value is string text) { staged.Set(key, normalize(text)); } }
        });
    }

    private static void Hotkey(int index) {
        string key = $"AbilityKeyCodes.{index}";
        ConfigEntry<string> entry = ELConfig.AbilityKeyCodes[index];
        CfgSlotFor(key, entry);
        Add(new Binding {
            Key = key, DisplayName = $"Ability Hotkey {index + 1}", Kind = BindingKind.Key, Scope = BindingScope.Client,
            Options = HotkeyNames,
            Tooltip = () => QuickConfigTooltip.Of(entry),
            Get = staged => staged.Get(key, entry.Value),
            Set = (staged, value) => { if (value is string text) { staged.Set(key, text.Trim().ToLowerInvariant()); } },
            Validate = staged => {
                string value = staged.Get(key, "");
                if (string.IsNullOrWhiteSpace(value)) { return $"Ability Hotkey {index + 1} must not be empty."; }
                if (IsValidHotkey(value) == false) { return $"Ability Hotkey {index + 1}: '{value}' is not a key name the game recognises."; }
                for (int other = 0; other < ELConfig.AbilityKeyCodes.Length; other++) {
                    if (other != index && string.Equals(staged.Get($"AbilityKeyCodes.{other}", ""), value, StringComparison.OrdinalIgnoreCase)) {
                        return "The three ability hotkeys must be different keys.";
                    }
                }
                return null;
            }
        });
    }

    // The Advanced page's "Show Welcome Wizard Next Launch" box, over the three-way WelcomeWizardMode: ticked is
    // ShowNextLaunch, and unticking only takes back a ShowNextLaunch, leaving Never alone. The row keeps the key of the
    // bool setting it replaced, because that name is baked into the Page_Advanced prefab.
    private static void WelcomeWizardNextLaunch() {
        const string key = "AlwaysShowWelcomeMessage";
        ConfigEntry<FirstRunMode> entry = ELConfig.WelcomeWizardMode;
        slots.Add(new CfgSlot {
            Key = key,
            Entry = entry,
            Read = () => entry.Value == FirstRunMode.ShowNextLaunch,
            ReadDefault = () => (FirstRunMode)entry.DefaultValue == FirstRunMode.ShowNextLaunch,
            Write = value => {
                if (value is bool next && next) {
                    entry.Value = FirstRunMode.ShowNextLaunch;
                } else if (entry.Value == FirstRunMode.ShowNextLaunch) {
                    entry.Value = FirstRunMode.Auto;
                }
            }
        });
        Add(new Binding {
            Key = key, DisplayName = "Show Welcome Wizard Next Launch", Kind = BindingKind.Bool, Scope = BindingScope.Client,
            Tooltip = () => QuickConfigTooltip.Of(entry),
            Get = staged => staged.Get(key, entry.Value == FirstRunMode.ShowNextLaunch),
            Set = (staged, value) => staged.Set(key, value is bool b && b)
        });
    }

    private static void Vector2Part(string key, ConfigEntry<Vector2> entry, string name, bool isX) {
        CfgSlot slot = new CfgSlot {
            Key = key,
            Entry = entry,
            Read = () => isX ? entry.Value.x : entry.Value.y,
            ReadDefault = () => isX ? ((Vector2)entry.DefaultValue).x : ((Vector2)entry.DefaultValue).y,
            Write = value => {
                Vector2 current = entry.Value;
                entry.Value = isX ? new Vector2((float)value, current.y) : new Vector2(current.x, (float)value);
            }
        };
        slots.Add(slot);
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Float, Scope = BindingScope.Client, Min = -1000f, Max = 1000f, Step = 1f,
            Tooltip = () => QuickConfigTooltip.Of(entry),
            Get = staged => staged.Get(key, isX ? entry.Value.x : entry.Value.y),
            Set = (staged, value) => staged.Set(key, Snap(ToFloat(value, 0f), -1000f, 1000f, 1f))
        });
    }

    private static void BossDrop(string modeKey, ConfigEntry<BossDropMode> modeEntry, string modeName,
        string rangeKey, ConfigEntry<float> rangeEntry, string rangeName) {
        EnumCycle(modeKey, modeEntry, modeName, BindingScope.Server);
        FloatSlider(rangeKey, rangeEntry, rangeName, BindingScope.Server, 10f, 500f, 5f,
            visible: staged => staged.Get(modeKey, modeEntry.Value) == BossDropMode.OnePerPlayerNearBoss);
    }

    private static void BossExtraDrop(string modeKey, ConfigEntry<BossExtraDropMode> modeEntry,
        string rangeKey, ConfigEntry<float> rangeEntry, string boss) {
        EnumCycle(modeKey, modeEntry, $"{boss} Drop Mode", BindingScope.Server);
        FloatSlider(rangeKey, rangeEntry, $"{boss} Drop Player Range", BindingScope.Server, 10f, 500f, 5f,
            visible: staged => staged.Get(modeKey, modeEntry.Value) != BossExtraDropMode.Default);
    }

    private static void ResetAction(string key, string name, string xKey, string yKey,
        ConfigEntry<float> xEntry, ConfigEntry<float> yEntry, string description) {
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Action, Scope = BindingScope.Action,
            Tooltip = () => QuickConfigTooltip.Text(name, description + " Takes effect on Save."),
            Act = staged => {
                staged.Set(xKey, (float)xEntry.DefaultValue);
                staged.Set(yKey, (float)yEntry.DefaultValue);
                return $"{name}: staged. Save to apply.";
            }
        });
    }

    // Show / Hide Advanced Configs: one of the two is visible at a time, in the same place. Pressing it
    // acts at once rather than staging anything, since it changes what the panel shows, not a setting.
    private static void AdvancedPagesAction(string key, string name, bool show, string description) {
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Action, Scope = BindingScope.Action,
            VisibleWhen = _ => QuickConfigureTool.AdvancedPagesShown != show,
            Tooltip = () => QuickConfigTooltip.Text(QuickConfigureTool.L(name), description),
            Act = _ => {
                QuickConfigureTool.SetAdvancedPagesShown(show);
                return null;
            }
        });
    }

    private static void UrlAction(string key, string name, string url, string description) {
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Action, Scope = BindingScope.Action,
            Tooltip = () => QuickConfigTooltip.Text(url, description),
            Act = _ => {
                Application.OpenURL(url);
                return null;
            }
        });
    }

    private static void RarityColor(string colorKey, ConfigEntry<string> colorEntry, string iconKey, ConfigEntry<int> iconEntry, string rarity) {
        CfgSlotFor(colorKey, colorEntry);
        CfgSlotFor(iconKey, iconEntry);
        List<string> colorNames = EpicLoot.MagicItemColors.Keys.ToList();
        Add(new Binding {
            Key = iconKey, DisplayName = $"{rarity} Crafting Material Icon", Kind = BindingKind.Int, Scope = BindingScope.Client,
            Min = 0, Max = 9, Step = 1f,
            Options = () => colorNames,
            Tooltip = () => QuickConfigTooltip.Of(iconEntry),
            Get = staged => staged.Get(iconKey, iconEntry.Value),
            Set = (staged, value) => staged.Set(iconKey, Mathf.Clamp(ToInt(value, iconEntry.Value), 0, 9)),
            Validate = staged => {
                int index = staged.Get(iconKey, iconEntry.Value);
                return index < 0 || index > 9 ? $"{rarity} crafting material icon index must be between 0 and 9." : null;
            }
        });
        Add(new Binding {
            Key = colorKey, DisplayName = $"{rarity} Rarity Color", Kind = BindingKind.Color, Scope = BindingScope.Client,
            IconKey = iconKey,
            Options = () => colorNames,
            Tooltip = () => QuickConfigTooltip.Of(colorEntry),
            Get = staged => staged.Get(colorKey, colorEntry.Value),
            Set = (staged, value) => { if (value is string text) { staged.Set(colorKey, NormalizeColor(text)); } },
            Validate = staged => ColorError(staged.Get(colorKey, ""), false, $"{rarity} Rarity Color")
        });
    }

    // A #hex-only colour. The picker still offers the named colours, stored as their hex value.
    private static void HexColor(string key, ConfigEntry<string> entry, string name) {
        CfgSlotFor(key, entry);
        List<string> colorNames = EpicLoot.MagicItemColors.Keys.ToList();
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Color, Scope = BindingScope.Client,
            Options = () => colorNames,
            Tooltip = () => QuickConfigTooltip.Of(entry),
            Get = staged => staged.Get(key, entry.Value),
            Set = (staged, value) => { if (value is string text) { staged.Set(key, NamedColorToHex(text.Trim())); } },
            Validate = staged => ColorError(staged.Get(key, ""), true, name)
        });
    }

    private static string NamedColorToHex(string text) {
        foreach (KeyValuePair<string, string> pair in EpicLoot.MagicItemColors) {
            if (string.Equals(pair.Key, text, StringComparison.OrdinalIgnoreCase)) { return pair.Value; }
        }
        return text;
    }

    // ------------------------------------------------------------------------------------------------
    //  JSON helpers
    // ------------------------------------------------------------------------------------------------

    private static JsonSlot JsonSlotFor(string key, string file, Func<object> read, Action<JObject, object> write) {
        JsonSlot slot = new JsonSlot { Key = key, File = file, Read = read, Write = write };
        slots.Add(slot);
        return slot;
    }

    private static void JsonFloat(string key, string file, string name, float min, float max, float step,
        Func<float?> read, Action<JObject, float> write, string description, Func<StagedConfig, string> validate = null) {
        JsonSlot slot = JsonSlotFor(key, file, () => read(), (root, value) => write(root, (float)value));
        slot.Default = (_, shipped) => shipped.Token(file, JsonPath(key)) is JValue token ? token.ToObject<float>() : null;
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Float, Scope = BindingScope.Json, EditsFiles = true,
            Min = min, Max = max, Step = step,
            Tooltip = () => QuickConfigTooltip.Text(JsonTooltipKey(key), description),
            Get = staged => staged.Get(key, min),
            Set = (staged, value) => staged.Set(key, Snap(ToFloat(value, min), min, max, step)),
            Validate = validate
        });
    }

    private static void JsonInt(string key, string file, string name, int min, int max,
        Func<int?> read, Action<JObject, int> write, string description, Func<StagedConfig, string> validate = null) {
        JsonSlot slot = JsonSlotFor(key, file, () => read(), (root, value) => write(root, (int)value));
        slot.Default = (_, shipped) => shipped.Token(file, JsonPath(key)) is JValue token ? token.ToObject<int>() : null;
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Int, Scope = BindingScope.Json, EditsFiles = true,
            Min = min, Max = max, Step = 1f,
            Tooltip = () => QuickConfigTooltip.Text(JsonTooltipKey(key), description),
            Get = staged => staged.Get(key, min),
            Set = (staged, value) => staged.Set(key, Mathf.Clamp(ToInt(value, min), min, max)),
            Validate = validate
        });
    }

    // A key the shipped file leaves out is read at the config class's own default: pass it as shippedDefault.
    private static void JsonBool(string key, string file, string name,
        Func<bool?> read, Action<JObject, bool> write, string description, bool? shippedDefault = null) {
        JsonSlot slot = JsonSlotFor(key, file, () => read(), (root, value) => write(root, (bool)value));
        slot.Default = (_, shipped) => shipped.Token(file, JsonPath(key)) is JValue token ? token.ToObject<bool>() : shippedDefault;
        Add(new Binding {
            Key = key, DisplayName = name, Kind = BindingKind.Bool, Scope = BindingScope.Json, EditsFiles = true,
            Tooltip = () => QuickConfigTooltip.Text(JsonTooltipKey(key), description),
            Get = staged => staged.Get(key, false),
            Set = (staged, value) => staged.Set(key, value is bool b && b)
        });
    }

    // --- Enchantments and sockets per rarity (loottables.json MagicEffectsCount / SocketCounts) ---
    // One staged value for both tables of every rarity; a table is only written, rebalanced to 100,
    // when it differs from the baseline.

    // Where + starts looking for a free enchantment count. Validation still accepts 0, as the file does.
    internal const int MinEffectCount = 1;

    private static void RarityCounts() {
        const string key = "json:loottables:RarityCounts";
        JsonSlot slot = JsonSlotFor(key, "loottables.json",
            () => {
                MagicEffectsCountConfig effects = LootRoller.Config?.MagicEffectsCount;
                SocketCountsConfig sockets = LootRoller.Config?.SocketCounts;
                if (effects == null || sockets == null) { return null; }
                RarityCountsValue value = new RarityCountsValue();
                foreach (ItemRarity rarity in Rarities.All) {
                    value.Effects[rarity.ToString()] = WeightTable.FromRows(GetTable(effects, rarity));
                    value.Sockets[rarity.ToString()] = WeightTable.FromRows(GetTable(sockets, rarity));
                }
                return value;
            },
            null);
        slot.WriteWithBaseline = (root, stagedValue, baselineValue) => {
            RarityCountsValue current = (RarityCountsValue)stagedValue;
            RarityCountsValue before = baselineValue as RarityCountsValue;
            WriteRarityTables(root, "MagicEffectsCount", current.Effects, before?.Effects);
            WriteRarityTables(root, "SocketCounts", current.Sockets, before?.Sockets);
        };
        slot.Default = (currentValue, shipped) => {
            RarityCountsValue value = (currentValue as RarityCountsValue)?.Clone() ?? new RarityCountsValue();
            foreach (ItemRarity rarity in Rarities.All) {
                string r = rarity.ToString();
                if (shipped.Token("loottables.json", $"MagicEffectsCount.{r}") is JArray effects) {
                    value.Effects[r] = WeightTable.FromRows(effects.ToObject<float[][]>());
                }
                if (shipped.Token("loottables.json", $"SocketCounts.{r}") is JArray sockets) {
                    value.Sockets[r] = WeightTable.FromRows(sockets.ToObject<float[][]>());
                }
            }
            return value;
        };
        Add(new Binding {
            Key = key, DisplayName = "Rarity", Kind = BindingKind.RarityCounts, Scope = BindingScope.Json, EditsFiles = true,
            Options = () => Rarities.All.Select(rarity => rarity.ToString()).ToList(),
            Tooltip = () => QuickConfigTooltip.Text("loottables.json: MagicEffectsCount, SocketCounts",
                "How many enchantments and shard sockets an item of each rarity rolls. Each line is a possible " +
                "count and its chance in percent; + adds a count. The chances of one table should add up to 100: " +
                "on Save, the ones you did not change are raised or lowered to make up the rest. " +
                $"Sockets go from 0 to {LootRoller.MaxSocketCount}, and Brokkr's Gift can raise an item up to the " +
                "highest socket count listed."),
            Get = staged => staged.Get(key, new RarityCountsValue()),
            Set = (staged, value) => { if (value is RarityCountsValue table) { staged.Set(key, table); } },
            Validate = staged => {
                RarityCountsValue value = staged.Get(key, new RarityCountsValue());
                foreach (ItemRarity rarity in Rarities.All) {
                    string r = rarity.ToString();
                    string error = WeightTable.Validate(RarityCountsValue.Get(value.Effects, r), 0, int.MaxValue, $"{r} enchantments")
                        ?? WeightTable.Validate(RarityCountsValue.Get(value.Sockets, r), 0, LootRoller.MaxSocketCount, $"{r} sockets");
                    if (error != null) { return error; }
                }
                return null;
            }
        });
    }

    private static void WriteRarityTables(JObject root, string path, Dictionary<string, List<WeightEntry>> current,
        Dictionary<string, List<WeightEntry>> before) {
        foreach (KeyValuePair<string, List<WeightEntry>> pair in current) {
            List<WeightEntry> was = null;
            before?.TryGetValue(pair.Key, out was);
            if (WeightTable.Same(pair.Value, was)) { continue; }
            List<WeightEntry> rebalanced = WeightTable.Rebalance(pair.Value, was);
            JsonConfigEdits.SetPath(root, $"{path}.{pair.Key}", JsonConfigEdits.TableToJArray(WeightTable.ToRows(rebalanced)));
        }
    }

    // --- Enchanting table upgrade costs (enchantingupgrades.json UpgradeCosts) ---

    private static void UpgradeCosts() {
        const string key = "json:enchantingupgrades:UpgradeCosts";
        JsonSlot slot = JsonSlotFor(key, "enchantingupgrades.json", () => ReadUpgradeCosts(EnchantingTableUpgrades.Config), null);
        // The shipped costs, level by level; a level the shipped file lacks keeps its current cost.
        slot.Default = (currentValue, shipped) => {
            UpgradeCostsValue defaults = ReadUpgradeCosts(shipped.Typed<EnchantingUpgradesConfig>("enchantingupgrades.json"));
            if (defaults == null || currentValue is UpgradeCostsValue current == false) { return null; }
            UpgradeCostsValue value = current.Clone();
            foreach (KeyValuePair<EnchantingFeature, List<List<CostEntry>>> pair in value.Levels) {
                List<List<CostEntry>> shippedLevels = defaults.Get(pair.Key);
                for (int level = 0; shippedLevels != null && level < pair.Value.Count && level < shippedLevels.Count; level++) {
                    pair.Value[level] = shippedLevels[level];
                }
            }
            return value;
        };
        slot.WriteWithBaseline = (root, stagedValue, baselineValue) => {
            UpgradeCostsValue current = (UpgradeCostsValue)stagedValue;
            UpgradeCostsValue before = baselineValue as UpgradeCostsValue;
            foreach (KeyValuePair<EnchantingFeature, List<List<CostEntry>>> pair in current.Levels) {
                List<List<CostEntry>> was = before?.Get(pair.Key);
                // Levels added by raising the max land in the file first, with their UpgradeValues entry.
                JsonConfigEdits.EnsureUpgradeLevels(root, pair.Key, pair.Value.Count);
                for (int level = 0; level < pair.Value.Count; level++) {
                    List<CostEntry> cost = pair.Value[level];
                    bool added = was == null || level >= was.Count;
                    if (cost == null || (added == false && UpgradeCostsValue.SameLevel(cost, was[level]))) { continue; }
                    JsonConfigEdits.SetUpgradeCost(root, pair.Key, level, cost);
                }
            }
        };
        Add(new Binding {
            Key = key, DisplayName = "Upgrade costs", Kind = BindingKind.UpgradeCosts, Scope = BindingScope.Json, EditsFiles = true,
            Options = ItemPrefabNames,
            Tooltip = () => QuickConfigTooltip.Text("enchantingupgrades.json: UpgradeCosts",
                "What each enchanting table upgrade costs: pick a feature, then edit the items of each level. " +
                "Unlock is the cost of opening a feature that starts locked (default level -1). The levels follow " +
                "the feature's max level above; the item picker lists every item of a loaded world."),
            Get = staged => staged.Get(key, new UpgradeCostsValue()),
            Set = (staged, value) => { if (value is UpgradeCostsValue table) { staged.Set(key, table); } },
            Validate = staged => {
                UpgradeCostsValue value = staged.Get(key, new UpgradeCostsValue());
                foreach (KeyValuePair<EnchantingFeature, List<List<CostEntry>>> pair in value.Levels) {
                    for (int level = 0; level < pair.Value.Count; level++) {
                        foreach (CostEntry entry in pair.Value[level] ?? new List<CostEntry>()) {
                            string where = $"{pair.Key} {UpgradeLevelName(level)} cost";
                            if (string.IsNullOrWhiteSpace(entry.Item)) { return $"{where}: every row needs an item name."; }
                            if (entry.Amount < 1) { return $"{where}: the amount of '{entry.Item}' must be at least 1."; }
                        }
                    }
                }
                return null;
            }
        });
    }

    internal static string UpgradeLevelName(int level) => level == 0 ? "Unlock" : $"Level {level}";

    internal static string MaxFeatureLevelKey(EnchantingFeature feature) => $"json:enchantingupgrades:MaximumFeatureLevels.{feature}";

    /// <summary>How many cost levels a feature shows: Unlock, then Level 1 to its staged max level.</summary>
    internal static int UpgradeLevelsShown(StagedConfig staged, EnchantingFeature feature, int fallback) {
        string key = MaxFeatureLevelKey(feature);
        return staged.IsAvailable(key) ? Math.Max(0, staged.Get(key, 0) + 1) : fallback;
    }

    /// <summary>
    /// Sizes each feature's cost levels to its staged max level. A missing level starts as a copy of the
    /// one below, so a new level is never free. Levels above the max that the file has are kept, unseen,
    /// so lowering the max loses nothing; ones added earlier in this session are dropped again, so
    /// raising and then lowering the max is not an edit.
    /// </summary>
    internal static void FitUpgradeLevels(StagedConfig staged, UpgradeCostsValue value) {
        EnchantingUpgradeCosts fileCosts = EnchantingTableUpgrades.Config?.UpgradeCosts;
        foreach (KeyValuePair<EnchantingFeature, List<List<CostEntry>>> pair in value.Levels) {
            List<List<CostEntry>> levels = pair.Value;
            int shown = UpgradeLevelsShown(staged, pair.Key, levels.Count);
            int inFile = fileCosts == null ? 0 : CostLevels(fileCosts, pair.Key)?.Count ?? 0;
            int keep = Math.Max(shown, inFile);
            if (levels.Count > keep) { levels.RemoveRange(keep, levels.Count - keep); }
            while (levels.Count < shown) {
                List<CostEntry> below = levels.Count > 0 ? levels[levels.Count - 1] : null;
                levels.Add(below?.Select(entry => entry.Clone()).ToList() ?? new List<CostEntry>());
            }
        }
    }

    private static UpgradeCostsValue ReadUpgradeCosts(EnchantingUpgradesConfig config) {
        EnchantingUpgradeCosts costs = config?.UpgradeCosts;
        if (costs == null) { return null; }
        UpgradeCostsValue value = new UpgradeCostsValue();
        foreach (EnchantingFeature feature in (EnchantingFeature[])Enum.GetValues(typeof(EnchantingFeature))) {
            List<List<ItemAmount>> levels = CostLevels(costs, feature);
            if (levels == null) { continue; }
            value.Levels[feature] = levels
                .Select(level => level?.Select(item => new CostEntry { Item = item?.Item ?? "", Amount = item?.Amount ?? 1 }).ToList())
                .ToList();
        }
        return value;
    }

    private static List<List<ItemAmount>> CostLevels(EnchantingUpgradeCosts costs, EnchantingFeature feature) {
        return feature switch {
            EnchantingFeature.Sacrifice => costs.Sacrifice,
            EnchantingFeature.ConvertMaterials => costs.ConvertMaterials,
            EnchantingFeature.Enchant => costs.Enchant,
            EnchantingFeature.Augment => costs.Augment,
            EnchantingFeature.Disenchant => costs.Disenchant,
            EnchantingFeature.Rune => costs.Rune,
            EnchantingFeature.Storage => costs.Storage,
            _ => null
        };
    }

    private static void BountyTier(string tier, Func<int?> readMin, Func<int?> readMax, Func<float?> readHealth) {
        string minKey = $"json:adventuredata:Bounties.{tier}MinLevel";
        string maxKey = $"json:adventuredata:Bounties.{tier}MaxLevel";
        Func<StagedConfig, string> order = staged => staged.Get(minKey, 1) > staged.Get(maxKey, 1)
            ? $"{tier} bounty Min Level must not exceed Max Level." : null;
        JsonInt(minKey, "adventuredata.json", $"{tier} Min Level", 1, 10, readMin,
            (root, value) => JsonConfigEdits.SetPath(root, $"Bounties.{tier}MinLevel", value),
            $"Lowest star level a {tier} bounty target can spawn at.", order);
        JsonInt(maxKey, "adventuredata.json", $"{tier} Max Level", 1, 10, readMax,
            (root, value) => JsonConfigEdits.SetPath(root, $"Bounties.{tier}MaxLevel", value),
            $"Highest star level a {tier} bounty target can spawn at.", order);
        JsonFloat($"json:adventuredata:Bounties.{tier}HealthMultiplier", "adventuredata.json", $"{tier} Health Multiplier", 0.1f, 10f, 0.1f, readHealth,
            (root, value) => JsonConfigEdits.SetPath(root, $"Bounties.{tier}HealthMultiplier", value),
            $"Multiplies the health of a {tier} bounty target.");
    }

    // --- Effect configs: one editor over every loaded effect definition with a Config ---
    // One staged value, two files: a slot per file feeds the same key, and each slot only counts
    // as dirty (and only writes) when the effects that belong to its file changed.

    internal const string EffectConfigsKey = "json:magiceffects:EffectConfigs";

    private static void EffectConfigs() {
        const string key = EffectConfigsKey;
        JsonSlot shard = JsonSlotFor(key, "shardstones.json", () => EffectConfigTables.Read(), null);
        shard.Default = (current, shipped) => current is EffectConfigsValue value ? EffectConfigTables.Shipped(value, shipped) : null;
        shard.Changed = (current, before) => ((EffectConfigsValue)current).PartChanged(before as EffectConfigsValue, EffectSource.Shard);
        shard.WriteWithBaseline = (root, current, before) => WriteEffectConfigs(root, (EffectConfigsValue)current, before as EffectConfigsValue, EffectSource.Shard);

        JsonSlot overhaul = JsonSlotFor(key, BalancePreset.MagicEffectsFile, () => EffectConfigTables.Read(), null);
        overhaul.Changed = (current, before) => ((EffectConfigsValue)current).PartChanged(before as EffectConfigsValue, EffectSource.Overhaul);
        overhaul.WriteWithBaseline = (root, current, before) => WriteEffectConfigs(root, (EffectConfigsValue)current, before as EffectConfigsValue, EffectSource.Overhaul);

        Add(new Binding {
            Key = key, DisplayName = "Effect Configs", Kind = BindingKind.EffectConfigs, Scope = BindingScope.Json, EditsFiles = true,
            Options = ItemPrefabNames,
            Tooltip = () => QuickConfigTooltip.Text("Effect configs",
                "The per-effect tunables (Config block) of every loaded magic effect. Overhaul effects are " +
                "written to baseconfig/magiceffects.json; a Balance preset rewrites that file from its " +
                "template and resets them. Shard effects are written to baseconfig/shardstones.json, into " +
                "every shard slot that carries the effect, so all copies stay identical. Effects registered " +
                "by other mods or synthesized in code are shown read-only. Riches' keys are item prefab names " +
                "and its values the cost of each; every other effect has a fixed set of keys."),
            Get = staged => staged.Get(key, new EffectConfigsValue()),
            Set = (staged, value) => { if (value is EffectConfigsValue table) { staged.Set(key, table); } },
            Validate = staged => {
                EffectConfigsValue table = staged.Get(key, new EffectConfigsValue());
                foreach (EffectConfigSet set in table.Effects.Values) {
                    if (set.Source == EffectSource.ReadOnly) { continue; }
                    HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (EffectConfigEntry entry in set.Entries) {
                        if (float.IsNaN(entry.Value) || float.IsInfinity(entry.Value)) { return $"{set.DisplayName}: '{entry.Key}' must be a number."; }
                        if (set.OpenKeys == false) { continue; }
                        if (string.IsNullOrWhiteSpace(entry.Key)) { return $"{set.DisplayName}: every row needs an item name."; }
                        if (seen.Add(entry.Key.Trim()) == false) { return $"{set.DisplayName} lists '{entry.Key}' more than once."; }
                        if (entry.Value < 1f) { return $"{set.DisplayName}: the value of '{entry.Key}' must be at least 1."; }
                    }
                }
                return null;
            }
        });
    }

    private static void WriteEffectConfigs(JObject root, EffectConfigsValue current, EffectConfigsValue before, EffectSource source) {
        foreach (string type in current.Order) {
            EffectConfigSet set = current.Get(type);
            if (set == null || set.Source != source || EffectConfigSet.SameEntries(set, before?.Get(type))) { continue; }
            if (source == EffectSource.Shard) {
                JsonConfigEdits.SetShardEffectConfigs(root, type, set.Entries);
            } else {
                JsonConfigEdits.ReplaceEffectConfig(root, type, set.Entries);
            }
        }
    }

    // --- Bounty targets (adventuredata.json Bounties.Targets) ---

    private static void Bounties() {
        const string key = "json:adventuredata:Bounties.Targets";
        JsonSlot slot = JsonSlotFor(key, "adventuredata.json", () => BountyTables.Read(),
            (root, value) => JsonConfigEdits.SetBountyTargets(root, (BountiesValue)value));
        slot.Default = (currentValue, shipped) => {
            BountiesValue defaults = BountyTables.Read(shipped.Root("adventuredata.json"));
            if (defaults == null || currentValue is BountiesValue current == false) { return null; }
            BountiesValue value = current.Clone();
            foreach (string biome in defaults.FileBiomes) {
                value.Entries[biome] = defaults.Get(biome).Select(entry => entry.Clone()).ToList();
                if (value.BiomeOrder.Contains(biome) == false) { value.BiomeOrder.Add(biome); }
            }
            return value;
        };
        Add(new Binding {
            Key = key, DisplayName = "Bounty Targets", Kind = BindingKind.Bounties, Scope = BindingScope.Json, EditsFiles = true,
            Options = BountyTables.MonsterNames,
            Tooltip = () => QuickConfigTooltip.Text("adventuredata.json: Bounties.Targets",
                "The creatures the bounty board can post per biome, with the rewards paid per kill: iron " +
                "bounty tokens, gold bounty tokens and coins. A target's Adds (the minions that spawn " +
                "with it) and any other property are file-only and are preserved when the row is edited. " +
                "The monster picker lists every creature of a loaded world; on the main menu it only " +
                "knows the creatures the config files already name."),
            Get = staged => staged.Get(key, new BountiesValue()),
            Set = (staged, value) => { if (value is BountiesValue table) { staged.Set(key, table); } },
            Validate = staged => {
                BountiesValue table = staged.Get(key, new BountiesValue());
                foreach (string biome in table.BiomeOrder) {
                    foreach (BountyEntry entry in table.Get(biome) ?? new List<BountyEntry>()) {
                        string where = $"{BiomeDisplayName(biome)} bounty";
                        if (string.IsNullOrWhiteSpace(entry.TargetID)) { return $"{where}: every row needs a creature name."; }
                        if (entry.RewardIron < 0 || entry.RewardGold < 0 || entry.RewardCoins < 0) { return $"{where} '{entry.TargetID}': rewards must be 0 or more."; }
                    }
                }
                return null;
            }
        });
    }

    private static void Readout(string key, Func<StagedConfig, string> text, string name, string description) {
        Add(new Binding {
            Key = key, DisplayName = "", Kind = BindingKind.Readout, Scope = BindingScope.Action, Readout = text,
            Tooltip = () => QuickConfigTooltip.Text(name, description)
        });
    }

    // --- Treasure map biome costs (adventuredata.json TreasureMap.BiomeInfo) ---

    private static void BiomeCosts() {
        const string key = "json:adventuredata:TreasureMap.BiomeInfo";
        JsonSlot slot = JsonSlotFor(key, "adventuredata.json",
            () => {
                List<TreasureMapBiomeInfoConfig> infos = AdventureDataManager.Config?.TreasureMap?.BiomeInfo;
                if (infos == null) { return null; }
                return infos
                    .Where(info => info != null && string.IsNullOrEmpty(info.Biome) == false)
                    .Select(info => new BiomeCostEntry { Biome = info.Biome, Cost = info.Cost, ForestTokens = info.ForestTokens })
                    .ToList();
            },
            (root, value) => JsonConfigEdits.SetBiomeCosts(root, (List<BiomeCostEntry>)value));
        slot.Default = (currentValue, shipped) => {
            if (currentValue is List<BiomeCostEntry> current == false || shipped.Token("adventuredata.json", "TreasureMap.BiomeInfo") is JArray infos == false) { return null; }
            List<BiomeCostEntry> value = current.Select(entry => entry.Clone()).ToList();
            foreach (BiomeCostEntry entry in value) {
                JObject info = infos.OfType<JObject>().FirstOrDefault(token => string.Equals((string)token["Biome"], entry.Biome, StringComparison.OrdinalIgnoreCase));
                if (info == null) { continue; }
                entry.Cost = (int?)info["Cost"] ?? entry.Cost;
                entry.ForestTokens = (int?)info["ForestTokens"] ?? entry.ForestTokens;
            }
            return value;
        };
        Add(new Binding {
            Key = key, DisplayName = "Treasure Map Costs", Kind = BindingKind.BiomeCosts, Scope = BindingScope.Json, EditsFiles = true,
            Tooltip = () => QuickConfigTooltip.Text("adventuredata.json: TreasureMap.BiomeInfo",
                "What Haldor charges for a treasure map into each biome. Cost is in coins; -1 means no map " +
                "is offered for that biome. Forest tokens is the token price paid alongside the coins. The " +
                "biome list and each biome's radius band are file-only: edit adventuredata.json to change them."),
            Get = staged => staged.Get(key, new List<BiomeCostEntry>()),
            Set = (staged, value) => { if (value is List<BiomeCostEntry> list) { staged.Set(key, list); } },
            Validate = staged => {
                foreach (BiomeCostEntry entry in staged.Get(key, new List<BiomeCostEntry>())) {
                    if (entry.Cost < -1) { return $"Treasure map cost for {BiomeDisplayName(entry.Biome)} must be -1 (not offered) or more."; }
                    if (entry.ForestTokens < 0) { return $"Treasure map forest tokens for {BiomeDisplayName(entry.Biome)} must be 0 or more."; }
                }
                return null;
            }
        });
    }

    // --- Biome drop tables (loottables.json LootTables: Drops + per-entry Rarity weights) ---

    private static void BiomeDrops() {
        const string key = "json:loottables:LootTables";
        JsonSlot slot = JsonSlotFor(key, "loottables.json", BiomeDropTables.Read, null);
        // A table the shipped file lacks (a creature another mod added) keeps its values.
        slot.Default = (currentValue, shipped) => {
            LootConfig config = shipped.Typed<LootConfig>("loottables.json");
            if (config?.LootTables == null || currentValue is List<BiomeDropRow> current == false) { return null; }
            foreach (LootTable table in config.LootTables) {
                if (table != null) { LootTableMigration.Normalize(table); }
            }
            Dictionary<string, BiomeDropRow> defaults = new Dictionary<string, BiomeDropRow>(StringComparer.Ordinal);
            foreach (BiomeDropRow row in BiomeDropTables.Read(config) ?? new List<BiomeDropRow>()) { defaults[row.Id] = row; }
            List<BiomeDropRow> value = current.Select(row => row.Clone()).ToList();
            foreach (BiomeDropRow row in value) {
                if (defaults.TryGetValue(row.Id, out BiomeDropRow shippedRow) == false) { continue; }
                row.Amount = WeightTable.Clone(shippedRow.Amount) ?? row.Amount;
                row.Rarity = WeightTable.Clone(shippedRow.Rarity) ?? row.Rarity;
            }
            return value;
        };
        slot.WriteWithBaseline = (root, stagedValue, baselineValue) => {
            List<BiomeDropRow> current = (List<BiomeDropRow>)stagedValue;
            Dictionary<string, BiomeDropRow> before = new Dictionary<string, BiomeDropRow>(StringComparer.Ordinal);
            foreach (BiomeDropRow row in baselineValue as List<BiomeDropRow> ?? new List<BiomeDropRow>()) { before[row.Id] = row; }
            foreach (BiomeDropRow row in current) {
                before.TryGetValue(row.Id, out BiomeDropRow was);
                bool amount = row.Amount != null && row.Amount.Count > 0 && WeightTable.Same(row.Amount, was?.Amount) == false;
                bool rarity = row.Rarity != null && WeightTable.Same(row.Rarity, was?.Rarity) == false;
                if (amount || rarity) {
                    JsonConfigEdits.SetBiomeDrops(root, row,
                        amount ? WeightTable.Rebalance(row.Amount, was?.Amount) : null,
                        rarity ? WeightTable.Rebalance(row.Rarity, was?.Rarity) : null);
                }
            }
        };
        Add(new Binding {
            Key = key, DisplayName = "Biome Drops", Kind = BindingKind.BiomeDrops, Scope = BindingScope.Json, EditsFiles = true,
            Tooltip = () => QuickConfigTooltip.Text("loottables.json: LootTables",
                "Per biome and loot table, how many magic items a creature or chest drops and at which rarity. " +
                "Drop amount lists each possible number of items with its chance in percent (0 items 95%, 1 item 5%); " +
                $"Rarity gives the chance of each rarity, Magic to {Rarities.Highest}. Each should add up to 100: on " +
                "Save, the chances you did not change are raised or lowered to make up the rest. Creatures inherit " +
                "their tier template (Tier1Mob, Tier3EliteMob...), so editing the template changes every creature " +
                "listed beside it. A table marked (mixed) has entries with different rarity weights: the first is " +
                "shown, and saving applies it to all of that table's entries. Entries whose rarity picks the item, " +
                "such as shardstones and enchanting materials, keep their own. Auto Add Equipment's loot-list " +
                "validation rewrites loottables.json but keeps these values."),
            Get = staged => staged.Get(key, new List<BiomeDropRow>()),
            Set = (staged, value) => { if (value is List<BiomeDropRow> rows) { staged.Set(key, rows); } },
            Validate = staged => {
                foreach (BiomeDropRow row in staged.Get(key, new List<BiomeDropRow>())) {
                    string error = (row.Amount != null && row.Amount.Count > 0
                            ? WeightTable.Validate(row.Amount, 0, int.MaxValue, $"{row.Label}: drop amount") : null)
                        ?? (row.Rarity != null ? WeightTable.Validate(row.Rarity, 0, Rarities.Count - 1, $"{row.Label}: rarity") : null);
                    if (error != null) { return error; }
                }
                return null;
            }
        });
    }

    /// <summary>The registry's display name for a biome string, else the string itself.</summary>
    internal static string BiomeDisplayName(string raw) {
        if (string.IsNullOrEmpty(raw)) { return ""; }
        try {
            if (BiomeDataManager.TryResolve(raw, out Heightmap.Biome biome)) {
                string localized = QuickConfigureTool.L(BiomeDataManager.GetLocalizationToken(biome));
                if (string.IsNullOrEmpty(localized) == false && localized.StartsWith("[") == false) { return localized; }
            }
        } catch (Exception) {
            // The registry may not be loaded yet; the raw name is still meaningful.
        }
        return raw;
    }

    // --- Item categories (iteminfo.json ItemInfo[].ItemsByBoss) ---

    private static void ItemCategories() {
        const string key = "json:iteminfo:ItemInfo";
        JsonSlot slot = JsonSlotFor(key, "iteminfo.json", () => ReadItemCategories(GatedItemTypeHelper.GatedConfig), null);
        // The shipped categories' items; a category the shipped file lacks keeps its own.
        slot.Default = (currentValue, shipped) => {
            ItemCategoriesValue defaults = ReadItemCategories(shipped.Typed<ItemInfoConfig>("iteminfo.json"));
            if (defaults == null || currentValue is ItemCategoriesValue current == false) { return null; }
            ItemCategoriesValue value = current.Clone();
            foreach (string category in value.Categories) {
                List<ItemByBossEntry> entries = defaults.Get(category);
                if (entries != null) { value.Entries[category] = entries; }
            }
            return value;
        };
        slot.WriteWithBaseline = (root, stagedValue, baselineValue) => {
            ItemCategoriesValue current = (ItemCategoriesValue)stagedValue;
            ItemCategoriesValue before = baselineValue as ItemCategoriesValue;
            foreach (string category in current.Categories) {
                List<ItemByBossEntry> entries = current.Get(category);
                if (before != null && ItemCategoriesValue.SameList(entries, before.Get(category))) { continue; }
                JsonConfigEdits.SetItemsByBoss(root, category, entries);
            }
        };
        Add(new Binding {
            Key = key, DisplayName = "Item Categories", Kind = BindingKind.ItemCategories, Scope = BindingScope.Json, EditsFiles = true,
            Options = ItemPrefabNames,
            KeyOptions = BossKeyOptions,
            Tooltip = () => QuickConfigTooltip.Text("iteminfo.json: ItemInfo",
                "Which items belong to each equipment category and which boss must be dead before they can " +
                "drop (Item Drop Limits). 'none' means ungated. Note: with Auto Add Equipment on, every " +
                "item the game still has is re-added to its category on the next world load, so removing " +
                "one only sticks while Auto Add Equipment is off, or for items that no longer exist."),
            Get = staged => staged.Get(key, new ItemCategoriesValue()),
            Set = (staged, value) => { if (value is ItemCategoriesValue table) { staged.Set(key, table); } },
            Validate = staged => {
                ItemCategoriesValue table = staged.Get(key, new ItemCategoriesValue());
                foreach (string category in table.Categories) {
                    HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (ItemByBossEntry entry in table.Get(category) ?? new List<ItemByBossEntry>()) {
                        if (string.IsNullOrWhiteSpace(entry.Boss)) { return $"{category}: every item needs a boss key ('none' for ungated)."; }
                        if (string.IsNullOrWhiteSpace(entry.Item)) { return $"{category}: every row needs an item name."; }
                        if (seen.Add(entry.Item.Trim()) == false) { return $"{category} lists '{entry.Item}' more than once."; }
                    }
                }
                return null;
            }
        });
    }

    private static ItemCategoriesValue ReadItemCategories(ItemInfoConfig config) {
        List<ItemTypeInfo> infos = config?.ItemInfo;
        if (infos == null) { return null; }
        ItemCategoriesValue value = new ItemCategoriesValue();
        foreach (ItemTypeInfo info in infos) {
            if (info == null || string.IsNullOrEmpty(info.Type) || value.Entries.ContainsKey(info.Type)) { continue; }
            List<ItemByBossEntry> entries = new List<ItemByBossEntry>();
            if (info.ItemsByBoss != null) {
                foreach (KeyValuePair<string, List<string>> pair in info.ItemsByBoss) {
                    if (pair.Value == null) { continue; }
                    foreach (string item in pair.Value) {
                        entries.Add(new ItemByBossEntry { Boss = pair.Key, Item = item ?? "" });
                    }
                }
            }
            value.Categories.Add(info.Type);
            value.Entries[info.Type] = entries;
        }
        return value;
    }

    // "none", then the boss keys the biome registry knows in progression order, then any key the file
    // already uses that the registry does not know (a custom biome's key, or a typo worth keeping visible).
    private static IList<string> BossKeyOptions(StagedConfig staged) {
        List<string> keys = new List<string> { "none" };
        try {
            foreach (string bossKey in BiomeDataManager.BossKeysInOrder) {
                if (string.IsNullOrEmpty(bossKey) == false && keys.Contains(bossKey) == false) { keys.Add(bossKey); }
            }
        } catch (Exception) {
            // Registry not loaded: the file's own keys below still make a usable list.
        }
        ItemCategoriesValue table = staged?.Get("json:iteminfo:ItemInfo", (ItemCategoriesValue)null);
        if (table != null) {
            foreach (List<ItemByBossEntry> entries in table.Entries.Values) {
                foreach (ItemByBossEntry entry in entries) {
                    if (string.IsNullOrEmpty(entry.Boss) == false && keys.Contains(entry.Boss) == false) { keys.Add(entry.Boss); }
                }
            }
        }
        return keys;
    }

    // ------------------------------------------------------------------------------------------------
    //  Live readers
    // ------------------------------------------------------------------------------------------------

    private static float[][] GetTable(MagicEffectsCountConfig config, ItemRarity rarity) {
        if (config == null) { return null; }
        return rarity switch {
            ItemRarity.Magic => config.Magic,
            ItemRarity.Rare => config.Rare,
            ItemRarity.Epic => config.Epic,
            ItemRarity.Legendary => config.Legendary,
            ItemRarity.Mythic => config.Mythic,
            ItemRarity.Ancient => config.Ancient,
            _ => null
        };
    }

    private static float[][] GetTable(SocketCountsConfig config, ItemRarity rarity) {
        if (config == null) { return null; }
        return rarity switch {
            ItemRarity.Magic => config.Magic,
            ItemRarity.Rare => config.Rare,
            ItemRarity.Epic => config.Epic,
            ItemRarity.Legendary => config.Legendary,
            ItemRarity.Mythic => config.Mythic,
            ItemRarity.Ancient => config.Ancient,
            _ => null
        };
    }

    private static int? FeatureLevel(Dictionary<EnchantingFeature, int> levels, EnchantingFeature feature) {
        if (levels == null) { return null; }
        return levels.TryGetValue(feature, out int level) ? level : (int?)null;
    }

    private static string FeatureLevelError(StagedConfig staged, EnchantingFeature feature, string defaultKey, string maxKey) {
        int defaultLevel = staged.Get(defaultKey, 0);
        int maxLevel = staged.Get(maxKey, 0);
        if (defaultLevel > maxLevel) { return $"{feature}: the default level must not exceed the max level."; }
        return null;
    }

    private static float? ShardGlobal(string name) {
        Dictionary<string, float> values = Shards.GetCFG()?.Global?.Values;
        return values != null && values.TryGetValue(name, out float value) ? value : (float?)null;
    }

    private static string DropMixReadout(StagedConfig staged) {
        float items = staged.Get("ItemDropRatio", 0f);
        float shards = staged.Get("ShardStoneDropRatio", 0f);
        float unidentified = staged.Get("ItemsUnidentifiedDropRatio", 0f);
        float materials = staged.Get("MaterialsDropRatio", 0f);
        float total = items + shards + unidentified + materials;
        if (total <= 0f) { return "All four weights are 0: every drop falls back to a normal item."; }
        return $"Of each drop: {Percent(items / total)} items, {Percent(shards / total)} shard stones, " +
            $"{Percent(unidentified / total)} unidentified, {Percent(materials / total)} materials";
    }

    private static string Percent(float share) => Mathf.RoundToInt(share * 100f) + "%";

    // ------------------------------------------------------------------------------------------------
    //  Option lists
    // ------------------------------------------------------------------------------------------------

    private static List<string> hotkeyNames;

    // The names Input.GetKey(string) accepts, which is how the ability bar polls its hotkeys. They are
    // not the KeyCode member names ("left shift", not "LeftShift"), so the list is found by asking.
    internal static IList<string> HotkeyNames() {
        if (hotkeyNames != null) { return hotkeyNames; }
        HashSet<string> candidates = new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in Enum.GetNames(typeof(KeyCode))) { candidates.Add(name.ToLowerInvariant()); }
        string[] extra = {
            "left shift", "right shift", "left ctrl", "right ctrl", "left alt", "right alt", "left cmd", "right cmd",
            "page up", "page down", "caps lock", "num lock", "scroll lock", "enter", "return", "tab", "backspace",
            "delete", "insert", "home", "end", "up", "down", "left", "right", "space", "escape",
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
            "[0]", "[1]", "[2]", "[3]", "[4]", "[5]", "[6]", "[7]", "[8]", "[9]", "[+]", "[-]", "[*]", "[/]", "[.]", "[=]",
            "-", "=", "[", "]", ";", "'", ",", ".", "/", "\\", "`",
            "mouse 0", "mouse 1", "mouse 2", "mouse 3", "mouse 4", "mouse 5", "mouse 6"
        };
        foreach (string name in extra) { candidates.Add(name); }
        for (int i = 0; i < 20; i++) { candidates.Add($"joystick button {i}"); }
        hotkeyNames = candidates.Where(IsValidHotkey).OrderBy(name => name.Length).ThenBy(name => name, StringComparer.Ordinal).ToList();
        return hotkeyNames;
    }

    internal static bool IsValidHotkey(string name) {
        if (string.IsNullOrWhiteSpace(name)) { return false; }
        try {
            Input.GetKey(name);
            return true;
        } catch (Exception) {
            return false;
        }
    }

    // ObjectDB item prefab names in-world; null on the main menu, where the field is free text.
    internal static IList<string> ItemPrefabNames() {
        if (ObjectDB.instance == null || ObjectDB.instance.m_items == null) { return null; }
        return ObjectDB.instance.m_items
            .Where(go => go != null)
            .Select(go => go.name)
            .Distinct()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ------------------------------------------------------------------------------------------------
    //  Value helpers
    // ------------------------------------------------------------------------------------------------

    // "json:adventuredata:Gamble.GamblesCount" -> "Gamble.GamblesCount", the dotted path inside the file.
    private static string JsonPath(string key) {
        string[] parts = key.Split(new[] { ':' }, 3);
        return parts.Length == 3 ? parts[2] : key;
    }

    private static string JsonTooltipKey(string key) {
        // "json:adventuredata:Gamble.GamblesCount" -> "adventuredata.json: Gamble.GamblesCount"
        string[] parts = key.Split(new[] { ':' }, 3);
        return parts.Length == 3 ? $"{parts[1]}.json: {parts[2]}" : key;
    }

    internal static float ToFloat(object value, float fallback) {
        try {
            return value switch {
                float f => f,
                double d => (float)d,
                int i => i,
                // A comma is taken as the decimal point: the Value boxes accept both, and a German
                // keyboard types the comma.
                string s => float.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : fallback,
                _ => Convert.ToSingle(value, CultureInfo.InvariantCulture)
            };
        } catch (Exception) {
            return fallback;
        }
    }

    internal static int ToInt(object value, int fallback) {
        try {
            return value switch {
                int i => i,
                float f => Mathf.RoundToInt(f),
                double d => (int)Math.Round(d),
                string s => int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                    ? parsed
                    : float.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFloat) ? Mathf.RoundToInt(parsedFloat) : fallback,
                _ => Convert.ToInt32(value, CultureInfo.InvariantCulture)
            };
        } catch (Exception) {
            return fallback;
        }
    }

    /// <summary>Clamps and rounds a slider value to its step, then to the step's decimals so the .cfg round-trips exactly.</summary>
    internal static float Snap(float value, float min, float max, float step) {
        if (float.IsNaN(value) || float.IsInfinity(value)) { value = min; }
        if (step > 0f) {
            value = Mathf.Round(value / step) * step;
            value = (float)Math.Round(value, StepDecimals(step));
        }
        return Mathf.Clamp(value, min, max);
    }

    internal static int StepDecimals(float step) {
        if (step <= 0f) { return 3; }
        int decimals = 0;
        double s = step;
        while (decimals < 6 && Math.Abs(s - Math.Round(s)) > 1e-6) {
            s *= 10;
            decimals++;
        }
        return decimals;
    }

    internal static string FormatFloat(float value, float step) {
        return value.ToString("F" + StepDecimals(step), CultureInfo.InvariantCulture);
    }

    private static string NormalizeColor(string text) {
        string trimmed = (text ?? "").Trim();
        if (trimmed.StartsWith("#")) { return trimmed; }
        foreach (string name in EpicLoot.MagicItemColors.Keys) {
            if (string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase)) { return name; }
        }
        return trimmed;
    }

    private static string ColorError(string value, bool hexOnly, string label) {
        if (string.IsNullOrWhiteSpace(value)) { return $"{label} must not be empty."; }
        if (value.StartsWith("#")) {
            return ColorUtility.TryParseHtmlString(value, out _) ? null : $"{label}: '{value}' is not a valid #hex colour.";
        }
        if (hexOnly) { return $"{label} must be a #hex colour, for example #26ffff."; }
        if (EpicLoot.MagicItemColors.ContainsKey(value)) { return null; }
        return $"{label}: '{value}' is neither a colour name ({string.Join(", ", EpicLoot.MagicItemColors.Keys)}) nor a #hex colour.";
    }

    /// <summary>The Color a rarity or set-item colour string stands for, or null when it is not valid.</summary>
    internal static Color? ParseColor(string value) {
        if (string.IsNullOrWhiteSpace(value)) { return null; }
        string html = value.StartsWith("#") ? value : EpicLoot.MagicItemColors.TryGetValue(value, out string hex) ? hex : null;
        if (html != null && ColorUtility.TryParseHtmlString(html, out Color color)) { return color; }
        return null;
    }
}
