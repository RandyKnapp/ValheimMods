using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpicLoot.Adventure;
using EpicLoot.Adventure.Feature;
using EpicLoot.Biomes;
using UnityEngine;
using Random = System.Random;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void TestTreasureMap(Terminal.ConsoleEventArgs args)
    {
        Player player = Player.m_localPlayer;

        int count = args.TryParameterInt(1, 1);
        // A vanilla name, a biomedata.json name, or a numeric value; blank picks a random map biome.
        Heightmap.Biome biome = BiomeDataManager.Resolve(args.GetString(2, ""), Heightmap.Biome.None);
        int overrideTreasureMapCount = args.TryParameterInt(3, -1);

        AdventureDataManager.CheatNumberOfBounties = overrideTreasureMapCount;
        AdventureSaveData saveData = player.GetAdventureSaveData();
        player.StartCoroutine(TestTreasureMapCoroutine(saveData, biome, player, count));
    }
    
    // TODO: update these tests
    private static IEnumerator TestTreasureMapCoroutine(AdventureSaveData saveData, Heightmap.Biome biome, Player player, int count)
    {
        // Every biome that sells a treasure map, so a biomedata.json biome is covered too.
        Heightmap.Biome[] biomes = AdventureDataManager.Config.TreasureMap.BiomeInfo
            .Where(x => x.Cost > 0)
            .Select(x => x.GetBiome())
            .Where(x => x != Heightmap.Biome.None)
            .Distinct()
            .ToArray();
        if (biome == Heightmap.Biome.None && biomes.Length == 0)
        {
            Console.instance.PrintInfo("> No treasure map biomes are configured");
            yield break;
        }

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
    
    private static void OnTreasureChestSpawnComplete( int price, bool success, Vector3 spawnPoint)
    {
        string output = "> Failed to spawn treasure map chest";
        if (success)
        {
            output = $"> Spawning Treasure Map Chest at <{spawnPoint.x:0.#}, {spawnPoint.z:0.#}> (height:{spawnPoint.y:0.#})";
        }

        Console.instance.PrintInfo(output);
        EpicLoot.LogWarning(output);
    }

    private static void ResetTreasureMap(Terminal.ConsoleEventArgs args)
    {
        Player player = Player.m_localPlayer;
        AdventureSaveData saveData = player.GetAdventureSaveData();
        saveData.TreasureMaps.Clear();
        saveData.NumberOfTreasureMapsOrBountiesStarted = 0;
        ResetMinimap();
        args.Context.PrintInfo("> Treasure Maps removed");
    }
    
    private static void ResetMinimap()
    {
        PinJob pinJob = new PinJob
        {
            Task = MinimapPinQueueTask.RefreshAll
        };
        MinimapController.AddPinJobToQueue(pinJob);
    }

    private static void DebugTreasureMap(Terminal.ConsoleEventArgs args)
    {
        MinimapController.DebugMode = !MinimapController.DebugMode;
        args.Context.PrintInfo($"> Treasure Map Debug Mode: {MinimapController.DebugMode}");
    }

    private static void ResetBounties(Terminal.ConsoleEventArgs args)
    {
        Player player = Player.m_localPlayer;
        AdventureSaveData saveData = player.GetAdventureSaveData();
        saveData.Bounties.Clear();
        ResetMinimap();
        args.Context.PrintInfo("> Bounties removed");
    }

    private static void TestBountyNames(Terminal.ConsoleEventArgs args)
    {
        Random random = new Random();
        int count = args.TryParameterInt(1, 10);
        for (int i = 0; i < count; ++i)
        {
            string name = BountiesAdventureFeature.GenerateTargetName(random);
            args.Context.PrintInfo(name);
        }
    }

    private static void ResetAdventure(Terminal.ConsoleEventArgs args)
    {
        Player player = Player.m_localPlayer;
        AdventureComponent adventureComponent = player.GetComponent<AdventureComponent>();
        adventureComponent.SaveData = new AdventureSaveDataList();
        ResetMinimap();
        args.Context.PrintInfo("> Cleared adventure data");
    }

    /// <summary>
    /// Reports what the adventure spawn picker can actually see. When a player says a bounty or
    /// treasure map "did nothing", this is the first thing to run: it shows the resolved world size,
    /// whether the biome index built, and per biome how many cells fall inside that biome's
    /// configured radius band. A biome with cells but none in band is the failure that used to be
    /// silent.
    /// </summary>
    private static void AdventureIndexInfo(Terminal.ConsoleEventArgs args)
    {
        if (args.Length > 1 && args[1].Equals("rebuild", System.StringComparison.OrdinalIgnoreCase))
        {
            WorldBiomeIndex.Reset();
            WorldBiomeIndex.EnsureBuilt();
            args.Context.PrintInfo("> Rebuilding world biome index...");
            return;
        }

        WorldBiomeIndex.EnsureBuilt();

        var sb = new StringBuilder();
        sb.AppendLine($"World extent: {WorldExtent.Describe()}");
        sb.AppendLine($"Biome index: {WorldBiomeIndex.Describe()}");

        if (WorldBiomeIndex.State != BiomeIndexState.Ready)
        {
            sb.AppendLine("(index not ready; run again in a moment)");
            args.Context.PrintInfo(sb.ToString());
            return;
        }

        foreach (Heightmap.Biome biome in WorldBiomeIndex.IndexedBiomes.OrderBy(x => x.ToString()))
        {
            BountyLocationEarlyCache.GetRadiusBand(biome, out float min, out float max);
            int inBand = WorldBiomeIndex.CountCellsInBand(biome, min, max);
            string warning = inBand == 0 ? "  <-- NOTHING IN BAND" : string.Empty;
            sb.AppendLine($"{biome}: {WorldBiomeIndex.CountCells(biome)} cells, " +
                $"{inBand} in band {min:0}-{max:0}m{warning}");
        }

        args.Context.PrintInfo(sb.ToString());
    }

    private static void PrintAvailableBounties(Terminal.ConsoleEventArgs args)
    {
        int interval = args.TryParameterInt(1, AdventureDataManager.Bounties.GetCurrentInterval());
        List<BountyInfo> availableBounties = AdventureDataManager.Bounties.GetAvailableBounties(interval, false);
        if (availableBounties.Count <= 0)
        {
            args.Context.PrintInfo($"Bounties for Interval {interval}: (None)");
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Bounties for Interval {interval}:");
            for (var i = 0; i < availableBounties.Count; ++i)
            {
                var bountyInfo = availableBounties[i];
                sb.AppendLine($"{i} - {bountyInfo.Interval}, {bountyInfo.Biome}, " +
                              $"{bountyInfo.TargetName}, ID={bountyInfo.ID}, state={bountyInfo.State}");
            }
            args.Context.PrintInfo(sb.ToString());
        }
    }

    private static void PrintPlayerAvailableBounties(Terminal.ConsoleEventArgs args)
    {
        Player player = Player.m_localPlayer;
        List<BountyInfo> availableBounties = player.GetAdventureSaveData().Bounties;
        if (availableBounties.Count <= 0)
        {
            args.Context.PrintInfo("Player Bounties:: (None)");
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine("Player Bounties:");
            for (var i = 0; i < availableBounties.Count; ++i)
            {
                var bountyInfo = availableBounties[i];
                sb.AppendLine($"{i} - {bountyInfo.Interval}, {bountyInfo.Biome}, " +
                              $"{bountyInfo.TargetName}, ID={bountyInfo.ID}, state={bountyInfo.State}");
            }
            args.Context.PrintInfo(sb.ToString());
        }
    }
}