using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Magic.MagicItemEffects
{
    public static class Headhunter
    {
        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
        public static class IncreaseTrophyDropChance
        {
            private static void Postfix(CharacterDrop __instance)
            {
                if (Player.m_localPlayer == null ||
                    !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.HeadHunter, out float effectValue, 0.01f))
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
