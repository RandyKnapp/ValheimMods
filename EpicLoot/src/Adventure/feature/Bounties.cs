using BepInEx;
using Common;
using EpicLoot.Biomes;
using EpicLoot.Config;
using EpicLoot_UnityLib;
using HarmonyLib;
using Jotunn.Managers;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace EpicLoot.Adventure.Feature
{
    public class BountiesAdventureFeature : AdventureFeature
    {
        public BountyLedger BountyLedger => BountyManagmentSystem.Instance.BountyLedger;

        public override int RefreshInterval => AdventureDataManager.Config.Bounties.RefreshInterval;

        public List<BountyInfo> GetAvailableBounties()
        {
            return GetAvailableBounties(GetCurrentInterval());
        }

        public bool BossBountiesGated()
        {
            switch (ELConfig.BossBountyMode.Value) 
            {
                case GatedBountyMode.BossKillUnlocksCurrentBiomeBounties:
                case GatedBountyMode.BossKillUnlocksNextBiomeBounties:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Why one configured bounty target was not offered. Diagnostics only.</summary>
        public sealed class BountyFilterNote
        {
            public string TargetID;
            public string ConfiguredBiome;
            public Heightmap.Biome Biome;
            public string Reason;
        }

        /// <param name="diagnostics">
        /// When non-null, every target dropped by the filter is recorded here with the reason. The
        /// bounty list is silent by design - an empty merchant panel looks identical whether the
        /// config is empty, the prefabs are missing or the player has been nowhere - so the console
        /// command feeds this in to say which.
        /// </param>
        public List<BountyInfo> GetAvailableBounties(int interval, bool removeAcceptedBounties = true,
            List<BountyFilterNote> diagnostics = null)
        {
            var player = Player.m_localPlayer;
            var bountiesPerBiome = new MultiValueDictionary<Heightmap.Biome, BountyTargetConfig>();

            bool bossBountiesGated = BossBountiesGated();
            HashSet<Heightmap.Biome> unlockedBiomes = bossBountiesGated ?
                ComputeUnlockedBiomes(ELConfig.BossBountyMode.Value, key => ZoneSystem.instance.GetGlobalKey(key)) :
                null;

            void Note(BountyTargetConfig config, Heightmap.Biome biome, string reason)
            {
                diagnostics?.Add(new BountyFilterNote
                {
                    TargetID = config.TargetID,
                    ConfiguredBiome = config.Biome,
                    Biome = biome,
                    Reason = reason
                });
            }

            foreach (var targetConfig in AdventureDataManager.Config.Bounties.Targets)
            {
                if (PrefabManager.Instance.GetPrefab(targetConfig.TargetID) == null)
                {
                    EpicLoot.Log($"Could not find bounty prefab {targetConfig.TargetID}");
                    Note(targetConfig, Heightmap.Biome.None, "prefab not found");
                    continue;
                }

                // An unresolvable biome name was already reported when the config loaded.
                Heightmap.Biome biome = targetConfig.GetBiome();
                if (biome == Heightmap.Biome.None)
                {
                    Note(targetConfig, biome, "biome name does not resolve");
                    continue;
                }

                if (bossBountiesGated && !unlockedBiomes.Contains(biome))
                {
                    Note(targetConfig, biome, "biome boss not defeated");
                    continue;
                }

                if (!BiomeDataManager.IsDiscoveredBy(player, biome))
                {
                    Note(targetConfig, biome, "biome not discovered");
                    continue;
                }

                bountiesPerBiome.Add(biome, targetConfig);
            }

            var saveData = player.GetAdventureSaveData();
            var results = new List<BountyInfo>();
            foreach (var entry in bountiesPerBiome)
            {
                // Each biome rolls from its own stream: with one shared stream, discovering a biome or killing a
                // boss shifted every later biome's roll, so an accepted bounty came back under a new target.
                Heightmap.Biome biome = entry.Key;
                var random = GetRandomForInterval(interval, RefreshInterval, BiomeDataManager.GetName(biome).GetStableHashCode());
                var targetConfig = RollOnList(random, entry.Value);
                results.Add(new BountyInfo()
                {
                    Biome = biome,
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
                });
            }

            if (removeAcceptedBounties)
            {
                // One bounty per biome per interval, whatever target that biome rolls now.
                results.RemoveAll(x => saveData.Bounties.Exists(taken => taken.Interval == x.Interval && taken.Biome == x.Biome));
            }

            return results;
        }

        /// <summary>
        /// The biomes allowed to offer bounties under a gated mode. A biome is defeated when every one
        /// of its boss keys is set (no keys: always). Under BossKillUnlocksNextBiomeBounties a biome is
        /// also unlocked when the biome group before it is defeated, where consecutive biomes sharing
        /// one key set form a group: Ocean rides on BlackForest's key, so Eikthyr alone unlocks both.
        /// </summary>
        internal static HashSet<Heightmap.Biome> ComputeUnlockedBiomes(GatedBountyMode mode, Func<string, bool> hasKey)
        {
            var unlocked = new HashSet<Heightmap.Biome>();
            bool nextMode = mode == GatedBountyMode.BossKillUnlocksNextBiomeBounties;
            bool previousGroupDefeated = true; // nothing precedes the first biome
            bool currentGroupDefeated = false;
            IReadOnlyList<string> currentGroupKeys = null;

            foreach (BiomeDefinition definition in BiomeDataManager.BiomesInOrder)
            {
                bool defeated = definition.BossDefeatedKeys.All(hasKey);
                if (currentGroupKeys != null && !SameKeySet(currentGroupKeys, definition.BossDefeatedKeys))
                {
                    previousGroupDefeated = currentGroupDefeated;
                }

                currentGroupKeys = definition.BossDefeatedKeys;
                currentGroupDefeated = defeated;

                if (defeated || (nextMode && previousGroupDefeated))
                {
                    unlocked.Add(definition.Biome);
                }
            }

            return unlocked;
        }

        private static bool SameKeySet(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            return a.Count == b.Count && a.All(b.Contains);
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
            var min = isAdd ? AdventureDataManager.Config.Bounties.AddsMinLevel : (isGold ? AdventureDataManager.Config.Bounties.GoldMinLevel : AdventureDataManager.Config.Bounties.IronMinLevel);
            var max = isAdd ? AdventureDataManager.Config.Bounties.AddsMaxLevel : (isGold ? AdventureDataManager.Config.Bounties.GoldMaxLevel : AdventureDataManager.Config.Bounties.IronMaxLevel);
            return random.Next(min, max + 1);
        }

        public List<BountyInfo> GetClaimableBounties()
        {
            var results = new List<BountyInfo>();

            var saveData = Player.m_localPlayer.GetAdventureSaveData();
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
            yield return BountyLocationEarlyCache.TryGetBiomePoint(bounty.Biome, (success, spawnPoint) =>
            {
                if (success)
                {
                    // Spawning a bounty AcceptedBounty refused would make creatures with no save
                    // record, impossible to complete and never despawned. Forced so a silent
                    // "Accept did nothing" always shows in the log.
                    if (!saveData.AcceptedBounty(bounty, spawnPoint, Vector3.zero))
                    {
                        EpicLoot.LogWarningForce($"Save data refused bounty {bounty.ID} " +
                            $"({bounty.Biome}, state={bounty.State}); nothing was spawned.");
                        callback?.Invoke(false, spawnPoint);
                        return;
                    }

                    saveData.NumberOfTreasureMapsOrBountiesStarted++;
                    player.SaveAdventureSaveData();
                    SpawnBountyInitilizer(bounty, spawnPoint, Vector3.zero);
                }

                callback?.Invoke(success, spawnPoint);
            });
        }

        private static void SpawnBountyInitilizer(BountyInfo bounty, Vector3 spawnPoint, Vector3 offset)
        {
            bounty.Position = spawnPoint;
            AdventureSpawnController.CreateForBounty(bounty, spawnPoint);
            Minimap.instance.ShowPointOnMap(spawnPoint + offset);
        }

        private static void OnBountyTargetSlain(BountyInfo bounty, string monsterID, bool isAdd)
        {
            if (Player.m_localPlayer == null || bounty.PlayerID != Player.m_localPlayer.GetPlayerID())
            {
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

            // Persist on every credited kill, not just on completion: the add counts above are
            // progress in their own right. This also covers the offline kill ledger, which replays
            // through here after the server has already deleted the logs it sent -- an unwritten
            // credit from that path cannot be recovered.
            PlayerExtensions_Adventure.PersistLocalPlayerAdventureData();
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
            player.SaveAdventureSaveData();

            MessageHud.instance.ShowBiomeFoundMsg("$mod_epicloot_bounties_claimedmsg", true);

            if (bountyInfo.RewardIron > 0)
            {
                InventoryManagement.Instance.GiveItem("IronBountyToken", bountyInfo.RewardIron);
            }
            if (bountyInfo.RewardGold > 0)
            {
                InventoryManagement.Instance.GiveItem("GoldBountyToken", bountyInfo.RewardGold);
            }
            if (bountyInfo.RewardCoins > 0)
            {
                InventoryManagement.Instance.GiveItem("Coins", bountyInfo.RewardCoins);
            }

            API.RaiseBountyCompleted(player, bountyInfo.Target?.MonsterID);
        }

        public void AbandonBounty(BountyInfo bountyInfo)
        {
            var saveData = Player.m_localPlayer.GetAdventureSaveData();
            if (saveData != null && bountyInfo != null && saveData.BountyIsInProgress(bountyInfo.Interval, bountyInfo.ID))
            {
                saveData.AbandonedBounty(bountyInfo.ID);
                PlayerExtensions_Adventure.PersistLocalPlayerAdventureData();
                RemoveMinimapPin(bountyInfo);

                // Its spawner and creatures stay in the world otherwise, and keep spawning boosted targets nobody
                // can complete. Only the server can reach them wherever they are.
                var pkg = new ZPackage();
                bountyInfo.ToPackage(pkg);
                ZRoutedRpc.instance?.InvokeRoutedRPC("AbandonBounty", pkg);
            }
        }

        private void RPC_Server_AbandonBounty(long sender, ZPackage pkg)
        {
            if (!Common.Utils.IsServer())
            {
                return;
            }

            BountyInfo bounty;
            try
            {
                bounty = BountyInfo.FromPackage(pkg);
            }
            catch (Exception)
            {
                return;
            }

            if (!SenderIsPlayer(sender, bounty.PlayerID))
            {
                EpicLoot.LogWarning($"Ignored a request from peer {sender} to remove someone else's bounty {bounty.ID}.");
                return;
            }

            ZNet.instance.StartCoroutine(RemoveBountySpawns(bounty));
        }

        // Spread over frames: each prefab is a scan of every sector's objects.
        private static IEnumerator RemoveBountySpawns(BountyInfo bounty)
        {
            var prefabs = new List<string> { AdventureSpawnController.PrefabName, bounty.Target?.MonsterID };
            prefabs.AddRange(bounty.Adds.Select(x => x.MonsterID));
            int removed = 0;
            foreach (string prefab in prefabs.Where(x => !string.IsNullOrEmpty(x)).Distinct())
            {
                var zdos = new List<ZDO>();
                int index = 0;
                while (ZDOMan.instance != null && !ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab, zdos, ref index))
                {
                    yield return null;
                }

                if (ZDOMan.instance == null)
                {
                    yield break;
                }

                foreach (ZDO zdo in zdos)
                {
                    bool matches = prefab == AdventureSpawnController.PrefabName
                        ? AdventureSpawnController.TryReadBounty(zdo, out BountyInfo carried) && carried.ID == bounty.ID
                        : zdo.GetString(BountyTargetComponent.BountyIDKey) == bounty.ID;
                    if (!matches || !zdo.IsValid())
                    {
                        continue;
                    }

                    ZNetView instance = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(zdo) : null;
                    zdo.SetOwner(ZDOMan.GetSessionID());
                    if (instance != null)
                    {
                        ZNetScene.instance.Destroy(instance.gameObject);
                    }
                    else
                    {
                        ZDOMan.instance.DestroyZDO(zdo);
                    }

                    removed++;
                }
            }

            EpicLoot.Log($"Removed {removed} spawner and creature object(s) of abandoned bounty {bounty.ID}.");
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
                routedRpc.Register<string>("SendKillLogs", RPC_Client_ReceiveKillLogs);
                routedRpc.Register<ZPackage, string, bool>("SlayBountyTargetFromServer", RPC_Client_SlayBountyTargetFromServer);
            }

            if (Common.Utils.IsServer())
            {
                routedRpc.Register<ZDOID, string, bool>("SlayBountyTarget", RPC_SlayBountyTarget);
                routedRpc.Register<long>("RequestKillLogs", RPC_Server_RequestKillLogs);
                routedRpc.Register<ZPackage>("AbandonBounty", RPC_Server_AbandonBounty);
            }
        }

        private static bool IsFromServer(long sender)
        {
            return ZNet.instance.IsServer()
                ? sender == ZDOMan.GetSessionID()
                : ZNet.instance.GetServerPeer() is { } server && sender == server.m_uid;
        }

        private void RPC_Client_SlayBountyTargetFromServer(long sender, ZPackage pkg, string monsterID, bool isAdd)
        {
            if (!IsFromServer(sender))
            {
                return;
            }

            var bounty = BountyInfo.FromPackage(pkg);
            OnBountyTargetSlain(bounty, monsterID, isAdd);
        }

        private readonly HashSet<ZDOID> _reportedKills = new HashSet<ZDOID>();

        /// <summary>
        /// A bounty kill reported to the server. It is believed only for a bounty creature that still exists here
        /// and is owned by the sender, once per creature, and the bounty is the one stored on that creature.
        /// </summary>
        public void RPC_SlayBountyTarget(long sender, ZDOID creatureID, string monsterID, bool isAdd)
        {
            if (!Common.Utils.IsServer())
            {
                return;
            }

            ZDO zdo = ZDOMan.instance.GetZDO(creatureID);
            if (zdo == null || zdo.GetOwner() != sender ||
                zdo.GetString(BountyTargetComponent.MonsterIDKey) != monsterID ||
                zdo.GetBool(BountyTargetComponent.IsAddKey) != isAdd)
            {
                EpicLoot.LogWarning($"Ignored a bounty kill report from peer {sender}: the creature is gone, not " +
                    $"owned by that peer, or not that bounty's {monsterID}.");
                return;
            }

            if (_reportedKills.Count >= 4096)
            {
                _reportedKills.Clear();
            }

            if (!_reportedKills.Add(creatureID))
            {
                return;
            }

            BountyInfo bounty;
            try
            {
                bounty = BountyInfo.FromPackage(new ZPackage(zdo.GetString(BountyTargetComponent.BountyDataKey)));
            }
            catch (Exception)
            {
                EpicLoot.LogWarning($"Bounty creature {monsterID} carries no readable bounty; the kill is not credited.");
                return;
            }

            if (bounty.ID != zdo.GetString(BountyTargetComponent.BountyIDKey))
            {
                return;
            }

            EpicLoot.Log($"Bounty kill: {monsterID} ({(isAdd ? "minion" : "target")}) for {bounty.ID}");
            var pkg = new ZPackage();
            bounty.ToPackage(pkg);
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SlayBountyTargetFromServer", pkg, monsterID, isAdd);

            AddSlainBountyTargetToLedger(bounty, monsterID, isAdd);
            API.RaiseBountyTargetSlain(bounty, monsterID, isAdd);
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

        public override void OnWorldSave()
        {
            BountyManagmentSystem.Instance.Save();
        }

        /// <summary>Whether the peer's own character is this player, so one player cannot take another's kill logs.</summary>
        private static bool SenderIsPlayer(long sender, long playerID)
        {
            if (sender == ZDOMan.GetSessionID())
            {
                return Player.m_localPlayer != null && Player.m_localPlayer.GetPlayerID() == playerID;
            }

            ZNetPeer peer = ZNet.instance.GetPeer(sender);
            ZDO character = peer != null ? ZDOMan.instance.GetZDO(peer.m_characterID) : null;
            return character != null && character.GetLong(ZDOVars.s_playerID) == playerID;
        }

        /// <summary>
        /// Receives a request to send bounty kills for player then sends a response to the client.
        /// Clears the stored bounty kills data for the player.
        /// </summary>
        private void RPC_Server_RequestKillLogs(long sender, long playerID)
        {
            if (!Common.Utils.IsServer() || BountyLedger == null || !SenderIsPlayer(sender, playerID))
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
            if (!IsFromServer(sender))
            {
                return;
            }

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
