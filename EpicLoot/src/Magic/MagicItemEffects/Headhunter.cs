using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Magic.MagicItemEffects
{
    public static class Headhunter
    {
        // Matches Riches and Lucky Loot. The effect's owner has to be in the area for the kill to count.
        private const float PlayerScanRange = 100f;

        // Mirrored by Multiplayer_Player_Patch.UpdateRichesAndLuck on equip/unequip.
        public const string ZdoValueKey = "el-hh";

        // GenerateDropList runs on the creature's owner, which may be another client or a dedicated server with
        // no local player, so the value is read off nearby players' ZDOs. Max rather than Riches' sum: it is a
        // proc chance for one extra trophy, and summing would give a group odds no one's gear grants.
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
                if (__instance.m_character == null)
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

            // Outside the drop system, or mods like DropThat filter it out.
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
