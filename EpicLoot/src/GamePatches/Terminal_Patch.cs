using BepInEx;
using EpicLoot.Abilities;
using EpicLoot.Adventure;
using EpicLoot.Adventure.Feature;
using EpicLoot.GatedItemType;
using EpicLoot.LegendarySystem;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class Terminal_Patch
    {
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
                SpawnLegendaryMagicItem(args.Context, args.Args, ItemRarity.Legendary);
            }), true);
            new Terminal.ConsoleCommand("milegend", "", (args =>
            {
                SpawnLegendaryMagicItem(args.Context, args.Args, ItemRarity.Legendary);
            }), true);
            new Terminal.ConsoleCommand("magicitemmythic", "", (args =>
            {
                SpawnLegendaryMagicItem(args.Context, args.Args, ItemRarity.Mythic);
            }), true);
            new Terminal.ConsoleCommand("mimythic", "", (args =>
            {
                SpawnLegendaryMagicItem(args.Context, args.Args, ItemRarity.Mythic);
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
                Player player = Player.m_localPlayer;
                AdventureSaveData saveData = player.GetAdventureSaveData();
                saveData.TreasureMaps.Clear();
                saveData.NumberOfTreasureMapsOrBountiesStarted = 0;
                ResetMinimap();
            }));
            new Terminal.ConsoleCommand("resettm", "", (args =>
            {
                Player player = Player.m_localPlayer;
                AdventureSaveData saveData = player.GetAdventureSaveData();
                saveData.TreasureMaps.Clear();
                saveData.NumberOfTreasureMapsOrBountiesStarted = 0;
                ResetMinimap();
            }));
            new Terminal.ConsoleCommand("debugtreasuremap", "", (args =>
            {
                MinimapController.DebugMode = !MinimapController.DebugMode;
                args.Context.AddString($"> Treasure Map Debug Mode: {MinimapController.DebugMode}");
            }));
            new Terminal.ConsoleCommand("debugtm", "", (args =>
            {
                MinimapController.DebugMode = !MinimapController.DebugMode;
                args.Context.AddString($"> Treasure Map Debug Mode: {MinimapController.DebugMode}");
            }));
            new Terminal.ConsoleCommand("resetbounties", "", (args =>
            {
                Player player = Player.m_localPlayer;
                AdventureSaveData saveData = player.GetAdventureSaveData();
                saveData.Bounties.Clear();
                ResetMinimap();
            }));
            new Terminal.ConsoleCommand("testbountynames", "", (args =>
            {
                Random random = new Random();
                int count = (args.Length >= 2) ? int.Parse(args[1]) : 10;
                for (int i = 0; i < count; ++i)
                {
                    string name = BountiesAdventureFeature.GenerateTargetName(random);
                    args.Context.AddString(name);
                }
            }));
            new Terminal.ConsoleCommand("resetadventure", "", (args =>
            {
                Player player = Player.m_localPlayer;
                AdventureComponent adventureComponent = player.GetComponent<AdventureComponent>();
                adventureComponent.SaveData = new AdventureSaveDataList();
                ResetMinimap();
            }));
            new Terminal.ConsoleCommand("bounties", "", (args =>
            {
                int interval = (args.Length >= 2) ? int.Parse(args[1]) : AdventureDataManager.Bounties.GetCurrentInterval();
                List<BountyInfo> availableBounties = AdventureDataManager.Bounties.GetAvailableBounties(interval, false);
                BountiesAdventureFeature.PrintBounties($"Bounties for Interval {interval}:", availableBounties);
            }));
            new Terminal.ConsoleCommand("playerbounties", "", (args =>
            {
                Player player = Player.m_localPlayer;
                List<BountyInfo> availableBounties = player.GetAdventureSaveData().Bounties;
                BountiesAdventureFeature.PrintBounties($"Player Bounties:", availableBounties);
            }));
            new Terminal.ConsoleCommand("gotomerchant", "", (args =>
            {
                Player player = Player.m_localPlayer;
                if (ZoneSystem.instance.FindClosestLocation("Vendor_BlackForest", player.transform.position, out var location))
                {
                    Console.instance.AddString(location.m_position.ToString());
                    player.TeleportTo(location.m_position + Vector3.right * 5, player.transform.rotation, true);
                }
            }), true);
            new Terminal.ConsoleCommand("gotom", "", (args =>
            {
                Player player = Player.m_localPlayer;
                if (ZoneSystem.instance.FindClosestLocation("Vendor_BlackForest", player.transform.position, out var location))
                {
                    Console.instance.AddString(location.m_position.ToString());
                    player.TeleportTo(location.m_position + Vector3.right * 5, player.transform.rotation, true);
                }
            }), true);
            new Terminal.ConsoleCommand("globalkeys", "", (args =>
            {
                if (ZoneSystem.instance != null)
                {
                    args.Context.AddString("> Print Global Keys:");
                    foreach (string globalKey in ZoneSystem.instance.GetGlobalKeys())
                    {
                        args.Context.AddString("> " + globalKey);
                    }
                }
            }));
            new Terminal.ConsoleCommand("lootres", "", (args =>
            {
                string lootTable = args.Length > 1 ? args[1] : "Greydwarf";
                int level = args.Length > 2 ? int.Parse(args[2]) : 1;
                int itemIndex = args.Length > 3 ? int.Parse(args[3]) : 0;
                LootRoller.PrintLootResolutionTest(lootTable, level, itemIndex);
            }));
            new Terminal.ConsoleCommand("resetcooldowns", "", (args =>
            {
                Player player = Player.m_localPlayer;
                if (player != null)
                {
                    AbilityController abilityController = player.GetComponent<AbilityController>();
                    if (abilityController != null)
                    {
                        foreach (Ability ability in abilityController.CurrentAbilities)
                        {
                            ability.ResetCooldown();
                        }
                    }
                }
            }), true);
            new Terminal.ConsoleCommand("debugluck", "", (args => {
                LootRoller.DebugLuckFactor();
            }));
            new Terminal.ConsoleCommand("tooltipdebug", "", (args => {

                GenerateTooltipTest.GenerateInventoryTooltips(false);
            }));
            new Terminal.ConsoleCommand("tooltipdebugvanilla", "", (args => {

                GenerateTooltipTest.GenerateInventoryTooltips(true);
            }));
        }

        private static void ResetMinimap()
        {
            PinJob pinJob = new PinJob
            {
                Task = MinimapPinQueueTask.RefreshAll
            };
            MinimapController.AddPinJobToQueue(pinJob);
        }

        private static void TestTreasureMap(string[] args)
        {
            Player player = Player.m_localPlayer;

            int count = 1;
            if (args.Length >= 2)
            {
                int.TryParse(args[1], out count);
            }

            Heightmap.Biome biome = Heightmap.Biome.None;
            if (args.Length >= 3)
            {
                Enum.TryParse(args[2], out biome);
            }

            int overrideTreasureMapCount = -1;
            if (args.Length >= 4)
            {
                int.TryParse(args[3], out overrideTreasureMapCount);
            }

            AdventureDataManager.CheatNumberOfBounties = overrideTreasureMapCount;
            AdventureSaveData saveData = player.GetAdventureSaveData();
            player.StartCoroutine(TestTreasureMapCoroutine(saveData, biome, player, count));
        }

        // TODO: update these tests
        private static IEnumerator TestTreasureMapCoroutine(AdventureSaveData saveData, Heightmap.Biome biome, Player player, int count)
        {
            Heightmap.Biome[] biomes = new[] { Heightmap.Biome.Meadows, Heightmap.Biome.BlackForest, Heightmap.Biome.Swamp,
                Heightmap.Biome.Mountain, Heightmap.Biome.Plains };

            saveData.DebugMode = true;
            int startInterval = saveData.TreasureMaps.Count == 0 ? -1 : saveData.TreasureMaps.Min(x => x.Interval) - 1;
            for (int i = 0; i < count; ++i)
            {
                saveData.IntervalOverride = startInterval - (i + 1);
                Heightmap.Biome selectedBiome = biome == Heightmap.Biome.None ? biomes[UnityEngine.Random.Range(0, biomes.Length)] : biome;
                yield return AdventureDataManager.TreasureMaps.SpawnTreasureChest(selectedBiome, player, 0, OnTreasureChestSpawnComplete);
            }
            saveData.DebugMode = false;
            AdventureDataManager.CheatNumberOfBounties = -1;
        }

        private static void OnTreasureChestSpawnComplete(int price, bool success, Vector3 spawnPoint)
        {
            string output = "> Failed to spawn treasure map chest";
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
            foreach (string type in EpicLoot.MagicMaterials)
            {
                foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
                {
                    string assetName = $"{type}{rarity}";
                    GameObject itemPrefab = PrefabManager.Instance.GetPrefab(assetName);
                    Transform transform = Player.m_localPlayer.transform;
                    ItemDrop itemDrop = UnityEngine.Object.Instantiate(itemPrefab,
                        transform.position + transform.forward * 2f + Vector3.up,
                        Quaternion.identity).GetComponent<ItemDrop>();
                    itemDrop.m_itemData.m_stack = itemDrop.m_itemData.m_shared.m_maxStackSize / 2;
                }
            }
        }

        public static void MagicItem(Terminal context, string[] args)
        {
            string rarityArg = args.Length >= 2 ? args[1] : "random";
            string itemArg = args.Length >= 3 ? args[2] : "random";
            int count = args.Length >= 4 ? int.Parse(args[3]) : 1;
            int effectCount = args.Length >= 5 ? int.Parse(args[4]) : -1;

            context.AddString($"magicitem - rarity:{rarityArg}, item:{itemArg}, count:{count}");

            List<string> allItemNames = ObjectDB.instance.m_items
                .Where(x => EpicLoot.CanBeMagicItem(x.GetComponent<ItemDrop>().m_itemData))
                .Where(x => x.name != "HelmetDverger" && x.name != "BeltStrength" && x.name != "Wishbone")
                .Select(x => x.name)
                .ToList();

            if (Player.m_localPlayer == null)
            {
                return;
            }

            LootRoller.CheatEffectCount = effectCount;
            for (int i = 0; i < count; i++)
            {
                float[] rarityTable = GetRarityTable(rarityArg);

                string item = itemArg;
                if (item == "random")
                {
                    WeightedRandomCollection<string> weightedRandomTable =
                        new WeightedRandomCollection<string>(allItemNames, x => 1);
                    item = weightedRandomTable.Roll();
                }

                if (ObjectDB.instance.GetItemPrefab(item) == null)
                {
                    context.AddString($"> Could not find item: {item}");
                    break;
                }

                context.AddString($">  {i + 1} - rarity: [{string.Join(", ", rarityTable)}], item: {item}");

                LootTable loot = new LootTable()
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

                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere;
                Vector3 dropPoint = Player.m_localPlayer.transform.position +
                    Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
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

            string effectArg = args[1];
            string itemPrefabNameArg = args[2];
            context.AddString($"magicitem - {itemPrefabNameArg} with effect: {effectArg}");

            MagicItemEffectDefinition magicItemEffectDef = MagicItemEffectDefinitions.Get(effectArg);
            if (magicItemEffectDef == null)
            {
                context.AddString($"> Could not find effect: {effectArg}");
                return;
            }

            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemPrefabNameArg);
            if (itemPrefab == null)
            {
                context.AddString($"> Could not find item: {itemPrefabNameArg}");
                return;
            }

            ItemDrop.ItemData fromItemData = itemPrefab.GetComponent<ItemDrop>().m_itemData;
            if (!EpicLoot.CanBeMagicItem(fromItemData))
            {
                context.AddString($"> Can't be magic item: {itemPrefabNameArg}");
                return;
            }

            MagicItemEffectRequirements effectRequirements = magicItemEffectDef.Requirements;
            ItemRarity itemRarity = effectRequirements.AllowedRarities.Count == 0 ? ItemRarity.Magic :
                effectRequirements.AllowedRarities.First();
            float[] rarityTable = GetRarityTable(itemRarity.ToString());
            LootTable loot = new LootTable
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

            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere;
            Vector3 dropPoint = Player.m_localPlayer.transform.position +
                Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
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
            float[] rarityTable = new float[] {1, 1, 1, 1, 1};
            switch (rarityName.ToLowerInvariant())
            {
                case "magic":
                    rarityTable = new float[] {1, 0, 0, 0, 0};
                    break;
                case "rare":
                    rarityTable = new float[] {0, 1, 0, 0, 0 };
                    break;
                case "epic":
                    rarityTable = new float[] {0, 0, 1, 0, 0 };
                    break;
                case "legendary":
                    rarityTable = new float[] {0, 0, 0, 1, 0 };
                    break;
                case "mythic":
                    rarityTable = new float[] { 0, 0, 0, 0, 1 };
                    break;
            }

            return rarityTable;
        }

        private static void SpawnLegendaryMagicItem(Terminal context, string[] args, ItemRarity rarity = ItemRarity.Legendary)
        {
            if (args.Length < 2)
            {
                context.AddString("> Specify legendaryID, itemID (optional)");
                return;
            }

            string legendaryID = args[1];
            string itemType = args.Length >= 3 ? args[2] : null;

            if (rarity == ItemRarity.Legendary)
            {
                context.AddString($"magicitemlegendary - legendaryID:{legendaryID}");
            }
            else
            {
                context.AddString($"magicitemmythic - legendaryID:{legendaryID}");
            }

            SpawnLegendaryItemHelper(legendaryID, itemType, context, rarity);
        }

        private static void SpawnLegendaryItemHelper(string legendaryID, string itemType, Terminal context, ItemRarity rarity)
        {
            if (!UniqueLegendaryHelper.TryGetLegendaryInfo(legendaryID, out LegendaryInfo itemInfo))
            {
                if (context != null)
                {
                    context.AddString($"> Could not find legendary/mythic info for legendaryID: ({legendaryID})");
                }
                return;
            }

            if (string.IsNullOrEmpty(itemType))
            {
                MagicItem dummyMagicItem = new MagicItem { Rarity = rarity };
                List<ItemDrop> allowedItems = new List<ItemDrop>();
                foreach (string itemName in GatedItemTypeHelper.AllItemsWithDetails.Keys)
                {
                    GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
                    if (itemPrefab == null)
                    {
                        continue;
                    }

                    ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
                    if (itemDrop == null)
                    {
                        continue;
                    }

                    ItemDrop.ItemData itemData = itemDrop.m_itemData;
                    itemData.m_dropPrefab = itemPrefab;
                    bool checkRequirements = itemInfo.Requirements.CheckRequirements(itemData, dummyMagicItem);

                    if (checkRequirements)
                    {
                        allowedItems.Add(itemDrop);
                    }
                }

                if (allowedItems.Count == 0)
                {
                    context.AddString($"> Could not find suitable items with parameter ({itemType}) for legendaryID: ({legendaryID})");
                    return;
                }

                int selected = UnityEngine.Random.Range(0, allowedItems.Count);
                itemType = allowedItems.ElementAt(selected).name;
            }

            if (itemType.IsNullOrWhiteSpace())
            {
                context.AddString($"> Could not find suitable item for legendaryID: ({legendaryID})");
            }

            LootTable loot = new LootTable
            {
                Object = "Console",
                Drops = new[] { new float[] { 1, 1 } },
                Loot = new[]
                {
                    new LootDrop
                    {
                        Item = itemType,
                        Rarity = GetRarityTable(rarity.ToString())
                    }
                }
            };

            if (rarity == ItemRarity.Legendary)
            {
                LootRoller.CheatForceLegendary = legendaryID;
            }
            else
            {
                LootRoller.CheatForceMythic = legendaryID;
            }

            bool previousDisableGatingState = LootRoller.CheatDisableGating;
            LootRoller.CheatDisableGating = true;

            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere;
            Vector3 dropPoint = Player.m_localPlayer.transform.position +
                Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
            LootRoller.CheatRollingItem = true;
            LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);

            LootRoller.CheatRollingItem = false;
            LootRoller.CheatForceLegendary = null;
            LootRoller.CheatForceMythic = null;
            LootRoller.CheatDisableGating = previousDisableGatingState;
        }

        private static void SpawnMagicItemSet(Terminal terminal, string[] args)
        {
            if (args.Length < 2)
            {
                terminal.AddString("> Specify Set ID");
                return;
            }

            string setID = args[1];
            terminal.AddString($"magicitemset - setID:{setID}");

            if (!UniqueLegendaryHelper.TryGetLegendarySetInfo(setID,
                out LegendarySetInfo setInfo, out ItemRarity rarity))
            {
                terminal.AddString($"> Could not find set info for setID: ({setID})");
                return;
            }

            if (setInfo != null)
            {
                foreach (string legendaryID in setInfo.LegendaryIDs)
                {
                    SpawnLegendaryItemHelper(legendaryID, null, terminal, rarity);
                }
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

            int count = 0;
            foreach (GameObject itemObject in ObjectDB.instance.m_items)
            {
                ItemDrop itemDrop = itemObject.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    continue;
                }

                ItemDrop.ItemData itemData = itemDrop.m_itemData;

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
    }
}
