using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using Common;
using EpicLoot.Crafting;
using ExtendedItemDataFramework;
using fastJSON;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EpicLoot
{
    public class Assets
    {
        public Sprite EquippedSprite;
        public Sprite GenericSetItemSprite;
        public Sprite GenericItemBgSprite;
        public GameObject[] MagicItemLootBeamPrefabs = new GameObject[4];
        public readonly Dictionary<string, GameObject[]> CraftingMaterialPrefabs = new Dictionary<string, GameObject[]>();
    }

    public class PieceDef
    {
        public string Table;
        public string CraftingStation;
        public string ExtendStation;
        public List<RecipeRequirementConfig> Resources = new List<RecipeRequirementConfig>();
    }

    [BepInPlugin(PluginId, "Epic Loot", Version)]
    [BepInDependency("randyknapp.mods.extendeditemdataframework")]
    public class EpicLoot : BaseUnityPlugin
    {
        private const string PluginId = "randyknapp.mods.epicloot";
        private const string Version = "0.5.11";

        private static ConfigEntry<string> _setItemColor;
        private static ConfigEntry<string> _magicRarityColor;
        private static ConfigEntry<string> _rareRarityColor;
        private static ConfigEntry<string> _epicRarityColor;
        private static ConfigEntry<string> _legendaryRarityColor;
        private static ConfigEntry<int> _magicMaterialIconColor;
        private static ConfigEntry<int> _rareMaterialIconColor;
        private static ConfigEntry<int> _epicMaterialIconColor;
        private static ConfigEntry<int> _legendaryMaterialIconColor;
        private static ConfigEntry<string> _magicRarityDisplayName;
        private static ConfigEntry<string> _rareRarityDisplayName;
        private static ConfigEntry<string> _epicRarityDisplayName;
        private static ConfigEntry<string> _legendaryRarityDisplayName;
        public static ConfigEntry<bool> UseScrollingCraftDescription;

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

        public static readonly List<string> RestrictedItemNames = new List<string>
        {
            "$item_tankard", "Unarmed", "CAPE TEST", "Cheat sword", "$item_sword_fire", "$item_tankard_odin", "$item_cape_odin", "$item_helmet_odin"
        };

        public static readonly Assets Assets = new Assets();
        public static readonly List<GameObject> RegisteredPrefabs = new List<GameObject>();
        public static readonly List<GameObject> RegisteredItemPrefabs = new List<GameObject>();
        public static readonly Dictionary<GameObject, PieceDef> RegisteredPieces = new Dictionary<GameObject, PieceDef>();

        public static event Action LootTableLoaded;

        private Harmony _harmony;

        [UsedImplicitly]
        private void Awake()
        {
            _magicRarityColor = Config.Bind("Item Colors", "Magic Rarity Color", "Blue", "The color of Magic rarity items, the lowest magic item tier. (Optional, use an HTML hex color starting with # to have a custom color.) Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
            _magicMaterialIconColor = Config.Bind("Item Colors", "Magic Crafting Material Icon Index", 5, "Indicates the color of the icon used for magic crafting materials. A number between 0 and 9. Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
            _rareRarityColor = Config.Bind("Item Colors", "Rare Rarity Color", "Yellow", "The color of Rare rarity items, the second magic item tier. (Optional, use an HTML hex color starting with # to have a custom color.) Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
            _rareMaterialIconColor = Config.Bind("Item Colors", "Rare Crafting Material Icon Index", 2, "Indicates the color of the icon used for rare crafting materials. A number between 0 and 9. Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
            _epicRarityColor = Config.Bind("Item Colors", "Epic Rarity Color", "Purple", "The color of Epic rarity items, the third magic item tier. (Optional, use an HTML hex color starting with # to have a custom color.) Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
            _epicMaterialIconColor = Config.Bind("Item Colors", "Epic Crafting Material Icon Index", 7, "Indicates the color of the icon used for epic crafting materials. A number between 0 and 9. Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
            _legendaryRarityColor = Config.Bind("Item Colors", "Legendary Rarity Color", "Teal", "The color of Legendary rarity items, the highest magic item tier. (Optional, use an HTML hex color starting with # to have a custom color.) Available options: Red, Orange, Yellow, Green, Teal, Blue, Indigo, Purple, Pink, Gray");
            _legendaryMaterialIconColor = Config.Bind("Item Colors", "Legendary Crafting Material Icon Index", 4, "Indicates the color of the icon used for legendary crafting materials. A number between 0 and 9. Available options: 0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Teal, 5=Blue, 6=Indigo, 7=Purple, 8=Pink, 9=Gray");
            _setItemColor = Config.Bind("Item Colors", "Set Item Color", "#26ffff", "The color of set item text and the set item icon. Use a hex color, default is cyan");
            _magicRarityDisplayName = Config.Bind("Rarity", "Magic Rarity Display Name", "Magic", "The name of the lowest rarity.");
            _rareRarityDisplayName = Config.Bind("Rarity", "Rare Rarity Display Name", "Rare", "The name of the second rarity.");
            _epicRarityDisplayName = Config.Bind("Rarity", "Epic Rarity Display Name", "Epic", "The name of the third rarity.");
            _legendaryRarityDisplayName = Config.Bind("Rarity", "Legendary Rarity Display Name", "Legendary", "The name of the highest rarity.");
            UseScrollingCraftDescription = Config.Bind("Crafting UI", "Use Scrolling Craft Description", true, "Changes the item description in the crafting panel to scroll instead of scale when it gets too long for the space.");

            MagicItemEffectDefinitions.SetupMagicItemEffectDefinitions();

            LootRoller.Initialize(LoadJsonFile<LootConfig>("loottables.json"));
            PrintInfo();

            LoadAssets();

            ExtendedItemData.LoadExtendedItemData += SetupTestMagicItem;
            ExtendedItemData.LoadExtendedItemData += MagicItemComponent.OnNewExtendedItemData;
            ExtendedItemData.NewExtendedItemData += MagicItemComponent.OnNewExtendedItemData;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);

            LootTableLoaded?.Invoke();
        }

        /*private void Update()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            Debug.LogWarning("== Objects under cursor: ==");
            if (Input.GetKeyDown(KeyCode.I))
            {
                results.ForEach((result) => {
                    Debug.Log($"- {result.gameObject.name} ({result.gameObject.transform.parent.name})");
                });
            }
        }*/

        private void LoadAssets()
        {
            var assetBundle = LoadAssetBundle("epicloot");
            Assets.EquippedSprite = assetBundle.LoadAsset<Sprite>("Equipped");
            Assets.GenericSetItemSprite = assetBundle.LoadAsset<Sprite>("GenericSetItemMarker");
            Assets.GenericItemBgSprite = assetBundle.LoadAsset<Sprite>("GenericItemBg");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Magic] = assetBundle.LoadAsset<GameObject>("MagicLootBeam");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Rare] = assetBundle.LoadAsset<GameObject>("RareLootBeam");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Epic] = assetBundle.LoadAsset<GameObject>("EpicLootBeam");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Legendary] = assetBundle.LoadAsset<GameObject>("LegendaryLootBeam");

            LoadCraftingMaterialAssets(assetBundle, "Runestone");

            LoadCraftingMaterialAssets(assetBundle, "Shard");
            LoadCraftingMaterialAssets(assetBundle, "Dust");
            LoadCraftingMaterialAssets(assetBundle, "Reagent");
            LoadCraftingMaterialAssets(assetBundle, "Essence");

            LoadStationExtension(assetBundle, "piece_enchanter", new PieceDef()
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
            LoadStationExtension(assetBundle, "piece_augmenter", new PieceDef()
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
        }

        private static void LoadStationExtension(AssetBundle assetBundle, string assetName, PieceDef pieceDef)
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
                    Debug.LogError($"Tried to load asset {assetName} but it does not exist in the asset bundle!");
                    continue;
                }
                prefabs[(int) rarity] = prefab;
                RegisteredPrefabs.Add(prefab);
                RegisteredItemPrefabs.Add(prefab);
            }
            Assets.CraftingMaterialPrefabs.Add(type, prefabs);
        }

        private static void SetupTestMagicItem(ExtendedItemData itemdata)
        {
            // Weapon (Club)
            /*if (itemdata.GetUniqueId() == "1493f9a4-65b4-41e3-8871-611ec8cb7564")
            {
                var magicItem = new MagicItem {Rarity = ItemRarity.Epic};
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.ModifyAttackStaminaUse), magicItem.Rarity));
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.Indestructible), magicItem.Rarity));
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.Weightless), magicItem.Rarity));
                //var magicItem = RollMagicItem(new LootDrop() { Rarity = { 6, 4, 2, 1 } }, itemdata);
                itemdata.ReplaceComponent<MagicItemComponent>().SetMagicItem(magicItem);
            }
            // Armor (Bronze Cuirass)
            else if (itemdata.GetUniqueId() == "84c006c7-3819-463c-b3b6-cb812f184655")
            {
                var magicItem = new MagicItem {Rarity = ItemRarity.Epic };
                //var magicItem = RollMagicItem(new LootDrop() { Rarity = { 6, 4, 2, 1 } }, itemdata);
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.ModifyMovementSpeed), magicItem.Rarity));
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.Indestructible), magicItem.Rarity));
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.Weightless), magicItem.Rarity));
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.AddCarryWeight), magicItem.Rarity));
                itemdata.ReplaceComponent<MagicItemComponent>().SetMagicItem(magicItem);
            }
            // Shield (Wood Shield)
            else if (itemdata.GetUniqueId() == "c0d8fb31-04dd-4499-b347-d0484416f159")
            {
                var magicItem = new MagicItem {Rarity = ItemRarity.Epic};
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.ModifyBlockStaminaUse), magicItem.Rarity));
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.Weightless), magicItem.Rarity));
                //var magicItem = RollMagicItem(new LootDrop() { Rarity = { 6, 4, 2, 1 } }, itemdata);
                itemdata.ReplaceComponent<MagicItemComponent>().SetMagicItem(magicItem);
            }
            // Legs (Troll Hide Legs)
            else if (itemdata.GetUniqueId() == "ec539738-6a73-492b-85d8-ce80eb0944f1")
            {
                var magicItem = new MagicItem { Rarity = ItemRarity.Epic };
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.ModifyMovementSpeed), magicItem.Rarity));
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.ModifySprintStaminaUse), magicItem.Rarity));
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.ModifyJumpStaminaUse), magicItem.Rarity));
                magicItem.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(MagicEffectType.AddCarryWeight), magicItem.Rarity));
                itemdata.ReplaceComponent<MagicItemComponent>().SetMagicItem(magicItem);
            }*/
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            ExtendedItemData.LoadExtendedItemData -= SetupTestMagicItem;
            _harmony?.UnpatchAll(PluginId);
        }

        public static void TryRegisterPrefabs(ZNetScene zNetScene)
        {
            if (zNetScene == null)
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
                var pieceDef = entry.Value;

                var piece = prefab.GetComponent<Piece>();

                var pieceTable = pieceTables.Find(x => x.name == pieceDef.Table);
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
                    piece.m_placeEffect.m_effectPrefabs.AddRangeToArray(otherPiece.m_placeEffect.m_effectPrefabs);
                }
            }
        }

        public static void TryRegisterItems()
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
            {
                return;
            }

            // Fix custom name and icons for crafting materials
            foreach (var prefab in RegisteredItemPrefabs)
            {
                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    if (itemDrop.m_itemData.IsMagicCraftingMaterial() || itemDrop.m_itemData.IsRunestone())
                    {
                        var rarity = itemDrop.m_itemData.GetRarity();
                        var correctName = GetRarityDisplayName(rarity);
                        if (!itemDrop.m_itemData.m_shared.m_name.StartsWith(correctName))
                        {
                            itemDrop.m_itemData.m_shared.m_name = itemDrop.m_itemData.m_shared.m_name.Replace(rarity.ToString(), correctName);
                        }

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

            var pieceTables = new List<PieceTable>();
            foreach (var itemPrefab in ObjectDB.instance.m_items)
            {
                var item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
                if (item.m_shared.m_buildPieces != null && !pieceTables.Contains(item.m_shared.m_buildPieces))
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
        }

        public static void TryRegisterRecipes()
        {
            Recipes_Setup.SetupRecipes();
        }

        private static T LoadJsonFile<T>(string filename) where T : class
        {
            var jsonFileName = GetAssetPath(filename);
            if (!string.IsNullOrEmpty(jsonFileName))
            {
                var jsonFile = File.ReadAllText(jsonFileName);
                return JSON.ToObject<T>(jsonFile);
            }

            return null;
        }

        private static AssetBundle LoadAssetBundle(string filename)
        {
            var assetBundlePath = GetAssetPath(filename);
            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                return AssetBundle.LoadFromFile(assetBundlePath);
            }

            return null;
        }

        private static string GetAssetPath(string assetName)
        {
            var assetFileName = Path.Combine(Paths.PluginPath, "EpicLoot", assetName);
            if (!File.Exists(assetFileName))
            {
                Assembly assembly = typeof(EpicLoot).Assembly;
                assetFileName = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, assetName);
                if (!File.Exists(assetFileName))
                {
                    Debug.LogError($"Could not find asset ({assetName})");
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
            return Assets.GenericItemBgSprite;
        }

        private static bool IsNotRestrictedItem(ItemDrop.ItemData item)
        {
            // This is dumb, but it's the only way I can think of to do this
            return !RestrictedItemNames.Contains(item.m_shared.m_name);
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
            var characterName = GetCharacterCleanName(characterDrop.m_character);
            var level = characterDrop.m_character.GetLevel();
            var dropPoint = characterDrop.m_character.GetCenterPoint() + characterDrop.transform.TransformVector(characterDrop.m_spawnOffset);

            OnCharacterDeath(characterName, level, dropPoint);
        }

        public static void OnCharacterDeath(string characterName, int level, Vector3 dropPoint)
        {
            var lootTables = LootRoller.GetLootTable(characterName);
            if (lootTables != null && lootTables.Count > 0)
            {
                List<GameObject> loot = LootRoller.RollLootTableAndSpawnObjects(lootTables, level, characterName, dropPoint);
                Debug.Log($"Rolling on loot table: {characterName} (lvl {level}), spawned {loot.Count} items at drop point({dropPoint}).");
                DropItems(loot, dropPoint);
                foreach (var l in loot)
                {
                    var itemData = l.GetComponent<ItemDrop>().m_itemData;
                    var magicItem = itemData.GetMagicItem();
                    if (magicItem != null)
                    {
                        Debug.Log($"  - {itemData.m_shared.m_name} <{l.transform.position}>: {string.Join(", ", magicItem.Effects.Select(x => x.EffectType.ToString()))}");
                    }
                }
            }
            else
            {
                Debug.Log($"Could not find loot table for: {characterName} (lvl {level})");
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

            // Magic item effects
            t.AppendLine("# MagicItemEffect List");
            t.AppendLine();
            t.AppendLine("The following lists all the built-in **MagicItemEffects**. MagicItemEffects are hardcoded in `MagicItemEffectDefinitions_Setup.cs` and " +
                         "added to `MagicItemEffectDefinitions`. EpicLoot uses an enum for the types of magic effects, but the backing field underneath is an int. " +
                         "You can add your own new types using your own enum that starts after `MagicEffectType.MagicEffectEnumEnd` and cast it to `MagicEffectType` " +
                         "or use your own range of int identifiers.");
            t.AppendLine();
            t.AppendLine("Listen to the event `MagicItemEffectDefinitions.OnSetupMagicItemEffectDefinitions` (which gets called in `EpicLoot.Awake`) to add your own.");
            t.AppendLine();
            t.AppendLine("The int value of the type is displayed in parentheses after the name.");
            t.AppendLine();
            t.AppendLine("  * **Display Text:** This text appears in the tooltip for the magic item, with {0:?} replaced with the rolled value for the effect, formatted using the shown C# string format.");
            t.AppendLine("  * **Allowed Item Types:** This effect may only be rolled on items of a the types in this list. When this list is empty, this is usually done because this is a special effect type added programmatically  or currently not allowed to roll.");
            t.AppendLine("  * **Requirement:** A function called when attempting to add this effect to an item. The `Requirement` function must return true for this effect to be able to be added to this magic item.");
            t.AppendLine("  * **Value Per Rarity:** This effect may only be rolled on items of a rarity included in this table. The value is rolled using a linear distribution between Min and Max and divisible by the Increment.");
            t.AppendLine();
            t.AppendLine("Some lists of effect types are used in requirements to consolidate code. They are: PhysicalDamageEffects, ElementalDamageEffects, and AllDamageEffects. Included here for your reference:");
            t.AppendLine();
            t.AppendLine("  * **`PhysicalDamageEffects`:** AddBluntDamage, AddSlashingDamage, AddPiercingDamage");
            t.AppendLine("  * **`ElementalDamageEffects`:** AddFireDamage, AddFrostDamage, AddLightningDamage");
            t.AppendLine("  * **`AllDamageEffects`:** AddBluntDamage, AddSlashingDamage, AddPiercingDamage, AddFireDamage, AddFrostDamage, AddLightningDamage, AddPoisonDamage, AddSpiritDamage");
            t.AppendLine();

            Dictionary<string, string> requirementSource = null;
            const string devSourceFile = @"C:\Users\rknapp\Documents\GitHub\ValheimMods\EpicLoot\MagicItemEffectDefinitions_Setup.cs";
            if (File.Exists(devSourceFile))
            {
                requirementSource = ParseSource(File.ReadLines(devSourceFile));
            }

            foreach (var definitionEntry in MagicItemEffectDefinitions.AllDefinitions)
            {
                var def = definitionEntry.Value;
                t.AppendLine($"## {def.Type} ({def.IntType})");
                t.AppendLine();
                t.AppendLine($"> **Display Text:** {def.DisplayText}");
                t.AppendLine("> ");
                t.AppendLine("> **Allowed Item Types:** " + (def.AllowedItemTypes.Count == 0 ? "*None*" : string.Join(", ", def.AllowedItemTypes)));

                if (requirementSource != null && requirementSource.ContainsKey(def.Type.ToString()))
                {
                    t.AppendLine("> ");
                    t.AppendLine("> **Requirement:**");
                    t.AppendLine("> ```");
                    t.AppendLine($"> {requirementSource[def.Type.ToString()]}");
                    t.AppendLine("> ```");
                    t.AppendLine("> ");
                }

                if (def.ValuesPerRarity.Count > 0)
                {
                    t.AppendLine("> ");
                    t.AppendLine("> **Value Per Rarity:**");
                    t.AppendLine("> ");
                    t.AppendLine("> |Rarity|Min|Max|Increment|");
                    t.AppendLine("> |--|--|--|--|");
                    foreach (var entry in def.ValuesPerRarity)
                    {
                        var v = entry.Value;
                        t.AppendLine($"> |{entry.Key}|{v.MinValue}|{v.MaxValue}|{v.Increment}|");
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
                WriteLootList(t, string.Empty, itemSet.Loot);
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

            //var outputFilePath = Path.Combine(Path.GetDirectoryName(typeof(EpicLoot).Assembly.Location), "info.md");
            //File.WriteAllText(outputFilePath, t.ToString());

            const string devOutputPath = @"C:\Users\rknapp\Documents\GitHub\ValheimMods\EpicLoot";
            if (Directory.Exists(devOutputPath))
            {
                File.WriteAllText(Path.Combine(devOutputPath, "info.md"), t.ToString());
            }
        }

        private static void WriteLootTableDrops(StringBuilder t, LootTable lootTable)
        {
            var dropTables = new[] { lootTable.Drops, lootTable.Drops2, lootTable.Drops3 };
            for (var i = 0; i < 3; i++)
            {
                var levelDisplay = $" (lvl {i + 1})";
                if (i == 0 && ArrayUtils.IsNullOrEmpty(lootTable.Drops2) && ArrayUtils.IsNullOrEmpty(lootTable.Drops3))
                {
                    levelDisplay = "";
                }
                else if (i == 0 && ArrayUtils.IsNullOrEmpty(lootTable.Drops2))
                {
                    levelDisplay = " (lvl 1, 2)";
                }
                else if (i == 0 && ArrayUtils.IsNullOrEmpty(lootTable.Drops3))
                {
                    levelDisplay = " (lvl 1, 3)";
                }

                var dropTable = dropTables[i];
                if (ArrayUtils.IsNullOrEmpty(dropTable))
                {
                    continue;
                }

                float total = lootTable.Drops.Sum(x => x.Length > 1 ? x[1] : 0);
                if (total > 0)
                {
                    t.AppendLine($"> | Drops{levelDisplay} | Weight (Chance) |");
                    t.AppendLine($"> | -- | -- |");
                    foreach (var drop in dropTable)
                    {
                        var count = drop.Length > 0 ? drop[0] : 0;
                        var value = drop.Length > 1 ? drop[1] : 0;
                        var percent = (value / total) * 100;
                        t.AppendLine($"> | {count} | {value} ({percent:0.#}%) |");
                    }
                }
                t.AppendLine();
            }
        }

        private static void WriteLootTableItems(StringBuilder t, LootTable lootTable)
        {
            var lootLists = new[] { lootTable.Loot, lootTable.Loot2, lootTable.Loot3 };

            for (var i = 0; i < 3; i++)
            {
                var levelDisplay = $" (lvl {i + 1}+)";
                if (i == 0 && ArrayUtils.IsNullOrEmpty(lootTable.Loot2) && ArrayUtils.IsNullOrEmpty(lootTable.Loot3))
                {
                    levelDisplay = "";
                }

                var lootList = lootLists[i];
                if (ArrayUtils.IsNullOrEmpty(lootList))
                {
                    continue;
                }

                WriteLootList(t, levelDisplay, lootList);
            }
        }

        private static void WriteLootList(StringBuilder t, string levelDisplay, LootDrop[] lootList)
        {
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

        private static Dictionary<string, string> ParseSource(IEnumerable<string> lines)
        {
            var results = new Dictionary<string, string>();
            var currentType = "";
            foreach (var sourceLine in lines)
            {
                var line = sourceLine.Trim();
                if (string.IsNullOrEmpty(currentType))
                {
                    if (line.StartsWith("Type = "))
                    {
                        var start = line.IndexOf(".", StringComparison.InvariantCulture);
                        var end = line.IndexOf(",", StringComparison.InvariantCulture);
                        if (start < 0 || end < 0)
                        {
                            continue;
                        }

                        start += 1;
                        currentType = line.Substring(start, end - start);
                    }
                }
                else
                {
                    if (line.StartsWith("});") || line.StartsWith("Add("))
                    {
                        currentType = "";
                    }
                    else if (line.StartsWith("Requirement"))
                    {
                        var start = ("Requirement = ").Length;
                        var end = line.Length - 1;
                        var requirementText = line.Substring(start, end - start);
                        results.Add(currentType, requirementText);
                    }
                }
            }

            return results;
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
                    return _magicRarityDisplayName.Value;
                case ItemRarity.Rare:
                    return _rareRarityDisplayName.Value;
                case ItemRarity.Epic:
                    return _epicRarityDisplayName.Value;
                case ItemRarity.Legendary:
                    return _legendaryRarityDisplayName.Value;
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
    }
}
