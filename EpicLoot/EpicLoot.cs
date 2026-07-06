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
public sealed class EpicLoot : BaseUnityPlugin
{
    public const string PluginId = "randyknapp.mods.epicloot";
    public const string DisplayName = "Epic Loot";
    public const string Version = "0.12.15";

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

    public static string[] ItemNames = new string[]
    {
        "LeatherBelt",
        "SilverRing",
        "GoldRubyRing",
        "ForestToken",
        "IronBountyToken",
        "GoldBountyToken"
    };

    public static bool AlwaysDropCheat = false;
    public const Minimap.PinType BountyPinType = (Minimap.PinType) 800;
    public const Minimap.PinType TreasureMapPinType = (Minimap.PinType) 801;
    public static bool HasAuga;
    public static bool AugaTooltipNoTextBoxes;

    public static event Action AbilitiesInitialized;
    public static event Action LootTableLoaded;

    private static EpicLoot _instance;
    private Harmony _harmony;
    private float _worldLuckFactor;
    internal ELConfig cfg;

    [UsedImplicitly]
    void Awake()
    {
        _instance = this;

        Assembly assembly = Assembly.GetExecutingAssembly();

        LoadEmbeddedAssembly(assembly, "EpicLoot-UnityLib.dll");

        cfg = new ELConfig(Config);

        // Set the referenced common logger to the EL specific reference so that common things get logged
        PrefabCreator.Logger = Logger;
        InitializeAbilities();
        AddLocalizations();
        LoadAssets();
        EnchantingUIController.Initialize();
        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);

        LootTableLoaded?.Invoke();
        RegisterMagicEffectEvents();

