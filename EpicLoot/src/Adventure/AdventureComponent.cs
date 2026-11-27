using Newtonsoft.Json;
using System;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [RequireComponent(typeof(Player))]
    public class AdventureComponent : MonoBehaviour
    {
        public const string SaveDataKey = EpicLoot.PluginId + "+" + nameof(AdventureSaveData);
        private const string UnreadableSaveDataKey = SaveDataKey + ".unreadable";

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
                if (SaveData == null)
                {
                    // Kept under its own key in the character file, since the next Save() overwrites this one.
                    _player.m_customData[UnreadableSaveDataKey] = data;
                    EpicLoot.LogErrorForce($"Could not read {_player.GetPlayerName()}'s bounties and treasure maps; " +
                        $"the unreadable data is kept in the character file under \"{UnreadableSaveDataKey}\".");
                    SaveData = new AdventureSaveDataList();
                }

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
        /// Reads the save blob: a base64-encoded ZPackage, or legacy JSON (which starts with '{') that the next
        /// Save() upgrades. Null when it is neither.
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
                return JsonConvert.DeserializeObject<AdventureSaveDataList>(data);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Save()
        {
            PruneStaleRecords();

            var pkg = new ZPackage();
            SaveData.ToPackage(pkg);
            pkg.SetPos(0);
            _player.m_customData[SaveDataKey] = pkg.GetBase64();

            // Every adventure state change is persisted through here, which makes this the one place to tell
            // other mods about it.
            if (_player == Player.m_localPlayer)
            {
                API.RaiseAdventureDataChanged();
            }
        }

        /// <summary>
        /// Drops finished records from elapsed intervals before serializing. Only runs with the
        /// world loaded, since GetCurrentInterval() dereferences EnvMan.instance.
        /// </summary>
        private void PruneStaleRecords()
        {
            if (ZNet.m_world == null || EnvMan.instance == null
                || AdventureDataManager.Bounties == null || AdventureDataManager.TreasureMaps == null
                || AdventureDataManager.Gamble == null)
            {
                return;
            }

            var currentBountyInterval = AdventureDataManager.Bounties.GetCurrentInterval();
            var currentTreasureInterval = AdventureDataManager.TreasureMaps.GetCurrentInterval();
            var currentGambleInterval = AdventureDataManager.Gamble.GetCurrentInterval();

            foreach (var saveData in SaveData.AllSaveData)
            {
                saveData.PruneStaleRecords(currentBountyInterval, currentTreasureInterval,
                    currentGambleInterval);
            }
        }
    }
}
