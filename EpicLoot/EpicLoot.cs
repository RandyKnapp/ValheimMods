using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using Common;
using EpicLoot.Abilities;
using EpicLoot.Adventure;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using EpicLoot.GatedItemType;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using EpicLoot.Patching;
using ExtendedItemDataFramework;
using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using ServerSync;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace EpicLoot
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public enum BossDropMode
    {
        Default,
        OnePerPlayerOnServer,
        OnePerPlayerNearBoss
    }

    public enum GatedBountyMode
    {
        Unlimited,
        BossKillUnlocksCurrentBiomeBounties,
        BossKillUnlocksNextBiomeBounties
    }

    public class Assets
    {
        public AssetBundle AssetBundle;
        public Sprite EquippedSprite;
        public Sprite AugaEquippedSprite;
        public Sprite GenericSetItemSprite;
        public Sprite AugaSetItemSprite;
        public Sprite GenericItemBgSprite;
        public Sprite AugaItemBgSprite;
        public GameObject[] MagicItemLootBeamPrefabs = new GameObject[4];
        public readonly Dictionary<string, GameObject[]> CraftingMaterialPrefabs = new Dictionary<string, GameObject[]>();
        public Sprite SmallButtonEnchantOverlay;
        public AudioClip[] MagicItemDropSFX = new AudioClip[4];
        public AudioClip ItemLoopSFX;
        public AudioClip AugmentItemSFX;
        public GameObject MerchantPanel;
        public Sprite MapIconTreasureMap;
        public Sprite MapIconBounty;
        public AudioClip AbandonBountySFX;
        public AudioClip DoubleJumpSFX;
        public GameObject DebugTextPrefab;
        public GameObject AbilityBar;
        public GameObject WelcomMessagePrefab;
    }

    public class PieceDef
    {
        public string Table;
        public string CraftingStation;
        public string ExtendStation;
        public List<RecipeRequirementConfig> Resources = new List<RecipeRequirementConfig>();
    }

    [BepInPlugin(PluginId, DisplayName, Version)]
    [BepInDependency("randyknapp.mods.extendeditemdataframework", "1.0.10")]
    [BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
    public class EpicLoot : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.epicloot";
        public const string DisplayName = "Epic Loot";
        public const string Version = "0.9.3";

        private readonly ConfigSync _configSync = new ConfigSync(PluginId) { DisplayName = DisplayName, CurrentVersion = Version, MinimumRequiredVersion = "0.9.3" };

        private static ConfigEntry<string> _setItemColor;
        private static ConfigEntry<string> _magicRarityColor;
        private static ConfigEntry<string> _rareRarityColor;
        private static ConfigEntry<string> _epicRarityColor;
        private static ConfigEntry<string> _legendaryRarityColor;
        private static ConfigEntry<int> _magicMaterialIconColor;
        private static ConfigEntry<int> _rareMaterialIconColor;
        private static ConfigEntry<int> _epicMaterialIconColor;
        private static ConfigEntry<int> _legendaryMaterialIconColor;
        public static ConfigEntry<bool> UseScrollingCraftDescription;
        public static ConfigEntry<CraftingTabStyle> CraftingTabStyle;
        private static ConfigEntry<bool> _loggingEnabled;
        private static ConfigEntry<LogLevel> _logLevel;
        public static ConfigEntry<bool> UseGeneratedMagicItemNames;
        private static ConfigEntry<GatedItemTypeMode> _gatedItemTypeModeConfig;
        public static ConfigEntry<GatedBountyMode> BossBountyMode;
        private static ConfigEntry<BossDropMode> _bossTrophyDropMode;
        private static ConfigEntry<float> _bossTrophyDropPlayerRange;
        private static ConfigEntry<int> _andvaranautRange;
        public static ConfigEntry<bool> ShowEquippedAndHotbarItemsInSacrificeTab;
        private static ConfigEntry<bool> _adventureModeEnabled;
        private static ConfigEntry<bool> _serverConfigLocked;
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

        public static Dictionary<string, CustomSyncedValue<string>> SyncedJsonFiles = new Dictionary<string, CustomSyncedValue<string>>();

        public static readonly List<ItemDrop.ItemData.ItemType> AllowedMagicItemTypes = new List<ItemDrop.ItemData.ItemType>
        {
            ItemDrop.ItemData.ItemType.Helmet,
            ItemDrop.ItemData.ItemType.Chest,
            ItemDrop.ItemData.ItemType.Legs,
            ItemDrop.ItemData.ItemType.Shoulder,
            ItemDrop.ItemData.ItemType.Utility,
            ItemDrop.ItemData.ItemType.Bow,
            ItemDrop.ItemData.ItemType.OneHandedWeapon,
            ItemDrop.ItemData.ItemType.TwoHandedWeapon,
            ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft,
            ItemDrop.ItemData.ItemType.Shield,
            ItemDrop.ItemData.ItemType.Tool,
            ItemDrop.ItemData.ItemType.Torch,
        };

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

        public static readonly Assets Assets = new Assets();
        public static readonly List<GameObject> RegisteredPrefabs = new List<GameObject>();
        public static readonly List<GameObject> RegisteredItemPrefabs = new List<GameObject>();
        public static readonly Dictionary<GameObject, PieceDef> RegisteredPieces = new Dictionary<GameObject, PieceDef>();
        private static readonly Dictionary<string, Action<ItemDrop>> _customItemSetupActions = new Dictionary<string, Action<ItemDrop>>();
        private static readonly Dictionary<string, Object> _assetCache = new Dictionary<string, Object>();
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

        [UsedImplicitly]
        private void Awake()
        {
            _instance = this;

            _magicRarityColor = Config.Bind("Item Colors", "Magic Rarity Color", "Blue", "The color of Magic rarity items, the lowest magic item tier. (Optional, use an HTML hex color starting with # to have a custom color.) Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
            _magicMaterialIconColor = Config.Bind("Item Colors", "Magic Crafting Material Icon Index", 5, "Indicates the color of the icon used for magic crafting materials. A number between 0 and 9. Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
            _rareRarityColor = Config.Bind("Item Colors", "Rare Rarity Color", "Yellow", "The color of Rare rarity items, the second magic item tier. (Optional, use an HTML hex color starting with # to have a custom color.) Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
            _rareMaterialIconColor = Config.Bind("Item Colors", "Rare Crafting Material Icon Index", 2, "Indicates the color of the icon used for rare crafting materials. A number between 0 and 9. Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
            _epicRarityColor = Config.Bind("Item Colors", "Epic Rarity Color", "Purple", "The color of Epic rarity items, the third magic item tier. (Optional, use an HTML hex color starting with # to have a custom color.) Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
            _epicMaterialIconColor = Config.Bind("Item Colors", "Epic Crafting Material Icon Index", 7, "Indicates the color of the icon used for epic crafting materials. A number between 0 and 9. Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
            _legendaryRarityColor = Config.Bind("Item Colors", "Legendary Rarity Color", "Teal", "The color of Legendary rarity items, the highest magic item tier. (Optional, use an HTML hex color starting with # to have a custom color.) Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
            _legendaryMaterialIconColor = Config.Bind("Item Colors", "Legendary Crafting Material Icon Index", 4, "Indicates the color of the icon used for legendary crafting materials. A number between 0 and 9. Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
            _setItemColor = Config.Bind("Item Colors", "Set Item Color", "#26ffff", "The color of set item text and the set item icon. Use a hex color, default is cyan");
            UseScrollingCraftDescription = Config.Bind("Crafting UI", "Use Scrolling Craft Description", true, "Changes the item description in the crafting panel to scroll instead of scale when it gets too long for the space.");
            CraftingTabStyle = Config.Bind("Crafting UI", "Crafting Tab Style", Crafting.CraftingTabStyle.HorizontalSquish, "Sets the layout style for crafting tabs, if you've got too many. Horizontal is the vanilla method, but might overlap other mods or run off the screen. HorizontalSquish makes the buttons narrower, works okay with 6 or 7 buttons. Vertical puts the tabs in a column to the left the crafting window. Angled tries to make more room at the top of the crafting panel by angling the tabs, works okay with 6 or 7 tabs.");
            ShowEquippedAndHotbarItemsInSacrificeTab = Config.Bind("Crafting UI", "ShowEquippedAndHotbarItemsInSacrificeTab", false, "If set to false, hides the items that are equipped or on your hotbar in the Sacrifice items list.");
            _loggingEnabled = Config.Bind("Logging", "Logging Enabled", false, "Enable logging");
            _logLevel = Config.Bind("Logging", "Log Level", LogLevel.Info, "Only log messages of the selected level or higher");
            UseGeneratedMagicItemNames = Config.Bind("General", "Use Generated Magic Item Names", true, "If true, magic items uses special, randomly generated names based on their rarity, type, and magic effects.");
            _gatedItemTypeModeConfig = SyncedConfig("Balance", "Item Drop Limits", GatedItemTypeMode.BossKillUnlocksCurrentBiomeItems, "Sets how the drop system limits what item types can drop. Unlimited: no limits, exactly what's in the loot table will drop. BossKillUnlocksCurrentBiomeItems: items will drop for the current biome if the that biome's boss has been killed (Leather gear will drop once Eikthyr is killed). BossKillUnlocksNextBiomeItems: items will only drop for the current biome if the previous biome's boss is killed (Bronze gear will drop once Eikthyr is killed). PlayerMustKnowRecipe: (local world only) the item can drop if the player can craft it. PlayerMustHaveCraftedItem: (local world only) the item can drop if the player has already crafted it or otherwise picked it up. If an item type cannot drop, it will downgrade to an item of the same type and skill that the player has unlocked (i.e. swords will stay swords) according to iteminfo.json.");
            BossBountyMode = SyncedConfig("Balance", "Gated Bounty Mode", GatedBountyMode.Unlimited, "Sets whether available bounties are ungated or gated by boss kills.");
            _bossTrophyDropMode = SyncedConfig("Balance", "Boss Trophy Drop Mode", BossDropMode.OnePerPlayerNearBoss, "Sets bosses to drop a number of trophies equal to the number of players, similar to the way Wishbone works in vanilla. Optionally set it to only include players within a certain distance, use 'Boss Trophy Drop Player Range' to set the range.");
            _bossTrophyDropPlayerRange = SyncedConfig("Balance", "Boss Trophy Drop Player Range", 100.0f, "Sets the range that bosses check when dropping multiple trophies using the OnePerPlayerNearBoss drop mode.");
            _adventureModeEnabled = SyncedConfig("Balance", "Adventure Mode Enabled", true, "Set to true to enable all the adventure mode features: secret stash, gambling, treasure maps, and bounties. Set to false to disable. This will not actually remove active treasure maps or bounties from your save.");
            _andvaranautRange = SyncedConfig("Balance", "Andvaranaut Range", 20, "Sets the range that Andvaranaut will locate a treasure chest.");
            _serverConfigLocked = SyncedConfig("Config Sync", "Lock Config", false, new ConfigDescription("[Server Only] The configuration is locked and may not be changed by clients once it has been synced from the server. Only valid for server config, will have no effect on clients."));
            SetItemDropChance = SyncedConfig("Balance", "Set Item Drop Chance", 0.15f, "The percent chance that a legendary item will be a set item. Min = 0, Max = 1");
            GlobalDropRateModifier = SyncedConfig("Balance", "Global Drop Rate Modifier", 1.0f, "A global percentage that modifies how likely items are to drop. 1 = Exactly what is in the loot tables will drop. 0 = Nothing will drop. 2 = The number of items in the drop table are twice as likely to drop (note, this doesn't double the number of items dropped, just doubles the relative chance for them to drop). Min = 0, Max = 4");
            ItemsToMaterialsDropRatio = SyncedConfig("Balance", "Items To Materials Drop Ratio", 0.0f, "Sets the chance that item drops are instead dropped as magic crafting materials. 0 = all items, no materials. 1 = all materials, no items. Values between 0 and 1 change the ratio of items to materials that drop. At 0.5, half of everything that drops would be items and the other half would be materials. Min = 0, Max = 1");

            AlwaysShowWelcomeMessage = Config.Bind("Debug", "AlwaysShowWelcomeMessage", false, "Just a debug flag for testing the welcome message, do not use.");
            OutputPatchedConfigFiles = Config.Bind("Debug", "OutputPatchedConfigFiles", false, "Just a debug flag for testing the patching system, do not use.");

            AbilityKeyCodes[0] = Config.Bind("Abilities", "Ability Hotkey 1", "g", "Hotkey for Ability Slot 1.");
            AbilityKeyCodes[1] = Config.Bind("Abilities", "Ability Hotkey 2", "h", "Hotkey for Ability Slot 2.");
            AbilityKeyCodes[2] = Config.Bind("Abilities", "Ability Hotkey 3", "j", "Hotkey for Ability Slot 3.");
            AbilityBarAnchor = Config.Bind("Abilities", "Ability Bar Anchor", TextAnchor.LowerLeft, "The point on the HUD to anchor the ability bar. Changing this also changes the pivot of the ability bar to that corner. For reference: the ability bar size is 208 by 64.");
            AbilityBarPosition = Config.Bind("Abilities", "Ability Bar Position", new Vector2(150, 170), "The position offset from the Ability Bar Anchor at which to place the ability bar.");
            AbilityBarLayoutAlignment = Config.Bind("Abilities", "Ability Bar Layout Alignment", TextAnchor.LowerLeft, "The Ability Bar is a Horizontal Layout Group. This value indicates how the elements inside are aligned. Choices with 'Center' in them will keep the items centered on the bar, even if there are fewer than the maximum allowed. 'Left' will be left aligned, and similar for 'Right'.");
            AbilityBarIconSpacing = Config.Bind("Abilities", "Ability Bar Icon Spacing", 8.0f, "The number of units between the icons on the ability bar.");

            _configSync.AddLockingConfigEntry(_serverConfigLocked);

            ExtendedItemData.RegisterCustomTypeID(MagicItemComponent.TypeID, typeof(MagicItemComponent));

            var assembly = Assembly.GetExecutingAssembly();
            LoadEmbeddedAssembly(assembly, "EpicLoot-UnityLib.dll");

            LoadPatches();
            LoadTranslations();
            InitializeConfig();
            InitializeAbilities();
            PrintInfo();
            //GenerateTranslations();

            LoadAssets();

            EnchantingUIController.Initialize();
            ExtendedItemData.LoadExtendedItemData += MagicItemComponent.OnNewExtendedItemData;
            ExtendedItemData.NewExtendedItemData += MagicItemComponent.OnNewExtendedItemData;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);

            LootTableLoaded?.Invoke();
        }

        private static void LoadEmbeddedAssembly(Assembly assembly, string assemblyName)
        {
            var stream = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{assemblyName}");
            if (stream == null)
            {
                LogError($"Could not load embedded assembly ({assemblyName})!");
                return;
            }

            using (stream)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                Assembly.Load(data);
            }
        }

        public void Start()
        {
            HasAuga = Auga.API.IsLoaded();

            if (HasAuga)
            {
                Auga.API.ComplexTooltip_AddItemTooltipCreatedListener(ExtendAugaTooltipForMagicItem);
                Auga.API.ComplexTooltip_AddItemStatPreprocessor(AugaTooltipPreprocessor.PreprocessTooltipStat);
            }
        }

        public static void ExtendAugaTooltipForMagicItem(GameObject complexTooltip, ItemDrop.ItemData item)
        {
            Auga.API.ComplexTooltip_SetTopic(complexTooltip, Localization.instance.Localize(item.GetDecoratedName()));

            var isMagic = item.IsMagic(out var magicItem);

            var inFront = true;
            var itemBG = complexTooltip.transform.Find("Tooltip/IconHeader/IconBkg/Item");
            if (itemBG == null)
            {
                itemBG = complexTooltip.transform.Find("InventoryElement/icon");
                inFront = false;
            }

            RectTransform magicBG = null;
            if (itemBG != null)
            {
                var itemBGImage = itemBG.GetComponent<Image>();
                magicBG = (RectTransform)itemBG.transform.Find("magicItem");
                if (magicBG == null)
                {
                    var magicItemObject = Instantiate(itemBGImage, inFront ? itemBG.transform : itemBG.transform.parent).gameObject;
                    magicItemObject.name = "magicItem";
                    magicItemObject.SetActive(true);
                    magicBG = (RectTransform)magicItemObject.transform;
                    magicBG.anchorMin = Vector2.zero;
                    magicBG.anchorMax = new Vector2(1, 1);
                    magicBG.sizeDelta = Vector2.zero;
                    magicBG.pivot = new Vector2(0.5f, 0.5f);
                    magicBG.anchoredPosition = Vector2.zero;
                    var magicItemInit = magicBG.GetComponent<Image>();
                    magicItemInit.color = Color.white;
                    magicItemInit.raycastTarget = false;
                    magicItemInit.sprite = GetMagicItemBgSprite();

                    if (!inFront)
                    {
                        magicBG.SetSiblingIndex(0);
                    }
                }
            }

            if (magicBG != null)
            {
                magicBG.gameObject.SetActive(isMagic);
            }

            if (item.IsMagicCraftingMaterial())
            {
                var rarity = item.GetCraftingMaterialRarity();
                Auga.API.ComplexTooltip_SetIcon(complexTooltip, item.m_shared.m_icons[GetRarityIconIndex(rarity)]);
            }

            if (isMagic)
            {
                var magicColor = magicItem.GetColorString();
                var itemTypeName = magicItem.GetItemTypeName(item.Extended());

                if (magicBG != null)
                {
                    magicBG.GetComponent<Image>().color = item.GetRarityColor();
                }

                Auga.API.ComplexTooltip_SetIcon(complexTooltip, item.GetIcon());

                if (item.IsLegendarySetItem())
                {
                    Auga.API.ComplexTooltip_SetSubtitle(complexTooltip, Localization.instance.Localize($"<color={GetSetItemColor()}>$mod_epicloot_legendarysetlabel</color>, {itemTypeName}\n"));
                }
                else
                {
                    Auga.API.ComplexTooltip_SetSubtitle(complexTooltip, Localization.instance.Localize($"<color={magicColor}>{magicItem.GetRarityDisplay()} {itemTypeName}</color>"));
                }

                if (AugaTooltipNoTextBoxes)
                    return;

                Auga.API.ComplexTooltip_AddDivider(complexTooltip);

                var magicItemText = magicItem.GetTooltip();
                var textBox = Auga.API.ComplexTooltip_AddTwoColumnTextBox(complexTooltip);
                magicItemText = magicItemText.Replace("\n\n", "");
                Auga.API.TooltipTextBox_AddLine(textBox, magicItemText);

                if (magicItem.IsLegendarySetItem())
                {
                    var textBox2 = Auga.API.ComplexTooltip_AddTwoColumnTextBox(complexTooltip);
                    Auga.API.TooltipTextBox_AddLine(textBox2, item.GetSetTooltip());
                }

                Auga.API.ComplexTooltip_SetDescription(complexTooltip, Localization.instance.Localize(item.GetDescription()));
            }
        }

        private ConfigEntry<T> SyncedConfig<T>(string group, string configName, T value, string description, bool synchronizedSetting = true) => SyncedConfig(group, configName, value, new ConfigDescription(description), synchronizedSetting);
        
        private ConfigEntry<T> SyncedConfig<T>(string group, string configName, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            var configEntry = Config.Bind(group, configName, value, description);

            var syncedConfigEntry = _configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        public static void LoadPatches()
        {
            FilePatching.LoadAllPatches();
        }

        private static void LoadTranslations()
        {
            var translationsJsonText = LoadJsonText("translations.json");
            var translations = JsonConvert.DeserializeObject<IDictionary<string, object>>(translationsJsonText);
            if (translations == null)
            {
                Debug.LogError("Could not parse translations.json!");
                return;
            }

            foreach (var translation in translations)
            {
                //Log($"Translation: {translation.Key}, {translation.Value}");
                Localization.instance.AddWord(translation.Key, translation.Value.ToString());
            }
        }

        public static ConfigFile GetConfigObject()
        {
            return _instance.Config;
        }

        public static void InitializeConfig()
        {
            LoadJsonFile<LootConfig>("loottables.json", LootRoller.Initialize);
            LoadJsonFile<MagicItemEffectsList>("magiceffects.json", MagicItemEffectDefinitions.Initialize);
            LoadJsonFile<ItemInfoConfig>("iteminfo.json", GatedItemTypeHelper.Initialize);
            LoadJsonFile<RecipesConfig>("recipes.json", RecipesHelper.Initialize);
            LoadJsonFile<EnchantingCostsConfig>("enchantcosts.json", EnchantCostsHelper.Initialize);
            LoadJsonFile<ItemNameConfig>("itemnames.json", MagicItemNames.Initialize);
            LoadJsonFile<AdventureDataConfig>("adventuredata.json", AdventureDataManager.Initialize);
            LoadJsonFile<LegendaryItemConfig>("legendaries.json", UniqueLegendaryHelper.Initialize);
            LoadJsonFile<AbilityConfig>("abilities.json", AbilityDefinitions.Initialize);
            WatchNewPatchConfig();
        }

        public static void WatchNewPatchConfig()
        {

            Log($"Watching For Files");
            //Patch JSON Watcher

            void ConsumeNewPatchFile(object s, FileSystemEventArgs e)
            {
                FileInfo fileInfo = null;

                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                        //File Created
                        fileInfo = new FileInfo(e.FullPath);
                        if (!fileInfo.Exists) return;

                        FilePatching.ProcessPatchFile(fileInfo);
                        var sourceFile = fileInfo.Name;

                        foreach (var fileName in FilePatching.PatchesPerFile.Values.SelectMany(l => l).ToList()
                                     .Where(u => u.SourceFile.Equals(sourceFile)).Select(p => p.TargetFile).Distinct()
                                     .ToArray())
                        {
                            SyncedJsonFiles[fileName].AssignLocalValue(LoadJsonText(fileName));
                            AddPatchFileWatcher(fileName, sourceFile);
                        }
                        
                        break;
                }

            }

            var newPatchWatcher = new FileSystemWatcher(FilePatching.PatchesDirPath, "*.json");

            newPatchWatcher.Created += ConsumeNewPatchFile;
            newPatchWatcher.IncludeSubdirectories = true;
            newPatchWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            newPatchWatcher.EnableRaisingEvents = true;


        }

        private static void InitializeAbilities()
        {
            MagicEffectType.Initialize();
            AbilitiesInitialized?.Invoke();
        }

        public static void Log(string message)
        {
            if (_loggingEnabled.Value && _logLevel.Value <= LogLevel.Info)
            {
                _instance.Logger.LogInfo(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (_loggingEnabled.Value && _logLevel.Value <= LogLevel.Warning)
            {
                _instance.Logger.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (_loggingEnabled.Value && _logLevel.Value <= LogLevel.Error)
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

        /*private void Update()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            EpicLoot.LogWarning("== Objects under cursor: ==");
            if (Input.GetKeyDown(KeyCode.I))
            {
                results.ForEach((result) => {
                    EpicLoot.Log($"- {result.gameObject.name} ({result.gameObject.transform.parent.name})");
                });
            }
        }*/

        /*[UsedImplicitly]
        private void Update()
        {
            if (Input.GetKey(KeyCode.RightControl) && Input.GetKeyDown(KeyCode.Backspace))
            {
                Time.timeScale = Time.timeScale == 0 ? 1 : 0;
            }
        }*/

        private void LoadAssets()
        {
            var assetBundle = LoadAssetBundle("epicloot");
            Assets.AssetBundle = assetBundle;
            Assets.EquippedSprite = assetBundle.LoadAsset<Sprite>("Equipped");
            Assets.AugaEquippedSprite = assetBundle.LoadAsset<Sprite>("AugaEquipped");
            Assets.GenericSetItemSprite = assetBundle.LoadAsset<Sprite>("GenericSetItemMarker");
            Assets.AugaSetItemSprite = assetBundle.LoadAsset<Sprite>("AugaSetItem");
            Assets.GenericItemBgSprite = assetBundle.LoadAsset<Sprite>("GenericItemBg");
            Assets.AugaItemBgSprite = assetBundle.LoadAsset<Sprite>("AugaItemBG");
            Assets.SmallButtonEnchantOverlay = assetBundle.LoadAsset<Sprite>("SmallButtonEnchantOverlay");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Magic] = assetBundle.LoadAsset<GameObject>("MagicLootBeam");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Rare] = assetBundle.LoadAsset<GameObject>("RareLootBeam");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Epic] = assetBundle.LoadAsset<GameObject>("EpicLootBeam");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Legendary] = assetBundle.LoadAsset<GameObject>("LegendaryLootBeam");

            Assets.MagicItemDropSFX[(int)ItemRarity.Magic] = assetBundle.LoadAsset<AudioClip>("MagicItemDrop");
            Assets.MagicItemDropSFX[(int)ItemRarity.Rare] = assetBundle.LoadAsset<AudioClip>("RareItemDrop");
            Assets.MagicItemDropSFX[(int)ItemRarity.Epic] = assetBundle.LoadAsset<AudioClip>("EpicItemDrop");
            Assets.MagicItemDropSFX[(int)ItemRarity.Legendary] = assetBundle.LoadAsset<AudioClip>("LegendaryItemDrop");
            Assets.ItemLoopSFX = assetBundle.LoadAsset<AudioClip>("ItemLoop");
            Assets.AugmentItemSFX = assetBundle.LoadAsset<AudioClip>("AugmentItem");

            Assets.MerchantPanel = assetBundle.LoadAsset<GameObject>("MerchantPanel");
            Assets.MapIconTreasureMap = assetBundle.LoadAsset<Sprite>("TreasureMapIcon");
            Assets.MapIconBounty = assetBundle.LoadAsset<Sprite>("MapIconBounty");
            Assets.AbandonBountySFX = assetBundle.LoadAsset<AudioClip>("AbandonBounty");
            Assets.DoubleJumpSFX = assetBundle.LoadAsset<AudioClip>("DoubleJump");
            Assets.DebugTextPrefab = assetBundle.LoadAsset<GameObject>("DebugText");
            Assets.AbilityBar = assetBundle.LoadAsset<GameObject>("AbilityBar");
            Assets.WelcomMessagePrefab = assetBundle.LoadAsset<GameObject>("WelcomeMessage");

            LoadCraftingMaterialAssets(assetBundle, "Runestone");

            LoadCraftingMaterialAssets(assetBundle, "Shard");
            LoadCraftingMaterialAssets(assetBundle, "Dust");
            LoadCraftingMaterialAssets(assetBundle, "Reagent");
            LoadCraftingMaterialAssets(assetBundle, "Essence");

            LoadBuildPiece(assetBundle, "piece_enchanter", new PieceDef()
            {
                Table = "_HammerPieceTable",
                CraftingStation = "piece_workbench",
                ExtendStation = "forge",
                Resources = new List<RecipeRequirementConfig>
                {
                    new RecipeRequirementConfig { item = "Stone", amount = 10 },
                    new RecipeRequirementConfig { item = "SurtlingCore", amount = 3 },
                    new RecipeRequirementConfig { item = "Copper", amount = 3 },
                }
            });
            LoadBuildPiece(assetBundle, "piece_augmenter", new PieceDef()
            {
                Table = "_HammerPieceTable",
                CraftingStation = "piece_workbench",
                ExtendStation = "forge",
                Resources = new List<RecipeRequirementConfig>
                {
                    new RecipeRequirementConfig { item = "Obsidian", amount = 10 },
                    new RecipeRequirementConfig { item = "Crystal", amount = 3 },
                    new RecipeRequirementConfig { item = "Bronze", amount = 3 },
                }
            });
            LoadBuildPiece(assetBundle, "piece_enchantingtable", new PieceDef() {
                Table = "_HammerPieceTable",
                CraftingStation = "piece_workbench",
                Resources = new List<RecipeRequirementConfig>
                {
                    new RecipeRequirementConfig { item = "FineWood", amount = 10 },
                    new RecipeRequirementConfig { item = "SurtlingCore", amount = 1 }
                }
            });

            LoadItem(assetBundle, "LeatherBelt");
            LoadItem(assetBundle, "SilverRing");
            LoadItem(assetBundle, "GoldRubyRing");
            LoadItem(assetBundle, "Andvaranaut", SetupAndvaranaut);

            LoadItem(assetBundle, "ForestToken");
            LoadItem(assetBundle, "IronBountyToken");
            LoadItem(assetBundle, "GoldBountyToken");

            LoadAllZNetAssets(assetBundle);
        }

        public static T LoadAsset<T>(string assetName) where T : Object
        {
            try
            {
                if (_assetCache.ContainsKey(assetName))
                {
                    return (T)_assetCache[assetName];
                }

                var asset = Assets.AssetBundle.LoadAsset<T>(assetName);
                _assetCache.Add(assetName, asset);
                return asset;
            }
            catch (Exception e)
            {
                LogError($"Error loading asset ({assetName}): {e.Message}");
                return null;
            }
        }

        private static void LoadItem(AssetBundle assetBundle, string assetName, Action<ItemDrop> customSetupAction = null)
        {
            var prevForceDisable = ZNetView.m_forceDisableInit;
            ZNetView.m_forceDisableInit = true;
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            ZNetView.m_forceDisableInit = prevForceDisable;
            RegisteredItemPrefabs.Add(prefab);
            RegisteredPrefabs.Add(prefab);
            if (customSetupAction != null)
            {
                _customItemSetupActions.Add(prefab.name, customSetupAction);
            }
        }

        private static void LoadBuildPiece(AssetBundle assetBundle, string assetName, PieceDef pieceDef)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            RegisteredPieces.Add(prefab, pieceDef);
            RegisteredPrefabs.Add(prefab);
        }

        private static void LoadCraftingMaterialAssets(AssetBundle assetBundle, string type)
        {
            var prefabs = new GameObject[4];
            foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
            {
                var assetName = $"{type}{rarity}";
                var prefab = assetBundle.LoadAsset<GameObject>(assetName);
                if (prefab == null)
                {
                    LogError($"Tried to load asset {assetName} but it does not exist in the asset bundle!");
                    continue;
                }
                prefabs[(int) rarity] = prefab;
                RegisteredPrefabs.Add(prefab);
                RegisteredItemPrefabs.Add(prefab);
            }
            Assets.CraftingMaterialPrefabs.Add(type, prefabs);
        }

        private void LoadAllZNetAssets(AssetBundle assetBundle)
        {
            var znetAssets = assetBundle.LoadAllAssets();
            foreach (var asset in znetAssets)
            {
                if (asset is GameObject assetGo && assetGo.GetComponent<ZNetView>() != null)
                {
                    _assetCache.Add(asset.name, assetGo);
                    RegisteredPrefabs.Add(assetGo);
                }
            }
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            _instance = null;
            _harmony?.UnpatchSelf();
        }

        public static void TryRegisterPrefabs(ZNetScene zNetScene)
        {
            if (zNetScene == null || zNetScene.m_prefabs == null || zNetScene.m_prefabs.Count <= 0)
            {
                return;
            }

            foreach (var prefab in RegisteredPrefabs)
            {
                if (!zNetScene.m_prefabs.Contains(prefab))
                {
                    zNetScene.m_prefabs.Add(prefab);
                }
            }
        }

        public static void TryRegisterPieces(List<PieceTable> pieceTables, List<CraftingStation> craftingStations)
        {
            foreach (var entry in RegisteredPieces)
            {
                var prefab = entry.Key;
                if (prefab == null)
                {
                    LogError($"Tried to register piece but prefab was null!");
                    continue;
                }

                var pieceDef = entry.Value;
                if (pieceDef == null)
                {
                    LogError($"Tried to register piece ({prefab}) but pieceDef was null!");
                    continue;
                }

                var piece = prefab.GetComponent<Piece>();
                if (piece == null)
                {
                    LogError($"Tried to register piece ({prefab}) but Piece component was missing!");
                    continue;
                }

                var pieceTable = pieceTables.Find(x => x.name == pieceDef.Table);
                if (pieceTable == null)
                {
                    LogError($"Tried to register piece ({prefab}) but could not find piece table ({pieceDef.Table}) (pieceTables({pieceTables.Count})= {string.Join(", ", pieceTables.Select(x =>x.name))})!");
                    continue;
                }

                if (pieceTable.m_pieces.Contains(prefab))
                {
                    continue;
                }

                pieceTable.m_pieces.Add(prefab);

                var pieceStation = craftingStations.Find(x => x.name == pieceDef.CraftingStation);
                piece.m_craftingStation = pieceStation;

                var resources = new List<Piece.Requirement>();
                foreach (var resource in pieceDef.Resources)
                {
                    var resourcePrefab = ObjectDB.instance.GetItemPrefab(resource.item);
                    resources.Add(new Piece.Requirement()
                    {
                        m_resItem = resourcePrefab.GetComponent<ItemDrop>(),
                        m_amount = resource.amount
                    });
                }
                piece.m_resources = resources.ToArray();

                var stationExt = prefab.GetComponent<StationExtension>();
                if (stationExt != null && !string.IsNullOrEmpty(pieceDef.ExtendStation))
                {
                    var stationPrefab = pieceTable.m_pieces.Find(x => x.name == pieceDef.ExtendStation);
                    if (stationPrefab != null)
                    {
                        var station = stationPrefab.GetComponent<CraftingStation>();
                        stationExt.m_craftingStation = station;
                    }

                    var otherExt = pieceTable.m_pieces.Find(x => x.GetComponent<StationExtension>() != null);
                    if (otherExt != null)
                    {
                        var otherStationExt = otherExt.GetComponent<StationExtension>();
                        var otherPiece = otherExt.GetComponent<Piece>();

                        stationExt.m_connectionPrefab = otherStationExt.m_connectionPrefab;
                        piece.m_placeEffect.m_effectPrefabs = otherPiece.m_placeEffect.m_effectPrefabs.ToArray();
                    }
                }
                else
                {
                    var otherPiece = pieceTable.m_pieces.Find(x => x.GetComponent<Piece>() != null).GetComponent<Piece>();
                    piece.m_placeEffect.m_effectPrefabs = otherPiece.m_placeEffect.m_effectPrefabs.ToArray();
                }
            }
        }

        public static bool IsObjectDBReady()
        {
            // Hack, just making sure the built-in items and prefabs have loaded
            return ObjectDB.instance != null && ObjectDB.instance.m_items.Count != 0 && ObjectDB.instance.GetItemPrefab("Amber") != null;
        }

        public static void TryRegisterItems()
        {
            if (!IsObjectDBReady())
            {
                return;
            }

            
            foreach (var prefab in RegisteredItemPrefabs)
            {
                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    //Set icons for crafting materials

                    if (itemDrop.m_itemData.IsMagicCraftingMaterial() || itemDrop.m_itemData.IsRunestone())
                    {
                        var rarity = itemDrop.m_itemData.GetRarity();
                        
                        if (itemDrop.m_itemData.IsMagicCraftingMaterial())
                        {
                            itemDrop.m_itemData.m_variant = GetRarityIconIndex(rarity);
                        }
                    }
                }
            }

            foreach (var prefab in RegisteredItemPrefabs)
            {
                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    if (ObjectDB.instance.GetItemPrefab(prefab.name.GetStableHashCode()) == null)
                    {
                        ObjectDB.instance.m_items.Add(prefab);
                    }
                }
            }

            foreach (var prefab in RegisteredItemPrefabs)
            {
                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    if (_customItemSetupActions.TryGetValue(prefab.name, out var action))
                    {
                        action?.Invoke(itemDrop);
                    }
                }
            }

            ObjectDB.instance.UpdateItemHashes();

            var pieceTables = new List<PieceTable>();
            foreach (var itemPrefab in ObjectDB.instance.m_items)
            {
                var itemDrop = itemPrefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    LogError($"An item without an ItemDrop ({itemPrefab}) exists in ObjectDB.instance.m_items! Don't do this!");
                    continue;
                }
                var item = itemDrop.m_itemData;
                if (item != null && item.m_shared.m_buildPieces != null && !pieceTables.Contains(item.m_shared.m_buildPieces))
                {
                    pieceTables.Add(item.m_shared.m_buildPieces);
                }
            }

            var craftingStations = new List<CraftingStation>();
            foreach (var pieceTable in pieceTables)
            {
                craftingStations.AddRange(pieceTable.m_pieces
                    .Where(x => x.GetComponent<CraftingStation>() != null)
                    .Select(x => x.GetComponent<CraftingStation>()));
            }

            TryRegisterPieces(pieceTables, craftingStations);
            SetupStatusEffects();
        }

        public static void TryRegisterRecipes()
        {
            if (!IsObjectDBReady())
            {
                return;
            }

            RecipesHelper.SetupRecipes();
        }

        private static void SetupAndvaranaut(ItemDrop prefab)
        {
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
            andvaranautFinder.RequiredComponentTypes = new List<Type> { typeof(TreasureMapChest) };
            wishboneFinder.DisallowedComponentTypes = new List<Type> { typeof(TreasureMapChest) };

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

            prefab.m_itemData = new ExtendedItemData(prefab.m_itemData);
            prefab.m_itemData.Extended().ReplaceComponent<MagicItemComponent>().MagicItem = magicItem;
        }

        private static void SetupStatusEffects()
        {
            var lightning = ObjectDB.instance.GetStatusEffect("Lightning");
            var paralyzed = ScriptableObject.CreateInstance<SE_Paralyzed>();
            Common.Utils.CopyFields(lightning, paralyzed, typeof(StatusEffect));
            paralyzed.name = "Paralyze";
            paralyzed.m_name = "mod_epicloot_se_paralyze";

            ObjectDB.instance.m_StatusEffects.Add(paralyzed);
        }

        public static void LoadJsonFile<T>(string filename, Action<T> onFileLoad, bool update = false) where T : class
        {
            
            var jsonFile = LoadJsonText(filename);

            if (!update)
            {
                SyncedJsonFiles.Add(filename, new CustomSyncedValue<string>(_instance._configSync, filename, jsonFile));
            }
            
            void Process()
            {
                T result;
                try
                {
                    result = string.IsNullOrEmpty(SyncedJsonFiles[filename].Value) ? null : JsonConvert.DeserializeObject<T>(SyncedJsonFiles[filename].Value);
                }
                catch (Exception)
                {
                    LogError($"Could not parse file '{filename}'! Errors in JSON!");
                    throw;
                }

                onFileLoad(result);
            }

            SyncedJsonFiles[filename].ValueChanged += Process;
            Process();

            if (jsonFile != null)
            {
                //Primary JSON Watcher
	            void ConsumeConfigFileEvent(object s, FileSystemEventArgs e) => SyncedJsonFiles[filename].AssignLocalValue(LoadJsonText(filename));

	            var filePath = GetAssetPath(filename);
	            FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
	            watcher.Changed += ConsumeConfigFileEvent;
	            watcher.Created += ConsumeConfigFileEvent;
	            watcher.Renamed += ConsumeConfigFileEvent;
	            watcher.IncludeSubdirectories = true;
	            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
	            watcher.EnableRaisingEvents = true;

                //Patch JSON Watcher
                for (var i = 0; i < FilePatching.PatchesPerFile.Where(y => y.Key.Equals(filename)).ToList().Count; i++)
                {
                    var configFile = FilePatching.PatchesPerFile.Where(y => y.Key.Equals(filename)).ToList()[i];
                    var lists = configFile.Value.Select(p => p.SourceFile).Distinct().ToList();

                    for (var index = 0; index < lists.Count; index++)
                    {
                        var patchfile = lists[index];
                        AddPatchFileWatcher(filename,patchfile);
                        
                    }
                }
            }
        }

        private static void AddPatchFileWatcher(string fileName, string patchFile)
        {
            var fullPatchFilename = Path.Combine(FilePatching.PatchesDirPath, patchFile);
            Log($"[AddPatchFileWatcher] Full Patch File Name = {fullPatchFilename}");
            void ConsumePatchFileEvent(object s, FileSystemEventArgs e)
            {
                FileInfo fileInfo = null;

                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Deleted:
                        //File Deleted
                        Debug.Log($"Function Deleted");
                        FilePatching.RemoveFilePatches(fileName, patchFile);
                        break;
                    case WatcherChangeTypes.Changed:
                        //File Changed
                        Debug.Log($"Function Changed");
                        FilePatching.RemoveFilePatches(fileName, patchFile);
                        fileInfo = new FileInfo(fullPatchFilename);
                        break;
                }

                if (fileInfo != null && fileInfo.Exists)
                    FilePatching.ProcessPatchFile(fileInfo);

                SyncedJsonFiles[fileName].AssignLocalValue(LoadJsonText(fileName));
            }

            var patchWatcher = new FileSystemWatcher(Path.GetDirectoryName(fullPatchFilename), Path.GetFileName(fullPatchFilename));

            patchWatcher.Changed += ConsumePatchFileEvent;
            patchWatcher.Deleted += ConsumePatchFileEvent;
            patchWatcher.IncludeSubdirectories = true;
            patchWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            patchWatcher.EnableRaisingEvents = true;

        }


        public static string LoadJsonText(string filename)
        {
            var jsonFilePath = GetAssetPath(filename);
            if (string.IsNullOrEmpty(jsonFilePath))
                return null;

            var jsonFileText = File.ReadAllText(jsonFilePath);
            var patchedJsonFileText = FilePatching.ProcessConfigFile(filename, jsonFileText);
            if (OutputPatchedConfigFiles.Value && jsonFileText != patchedJsonFileText)
            {
                var debugFilePath = jsonFilePath.Replace(".json", "_patched.json");
                File.WriteAllText(debugFilePath, patchedJsonFileText);
            }
            return patchedJsonFileText;
        }

        public static AssetBundle LoadAssetBundle(string filename)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assetBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{filename}"));

            return assetBundle;
        }

        public static string GenerateAssetPathAtAssembly(string assetName)
        {
            var assembly = typeof(EpicLoot).Assembly;
            return Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, assetName);
        }

        public static string GetAssetPath(string assetName)
        {
            var assetFileName = Path.Combine(Paths.PluginPath, "EpicLoot", assetName);
            if (!File.Exists(assetFileName))
            {
                assetFileName = GenerateAssetPathAtAssembly(assetName);
                if (!File.Exists(assetFileName))
                {
                    LogError($"Could not find asset ({assetName})");
                    return null;
                }
            }

            return assetFileName;
        }

        public static bool CanBeMagicItem(ItemDrop.ItemData item)
        {
            return item != null && IsPlayerItem(item) && Nonstackable(item) && IsNotRestrictedItem(item) && AllowedMagicItemTypes.Contains(item.m_shared.m_itemType);
        }

        public static Sprite GetMagicItemBgSprite()
        {
            return HasAuga ? Assets.AugaItemBgSprite : Assets.GenericItemBgSprite;
        }

        public static Sprite GetEquippedSprite()
        {
            return HasAuga ? Assets.AugaEquippedSprite : Assets.EquippedSprite;
        }

        public static Sprite GetSetItemSprite()
        {
            return HasAuga ? Assets.AugaSetItemSprite : Assets.GenericSetItemSprite;
        }

        public static string GetMagicEffectPip(bool hasBeenAugmented)
        {
            return HasAuga ? (hasBeenAugmented ? "♢" : "♦") : (hasBeenAugmented ? "◇" : "◆");
        }

        private static bool IsNotRestrictedItem(ItemDrop.ItemData item)
        {
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

        public static void OnCharacterDeath(CharacterDrop characterDrop)
        {
            if (!CanCharacterDropLoot(characterDrop.m_character))
            {
                return;
            }

            var characterName = GetCharacterCleanName(characterDrop.m_character);
            var level = characterDrop.m_character.GetLevel();
            var dropPoint = characterDrop.m_character.GetCenterPoint() + characterDrop.transform.TransformVector(characterDrop.m_spawnOffset);

            OnCharacterDeath(characterName, level, dropPoint);
        }

        public static bool CanCharacterDropLoot(Character character)
        {
            return character != null && !character.IsTamed();
        }

        public static void OnCharacterDeath(string characterName, int level, Vector3 dropPoint)
        {
            var lootTables = LootRoller.GetLootTable(characterName);
            if (lootTables != null && lootTables.Count > 0)
            {
                var loot = LootRoller.RollLootTableAndSpawnObjects(lootTables, level, characterName, dropPoint);
                Log($"Rolling on loot table: {characterName} (lvl {level}), spawned {loot.Count} items at drop point({dropPoint}).");
                DropItems(loot, dropPoint);
                foreach (var l in loot)
                {
                    var itemData = l.GetComponent<ItemDrop>().m_itemData;
                    var magicItem = itemData.GetMagicItem();
                    if (magicItem != null)
                    {
                        Log($"  - {itemData.m_shared.m_name} <{l.transform.position}>: {string.Join(", ", magicItem.Effects.Select(x => x.EffectType.ToString()))}");
                    }
                }
            }
            else
            {
                Log($"Could not find loot table for: {characterName} (lvl {level})");
            }
        }

        public static void DropItems(List<GameObject> loot, Vector3 centerPos, float dropHemisphereRadius = 0.5f)
        {
            foreach (var item in loot)
            {
                var vector3 = Random.insideUnitSphere * dropHemisphereRadius;
                vector3.y = Mathf.Abs(vector3.y);
                item.transform.position = centerPos + vector3;
                item.transform.rotation = Quaternion.Euler(0.0f, Random.Range(0, 360), 0.0f);

                var rigidbody = item.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    var insideUnitSphere = Random.insideUnitSphere;
                    if (insideUnitSphere.y < 0.0)
                    {
                        insideUnitSphere.y = -insideUnitSphere.y;
                    }
                    rigidbody.AddForce(insideUnitSphere * 5f, ForceMode.VelocityChange);
                }
            }
        }

        private void PrintInfo()
        {
            const string devOutputPath = @"C:\Users\rknapp\Documents\GitHub\ValheimMods\EpicLoot";
            if (!Directory.Exists(devOutputPath))
            {
                return;
            }

            var t = new StringBuilder();
            t.AppendLine($"# EpicLoot Data v{Version}");
            t.AppendLine();
            t.AppendLine("*Author: RandyKnapp*");
            t.AppendLine("*Source: [Github](https://github.com/RandyKnapp/ValheimMods/tree/main/EpicLoot)*");
            t.AppendLine();

            // Magic item effects per rarity
            t.AppendLine("# Magic Effect Count Weights Per Rarity");
            t.AppendLine();
            t.AppendLine("Each time a **MagicItem** is rolled a number of **MagicItemEffects** are added based on its **Rarity**. The percent chance to roll each number of effects is found on the following table. These values are hardcoded.");
            t.AppendLine();
            t.AppendLine("The raw weight value is shown first, followed by the calculated percentage chance in parentheses.");
            t.AppendLine();
            t.AppendLine("|Rarity|1|2|3|4|5|6|");
            t.AppendLine("|--|--|--|--|--|--|--|");
            t.AppendLine(GetMagicEffectCountTableLine(ItemRarity.Magic));
            t.AppendLine(GetMagicEffectCountTableLine(ItemRarity.Rare));
            t.AppendLine(GetMagicEffectCountTableLine(ItemRarity.Epic));
            t.AppendLine(GetMagicEffectCountTableLine(ItemRarity.Legendary));
            t.AppendLine();

            var rarities = new List<ItemRarity>();
            foreach (ItemRarity value in Enum.GetValues(typeof(ItemRarity)))
            {
                rarities.Add(value);
            }

            var skillTypes = new List<Skills.SkillType>();
            foreach (Skills.SkillType value in Enum.GetValues(typeof(Skills.SkillType)))
            {
                if (value == Skills.SkillType.None
                    || value == Skills.SkillType.WoodCutting
                    || value == Skills.SkillType.Jump
                    || value == Skills.SkillType.Sneak
                    || value == Skills.SkillType.Run
                    || value == Skills.SkillType.Swim
                    || value == Skills.SkillType.All)
                {
                    continue;
                }
                skillTypes.Add(value);
            }

            // Magic item effects
            t.AppendLine("# MagicItemEffect List");
            t.AppendLine();
            t.AppendLine("The following lists all the built-in **MagicItemEffects**. MagicItemEffects are defined in `magiceffects.json` and are parsed and added " +
                         "to `MagicItemEffectDefinitions` on Awake. EpicLoot uses an string for the types of magic effects. You can add your own new types using your own identifiers.");
            t.AppendLine();
            t.AppendLine("Listen to the event `MagicItemEffectDefinitions.OnSetupMagicItemEffectDefinitions` (which gets called in `EpicLoot.Awake`) to add your own using instances of MagicItemEffectDefinition.");
            t.AppendLine();
            t.AppendLine("  * **Display Text:** This text appears in the tooltip for the magic item, with {0:?} replaced with the rolled value for the effect, formatted using the shown C# string format.");
            t.AppendLine("  * **Requirements:** A set of requirements.");
            t.AppendLine("    * **Flags:** A set of predefined flags to check certain weapon properties. The list of flags is: `NoRoll, ExclusiveSelf, ItemHasPhysicalDamage, ItemHasElementalDamage, ItemUsesDurability, ItemHasNegativeMovementSpeedModifier, ItemHasBlockPower, ItemHasNoParryPower, ItemHasParryPower, ItemHasArmor, ItemHasBackstabBonus, ItemUsesStaminaOnAttack`");
            t.AppendLine("    * **ExclusiveEffectTypes:** This effect may not be rolled on an item that has already rolled on of these effects");
            t.AppendLine($"    * **AllowedItemTypes:** This effect may only be rolled on items of a the types in this list. When this list is empty, this is usually done because this is a special effect type added programmatically  or currently not allowed to roll. Options are: `{string.Join(", ", AllowedMagicItemTypes)}`");
            t.AppendLine($"    * **ExcludedItemTypes:** This effect may only be rolled on items that are not one of the types on this list.");
            t.AppendLine($"    * **AllowedRarities:** This effect may only be rolled on an item of one of these rarities. Options are: `{string.Join(", ", rarities)}`");
            t.AppendLine($"    * **ExcludedRarities:** This effect may only be rolled on an item that is not of one of these rarities.");
            t.AppendLine($"    * **AllowedSkillTypes:** This effect may only be rolled on an item that uses one of these skill types. Options are: `{string.Join(", ", skillTypes)}`");
            t.AppendLine($"    * **ExcludedSkillTypes:** This effect may only be rolled on an item that does not use one of these skill types.");
            t.AppendLine("    * **AllowedItemNames:** This effect may only be rolled on an item with one of these names. Use the unlocalized shared name, i.e.: `$item_sword_iron`");
            t.AppendLine("    * **ExcludedItemNames:** This effect may only be rolled on an item that does not have one of these names.");
            t.AppendLine("    * **CustomFlags:** A set of any arbitrary strings for future use");
            t.AppendLine("  * **Value Per Rarity:** This effect may only be rolled on items of a rarity included in this table. The value is rolled using a linear distribution between Min and Max and divisible by the Increment.");
            t.AppendLine();

            foreach (var definitionEntry in MagicItemEffectDefinitions.AllDefinitions)
            {
                var def = definitionEntry.Value;
                t.AppendLine($"## {def.Type}");
                t.AppendLine();
                t.AppendLine($"> **Display Text:** {Localization.instance.Localize(def.DisplayText)}");
                t.AppendLine("> ");

                if (def.Prefixes.Count > 0)
                {
                    t.AppendLine($"> **Prefixes:** {Localization.instance.Localize(string.Join(", ", def.Prefixes))}");
                }

                if (def.Suffixes.Count > 0)
                {
                    t.AppendLine($"> **Suffixes:** {Localization.instance.Localize(string.Join(", ", def.Suffixes))}");
                }

                if (def.Prefixes.Count > 0 || def.Suffixes.Count > 0)
                {
                    t.AppendLine("> ");
                }

                var allowedItemTypes = def.GetAllowedItemTypes();
                t.AppendLine("> **Allowed Item Types:** " + (allowedItemTypes.Count == 0 ? "*None*" : string.Join(", ", allowedItemTypes)));
                t.AppendLine("> ");
                t.AppendLine("> **Requirements:**");
                t.Append(def.Requirements);

                if (def.HasRarityValues())
                {
                    t.AppendLine("> ");
                    t.AppendLine("> **Value Per Rarity:**");
                    t.AppendLine("> ");
                    t.AppendLine("> |Rarity|Min|Max|Increment|");
                    t.AppendLine("> |--|--|--|--|");

                    if (def.ValuesPerRarity.Magic != null)
                    {
                        var v = def.ValuesPerRarity.Magic;
                        t.AppendLine($"> |Magic|{v.MinValue}|{v.MaxValue}|{v.Increment}|");
                    }
                    if (def.ValuesPerRarity.Rare != null)
                    {
                        var v = def.ValuesPerRarity.Rare;
                        t.AppendLine($"> |Rare|{v.MinValue}|{v.MaxValue}|{v.Increment}|");
                    }
                    if (def.ValuesPerRarity.Epic != null)
                    {
                        var v = def.ValuesPerRarity.Epic;
                        t.AppendLine($"> |Epic|{v.MinValue}|{v.MaxValue}|{v.Increment}|");
                    }
                    if (def.ValuesPerRarity.Legendary != null)
                    {
                        var v = def.ValuesPerRarity.Legendary;
                        t.AppendLine($"> |Legendary|{v.MinValue}|{v.MaxValue}|{v.Increment}|");
                    }
                }
                if (!string.IsNullOrEmpty(def.Comment))
                {
                    t.AppendLine("> ");
                    t.AppendLine($"> ***Notes:*** *{def.Comment}*");
                }

                t.AppendLine();
            }

            // Item Sets
            t.AppendLine("# Item Sets");
            t.AppendLine();
            t.AppendLine("Sets of loot drop data that can be referenced in the loot tables");

            foreach (var lootTableEntry in LootRoller.ItemSets)
            {
                var itemSet = lootTableEntry.Value;

                t.AppendLine($"## {lootTableEntry.Key}");
                t.AppendLine();
                WriteLootList(t, 0, itemSet.Loot);
                t.AppendLine();
            }

            // Loot tables
            t.AppendLine("# Loot Tables");
            t.AppendLine();
            t.AppendLine("A list of every built-in loot table from the mod. The name of the loot table is the object name followed by a number signifying the level of the object.");

            foreach (var lootTableEntry in LootRoller.LootTables)
            {
                var list = lootTableEntry.Value;

                foreach (var lootTable in list)
                {
                    t.AppendLine($"## {lootTableEntry.Key}");
                    t.AppendLine();
                    WriteLootTableDrops(t, lootTable);
                    WriteLootTableItems(t, lootTable);
                    t.AppendLine();
                }
            }

            File.WriteAllText(Path.Combine(devOutputPath, "info.md"), t.ToString());
        }

        private static void WriteLootTableDrops(StringBuilder t, LootTable lootTable)
        {
            var highestLevel = lootTable.LeveledLoot != null && lootTable.LeveledLoot.Count > 0 ? lootTable.LeveledLoot.Max(x => x.Level) : 0;
            var limit = Mathf.Max(3, highestLevel);
            for (var i = 0; i < limit; i++)
            {
                var level = i + 1;
                var dropTable = LootRoller.GetDropsForLevel(lootTable, level, false);
                if (dropTable == null || dropTable.Count == 0)
                {
                    continue;
                }

                float total = dropTable.Sum(x => x.Value);
                if (total > 0)
                {
                    t.AppendLine($"> | Drops (lvl {level}) | Weight (Chance) |");
                    t.AppendLine($"> | -- | -- |");
                    foreach (var drop in dropTable)
                    {
                        var count = drop.Key;
                        var value = drop.Value;
                        var percent = (value / total) * 100;
                        t.AppendLine($"> | {count} | {value} ({percent:0.#}%) |");
                    }
                }
                t.AppendLine();
            }
        }

        private static void WriteLootTableItems(StringBuilder t, LootTable lootTable)
        {
            var highestLevel = lootTable.LeveledLoot != null && lootTable.LeveledLoot.Count > 0 ? lootTable.LeveledLoot.Max(x => x.Level) : 0;
            var limit = Mathf.Max(3, highestLevel);
            for (var i = 0; i < limit; i++)
            {
                var level = i + 1;
                var lootList = LootRoller.GetLootForLevel(lootTable, level, false);
                if (ArrayUtils.IsNullOrEmpty(lootList))
                {
                    continue;
                }

                WriteLootList(t, level, lootList);
            }
        }

        private static void WriteLootList(StringBuilder t, int level, LootDrop[] lootList)
        {
            var levelDisplay = level > 0 ? $" (lvl {level})" : "";
            t.AppendLine($"> | Items{levelDisplay} | Weight (Chance) | Magic | Rare | Epic | Legendary |");
            t.AppendLine("> | -- | -- | -- | -- | -- | -- |");

            float totalLootWeight = lootList.Sum(x => x.Weight);
            foreach (var lootDrop in lootList)
            {
                var percentChance = lootDrop.Weight / totalLootWeight * 100;
                if (lootDrop.Rarity == null || lootDrop.Rarity.Length == 0)
                {
                    t.AppendLine($"> | {lootDrop.Item} | {lootDrop.Weight} ({percentChance:0.#}%) | 1 (100%) | 0 (0%) | 0 (0%) | 0 (0%) |");
                    continue;
                }

                float rarityTotal = lootDrop.Rarity.Sum();
                float[] rarityPercent =
                {
                    lootDrop.Rarity[0] / rarityTotal * 100,
                    lootDrop.Rarity[1] / rarityTotal * 100,
                    lootDrop.Rarity[2] / rarityTotal * 100,
                    lootDrop.Rarity[3] / rarityTotal * 100,
                };
                t.AppendLine($"> | {lootDrop.Item} | {lootDrop.Weight} ({percentChance:0.#}%) " +
                             $"| {lootDrop.Rarity[0]} ({rarityPercent[0]:0.#}%) " +
                             $"| {lootDrop.Rarity[1]} ({rarityPercent[1]:0.#}%) " +
                             $"| {lootDrop.Rarity[2]:0.#} ({rarityPercent[2]:0.#}%) " +
                             $"| {lootDrop.Rarity[3]} ({rarityPercent[3]:0.#}%) |");
            }

            t.AppendLine();
        }

        private static string GetMagicEffectCountTableLine(ItemRarity rarity)
        {
            var effectCounts = LootRoller.GetEffectCountsPerRarity(rarity);
            float total = effectCounts.Sum(x => x.Value);
            var result = $"|{rarity}|";
            for (var i = 1; i <= 6; ++i)
            {
                var valueString = " ";
                var i1 = i;
                if (effectCounts.TryFind(x => x.Key == i1, out var found))
                {
                    var value = found.Value;
                    var percent = value / total * 100;
                    valueString = $"{value} ({percent:0.#}%)";
                }
                result += $"{valueString}|";
            }
            return result;
        }

        public static string GetSetItemColor()
        {
            return _setItemColor.Value;
        }

        public static string GetRarityDisplayName(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Magic:
                    return "$mod_epicloot_magic";
                case ItemRarity.Rare:
                    return "$mod_epicloot_rare";
                case ItemRarity.Epic:
                    return "$mod_epicloot_epic";
                case ItemRarity.Legendary:
                    return "$mod_epicloot_legendary";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
            }
        }

        public static string GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Magic:
                    return GetColor(_magicRarityColor.Value);
                case ItemRarity.Rare:
                    return GetColor(_rareRarityColor.Value);
                case ItemRarity.Epic:
                    return GetColor(_epicRarityColor.Value);
                case ItemRarity.Legendary:
                    return GetColor(_legendaryRarityColor.Value);
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
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
                    return Mathf.Clamp(_magicMaterialIconColor.Value, 0, 9);
                case ItemRarity.Rare:
                    return Mathf.Clamp(_rareMaterialIconColor.Value, 0, 9);
                case ItemRarity.Epic:
                    return Mathf.Clamp(_epicMaterialIconColor.Value, 0, 9);
                case ItemRarity.Legendary:
                    return Mathf.Clamp(_legendaryMaterialIconColor.Value, 0, 9);
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
            }
        }

        public static AudioClip GetMagicItemDropSFX(ItemRarity rarity)
        {
            return Assets.MagicItemDropSFX[(int) rarity];
        }

        public static GatedItemTypeMode GetGatedItemTypeMode()
        {
            return _gatedItemTypeModeConfig.Value;
        }

        public static BossDropMode GetBossTrophyDropMode()
        {
            return _bossTrophyDropMode.Value;
        }

        public static float GetBossTrophyDropPlayerRange()
        {
            return _bossTrophyDropPlayerRange.Value;
        }

        public static int GetAndvaranautRange()
        {
          return _andvaranautRange.Value;
        }

        public static bool IsAdventureModeEnabled()
        {
            return _adventureModeEnabled.Value;
        }

        private static void GenerateTranslations()
        {
            var jsonFile = LoadJsonText("magiceffects.json");
            var config = JsonConvert.DeserializeObject<MagicItemEffectsList>(jsonFile);

            var translations = new Dictionary<string, string>();

            foreach (var effectDef in config.MagicItemEffects)
            {
                if (string.IsNullOrEmpty(effectDef.Description))
                {
                    effectDef.Description = effectDef.DisplayText.Replace("display", "desc");
                    jsonFile = jsonFile.Replace($"\"DisplayText\" : \"{effectDef.DisplayText}\"", $"\"DisplayText\" : \"{effectDef.DisplayText}\",\n      \"Description\" : \"{effectDef.Description}\"");
                    translations.Add(effectDef.Description, "");
                }
            }

            var outputPath = GenerateAssetPathAtAssembly("magiceffects_translated.json");
            File.WriteAllText(outputPath, jsonFile);

            var translationsOutputPath = GenerateAssetPathAtAssembly("new_translations.json");
            var translationsText = "{\n" + string.Join("\n", translations.Select(x => $"  \"{x.Key}\": \"{x.Value}\",")) +"\n}";
            File.WriteAllText(translationsOutputPath, translationsText);
        }

        private static string Clean(string name)
        {
            return name.Replace("'", "").Replace(",", "").Trim().Replace(" ", "_").ToLowerInvariant();
        }

        private static void ReplaceValueList(List<string> values, string field, string label, MagicItemEffectDefinition effectDef, Dictionary<string, string> translations, ref string magicEffectsJson)
        {
            var newValues = new List<string>();
            for (var index = 0; index < values.Count; index++)
            {
                var value = values[index];
                string key;
                if (value.StartsWith("$"))
                {
                    key = value;
                }
                else
                {
                    key = GetId(effectDef, $"{field}{index + 1}");
                    AddTranslation(translations, key, value);
                }
                newValues.Add(key);
            }

            if (newValues.Count > 0)
            {
                var old = $"\"{label}\": [ {string.Join(", ", values.Select(x => $"\"{x}\""))} ]";
                var toReplace = $"\"{label}\": [\n        {string.Join(",\n        ", newValues.Select(x => (x.StartsWith("$") ? $"\"{x}\"" : $"\"${x}\"")))}\n      ]";
                magicEffectsJson = ReplaceTranslation(magicEffectsJson, old, toReplace);
            }
        }

        private static string GetId(MagicItemEffectDefinition effectDef, string field)
        {
            return $"mod_epicloot_me_{effectDef.Type.ToLowerInvariant()}_{field.ToLowerInvariant()}";
        }

        private static void AddTranslation(Dictionary<string, string> translations, string key, string value)
        {
            translations.Add(key, value);
        }

        private static string ReplaceTranslation(string jsonFile, string original, string locId)
        {
            return jsonFile.Replace(original, locId);
        }

        private static string SetupTranslation(MagicItemEffectDefinition effectDef, string value, string field, string replaceFormat, Dictionary<string, string> translations, string jsonFile)
        {
            if (string.IsNullOrEmpty(value) || value.StartsWith("$"))
            {
                return jsonFile;
            }

            var key = GetId(effectDef, field);
            AddTranslation(translations, key, value);
            return ReplaceTranslation(jsonFile, string.Format(replaceFormat, value), string.Format(replaceFormat, $"${key}"));
        }

        public static float GetWorldLuckFactor()
        {
            return _instance._worldLuckFactor;
        }

        public static void SetWorldLuckFactor(float luckFactor)
        {
            _instance._worldLuckFactor = luckFactor;
        }
    }
}