        TerminalCommands.AddTerminalCommands();
        // Main file config watcher
        SetupWatcher();
    }

    private static void RegisterMagicEffectEvents()
    {
        // This needs to not run until after the game is loaded, otherwise it will not be able to find the ObjectDB
        MagicItemEffectDefinitions.OnSetupMagicItemEffectDefinitions += Riches_CharacterDrop_GenerateDropList_Patch.UpdateRichesOnEffectSetup;
    }

    private static void LoadEmbeddedAssembly(Assembly assembly, string assemblyName)
    {
        var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{assemblyName}");
        if (stream == null)
        {
            LogErrorForce($"Could not load embedded assembly ({assemblyName})!");
            return;
        }

        using (stream)
        {
            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            Assembly.Load(data);
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

    private void AddLocalizations()
    {
        CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();
        // load all localization files within the localizations directory
        Log("Loading Localizations.");
        foreach (string embeddedResouce in typeof(EpicLoot).Assembly.GetManifestResourceNames())
        {
            if (!embeddedResouce.Contains("localizations")) { continue; }
            string localization = ReadEmbeddedResourceFile(embeddedResouce);
            // This will clean comments out of the localization files
            string cleaned_localization = Regex.Replace(localization, @"\/\/.*\n", "");
            // Log($"Cleaned Localization: {cleaned_localization}");
            var name = embeddedResouce.Split('.');
            Log($"Adding localization: {name[2]}");
            Localization.AddJsonFile(name[2], cleaned_localization);
        }
        // Load the localization patches and additional languages
        ELConfig.StartupProcessModifiedLocalizations();
    }

    private static void InitializeAbilities()
    {
        MagicEffectType.Initialize();
        AbilitiesInitialized?.Invoke();
    }

    public static void Log(string message)
    {
        if (ELConfig._loggingEnabled.Value && ELConfig._logLevel.Value <= LogLevel.Info)
        {
            _instance.Logger.LogInfo(message);
        }
    }

    public static void LogWarning(string message)
    {
        if (ELConfig._loggingEnabled.Value && ELConfig._logLevel.Value <= LogLevel.Warning)
        {
            _instance.Logger.LogWarning(message);
        }
    }

    public static void LogError(string message)
    {
        if (ELConfig._loggingEnabled.Value && ELConfig._logLevel.Value <= LogLevel.Error)
        {
            _instance.Logger.LogError(message);
        }
    }

    public static void LogWarningForce(string message)
    {
        _instance.Logger.LogWarning(message);
    }

    public static void LogErrorForce(string message)
    {
        _instance.Logger.LogError(message);
    }

    private static void LoadAssets()
    {
        var assetBundle = LoadAssetBundle("epicloot");

        if (assetBundle == null)
        {
            LogErrorForce("Unable to load asset bundle! This mod will not behave as expected!");
            return;
        }

        EpicAssets.AssetBundle = assetBundle;
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
        EpicAssets.MapIconTreasureMap = assetBundle.LoadAsset<Sprite>("TreasureMapIcon");
        EpicAssets.MapIconBounty = assetBundle.LoadAsset<Sprite>("MapIconBounty");
        EpicAssets.AbandonBountySFX = assetBundle.LoadAsset<AudioClip>("AbandonBounty");
        EpicAssets.DoubleJumpSFX = assetBundle.LoadAsset<AudioClip>("DoubleJump");
        EpicAssets.OffSetSFX = assetBundle.LoadAsset<AudioClip>("sfx_offset");
        EpicAssets.DebugTextPrefab = assetBundle.LoadAsset<GameObject>("DebugText");
        EpicAssets.AbilityBar = assetBundle.LoadAsset<GameObject>("AbilityBar");
        EpicAssets.WelcomMessagePrefab = assetBundle.LoadAsset<GameObject>("WelcomeMessage");

        EpicAssets.BulwarkStatusEffect = assetBundle.LoadAsset<SE_Stats>(EpicAssets.Bulwark_SE_Name);
        EpicAssets.BulwarkMagicShieldVFX = assetBundle.LoadAsset<GameObject>("MagicShield");
        EpicAssets.BulwarkMagicShieldSFX = assetBundle.LoadAsset<GameObject>("sfx_bulwark");

        EpicAssets.UndyingStatusEffect = assetBundle.LoadAsset<SE_Stats>(EpicAssets.Undying_SE_Name);
        EpicAssets.UndyingVFX = assetBundle.LoadAsset<GameObject>("Undying");
        EpicAssets.UndyingSFX = assetBundle.LoadAsset<GameObject>("sfx_undying");

        EpicAssets.BerserkerStatusEffect = assetBundle.LoadAsset<SE_Stats>(EpicAssets.Berserker_SE_Name);
        EpicAssets.BerserkerVFX = assetBundle.LoadAsset<GameObject>("Berserker");
        EpicAssets.BerserkerSFX =  assetBundle.LoadAsset<GameObject>("sfx_berserker");

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
        ItemManager.OnItemsRegistered += SetupStatusEffects;
        LoadUnidentifiedItems();
        // Needs to trigger late in order to get all potentially added items by other mods
        MinimapManager.OnVanillaMapDataLoaded += () => AutoAddEnchantableItems.CheckAndAddAllEnchantableItems();

        EpicAssets.AssertAssetIntegrety();
    }

    public static T LoadAsset<T>(string assetName) where T : Object
    {
        try
        {
            if (EpicAssets.AssetCache.ContainsKey(assetName))
            {
                return (T)EpicAssets.AssetCache[assetName];
            }

            var asset = EpicAssets.AssetBundle.LoadAsset<T>(assetName);
            EpicAssets.AssetCache.Add(assetName, asset);
            return asset;
        }
        catch (Exception e)
        {
            LogErrorForce($"Error loading asset ({assetName}): {e.Message}");
            return null;
        }
    }

    private static void LoadPieces()
    {
        GameObject enchanter = EpicAssets.AssetBundle.LoadAsset<GameObject>("piece_enchanter");
        PieceConfig enchanterPC = new PieceConfig();
        enchanterPC.PieceTable = "Hammer";
        enchanterPC.Category = PieceCategories.Misc;
        enchanterPC.AllowedInDungeons = false;
        enchanterPC.Requirements = new RequirementConfig[]
        {
            new RequirementConfig() { Item = "Stone", Amount = 10, Recover = true },
            new RequirementConfig() { Item = "SurtlingCore", Amount = 3, Recover = true },
            new RequirementConfig() { Item = "Copper", Amount = 3, Recover = true },
            new RequirementConfig() { Item = "SwordCheat", Amount = 1, Recover = false }
        };
        PieceManager.Instance.AddPiece(new CustomPiece(enchanter, true, enchanterPC));

        GameObject augmenter = EpicAssets.AssetBundle.LoadAsset<GameObject>("piece_augmenter");
        PieceConfig augmenterPC = new PieceConfig();
        augmenterPC.PieceTable = "Hammer";
        augmenterPC.Category = PieceCategories.Misc;
        augmenterPC.AllowedInDungeons = false;
        augmenterPC.Requirements = new RequirementConfig[]
        {
            new RequirementConfig() { Item = "Obsidian", Amount = 10, Recover = true },
            new RequirementConfig() { Item = "Crystal", Amount = 3, Recover = true },
            new RequirementConfig() { Item = "Bronze", Amount = 3, Recover = true },
            new RequirementConfig() { Item = "SwordCheat", Amount = 1, Recover = false }
        };
        PieceManager.Instance.AddPiece(new CustomPiece(augmenter, true, augmenterPC));

        GameObject table = EpicAssets.AssetBundle.LoadAsset<GameObject>("piece_enchantingtable");
        PieceConfig tablePC = new PieceConfig();
        tablePC.PieceTable = "Hammer";
        tablePC.Category = PieceCategories.Crafting;
        tablePC.Requirements = new RequirementConfig[]
        {
            new RequirementConfig() { Item = "FineWood", Amount = 10, Recover = true },
            new RequirementConfig() { Item = "SurtlingCore", Amount = 1, Recover = true }
        };
        PieceManager.Instance.AddPiece(new CustomPiece(table, true, tablePC));
    }

    private static void LoadItems()
    {
        foreach (var item in ItemNames)
        {
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
    }

    private static void LoadBountySpawner()
    {
        GameObject bounty_spawner = EpicAssets.AssetBundle.LoadAsset<GameObject>("EL_SpawnController");

        if (bounty_spawner == null)
        {
            LogErrorForce("Unable to find bounty spawner asset! This mod will not behave as expected!");
        }
        else
        {
            bounty_spawner.AddComponent<AdventureSpawnController>();
            CustomPrefab prefab_obj = new CustomPrefab(bounty_spawner, false);
            PrefabManager.Instance.AddPrefab(prefab_obj);
        }
    }

    private static void LoadCraftingMaterialAssets()
    {
        foreach (string type in MagicMaterials)
        {
            foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
            {
                string assetName = $"{type}{rarity}";
                GameObject prefab = EpicAssets.AssetBundle.LoadAsset<GameObject>(assetName);

                if (!prefab)
                {
                    LogErrorForce($"Tried to load asset {assetName} but it does not exist in the asset bundle!");
                    continue;
                }

                if (prefab.TryGetComponent(out ItemDrop itemDrop))
                {
                    if (itemDrop.m_itemData.IsMagicCraftingMaterial())
                    {
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

    private static void LoadUnidentifiedItems()
    {
        // TODO: Add support for biomes added by other mods as needed.
        GameObject genericPrefab = EpicAssets.AssetBundle.LoadAsset<GameObject>("_Unidentified");
        CustomItem genericUnidentified = new CustomItem(genericPrefab, false);
        ItemManager.Instance.AddItem(genericUnidentified);
        genericPrefab.SetActive(false);

        foreach (string biome in Enum.GetNames(typeof(Heightmap.Biome)))
        {
            if (biome == "None" || biome == "All")
            {
                continue;
            }

            foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
            {
                var prefab = Object.Instantiate(genericPrefab);
                string prefabName = $"{biome}_{rarity}_Unidentified";
                prefab.name = prefabName;
                ItemDrop pid = prefab.GetComponent<ItemDrop>();
                var magicItemComponent = pid.m_itemData.Data().GetOrCreate<MagicItemComponent>();
                pid.m_itemData.m_dropPrefab = prefab;
                magicItemComponent.SetMagicItem(new MagicItem
                {
                    Rarity = rarity,
                    IsUnidentified = true,
                });
                magicItemComponent.Save();
                
                ItemConfig unidentifiedIC = new ItemConfig()
                {
                    Name = $"$mod_epicloot_{rarity} $mod_epicloot_unidentified_{biome}",
                    Description = "$mod_epicloot_unidentified_introduce",
                };

                CustomItem custom = new CustomItem(prefab, false, unidentifiedIC);
                ItemManager.Instance.AddItem(custom);

                // Enable Items once things are working so that ZNet issues don't happen
                void EnableUnidentified(string prefabname)
                {
                    PrefabManager.Instance.GetPrefab(prefabName).SetActive(true);
                    PrefabManager.Instance.GetPrefab(prefabName).GetComponent<ItemDrop>().m_itemData.m_dropPrefab =
                        PrefabManager.Instance.GetPrefab(prefabName);
                    ItemManager.OnItemsRegistered -= () => EnableUnidentified(prefabname);
                }

                ItemManager.OnItemsRegistered += () => EnableUnidentified(prefabName);
            }
        }
    }

    private static void RegisterStatusEffects()
    {
        RegisterBulwark();
        RegisterUndying();
        RegisterBerserker();
        RegisterAdrenalineRush();
    }

    private static void RegisterBulwark()
    {
        PrefabManager.Instance.AddPrefab(EpicAssets.BulwarkMagicShieldVFX);
        PrefabManager.Instance.AddPrefab(EpicAssets.BulwarkMagicShieldSFX);
        ItemManager.OnItemsRegistered += () => ObjectDB.instance.m_StatusEffects.Add(EpicAssets.BulwarkStatusEffect);
    }

    private static void RegisterBerserker()
    {
        PrefabManager.Instance.AddPrefab(EpicAssets.BerserkerVFX);
        PrefabManager.Instance.AddPrefab(EpicAssets.BerserkerSFX);
        ItemManager.OnItemsRegistered += () => ObjectDB.instance.m_StatusEffects.Add(EpicAssets.BerserkerStatusEffect);
    }

    private static void RegisterUndying()
    {
        PrefabManager.Instance.AddPrefab(EpicAssets.UndyingVFX);
        PrefabManager.Instance.AddPrefab(EpicAssets.UndyingSFX);
        ItemManager.OnItemsRegistered += () => ObjectDB.instance.m_StatusEffects.Add(EpicAssets.UndyingStatusEffect);
    }

    private static void RegisterAdrenalineRush()
    {
        PrefabManager.Instance.AddPrefab(EpicAssets.DodgeBuffSFX);
        ItemManager.OnItemsRegistered += () => ObjectDB.instance.m_StatusEffects.Add(EpicAssets.DodgeBuffStatusEffect);
    }

    [UsedImplicitly]
    void OnDestroy()
    {
        Config.Save();
        _instance = null;
    }

    public static bool IsObjectDBReady()
    {
        // Hack, just making sure the built-in items and prefabs have loaded
        return ObjectDB.instance != null && ObjectDB.instance.m_items.Count != 0 &&
            ObjectDB.instance.GetItemPrefab("Amber") != null;
    }

    private static void SetupAndvaranaut()
    {
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
        var magicItem = new MagicItem
        {
            Rarity = ItemRarity.Epic,
            TypeNameOverride = "$mod_epicloot_item_andvaranaut_type"
        };
        magicItem.Effects.Add(new MagicItemEffect(MagicEffectType.Andvaranaut));

        prefab.m_itemData.SaveMagicItem(magicItem);

        var customItem = new CustomItem(go, false);
        ItemManager.Instance.AddItem(customItem);

        PrefabManager.OnPrefabsRegistered -= SetupAndvaranaut;
    }

    private static void SetupStatusEffects()
    {
        var lightning = ObjectDB.instance.GetStatusEffect("Lightning".GetHashCode());
        var paralyzed = ScriptableObject.CreateInstance<SE_Paralyzed>();
        Common.Utils.CopyFields(lightning, paralyzed, typeof(StatusEffect));
        paralyzed.name = "Paralyze";
        paralyzed.m_name = "$mod_epicloot_se_paralyze";

        ObjectDB.instance.m_StatusEffects.Add(paralyzed);
        ItemManager.OnItemsRegistered -= SetupStatusEffects;
    }

    public static AssetBundle LoadAssetBundle(string filename)
    {
        var assembly = Assembly.GetCallingAssembly();
        var assetBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream(
            $"{assembly.GetName().Name}.{filename}"));

        return assetBundle;
    }

    /// <summary>
    /// This reads an embedded file resouce name, these are all resouces packed into the DLL
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    internal static string ReadEmbeddedResourceFile(string filename)
    {
        using (var stream = typeof(EpicLoot).Assembly.GetManifestResourceStream(filename))
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }

    internal static List<string> GetEmbeddedResourceNamesFromDirectory(string embedded_location = "EpicLoot.config.")
    {
        List<string> resourcenames = new List<string>();
        foreach (string embeddedResouce in typeof(EpicLoot).Assembly.GetManifestResourceNames())
        {
            if (embeddedResouce.Contains(embedded_location))
            {
                // Got to strip the starting 'EpicLoot.config.' off, so we just take the ending substring
                resourcenames.Add(embeddedResouce.Substring(16));
            }
        }
        return resourcenames;
    }

    public static bool CanBeMagicItem(ItemDrop.ItemData item)
    {
        return item != null
               && IsPlayerItem(item)
               && Nonstackable(item)
               && IsNotRestrictedItem(item)
               && IsAllowedMagicItemType(item);
    }

    public static bool IsAllowedMagicItemType(ItemDrop.ItemData item)
    {
        switch (item.m_shared.m_itemType)
        {
            case ItemDrop.ItemData.ItemType.Ammo:
            case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                    return false;
            default:
                return item.IsEquipable();
        }
    }

    public static Sprite GetMagicItemBgSprite()
    {
        return HasAuga ? EpicAssets.AugaItemBgSprite : EpicAssets.GenericItemBgSprite;
    }

    public static Sprite GetEquippedSprite()
    {
        return HasAuga ? EpicAssets.AugaEquippedSprite : EpicAssets.EquippedSprite;
    }

    public static Sprite GetSetItemSprite()
    {
        return HasAuga ? EpicAssets.AugaSetItemSprite : EpicAssets.GenericSetItemSprite;
    }

    public static string GetMagicEffectPip(bool hasBeenAugmented)
    {
        return HasAuga ? (hasBeenAugmented ? "▾" : "♦") : (hasBeenAugmented ? "▼" : "◆"); // //🞠🞛
    }

    private static bool IsNotRestrictedItem(ItemDrop.ItemData item)
    {
        if (item.m_dropPrefab != null && LootRoller.Config.RestrictedItems.Contains(item.m_dropPrefab.name))
        {
            return false;
        }

        return !LootRoller.Config.RestrictedItems.Contains(item.m_shared.m_name);
    }

    private static bool Nonstackable(ItemDrop.ItemData item)
    {
        return item.m_shared.m_maxStackSize == 1;
    }

    private static bool IsPlayerItem(ItemDrop.ItemData item)
    {
        // WTF, this is the only thing I found different between player usable items and items that are only for enemies
        return item.m_shared.m_icons.Length > 0;
    }

    public static string GetCharacterCleanName(Character character)
    {
        return character.name.Replace("(Clone)", "").Trim();
    }

    public static string GetSetItemColor()
    {
        return ELConfig._setItemColor.Value;
    }

    public static string GetRarityDisplayName(ItemRarity rarity)
    {
        switch (rarity)
        {
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

    public static string GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
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

    public static Color GetRarityColorARGB(ItemRarity rarity)
    {
        return ColorUtility.TryParseHtmlString(GetRarityColor(rarity), out var color) ? color : Color.white;
    }

    private static string GetColor(string configValue)
    {
        if (configValue.StartsWith("#"))
        {
            return configValue;
        }
        else
        {
            if (MagicItemColors.TryGetValue(configValue, out var color))
            {
                return color;
            }
        }

        return "#000000";
    }

    public static int GetRarityIconIndex(ItemRarity rarity)
    {
        switch (rarity)
        {
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

    public static AudioClip GetMagicItemDropSFX(ItemRarity rarity)
    {
        return EpicAssets.MagicItemDropSFX[(int) rarity];
    }

    public static GatedItemTypeMode GetGatedItemTypeMode()
    {
        return ELConfig._gatedItemTypeModeConfig.Value;
    }

    public static BossDropMode GetBossTrophyDropMode()
    {
        return ELConfig._bossTrophyDropMode.Value;
    }

    public static float GetBossTrophyDropPlayerRange()
    {
        return ELConfig._bossTrophyDropPlayerRange.Value;
    }

    public static float GetBossCryptKeyPlayerRange()
    {
        return ELConfig._bossCryptKeyDropPlayerRange.Value;
    }

    public static BossDropMode GetBossCryptKeyDropMode()
    {
        return ELConfig._bossCryptKeyDropMode.Value;
    }

    public static BossDropMode GetBossWishboneDropMode()
    {
        return ELConfig._bossWishboneDropMode.Value;
    }

    public static float GetBossWishboneDropPlayerRange()
    {
        return ELConfig._bossWishboneDropPlayerRange.Value;
    }

    public static int GetAndvaranautRange()
    {
      return ELConfig._andvaranautRange.Value;
    }

    public static bool IsAdventureModeEnabled()
    {
        return ELConfig._adventureModeEnabled.Value;
    }

    public static float GetWorldLuckFactor()
    {
        return _instance._worldLuckFactor;
    }

    // TODO, why isn't this used?
    public static void SetWorldLuckFactor(float luckFactor)
    {
        _instance._worldLuckFactor = luckFactor;
    }

    private void SetupWatcher()
    {
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

    private void ReadConfigValues(object sender, FileSystemEventArgs e)
    {
        var now = DateTime.Now;
        var time = now.Ticks - _lastReloadTime.Ticks;
        if (!File.Exists(ConfigFileFullPath) || time < RELOAD_DELAY) return;

        try
        {
            Log("Attempting to reload configuration...");
            Config.Reload();
        }
        catch
        {
            Log($"There was an issue loading {ConfigFileName}");
            return;
        }

        _lastReloadTime = now;
    }
}
