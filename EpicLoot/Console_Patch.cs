using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common;
using EpicLoot.Abilities;
using EpicLoot.Adventure;
using EpicLoot.Adventure.Feature;
using EpicLoot.Crafting;
using EpicLoot.GatedItemType;
using EpicLoot.LegendarySystem;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;
using Random = System.Random;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Console), nameof(Console.InputText))]
    public static class Console_Patch
    {
        private static readonly Random _random = new Random();

        public static bool Prefix(Console __instance)
        {
            var input = __instance.m_input.text;
            var args = input.Split(' ');
            if (args.Length == 0)
            {
                return true;
            }

            var player = Player.m_localPlayer;

            var command = args[0];
            if (CheatCommand(command, "magicitem", "mi"))
            {
                MagicItem(__instance, args);
            }
            else if (CheatCommand(command, "magicitemwitheffect", "mieffect")) 
            {
                SpawnMagicItemWithEffect(__instance, args);
            }
            else if (CheatCommand(command, "magicitemlegendary", "milegend"))
            {
                SpawnLegendaryMagicItem(__instance, args);
            }
            else if (CheatCommand(command, "magicitemset", "miset"))
            {
                SpawnMagicItemSet(__instance, args);
            }
            else if (Command(command, "checkstackquality"))
            {
                CheckStackQuality(__instance);
            }
            else if (CheatCommand(command, "magicmats"))
            {
                SpawnMagicCraftingMaterials();
            }
            else if (CheatCommand(command, "alwaysdrop"))
            {
                ToggleAlwaysDrop(__instance);
            }
            else if (CheatCommand(command, "cheatgating"))
            {
                LootRoller.CheatDisableGating = !LootRoller.CheatDisableGating;
                __instance.AddString($"> Disable gating for magic item drops: {LootRoller.CheatDisableGating}");
            }
            else if (CheatCommand(command, "testtreasuremap", "testtm"))
            {
                TestTreasureMap(args);
            }
            else if (Command(command, "resettreasuremap", "resettm"))
            {
                var saveData = player.GetAdventureSaveData();
                saveData.TreasureMaps.Clear();
                saveData.NumberOfTreasureMapsOrBountiesStarted = 0;
                player.SaveAdventureSaveData();
            }
            else if (Command(command, "debugtreasuremap", "debugtm"))
            {
                Minimap_Patch.DebugMode = !Minimap_Patch.DebugMode;
                __instance.AddString($"> Treasure Map Debug Mode: {Minimap_Patch.DebugMode}");
            }
            else if (Command(command, "resetbounties"))
            {
                var saveData = player.GetAdventureSaveData();
                saveData.Bounties.Clear();
                player.SaveAdventureSaveData();
            }
            else if (Command(command, "testbountynames"))
            {
                var random = new Random();
                var count = (args.Length >= 2) ? int.Parse(args[1]) : 10;
                for (var i = 0; i < count; ++i)
                {
                    var name = BountiesAdventureFeature.GenerateTargetName(random);
                    __instance.AddString(name);
                }
            }
            else if (Command(command, "resetadventure"))
            {
                var adventureComponent = player.GetComponent<AdventureComponent>();
                adventureComponent.SaveData = new AdventureSaveDataList();
                player.SaveAdventureSaveData();
            }
            else if (Command(command, "bounties"))
            {
                var interval = (args.Length >= 2) ? int.Parse(args[1]) : AdventureDataManager.Bounties.GetCurrentInterval();
                var availableBounties = AdventureDataManager.Bounties.GetAvailableBounties(interval, false);
                BountiesAdventureFeature.PrintBounties($"Bounties for Interval {interval}:", availableBounties);
            }
            else if (Command(command, "playerbounties"))
            {
                var availableBounties = player.GetAdventureSaveData().Bounties;
                BountiesAdventureFeature.PrintBounties($"Player Bounties:", availableBounties);
            }
            else if (CheatCommand(command, "timescale", "ts"))
            {
                var timeScale = (args.Length >= 2) ? float.Parse(args[1]) : 1;
                Time.timeScale = timeScale;
            }
            else if (CheatCommand(command, "gotomerchant", "gotom"))
            {
                if (ZoneSystem.instance.FindClosestLocation("Vendor_BlackForest", player.transform.position, out var location))
                {
                    player.TeleportTo(location.m_position + Vector3.right * 5, player.transform.rotation, true);
                }
            }
            else if (Command(command, "globalkeys"))
            {
                if (ZoneSystem.instance != null)
                {
                    __instance.AddString("> Print Global Keys:");
                    foreach (var globalKey in ZoneSystem.instance.GetGlobalKeys())
                    {
                        __instance.AddString("> " + globalKey);
                    }
                }
            }
            else if (Command(command, "fixresistances"))
            {
                FixResistances(player);
            }
            else if (Command(command, "lucktest"))
            {
                var lootTable = args.Length > 1 ? args[1] : "Greydwarf";
                var luckFactor = args.Length > 2 ? float.Parse(args[2]) : 0;
                LootRoller.PrintLuckTest(lootTable, luckFactor);
            }
            else if (Command(command, "lootres"))
            {
                var lootTable = args.Length > 1 ? args[1] : "Greydwarf";
                var level = args.Length > 2 ? int.Parse(args[2]) : 1;
                var itemIndex = args.Length > 3 ? int.Parse(args[3]) : 0;
                LootRoller.PrintLootResolutionTest(lootTable, level, itemIndex);
            }
            else if (CheatCommand(command, "resetcooldowns"))
            {
                if (player != null)
                {
                    var abilityController = player.GetComponent<AbilityController>();
                    if (abilityController != null)
                    {
                        foreach (var ability in abilityController.CurrentAbilities)
                        {
                            ability.ResetCooldown();
                        }
                    }
                }
            }

            return true;
        }

        private static bool Command(string command, params string[] args)
        {
            return args.Contains(command);
        }

        private static bool CheatCommand(string command, params string[] args)
        {
            return Console.instance.IsCheatsEnabled() && args.Contains(command);
        }

        private static void TestTreasureMap(string[] args)
        {
            var player = Player.m_localPlayer;

            var count = 1;
            if (args.Length >= 2)
            {
                int.TryParse(args[1], out count);
            }

            var biome = Heightmap.Biome.None;
            if (args.Length >= 3)
            {
                Enum.TryParse(args[2], out biome);
            }

            var overrideTreasureMapCount = -1;
            if (args.Length >= 4)
            {
                int.TryParse(args[3], out overrideTreasureMapCount);
            }

            AdventureDataManager.CheatNumberOfBounties = overrideTreasureMapCount;
            var saveData = player.GetAdventureSaveData();
            player.StartCoroutine(TestTreasureMapCoroutine(saveData, biome, player, count));
        }

        private static IEnumerator TestTreasureMapCoroutine(AdventureSaveData saveData, Heightmap.Biome biome, Player player, int count)
        {
            var biomes = new[] { Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp, Heightmap.Biome.Mountain, Heightmap.Biome.Plains };

            saveData.DebugMode = true;
            var startInterval = saveData.TreasureMaps.Count == 0 ? -1 : saveData.TreasureMaps.Min(x => x.Interval) - 1;
            for (var i = 0; i < count; ++i)
            {
                saveData.IntervalOverride = startInterval - (i + 1);
                var selectedBiome = biome == Heightmap.Biome.None ? biomes[UnityEngine.Random.Range(0, biomes.Length)] : biome;
                yield return AdventureDataManager.TreasureMaps.SpawnTreasureChest(selectedBiome, player, OnTreasureChestSpawnComplete);
            }
            saveData.DebugMode = false;
            AdventureDataManager.CheatNumberOfBounties = -1;
        }

        private static void OnTreasureChestSpawnComplete(bool success, Vector3 spawnPoint)
        {
            var output = "> Failed to spawn treasure map chest";
            if (success)
            {
                output = $"> Spawning Treasure Map Chest at <{spawnPoint.x:0.#}, {spawnPoint.z:0.#}> (height:{spawnPoint.y:0.#})";
            }

            Console.instance.AddString(output);
            EpicLoot.LogWarning(output);
        }

        private static void ToggleAlwaysDrop(Console __instance)
        {
            EpicLoot.AlwaysDropCheat = !EpicLoot.AlwaysDropCheat;
            __instance.AddString($"> Always Drop: {EpicLoot.AlwaysDropCheat}");
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
                var rarityTable = GetRarityTable(rarityArg);

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

                __instance.AddString($">  {i + 1} - rarity: [{string.Join(", ", rarityTable)}], item: {item}");

                var loot = new LootTable()
                {
                    Object = "Console",
                    Drops = new[] { new float[] { 1, 1 } },
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

        public static void SpawnMagicItemWithEffect(Console __instance, string[] args)
        {
            if (args.Length < 3)
            {
                EpicLoot.LogError("Specify effect and item name");
                return;
            }

            if (Player.m_localPlayer == null) return;

            var effectArg = args[1];
            var itemPrefabNameArg = args[2];
            __instance.AddString($"magicitem - {itemPrefabNameArg} with effect: {effectArg}");

            var magicItemEffectDef = MagicItemEffectDefinitions.Get(effectArg);
            if (magicItemEffectDef == null)
            {
                __instance.AddString($"> Could not find effect: {effectArg}");
                return;
            }

            var itemPrefab = ObjectDB.instance.GetItemPrefab(itemPrefabNameArg);
            if (itemPrefab == null)
            {
                __instance.AddString($"> Could not find item: {itemPrefabNameArg}");
                return;
            }

            var fromItemData = itemPrefab.GetComponent<ItemDrop>().m_itemData;
            if (!EpicLoot.CanBeMagicItem(fromItemData))
            {
                __instance.AddString($"> Can't be magic item: {itemPrefabNameArg}");
                return;
            }

            var effectRequirements = magicItemEffectDef.Requirements;
            var itemRarity = effectRequirements.AllowedRarities.Count == 0 ? ItemRarity.Magic : effectRequirements.AllowedRarities.First();
            var rarityTable = GetRarityTable(itemRarity.ToString());
            var loot = new LootTable
            {
                Object = "Console",
                Drops = new[] { new float[] {1, 1} },
                Loot = new[]
                {
                    new LootDrop
                    {
                        Item = itemPrefab.name,
                        Rarity = rarityTable
                    }
                }
            };

            var randomOffset = UnityEngine.Random.insideUnitSphere;
            var dropPoint = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
            LootRoller.CheatForceMagicEffect = true;
            LootRoller.ForcedMagicEffect = effectArg;
            LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);
            LootRoller.CheatForceMagicEffect = false;
            LootRoller.ForcedMagicEffect = string.Empty;
        }

        private static float[] GetRarityTable(string rarityName)
        {
            var rarityTable = new float[] {1, 1, 1, 1};
            switch (rarityName.ToLowerInvariant())
            {
                case "magic":
                    rarityTable = new float[] {1, 0, 0, 0};
                    break;
                case "rare":
                    rarityTable = new float[] {0, 1, 0, 0};
                    break;
                case "epic":
                    rarityTable = new float[] {0, 0, 1, 0};
                    break;
                case "legendary":
                    rarityTable = new float[] {0, 0, 0, 1};
                    break;
            }

            return rarityTable;
        }

        private static void SpawnLegendaryMagicItem(Console __instance, string[] args)
        {
            if (args.Length < 2)
            {
                __instance.AddString("> Specify legendaryID, itemID (optional)");
                return;
            }

            var legendaryID = args[1];
            var itemType = args.Length >= 3 ? args[2] : null;

            __instance.AddString($"magicitemlegendary - legendaryID:{legendaryID}");
            SpawnLegendaryItemHelper(legendaryID, itemType, __instance);
        }

        private static void SpawnLegendaryItemHelper(string legendaryID, string itemType, Console __instance)
        {
            if (!UniqueLegendaryHelper.TryGetLegendaryInfo(legendaryID, out var legendaryInfo))
            {
                if (__instance != null)
                {
                    __instance.AddString($"> Could not find info for legendaryID: ({legendaryID})");
                }
                return;
            }

            if (string.IsNullOrEmpty(itemType))
            {
                Debug.LogWarning($"Finding items for legendary ({legendaryInfo.ID})");
                var dummyMagicItem = new MagicItem { Rarity = ItemRarity.Legendary };
                var allowedItems = new List<ItemDrop>();
                foreach (var itemName in GatedItemTypeHelper.ItemInfoByID.Keys)
                {
                    var itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
                    if (itemPrefab == null)
                    {
                        continue;
                    }

                    var itemDrop = itemPrefab.GetComponent<ItemDrop>();
                    if (itemDrop == null)
                    {
                        continue;
                    }

                    var itemData = itemDrop.m_itemData;
                    if (legendaryInfo.Requirements.CheckRequirements(itemData, dummyMagicItem))
                    {
                        Debug.Log($"> {itemDrop.name} ({itemDrop.m_itemData.m_shared.m_itemType.ToString()})");
                        allowedItems.Add(itemDrop);
                    }
                }

                itemType = allowedItems.LastOrDefault()?.name;
            }
            
            if (string.IsNullOrEmpty(itemType))
            {
                itemType = "Club";
            }

            var loot = new LootTable
            {
                Object = "Console",
                Drops = new[] { new float[] { 1, 1 } },
                Loot = new[]
                {
                    new LootDrop
                    {
                        Item = itemType,
                        Rarity = new float[]{ 0, 0, 0, 1 }
                    }
                }
            };

            LootRoller.CheatForceLegendary = legendaryID;
            var previousDisableGatingState = LootRoller.CheatDisableGating;
            LootRoller.CheatDisableGating = true;

            var randomOffset = UnityEngine.Random.insideUnitSphere;
            var dropPoint = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
            LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);

            LootRoller.CheatForceLegendary = null;
            LootRoller.CheatDisableGating = previousDisableGatingState;
        }

        private static void SpawnMagicItemSet(Console console, string[] args)
        {
            if (args.Length < 2)
            {
                console.AddString("> Specify Set ID");
                return;
            }

            var setID = args[1];
            console.AddString($"magicitemset - setID:{setID}");

            if (!UniqueLegendaryHelper.TryGetLegendarySetInfo(setID, out var setInfo))
            {
                console.AddString($"> Could not find set info for setID: ({setID})");
                return;
            }

            foreach (var legendaryID in setInfo.LegendaryIDs)
            {
                SpawnLegendaryItemHelper(legendaryID, null, console);
            }
        }

        public static void CheckStackQuality(Console __instance)
        {
            __instance.AddString("CheckStackQuality");
            if (ObjectDB.instance == null)
            {
                __instance.AddString("> ObjectDB is null");
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
                    __instance.AddString($"> {itemDrop.name}");
                }
            }

            if (count == 0)
            {
                __instance.AddString("> (none)");
            }
        }

        private static void FixResistances(Player player)
        {
            var oldResistanceTypes = new[]
            {
                MagicEffectType.AddFireResistance,
                MagicEffectType.AddFrostResistance,
                MagicEffectType.AddLightningResistance,
                MagicEffectType.AddPoisonResistance,
                MagicEffectType.AddSpiritResistance
            };
            foreach (var itemData in player.GetInventory().GetAllItems())
            {
                if (itemData.IsMagic() && itemData.GetMagicItem().HasAnyEffect(oldResistanceTypes))
                {
                    var magicItem = itemData.GetMagicItem();
                    var currentEffects = magicItem.Effects;
                    for (var index = 0; index < currentEffects.Count; index++)
                    {
                        var effect = currentEffects[index];
                        if (oldResistanceTypes.Contains(effect.EffectType))
                        {
                            ReplaceMagicEffect(itemData, magicItem, effect, index);
                        }
                    }
                }
            }
        }

        private static void ReplaceMagicEffect(ItemDrop.ItemData itemData, MagicItem magicItem, MagicItemEffect effect, int index)
        {
            var replacementEffectDef = GetReplacementEffectDef(effect);
            if (replacementEffectDef == null)
            {
                return;
            }

            var replacementEffect = LootRoller.RollEffect(replacementEffectDef, magicItem.Rarity);
            magicItem.Effects[index] = replacementEffect;
            itemData.Extended().Save();
        }

        private static MagicItemEffectDefinition GetReplacementEffectDef(MagicItemEffect effect)
        {
            switch (effect.EffectType)
            {
                case "AddFireResistance":
                    return MagicItemEffectDefinitions.Get(MagicEffectType.AddFireResistancePercentage);
                case "AddFrostResistance":
                    return MagicItemEffectDefinitions.Get(MagicEffectType.AddFrostResistancePercentage);
                case "AddLightningResistance":
                    return MagicItemEffectDefinitions.Get(MagicEffectType.AddLightningResistancePercentage);
                case "AddPoisonResistance":
                    return MagicItemEffectDefinitions.Get(MagicEffectType.AddPoisonResistancePercentage);
                case "AddSpiritResistance":
                    return MagicItemEffectDefinitions.Get(MagicEffectType.AddElementalResistancePercentage);
            }
            return null;
        }
    }
}
