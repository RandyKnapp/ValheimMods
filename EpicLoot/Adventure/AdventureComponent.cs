using System;
using fastJSON;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [RequireComponent(typeof(Player))]
    public class AdventureComponent : MonoBehaviour
    {
        public const string SaveDataKey = EpicLoot.PluginId + "+" + nameof(AdventureSaveData);
        private static readonly JSONParameters _saveLoadParams = new JSONParameters { UseExtensions = false };

        private Player _player;
        public AdventureSaveDataList SaveData = new AdventureSaveDataList();

        public void Awake()
        {
            _player = GetComponent<Player>();
            Load();
        }

        public void Load()
        {
            if (_player.m_knownTexts.TryGetValue(SaveDataKey, out var data))
            {
                try
                {
                    SaveData = JSON.ToObject<AdventureSaveDataList>(data, _saveLoadParams);

                    // Clean up old bounties
                    var removed = 0;
                    foreach (var saveData in SaveData.AllSaveData)
                    {
                        removed += saveData.Bounties.RemoveAll(x => x.State == BountyState.InProgress && x.PlayerID == 0);
                    }

                    if (removed > 0)
                    {
                        EpicLoot.LogWarning($"Removed {removed} invalid bounties");
                        Save();
                    }
                }
                catch (Exception)
                {
                    SaveData = new AdventureSaveDataList();
                }
            }
            else
            {
                SaveData = new AdventureSaveDataList();
            }
        }

        public void Save()
        {
            var data = JSON.ToJSON(SaveData, _saveLoadParams);
            _player.m_knownTexts[SaveDataKey] = data;
        }
    }

    [HarmonyPatch(typeof(TextsDialog), "UpdateTextsList")]
    public static class TextsDialog_UpdateTextsList_Patch
    {
        public static void Postfix(TextsDialog __instance)
        {
            //__instance.m_texts.RemoveAll(x => x.m_topic.Equals(AdventureComponent.SaveDataKey, StringComparison.InvariantCulture));
        }
    }
}
