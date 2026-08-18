using Newtonsoft.Json;
using System;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [RequireComponent(typeof(Player))]
    public class AdventureComponent : MonoBehaviour
    {
        public const string SaveDataKey = EpicLoot.PluginId + "+" + nameof(AdventureSaveData);

        private Player _player;
        public AdventureSaveDataList SaveData = new AdventureSaveDataList();

        public void Awake()
        {
            _player = GetComponent<Player>();
            Load();
        }

        public void Load()
        {
            // Upgrade old bounty information to new save system
            if (_player.m_knownTexts.TryGetValue(SaveDataKey, out var oldData))
            {
                if (!_player.m_customData.ContainsKey(SaveDataKey))
                {
                    _player.m_customData.Add(SaveDataKey, oldData);
                }

                _player.m_knownTexts.Remove(SaveDataKey);
            }

            if (_player.m_customData.TryGetValue(SaveDataKey, out var data))
            {
                SaveData = Deserialize(data);

                // Clean up old bounties
                var removed = 0;
                foreach (var saveData in SaveData.AllSaveData)
                {
                    removed += saveData.Bounties.RemoveAll(x => x.State == BountyState.InProgress && x.PlayerID == 0);
                }

                if (removed > 0)
                {
                    EpicLoot.LogWarning($"Removed {removed} invalid bounties");
                }
            }
            else
            {
                SaveData = new AdventureSaveDataList();
            }
        }

        /// <summary>
        /// Reads the save blob. New saves are a base64-encoded ZPackage; legacy saves are JSON
        /// (which starts with '{'). Legacy blobs are upgraded transparently on the next Save().
        /// </summary>
        private static AdventureSaveDataList Deserialize(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new AdventureSaveDataList();
            }

            if (!data.StartsWith("{"))
            {
                try
                {
                    return AdventureSaveDataList.FromPackage(new ZPackage(data));
                }
                catch (Exception)
                {
                    // Not a valid binary blob; fall back to the legacy JSON path below.
                }
            }

            try
            {
                return JsonConvert.DeserializeObject<AdventureSaveDataList>(data) ?? new AdventureSaveDataList();
            }
            catch (Exception)
            {
                return new AdventureSaveDataList();
            }
        }

        public void Save()
        {
            PruneStaleRecords();

            var pkg = new ZPackage();
            SaveData.ToPackage(pkg);
            pkg.SetPos(0);
            _player.m_customData[SaveDataKey] = pkg.GetBase64();
        }

        /// <summary>
        /// Drops finished records from elapsed intervals before serializing. Only runs with the
        /// world loaded, since GetCurrentInterval() dereferences EnvMan.instance.
        /// </summary>
        private void PruneStaleRecords()
        {
            if (ZNet.m_world == null || EnvMan.instance == null
                || AdventureDataManager.Bounties == null || AdventureDataManager.TreasureMaps == null)
            {
                return;
            }

            var currentBountyInterval = AdventureDataManager.Bounties.GetCurrentInterval();
            var currentTreasureInterval = AdventureDataManager.TreasureMaps.GetCurrentInterval();

            foreach (var saveData in SaveData.AllSaveData)
            {
                saveData.PruneStaleRecords(currentBountyInterval, currentTreasureInterval);
            }
        }
    }
}
