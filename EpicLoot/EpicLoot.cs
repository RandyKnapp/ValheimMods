using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using LitJson;
using UnityEngine;

namespace EpicLoot
{
    [BepInPlugin("randyknapp.mods.epicloot", "Epic Loot", "1.0.0")]
    [BepInDependency("randyknapp.mods.uniqueitemidentifiers")]
    public class EpicLoot : BaseUnityPlugin
    {
        public static readonly List<ItemDrop.ItemData.ItemType> CanBeMagicItemTypes = new List<ItemDrop.ItemData.ItemType>
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
        };

        public static Dictionary<string, LootTable> LootTables = new Dictionary<string, LootTable>();
        public static bool HasConvertedItemPrefabs;

        private Harmony _harmony;
        private static ZDO _zdo;

        private void Awake()
        {
            LootTables.Clear();
            var lootConfigFile = File.ReadAllText(Path.Combine(Paths.PluginPath, "EpicLoot", "loottables.json"));
            var lootConfig = LitJson.JsonMapper.ToObject<LootConfig>(lootConfigFile);
            foreach (var lootTable in lootConfig.LootTables)
            {
                LootTables.Add(lootTable.Character, lootTable);
            }

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        private void OnDestroy()
        {
            HasConvertedItemPrefabs = false;
            _harmony?.UnpatchAll();
        }

        public static bool CanBeMagicItem(ItemDrop.ItemData item)
        {
            return item != null && CanBeMagicItemTypes.Contains(item.m_shared.m_itemType);
        }

        public static MagicItem GetMagicItem(string guid)
        {
            if (_zdo == null)
            {
                return null;
            }

            var magicItemJson = _zdo.GetString(guid);
            if (string.IsNullOrEmpty(magicItemJson))
            {
                return null;
            }

            var magicItem = JsonMapper.ToObject<MagicItem>(magicItemJson);
            return magicItem;
        }

        public static void SaveMagicItem(string guid, MagicItem magicItem)
        {
            if (_zdo == null)
            {
                Debug.LogError($"Tried to save magic item ({guid}) but _zdo was null!");
                return;
            }

            _zdo.Set(guid, JsonMapper.ToJson(magicItem));
        }

        public static void OnCharacterDeath(CharacterDrop characterDrop)
        {
            var characterName = characterDrop.name.Replace("(Clone)", "");
            if (LootTables.TryGetValue(characterName, out LootTable lootTable))
            {
                Debug.Log($"CharacterDrop OnDeath: {characterName}");
                List<GameObject> loot = GetLoot(lootTable, characterName);
                DropItems(loot, characterDrop.m_character.GetCenterPoint() + characterDrop.transform.TransformVector(characterDrop.m_spawnOffset), 0.5f);
            }
            else
            {
                Debug.Log($"CharacterDrop OnDeath (no loot table): {characterName}");
            }
        }

        private static List<GameObject> GetLoot(LootTable lootTable, string characterName)
        {
            var results = new List<GameObject>();
            foreach (var lootDrop in lootTable.Loot)
            {
                var shouldDrop = Random.Range(0.0f, 1.0f) <= lootDrop.PercentDrop;
                if (shouldDrop)
                {
                    var itemPrefab = ObjectDB.instance.GetItemPrefab(lootDrop.Item);
                    if (itemPrefab == null)
                    {
                        Debug.LogError($"Tried to spawn loot ({lootDrop.Item}) for character ({characterName}), but the item prefab was not found!");
                        continue;
                    }
                    var item = Instantiate(itemPrefab);
                    var itemDrop = item.GetComponent<ItemDrop>();
                    //itemDrop.m_itemData = UniqueItemIdentifiers.UniqueItemIdentifiers.ConvertToUniqueItemData(itemDrop.m_itemData, "EpicLoot.GetLoot");

                    var magicItem = new MagicItem();
                    // TODO: Generate magic item
                    magicItem.Rarity = GetItemRarity(lootDrop);
                    magicItem.Effects.Add(new MagicItemEffect() { EffectType = MagicEffectType.ModifyDamage, EffectValue = 0.10f });

                    //SaveMagicItem((itemDrop.m_itemData as UniqueItemData).m_guid, magicItem);
                }
            }
            return results;
        }

        private static ItemRarity GetItemRarity(LootDrop lootDrop)
        {
            var roll = Random.Range(0.0f, 1.0f);
            var lowerRange = 0.0f;
            if (roll >= lowerRange && roll <= lootDrop.PercentMagic)
            {
                return ItemRarity.Magic;
            }
            lowerRange += lootDrop.PercentMagic;

            if (roll > lowerRange && roll <= lowerRange + lootDrop.PercentRare)
            {
                return ItemRarity.Rare;
            }
            lowerRange += lootDrop.PercentRare;

            if (roll > lowerRange && roll <= lowerRange + lootDrop.PercentEpic)
            {
                return ItemRarity.Epic;
            }

            return ItemRarity.Legendary;
        }

        public static void DropItems(List<GameObject> loot, Vector3 centerPos, float dropArea)
        {
            foreach (var item in loot)
            {
                var vector3 = Random.insideUnitSphere * dropArea;
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

        public static void OnGameStart()
        {
            Debug.Log($"OnGameStart: ZDOMan.instance: {ZDOMan.instance}");
            var zdoidHashes = ZDO.GetHashZDOID("EpicLoot");
            var zdoid = new ZDOID(zdoidHashes.Key, (uint)zdoidHashes.Value);
            _zdo = ZDOMan.instance.GetZDO(zdoid);
            if (_zdo == null)
            {
                _zdo = ZDOMan.instance.CreateNewZDO(zdoid, Vector3.zero);
            }
        }

        public static void OnGameShutdown()
        {
            ZDOMan.instance.DestroyZDO(_zdo);
            _zdo = null;
        }
    }
}
