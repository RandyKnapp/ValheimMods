using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Generates the Quick Configure prefabs under Assets/EpicLoot/Prefabs/UI/QuickConfigure.
///
/// The contract with the mod code (row keys, widget child names, shell/page structure) is documented in
/// EpicLoot/src/QuickConfig/README.md; keep the two in step. The generator exists so the initial set of
/// prefabs is reproducible: "Build Row Templates" and "Build Shell And Overlays" overwrite their outputs,
/// "Build Missing Pages" only creates pages that do not exist yet so hand edits in the editor survive,
/// and "Rebuild Selected Pages" / "Rebuild All Pages" are the deliberate resets.
///
/// The prefabs carry no EpicLoot components, by design: a row is identified by its GameObject name and a
/// page by its root name, so the prefabs never depend on the mod assembly (and they open cleanly in an
/// editor without the mod DLLs). Vanilla's Localize (assembly_guiutils) is resolved by name so this file
/// has no compile dependency on the game DLLs either.
/// </summary>
public static class QuickConfigPrefabBuilder
{
    private const string Root = "Assets/EpicLoot/Prefabs/UI/QuickConfigure";
    private const string RowsDir = Root + "/Rows";
    private const string PagesDir = Root + "/Pages";

    private const string SpriteButton = "Assets/EpicLoot/Sprites/Adventure/button.png";
    private const string SpriteButtonHighlight = "Assets/EpicLoot/Sprites/Adventure/button_highlight.png";
    private const string SpriteButtonPressed = "Assets/EpicLoot/Sprites/Adventure/button_pressed.png";
    private const string SpriteButtonDisabled = "Assets/EpicLoot/Sprites/Adventure/button_disabled.png";
    private const string SpritePanel = "Assets/EpicLoot/Sprites/Adventure/woodpanel_large.png";
    private const string SpriteField = "Assets/EpicLoot/Sprites/Adventure/item_background.png";
    private const string SpriteCheck = "Assets/EpicLoot/Sprites/Enchanting/CheckMark.png";
    private const string SpriteInfo = "Assets/EpicLoot/Sprites/Adventure/woodpanel_info.png";

    private const float PanelW = 900f;
    private const float PanelH = 760f;
    private const float RowH = 34f;
    private const float SubRowH = 26f;
    private const float LabelW = 200f;
    private const float SmallButtonW = 40f;

    // The text palette of Star Level System's Quick Configure, which builds its panels with Jotunn's
    // GUIManager: these are GUIManager.ValheimBeige/ValheimYellow/ValheimOrange, copied because this
    // editor assembly does not reference Jotunn. Beige for setting labels and notes; yellow for titles,
    // section and column headers; orange for button captions, the names of list entries and warnings;
    // white for typed text with a grey placeholder, as Jotunn's input fields have.
    private static readonly Color Beige = new Color(0.8529f, 0.725f, 0.5331f, 1f);
    private static readonly Color Yellow = new Color(1f, 0.889f, 0f, 1f);
    private static readonly Color Orange = new Color(1f, 0.631f, 0.235f, 1f);
    private static readonly Color FieldText = Color.white;
    private static readonly Color Placeholder = Color.grey;
    private static readonly Color Dim = new Color(0f, 0f, 0f, 0.45f);
    // item_background.png is a white sliced sprite meant to be tinted; untinted it reads as a white box.
    private static readonly Color DarkField = new Color(0.10f, 0.08f, 0.06f, 0.85f);

    // ------------------------------------------------------------------------------------------------
    //  Menu
    // ------------------------------------------------------------------------------------------------

    // Every row template and its builder, in build order. Pages may nest any of these.
    private static readonly (string Name, Action Build)[] RowTemplates =
    {
        ("Row_Toggle", BuildToggleRow),
        ("Row_Slider", BuildSliderRow),
        ("Row_Cycle", BuildCycleRow),
        ("Row_Picker", BuildPickerRow),
        ("Row_TextField", BuildTextFieldRow),
        ("Row_Flags", BuildFlagsRow),
        ("Row_Button", BuildButtonRow),
        ("Row_Header", BuildHeaderRow),
        ("Row_Text", BuildTextRow),
        ("Row_Color", BuildColorRow),
        ("Row_ListEditor", BuildListEditorRow),
        ("Row_BiomeCosts", BuildBiomeCostsRow),
        ("Row_ItemCategories", BuildItemCategoriesRow),
        ("Row_BiomeDrops", BuildBiomeDropsRow),
        ("Row_EffectConfigs", BuildEffectConfigsRow),
        ("Row_Bounties", BuildBountiesRow),
        ("Row_RarityCounts", BuildRarityCountsRow),
        ("Row_UpgradeCosts", BuildUpgradeCostsRow),
    };

    [MenuItem("Mod/Quick Configure/Build Row Templates")]
    public static void BuildRowTemplates()
    {
        BuildRowTemplates(RowTemplates.Select(template => template.Name).ToArray());
    }

