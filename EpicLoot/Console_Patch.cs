using System;
using System.Linq;
using Common;
using EpicLoot.Adventure;
using EpicLoot.Adventure.Feature;
using EpicLoot.Crafting;
using HarmonyLib;
using UnityEngine;
using Random = System.Random;

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

            var command = args[0];
            if (command.Equals("magicitem", StringComparison.InvariantCultureIgnoreCase) ||
                command.Equals("mi", StringComparison.InvariantCultureIgnoreCase))
            {
                MagicItem(__instance, args);
                return false;
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
            else if (command.Equals("cheatgating", StringComparison.InvariantCultureIgnoreCase))
            {
                LootRoller.CheatDisableGating = !LootRoller.CheatDisableGating;
                __instance.AddString($"Disable gating for magic item drops: {LootRoller.CheatDisableGating}");
                return false;
            }
            else if (command.Equals("testtreasuremap", StringComparison.InvariantCultureIgnoreCase) ||
                     command.Equals("testtm", StringComparison.InvariantCultureIgnoreCase))
            {
                var player = Player.m_localPlayer;

                var biome = (args.Length >= 2) ? (Heightmap.Biome)Enum.Parse(typeof(Heightmap.Biome), args[1]) : Heightmap.Biome.Meadows;
                var overrideTreasureMapCount = (args.Length >= 3) ? int.Parse(args[2]) : -1;
                if (overrideTreasureMapCount >= 0)
                {
                    player.GetAdventureSaveData().NumberOfTreasureMapsOrBountiesStarted = overrideTreasureMapCount;
                }
                player.StartCoroutine(AdventureDataManager.TreasureMaps.SpawnTreasureChest(biome, player, OnSpawnTreasureChestResult));
            }
            else if (command.Equals("resettreasuremap", StringComparison.InvariantCultureIgnoreCase))
            {
                var player = Player.m_localPlayer;
                var saveData = player.GetAdventureSaveData();
                saveData.TreasureMaps.Clear();
                saveData.NumberOfTreasureMapsOrBountiesStarted = 0;
                player.SaveAdventureSaveData();
            }
            else if (command.Equals("resetbounties", StringComparison.InvariantCultureIgnoreCase))
            {
                var player = Player.m_localPlayer;
                var saveData = player.GetAdventureSaveData();
                saveData.Bounties.Clear();
                player.SaveAdventureSaveData();
            }
            else if (command.Equals("testbountynames", StringComparison.InvariantCultureIgnoreCase))
            {
                var random = new Random();
                var count = (args.Length >= 2) ? int.Parse(args[1]) : 10;
                for (var i = 0; i < count; ++i)
                {
                    var name = BountiesAdventureFeature.GenerateTargetName(random);
                    __instance.AddString(name);
                }
            }

            return true;
        }

        private static void OnSpawnTreasureChestResult(bool success, Vector3 spawnPoint)
        {
            var output = "Failed to spawn treasure map chest";
            if (success)
            {
                output = $"Spawning Treasure Map Chest at <{spawnPoint.x:0.#}, {spawnPoint.y:0.#}, {spawnPoint.z:0.#}>";
            }

            Console.instance.AddString(output);
            Debug.LogWarning(output);
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
                var itemDrop = UnityEngine.Object.Instantiate(itemPrefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>();
                if (itemDrop.m_itemData.IsMagicCraftingMaterial() || itemDrop.m_itemData.IsRunestone())
                {
                    itemDrop.m_itemData.m_stack = itemDrop.m_itemData.m_shared.m_maxStackSize / 2;
                }
                else
                {
                    UnityEngine.Object.Destroy(itemDrop.gameObject);
                }
            }
        }

        public static void MagicItem(Console __instance, string[] args)
        {
            var rarityArg = args.Length >= 2 ? args[1] : "random";
            var itemArg = args.Length >= 3 ? args[2] : "random";
            var count = args.Length >= 4 ? int.Parse(args[3]) : 1;
            var effectCount = args.Length >= 5 ? int.Parse(args[4]) : -1;

            __instance.AddString($"magicitem - rarity:{rarityArg}, item:{itemArg}, count:{count}");

            var allItemNames = ObjectDB.instance.m_items
                .Where(x => EpicLoot.CanBeMagicItem(x.GetComponent<ItemDrop>().m_itemData))
                .Where(x => x.name != "HelmetDverger" && x.name != "BeltStrength" && x.name != "Wishbone")
                .Select(x => x.name)
                .ToList();

            if (Player.m_localPlayer == null)
            {
                return;
            }

            LootRoller.CheatEffectCount = effectCount;
            for (var i = 0; i < count; i++)
            {
                var rarityTable = new[] { 1, 1, 1, 1 };
                switch (rarityArg.ToLowerInvariant())
                {
                    case "magic":
                        rarityTable = new[] { 1, 0, 0, 0, };
                        break;
                    case "rare":
                        rarityTable = new[] { 0, 1, 0, 0, };
                        break;
                    case "epic":
                        rarityTable = new[] { 0, 0, 1, 0, };
                        break;
                    case "legendary":
                        rarityTable = new[] { 0, 0, 0, 1, };
                        break;
                }

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
                LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);
            }
            LootRoller.CheatEffectCount = -1;
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
