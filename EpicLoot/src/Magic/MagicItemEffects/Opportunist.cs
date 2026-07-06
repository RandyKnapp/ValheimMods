using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    public class Opportunist_Character_RPC_Damage_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character __instance, HitData hit)
        {
            if (hit.GetAttacker() is Player player &&
                player.HasActiveMagicEffect(MagicEffectType.Opportunist, out float effectValue, 0.01f) &&
                __instance.IsStaggering())
            {
                if (Random.Range(0f, 1f) < effectValue)
                {
                    __instance.m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, __instance.transform);
                    hit.ApplyModifier(hit.m_backstabBonus);
                }
            }
        }
    }
}