    // Writes only the named templates, so a new or reworked one does not rewrite the others.
    public static void BuildRowTemplates(params string[] names)
    {
        EnsureFolders();
        HashSet<string> wanted = new HashSet<string>(names ?? new string[0]);
        int written = 0;
        foreach ((string name, Action build) in RowTemplates)
        {
            if (!wanted.Contains(name)) { continue; }
            build();
            written++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[QuickConfig] Wrote " + written + " row template(s) to " + RowsDir);
    }

    [MenuItem("Mod/Quick Configure/Build Shell And Overlays")]
    public static void BuildShellAndOverlays()
    {
        EnsureFolders();
        BuildPanel();
        BuildPicker();
        BuildConfirm();
        AssetDatabase.SaveAssets();
        Debug.Log("[QuickConfig] Shell and overlays written to " + Root);
    }

    [MenuItem("Mod/Quick Configure/Build Missing Pages")]
    public static void BuildMissingPages()
    {
        BuildPages(overwrite: false);
    }

    [MenuItem("Mod/Quick Configure/Rebuild All Pages (overwrite)")]
    public static void RebuildAllPages()
    {
        BuildPages(overwrite: true);
    }

    // Select page prefabs in the Project window first. Only those are reset to this script's table.
    [MenuItem("Mod/Quick Configure/Rebuild Selected Pages (overwrite)")]
    public static void RebuildSelectedPages()
    {
        RebuildPages(Selection.objects.OfType<GameObject>().Select(go => go.name).ToArray());
    }

    public static void RebuildPages(params string[] pageNames)
    {
        HashSet<string> only = new HashSet<string>(pageNames ?? new string[0]);
        only.IntersectWith(Pages.Select(page => page.Name));
        if (only.Count == 0)
        {
            Debug.LogWarning("[QuickConfig] No Quick Configure page selected; select page prefabs in " + PagesDir + " first.");
            return;
        }
        BuildPages(overwrite: true, only);
    }

    [MenuItem("Mod/Quick Configure/Build Everything")]
    public static void BuildEverything()
    {
        BuildRowTemplates();
        BuildShellAndOverlays();
        BuildMissingPages();
    }

    // ------------------------------------------------------------------------------------------------
    //  Page content. Keys follow EpicLoot/src/QuickConfig/README.md; labels are plain English the
    //  maintainer edits in place, or $tokens the code localizes.
    // ------------------------------------------------------------------------------------------------

    private class RowDef
    {
        public string Template;
        public string Key;
        public string Label;
        public float Height;
        public string ButtonText;
        public RowDef[] Children;   // a Row_Group: these rows side by side
        public float Width;         // > 0: fixed width as a Row_Group cell
        public bool Flexible;       // a Row_Group cell that shares the remaining width
        public string[] Hidden;     // children of the row switched off (a group cell showing only its Value)
        public float LabelWidth;    // > 0: the Label's fixed width
        public int LabelSize;       // > 0: label font size, centred vertically
        public bool AutoHeight;     // the row takes the height its content asks for

        public RowDef(string template, string key, string label, float height = 0f, string buttonText = null)
        {
            Template = template;
            Key = key;
            Label = label;
            Height = height;
            ButtonText = buttonText;
        }
    }

    private class PageDef
    {
        public string Name;
        public string Title;
        public RowDef[] Left;
        public RowDef[] Right;    // null = single column
        public RowDef[] Bottom;   // optional full-width "Column" under Left/Right
        public float BottomShare; // fraction of the page height the bottom container takes

        public PageDef(string name, string title, RowDef[] left, RowDef[] right = null, RowDef[] bottom = null, float bottomShare = 0.45f)
        {
            Name = name;
            Title = title;
            Left = left;
            Right = right;
            Bottom = bottom;
            BottomShare = bottomShare;
        }
    }

    private static RowDef T(string key, string label) => new RowDef("Row_Toggle", key, label);
    private static RowDef S(string key, string label) => new RowDef("Row_Slider", key, label);
    private static RowDef C(string key, string label) => new RowDef("Row_Cycle", key, label);
    private static RowDef P(string key, string label) => new RowDef("Row_Picker", key, label);
    private static RowDef F(string key, string label) => new RowDef("Row_TextField", key, label);
    private static RowDef FL(string key, string label) => new RowDef("Row_Flags", key, label, RowH * 2f + 4f);
    private static RowDef B(string key, string text, string label = "") => new RowDef("Row_Button", key, label, 0f, text);
    private static RowDef H(string label, float height = 0f) => new RowDef("Row_Header", "", label, height);
    private static RowDef X(string key, string text, float height = 44f) => new RowDef("Row_Text", key, text, height);
    private static RowDef Col(string key, string label) => new RowDef("Row_Color", key, label);
    private static RowDef L(string key, string label) => new RowDef("Row_ListEditor", key, label, 300f);
    private static RowDef BC(string key, string label) => new RowDef("Row_BiomeCosts", key, label, 270f);
    private static RowDef IC(string key, string label) => new RowDef("Row_ItemCategories", key, label, 300f);
    private static RowDef BD(string key, string label, float height = 220f) => new RowDef("Row_BiomeDrops", key, label, height);
    private static RowDef EC(string key, string label) => new RowDef("Row_EffectConfigs", key, label, 470f);
    private static RowDef BT(string key, string label) => new RowDef("Row_Bounties", key, label, 540f);

    // A Row_Group places rows side by side in one line of the column; the mod binds its children like
    // the column's own (see README). Cells are fixed-width or share what is left equally.
    private const string GroupName = "Row_Group";
    private static RowDef G(params RowDef[] cells) => new RowDef(GroupName, "", "") { Children = cells };
    private static RowDef Fixed(RowDef cell, float width) { cell.Width = width; return cell; }
    private static RowDef Flex(RowDef cell) { cell.Flexible = true; return cell; }
    private static RowDef Hide(RowDef row, params string[] children) { row.Hidden = children; return row; }
    private static RowDef Short(RowDef group) { group.Height = SubRowH; return group; }

    // A row sized by its content (a Flags row's grid grows with the enum), with a narrower label.
    private static RowDef Tall(RowDef row, float labelWidth) { row.AutoHeight = true; row.LabelWidth = labelWidth; return row; }

    // Column captions over group lines: small text cells laid out like the cells below them.
    private static RowDef Caption(string text)
    {
        RowDef caption = X("", text, SubRowH);
        caption.LabelSize = 13;
        return caption;
    }

    // An extra-boss-drop line: the mode (short label, cycle), then the player range as a bare value box.
    private const float RangeCellW = 76f;
    private static RowDef BossLine(string modeKey, string label, string rangeKey)
    {
        RowDef mode = C(modeKey, label);
        mode.LabelWidth = 100f;
        return G(Flex(mode), Fixed(Hide(S(rangeKey, ""), "Label", "Slider"), RangeCellW));
    }

    // A feature-level line of the enchanting table page: the feature, then its default and max level.
    private const float FeatureNameW = 110f;
    private static RowDef FeatureLine(string feature, string label)
    {
        RowDef name = X("", label, RowH);
        name.LabelSize = 15;
        return G(Fixed(name, FeatureNameW),
            Flex(Hide(F("json:enchantingupgrades:DefaultFeatureLevels." + feature, label + " default"), "Label")),
            Flex(Hide(F("json:enchantingupgrades:MaximumFeatureLevels." + feature, label + " max"), "Label")));
    }

    private static readonly PageDef[] Pages =
    {
        new PageDef("Page_Welcome", "$mod_epicloot_cfg_page_welcome", new[]
        {
            H("$mod_epicloot_cfg_welcome_title"),
            X("", "$mod_epicloot_cfg_welcome_intro", 60f),
            X("", "$mod_epicloot_cfg_welcome_bullets", 150f),
            X("", "$mod_epicloot_cfg_welcome_skip", 44f),
            X("", "$mod_epicloot_cfg_welcome_later", 44f),
            B("action:url:discord", "$mod_epicloot_cfg_welcome_discord"),
            B("action:url:patchnotes", "$mod_epicloot_cfg_welcome_patchnotes"),
        }),

        new PageDef("Page_Balance", "$mod_epicloot_cfg_page_balance",
            new[]
            {
                H("Balance template"),
                B("action:preset:balanced", "$mod_epicloot_cfg_preset_balanced", "Recommended: enchantments are powerful, stronger enemies stay a threat."),
                B("action:preset:minimal", "$mod_epicloot_cfg_preset_minimal", "Reduced enchantment power, for vanilla difficulty."),
                B("action:preset:legendary", "$mod_epicloot_cfg_preset_legendary", "Legacy balancing; players can become godlike."),
                X("readout:template", "", 30f),
                C("BalanceConfigurationType", "Balance Template"),
                X("", "$mod_epicloot_cfg_balance_note", 40f),
                H("Drops"),
                S("GlobalDropRateModifier", "Global Drop Rate Modifier"),
                C("_gatedItemTypeModeConfig", "Item Drop Limits"),
                S("SetItemDropChance", "Set Item Drop Chance"),
                T("HealthCriticalEffectsEnabled", "Health Critical Enchantments"),
                T("TransferMagicItemToCrafts", "Transfer Enchants to Crafted Items"),
                // Only one of the two is shown at a time; the mod swaps them.
                B("action:advanced:show", "$mod_epicloot_cfg_show_advanced", "The other pages: loot, shardstones, the enchanting table, adventure and more."),
                B("action:advanced:hide", "$mod_epicloot_cfg_hide_advanced", "Back to this page only. Edits on the other pages are kept."),
            },
            new[]
            {
                H("Drop mix"),
                S("ItemDropRatio", "Item Drop Ratio"),
                S("ShardStoneDropRatio", "Shard Stone Drop Ratio"),
                S("ItemsUnidentifiedDropRatio", "Unidentified Drop Ratio"),
                S("MaterialsDropRatio", "Materials Drop Ratio"),
                X("readout:dropmix", "", 40f),
                new RowDef("Row_RarityCounts", "json:loottables:RarityCounts", "Enchantments and sockets of", 356f),
            }),

        new PageDef("Page_LootDrops", "$mod_epicloot_cfg_page_lootdrops",
            new[]
            {
                H("Extra boss drops for more players"),
                Short(G(Fixed(Caption("Drop"), 110f), Flex(Caption("Mode")), Fixed(Caption("Range (m)"), RangeCellW))),
                BossLine("_bossTrophyDropMode", "Trophies", "_bossTrophyDropPlayerRange"),
                BossLine("_bossCryptKeyDropMode", "Crypt Key", "_bossCryptKeyDropPlayerRange"),
                BossLine("_bossWishboneDropMode", "Wishbone", "_bossWishboneDropPlayerRange"),
                BossLine("ModerDropMode", "Moder", "ModerDropPlayerRange"),
                BossLine("YagluthDropMode", "Yagluth", "YagluthDropPlayerRange"),
                BossLine("QueenDropMode", "The Queen", "QueenDropPlayerRange"),
                BossLine("FaderDropMode", "Fader", "FaderDropPlayerRange"),
                BossLine("FrozenKingDropMode", "Frozen King", "FrozenKingDropPlayerRange"),
                H("Elite runestone drops"),
                T("EliteRunestoneDrops", "Elite Runestone Drops"),
                S("EliteRunestoneDropChance", "Runestone Drop Chance"),
            },
            new[]
            {
                H("Item categories"),
                X("", "Which items each category can drop, and the boss that unlocks them. Items other mods add are detected automatically when Auto Add Equipment (Advanced page) is on.", 60f),
                IC("json:iteminfo:ItemInfo", "Category items"),
            }),

        new PageDef("Page_Shardstones", "$mod_epicloot_cfg_page_shardstones",
            new[]
            {
                H("Socket rules"),
                T("AllowRuneSocketing", "Allow Rune Socketing"),
                T("AllowDuplicateSocketedEffects", "Allow Duplicate Socketed Effects"),
                T("AllowShardstoneDuplicateItemEffect", "Shardstone On Matching Item Effect"),
                T("AllowRunestoneDuplicateItemEffect", "Runestone On Matching Item Effect"),
                C("ShardSocketRemovalMode", "Shard Removal Mode"),
                C("RuneSocketRemovalMode", "Rune Removal Mode"),
                C("ShardStackingMode", "Shard Stack Mode"),
                S("ShardStackDecayFactor", "Shard Stack Decay Factor"),
                T("RuneDiminishingReturns", "Rune Diminishing Returns"),
                S("RuneStackDecayFactor", "Rune Stack Decay Factor"),
                C("RuneExtractItemMode", "Rune Extract Mode"),
                C("RuneSetExtractItemMode", "Set Rune Extract Mode"),
            },
            new[]
            {
                H("Brokkr's Gift"),
                X("", "Brokkr's Gift adds shard sockets to an item you already own: drag a Legendary, Mythic or Ancient gift onto a magic item. Each tier adds the slots set below, up to the most sockets the item's rarity can roll (the Sockets table on the Balance page). A failed roll still uses up the gift.", 60f),
                T("AllowGiftOnItemsWithSlots", "Allow Gift On Items With Slots"),
                S("LegendaryGiftSlotsAdded", "Legendary Slots Added"),
                S("MythicGiftSlotsAdded", "Mythic Slots Added"),
                S("AncientGiftSlotsAdded", "Ancient Slots Added"),
                S("LegendaryGiftSuccessChance", "Legendary Success Chance"),
                S("MythicGiftSuccessChance", "Mythic Success Chance"),
                S("AncientGiftSuccessChance", "Ancient Success Chance"),
                H("Shard globals"),
                S("json:shardstones:Global.Values.MovementPenaltyReference", "Movement Penalty Reference"),
                S("json:shardstones:Global.Values.BloodBlockSelfDamagePercent", "Blood Block Self Damage %"),
            }),

        new PageDef("Page_EnchantingTable", "$mod_epicloot_cfg_page_enchantingtable",
            new[]
            {
                H("Table"),
                T("EnchantingTableUpgradesActive", "Enchanting Table Upgrades Active"),
                Tall(FL("EnchantingTableActivatedTabs", "Active features"), 90f),
                H("Table UI"),
                T("ShowEnchantSelectionChance", "Show Enchant Selection Chance"),
                T("ShowEquippedAndHotbarItemsInSacrificeTab", "Show Equipped & Hotbar In Sacrifice"),
            },
            new[]
            {
                H("Feature levels (-1 = locked)"),
                Short(G(Fixed(Caption("Feature"), FeatureNameW), Flex(Caption("Default")), Flex(Caption("Max")))),
                FeatureLine("Sacrifice", "Sacrifice"),
                FeatureLine("ConvertMaterials", "Convert"),
                FeatureLine("Enchant", "Enchant"),
                FeatureLine("Augment", "Augment"),
                FeatureLine("Disenchant", "Disenchant"),
                FeatureLine("Rune", "Rune"),
            },
            bottom: new[]
            {
                new RowDef("Row_UpgradeCosts", "json:enchantingupgrades:UpgradeCosts", "Upgrade costs of", 280f),
            },
            bottomShare: 0.47f),

        new PageDef("Page_Merchant", "$mod_epicloot_cfg_page_merchant",
            new[]
            {
                H("Merchant"),
                T("_adventureModeEnabled", "Adventure Mode Enabled"),
                T("RemovePurchasedGambles", "Remove Purchased Gambles"),
                S("json:adventuredata:SecretStash.RefreshInterval", "Secret Stash Refresh (days)"),
                H("Gambling"),
                S("json:adventuredata:Gamble.RefreshInterval", "Gamble Refresh (days)"),
                S("json:adventuredata:Gamble.GamblesCount", "Gambles Offered"),
                S("json:adventuredata:Gamble.ForestTokenGamblesCount", "Forest Token Gambles"),
                S("json:adventuredata:Gamble.IronBountyGamblesCount", "Iron Token Gambles"),
                S("json:adventuredata:Gamble.GoldBountyGamblesCount", "Gold Token Gambles"),
                // 4 px shorter than a normal header: with every Tempering row shown the column needs 600
                // of its 602 px. The rows beside it on the right are the biome cost list, so nothing misaligns.
                H("Tempering", RowH - 4f),
                S("TemperBaseChance", "Temper Base Chance"),
                S("TemperDecrement", "Temper Decrement Amount"),
                T("TemperFailDowngrades", "Temper Fail Downgrades Effect"),
                T("TemperDestroysItem", "Temper Fail Destroys Item"),
                S("TemperChanceToDestroy", "Temper Destroy Chance"),
            },
            new[]
            {
                H("Treasure maps"),
                S("json:adventuredata:TreasureMap.RefreshInterval", "Map Refresh (days)"),
                S("json:adventuredata:TreasureMap.StartRadiusMin", "Start Radius Min"),
                S("json:adventuredata:TreasureMap.StartRadiusMax", "Start Radius Max"),
                S("json:adventuredata:TreasureMap.MinimapAreaRadius", "Map Circle Radius"),
                T("json:adventuredata:TreasureMap.ScaleRadiiToWorldSize", "Scale Radii To World Size"),
                // Andvaranaut finds treasure map chests; it sits here so the left column has room for Tempering.
                S("_andvaranautRange", "Andvaranaut Range"),
                BC("json:adventuredata:TreasureMap.BiomeInfo", "Map cost per biome"),
            }),

        new PageDef("Page_Bounties", "$mod_epicloot_cfg_page_bounties",
            new[]
            {
                H("Bounties"),
                C("BossBountyMode", "Gated Bounty Mode"),
                T("EnableLimitedBountiesInProgress", "Enable Bounty Limit"),
                S("MaxInProgressBounties", "Max Bounties Per Player"),
                S("json:adventuredata:Bounties.RefreshInterval", "Bounty Refresh (days)"),
                H("Target levels"),
                S("json:adventuredata:Bounties.IronMinLevel", "Iron Min Level"),
                S("json:adventuredata:Bounties.IronMaxLevel", "Iron Max Level"),
                S("json:adventuredata:Bounties.IronHealthMultiplier", "Iron Health Multiplier"),
                S("json:adventuredata:Bounties.GoldMinLevel", "Gold Min Level"),
                S("json:adventuredata:Bounties.GoldMaxLevel", "Gold Max Level"),
                S("json:adventuredata:Bounties.GoldHealthMultiplier", "Gold Health Multiplier"),
                S("json:adventuredata:Bounties.AddsMinLevel", "Adds Min Level"),
                S("json:adventuredata:Bounties.AddsMaxLevel", "Adds Max Level"),
                S("json:adventuredata:Bounties.AddsHealthMultiplier", "Adds Health Multiplier"),
            },
            new[]
            {
                H("Bounties by biome"),
                BT("json:adventuredata:Bounties.Targets", "Target / iron / gold / coins"),
            }),

        new PageDef("Page_Interface", "$mod_epicloot_cfg_page_interface",
            new[]
            {
                H("Interface"),
                T("UseGeneratedMagicItemNames", "Use Generated Magic Item Names"),
                T("KeepInventoryOpenOverItems", "Keep Inventory Open Over Items"),
                T("ShowRarityInRecipeList", "Show Rarity In Recipe List"),
                S("UIAudioVolumeAdjustment", "UI Audio Volume"),
                S("TooltipMaxWidth", "Tooltip Max Width"),
                S("TooltipMaxHeight", "Tooltip Max Height"),
                P("SocketOverlayShortcut", "Socket Overlay Shortcut"),
                P("TraderPanelDragKey", "Trader Panel Drag Key"),
                B("action:reset:TraderPanelPosition", "Reset", "Trader panel position"),
                B("action:reset:TemperPanelPosition", "Reset", "Temper panel position"),
                T("ShowQuickConfigButton", "Show in Mod Config launcher"),
            },
            new[]
            {
                H("Abilities"),
                P("AbilityKeyCodes.0", "Ability Hotkey 1"),
                P("AbilityKeyCodes.1", "Ability Hotkey 2"),
                P("AbilityKeyCodes.2", "Ability Hotkey 3"),
                P("AbilityBarAnchor", "Ability Bar Anchor"),
                P("AbilityBarLayoutAlignment", "Ability Bar Layout Alignment"),
                S("AbilityBarPosition.x", "Ability Bar Position X"),
                S("AbilityBarPosition.y", "Ability Bar Position Y"),
                S("AbilityBarIconSpacing", "Ability Bar Icon Spacing"),
            }),

        new PageDef("Page_ItemColors", "$mod_epicloot_cfg_page_itemcolors", new[]
        {
            H("Rarity colours and crafting material icon colours"),
            Col("_magicRarityColor", "Magic"),
            Col("_rareRarityColor", "Rare"),
            Col("_epicRarityColor", "Epic"),
            Col("_legendaryRarityColor", "Legendary"),
            Col("_mythicRarityColor", "Mythic"),
            Col("_ancientRarityColor", "Ancient"),
            Col("_setItemColor", "Set Item"),
            X("", "Pick a named colour or type a #hex value. The right-hand button picks the icon colour used for that rarity's crafting materials.", 44f),
        }),

        new PageDef("Page_Rarity", "$mod_epicloot_cfg_page_rarity", new[]
        {
            H("Drops by biome"),
            BD("json:loottables:LootTables", "Loot table", 560f),
        }),

        new PageDef("Page_EffectTuning", "$mod_epicloot_cfg_page_effecttuning", new[]
        {
            C("GatedFreebuildMode", "Gated Freebuild Mode"),
            X("", "$mod_epicloot_cfg_effecttuning_note", 40f),
            H("Effect configuration"),
            EC("json:magiceffects:EffectConfigs", "Effect"),
        }),

        new PageDef("Page_Advanced", "$mod_epicloot_cfg_page_advanced", new[]
        {
            H("Auto-add equipment and creatures"),
            T("AutoAddEquipment", "Auto Add Equipment"),
            T("AutoRemoveEquipmentNotFound", "    Auto Remove Equipment Not Found"),
            T("OnlyAddEquipmentWithRecipes", "    Only Add Equipment With Recipes"),
            T("AutoAddRemoveEquipmentFromVendor", "    Add / Remove From Vendor"),
            T("AutoAddRemoveEquipmentFromLootLists", "    Add / Remove From Loot Lists"),
            T("AutoAddCreaturesToLootTables", "Auto Add Creatures To Loot Tables"),
            H("Diagnostics"),
            T("_loggingEnabled", "Logging Enabled"),
            P("_logLevel", "Log Level"),
            T("EnableHotReloadPatches", "Enable Hot Reloading Patches"),
            T("AlwaysRefreshCoreConfigs", "Always Refresh Core Configs"),
            X("", "Always Refresh Core Configs overwrites every baseconfig file with the mod defaults on startup. This deletes any modifications to the core configs.", 40f),
            T("Common.EnableDebugMode", "Debug Mode"),
            S("Common.ConfigApplyDelay", "Config Apply Delay"),
            T("AlwaysShowWelcomeMessage", "Show Welcome Wizard Next Launch"),
        }),
    };

    // ------------------------------------------------------------------------------------------------
    //  Row templates
    // ------------------------------------------------------------------------------------------------

    private static void BuildToggleRow()
    {
        GameObject row = NewRow("Row_Toggle", RowH);
        AddLabel(row.transform, "Label", "Toggle", 15, Beige, flexible: true);
        MakeToggle(row.transform, "Toggle", 26f);
        SavePrefab(row, RowsDir + "/Row_Toggle.prefab");
    }

    private static void BuildSliderRow()
    {
        GameObject row = NewRow("Row_Slider", RowH);
        AddLabel(row.transform, "Label", "Slider", 15, Beige, flexible: true, preferredWidth: LabelW);
        MakeSlider(row.transform, "Slider", 150f);
        MakeInputField(row.transform, "Value", 64f, 28f, TMP_InputField.ContentType.DecimalNumber, TextAlignmentOptions.MidlineRight);
        SavePrefab(row, RowsDir + "/Row_Slider.prefab");
    }

    private static void BuildCycleRow()
    {
        GameObject row = NewRow("Row_Cycle", RowH);
        AddLabel(row.transform, "Label", "Cycle", 15, Beige, flexible: true, preferredWidth: LabelW);
        MakeButton(row.transform, "Cycle", "Option", 200f, 28f, 14);
        SavePrefab(row, RowsDir + "/Row_Cycle.prefab");
    }

    private static void BuildPickerRow()
    {
        GameObject row = NewRow("Row_Picker", RowH);
        AddLabel(row.transform, "Label", "Picker", 15, Beige, flexible: true, preferredWidth: LabelW);
        MakeInputField(row.transform, "Field", 160f, 28f, TMP_InputField.ContentType.Standard, TextAlignmentOptions.MidlineLeft);
        MakeButton(row.transform, "Pick", "...", 34f, 28f, 14);
        SavePrefab(row, RowsDir + "/Row_Picker.prefab");
    }

    private static void BuildTextFieldRow()
    {
        GameObject row = NewRow("Row_TextField", RowH);
        AddLabel(row.transform, "Label", "Text", 15, Beige, flexible: false, preferredWidth: 150f);
        TMP_InputField field = MakeInputField(row.transform, "Field", 200f, 28f, TMP_InputField.ContentType.Standard, TextAlignmentOptions.MidlineLeft);
        field.GetComponent<LayoutElement>().flexibleWidth = 1f;
        SavePrefab(row, RowsDir + "/Row_TextField.prefab");
    }

    private static void BuildFlagsRow()
    {
        GameObject row = NewRow("Row_Flags", RowH * 2f + 4f);
        HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.UpperLeft;
        AddLabel(row.transform, "Label", "Flags", 15, Beige, flexible: false, preferredWidth: LabelW);

        GameObject flags = NewUI("Flags", row.transform);
        LayoutElement fle = flags.AddComponent<LayoutElement>();
        fle.flexibleWidth = 1f;
        GridLayoutGroup grid = flags.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(150f, SubRowH);
        grid.spacing = new Vector2(6f, 4f);
        grid.childAlignment = TextAnchor.UpperLeft;

        GameObject flag = NewUI("Flag", flags.transform);
        HorizontalLayoutGroup fh = flag.AddComponent<HorizontalLayoutGroup>();
        fh.childAlignment = TextAnchor.MiddleLeft;
        fh.spacing = 6f;
        fh.childControlWidth = true;
        fh.childControlHeight = true;
        fh.childForceExpandWidth = false;
        fh.childForceExpandHeight = false;
        MakeToggle(flag.transform, "Toggle", 22f);
        AddLabel(flag.transform, "Label", "Flag", 13, Beige, flexible: true);

        SavePrefab(row, RowsDir + "/Row_Flags.prefab");
    }

    private static void BuildButtonRow()
    {
        GameObject row = NewRow("Row_Button", RowH + 6f);
        MakeButton(row.transform, "Button", "Button", 180f, 34f, 15);
        AddLabel(row.transform, "Label", "", 13, Beige, flexible: true);
        SavePrefab(row, RowsDir + "/Row_Button.prefab");
    }

    private static void BuildHeaderRow()
    {
        GameObject row = NewRow("Row_Header", RowH);
        TextMeshProUGUI label = AddLabel(row.transform, "Label", "Header", 18, Yellow, flexible: true);
        label.fontStyle = FontStyles.Bold;
        SavePrefab(row, RowsDir + "/Row_Header.prefab");
    }

    private static void BuildTextRow()
    {
        GameObject row = NewRow("Row_Text", 44f);
        row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
        TextMeshProUGUI label = AddLabel(row.transform, "Label", "Text", 13, Beige, flexible: true);
        label.alignment = TextAlignmentOptions.TopLeft;
        SavePrefab(row, RowsDir + "/Row_Text.prefab");
    }

    private static void BuildColorRow()
    {
        GameObject row = NewRow("Row_Color", RowH);
        AddLabel(row.transform, "Label", "Colour", 15, Beige, flexible: true, preferredWidth: 110f);
        GameObject swatch = NewUI("Swatch", row.transform);
        Image sw = swatch.AddComponent<Image>();
        sw.sprite = Sprite(SpriteField);
        sw.type = Image.Type.Sliced;
        sw.color = Color.white;
        LayoutElement swle = swatch.AddComponent<LayoutElement>();
        swle.preferredWidth = 26f;
        swle.preferredHeight = 26f;
        MakeInputField(row.transform, "Field", 130f, 28f, TMP_InputField.ContentType.Standard, TextAlignmentOptions.MidlineLeft);
        MakeButton(row.transform, "Pick", "...", 34f, 28f, 14);
        MakeButton(row.transform, "Cycle", "Icon", 110f, 28f, 13);
        SavePrefab(row, RowsDir + "/Row_Color.prefab");
    }

    // The Riches editor: an open item -> cost map.
    private static void BuildListEditorRow()
    {
        GameObject row = NewListRow("Row_ListEditor", 300f, "List", out Transform head, out Transform content);
        MakeButton(head, "Add", "Add", 80f, 26f, 13);

        Transform item = NewListItem(content);
        TMP_InputField name = MakeInputField(item, "Field", 150f, 26f, TMP_InputField.ContentType.Standard, TextAlignmentOptions.MidlineLeft);
        name.GetComponent<LayoutElement>().flexibleWidth = 1f;
        MakeButton(item, "Pick", "...", 30f, 26f, 13);
        MakeInputField(item, "Cost", 70f, 26f, TMP_InputField.ContentType.IntegerNumber, TextAlignmentOptions.MidlineRight);
        MakeButton(item, "Remove", "x", 26f, 26f, 13);

        SavePrefab(row, RowsDir + "/Row_ListEditor.prefab");
    }

    // Treasure map cost per biome: a fixed set of rows (the file's biomes), two numbers each.
    private static void BuildBiomeCostsRow()
    {
        GameObject row = NewListRow("Row_BiomeCosts", 270f, "Biome costs", out Transform head, out Transform content);
        AddLabel(head, "CostHead", "Coins", 12, Yellow, flexible: false, preferredWidth: 80f).alignment = TextAlignmentOptions.MidlineRight;
        AddLabel(head, "TokensHead", "Forest tokens", 12, Yellow, flexible: false, preferredWidth: 100f).alignment = TextAlignmentOptions.MidlineRight;

        Transform item = NewListItem(content);
        AddLabel(item, "Name", "Biome", 14, Orange, flexible: true, preferredWidth: 150f);
        MakeInputField(item, "Cost", 80f, 26f, TMP_InputField.ContentType.IntegerNumber, TextAlignmentOptions.MidlineRight);
        MakeInputField(item, "Tokens", 100f, 26f, TMP_InputField.ContentType.IntegerNumber, TextAlignmentOptions.MidlineRight);

        SavePrefab(row, RowsDir + "/Row_BiomeCosts.prefab");
    }

    // loottables.json per biome: pick a biome, edit the drop amount table and rarity weights of each
    // creature tier level and chest that belongs to it.
    // loottables.json per biome: pick a biome and one of its loot tables (creature tier level, boss or
    // chest), then edit how many items it drops and at which rarity, as count:chance lists.
    private static void BuildBiomeDropsRow()
    {
        GameObject row = NewStackRow("Row_BiomeDrops", 220f, "Loot table", out Transform head);
        LayoutElement label = head.Find("Label").GetComponent<LayoutElement>();
        label.flexibleWidth = 0f;
        label.preferredWidth = 80f;
        MakeButton(head, "Biome", "Biome", 170f, 26f, 13);
        MakeButton(head, "Prev", "<", 28f, 26f, 14);
        MakeButton(head, "Target", "Table", 300f, 26f, 12).GetComponent<LayoutElement>().flexibleWidth = 1f;
        MakeButton(head, "Next", ">", 28f, 26f, 14);

        GameObject lists = NewUI("Lists", row.transform);
        LayoutElement le = lists.AddComponent<LayoutElement>();
        le.flexibleHeight = 1f;
        HorizontalLayoutGroup hlg = lists.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        MakeWeightList(lists.transform, "Amount", "Drop amount (items, chance)", openKeys: true);
        MakeWeightList(lists.transform, "Rarity", "Rarity (chance)", openKeys: false);

        SavePrefab(row, RowsDir + "/Row_BiomeDrops.prefab");
    }

    // loottables.json MagicEffectsCount and SocketCounts: pick a rarity, edit its two count:chance lists.
    private static void BuildRarityCountsRow()
    {
        GameObject row = NewStackRow("Row_RarityCounts", 356f, "Enchantments and sockets of", out Transform head);
        MakeButton(head, "Rarity", "Rarity", 150f, 26f, 14);
        MakeWeightList(row.transform, "Enchantments", "Enchantments (count, chance)", openKeys: true);
        MakeWeightList(row.transform, "Sockets", "Sockets (count, chance)", openKeys: true);
        SavePrefab(row, RowsDir + "/Row_RarityCounts.prefab");
    }

    // enchantingupgrades.json UpgradeCosts: pick a feature, and all its levels show at once as a grid
    // of Level blocks (the level's name and +, then its items with their amounts). The mod clones the
    // inactive Level template per level and the Item inside it per item, and sets the grid's cell
    // height to the level with the most items.
    private const float LevelBlockW = 202f;

    private static void BuildUpgradeCostsRow()
    {
        GameObject row = NewStackRow("Row_UpgradeCosts", 280f, "Upgrade costs of", out Transform head);
        MakeButton(head, "Feature", "Feature", 180f, 26f, 13);

        GameObject items = NewUI("Items", row.transform);
        LayoutElement ile = items.AddComponent<LayoutElement>();
        ile.flexibleHeight = 1f;
        ile.preferredHeight = 280f - RowH - 20f;
        Transform content = MakeScrollList(items);
        Object.DestroyImmediate(content.GetComponent<VerticalLayoutGroup>());
        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(LevelBlockW, 106f);
        grid.spacing = new Vector2(6f, 6f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperLeft;

        GameObject level = NewUI("Level", content);
        Image background = level.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.25f);
        VerticalLayoutGroup vlg = level.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(4, 4, 3, 3);
        vlg.spacing = 2f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject levelHead = NewUI("Head", level.transform);
        LayoutElement hle = levelHead.AddComponent<LayoutElement>();
        hle.preferredHeight = 22f;
        hle.minHeight = 22f;
        HorizontalLayoutGroup hh = levelHead.AddComponent<HorizontalLayoutGroup>();
        hh.childAlignment = TextAnchor.MiddleLeft;
        hh.spacing = 4f;
        hh.childControlWidth = true;
        hh.childControlHeight = true;
        hh.childForceExpandWidth = false;
        hh.childForceExpandHeight = false;
        AddLabel(levelHead.transform, "Label", "Level 1", 13, Yellow, flexible: true).fontStyle = FontStyles.Bold;
        MakeButton(levelHead.transform, "Add", "+", 22f, 20f, 14);

        Transform item = NewListItem(level.transform);
        item.GetComponent<LayoutElement>().preferredHeight = 24f;
        TMP_InputField name = MakeInputField(item, "Field", 90f, 22f, TMP_InputField.ContentType.Standard, TextAlignmentOptions.MidlineLeft);
        name.GetComponent<LayoutElement>().flexibleWidth = 1f;
        MakeButton(item, "Pick", "...", 22f, 22f, 12);
        MakeInputField(item, "Amount", 34f, 22f, TMP_InputField.ContentType.IntegerNumber, TextAlignmentOptions.MidlineRight);
        MakeButton(item, "Remove", "x", 20f, 22f, 12);

        SavePrefab(row, RowsDir + "/Row_UpgradeCosts.prefab");
    }

    // Every magic effect with a Config block: pick an effect, edit its key/value tunables.
    private static void BuildEffectConfigsRow()
    {
        GameObject row = NewListRow("Row_EffectConfigs", 470f, "Effect", out Transform head, out Transform content);
        MakeButton(head, "Effect", "Effect", 320f, 26f, 13);
        MakeButton(head, "Add", "Add", 70f, 26f, 13);

        Transform item = NewListItem(content);
        TMP_InputField key = MakeInputField(item, "Key", 260f, 26f, TMP_InputField.ContentType.Standard, TextAlignmentOptions.MidlineLeft);
        key.GetComponent<LayoutElement>().flexibleWidth = 1f;
        MakeButton(item, "Pick", "...", 30f, 26f, 13);
        MakeInputField(item, "Value", 140f, 26f, TMP_InputField.ContentType.DecimalNumber, TextAlignmentOptions.MidlineRight);
        MakeButton(item, "Remove", "x", 26f, 26f, 13);

        SavePrefab(row, RowsDir + "/Row_EffectConfigs.prefab");
    }

    // adventuredata.json bounties: pick a biome, edit its targets and rewards, add a registered monster.
    private static void BuildBountiesRow()
    {
        GameObject row = NewListRow("Row_Bounties", 540f, "Target / iron / gold / coins", out Transform head, out Transform content);
        MakeButton(head, "Biome", "Biome", 150f, 26f, 13);
        MakeButton(head, "Add", "Add", 60f, 26f, 13);

        Transform item = NewListItem(content);
        TMP_InputField target = MakeInputField(item, "Target", 120f, 26f, TMP_InputField.ContentType.Standard, TextAlignmentOptions.MidlineLeft);
        target.GetComponent<LayoutElement>().flexibleWidth = 1f;
        MakeButton(item, "Pick", "...", 30f, 26f, 13);
        MakeInputField(item, "Iron", 46f, 26f, TMP_InputField.ContentType.IntegerNumber, TextAlignmentOptions.MidlineRight);
        MakeInputField(item, "Gold", 46f, 26f, TMP_InputField.ContentType.IntegerNumber, TextAlignmentOptions.MidlineRight);
        MakeInputField(item, "Coins", 56f, 26f, TMP_InputField.ContentType.IntegerNumber, TextAlignmentOptions.MidlineRight);
        MakeButton(item, "Remove", "x", 26f, 26f, 13);

        SavePrefab(row, RowsDir + "/Row_Bounties.prefab");
    }

    // iteminfo.json categories: pick a category, edit its (boss key, item) entries.
    private static void BuildItemCategoriesRow()
    {
        GameObject row = NewListRow("Row_ItemCategories", 300f, "Category items", out Transform head, out Transform content);
        MakeButton(head, "Category", "Category", 170f, 26f, 13);
        MakeButton(head, "Add", "Add", 70f, 26f, 13);

        Transform item = NewListItem(content);
        MakeButton(item, "Boss", "none", 140f, 26f, 12);
        TMP_InputField name = MakeInputField(item, "Field", 130f, 26f, TMP_InputField.ContentType.Standard, TextAlignmentOptions.MidlineLeft);
        name.GetComponent<LayoutElement>().flexibleWidth = 1f;
        MakeButton(item, "Pick", "...", 30f, 26f, 13);
        MakeButton(item, "Remove", "x", 26f, 26f, 13);

        SavePrefab(row, RowsDir + "/Row_ItemCategories.prefab");
    }

    // The shell shared by the list rows: a vertical row with a Head line (Label first; the caller adds
    // its buttons after) and an Items scroll list whose content the caller fills with an Item template.
    private static GameObject NewListRow(string name, float height, string label, out Transform head, out Transform content)
    {
        GameObject row = NewStackRow(name, height, label, out head);
        GameObject items = NewUI("Items", row.transform);
        LayoutElement ile = items.AddComponent<LayoutElement>();
        ile.flexibleHeight = 1f;
        ile.preferredHeight = height - RowH - 20f;
        content = MakeScrollList(items);
        return row;
    }

    // A vertical row whose first child is a Head line (Label first; the caller adds its buttons after).
    private static GameObject NewStackRow(string name, float height, string label, out Transform head)
    {
        GameObject row = NewRow(name, height);
        Object.DestroyImmediate(row.GetComponent<HorizontalLayoutGroup>());
        VerticalLayoutGroup vlg = row.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject headGo = NewUI("Head", row.transform);
        LayoutElement hle = headGo.AddComponent<LayoutElement>();
        hle.preferredHeight = RowH;
        HorizontalLayoutGroup hh = headGo.AddComponent<HorizontalLayoutGroup>();
        hh.childAlignment = TextAnchor.MiddleLeft;
        hh.spacing = 6f;
        hh.childControlWidth = true;
        hh.childControlHeight = true;
        hh.childForceExpandWidth = false;
        hh.childForceExpandHeight = false;
        AddLabel(headGo.transform, "Label", label, 15, Orange, flexible: true);
        head = headGo.transform;
        return row;
    }

    // A count:chance list (the mod's WeightListView): Head -> Label, Total (the warning shown while the
    // chances do not add up to 100), Add; Items -> Item with Count or Name, Slider (0-100), Value, Remove.
    // With open keys the counts are typed and entries added or removed; otherwise each entry is a fixed
    // key the mod names (the rarity chances).
    private static Transform MakeWeightList(Transform parent, string name, string label, bool openKeys)
    {
        GameObject list = NewUI(name, parent);
        LayoutElement le = list.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
        le.flexibleHeight = 1f;
        VerticalLayoutGroup vlg = list.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject head = NewUI("Head", list.transform);
        LayoutElement hle = head.AddComponent<LayoutElement>();
        hle.preferredHeight = 24f;
        hle.minHeight = 24f;
        HorizontalLayoutGroup hh = head.AddComponent<HorizontalLayoutGroup>();
        hh.childAlignment = TextAnchor.MiddleLeft;
        hh.spacing = 6f;
        hh.childControlWidth = true;
        hh.childControlHeight = true;
        hh.childForceExpandWidth = false;
        hh.childForceExpandHeight = false;
        AddLabel(head.transform, "Label", label, 14, Yellow, flexible: true);
        AddLabel(head.transform, "Total", "", 12, Orange, flexible: false, preferredWidth: 170f).alignment = TextAlignmentOptions.MidlineRight;
        if (openKeys)
        {
            MakeButton(head.transform, "Add", "+", 26f, 22f, 15);
        }

        GameObject items = NewUI("Items", list.transform);
        LayoutElement ile = items.AddComponent<LayoutElement>();
        ile.flexibleHeight = 1f;
        ile.preferredHeight = 64f;
        Transform item = NewListItem(MakeScrollList(items));
        if (openKeys)
        {
            MakeInputField(item, "Count", 44f, 24f, TMP_InputField.ContentType.IntegerNumber, TextAlignmentOptions.MidlineRight);
        }
        else
        {
            AddLabel(item, "Name", "Rarity", 13, Orange, flexible: false, preferredWidth: 80f);
        }
        MakeSlider(item, "Slider", 100f).GetComponent<LayoutElement>().flexibleWidth = 1f;
        MakeInputField(item, "Value", 44f, 24f, TMP_InputField.ContentType.IntegerNumber, TextAlignmentOptions.MidlineRight);
        AddLabel(item, "Percent", "%", 13, Beige, flexible: false, preferredWidth: 14f);
        if (openKeys)
        {
            MakeButton(item, "Remove", "x", 24f, 24f, 13);
        }
        return list.transform;
    }

    private static Transform NewListItem(Transform content)
    {
        GameObject item = NewUI("Item", content);
        LayoutElement itle = item.AddComponent<LayoutElement>();
        itle.preferredHeight = SubRowH + 4f;
        HorizontalLayoutGroup ih = item.AddComponent<HorizontalLayoutGroup>();
        ih.childAlignment = TextAnchor.MiddleLeft;
        ih.spacing = 4f;
        ih.childControlWidth = true;
        ih.childControlHeight = true;
        ih.childForceExpandWidth = false;
        ih.childForceExpandHeight = false;
        return item.transform;
    }

    // ------------------------------------------------------------------------------------------------
    //  Shell and overlays
    // ------------------------------------------------------------------------------------------------

    private static void BuildPanel()
    {
        GameObject root = NewUI("QuickConfigPanel", null);
        Stretch(root);
        AddByTypeName(root, "Localize");

        GameObject overlay = NewUI("Overlay", root.transform);
        Stretch(overlay);
        Image dim = overlay.AddComponent<Image>();
        dim.color = Dim;
        dim.raycastTarget = true;

        GameObject panel = MakePanel(overlay.transform, "Panel", PanelW, PanelH);

        TextMeshProUGUI title = AddText(panel.transform, "Title", "Epic Loot", 22, Yellow, TextAlignmentOptions.Center);
        RectTransform trt = title.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -16f);
        trt.sizeDelta = new Vector2(-120f, 34f);
        title.fontStyle = FontStyles.Bold;

        Button close = MakeButton(panel.transform, "Close", "X", 30f, 30f, 14);
        Object.DestroyImmediate(close.GetComponent<LayoutElement>());
        RectTransform crt = (RectTransform)close.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 1f);
        crt.anchoredPosition = new Vector2(-16f, -14f);
        crt.sizeDelta = new Vector2(30f, 30f);

