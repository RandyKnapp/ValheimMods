using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Magic.MagicItemEffects
{
    public static class Headhunter
    {
        // Matches Riches and Lucky Loot. The effect's owner has to be in the area for the kill to count.
        private const float PlayerScanRange = 100f;

        // Player-ZDO key holding the local player's Head Hunter value, mirrored by
        // Multiplayer_Player_Patch.UpdateRichesAndLuck on equip/unequip.
        public const string ZdoValueKey = "el-hh";

        // GenerateDropList runs on the creature's owner, which is whichever machine simulates it -- another
        // player's client in ordinary multiplayer, or the dedicated server itself under serverside
        // simulation. Neither is reliably the killer's machine, and the server has no local player at all,
        // so reading Player.m_localPlayer here silently disabled the effect for most kills. The value is
        // read off nearby players' ZDOs instead, the same way Riches and Lucky Loot do it.
        //
        // Max rather than Riches' sum: this is purely a proc chance for a single extra trophy (the loop
        // below breaks after the first trophy in the drop list), so summing two players' 10% into 20% would
        // hand a group a chance neither of them earned. Taking the best nearby value keeps a player's own
        // odds exactly what their gear says they are. No cache, for Lucky Loot's reason -- a player scan on
        // death is cheap when deaths are rare relative to frames.
        private static float BestNearbyValue(Vector3 position)
        {
            var players = new List<Player>();
            Player.GetPlayersInRange(position, PlayerScanRange, players);

            var best = 0f;
            foreach (var player in players)
            {
                var zdo = player?.m_nview?.GetZDO();
                if (zdo == null)
                {
                    continue;
                }

                var value = zdo.GetFloat(ZdoValueKey);
                if (value > best)
                {
                    best = value;
                }
            }

            // Stored raw (as HasActiveMagicEffect's caller scaled it); 0.01f converts back to a fraction.
            return best * 0.01f;
        }

        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
        public static class IncreaseTrophyDropChance
        {
            private static void Postfix(CharacterDrop __instance)
            {
                if (__instance == null || __instance.m_character == null)
                {
                    return;
                }

                float effectValue = BestNearbyValue(__instance.m_character.transform.position);
                if (effectValue <= 0f)
                {
                    return;
                }

                foreach (var drop in __instance.m_drops)
                {
                    if (drop.m_prefab != null && drop.m_prefab.name.Contains("Trophy"))
                    {
                        // Roll a chance to add this to the drop list
                        float randomv = Random.Range(0f, 1f);
                        EpicLoot.Log($"Rolling for additional trophy drop: {randomv} < {effectValue} {randomv < effectValue}");

                        if (randomv < effectValue)
                        {
                            DropTrophy(drop.m_prefab, __instance.transform.position);
                        }

                        break;
                    }
                }
            }

            /// <summary>
            /// Drop a trophy, this happens outside of the drop system because otherwise mods like
            /// DropThat will filter it out or prevent it.
            /// </summary>
            private static void DropTrophy(GameObject trophy, Vector3 position)
            {
                Vector3 iUS = UnityEngine.Random.insideUnitSphere;
                if (iUS.y < 0f)
                {
                    iUS.y = 0f - iUS.y;
                }

                GameObject go = GameObject.Instantiate(trophy,
                    (position + Vector3.up * 0.5f),
                    Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f));
                Rigidbody rb = go.GetComponent<Rigidbody>();
                rb.AddForce(iUS * 5f, ForceMode.VelocityChange);
            }
        }
    }
}
