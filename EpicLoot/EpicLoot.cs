using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using Common;
using ExtendedItemDataFramework;
using fastJSON;
using HarmonyLib;
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
    }

    [BepInPlugin("randyknapp.mods.epicloot", "Epic Loot", Version)]
    [BepInDependency("randyknapp.mods.extendeditemdataframework")]
    public class EpicLoot : BaseUnityPlugin
    {
        private const string Version = "0.1.0";

        public static ConfigEntry<string> SetItemColor;
        public static ConfigEntry<string> MagicRarityColor;
        public static ConfigEntry<string> RareRarityColor;
        public static ConfigEntry<string> EpicRarityColor;
        public static ConfigEntry<string> LegendaryRarityColor;

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

        public static readonly List<string> RestrictedItemNames = new List<string>
        {
            "$item_tankard", "$item_tankard_odin", "Unarmed", "CAPE TEST"
        };

        public static Dictionary<ItemRarity, Dictionary<int, float>> MagicEffectCountWeightsPerRarity = new Dictionary<ItemRarity, Dictionary<int, float>>()
        {
            { ItemRarity.Magic, new Dictionary<int, float>() { { 1, 80 }, { 2, 18 }, { 3, 2 } } },
            { ItemRarity.Rare, new Dictionary<int, float>() { { 2, 80 }, { 3, 18 }, { 4, 2 } } },
            { ItemRarity.Epic, new Dictionary<int, float>() { { 3, 80 }, { 4, 18 }, { 5, 2 } } },
            { ItemRarity.Legendary, new Dictionary<int, float>() { { 4, 80 }, { 5, 18 }, { 6, 2 } } }
        };

        public static Assets Assets = new Assets();
        public static Dictionary<string, List<LootTable>> LootTables = new Dictionary<string, List<LootTable>>();

        public static event Action LootTableLoaded;
        public static event Action<string, Character, List<LootTable>> CharacterDied;
        public static event Action<ExtendedItemData, MagicItem> MagicItemGenerated;

        private Harmony _harmony;

        private static WeightedRandomCollection<int[]> _weightedDropCountTable;
        private static WeightedRandomCollection<LootDrop> _weightedLootTable;
        private static WeightedRandomCollection<MagicItemEffectDefinition> _weightedEffectTable;
        private static WeightedRandomCollection<KeyValuePair<int, float>> _weightedEffectCountTable;
        private static WeightedRandomCollection<KeyValuePair<ItemRarity, int>> _weightedRarityTable;

        private void Awake()
        {
            var random = new System.Random();
            _weightedDropCountTable = new WeightedRandomCollection<int[]>(random);
            _weightedLootTable = new WeightedRandomCollection<LootDrop>(random);
            _weightedEffectTable = new WeightedRandomCollection<MagicItemEffectDefinition>(random);
            _weightedEffectCountTable = new WeightedRandomCollection<KeyValuePair<int, float>>(random);
            _weightedRarityTable = new WeightedRandomCollection<KeyValuePair<ItemRarity, int>>(random);

            MagicRarityColor = Config.Bind("Item Colors", "Magic Rarity Color", "#00abff", "The color of Magic rarity items, the lowest magic item tier. Default is blue.");
            RareRarityColor = Config.Bind("Item Colors", "Rare Rarity Color", "#ffff75", "The color of Rare rarity items, the second magic item tier. Default is yellow.");
            EpicRarityColor = Config.Bind("Item Colors", "Epic Rarity Color", "#d078ff", "The color of Epic rarity items, the third magic item tier. Default is purple.");
            LegendaryRarityColor = Config.Bind("Item Colors", "Legendary Rarity Color", "#18e775", "The color of Legendary rarity items, the highest magic item tier. Default is green.");
            SetItemColor = Config.Bind("Item Colors", "Set Item Color", "#26ffff", "The color of set item text and the set item icon. Default is cyan.");

            MagicItemEffectDefinitions.SetupMagicItemEffectDefinitions();

            LootTables.Clear();
            var lootConfig = LoadJsonFile<LootConfig>("loottables.json");
            AddLootTableConfig(lootConfig);
            PrintInfo();

            var assetBundle = LoadAssetBundle("epicloot");
            Assets.EquippedSprite = assetBundle.LoadAsset<Sprite>("Equipped");
            Assets.GenericSetItemSprite = assetBundle.LoadAsset<Sprite>("GenericSetItemMarker");
            Assets.GenericItemBgSprite = assetBundle.LoadAsset<Sprite>("GenericItemBg");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Magic] = assetBundle.LoadAsset<GameObject>("MagicLootBeam");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Rare] = assetBundle.LoadAsset<GameObject>("RareLootBeam");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Epic] = assetBundle.LoadAsset<GameObject>("EpicLootBeam");
            Assets.MagicItemLootBeamPrefabs[(int)ItemRarity.Legendary] = assetBundle.LoadAsset<GameObject>("LegendaryLootBeam");

            ExtendedItemData.LoadExtendedItemData += SetupTestMagicItem;
            ExtendedItemData.LoadExtendedItemData += MagicItemComponent.OnNewExtendedItemData;
            ExtendedItemData.NewExtendedItemData += MagicItemComponent.OnNewExtendedItemData;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            LootTableLoaded?.Invoke();
        }

        private static void SetupTestMagicItem(ExtendedItemData itemdata)
        {
            // Weapon (Club)
            if (itemdata.GetUniqueId() == "1493f9a4-65b4-41e3-8871-611ec8cb7564")
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
            }
        }

        private void OnDestroy()
        {
            ExtendedItemData.LoadExtendedItemData -= SetupTestMagicItem;
            _harmony?.UnpatchAll();
        }

        public static void AddLootTableConfig(LootConfig lootConfig)
        {
            foreach (var lootTable in lootConfig.LootTables)
            {
                AddLootTable(lootTable);
            }
        }

        public static void AddLootTable(LootTable lootTable)
        {
            var key = lootTable.Object;
            var levels = (lootTable.Level != null && lootTable.Level.Length > 0) ? string.Join(", ", lootTable.Level) : "all";
            Debug.LogWarning($"Added LootTable: {key} ({levels})");
            if (!LootTables.ContainsKey(key))
            {
                LootTables.Add(key, new List<LootTable>());
            }
            LootTables[key].Add(lootTable);
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

        private static string GetAssetPath(string assetName, bool ignoreErrors = false)
        {
            var assetFileName = Path.Combine(Paths.PluginPath, "EpicLoot", assetName);
            if (!File.Exists(assetFileName))
            {
                Assembly assembly = typeof(EpicLoot).Assembly;
                assetFileName = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
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

        public static void OnCharacterDeath(CharacterDrop characterDrop)
        {
            var characterName = characterDrop.name.Replace("(Clone)", "").Trim();
            var level = characterDrop.m_character.m_level;
            var lootTable = GetLootTable(characterName, level);
            if (lootTable != null)
            {
                Debug.Log($"CharacterDrop OnDeath: {characterName} (lvl {level})");
                CharacterDied?.Invoke(characterName, characterDrop.m_character, lootTable);
                List<GameObject> loot = RollLootTableAndSpawnObjects(lootTable, characterName);
                DropItems(loot, characterDrop.m_character.GetCenterPoint() + characterDrop.transform.TransformVector(characterDrop.m_spawnOffset));
            }
            else
            {
                Debug.Log($"CharacterDrop OnDeath (no loot table): {characterName} (lvl {level})");
            }
        }

        public static List<GameObject> RollLootTableAndSpawnObjects(List<LootTable> lootTables, string objectName)
        {
            return RollLootTableInternal(lootTables, objectName);
        }

        public static List<GameObject> RollLootTableAndSpawnObjects(LootTable lootTable, string objectName)
        {
            return RollLootTableInternal(lootTable, objectName);
        }

        public static List<ItemDrop.ItemData> RollLootTable(List<LootTable> lootTables, string objectName)
        {
            var results = new List<ItemDrop.ItemData>();
            var gameObjects = RollLootTableInternal(lootTables, objectName);
            foreach (var itemObject in gameObjects)
            {
                results.Add(itemObject.GetComponent<ItemDrop>().m_itemData);
                Destroy(itemObject);
            }

            return results;
        }

        public static List<ItemDrop.ItemData> RollLootTable(LootTable lootTable, string objectName)
        {
            var results = new List<ItemDrop.ItemData>();
            var gameObjects = RollLootTableInternal(lootTable, objectName);
            foreach (var itemObject in gameObjects)
            {
                results.Add(itemObject.GetComponent<ItemDrop>().m_itemData);
                Destroy(itemObject);
            }

            return results;
        }

        private static List<GameObject> RollLootTableInternal(List<LootTable> lootTables, string objectName)
        {
            var results = new List<GameObject>();
            foreach (var lootTable in lootTables)
            {
                results.AddRange(RollLootTableInternal(lootTable, objectName));
            }
            return results;
        }

        private static List<GameObject> RollLootTableInternal(LootTable lootTable, string objectName)
        {
            var results = new List<GameObject>();

            _weightedDropCountTable.Setup(lootTable.Drops, dropPair => dropPair.Length == 2 ? dropPair[1] : 1);
            var dropCountRollResult = _weightedDropCountTable.Roll();
            var dropCount = dropCountRollResult.Length >= 1 ? dropCountRollResult[0] : 0;
            if (dropCount == 0)
            {
                return results;
            }

            _weightedLootTable.Setup(lootTable.Loot, x => x.Weight);
            var selectedDrops = _weightedLootTable.Roll(dropCount);

            foreach (var lootDrop in selectedDrops)
            {
                var itemPrefab = ObjectDB.instance.GetItemPrefab(lootDrop.Item);
                if (itemPrefab == null)
                {
                    Debug.LogError($"Tried to spawn loot ({lootDrop.Item}) for ({objectName}), but the item prefab was not found!");
                    continue;
                }

                var item = Instantiate(itemPrefab);
                var itemDrop = item.GetComponent<ItemDrop>();
                if (!CanBeMagicItem(itemDrop.m_itemData))
                {
                    Debug.LogError($"Tried to spawn loot ({lootDrop.Item}) for ({objectName}), but the item type ({itemDrop.m_itemData.m_shared.m_itemType}) is not allowed as a magic item!");
                    continue;
                }

                var itemData = new ExtendedItemData(itemDrop.m_itemData);
                var magicItemComponent = itemData.AddComponent<MagicItemComponent>();
                var magicItem = RollMagicItem(lootDrop, itemData);
                magicItemComponent.SetMagicItem(magicItem);

                itemDrop.m_itemData = itemData;
                results.Add(item);

                MagicItemGenerated?.Invoke(itemData, magicItem);
            }

            return results;
        }

        public static MagicItem RollMagicItem(LootDrop lootDrop, ExtendedItemData baseItem)
        {
            var magicItem = new MagicItem();
            magicItem.Rarity = RollItemRarity(lootDrop);

            var effectCount = RollEffectCountPerRarity(magicItem.Rarity);
           
            for (int i = 0; i < effectCount; i++)
            {
                var availableEffects = MagicItemEffectDefinitions.GetAvailableEffects(baseItem, magicItem);
                if (availableEffects.Count == 0)
                {
                    Debug.LogWarning($"Tried to add more effects to magic item ({baseItem.m_shared.m_name}) but there were no more available effects. " +
                                     $"Current Effects: {(string.Join(", ", magicItem.Effects.Select(x => x.EffectType.ToString())))}");
                    break;
                }

                _weightedEffectTable.Setup(availableEffects, x => x.SelectionWeight);
                var effectDef = _weightedEffectTable.Roll();

                var effect = RollEffect(effectDef, magicItem.Rarity);
                magicItem.Effects.Add(effect);
            }

            return magicItem;
        }
        
        public static int RollEffectCountPerRarity(ItemRarity rarity)
        {
            Dictionary<int, float> countPercents = MagicEffectCountWeightsPerRarity[rarity];
            _weightedEffectCountTable.Setup(countPercents, x => x.Value);
            return _weightedEffectCountTable.Roll().Key;
        }

        public static MagicItemEffect RollEffect(MagicItemEffectDefinition effectDef, ItemRarity itemRarity)
        {
            var valuesDef = effectDef.ValuesPerRarity[itemRarity];
            float value = valuesDef.MinValue;
            if (valuesDef.Increment != 0)
            {
                int incrementCount = (int)((valuesDef.MaxValue - valuesDef.MinValue) / valuesDef.Increment);
                value = valuesDef.MinValue + (Random.Range(0, incrementCount + 1) * valuesDef.Increment);
            }

            return new MagicItemEffect()
            {
                EffectType = effectDef.Type,
                EffectValue = value
            };
        }

        public static ItemRarity RollItemRarity(LootDrop lootDrop)
        {
            if (lootDrop.Rarity == null || lootDrop.Rarity.Length == 0)
            {
                return ItemRarity.Magic;
            }

            Dictionary<ItemRarity, int> rarityWeights = new Dictionary<ItemRarity, int>()
            {
                { ItemRarity.Magic, lootDrop.Rarity.Length >= 1 ? lootDrop.Rarity[0] : 0 },
                { ItemRarity.Rare, lootDrop.Rarity.Length >= 2 ? lootDrop.Rarity[1] : 0 },
                { ItemRarity.Epic, lootDrop.Rarity.Length >= 3 ? lootDrop.Rarity[2] : 0 },
                { ItemRarity.Legendary, lootDrop.Rarity.Length >= 4 ? lootDrop.Rarity[3] : 0 }
            };

            _weightedRarityTable.Setup(rarityWeights, x => x.Value);
            return _weightedRarityTable.Roll().Key;
        }

        public static void DropItems(List<GameObject> loot, Vector3 centerPos, float dropHemisphereRadius = 0.5f)
        {
            foreach (var item in loot)
            {
                var vector3 = Random.insideUnitSphere * dropHemisphereRadius;
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

        public static List<LootTable> GetLootTable(string objectName, int level)
        {
            if (LootTables.TryGetValue(objectName, out List<LootTable> lootTable))
            {
                return lootTable.Where(x => x.Level == null || x.Level.Length == 0 || x.Level.Contains(level)).ToList();
            }
            return null;
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

            // Loot tables
            t.AppendLine("# Loot Tables");
            t.AppendLine();
            t.AppendLine("A list of every built-in loot table from the mod. The name of the loot table is the object name followed by a number signifying the level of the object.");

            foreach (var lootTableEntry in LootTables)
            {
                var list = lootTableEntry.Value;

                foreach (var lootTable in list)
                {
                    t.AppendLine($"## {lootTableEntry.Key}");
                    t.AppendLine();
                    var levelDisplay = (lootTable.Level == null || lootTable.Level.Length == 0) ? "All" : string.Join(", ", lootTable.Level);
                    t.AppendLine($"> **Levels:** {levelDisplay}");
                    t.AppendLine(">");
                    float total = lootTable.Drops.Sum(x => x.Length > 1 ? x[1] : 0);
                    if (total > 0)
                    {
                        t.AppendLine($"> | Number of Items | Weight (Chance) |");
                        t.AppendLine($"> | -- | -- |");
                        foreach (var drop in lootTable.Drops)
                        {
                            var count = drop.Length > 0 ? drop[0] : 0;
                            var value = drop.Length > 1 ? drop[1] : 0;
                            var percent = (value / total) * 100;
                            t.AppendLine($"> | {count} | {value} ({percent:0.#}%) |");
                        }
                    }

                    t.AppendLine(">");
                    t.AppendLine("> | Items | Weight (Chance) | Magic | Rare | Epic | Legendary |");
                    t.AppendLine("> | -- | -- | -- | -- | -- | -- |");

                    float totalLootWeight = lootTable.Loot.Sum(x => x.Weight);
                    foreach (var lootDrop in lootTable.Loot)
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
            }

            //var outputFilePath = Path.Combine(Path.GetDirectoryName(typeof(EpicLoot).Assembly.Location), "info.md");
            //File.WriteAllText(outputFilePath, t.ToString());

            const string devOutputPath = @"C:\Users\rknapp\Documents\GitHub\ValheimMods\EpicLoot";
            if (Directory.Exists(devOutputPath))
            {
                File.WriteAllText(Path.Combine(devOutputPath, "info.md"), t.ToString());
            }
        }

        private Dictionary<string, string> ParseSource(IEnumerable<string> lines)
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

        private string GetMagicEffectCountTableLine(ItemRarity rarity)
        {
            var effectCounts = MagicEffectCountWeightsPerRarity[rarity];
            var total = effectCounts.Sum(x => x.Value);
            var result = $"|{rarity}|";
            for (int i = 1; i <= 6; ++i)
            {
                var valueString = " ";
                if (effectCounts.TryGetValue(i, out float value))
                {
                    var percent = value / total * 100;
                    valueString = $"{value} ({percent:0.#}%)";
                }
                result += $"{valueString}|";
            }
            return result;
        }
    }
}
