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
        // Player-ZDO key holding the local player's Riches, mirrored by Multiplayer_Player_Patch.UpdateRichesAndLuck.
        // A float: diminishing shard stacking gives fractional totals, which the old int key "el-rch" truncated.
        public const string ZdoValueKey = "el-rich";
        private const float PlayerScanRange = 100f;

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
            // Summed per death over the players near this creature: the drop list is built on the creature's owner,
            // which may have no local player, so each player's total is read off their ZDO.
            var playerList = new List<Player>();
            Player.GetPlayersInRange(__instance.transform.position, PlayerScanRange, playerList);
            var richesValue = 0f;
            foreach (var player in playerList)
            {
                var zdo = player?.m_nview?.GetZDO();
                if (zdo != null)
                {
                    richesValue += zdo.GetFloat(ZdoValueKey) * 0.01f;
                }
            }

            // No riches present in the area, so nothing to do.
            if (richesValue <= 0) {
                return;
            }

            // Riches is the chance of a payout, certain from 100% up. Comparing two uniform rolls used to make it
            // about 50% at any strength up to 100%.
            if (richesValue < 1f && Random.value >= richesValue) {
                return;
            }

            // The payout budget, in table cost points; above 100% it grows in proportion.
            float richesValueRoll = Random.Range(0f, 1f) * 100f * Mathf.Max(1f, richesValue);

            // Riches table not setup, so we need to update it
            if (RichesTable.Count == 0) {
                UpdateRichesOnEffectSetup();
                // Still empty (nothing configured/resolvable): bail rather than indexing into
                // an empty table from inside CharacterDrop.GenerateDropList.
                if (RichesTable.Count == 0) {
                    return;
                }
            }

            // Randomly select _one_ loot item from the list, scale it based on the riches value, and add it to the drop list.
            // (int overload of Random.Range is max-exclusive; Count-1 made the last entry unreachable.)
            int selected = Random.Range(0, RichesTable.Count);
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