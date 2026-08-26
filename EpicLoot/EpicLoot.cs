using BepInEx;
using Common;
using EpicLoot.Adventure;
using EpicLoot.Config;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using EpicLoot.Data;
using EpicLoot.GatedItemType;
using EpicLoot.General;
using EpicLoot.Magic;
using EpicLoot.Magic.MagicItemEffects.Helpers;
using EpicLoot.MagicItemEffects;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot;

[BepInPlugin(PluginId, DisplayName, Version)]
[BepInDependency(Jotunn.Main.ModGuid)]
[BepInDependency("com.ValheimModding.NewtonsoftJsonDetector")]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
[BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("vapok.mods.adventurebackpacks", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("kg.ValheimEnchantmentSystem", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("org.bepinex.plugins.steadyregeneration", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class EpicLoot : BaseUnityPlugin {
    public const string PluginId = "randyknapp.mods.epicloot";
    public const string DisplayName = "Epic Loot";
    public const string Version = "0.13.3";

    private static string ConfigFileName = PluginId + ".cfg";
    private static string ConfigFileFullPath = BepInEx.Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

    public static readonly Dictionary<string, string> MagicItemColors = new Dictionary<string, string>()
    {
        { "Red",    "#ff4545" },
        { "Orange", "#ffac59" },
        { "Yellow", "#ffff75" },
        { "Green",  "#80fa70" },
        { "Teal",   "#18e7a9" },
        { "Blue",   "#00abff" },
        { "Indigo", "#709bba" },
        { "Purple", "#d078ff" },
        { "Pink",   "#ff63d6" },
        { "Gray",   "#dbcadb" },
    };

    public static string[] MagicMaterials = new string[]
    {
        "Runestone",
        "EtchedRunestone",
        "Shard",
        "Dust",
        "Reagent",
        "Essence"
    };

    /// <summary>
    /// Plain, non-craftable items registered straight from the bundle. Craftable items are declared in
    /// <see cref="LoadCraftableItems"/> instead, so their recipe and station are config driven.
    /// </summary>
    public static string[] ItemNames = new string[]
    {
        "ForestToken",
        "IronBountyToken",
        "GoldBountyToken"
    };

    public static bool AlwaysDropCheat = false;
    public const Minimap.PinType BountyPinType = (Minimap.PinType)800;
    public const Minimap.PinType TreasureMapPinType = (Minimap.PinType)801;
    public static bool HasAuga;
    public static bool AugaTooltipNoTextBoxes;

    public static event Action AbilitiesInitialized;
    public static event Action LootTableLoaded;

    private static EpicLoot _instance;
    private Harmony _harmony;
    private float _worldLuckFactor;
    internal ELConfig cfg;

    [UsedImplicitly]
    void Awake() {
        _instance = this;

        // Wire the shared Common support layer (config binder, piece loader, drawers) to this plugin before
        // any config is bound, so ConfigBinder and ModLogger have a config file and log source to use.
        ModContext.Initialize(this, Logger, "EpicLoot");

        cfg = new ELConfig(Config);

        // Set the referenced common logger to the EL specific reference so that common things get logged
        PrefabCreator.Logger = Logger;
        InitializeAbilities();
        AddLocalizations();
        LoadAssets();
        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);

        LootTableLoaded?.Invoke();
        RegisterMagicEffectEvents();

        // Scheduled (non-per-frame) effect drivers, on their own DontDestroyOnLoad objects.
        MagicItemEffects.Shards.PoisonAdrenalinePulse.Create();
        MagicItemEffects.Shards.StormFuryPulse.Create();

        // Main file config watcher
        SetupWatcher();
    }

    private static void RegisterMagicEffectEvents() {
        // Register per-effect tooltip display-value providers (effects that show more than one number).
        // These are keyed by effect-type constant and don't depend on config load, so register once here.
        MagicItemEffects.BulkupEffect.RegisterDisplayValues();
        Magic.MagicItemEffects.DartingThoughts.RegisterDisplayValues();
        MagicItemEffects.Shards.HealthGainPerXDamageDone.RegisterDisplayValues();
        MagicItemEffects.Shards.HealthOnEitrUse.RegisterDisplayValues();
        MagicItemEffects.Shards.Mercenary.RegisterDisplayValues();
        MagicItemEffects.Shards.Coinplated.RegisterDisplayValues();
        MagicItemEffects.Shards.Wager.RegisterDisplayValues();
        MagicItemEffects.Shards.ChanceToCritOnHit.RegisterDisplayValues();
        MagicItemEffects.Shards.BloodDrinker.RegisterDisplayValues();
        MagicItemEffects.Shards.TravelLight.RegisterDisplayValues();
        MagicItemEffects.Shards.PerfectDodge.RegisterDisplayValues();
        MagicItemEffects.Shards.AdrenalineFrostWave.RegisterDisplayValues();
        MagicItemEffects.Shards.AdrenalineIncreasesHealthRegen.RegisterDisplayValues();
        MagicItemEffects.Shards.GainAdrenalineWhenApplyingPoison.RegisterDisplayValues();
        MagicItemEffects.Shards.StormFury.RegisterDisplayValues();
        MagicItemEffects.Shards.LuckWhileFishing.RegisterDisplayValues();
        MagicItemEffects.Shards.Kindling.RegisterDisplayValues();
        MagicItemEffects.Shards.Conduit.RegisterDisplayValues();
        MagicItemEffects.Shards.Inspiration.RegisterDisplayValues();
        MagicItemEffects.Shards.LuckyLoot.RegisterDisplayValues();
        MagicItemEffects.Shards.Bloodrage.RegisterDisplayValues();
        MagicItemEffects.Shards.MeteorSummoner.RegisterDisplayValues();
        MagicItemEffects.Shards.BlockAsDodgeAsBlock.RegisterDisplayValues();
        MagicItemEffects.Shards.BlockAsWoodCuttingAndPickaxes.RegisterDisplayValues();

        // This needs to not run until after the game is loaded, otherwise it will not be able to find the ObjectDB
        MagicItemEffectDefinitions.OnSetupMagicItemEffectDefinitions += Riches_CharacterDrop_GenerateDropList_Patch.UpdateRichesOnEffectSetup;

        // Register definitions for the Shardstone-only effect types (blank tooltips + warnings otherwise).
        // Re-runs on every config (re)load; the defensive call covers the case where the effect config was
        // already loaded (during ELConfig construction) before this subscription was added.
        MagicItemEffectDefinitions.OnSetupMagicItemEffectDefinitions += ShardEffectDefinitions.RegisterShardEffectDefinitions;
        if (MagicItemEffectDefinitions.AllDefinitions.Count > 0) {
            ShardEffectDefinitions.RegisterShardEffectDefinitions();
        }

        // Fold the ShardStone recipes into the material conversions (enchanting "Convert Materials" tab).
        // Same event/defensive pattern as the shard effect definitions above: re-runs on every
        // material-conversions (re)load, with a defensive call if that config was already loaded before
        // this subscription was added.
        CraftingV2.MaterialConversions.OnSetupMaterialConversions += ShardStones.ShardStoneConversions.Merge;
        if (CraftingV2.MaterialConversions.Config != null) {
            ShardStones.ShardStoneConversions.Merge();
        }
    }


    //sealed void Start()
    //{
    //    //HasAuga = Auga.API.IsLoaded();

    //    //if (HasAuga)
    //    //{
    //    //    Auga.API.ComplexTooltip_AddItemTooltipCreatedListener(ExtendAugaTooltipForMagicItem);
    //    //    Auga.API.ComplexTooltip_AddItemStatPreprocessor(AugaTooltipPreprocessor.PreprocessTooltipStat);
    //    //}
    //}

    //public static void ExtendAugaTooltipForMagicItem(GameObject complexTooltip, ItemDrop.ItemData item)
    //{
    //    //Auga.API.ComplexTooltip_SetTopic(complexTooltip, Localization.instance.Localize(item.GetDecoratedName()));

    //    var isMagic = item.IsMagic(out var magicItem);

    //    var inFront = true;
    //    var itemBG = complexTooltip.transform.Find("Tooltip/IconHeader/IconBkg/Item");
    //    if (itemBG == null)
    //    {
    //        itemBG = complexTooltip.transform.Find("InventoryElement/icon");
    //        inFront = false;
    //    }

    //    RectTransform magicBG = null;
    //    if (itemBG != null)
    //    {
    //        var itemBGImage = itemBG.GetComponent<Image>();
    //        magicBG = (RectTransform)itemBG.transform.Find("magicItem");
    //        if (magicBG == null)
    //        {
    //            var magicItemObject = Instantiate(itemBGImage, inFront ?
    //                itemBG.transform : itemBG.transform.parent).gameObject;
    //            magicItemObject.name = "magicItem";
    //            magicItemObject.SetActive(true);
    //            magicBG = (RectTransform)magicItemObject.transform;
    //            magicBG.anchorMin = Vector2.zero;
    //            magicBG.anchorMax = new Vector2(1, 1);
    //            magicBG.sizeDelta = Vector2.zero;
    //            magicBG.pivot = new Vector2(0.5f, 0.5f);
    //            magicBG.anchoredPosition = Vector2.zero;
    //            var magicItemInit = magicBG.GetComponent<Image>();
    //            magicItemInit.color = Color.white;
    //            magicItemInit.raycastTarget = false;
    //            magicItemInit.sprite = GetMagicItemBgSprite();

    //            if (!inFront)
    //            {
    //                magicBG.SetSiblingIndex(0);
    //            }
    //        }
    //    }

    //    if (magicBG != null)
    //    {
    //        magicBG.gameObject.SetActive(isMagic);
    //    }

    //    if (item.IsMagicCraftingMaterial())
    //    {
    //        var rarity = item.GetCraftingMaterialRarity();
    //        //Auga.API.ComplexTooltip_SetIcon(complexTooltip, item.m_shared.m_icons[GetRarityIconIndex(rarity)]);
    //    }

    //    if (isMagic)
    //    {
    //        var magicColor = magicItem.GetColorString();
    //        var itemTypeName = magicItem.GetItemTypeName(item.Extended());

    //        if (magicBG != null)
    //        {
    //            magicBG.GetComponent<Image>().color = item.GetRarityColor();
    //        }

    //        //Auga.API.ComplexTooltip_SetIcon(complexTooltip, item.GetIcon());

    //        string localizedSubtitle;
    //        if (item.IsLegendarySetItem())
    //        {
    //            localizedSubtitle = $"<color={GetSetItemColor()}>" +
    //                $"$mod_epicloot_legendarysetlabel</color>, {itemTypeName}\n";
    //        }
    //        else
    //        {
    //            localizedSubtitle = $"<color={magicColor}>{magicItem.GetRarityDisplay()} {itemTypeName}</color>";
    //        }

    //        try
    //        {
    //            //Auga.API.ComplexTooltip_SetSubtitle(complexTooltip, Localization.instance.Localize(localizedSubtitle));
    //        }
    //        catch (Exception)
    //        {
    //            //Auga.API.ComplexTooltip_SetSubtitle(complexTooltip, localizedSubtitle);
    //        }

    //        if (AugaTooltipNoTextBoxes)
    //            return;

    //        //Don't need to process the InventoryTooltip Information.
    //        if (complexTooltip.name.Contains("InventoryTooltip"))
    //            return;

    //        //The following is used only for Crafting Result Panel.
    //        Auga.API.ComplexTooltip_AddDivider(complexTooltip);

    //        var magicItemText = magicItem.GetTooltip();
    //        var textBox = Auga.API.ComplexTooltip_AddTwoColumnTextBox(complexTooltip);
    //        magicItemText = magicItemText.Replace("\n\n", "");
    //        Auga.API.TooltipTextBox_AddLine(textBox, magicItemText);

    //        if (magicItem.IsLegendarySetItem())
    //        {
    //            var textBox2 = Auga.API.ComplexTooltip_AddTwoColumnTextBox(complexTooltip);
    //            Auga.API.TooltipTextBox_AddLine(textBox2, item.GetSetTooltip());
    //        }

    //        try
    //        {
    //            Auga.API.ComplexTooltip_SetDescription(complexTooltip,
    //                Localization.instance.Localize(item.GetDescription()));
    //        }
    //        catch (Exception)
    //        {
    //            Auga.API.ComplexTooltip_SetDescription(complexTooltip, item.GetDescription());
    //        }
    //    }
    //}

    private void AddLocalizations() {
        CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
        // load all localization files within the localizations directory
        Log("Loading Localizations.");
        foreach (string embeddedResouce in typeof(EpicLoot).Assembly.GetManifestResourceNames()) {
            if (!embeddedResouce.Contains("localizations")) { continue; }
            string localization = ReadEmbeddedResourceFile(embeddedResouce);
            // This will clean comments out of the localization files. Full-line comments only: the
            // old pattern also truncated any line containing "//" inside a value (URLs in
            // translations), corrupting the JSON.
            string cleaned_localization = Regex.Replace(localization, @"^\s*\/\/.*$", "", RegexOptions.Multiline);
            // Log($"Cleaned Localization: {cleaned_localization}");
            var name = embeddedResouce.Split('.');
            Log($"Adding localization: {name[2]}");
            Localization.AddJsonFile(name[2], cleaned_localization);
        }
        // Load the localization patches and additional languages
        ELConfig.StartupProcessModifiedLocalizations();
    }

    private static void InitializeAbilities() {
        MagicEffectType.Initialize();
        AbilitiesInitialized?.Invoke();
    }

    private static BepInEx.Logging.ManualLogSource _fallbackLogSink;

    /// <summary>
    /// Where every Log* below writes. Falls back to a standalone log source when <see cref="_instance"/>
    /// is null or has been destroyed, so logging survives outside the plugin's own lifetime.
    /// </summary>
    private static BepInEx.Logging.ManualLogSource LogSink =>
        _instance != null
            ? _instance.Logger
            : _fallbackLogSink ??= BepInEx.Logging.Logger.CreateLogSource(DisplayName);

    /// <summary>
    /// Gate for the level-filtered Log* helpers. Both config entries are bound in
    /// ELConfig.CreateConfigValues, but these helpers run from Harmony prefixes on hot vanilla methods
    /// (Inventory.AddItem among them), so an unguarded deref here does not merely lose a log line - it
    /// throws out of the vanilla method that was running. An unbound config logs rather than staying
    /// silent, since that state is itself worth seeing.
    /// </summary>
    private static bool ShouldLog(LogLevel level) {
        BepInEx.Configuration.ConfigEntry<bool> enabled = ELConfig._loggingEnabled;
        BepInEx.Configuration.ConfigEntry<LogLevel> threshold = ELConfig._logLevel;
        return enabled == null || threshold == null || (enabled.Value && threshold.Value <= level);
    }

    public static void Log(string message) {
        if (ShouldLog(LogLevel.Info)) {
            LogSink.LogInfo(message);
        }
    }

    public static void LogWarning(string message) {
        if (ShouldLog(LogLevel.Warning)) {
            LogSink.LogWarning(message);
        }
    }

    public static void LogError(string message) {
        if (ShouldLog(LogLevel.Error)) {
            LogSink.LogError(message);
        }
    }

    public static void LogForce(string message) {
        // Intentionally NOT gated by _loggingEnabled/_logLevel: some diagnostics must always be visible.
        LogSink.LogInfo(message);
    }

    public static void LogWarningForce(string message) {
        LogSink.LogWarning(message);
    }

    public static void LogErrorForce(string message) {
        LogSink.LogError(message);
    }

    private static void LoadAssets() {
        var assetBundle = LoadAssetBundle("epicloot");

        if (assetBundle == null) {
            LogErrorForce("Unable to load asset bundle! This mod will not behave as expected!");
            return;
        }

        EpicAssets.AssetBundle = assetBundle;
        // Shared Common loaders (PieceLoader) resolve their prefabs through here.
        ModContext.AssetBundle = assetBundle;
        EpicAssets.EquippedSprite = assetBundle.LoadAsset<Sprite>("Equipped");
        EpicAssets.AugaEquippedSprite = assetBundle.LoadAsset<Sprite>("AugaEquipped");
        EpicAssets.GenericSetItemSprite = assetBundle.LoadAsset<Sprite>("GenericSetItemMarker");
        EpicAssets.AugaSetItemSprite = assetBundle.LoadAsset<Sprite>("AugaSetItem");
        EpicAssets.GenericItemBgSprite = assetBundle.LoadAsset<Sprite>("GenericItemBg");
        EpicAssets.AugaItemBgSprite = assetBundle.LoadAsset<Sprite>("AugaItemBG");
        EpicAssets.SmallButtonEnchantOverlay = assetBundle.LoadAsset<Sprite>("SmallButtonEnchantOverlay");
        EpicAssets.DodgeBuffSprite = assetBundle.LoadAsset<Sprite>("DodgeBuff");
        EpicAssets.MagicItemLootBeamPrefabs[(int)ItemRarity.Magic] = assetBundle.LoadAsset<GameObject>("MagicLootBeam");
        EpicAssets.MagicItemLootBeamPrefabs[(int)ItemRarity.Rare] = assetBundle.LoadAsset<GameObject>("RareLootBeam");
        EpicAssets.MagicItemLootBeamPrefabs[(int)ItemRarity.Epic] = assetBundle.LoadAsset<GameObject>("EpicLootBeam");
        EpicAssets.MagicItemLootBeamPrefabs[(int)ItemRarity.Legendary] = assetBundle.LoadAsset<GameObject>("LegendaryLootBeam");
        EpicAssets.MagicItemLootBeamPrefabs[(int)ItemRarity.Mythic] = assetBundle.LoadAsset<GameObject>("MythicLootBeam");

        EpicAssets.MagicItemDropSFX[(int)ItemRarity.Magic] = assetBundle.LoadAsset<AudioClip>("MagicItemDrop");
        EpicAssets.MagicItemDropSFX[(int)ItemRarity.Rare] = assetBundle.LoadAsset<AudioClip>("RareItemDrop");
        EpicAssets.MagicItemDropSFX[(int)ItemRarity.Epic] = assetBundle.LoadAsset<AudioClip>("EpicItemDrop");
        EpicAssets.MagicItemDropSFX[(int)ItemRarity.Legendary] = assetBundle.LoadAsset<AudioClip>("LegendaryItemDrop");
        EpicAssets.MagicItemDropSFX[(int)ItemRarity.Mythic] = assetBundle.LoadAsset<AudioClip>("MythicItemDrop");
        EpicAssets.ItemLoopSFX = assetBundle.LoadAsset<AudioClip>("ItemLoop");
        EpicAssets.AugmentItemSFX = assetBundle.LoadAsset<AudioClip>("AugmentItem");

        EpicAssets.MerchantPanel = assetBundle.LoadAsset<GameObject>("MerchantPanel");
        EpicAssets.TemperPanel = assetBundle.LoadAsset<GameObject>("TemperPanel");

        EpicAssets.MapIconTreasureMap = assetBundle.LoadAsset<Sprite>("TreasureMapIcon");
        EpicAssets.MapIconBounty = assetBundle.LoadAsset<Sprite>("MapIconBounty");
        EpicAssets.AbandonBountySFX = assetBundle.LoadAsset<AudioClip>("AbandonBounty");
        EpicAssets.DoubleJumpSFX = assetBundle.LoadAsset<AudioClip>("DoubleJump");
        EpicAssets.OffSetSFX = assetBundle.LoadAsset<AudioClip>("sfx_offset");
        EpicAssets.DebugTextPrefab = assetBundle.LoadAsset<GameObject>("DebugText");
        EpicAssets.AbilityBar = assetBundle.LoadAsset<GameObject>("AbilityBar");
        EpicAssets.WelcomMessagePrefab = assetBundle.LoadAsset<GameObject>("WelcomeMessage");
        EpicAssets.ConfigMessagePrefab = assetBundle.LoadAsset<GameObject>("ConfigMessage");
        EpicAssets.SocketMessagePrefab = assetBundle.LoadAsset<GameObject>("SocketMessage");

        // Register the frost-cone AOE effect (Ragnar set / FrostDamageAOE) so its ZNetView networks
        // properly, and route its ripped SFX through the volume mixer once AudioMan exists (below).
        EpicAssets.IceSpikesVFX = assetBundle.LoadAsset<GameObject>(FrostAOE.Attack_DoMeleeAttack_Transpiler.FxPrefabName);
        PrefabManager.Instance.AddPrefab(new CustomPrefab(EpicAssets.IceSpikesVFX, false));

        EpicAssets.BulwarkStatusEffect = assetBundle.LoadAsset<SE_Stats>(EpicAssets.Bulwark_SE_Name);
        EpicAssets.BulwarkMagicShieldVFX = assetBundle.LoadAsset<GameObject>("MagicShield");
        EpicAssets.BulwarkMagicShieldSFX = assetBundle.LoadAsset<GameObject>("sfx_bulwark");

        EpicAssets.UndyingStatusEffect = assetBundle.LoadAsset<SE_Stats>(EpicAssets.Undying_SE_Name);
        EpicAssets.UndyingVFX = assetBundle.LoadAsset<GameObject>("Undying");
        EpicAssets.UndyingSFX = assetBundle.LoadAsset<GameObject>("sfx_undying");

        EpicAssets.BerserkerStatusEffect = assetBundle.LoadAsset<SE_Stats>(EpicAssets.Berserker_SE_Name);
        EpicAssets.BerserkerVFX = assetBundle.LoadAsset<GameObject>("Berserker");
        EpicAssets.BerserkerSFX = assetBundle.LoadAsset<GameObject>("sfx_berserker");

        EpicAssets.DodgeBuffStatusEffect = assetBundle.LoadAsset<SE_Stats>(EpicAssets.DodgeBuff_SE_Name);
        EpicAssets.DodgeBuffSFX = assetBundle.LoadAsset<GameObject>("sfx_dodgebuff");

        GameObject explosiveArrow = assetBundle.LoadAsset<GameObject>(EpicAssets.ExplosiveArrow);
        PrefabManager.Instance.AddPrefab(new CustomPrefab(explosiveArrow, true));

        LoadCraftingMaterialAssets();

        LoadPieces();
        LoadItems();
        LoadBountySpawner();
        RegisterStatusEffects();

        PrefabManager.OnPrefabsRegistered += SetupAndvaranaut;
        // Runs during ZNetScene setup (after IceSpikes is registered, while AudioMan exists) so the
        // frost-cone SFX is routed through the volume mixer instead of playing at full volume.
        PrefabManager.OnPrefabsRegistered += FrostAOE.HookUpIceSpikesAudio;
        // Registers our player-faction, tamed clone of the vanilla 'Bat' into ZNetScene on every client each
        // world load (fires as a ZNetScene.Awake postfix), so the SummonBat trinket shard can spawn a
        // reload-safe pet that stays friendly. Idempotent -- built once and re-injected each ZNetScene.
        PrefabManager.OnPrefabsRegistered += MagicItemEffects.Shards.SummonBatWhenActivatingAdrenaline.RegisterTamedBatPrefab;
        // The Stormcaller and Firewalker unique shards carry their whole effect in a prefab clone built at
        // ZNetScene setup: a damage-free copy of the vanilla lightning AOE, and the burning patch the fire
        // trail drops. Without these two lines each effect logs a missing-prefab warning once and then does
        // nothing at all, so they are load-bearing, not cosmetic.
        PrefabManager.OnPrefabsRegistered += MagicItemEffects.Shards.StrikeCausesLightning.RegisterVisualPrefab;
        PrefabManager.OnPrefabsRegistered += MagicItemEffects.Shards.Trailblazer.RegisterVfxPrefab;
        ItemManager.OnItemsRegistered += SetupStatusEffects;
        LoadUnidentifiedItems();
        ShardStones.Shards.CreateAndLoadShardItems();
        LoadShardSlotChisels();
        // Needs to trigger late in order to get all potentially added items by other mods.
        // Subscribed via the stored handler so the self-unsubscribe inside actually matches.
        MinimapManager.OnVanillaMapDataLoaded += AutoAddEnchantableItems.OnMapDataLoadedHandler;

        EpicAssets.AssertAssetIntegrety();
    }

    public static T LoadAsset<T>(string assetName) where T : Object {
        try {
            if (EpicAssets.AssetCache.ContainsKey(assetName)) {
                return (T)EpicAssets.AssetCache[assetName];
            }

            var asset = EpicAssets.AssetBundle.LoadAsset<T>(assetName);
            EpicAssets.AssetCache.Add(assetName, asset);
            return asset;
        } catch (Exception e) {
            LogErrorForce($"Error loading asset ({assetName}): {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Registers the build pieces through the shared <see cref="PieceLoader"/>, which exposes each piece's
    /// build cost, category and workbench requirement as server-synced config entries that apply live.
    /// None of these pieces required a crafting station before, hence RequiresWorkbench = false.
    /// </summary>
    private static void LoadPieces() {
        PieceLoader.Register(new PieceLoader.BuildPiece {
            Name = "Enchanter",
            Prefab = "piece_enchanter",
            Category = PieceCategories.Misc,
            RequiresWorkbench = false,
            AllowedInDungeons = false,
            Enabled = false,
            PieceCost = new List<PieceLoader.PieceCost>
            {
                new PieceLoader.PieceCost { Prefab = "Stone", Amount = 10, Refundable = true },
                new PieceLoader.PieceCost { Prefab = "SurtlingCore", Amount = 3, Refundable = true },
                new PieceLoader.PieceCost { Prefab = "Copper", Amount = 3, Refundable = true },
                new PieceLoader.PieceCost { Prefab = "SwordCheat", Amount = 1, Refundable = false }
            }
        });

        PieceLoader.Register(new PieceLoader.BuildPiece {
            Name = "Augmenter",
            Prefab = "piece_augmenter",
            Category = PieceCategories.Misc,
            RequiresWorkbench = false,
            AllowedInDungeons = false,
            Enabled = false,
            PieceCost = new List<PieceLoader.PieceCost>
            {
                new PieceLoader.PieceCost { Prefab = "Obsidian", Amount = 10, Refundable = true },
                new PieceLoader.PieceCost { Prefab = "Crystal", Amount = 3, Refundable = true },
                new PieceLoader.PieceCost { Prefab = "Bronze", Amount = 3, Refundable = true },
                new PieceLoader.PieceCost { Prefab = "SwordCheat", Amount = 1, Refundable = false }
            }
        });

        PieceLoader.Register(new PieceLoader.BuildPiece {
            Name = "Enchanting Table",
            Prefab = "piece_enchantingtable",
            Category = PieceCategories.Crafting,
            RequiresWorkbench = false,
            AllowedInDungeons = false,
            PieceCost = new List<PieceLoader.PieceCost>
            {
                new PieceLoader.PieceCost { Prefab = "Wood", Amount = 10, Refundable = true },
                new PieceLoader.PieceCost { Prefab = "GreydwarfEye", Amount = 2, Refundable = true },
            }
        });
    }

    private static void LoadItems() {
        foreach (var item in ItemNames) {
            var go = EpicAssets.AssetBundle.LoadAsset<GameObject>(item);
            var customItem = new CustomItem(go, false);
            ItemManager.Instance.AddItem(customItem);
        }

        // Make a dummy empty game object for later use.
        GameObject dummyGO = PrefabManager.Instance.CreateEmptyPrefab(EpicAssets.DummyName, true);
        ItemDrop itemDrop = dummyGO.AddComponent<ItemDrop>();
        itemDrop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData();
        itemDrop.m_itemData.m_shared.m_name = "";
        var dummyItem = new CustomItem(dummyGO, false);
        ItemManager.Instance.AddItem(dummyItem);

        LoadCraftableItems();
    }

    /// <summary>
    /// Registers the craftable items through the shared <see cref="ItemBatchLoader"/>, which gives each one
    /// server-synced config entries for its recipe, crafting station, station level and craft amount, all
    /// applying live without a restart.
    /// </summary>
    private static void LoadCraftableItems() {
        // EpicLoot's bundle stores assets under bare names, and these prefabs are authored against the real
        // game assets (no JVLmock_ references to resolve) and carry their own icons.
        ItemBatchLoader.PrefabPathFormat = null;
        ItemBatchLoader.IconPathFormat = null;
        ItemBatchLoader.FixReferences = false;

        var loader = new ItemBatchLoader();

        loader.AddDefinition(new ItemDefinition {
            Name = "Leather Belt",
            Prefab = "LeatherBelt",
            Category = ItemCategory.Misc,
            CraftedAt = "forge",
            ReqStationlevel = 1,
            CraftAmount = 1,
            Recipe = new RecipeDefinition {
                RecipeItems = new List<RecipeIngredient>
                {
                    new RecipeIngredient { Prefab = "LeatherScraps", Amount = 4 },
                    new RecipeIngredient { Prefab = "Bronze", Amount = 1 }
                }
            }
        });

        loader.AddDefinition(new ItemDefinition {
            Name = "Silver Ring",
            Prefab = "SilverRing",
            Category = ItemCategory.Misc,
            CraftedAt = "forge",
            ReqStationlevel = 1,
            CraftAmount = 1,
            Recipe = new RecipeDefinition {
                RecipeItems = new List<RecipeIngredient>
                {
                    new RecipeIngredient { Prefab = "Silver", Amount = 1 }
                }
            }
        });

        loader.AddDefinition(new ItemDefinition {
            Name = "Gold Ruby Ring",
            Prefab = "GoldRubyRing",
            Category = ItemCategory.Misc,
            CraftedAt = "forge",
            ReqStationlevel = 1,
            CraftAmount = 1,
            Recipe = new RecipeDefinition {
                RecipeItems = new List<RecipeIngredient>
                {
                    new RecipeIngredient { Prefab = "Coins", Amount = 200 },
                    new RecipeIngredient { Prefab = "Ruby", Amount = 1 }
                }
            }
        });

        loader.BatchSetup();
    }

    private static void LoadBountySpawner() {
        GameObject bounty_spawner = EpicAssets.AssetBundle.LoadAsset<GameObject>("EL_SpawnController");

        if (bounty_spawner == null) {
            LogErrorForce("Unable to find bounty spawner asset! This mod will not behave as expected!");
        } else {
            bounty_spawner.AddComponent<AdventureSpawnController>();
            CustomPrefab prefab_obj = new CustomPrefab(bounty_spawner, false);
            PrefabManager.Instance.AddPrefab(prefab_obj);
        }
    }

    private static void LoadCraftingMaterialAssets() {
        foreach (string type in MagicMaterials) {
            foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity))) {
                string assetName = $"{type}{rarity}";
                GameObject prefab = EpicAssets.AssetBundle.LoadAsset<GameObject>(assetName);

                if (!prefab) {
                    LogErrorForce($"Tried to load asset {assetName} but it does not exist in the asset bundle!");
                    continue;
                }

                if (prefab.TryGetComponent(out ItemDrop itemDrop)) {
                    if (itemDrop.m_itemData.IsMagicCraftingMaterial()) {
                        // Set icons for crafting materials.
                        itemDrop.m_itemData.m_variant = GetRarityIconIndex(rarity);
                    }

                    // Add MagicItemComponent or products will not stack until reloaded.
                    itemDrop.m_itemData.CreateMagicItem();
                }

                CustomItem custom = new CustomItem(prefab, false);
                ItemManager.Instance.AddItem(custom);
            }
        }
    }

    // Brokkr's Gift, the consumable that adds shard slots to a magic item. Two authored prefabs loaded
    // by name, not runtime clones, so there is no deferred SetActive dance here -- and no ItemConfig
    // either: name, description and icon are all baked on the prefab. Note the single icon: do NOT set
    // m_variant, which on the crafting materials selects out of a ten-icon rarity array these lack.
    private static void LoadShardSlotChisels() {
        var chisels = new (string Prefab, ItemRarity Rarity)[] {
            (ShardStones.ShardSlotChisel.LegendaryPrefab, ItemRarity.Legendary),
            (ShardStones.ShardSlotChisel.MythicPrefab, ItemRarity.Mythic),
        };

        foreach (var (prefabName, rarity) in chisels) {
            GameObject prefab = EpicAssets.AssetBundle.LoadAsset<GameObject>(prefabName);
            if (prefab == null) {
                LogErrorForce($"Tried to load asset {prefabName} but it does not exist in the asset bundle!");
                continue;
            }

            if (prefab.TryGetComponent(out ItemDrop itemDrop)) {
                itemDrop.m_itemData.m_dropPrefab = prefab;
                // Cosmetic only: this is what colours the name and gives it the magic item background.
                itemDrop.m_itemData.SaveMagicItem(new MagicItem { Rarity = rarity });
            }

            ItemManager.Instance.AddItem(new CustomItem(prefab, false));
        }
    }

    private static void LoadUnidentifiedItems() {
        // TODO: Add support for biomes added by other mods as needed.
        GameObject genericPrefab = EpicAssets.AssetBundle.LoadAsset<GameObject>("_Unidentified");
        CustomItem genericUnidentified = new CustomItem(genericPrefab, false);
        ItemManager.Instance.AddItem(genericUnidentified);
        genericPrefab.SetActive(false);

        var unidentifiedPrefabNames = new List<string>();

        foreach (string biome in Enum.GetNames(typeof(Heightmap.Biome))) {
            if (biome == "None" || biome == "All") {
                continue;
            }

            foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity))) {
                var prefab = Object.Instantiate(genericPrefab);
                string prefabName = $"{biome}_{rarity}_Unidentified";
                prefab.name = prefabName;
                ItemDrop pid = prefab.GetComponent<ItemDrop>();
                var magicItemComponent = pid.m_itemData.Data().GetOrCreate<MagicItemComponent>();
                pid.m_itemData.m_dropPrefab = prefab;
                magicItemComponent.SetMagicItem(new MagicItem {
                    Rarity = rarity,
                    IsUnidentified = true,
                });
                magicItemComponent.Save();
                pid.Save();

                ItemConfig unidentifiedIC = new ItemConfig() {
                    Name = $"$mod_epicloot_{rarity} $mod_epicloot_unidentified_{biome}",
                    Description = "$mod_epicloot_unidentified_introduce",
                };

                CustomItem custom = new CustomItem(prefab, false, unidentifiedIC);
                ItemManager.Instance.AddItem(custom);

                unidentifiedPrefabNames.Add(prefabName);
            }
        }

        // Enable items once things are working so that ZNet issues don't happen.
        // A single idempotent handler activates every registered prefab; a null
        // lookup logs and continues so one missing prefab can't leave the rest inactive.
        void EnableUnidentifiedItems() {
            foreach (string prefabName in unidentifiedPrefabNames) {
                GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);
                if (prefab == null) {
                    LogError($"Could not find unidentified prefab '{prefabName}' to activate.");
                    continue;
                }

                prefab.SetActive(true);
                prefab.GetComponent<ItemDrop>().m_itemData.m_dropPrefab = prefab;
            }
        }

        ItemManager.OnItemsRegistered += EnableUnidentifiedItems;
    }

    private static void RegisterStatusEffects() {
        RegisterBulwark();
        RegisterUndying();
        RegisterBerserker();
        RegisterAdrenalineRush();
    }

    private static void RegisterBulwark() {
        PrefabManager.Instance.AddPrefab(EpicAssets.BulwarkMagicShieldVFX);
        PrefabManager.Instance.AddPrefab(EpicAssets.BulwarkMagicShieldSFX);
        ItemManager.OnItemsRegistered += () => ObjectDB.instance.m_StatusEffects.Add(EpicAssets.BulwarkStatusEffect);
    }

    private static void RegisterBerserker() {
        PrefabManager.Instance.AddPrefab(EpicAssets.BerserkerVFX);
        PrefabManager.Instance.AddPrefab(EpicAssets.BerserkerSFX);
        ItemManager.OnItemsRegistered += () => ObjectDB.instance.m_StatusEffects.Add(EpicAssets.BerserkerStatusEffect);
    }

    private static void RegisterUndying() {
        PrefabManager.Instance.AddPrefab(EpicAssets.UndyingVFX);
        PrefabManager.Instance.AddPrefab(EpicAssets.UndyingSFX);
        ItemManager.OnItemsRegistered += () => ObjectDB.instance.m_StatusEffects.Add(EpicAssets.UndyingStatusEffect);
    }

    private static void RegisterAdrenalineRush() {
        PrefabManager.Instance.AddPrefab(EpicAssets.DodgeBuffSFX);
        ItemManager.OnItemsRegistered += () => ObjectDB.instance.m_StatusEffects.Add(EpicAssets.DodgeBuffStatusEffect);
    }

    [UsedImplicitly]
    void OnDestroy() {
        Config.Save();
        _instance = null;
    }

    public static bool IsObjectDBReady() {
        // Hack, just making sure the built-in items and prefabs have loaded
        return ObjectDB.instance != null && ObjectDB.instance.m_items.Count != 0 &&
            ObjectDB.instance.GetItemPrefab("Amber") != null;
    }

    private static void SetupAndvaranaut() {
        var go = EpicAssets.AssetBundle.LoadAsset<GameObject>("Andvaranaut");
        ItemDrop prefab = go.GetComponent<ItemDrop>();

        var andvaranaut = prefab.m_itemData;
        var wishbone = ObjectDB.instance.GetItemPrefab("Wishbone").GetComponent<ItemDrop>().m_itemData;

        // first, create custom status effect
        var originalFinder = wishbone.m_shared.m_equipStatusEffect;
        var wishboneFinder = ScriptableObject.CreateInstance<SE_CustomFinder>();

        // Copy all values from finder
        Common.Utils.CopyFields(originalFinder, wishboneFinder, typeof(SE_Finder));
        wishboneFinder.name = "CustomWishboneFinder";

        var andvaranautFinder = ScriptableObject.CreateInstance<SE_CustomFinder>();
        Common.Utils.CopyFields(wishboneFinder, andvaranautFinder, typeof(SE_CustomFinder));
        andvaranautFinder.name = "Andvaranaut";
        andvaranautFinder.m_name = "$mod_epicloot_item_andvaranaut";
        andvaranautFinder.m_icon = andvaranaut.GetIcon();
        andvaranautFinder.m_tooltip = "$mod_epicloot_item_andvaranaut_tooltip";
        andvaranautFinder.m_startMessage = "$mod_epicloot_item_andvaranaut_startmsg";

        // Setup restrictions
        andvaranautFinder.RequiredComponentTypes = new List<Type> { typeof(TreasureMapChest), typeof(BountyTarget) };
        wishboneFinder.DisallowedComponentTypes = new List<Type> { typeof(TreasureMapChest), typeof(BountyTarget) };

        // Add to list
        ObjectDB.instance.m_StatusEffects.Remove(originalFinder);
        ObjectDB.instance.m_StatusEffects.Add(andvaranautFinder);
        ObjectDB.instance.m_StatusEffects.Add(wishboneFinder);

        // Set new status effect
        andvaranaut.m_shared.m_equipStatusEffect = andvaranautFinder;
        wishbone.m_shared.m_equipStatusEffect = wishboneFinder;

        // Setup magic item
        var magicItem = new MagicItem {
            Rarity = ItemRarity.Epic,
            TypeNameOverride = "$mod_epicloot_item_andvaranaut_type"
        };
        magicItem.Effects.Add(new MagicItemEffect(MagicEffectType.Andvaranaut));

        prefab.m_itemData.SaveMagicItem(magicItem);

        var customItem = new CustomItem(go, false);
        ItemManager.Instance.AddItem(customItem);

        PrefabManager.OnPrefabsRegistered -= SetupAndvaranaut;
    }

    // Legacy registration of an ObjectDB-visible "Paralyze" status effect. Nothing in the mod
    // consumes it -- Paralyze.cs applies its own EL_Paralyze prototype directly via SEMan -- but it
    // is kept for anything external that looks the effect up by hash. The old lookup used
    // string.GetHashCode (ObjectDB matches on GetStableHashCode), always returned null, and
    // CopyFields(null, ...) then threw a TargetException on every world load (swallowed by Jotunn's
    // SafeInvoke), so the effect was never actually registered.
    private static void SetupStatusEffects() {
        var lightning = ObjectDB.instance.GetStatusEffect("Lightning".GetStableHashCode());
        if (lightning == null) {
            LogWarning("Vanilla 'Lightning' status effect not found; skipping legacy Paralyze ObjectDB registration.");
            ItemManager.OnItemsRegistered -= SetupStatusEffects;
            return;
        }
        var paralyzed = ScriptableObject.CreateInstance<SE_Paralyzed>();
        Common.Utils.CopyFields(lightning, paralyzed, typeof(StatusEffect));
        paralyzed.name = "Paralyze";
        paralyzed.m_name = "$mod_epicloot_se_paralyze";

        ObjectDB.instance.m_StatusEffects.Add(paralyzed);
        ItemManager.OnItemsRegistered -= SetupStatusEffects;
    }

    public static AssetBundle LoadAssetBundle(string filename) {
        return AssetBundleLoader.LoadFromResources(filename, typeof(EpicLoot).Assembly);
    }

    /// <summary>
    /// This reads an embedded file resouce name, these are all resouces packed into the DLL
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    internal static string ReadEmbeddedResourceFile(string filename) {
        using (var stream = typeof(EpicLoot).Assembly.GetManifestResourceStream(filename)) {
            using (var reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }
    }

    internal static List<string> GetEmbeddedResourceNamesFromDirectory(string embedded_location = "EpicLoot.config.") {
        List<string> resourcenames = new List<string>();
        foreach (string embeddedResouce in typeof(EpicLoot).Assembly.GetManifestResourceNames()) {
            if (embeddedResouce.Contains(embedded_location)) {
                // Got to strip the starting 'EpicLoot.config.' off, so we just take the ending substring
                resourcenames.Add(embeddedResouce.Substring(16));
            }
        }
        return resourcenames;
    }

    public static bool CanBeMagicItem(ItemDrop.ItemData item) {
        return item != null
               && IsPlayerItem(item)
               && Nonstackable(item)
               && IsNotRestrictedItem(item)
               && IsAllowedMagicItemType(item);
    }

    public static bool IsAllowedMagicItemType(ItemDrop.ItemData item) {
        switch (item.m_shared.m_itemType) {
            case ItemDrop.ItemData.ItemType.Ammo:
            case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                return false;
            default:
                return item.IsEquipable();
        }
    }

    public static Sprite GetMagicItemBgSprite() {
        return HasAuga ? EpicAssets.AugaItemBgSprite : EpicAssets.GenericItemBgSprite;
    }

    public static Sprite GetEquippedSprite() {
        return HasAuga ? EpicAssets.AugaEquippedSprite : EpicAssets.EquippedSprite;
    }

    public static Sprite GetSetItemSprite() {
        return HasAuga ? EpicAssets.AugaSetItemSprite : EpicAssets.GenericSetItemSprite;
    }

    // Escapes rather than literal glyphs: this file has no BOM and a CP1252 round-trip once
    // corrupted these characters into mojibake. \u25BE = small down triangle, \u2666 = diamond suit
    // (Auga); \u25BC = down triangle, \u25C6 = black diamond (default). Alt ideas: U+1F7A0, U+1F79B.
    public static string GetMagicEffectPip(bool hasBeenAugmented) {
        return HasAuga ? (hasBeenAugmented ? "\u25BE" : "\u2666") : (hasBeenAugmented ? "\u25BC" : "\u25C6");
    }

    private static bool IsNotRestrictedItem(ItemDrop.ItemData item) {
        if (item.m_dropPrefab != null && LootRoller.Config.RestrictedItems.Contains(item.m_dropPrefab.name)) {
            return false;
        }

        return !LootRoller.Config.RestrictedItems.Contains(item.m_shared.m_name);
    }

    private static bool Nonstackable(ItemDrop.ItemData item) {
        return item.m_shared.m_maxStackSize == 1;
    }

    private static bool IsPlayerItem(ItemDrop.ItemData item) {
        // WTF, this is the only thing I found different between player usable items and items that are only for enemies
        return item.m_shared.m_icons.Length > 0;
    }

    public static string GetCharacterCleanName(Character character) {
        return character.name.Replace("(Clone)", "").Trim();
    }

    public static string GetSetItemColor() {
        return ELConfig._setItemColor.Value;
    }

    public static string GetRarityDisplayName(ItemRarity rarity) {
        switch (rarity) {
            case ItemRarity.Magic:
                return "$mod_epicloot_Magic";
            case ItemRarity.Rare:
                return "$mod_epicloot_Rare";
            case ItemRarity.Epic:
                return "$mod_epicloot_Epic";
            case ItemRarity.Legendary:
                return "$mod_epicloot_Legendary";
            case ItemRarity.Mythic:
                return "$mod_epicloot_Mythic";
            default:
                return "<non magic>";
        }
    }

    public static string GetRarityColor(ItemRarity rarity) {
        switch (rarity) {
            case ItemRarity.Magic:
                return GetColor(ELConfig._magicRarityColor.Value);
            case ItemRarity.Rare:
                return GetColor(ELConfig._rareRarityColor.Value);
            case ItemRarity.Epic:
                return GetColor(ELConfig._epicRarityColor.Value);
            case ItemRarity.Legendary:
                return GetColor(ELConfig._legendaryRarityColor.Value);
            case ItemRarity.Mythic:
                return GetColor(ELConfig._mythicRarityColor.Value);
            default:
                return "#FFFFFF";
        }
    }

    public static Color GetRarityColorARGB(ItemRarity rarity) {
        return ColorUtility.TryParseHtmlString(GetRarityColor(rarity), out var color) ? color : Color.white;
    }

    private static string GetColor(string configValue) {
        if (configValue.StartsWith("#")) {
            return configValue;
        } else {
            if (MagicItemColors.TryGetValue(configValue, out var color)) {
                return color;
            }
        }

        return "#000000";
    }

    public static int GetRarityIconIndex(ItemRarity rarity) {
        switch (rarity) {
            case ItemRarity.Magic:
                return Mathf.Clamp(ELConfig._magicMaterialIconColor.Value, 0, 9);
            case ItemRarity.Rare:
                return Mathf.Clamp(ELConfig._rareMaterialIconColor.Value, 0, 9);
            case ItemRarity.Epic:
                return Mathf.Clamp(ELConfig._epicMaterialIconColor.Value, 0, 9);
            case ItemRarity.Legendary:
                return Mathf.Clamp(ELConfig._legendaryMaterialIconColor.Value, 0, 9);
            case ItemRarity.Mythic:
                return Mathf.Clamp(ELConfig._mythicMaterialIconColor.Value, 0, 9);
            default:
                throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
        }
    }

    public static AudioClip GetMagicItemDropSFX(ItemRarity rarity) {
        return EpicAssets.MagicItemDropSFX[(int)rarity];
    }

    public static GatedItemTypeMode GetGatedItemTypeMode() {
        return ELConfig._gatedItemTypeModeConfig.Value;
    }

    public static BossDropMode GetBossTrophyDropMode() {
        return ELConfig._bossTrophyDropMode.Value;
    }

    public static float GetBossTrophyDropPlayerRange() {
        return ELConfig._bossTrophyDropPlayerRange.Value;
    }

    public static float GetBossCryptKeyPlayerRange() {
        return ELConfig._bossCryptKeyDropPlayerRange.Value;
    }

    public static BossDropMode GetBossCryptKeyDropMode() {
        return ELConfig._bossCryptKeyDropMode.Value;
    }

    public static BossDropMode GetBossWishboneDropMode() {
        return ELConfig._bossWishboneDropMode.Value;
    }

    public static float GetBossWishboneDropPlayerRange() {
        return ELConfig._bossWishboneDropPlayerRange.Value;
    }

    public static int GetAndvaranautRange() {
        return ELConfig._andvaranautRange.Value;
    }

    public static bool IsAdventureModeEnabled() {
        return ELConfig._adventureModeEnabled.Value;
    }

    public static float GetWorldLuckFactor() {
        return _instance._worldLuckFactor;
    }

    // TODO, why isn't this used?
    public static void SetWorldLuckFactor(float luckFactor) {
        _instance._worldLuckFactor = luckFactor;
    }

    private void SetupWatcher() {
        FileSystemWatcher watcher = new(BepInEx.Paths.ConfigPath, ConfigFileName);
        watcher.Changed += ReadConfigValues;
        watcher.Created += ReadConfigValues;
        watcher.Renamed += ReadConfigValues;
        watcher.IncludeSubdirectories = true;
        watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        watcher.EnableRaisingEvents = true;
    }

    private DateTime _lastReloadTime;
    private const long RELOAD_DELAY = 10000000; // One second

    private void ReadConfigValues(object sender, FileSystemEventArgs e) {
        var now = DateTime.Now;
        var time = now.Ticks - _lastReloadTime.Ticks;
        if (!File.Exists(ConfigFileFullPath) || time < RELOAD_DELAY) return;

        try {
            Log("Attempting to reload configuration...");
            Config.Reload();
        } catch {
            Log($"There was an issue loading {ConfigFileName}");
            return;
        }

        _lastReloadTime = now;
    }
}
