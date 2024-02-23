using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using Common;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace EpicLoot.Adventure.Feature
{
    public class BountiesAdventureFeature : AdventureFeature
    {
        public BountyLedger BountyLedger => BountyManagmentSystem.Instance.BountyLedger;

        private const string LedgerIdentifier = "randyknapp.mods.epicloot.BountyLedger";

        public override AdventureFeatureType Type => AdventureFeatureType.Bounties;
        public override int RefreshInterval => AdventureDataManager.Config.Bounties.RefreshInterval;

        public List<BountyInfo> GetAvailableBounties()
        {
            return GetAvailableBounties(GetCurrentInterval());
        }

        public bool BossBountiesGated()
        {
            switch (EpicLoot.BossBountyMode.Value) 
            {
                case GatedBountyMode.BossKillUnlocksCurrentBiomeBounties:
                case GatedBountyMode.BossKillUnlocksNextBiomeBounties:
                    return true;
                default:
                    return false;
            }
        }

        public List<BountyInfo> GetAvailableBounties(int interval, bool removeAcceptedBounties = true)
        {
            var player = Player.m_localPlayer;
            var random = GetRandomForInterval(interval, RefreshInterval);

            var bountiesPerBiome = new MultiValueDictionary<Heightmap.Biome, BountyTargetConfig>();
            
            var defeatedBossBiomes = new List<Heightmap.Biome>();
            var previousBossKilled = false;
            var previousBoss = "";

            bool bossBountiesGated = BossBountiesGated();

            if (bossBountiesGated)
            {
                foreach (var bossConfig in AdventureDataManager.Config.Bounties.Bosses)
                {
                    if (previousBoss == "" && EpicLoot.BossBountyMode.Value == GatedBountyMode.BossKillUnlocksNextBiomeBounties)
                    {
                        defeatedBossBiomes.Add(bossConfig.Biome);
                        previousBoss = bossConfig.BossDefeatedKey;
                    }

                    if (ZoneSystem.instance.GetGlobalKey(bossConfig.BossDefeatedKey))
                    {
                        defeatedBossBiomes.Add(bossConfig.Biome);
                        previousBossKilled = true;
                        previousBoss = bossConfig.BossDefeatedKey;
                    }
                    else if ((previousBossKilled || previousBoss.Equals(bossConfig.BossDefeatedKey)) &&
                        EpicLoot.BossBountyMode.Value == GatedBountyMode.BossKillUnlocksNextBiomeBounties)
                    {
                        defeatedBossBiomes.Add(bossConfig.Biome);
                        previousBoss = bossConfig.BossDefeatedKey;
                        previousBossKilled = false;
                    }
                }
            }

            foreach (var targetConfig in AdventureDataManager.Config.Bounties.Targets)
            {
                if ((bossBountiesGated && !defeatedBossBiomes.Contains(targetConfig.Biome)) ||
                    !player.m_knownBiome.Contains(targetConfig.Biome))
                {
                    // Remove the results of undefeated biome bosses &
                    // Remove the results that the player doesn't know about yet
                    continue;
                }

                bountiesPerBiome.Add(targetConfig.Biome, targetConfig);
            }

            var selectedTargets = new List<BountyTargetConfig>();
            foreach (var entry in bountiesPerBiome)
            {
                var targets = entry.Value;
                RollOnListNTimes(random, targets, 1, selectedTargets);
            }

            var saveData = player.GetAdventureSaveData();

            var results = selectedTargets.Select(targetConfig => new BountyInfo()
            {
                Biome = targetConfig.Biome,
                Interval = interval,
                PlayerID = player.GetPlayerID(),
                Target = new BountyTargetInfo() {
                    MonsterID = targetConfig.TargetID,
                    Level = GetTargetLevel(random, targetConfig.RewardGold > 0, false),
                    Count = 1 },
                TargetName = GenerateTargetName(random),
                RewardIron = targetConfig.RewardIron,
                RewardGold = targetConfig.RewardGold,
                RewardCoins = targetConfig.RewardCoins,
                Adds = targetConfig.Adds.Select(x => new BountyTargetInfo() {
                    MonsterID = x.ID,
                    Count = x.Count,
                    Level = GetTargetLevel(random, false, true) }).ToList()
            }).ToList();

            if (removeAcceptedBounties)
            {
                results.RemoveAll(x => saveData.HasAcceptedBounty(x.Interval, x.ID));
            }

            return results;
        }

        public static string PrintBounties(string label, List<BountyInfo> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine(label);
            for (var index = 0; index < results.Count; index++)
            {
                var bountyInfo = results[index];
                sb.AppendLine($"{index} - {bountyInfo.Interval}, {bountyInfo.Biome}, " +
                    $"{bountyInfo.TargetName}, ID={bountyInfo.ID}, state={bountyInfo.State}");
            }

            EpicLoot.Log(sb.ToString());
            return sb.ToString();
        }

        public static string GenerateTargetName(Random random)
        {
            var specialNames = AdventureDataManager.Config.Bounties.Names.SpecialNames;
            var prefixes = AdventureDataManager.Config.Bounties.Names.Prefixes;
            var suffixes = AdventureDataManager.Config.Bounties.Names.Suffixes;
            if (specialNames.Count == 0 && (prefixes.Count == 0 || suffixes.Count == 0))
            {
                return string.Empty;
            }

            if (random.NextDouble() <= AdventureDataManager.Config.Bounties.Names.ChanceForSpecialName)
            {
                return RollOnList(random, specialNames);
            }

            var prefix = Localization.instance.Localize(RollOnList(random, prefixes));
            var suffix = Localization.instance.Localize(RollOnList(random, suffixes));
            var format = suffix.StartsWith(" ") || suffix.StartsWith(",") ?
                "$mod_epicloot_bounties_targetnameformat_nospace" :
                "$mod_epicloot_bounties_targetnameformat";
            return Localization.instance.Localize(format, prefix, suffix);
        }

        private static int GetTargetLevel(Random random, bool isGold, bool isAdd)
        {
            var config = AdventureDataManager.Config.Bounties;

            var min = isAdd ? config.AddsMinLevel : (isGold ? config.GoldMinLevel : config.IronMinLevel);
            var max = isAdd ? config.AddsMaxLevel : (isGold ? config.GoldMaxLevel : config.IronMaxLevel);

            return random.Next(min, max + 1);
        }

        public List<BountyInfo> GetClaimableBounties()
        {
            var results = new List<BountyInfo>();

            var saveData = Player.m_localPlayer?.GetAdventureSaveData();
            if (saveData == null)
            {
                return results;
            }

            return saveData.GetClaimableBounties().Concat(saveData.GetInProgressBounties()).ToList();
        }

        public IEnumerator AcceptBounty(Player player, BountyInfo bounty, Action<bool, Vector3> callback)
        {
            player.Message(MessageHud.MessageType.Center, "$mod_epicloot_bounties_locatingmsg");
            var saveData = player.GetAdventureSaveData();
            yield return GetRandomPointInBiome(bounty.Biome, saveData, (success, spawnPoint, _) =>
            {
                if (success)
                {
                    var offset2 = UnityEngine.Random.insideUnitCircle *
                        (AdventureDataManager.Config.TreasureMap.MinimapAreaRadius * 0.8f);
                    var offset = new Vector3(offset2.x, 0, offset2.y);
                    saveData.AcceptedBounty(bounty, spawnPoint, offset);
                    saveData.NumberOfTreasureMapsOrBountiesStarted++;

                    // Spawn Monster
                    SpawnBountyTargets(bounty, spawnPoint, offset);
                }
                else
                {
                    callback?.Invoke(false, Vector3.zero);
                }
            });
        }

        private static void SpawnBountyTargets(BountyInfo bounty, Vector3 spawnPoint, Vector3 offset)
        {
            var mainPrefab = ZNetScene.instance.GetPrefab(bounty.Target.MonsterID);
            if (mainPrefab == null)
            {
                EpicLoot.LogError($"Could not find prefab for bounty target! BountyID: " +
                    $"{bounty.ID}, MonsterID: {bounty.Target.MonsterID}");
                return;
            }

            var prefabs = new List<GameObject>() { mainPrefab };
            foreach (var addConfig in bounty.Adds)
            {
                for (var i = 0; i < addConfig.Count; i++)
                {
                    var prefab = ZNetScene.instance.GetPrefab(addConfig.MonsterID);
                    if (prefab == null)
                    {
                        EpicLoot.LogError($"Could not find prefab for bounty add! BountyID: " +
                            $"{bounty.ID}, MonsterID: {addConfig.MonsterID}");
                        return;
                    }
                    prefabs.Add(prefab);
                }
            }
            for (var index = 0; index < prefabs.Count; index++)
            {
                var prefab = prefabs[index];
                var isAdd = index > 0;

                var creature = Object.Instantiate(prefab, spawnPoint, Quaternion.identity);
                var bountyTarget = creature.AddComponent<BountyTarget>();
                bountyTarget.Initialize(bounty, prefab.name, isAdd);

                var randomSpacing = UnityEngine.Random.insideUnitSphere * 4;
                spawnPoint += randomSpacing;
                ZoneSystem.instance.FindFloor(spawnPoint, out var floorHeight);
                spawnPoint.y = floorHeight;
            }

            Minimap.instance.ShowPointOnMap(spawnPoint + offset);

            var pkg = new ZPackage();
            bounty.ToPackage(pkg);
            ZRoutedRpc.instance.InvokeRoutedRPC("SpawnBounties", pkg);
        }

        private static void OnBountyTargetSlain(BountyInfo bounty, string monsterID, bool isAdd)
        {
            if (Player.m_localPlayer == null || bounty.PlayerID != Player.m_localPlayer.GetPlayerID())
            {
                // Not my bounty
                return;
            }

            var saveData = Player.m_localPlayer.GetAdventureSaveData();

            OnBountyTargetSlain(saveData, bounty.ID, monsterID, isAdd);
        }

        private static void OnBountyTargetSlain(AdventureSaveData saveData, string bountyID, string monsterID, bool isAdd)
        {
            if (saveData == null)
            {
                return;
            }

            var bountyInfo = saveData.GetBountyInfoByID(bountyID);

            if (bountyInfo == null)
            {
                return;
            }

            if (!saveData.HasAcceptedBounty(bountyInfo.Interval, bountyInfo.ID) || bountyInfo.State != BountyState.InProgress)
            {
                return;
            }

            EpicLoot.Log($"Bounty Target Slain: bounty={bountyInfo.ID} monsterId={monsterID} " +
                $"({(isAdd ? "add" : "main target")})");

            if (!isAdd && bountyInfo.Target.MonsterID == monsterID)
            {
                bountyInfo.Slain = true;
            }
            
            if (isAdd)
            {
                foreach (var addConfig in bountyInfo.Adds)
                {
                    if (addConfig.MonsterID == monsterID && addConfig.Count > 0)
                    {
                        addConfig.Count--;
                        break;
                    }
                }
            }

            var isComplete = bountyInfo.Slain && bountyInfo.Adds.Sum(x => x.Count) == 0;
            if (isComplete)
            {
                MessageHud.instance.ShowBiomeFoundMsg("$mod_epicloot_bounties_completemsg", true);
                bountyInfo.State = BountyState.Complete;

                RemoveMinimapPin(bountyInfo);
            }
        }

        public void ClaimBountyReward(BountyInfo bountyInfo)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var saveData = player.GetAdventureSaveData();
            if (!saveData.HasAcceptedBounty(bountyInfo.Interval, bountyInfo.ID) || bountyInfo.State != BountyState.Complete)
            {
                return;
            }

            bountyInfo.State = BountyState.Claimed;

            MessageHud.instance.ShowBiomeFoundMsg("$mod_epicloot_bounties_claimedmsg", true);

            var inventory = player.GetInventory();
            if (bountyInfo.RewardIron > 0)
            {
                inventory.AddItem("IronBountyToken", bountyInfo.RewardIron, 1, 0, 0, string.Empty);
            }
            if (bountyInfo.RewardGold > 0)
            {
                inventory.AddItem("GoldBountyToken", bountyInfo.RewardGold, 1, 0, 0, string.Empty);
            }
            if (bountyInfo.RewardCoins > 0)
            {
                inventory.AddItem("Coins", bountyInfo.RewardCoins, 1, 0, 0, string.Empty);
            }
        }

        public void AbandonBounty(BountyInfo bountyInfo)
        {
            var saveData = Player.m_localPlayer?.GetAdventureSaveData();
            if (saveData != null && bountyInfo != null && saveData.BountyIsInProgress(bountyInfo.Interval, bountyInfo.ID))
            {
                saveData.AbandonedBounty(bountyInfo.ID);
                RemoveMinimapPin(bountyInfo);
            }
        }

        private static void RemoveMinimapPin(BountyInfo bountyInfo)
        {
            if (!MinimapController.BountyPins.ContainsKey(bountyInfo.ID)) return;

            var pinJob = new PinJob
            {
                Task = MinimapPinQueueTask.RemoveBountyPin,
                DebugMode = MinimapController.DebugMode,
                BountyPin = new KeyValuePair<string, AreaPinInfo>(bountyInfo.ID, MinimapController.BountyPins[bountyInfo.ID])
            };

            MinimapController.AddPinJobToQueue(pinJob);
        }

        public void RegisterRPC(ZRoutedRpc routedRpc)
        {
            if (!ZNet.instance.IsDedicated())
            {
                // Player RPCs
                routedRpc.Register<string>("SendKillLogs", RPC_Client_ReceiveKillLogs);
                routedRpc.Register<ZPackage, string, bool>("SlayBountyTargetFromServer", RPC_Client_SlayBountyTargetFromServer);
            }

            if (Common.Utils.IsServer())
            {
                // Server RPCs
                routedRpc.Register<ZPackage, string, bool>("SlayBountyTarget", RPC_SlayBountyTarget);
                routedRpc.Register<long>("RequestKillLogs", RPC_Server_RequestKillLogs);
            }
        }

        /// <summary>
        /// Received by the client, handles slaying a bounty.
        /// </summary>
        private void RPC_Client_SlayBountyTargetFromServer(long sender, ZPackage pkg, string monsterID, bool isAdd)
        {
            EpicLoot.Log($"RPC_Client_SlayBountyTargetFromServer triggered: {monsterID} ({(isAdd ? "minion" : "target")})");

            // TODO ensure do not need to check for dedicated here
            var bounty = BountyInfo.FromPackage(pkg);
            OnBountyTargetSlain(bounty, monsterID, isAdd);
        }

        /// <summary>
        /// Received by server when invoked by onDeath, handles slaying a bounty.
        /// </summary>
        public void RPC_SlayBountyTarget(long sender, ZPackage pkg, string monsterID, bool isAdd)
        {
            EpicLoot.Log($"RPC_SlayBountyTarget triggered: {monsterID} ({(isAdd ? "minion" : "target")})");

            if (!Common.Utils.IsServer())
            {
                return;
            }

            // Send to all clients that the bounty is slain
            // TODO: Can this just be sent to the player who owns the bounty?
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SlayBountyTargetFromServer", pkg, monsterID, isAdd);

            var bounty = BountyInfo.FromPackage(pkg);
            AddSlainBountyTargetToLedger(bounty, monsterID, isAdd);
        }

        /// <summary>
        /// Used to add a slain bounty to the ledger so offline players get credit upon relog.
        /// </summary>
        private void AddSlainBountyTargetToLedger(BountyInfo bounty, string monsterID, bool isAdd)
        {
            if (BountyLedger == null)
            {
                EpicLoot.LogError("[BountyLedger] Server tried to add kill log to bounty ledger but BountyLedger was null");
                return;
            }

            if (Player.m_localPlayer != null && Player.m_localPlayer.GetPlayerID() == bounty.PlayerID)
            {
                EpicLoot.Log($"[BountyLedger] This player ({bounty.PlayerID}) is the hosting local player");
                return;
            }

            // TODO: this may cause a bug if a player is respawning when the kill is sent?
            var characterZdos = ZNet.instance.GetAllCharacterZDOS();
            var playerIsOnline = characterZdos.Select(zdo => zdo.GetLong("playerID")).Any(playerID => playerID == bounty.PlayerID);
            if (playerIsOnline)
            {
                EpicLoot.Log($"[BountyLedger] This player ({bounty.PlayerID}) is connected to server, " +
                    $"do not need to log the kill.");
                return;
            }

            BountyLedger.AddKillLog(bounty.PlayerID, bounty.ID, monsterID, isAdd);
        }

        public override void OnZNetStart()
        {
        }

        public override void OnZNetDestroyed()
        {
        }

        public override void OnWorldSave()
        {
            BountyManagmentSystem.Instance.Save();
        }

        /// <summary>
        /// Receives a request to send bounty kills for player then sends a response to the client.
        /// Clears the stored bounty kills data for the player.
        /// </summary>
        private void RPC_Server_RequestKillLogs(long sender, long playerID)
        {
            if (!Common.Utils.IsServer() || BountyLedger == null)
            {
                return;
            }

            var logs = BountyLedger.GetAllKillLogs(playerID);
            if (logs == null || logs.Count == 0)
            {
                return;
            }

            var results = JsonConvert.SerializeObject(logs, Formatting.Indented);

            if (!results.IsNullOrWhiteSpace())
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, "SendKillLogs", results);
                BountyLedger.RemoveKillLogsForPlayer(playerID);
            }
        }

        /// <summary>
        /// Receives bounty kills for the player and handles syncing progress.
        /// Sends a response to the server to clear data for the player.
        /// </summary>
        private void RPC_Client_ReceiveKillLogs(long sender, string logData)
        {
            var logs = JsonConvert.DeserializeObject<BountyKillLog[]>(logData);
            if (logs == null || Player.m_localPlayer == null)
                return;

            var saveData = Player.m_localPlayer.GetAdventureSaveData();

            foreach (var killLog in logs)
            {
                OnBountyTargetSlain(saveData, killLog.BountyID, killLog.MonsterID, killLog.IsAdd);
            }
        }
    }

    /// <summary>
    /// Requests the cached bounty data from the server when the local player is spawned.
    /// </summary>
    [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
    public static class Game_SpawnPlayer_Patch
    {
        public static void Postfix()
        {
            if (ZRoutedRpc.instance != null && Player.m_localPlayer != null)
            {
                Player.m_localPlayer.StartCoroutine(WaitThenRequestKillLogs(Player.m_localPlayer));
            }
        }

        public static IEnumerator WaitThenRequestKillLogs(Player player)
        {
            yield return new WaitForSeconds(5);
            var playerID = player.GetPlayerID();
            ZRoutedRpc.instance.InvokeRoutedRPC("RequestKillLogs", playerID);
        }
    }
}