        GameObject pages = NewUI("Pages", panel.transform);
        RectTransform prt = (RectTransform)pages.transform;
        prt.anchorMin = new Vector2(0f, 0f);
        prt.anchorMax = new Vector2(1f, 1f);
        prt.offsetMin = new Vector2(26f, 96f);
        prt.offsetMax = new Vector2(-26f, -62f);

        TextMeshProUGUI status = AddText(panel.transform, "Status", "", 13, Beige, TextAlignmentOptions.Center);
        RectTransform srt = status.rectTransform;
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(1f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 66f);
        srt.sizeDelta = new Vector2(-52f, 24f);

        PlaceNavButton(MakeButton(panel.transform, "Back", "$mod_epicloot_cfg_back", 130f, 40f, 16), new Vector2(0f, 0f), new Vector2(26f, 18f));
        PlaceNavButton(MakeButton(panel.transform, "Reset", "$mod_epicloot_cfg_reset_page", 150f, 40f, 15), new Vector2(0f, 0f), new Vector2(168f, 18f));
        PlaceNavButton(MakeButton(panel.transform, "Save", "$mod_epicloot_cfg_save", 170f, 40f, 16), new Vector2(0.5f, 0f), new Vector2(0f, 18f));
        PlaceNavButton(MakeButton(panel.transform, "Next", "$mod_epicloot_cfg_next", 170f, 40f, 16), new Vector2(1f, 0f), new Vector2(-26f, 18f));
        PlaceNavButton(MakeButton(panel.transform, "Finish", "$mod_epicloot_cfg_finish", 170f, 40f, 16), new Vector2(1f, 0f), new Vector2(-26f, 18f));

