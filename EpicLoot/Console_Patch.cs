using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using EpicLoot.Crafting;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Console), "InputText")]
    public static class Console_Patch
    {
        private static readonly System.Random _random = new System.Random();

        public static bool Prefix(Console __instance)
        {
            var input = __instance.m_input.text;
            var args = input.Split(' ');
            if (args.Length == 0 || !__instance.IsCheatsEnabled())
            {
                return true;
            }
            
            // init debug hooks at next console command after enabling cheat mode
            if (!LootRoller_Debug._debugModeEnabled) {
                Debug.Log(nameof(LootRoller_Debug._debugModeEnabled));
                LootRoller_Debug._debugModeEnabled = true;
                LootRoller.MagicItemGenerated += LootRoller_Debug.AddDebugMagicEffects;
            }

            var command = args[0];
            if (command.Equals("magicitem", StringComparison.InvariantCultureIgnoreCase) ||
                command.Equals("mi", StringComparison.InvariantCultureIgnoreCase))
            {
                MagicItem(__instance, args);
                return false;
            } 
            else if (command.Equals("magicitemwitheffect", StringComparison.InvariantCultureIgnoreCase) 
                     || command.Equals("mieffect", StringComparison.InvariantCultureIgnoreCase)) 
            {
              SpawnMagicItemWithEffect(__instance, args);
            }
            else if (command.Equals("checkstackquality", StringComparison.InvariantCultureIgnoreCase))
            {
                CheckStackQuality(__instance);
                return false;
            }
            else if (command.Equals("magicmats", StringComparison.InvariantCultureIgnoreCase))
            {
                SpawnMagicCraftingMaterials();
                return false;
            }
            else if (command.Equals("alwaysdrop", StringComparison.InvariantCultureIgnoreCase))
            {
                ToggleAlwaysDrop(__instance);
                return false;
            }

            return true;
        }

        private static void ToggleAlwaysDrop(Console __instance)
        {
            EpicLoot.AlwaysDropCheat = !EpicLoot.AlwaysDropCheat;
            __instance.AddString($"Always Drop: {EpicLoot.AlwaysDropCheat}");
        }

        private static void SpawnMagicCraftingMaterials()
        {
            foreach (var itemPrefab in EpicLoot.RegisteredItemPrefabs)
            {
                var itemDrop = UnityEngine.Object.Instantiate<GameObject>(itemPrefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>();
                if (itemDrop.m_itemData.IsMagicCraftingMaterial() || itemDrop.m_itemData.IsRunestone())
                {
                    itemDrop.m_itemData.m_stack = itemDrop.m_itemData.m_shared.m_maxStackSize / 2;
                }
            }
        }

        public static void MagicItem(Console __instance, string[] args)
        {
            var rarityArg = args.Length >= 2 ? args[1] : "random";
            var itemArg = args.Length >= 3 ? args[2] : "random";
            var count = args.Length >= 4 ? int.Parse(args[3]) : 1;

            __instance.AddString($"magicitem - rarity:{rarityArg}, item:{itemArg}, count:{count}");

            var items = new List<GameObject>();
            var allItemNames = ObjectDB.instance.m_items
                .Where(x => EpicLoot.CanBeMagicItem(x.GetComponent<ItemDrop>().m_itemData))
                .Where(x => x.name != "HelmetDverger" && x.name != "BeltStrength" && x.name != "Wishbone")
                .Select(x => x.name)
                .ToList();

            if (Player.m_localPlayer == null)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                int[] rarityTable = GetRarityTable(rarityArg);

                var item = itemArg;
                if (item == "random")
                {
                    var weightedRandomTable = new WeightedRandomCollection<string>(_random, allItemNames, x => 1);
                    item = weightedRandomTable.Roll();
                }

                if (ObjectDB.instance.GetItemPrefab(item) == null)
                {
                    __instance.AddString($"> Could not find item: {item}");
                    break;
                }

                __instance.AddString($"  {i + 1} - rarity: [{string.Join(", ", rarityTable)}], item: {item}");

                var loot = new LootTable()
                {
                    Object = "Console",
                    Drops = new[] { new[] { 1, 1 } },
                    Loot = new[]
                    {
                        new LootDrop()
                        {
                            Item = item,
                            Rarity = rarityTable,
                            Weight = 1
                        }
                    }
                };

                var randomOffset = UnityEngine.Random.insideUnitSphere;
                var dropPoint = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
                items.AddRange(LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint));
            }
        }

        public static void SpawnMagicItemWithEffect(Console __instance, string[] args) {
            if (args.Length < 3) {
                Debug.LogError("specify effect and item name");
                return;
            }
            
            if (Player.m_localPlayer == null) {
                return;
            }
            
            var effectArg = args[1];
            var itemPrefabNameArg = args[2];
            __instance.AddString($"magicitem {itemPrefabNameArg} with effect: {effectArg}");

            var magicItemEffectDef = MagicItemEffectDefinitions.AllDefinitions[effectArg];
            var effectRequirements = magicItemEffectDef.Requirements;
            // TODO use magicItemEffectDef.GetAllowedItemTypes();

            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemPrefabNameArg);
            if (itemPrefab == null)
            {
                __instance.AddString($"> Could not find item: {itemPrefabNameArg}");
                return;
            }

            var fromItemData = itemPrefab.GetComponent<ItemDrop>().m_itemData;
            if (!EpicLoot.CanBeMagicItem(fromItemData)) {
                Debug.LogError("Can't be magic item");
                return;
            }

            ItemRarity itemRarity;
            if (effectRequirements.AllowedRarities.Count == 0) {
                Debug.Log("no req. rarity");
                itemRarity = ItemRarity.Legendary;
            } else {
                itemRarity = effectRequirements.AllowedRarities.First();
            }
            Debug.Log("rarity: " + itemRarity);
            
            int[] rarityTable = GetRarityTable(itemRarity.ToString());

            var loot = new LootTable() {
                    Object = "Console",
                    Drops = new[] {new[] {1, 1}},
                    Loot = new[] {
                            new LootDrop() {
                                    Item = itemPrefab.name,
                                    Rarity = rarityTable
                            }
                    }
            };

            var randomOffset = UnityEngine.Random.insideUnitSphere;
            var dropPoint = Player.m_localPlayer.transform.position +
                            Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
            // TODO add better hook for desired effect - currently effect will be discarded on next game load
            // if effect was added when magicItem had maximum of available effect
            // however still good for debug
            LootRoller_Debug.SelectedEffectForNextRolledItem = effectArg;
            LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);
        }
                
        private static int[] GetRarityTable(string rarityName) {
            var rarityTable = new[] {1, 1, 1, 1};
            switch (rarityName.ToLowerInvariant()) {
                case "magic":
                    rarityTable = new[] {1, 0, 0, 0,};
                    break;
                case "rare":
                    rarityTable = new[] {0, 1, 0, 0,};
                    break;
                case "epic":
                    rarityTable = new[] {0, 0, 1, 0,};
                    break;
                case "legendary":
                    rarityTable = new[] {0, 0, 0, 1,};
                    break;
            }
            return rarityTable;
        }

        public static void CheckStackQuality(Console __instance)
        {
            __instance.AddString("CheckStackQuality");
            if (ObjectDB.instance == null)
            {
                __instance.AddString(" - ObjectDB is null");
                return;
            }

            var count = 0;
            foreach (var itemObject in ObjectDB.instance.m_items)
            {
                var itemDrop = itemObject.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    continue;
                }

                var itemData = itemDrop.m_itemData;

                if (itemData.m_shared.m_maxStackSize > 1 && itemData.m_shared.m_maxQuality > 1)
                {
                    count++;
                    __instance.AddString($" - {itemDrop.name}");
                }
            }

            if (count == 0)
            {
                __instance.AddString(" (none)");
            }
        }
    }
}
