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
using HarmonyLib;
using UnityEngine;
using Random = System.Random;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class Terminal_Patch
    {
        private static readonly Random _random = new Random();

        public static void Postfix()
        {
            new Terminal.ConsoleCommand("magicitem", "", (args =>
            {
                MagicItem(args.Context, args.Args);
            }), true);
            new Terminal.ConsoleCommand("mi", "", (args =>
            {
                MagicItem(args.Context, args.Args);
            }), true);
            new Terminal.ConsoleCommand("magicitemwitheffect", "", (args =>
            {
                SpawnMagicItemWithEffect(args.Context, args.Args);
            }), true);
            new Terminal.ConsoleCommand("mieffect", "", (args =>
            {
                SpawnMagicItemWithEffect(args.Context, args.Args);
            }), true);
            new Terminal.ConsoleCommand("magicitemlegendary", "", (args =>
            {
                SpawnLegendaryMagicItem(args.Context, args.Args);
            }), true);
            new Terminal.ConsoleCommand("milegend", "", (args =>
            {
                SpawnLegendaryMagicItem(args.Context, args.Args);
            }), true);
            new Terminal.ConsoleCommand("magicitemset", "", (args =>
            {
                SpawnMagicItemSet(args.Context, args.Args);
            }), true);
            new Terminal.ConsoleCommand("miset", "", (args =>
            {
                SpawnMagicItemSet(args.Context, args.Args);
            }), true);
            new Terminal.ConsoleCommand("checkstackquality", "", (args =>
            {
                CheckStackQuality(args.Context);
            }));
            new Terminal.ConsoleCommand("magicmats", "", (args =>
            {
                SpawnMagicCraftingMaterials();
            }), true);
            new Terminal.ConsoleCommand("alwaysdrop", "", (args =>
            {
                ToggleAlwaysDrop(args.Context);
            }), true);
            new Terminal.ConsoleCommand("cheatgating", "", (args =>
            {
                LootRoller.CheatDisableGating = !LootRoller.CheatDisableGating;
                args.Context.AddString($"> Disable gating for magic item drops: {LootRoller.CheatDisableGating}");
            }), true);
            new Terminal.ConsoleCommand("testtreasuremap", "", (args =>
            {
                TestTreasureMap(args.Args);
            }), true);
            new Terminal.ConsoleCommand("testtm", "", (args =>
            {
                TestTreasureMap(args.Args);
            }), true);
            new Terminal.ConsoleCommand("resettreasuremap", "", (args =>
            {
                var player = Player.m_localPlayer;
                var saveData = player.GetAdventureSaveData();
                saveData.TreasureMaps.Clear();
                saveData.NumberOfTreasureMapsOrBountiesStarted = 0;
                player.SaveAdventureSaveData();
            }));
            new Terminal.ConsoleCommand("resettm", "", (args =>
            {
                var player = Player.m_localPlayer;
                var saveData = player.GetAdventureSaveData();
                saveData.TreasureMaps.Clear();
                saveData.NumberOfTreasureMapsOrBountiesStarted = 0;
                player.SaveAdventureSaveData();
            }));
            new Terminal.ConsoleCommand("debugtreasuremap", "", (args =>
            {
                Minimap_Patch.DebugMode = !Minimap_Patch.DebugMode;
                args.Context.AddString($"> Treasure Map Debug Mode: {Minimap_Patch.DebugMode}");
            }));
            new Terminal.ConsoleCommand("debugtm", "", (args =>
            {
                Minimap_Patch.DebugMode = !Minimap_Patch.DebugMode;
                args.Context.AddString($"> Treasure Map Debug Mode: {Minimap_Patch.DebugMode}");
            }));
            new Terminal.ConsoleCommand("resetbounties", "", (args =>
            {
                var player = Player.m_localPlayer;
                var saveData = player.GetAdventureSaveData();
                saveData.Bounties.Clear();
                player.SaveAdventureSaveData();
            }));
            new Terminal.ConsoleCommand("testbountynames", "", (args =>
            {
                var random = new Random();
                var count = (args.Length >= 2) ? int.Parse(args[1]) : 10;
                for (var i = 0; i < count; ++i)
                {
                    var name = BountiesAdventureFeature.GenerateTargetName(random);
                    args.Context.AddString(name);
                }
            }));
            new Terminal.ConsoleCommand("resetadventure", "", (args =>
            {
                var player = Player.m_localPlayer;
                var adventureComponent = player.GetComponent<AdventureComponent>();
                adventureComponent.SaveData = new AdventureSaveDataList();
                player.SaveAdventureSaveData();
            }));
            new Terminal.ConsoleCommand("bounties", "", (args =>
            {
                var interval = (args.Length >= 2) ? int.Parse(args[1]) : AdventureDataManager.Bounties.GetCurrentInterval();
                var availableBounties = AdventureDataManager.Bounties.GetAvailableBounties(interval, false);
                BountiesAdventureFeature.PrintBounties($"Bounties for Interval {interval}:", availableBounties);
            }));
            new Terminal.ConsoleCommand("playerbounties", "", (args =>
            {
                var player = Player.m_localPlayer;
                var availableBounties = player.GetAdventureSaveData().Bounties;
                BountiesAdventureFeature.PrintBounties($"Player Bounties:", availableBounties);
            }));
            new Terminal.ConsoleCommand("gotomerchant", "", (args =>
            {
                var player = Player.m_localPlayer;
                if (ZoneSystem.instance.FindClosestLocation("Vendor_BlackForest", player.transform.position, out var location))
                {
                    Console.instance.AddString(location.m_position.ToString());
                    //player.TeleportTo(location.m_position + Vector3.right * 5, player.transform.rotation, true);
                }
            }), true);
            new Terminal.ConsoleCommand("gotom", "", (args =>
            {
                var player = Player.m_localPlayer;
                if (ZoneSystem.instance.FindClosestLocation("Vendor_BlackForest", player.transform.position, out var location))
                {
                    Console.instance.AddString(location.m_position.ToString());
                    //player.TeleportTo(location.m_position + Vector3.right * 5, player.transform.rotation, true);
                }
            }), true);
            new Terminal.ConsoleCommand("globalkeys", "", (args =>
            {
                if (ZoneSystem.instance != null)
                {
                    args.Context.AddString("> Print Global Keys:");
                    foreach (var globalKey in ZoneSystem.instance.GetGlobalKeys())
                    {
                        args.Context.AddString("> " + globalKey);
                    }
                }
            }));
            new Terminal.ConsoleCommand("fixresistances", "", (args =>
            {
                var player = Player.m_localPlayer;
                FixResistances(player);
            }));
            new Terminal.ConsoleCommand("lucktest", "", (args =>
            {
                var lootTable = args.Length > 1 ? args[1] : "Greydwarf";
                var luckFactor = args.Length > 2 ? float.Parse(args[2]) : 0;
                LootRoller.PrintLuckTest(lootTable, luckFactor);
            }));
            new Terminal.ConsoleCommand("lootres", "", (args =>
            {
                var lootTable = args.Length > 1 ? args[1] : "Greydwarf";
                var level = args.Length > 2 ? int.Parse(args[2]) : 1;
                var itemIndex = args.Length > 3 ? int.Parse(args[3]) : 0;
                LootRoller.PrintLootResolutionTest(lootTable, level, itemIndex);
            }));
            new Terminal.ConsoleCommand("resetcooldowns", "", (args =>
            {
                var player = Player.m_localPlayer;
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
            }), true);
            new Terminal.ConsoleCommand("debugluck", "", (args => {
                LootRoller.DebugLuckFactor();
            }));
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

        private static void ToggleAlwaysDrop(Terminal context)
        {
            EpicLoot.AlwaysDropCheat = !EpicLoot.AlwaysDropCheat;
            context.AddString($"> Always Drop: {EpicLoot.AlwaysDropCheat}");
        }

        private static void SpawnMagicCraftingMaterials()
        {
            foreach (var itemPrefab in EpicLoot.RegisteredItemPrefabs)
            {
                var itemDrop = UnityEngine.Object.Instantiate(itemPrefab, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up, Quaternion.identity).GetComponent<ItemDrop>();
                // TODO Mythic Hookup
                if (itemDrop.m_itemData.IsMagicCraftingMaterial() || itemDrop.m_itemData.IsRunestone() && itemDrop.m_itemData.GetCraftingMaterialRarity() != ItemRarity.Mythic)
                {
                    itemDrop.m_itemData.m_stack = itemDrop.m_itemData.m_shared.m_maxStackSize / 2;
                }
                else
                {
                    ZNetScene.instance.Destroy(itemDrop.gameObject);
                }
            }
        }

        public static void MagicItem(Terminal context, string[] args)
        {
            var rarityArg = args.Length >= 2 ? args[1] : "random";
            var itemArg = args.Length >= 3 ? args[2] : "random";
            var count = args.Length >= 4 ? int.Parse(args[3]) : 1;
            var effectCount = args.Length >= 5 ? int.Parse(args[4]) : -1;

            context.AddString($"magicitem - rarity:{rarityArg}, item:{itemArg}, count:{count}");

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
                    context.AddString($"> Could not find item: {item}");
                    break;
                }

                context.AddString($">  {i + 1} - rarity: [{string.Join(", ", rarityTable)}], item: {item}");

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
                LootRoller.CheatRollingItem = true;
                LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);
                LootRoller.CheatRollingItem = false;
            }
            LootRoller.CheatEffectCount = -1;
        }

        public static void SpawnMagicItemWithEffect(Terminal context, string[] args)
        {
            if (args.Length < 3)
            {
                EpicLoot.LogError("Specify effect and item name");
                return;
            }

            if (Player.m_localPlayer == null) return;

            var effectArg = args[1];
            var itemPrefabNameArg = args[2];
            context.AddString($"magicitem - {itemPrefabNameArg} with effect: {effectArg}");

            var magicItemEffectDef = MagicItemEffectDefinitions.Get(effectArg);
            if (magicItemEffectDef == null)
            {
                context.AddString($"> Could not find effect: {effectArg}");
                return;
            }

            var itemPrefab = ObjectDB.instance.GetItemPrefab(itemPrefabNameArg);
            if (itemPrefab == null)
            {
                context.AddString($"> Could not find item: {itemPrefabNameArg}");
                return;
            }

            var fromItemData = itemPrefab.GetComponent<ItemDrop>().m_itemData;
            if (!EpicLoot.CanBeMagicItem(fromItemData))
            {
                context.AddString($"> Can't be magic item: {itemPrefabNameArg}");
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
            LootRoller.CheatRollingItem = true;
            LootRoller.CheatForceMagicEffect = true;
            LootRoller.ForcedMagicEffect = effectArg;
            LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);
            LootRoller.CheatForceMagicEffect = false;
            LootRoller.ForcedMagicEffect = string.Empty;
            LootRoller.CheatRollingItem = false;
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

        private static void SpawnLegendaryMagicItem(Terminal context, string[] args)
        {
            if (args.Length < 2)
            {
                context.AddString("> Specify legendaryID, itemID (optional)");
                return;
            }

            var legendaryID = args[1];
            var itemType = args.Length >= 3 ? args[2] : null;

            context.AddString($"magicitemlegendary - legendaryID:{legendaryID}");
            SpawnLegendaryItemHelper(legendaryID, itemType, context);
        }

        private static void SpawnLegendaryItemHelper(string legendaryID, string itemType, Terminal context)
        {
            if (!UniqueLegendaryHelper.TryGetLegendaryInfo(legendaryID, out var legendaryInfo))
            {
                if (context != null)
                {
                    context.AddString($"> Could not find info for legendaryID: ({legendaryID})");
                }
                return;
            }

            if (string.IsNullOrEmpty(itemType))
            {
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
                    itemData.m_dropPrefab = itemPrefab;
                    var checkRequirements = legendaryInfo.Requirements.CheckRequirements(itemData, dummyMagicItem);

                    if (checkRequirements)
                    {
                        allowedItems.Add(itemDrop);
                    }
                }
                itemType = allowedItems.LastOrDefault()?.name;
            }
            
            if (string.IsNullOrEmpty(itemType))
            {
                EpicLoot.LogWarning($"No Item Type Found for LegendaryID {legendaryID} - This would've made a Club.");
                return;
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
            LootRoller.CheatRollingItem = true;
            LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);
            LootRoller.CheatRollingItem = false;

            LootRoller.CheatForceLegendary = null;
            LootRoller.CheatDisableGating = previousDisableGatingState;
        }

        private static void SpawnMagicItemSet(Terminal terminal, string[] args)
        {
            if (args.Length < 2)
            {
                terminal.AddString("> Specify Set ID");
                return;
            }

            var setID = args[1];
            terminal.AddString($"magicitemset - setID:{setID}");

            if (!UniqueLegendaryHelper.TryGetLegendarySetInfo(setID, out var setInfo))
            {
                terminal.AddString($"> Could not find set info for setID: ({setID})");
                return;
            }

            foreach (var legendaryID in setInfo.LegendaryIDs)
            {
                SpawnLegendaryItemHelper(legendaryID, null, terminal);
            }
        }

        public static void CheckStackQuality(Terminal context)
        {
            context.AddString("CheckStackQuality");
            if (ObjectDB.instance == null)
            {
                context.AddString("> ObjectDB is null");
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
                    context.AddString($"> {itemDrop.name}");
                }
            }

            if (count == 0)
            {
                context.AddString("> (none)");
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
            itemData.SaveMagicItem(magicItem);
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
