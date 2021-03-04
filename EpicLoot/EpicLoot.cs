using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
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
        public Sprite SetItemSprite;
        public Sprite[] MagicItemBgSprites = new Sprite[4];
        public GameObject[] MagicItemLootBeamPrefabs = new GameObject[4];
    }

    [BepInPlugin("randyknapp.mods.epicloot", "Epic Loot", Version)]
    [BepInDependency("randyknapp.mods.extendeditemdataframework")]
    public class EpicLoot : BaseUnityPlugin
    {
        private const string Version = "0.1.0";
        public const string SetItemColor = "#26FFFF";

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

        private void Awake()
        {
            MagicItemEffectDefinitions.SetupMagicItemEffectDefinitions();

            LootTables.Clear();
            var lootConfig = LoadJsonFile<LootConfig>("loottables.json");
            AddLootTableConfig(lootConfig);
            PrintInfo();

            var assetBundle = LoadAssetBundle("epicloot");
            Assets.MagicItemBgSprites[(int)ItemRarity.Magic] = assetBundle.LoadAsset<Sprite>("MagicItemBg");
            Assets.MagicItemBgSprites[(int)ItemRarity.Rare] = assetBundle.LoadAsset<Sprite>("RareItemBg");
            Assets.MagicItemBgSprites[(int)ItemRarity.Epic] = assetBundle.LoadAsset<Sprite>("EpicItemBg");
            Assets.MagicItemBgSprites[(int)ItemRarity.Legendary] = assetBundle.LoadAsset<Sprite>("LegendaryItemBg");
            Assets.EquippedSprite = assetBundle.LoadAsset<Sprite>("Equipped");
            Assets.SetItemSprite = assetBundle.LoadAsset<Sprite>("SetItemMarker");
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
            return item != null && AllowedMagicItemTypes.Contains(item.m_shared.m_itemType);
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
                DropItems(loot, characterDrop.m_character.GetCenterPoint() + characterDrop.transform.TransformVector(characterDrop.m_spawnOffset), 0.5f);
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

            var weightedDropCountTable = new WeightedRandomCollection<int[]>(lootTable.Drops, dropPair => dropPair.Length == 2 ? dropPair[1] : 1);
            var dropCountRollResult = weightedDropCountTable.Roll();
            var dropCount = dropCountRollResult.Length >= 1 ? dropCountRollResult[0] : 0;
            if (dropCount == 0)
            {
                return results;
            }

            var weightedLootTable = new WeightedRandomCollection<LootDrop>(lootTable.Loot, x => x.Weight);
            var selectedDrops = weightedLootTable.Roll(dropCount);

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
            var availableEffects = MagicItemEffectDefinitions.GetAvailableEffects(baseItem.m_shared.m_itemType, magicItem.Rarity);
            var weightedEffectTable = new WeightedRandomCollection<MagicItemEffectDefinition>(availableEffects, x => x.SelectionWeight, true);
            for (int i = 0; i < effectCount && i < availableEffects.Count; i++)
            {
                var effectDef = weightedEffectTable.Roll();
                magicItem.Effects.Add(RollEffect(effectDef, magicItem.Rarity));
            }

            return magicItem;
        }
        
        public static int RollEffectCountPerRarity(ItemRarity rarity)
        {
            Dictionary<int, float> countPercents = MagicEffectCountWeightsPerRarity[rarity];
            var weightedEffectCountTable = new WeightedRandomCollection<KeyValuePair<int, float>>(countPercents, x => x.Value);
            return weightedEffectCountTable.Roll().Key;
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

            var weightedRarityTable = new WeightedRandomCollection<KeyValuePair<ItemRarity, int>>(rarityWeights, x => x.Value);
            return weightedRarityTable.Roll().Key;
        }

        public static void DropItems(List<GameObject> loot, Vector3 centerPos, float dropHemisphereRadius)
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
            t.AppendLine();
            t.AppendLine("  * **Display Text:** This text appears in the tooltip for the magic item, with {0:?} replaced with the rolled value for the effect, formatted using the shown C# string format.");
            t.AppendLine("  * **Allowed Item Types:** This effect may only be rolled on items of a the types in this list. When this list is empty, this is usually done because this is a special effect type added programatically or currently not allowed to roll.");
            t.AppendLine("  * **Value Per Rarity:** This effect may only be rolled on items of a rarity included in this table. The value is rolled using a linear distribution between Min and Max and divisble by the Increment.");
            t.AppendLine();

            foreach (var definitionEntry in MagicItemEffectDefinitions.AllDefinitions)
            {
                var def = definitionEntry.Value;
                t.AppendLine($"## {def.Type} ({def.IntType})");
                t.AppendLine();
                t.AppendLine($"> **Display Text:** {def.DisplayText}");
                t.AppendLine("> ");
                t.AppendLine("> **Allowed Item Types:** " + (def.AllowedItemTypes.Count == 0 ? "*None*" : string.Join(", ", def.AllowedItemTypes)));
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

            const string devOutputPath = "C:\\Users\\rknapp\\Documents\\GitHub\\ValheimMods\\EpicLoot";
            if (Directory.Exists(devOutputPath))
            {
                File.WriteAllText(Path.Combine(devOutputPath, "info.md"), t.ToString());
            }
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