        SavePrefab(root, Root + "/QuickConfigPanel.prefab");
    }

    private static void BuildPicker()
    {
        GameObject root = NewUI("QuickConfigPicker", null);
        Stretch(root);
        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.35f);
        dim.raycastTarget = true;

        GameObject panel = MakePanel(root.transform, "Panel", 440f, 560f);

        TextMeshProUGUI title = AddText(panel.transform, "Title", "Pick", 20, Yellow, TextAlignmentOptions.Center);
        RectTransform trt = title.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -16f);
        trt.sizeDelta = new Vector2(-100f, 30f);

        TMP_InputField search = MakeInputField(panel.transform, "Search", 380f, 30f, TMP_InputField.ContentType.Standard, TextAlignmentOptions.MidlineLeft);
        Object.DestroyImmediate(search.GetComponent<LayoutElement>());
        RectTransform srt = (RectTransform)search.transform;
        srt.anchorMin = new Vector2(0f, 1f);
        srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -54f);
        srt.sizeDelta = new Vector2(-40f, 30f);
        SetPlaceholder(search, "$mod_epicloot_cfg_picker_search");

        GameObject list = NewUI("List", panel.transform);
        RectTransform lrt = (RectTransform)list.transform;
        lrt.anchorMin = new Vector2(0f, 0f);
        lrt.anchorMax = new Vector2(1f, 1f);
        lrt.offsetMin = new Vector2(20f, 64f);
        lrt.offsetMax = new Vector2(-20f, -92f);
        Transform content = MakeScrollList(list);

        Button entry = MakeButton(content, "Entry", "Entry", 360f, 28f, 14);
        entry.GetComponent<LayoutElement>().flexibleWidth = 1f;
        TextMeshProUGUI entryText = entry.GetComponentInChildren<TextMeshProUGUI>();
        entryText.alignment = TextAlignmentOptions.MidlineLeft;

        Button close = MakeButton(panel.transform, "Close", "$mod_epicloot_cfg_close", 140f, 36f, 15);
        Object.DestroyImmediate(close.GetComponent<LayoutElement>());
        RectTransform crt = (RectTransform)close.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.anchoredPosition = new Vector2(0f, 18f);
        crt.sizeDelta = new Vector2(140f, 36f);

        SavePrefab(root, Root + "/QuickConfigPicker.prefab");
    }

    private static void BuildConfirm()
    {
        GameObject root = NewUI("QuickConfigConfirm", null);
        Stretch(root);
        Image dim = root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.35f);
        dim.raycastTarget = true;

        GameObject panel = MakePanel(root.transform, "Panel", 520f, 220f);

        TextMeshProUGUI title = AddText(panel.transform, "Title", "$mod_epicloot_cfg_discard_title", 20, Yellow, TextAlignmentOptions.Center);
        RectTransform trt = title.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -16f);
        trt.sizeDelta = new Vector2(-40f, 30f);

        TextMeshProUGUI body = AddText(panel.transform, "Body", "$mod_epicloot_cfg_discard_body", 14, Beige, TextAlignmentOptions.Center);
        RectTransform brt = body.rectTransform;
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.offsetMin = new Vector2(24f, 72f);
        brt.offsetMax = new Vector2(-24f, -54f);

        GameObject buttons = NewUI("Buttons", panel.transform);
        RectTransform burt = (RectTransform)buttons.transform;
        burt.anchorMin = new Vector2(0f, 0f);
        burt.anchorMax = new Vector2(1f, 0f);
        burt.pivot = new Vector2(0.5f, 0f);
        burt.anchoredPosition = new Vector2(0f, 18f);
        burt.sizeDelta = new Vector2(-40f, 40f);
        HorizontalLayoutGroup hlg = buttons.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 12f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        MakeButton(buttons.transform, "Keep", "$mod_epicloot_cfg_keep_editing", 150f, 36f, 14);
        MakeButton(buttons.transform, "Discard", "$mod_epicloot_cfg_discard", 150f, 36f, 14);
        MakeButton(buttons.transform, "SaveClose", "$mod_epicloot_cfg_save_close", 150f, 36f, 14);

        SavePrefab(root, Root + "/QuickConfigConfirm.prefab");
    }

    // ------------------------------------------------------------------------------------------------
    //  Pages
    // ------------------------------------------------------------------------------------------------

    private static void BuildPages(bool overwrite, ICollection<string> only = null)
    {
        EnsureFolders();
        Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        foreach (string name in RowTemplates.Select(template => template.Name))
        {
            GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(RowsDir + "/" + name + ".prefab");
            if (template == null)
            {
                Debug.LogError("[QuickConfig] Row template missing: " + name + ". Run 'Build Row Templates' first.");
                return;
            }
            templates[name] = template;
        }

        int written = 0;
        foreach (PageDef page in Pages)
        {
            string path = PagesDir + "/" + page.Name + ".prefab";
            if ((!overwrite && File.Exists(path)) || (only != null && !only.Contains(page.Name)))
            {
                continue;
            }

            // The root's name is the page id; the mod derives the title token from it (see README).
            GameObject root = NewUI(page.Name, null);
            Stretch(root);

            // The mod's RowBinder walks the containers named Column, Left and Right, so a page may
            // combine two columns on top with one full-width Column underneath.
            float split = page.Bottom != null ? page.BottomShare : 0f;
            if (page.Right == null)
            {
                Transform column = MakeColumn(root.transform, "Column", 0f, 1f, 0f, 0f, split, 1f);
                FillColumn(column, page.Left, templates);
            }
            else
            {
                Transform left = MakeColumn(root.transform, "Left", 0f, 0.5f, 0f, -8f, split, 1f);
                Transform right = MakeColumn(root.transform, "Right", 0.5f, 1f, 8f, 0f, split, 1f);
                FillColumn(left, page.Left, templates);
                FillColumn(right, page.Right, templates);
                if (page.Bottom != null)
                {
                    Transform bottom = MakeColumn(root.transform, "Column", 0f, 1f, 0f, 0f, 0f, split);
                    FillColumn(bottom, page.Bottom, templates);
                }
            }

            SavePrefab(root, path);
            written++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[QuickConfig] Wrote " + written + " page prefab(s) to " + PagesDir + (overwrite ? " (overwrite)" : " (missing only)"));
    }

    private static Transform MakeColumn(Transform parent, string name, float xMin, float xMax, float leftInset, float rightInset, float yMin = 0f, float yMax = 1f)
    {
        GameObject column = NewUI(name, parent);
        RectTransform rt = (RectTransform)column.transform;
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = new Vector2(leftInset, 0f);
        rt.offsetMax = new Vector2(rightInset, 0f);
        VerticalLayoutGroup vlg = column.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        return column.transform;
    }

    private static void FillColumn(Transform column, RowDef[] rows, Dictionary<string, GameObject> templates)
    {
        foreach (RowDef def in rows)
        {
            if (def.Children != null)
            {
                FillGroup(column, def, templates);
            }
            else
            {
                AddRow(column, def, templates);
            }
        }
    }

    // A plain container, not a template: nothing in it is bound but its rows, and it keeps its name.
    private static void FillGroup(Transform column, RowDef def, Dictionary<string, GameObject> templates)
    {
        GameObject group = NewRow(GroupName, def.Height > 0f ? def.Height : RowH);
        group.transform.SetParent(column, false);
        HorizontalLayoutGroup hlg = group.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(0, 0, 0, 0);
        hlg.childForceExpandHeight = true;
        // Force-expanding children makes the group report a flexible height, and the column would hand
        // it the column's spare space.
        group.GetComponent<LayoutElement>().flexibleHeight = 0f;
        foreach (RowDef cell in def.Children)
        {
            AddRow(group.transform, cell, templates);
        }
    }

    private static void AddRow(Transform parent, RowDef def, Dictionary<string, GameObject> templates)
    {
        GameObject row = (GameObject)PrefabUtility.InstantiatePrefab(templates[def.Template], parent);
        // The row's name IS its key: that is how the mod finds it (see README). Decorative rows keep
        // their template name and are never bound.
        row.name = string.IsNullOrEmpty(def.Key) ? def.Template : def.Key;

        Transform label = row.transform.Find("Label") ?? row.transform.Find("Head/Label");
        if (label != null)
        {
            TextMeshProUGUI text = label.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = def.Label ?? "";
                if (def.LabelSize > 0)
                {
                    text.fontSize = def.LabelSize;
                    text.alignment = TextAlignmentOptions.MidlineLeft;
                    HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
                    if (hlg != null) { hlg.childAlignment = TextAnchor.MiddleLeft; }
                }
            }
            if (def.LabelWidth > 0f)
            {
                LayoutElement labelLayout = label.GetComponent<LayoutElement>() ?? label.gameObject.AddComponent<LayoutElement>();
                labelLayout.preferredWidth = def.LabelWidth;
                labelLayout.flexibleWidth = 0f;
            }
        }
        foreach (string hidden in def.Hidden ?? new string[0])
        {
            Transform child = row.transform.Find(hidden);
            if (child != null) { child.gameObject.SetActive(false); }
        }

        if (!string.IsNullOrEmpty(def.ButtonText))
        {
            Transform button = row.transform.Find("Button");
            TextMeshProUGUI text = button != null ? button.GetComponentInChildren<TextMeshProUGUI>() : null;
            if (text != null) { text.text = def.ButtonText; }
        }

        LayoutElement le = row.GetComponent<LayoutElement>();
        if (le == null) { return; }
        if (def.Height > 0f)
        {
            le.preferredHeight = def.Height;
            le.minHeight = def.Height;
        }
        if (def.AutoHeight)
        {
            le.preferredHeight = -1f;
            le.minHeight = RowH;
        }
        // Group cells: the fixed ones hold their width, the flexible ones split the rest equally.
        if (def.Width > 0f)
        {
            le.preferredWidth = def.Width;
            le.minWidth = def.Width;
            le.flexibleWidth = 0f;
        }
        else if (def.Flexible)
        {
            le.preferredWidth = 0f;
            le.minWidth = 0f;
            le.flexibleWidth = 1f;
        }
    }

    // ------------------------------------------------------------------------------------------------
    //  Widget factories
    // ------------------------------------------------------------------------------------------------

    private static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        if (parent != null) { go.transform.SetParent(parent, false); }
        return go;
    }

    private static void Stretch(GameObject go)
    {
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject NewRow(string name, float height)
    {
        GameObject row = NewUI(name, null);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        le.flexibleWidth = 1f;
        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(4, 4, 0, 0);
        hlg.spacing = 6f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        return row;
    }

    private static TextMeshProUGUI AddText(Transform parent, string name, string text, int size, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = NewUI(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static TextMeshProUGUI AddLabel(Transform parent, string name, string text, int size, Color color, bool flexible, float preferredWidth = 0f)
    {
        TextMeshProUGUI tmp = AddText(parent, name, text, size, color, TextAlignmentOptions.MidlineLeft);
        LayoutElement le = tmp.gameObject.AddComponent<LayoutElement>();
        if (preferredWidth > 0f) { le.preferredWidth = preferredWidth; }
        if (flexible) { le.flexibleWidth = 1f; }
        return tmp;
    }

    private static GameObject MakePanel(Transform parent, string name, float w, float h)
    {
        GameObject panel = NewUI(name, parent);
        RectTransform rt = (RectTransform)panel.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        Image img = panel.AddComponent<Image>();
        img.sprite = Sprite(SpritePanel);
        img.type = Image.Type.Sliced;
        img.raycastTarget = true;
        return panel;
    }

    private static Button MakeButton(Transform parent, string name, string text, float w, float h, int fontSize)
    {
        GameObject go = NewUI(name, parent);
        Image img = go.AddComponent<Image>();
        img.sprite = Sprite(SpriteButton);
        img.type = Image.Type.Sliced;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = img;
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState state = new SpriteState
        {
            highlightedSprite = Sprite(SpriteButtonHighlight),
            pressedSprite = Sprite(SpriteButtonPressed),
            disabledSprite = Sprite(SpriteButtonDisabled),
            selectedSprite = Sprite(SpriteButtonHighlight),
        };
        button.spriteState = state;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;
        le.minHeight = h;
        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(w, h);

        TextMeshProUGUI label = AddText(go.transform, "Text", text, fontSize, Orange, TextAlignmentOptions.Center);
        Stretch(label.gameObject);
        RectTransform lrt = label.rectTransform;
        if (w <= SmallButtonW)
        {
            // A one-glyph caption (+, x, ...): the game's font sets a taller line than the placeholder, and
            // Truncate drops a line that does not fit, which left + blank in game. No margins, no wrap,
            // allowed to overflow.
            label.overflowMode = TextOverflowModes.Overflow;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return button;
        }
        lrt.offsetMin = new Vector2(6f, 2f);
        lrt.offsetMax = new Vector2(-6f, -2f);
        return button;
    }

    private static void PlaceNavButton(Button button, Vector2 anchor, Vector2 position)
    {
        Object.DestroyImmediate(button.GetComponent<LayoutElement>());
        RectTransform rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(anchor.x, 0f);
        rt.anchoredPosition = position;
    }

    private static Toggle MakeToggle(Transform parent, string name, float size)
    {
        GameObject go = NewUI(name, parent);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = size;
        le.preferredHeight = size;
        le.minWidth = size;
        le.minHeight = size;

        GameObject bg = NewUI("Background", go.transform);
        Stretch(bg);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.sprite = Sprite(SpriteField);
        bgImg.type = Image.Type.Sliced;
        bgImg.color = DarkField;

        GameObject check = NewUI("Checkmark", bg.transform);
        Stretch(check);
        RectTransform crt = (RectTransform)check.transform;
        crt.offsetMin = new Vector2(3f, 3f);
        crt.offsetMax = new Vector2(-3f, -3f);
        Image checkImg = check.AddComponent<Image>();
        checkImg.sprite = Sprite(SpriteCheck);
        checkImg.preserveAspect = true;
        checkImg.raycastTarget = false;

        Toggle toggle = go.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = false;
        return toggle;
    }

    private static Slider MakeSlider(Transform parent, string name, float width)
    {
        GameObject go = NewUI(name, parent);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.preferredHeight = 20f;
        le.flexibleWidth = 0.5f;

        GameObject bg = NewUI("Background", go.transform);
        RectTransform bgRT = (RectTransform)bg.transform;
        bgRT.anchorMin = new Vector2(0f, 0.3f);
        bgRT.anchorMax = new Vector2(1f, 0.7f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.sprite = Sprite(SpriteField);
        bgImg.type = Image.Type.Sliced;
        bgImg.color = DarkField;

        GameObject fillArea = NewUI("Fill Area", go.transform);
        RectTransform faRT = (RectTransform)fillArea.transform;
        faRT.anchorMin = new Vector2(0f, 0.3f);
        faRT.anchorMax = new Vector2(1f, 0.7f);
        faRT.offsetMin = new Vector2(8f, 0f);
        faRT.offsetMax = new Vector2(-8f, 0f);

        GameObject fill = NewUI("Fill", fillArea.transform);
        RectTransform fillRT = (RectTransform)fill.transform;
        fillRT.sizeDelta = new Vector2(10f, 0f);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.72f, 0.6f, 0.38f, 0.85f);
        fillImg.raycastTarget = false;

        GameObject handleArea = NewUI("Handle Slide Area", go.transform);
        RectTransform haRT = (RectTransform)handleArea.transform;
        haRT.anchorMin = Vector2.zero;
        haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(8f, 0f);
        haRT.offsetMax = new Vector2(-8f, 0f);

        GameObject handle = NewUI("Handle", handleArea.transform);
        RectTransform hRT = (RectTransform)handle.transform;
        hRT.sizeDelta = new Vector2(16f, 0f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.sprite = Sprite(SpriteButton);
        handleImg.type = Image.Type.Sliced;

        Slider slider = go.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = hRT;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        return slider;
    }

    private static TMP_InputField MakeInputField(Transform parent, string name, float w, float h, TMP_InputField.ContentType contentType, TextAlignmentOptions alignment)
    {
        GameObject go = NewUI(name, parent);
        Image bg = go.AddComponent<Image>();
        bg.sprite = Sprite(SpriteField);
        bg.type = Image.Type.Sliced;
        bg.color = DarkField;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = w;
        le.preferredHeight = h;
        le.minHeight = h;
        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(w, h);

        GameObject area = NewUI("Text Area", go.transform);
        Stretch(area);
        RectTransform art = (RectTransform)area.transform;
        art.offsetMin = new Vector2(8f, 3f);
        art.offsetMax = new Vector2(-8f, -3f);
        area.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = AddText(area.transform, "Placeholder", "", (int)Mathf.Max(11f, h * 0.5f), Placeholder, alignment);
        Stretch(placeholder.gameObject);
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.overflowMode = TextOverflowModes.Overflow;

        TextMeshProUGUI text = AddText(area.transform, "Text", "", (int)Mathf.Max(11f, h * 0.5f), FieldText, alignment);
        Stretch(text.gameObject);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;

        TMP_InputField field = go.AddComponent<TMP_InputField>();
        field.targetGraphic = bg;
        field.textViewport = art;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.contentType = contentType;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.pointSize = text.fontSize;
        field.caretColor = FieldText;
        field.customCaretColor = true;
        field.selectionColor = new Color(0.72f, 0.6f, 0.38f, 0.5f);
        return field;
    }

    private static void SetPlaceholder(TMP_InputField field, string text)
    {
        TextMeshProUGUI placeholder = field.placeholder as TextMeshProUGUI;
        if (placeholder != null) { placeholder.text = text; }
    }

    // A vertical scroll list. Returns the content transform the caller fills.
    private static Transform MakeScrollList(GameObject holder)
    {
        Image bg = holder.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);
        bg.raycastTarget = true;
        ScrollRect scroll = holder.AddComponent<ScrollRect>();

        GameObject viewport = NewUI("Viewport", holder.transform);
        Stretch(viewport);
        RectTransform vrt = (RectTransform)viewport.transform;
        vrt.offsetMin = new Vector2(4f, 4f);
        // Room on the right for the scrollbar, so it never covers a row's last widget.
        vrt.offsetMax = new Vector2(-(ScrollbarW + 6f), -4f);
        viewport.AddComponent<RectMask2D>();
        Image vimg = viewport.AddComponent<Image>();
        vimg.color = new Color(0f, 0f, 0f, 0f);

        GameObject content = NewUI("Content", viewport.transform);
        RectTransform crt = (RectTransform)content.transform;
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = vrt;
        scroll.content = crt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        // The mod raises this and pins the handle size at runtime (QuickConfigStyle); see there why.
        scroll.scrollSensitivity = 800f;
        scroll.verticalScrollbar = MakeScrollbar(holder.transform);
        // Shown whenever the list holds more than fits, so it is obvious there is more to see.
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return content.transform;
    }

    // The merchant panel's scrollbar: a thin dark track on the right edge, an orange item_background handle.
    private const float ScrollbarW = 8f;
    private static readonly Color ScrollTrack = new Color(0.19215687f, 0.1254902f, 0.078431375f, 1f);

    private static Scrollbar MakeScrollbar(Transform holder)
    {
        GameObject bar = NewUI("Scrollbar", holder);
        RectTransform rt = (RectTransform)bar.transform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-3f, -4f);
        rt.sizeDelta = new Vector2(ScrollbarW, -8f);
        Image track = bar.AddComponent<Image>();
        track.sprite = Sprite(SpriteField);
        track.type = Image.Type.Sliced;
        track.color = ScrollTrack;

        GameObject area = NewUI("Sliding Area", bar.transform);
        Stretch(area);
        GameObject handle = NewUI("Handle", area.transform);
        Stretch(handle);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = Sprite(SpriteField);
        handleImage.type = Image.Type.Sliced;

        Scrollbar scrollbar = bar.AddComponent<Scrollbar>();
        scrollbar.handleRect = (RectTransform)handle.transform;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = scrollbar.colors;
        colors.normalColor = new Color(0.9254902f, 0.64705884f, 0.34117648f, 1f);
        colors.highlightedColor = new Color(1f, 0.78431374f, 0.09019608f, 1f);
        colors.pressedColor = new Color(0.8392157f, 0.64705884f, 0.03137255f, 1f);
        colors.selectedColor = colors.highlightedColor;
        scrollbar.colors = colors;
        // Kept out of gamepad navigation, so the D-pad never stops on it between two rows.
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
        return scrollbar;
    }

    // ------------------------------------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------------------------------------

    private static void EnsureFolders()
    {
        foreach (string dir in new[] { Root, RowsDir, PagesDir })
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                string parent = Path.GetDirectoryName(dir).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(dir));
            }
        }
    }

    private static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
        Object.DestroyImmediate(go);
        if (!ok) { Debug.LogError("[QuickConfig] Failed to save " + path); }
    }

    private static Sprite Sprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) { Debug.LogWarning("[QuickConfig] Sprite not found: " + path); }
        return sprite;
    }

    // Asks each assembly for the one type by name rather than enumerating all of its types: a plugin
    // DLL with any unresolvable reference makes GetTypes() throw for the whole assembly even though the
    // one type wanted would load fine.
    private static Type TypeByName(string fullName)
    {
        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type match = null;
            try { match = assembly.GetType(fullName, false); }
            catch { /* an assembly that cannot resolve its references is not the one we want */ }
            if (match != null) { return match; }
        }
        return null;
    }

    private static Component AddByTypeName(GameObject go, string fullName)
    {
        Type type = TypeByName(fullName);
        if (type == null)
        {
            Debug.LogWarning("[QuickConfig] Component type not loaded: " + fullName + " (is ExternalLibraries/EpicLoot.dll current?)");
            return null;
        }
        return go.AddComponent(type);
    }

    private static void SetField(Component component, string field, string value)
    {
        if (component == null) { return; }
        System.Reflection.FieldInfo info = component.GetType().GetField(field);
        if (info != null) { info.SetValue(component, value); }
    }
}
