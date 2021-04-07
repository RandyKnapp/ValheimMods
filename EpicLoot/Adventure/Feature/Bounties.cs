using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace EpicLoot.Adventure.Feature
{
    public class BountiesAdventureFeature : AdventureFeature
    {
        public override AdventureFeatureType Type => AdventureFeatureType.Bounties;
        public override int RefreshInterval => AdventureDataManager.Config.Bounties.RefreshInterval;

        public List<BountyInfo> GetAvailableBounties()
        {
            return GetAvailableBounties(GetCurrentInterval());
        }

        public List<BountyInfo> GetAvailableBounties(int interval, bool removeAcceptedBounties = true)
        {
            var player = Player.m_localPlayer;
            var random = GetRandomForInterval(interval, RefreshInterval);

            var bountiesPerBiome = new MultiValueDictionary<Heightmap.Biome, BountyTargetConfig>();
            foreach (var targetConfig in AdventureDataManager.Config.Bounties.Targets)
            {
                bountiesPerBiome.Add(targetConfig.Biome, targetConfig);
            }

            var selectedTargets = new List<BountyTargetConfig>();
            foreach (var entry in bountiesPerBiome)
            {
                var targets = entry.Value;
                RollOnListNTimes(random, targets, 1, selectedTargets);
            }

            // Remove the results that the player doesn't know about yet
            selectedTargets.RemoveAll(result => !player.m_knownBiome.Contains(result.Biome));
            var saveData = player.GetAdventureSaveData();

            var results = selectedTargets.Select(targetConfig => new BountyInfo()
            {
                Biome = targetConfig.Biome,
                Interval = interval,
                PlayerID = player.GetPlayerID(),
                Target = new BountyTargetInfo() { MonsterID = targetConfig.TargetID, Level = GetTargetLevel(random, targetConfig.RewardGold > 0, false), Count = 1 },
                TargetName = GenerateTargetName(random),
                RewardIron = targetConfig.RewardIron,
                RewardGold = targetConfig.RewardGold,
                Adds = targetConfig.Adds.Select(x => new BountyTargetInfo() { MonsterID = x.ID, Count = x.Count, Level = GetTargetLevel(random, false, true) }).ToList()
            })
                .ToList();

            //PrintBounties("Player Bounties: ", saveData.Bounties);
            //PrintBounties("Before: ", results);
            if (removeAcceptedBounties)
            {
                results.RemoveAll(x => saveData.HasAcceptedBounty(x.Interval, x.ID));
            }
            //PrintBounties("After: ", results);

            return results;
        }

        public static string PrintBounties(string label, List<BountyInfo> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine(label);
            for (var index = 0; index < results.Count; index++)
            {
                var bountyInfo = results[index];
                sb.AppendLine($"{index} - {bountyInfo.Interval}, {bountyInfo.Biome}, {bountyInfo.TargetName}, ID={bountyInfo.ID}, state={bountyInfo.State}");
            }

            EpicLoot.Log(sb.ToString());
            return sb.ToString();
        }

        public static string GenerateTargetName(Random random)
        {
            var prefixes = AdventureDataManager.Config.Bounties.Names.Prefixes;
            var suffixes = AdventureDataManager.Config.Bounties.Names.Suffixes;
            if (prefixes.Count == 0 || suffixes.Count == 0)
            {
                return string.Empty;
            }

            var prefix = RollOnList(random, prefixes);
            var suffix = RollOnList(random, suffixes);
            var space = suffix.StartsWith(" ") || suffix.StartsWith(",") ? "" : " ";
            return $"{prefix}{space}{suffix}";
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
                    var offset2 = UnityEngine.Random.insideUnitCircle * AdventureDataManager.Config.TreasureMap.MinimapAreaRadius;
                    var offset = new Vector3(offset2.x, 0, offset2.y);
                    saveData.AcceptedBounty(bounty, spawnPoint, offset);
                    saveData.NumberOfTreasureMapsOrBountiesStarted++;
                    player.SaveAdventureSaveData();

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
            var prefabNames = new List<string>() { bounty.Target.MonsterID };
            foreach (var addConfig in bounty.Adds)
            {
                for (var i = 0; i < addConfig.Count; i++)
                {
                    prefabNames.Add(addConfig.MonsterID);
                }
            }

            for (var index = 0; index < prefabNames.Count; index++)
            {
                var prefabName = prefabNames[index];
                var isAdd = index > 0;

                var prefab = ZNetScene.instance.GetPrefab(prefabName);
                var creature = Object.Instantiate(prefab, spawnPoint, Quaternion.identity);
                var bountyTarget = creature.AddComponent<BountyTarget>();
                bountyTarget.Initialize(bounty, prefabName, isAdd);

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

        private static void OnBountyTargetSlain(string bountyID, string monsterID, bool isAdd)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var saveData = player.GetAdventureSaveData();
            var bountyInfo = saveData.GetBountyInfoByID(bountyID);

            if (bountyInfo == null || bountyInfo.PlayerID != player.GetPlayerID())
            {
                // Someone else's bounty
                return;
            }

            if (!saveData.HasAcceptedBounty(bountyInfo.Interval, bountyInfo.ID) || bountyInfo.State != BountyState.InProgress)
            {
                return;
            }

            EpicLoot.Log($"Bounty Target Slain: bounty={bountyInfo.ID} monsterId={monsterID} ({(isAdd ? "add" : "main target")})");

            if (!isAdd && bountyInfo.Target.MonsterID == monsterID)
            {
                bountyInfo.Slain = true;
                player.SaveAdventureSaveData();
            }

            if (isAdd)
            {
                foreach (var addConfig in bountyInfo.Adds)
                {
                    if (addConfig.MonsterID == monsterID && addConfig.Count > 0)
                    {
                        addConfig.Count--;
                        player.SaveAdventureSaveData();
                        break;
                    }
                }
            }

            var isComplete = bountyInfo.Slain && bountyInfo.Adds.Sum(x => x.Count) == 0;
            if (isComplete)
            {
                MessageHud.instance.ShowBiomeFoundMsg("$mod_epicloot_bounties_completemsg", true);
                bountyInfo.State = BountyState.Complete;
                player.SaveAdventureSaveData();
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
            player.SaveAdventureSaveData();

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
        }

        public void AbandonBounty(BountyInfo bountyInfo)
        {
            var saveData = Player.m_localPlayer?.GetAdventureSaveData();
            if (saveData != null && bountyInfo != null && saveData.BountyIsInProgress(bountyInfo.Interval, bountyInfo.ID))
            {
                saveData.AbandonedBounty(bountyInfo.ID);
                Player.m_localPlayer.SaveAdventureSaveData();
            }
        }

        public void RegisterRPC(ZRoutedRpc routedRpc)
        {
            routedRpc.Register<ZPackage>("SpawnBounties", RPC_SpawnBounties);
            routedRpc.Register<ZPackage, string, bool>("SlayBountyTarget", RPC_SlayBountyTarget);
        }

        public void RPC_SpawnBounties(long sender, ZPackage pkg)
        {
            var bounty = BountyInfo.FromPackage(pkg);
            Debug.LogWarning($"RPC_SpawnBounties: {bounty.ID}");
        }

        public void RPC_SlayBountyTarget(long sender, ZPackage pkg, string monsterID, bool isAdd)
        {
            var bounty = BountyInfo.FromPackage(pkg);
            Debug.LogWarning($"RPC_SlayBountyTarget: {monsterID} ({(isAdd ? "minion" : "target")})");

            if (Player.m_localPlayer == null || bounty.PlayerID != Player.m_localPlayer.GetPlayerID())
            {
                // Not my bounty
                return;
            }

            OnBountyTargetSlain(bounty.ID, monsterID, isAdd);
        }
    }
}
