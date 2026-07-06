using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.SpawnOnHit))]
    public class Apportation
    {
        public static void Postfix(Projectile __instance, GameObject go, Collider collider, Vector3 normal)
        {
            if (__instance == null || __instance.m_spawnItem == null) { return; }
            var item = __instance.m_spawnItem;
            GameObject terrain_water = go ?? collider?.gameObject;
            if (terrain_water == null) return;
            if ((go.GetComponent<MonsterAI>() || go.GetComponent<BaseAI>()) && item != null && item.HasMagicEffect(MagicEffectType.Apportation)) {
                Vector3 weaponPosition = __instance.transform.position;
                Vector3 targetPosition = weaponPosition + __instance.transform.TransformDirection(__instance.m_spawnOffset);
                if (Player.m_localPlayer != null && Player.m_localPlayer == __instance.m_owner) {
                    Player.m_localPlayer.transform.position = targetPosition;
                }
            }
        }
    }
}