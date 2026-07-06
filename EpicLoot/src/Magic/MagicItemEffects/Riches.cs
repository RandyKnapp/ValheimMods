using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    public static class Riches_CharacterDrop_GenerateDropList_Patch
    {
        private static float richesValue = 0f;
        private static float lastUpdateCheck = 0;
        public static readonly Dictionary<string, float> DefaultRichesTable = new Dictionary<string, float> {
            { "SilverNecklace", 30 },
            { "Ruby", 20 },
            { "AmberPearl", 10 },
            { "Amber", 5 },
            { "Coins", 1 },
        };

        public static Dictionary<GameObject, int> RichesTable = new Dictionary<GameObject, int>();
        public static KeyValuePair<GameObject, int> LowestCostEntry = new KeyValuePair<GameObject, int>(null, 0);

        public static void UpdateRichesOnEffectSetup() {
            // Don't do setup if we are not in the game scene yet (main menu doesn't count)
            if (!SceneManager.GetActiveScene().name.Equals("main"))
            {
                return;
            }

            if (MagicItemEffectDefinitions.AllDefinitions.Count > 0 && MagicItemEffectDefinitions.AllDefinitions.ContainsKey(MagicEffectType.Riches)) {
                if (MagicItemEffectDefinitions.AllDefinitions[MagicEffectType.Riches].Config != null && MagicItemEffectDefinitions.AllDefinitions[MagicEffectType.Riches].Config.Count > 0) {
                    var richesConfig = MagicItemEffectDefinitions.AllDefinitions[MagicEffectType.Riches].Config;
                    if (richesConfig.Count > 0) {
                        UpdateRichesTable(richesConfig);
                    }
                }
            }
            // Safety fallthrough in case no riches config is set
            if (RichesTable.Count == 0) {
                UpdateRichesTable(DefaultRichesTable);
            }
        }

        public static void UpdateRichesTable(Dictionary<string, float> config) {
            Dictionary<GameObject, int> newRichesTable = new Dictionary<GameObject, int>();
            LowestCostEntry = new KeyValuePair<GameObject, int>(null, 100);
            foreach (KeyValuePair<string, float> kv in config) {
                if (ObjectDB.instance.TryGetItemPrefab(kv.Key, out GameObject itemPrefab)) {
                    newRichesTable.Add(itemPrefab, Mathf.RoundToInt(kv.Value));
                    if (kv.Value < LowestCostEntry.Value) {
                        LowestCostEntry = new KeyValuePair<GameObject, int>(itemPrefab, Mathf.RoundToInt(kv.Value));
                    }
                }
            }
            if (newRichesTable.Count == 0) {
                EpicLoot.LogWarning($"Riches table is empty after update, using default riches table.");
                return;
            }

            RichesTable.Clear();
            RichesTable = newRichesTable;

            if (RichesTable.Count == 0) {
                foreach (KeyValuePair<string, float> kv in DefaultRichesTable) {
                    if (ObjectDB.instance.TryGetItemPrefab(kv.Key, out GameObject itemPrefab)) {
                        RichesTable.Add(itemPrefab, Mathf.RoundToInt(kv.Value));
                    }
                }
            }
        }

        [UsedImplicitly]
        private static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            // Only do network updates for riches every minute
            if (lastUpdateCheck < Time.time) {
                lastUpdateCheck = Time.time + 5f;

                var playerList = new List<Player>();
                Player.GetPlayersInRange(__instance.m_character.transform.position, 100f, playerList);

                richesValue = playerList.Sum(player => player.m_nview.GetZDO().GetInt("el-rch")) * 0.01f;
            }
            // No riches present in the area, so nothing to do.
            if (richesValue <= 0) {
                return;
            }
            var richesRandomRoll = Random.Range(0f, 1f);

            if (richesValue > 1) {
                richesRandomRoll = Mathf.RoundToInt(richesRandomRoll * richesValue);
            }

            float richesActivateRoll = Random.Range(0f, 1f);
            if (richesActivateRoll < richesRandomRoll) {

                // Riches table not setup, so we need to update it
                if (RichesTable.Count == 0) {
                    UpdateRichesOnEffectSetup();
                }

                // Randomly select _one_ loot item from the list, scale it based on the riches value, and add it to the drop list
                int selected = Random.Range(0, RichesTable.Count()-1);
                float richesValueRoll = richesRandomRoll * 100;
                float richesCost = RichesTable[RichesTable.Keys.ElementAt(selected)];
                float richesAmount = richesValueRoll / richesCost;
                int amount = richesAmount < 1 ? 0 : Mathf.RoundToInt(richesAmount);
                GameObject selectedPrefab = RichesTable.Keys.ElementAt(selected);
                if (amount == 0 && LowestCostEntry.Key != null) {
                    amount = Mathf.RoundToInt(richesValueRoll / LowestCostEntry.Value);
                    selectedPrefab = LowestCostEntry.Key;
                }
                if (amount >= 1) {
                    __result.Add(new KeyValuePair<GameObject, int>(selectedPrefab, amount));
                }
            }
        }
    }
}