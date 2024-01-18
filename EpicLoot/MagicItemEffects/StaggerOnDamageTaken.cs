using System;
using HarmonyLib;
using JetBrains.Annotations;
using Random = UnityEngine.Random;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public class RPC_StaggerOnDamageTaken_Character_RPC_Damage_Patch
	{
		[UsedImplicitly]
		private static void Postfix(Character __instance, HitData hit)
        {
            if (hit == null || __instance == null)
                return; 

            var attacker = hit.GetAttacker();

            try
            {
                if (__instance is Player player && player.HasActiveMagicEffect(MagicEffectType.StaggerOnDamageTaken) && attacker != null && attacker != __instance && !attacker.IsStaggering())
                {
                    var staggerChance = player.GetTotalActiveMagicEffectValue(MagicEffectType.StaggerOnDamageTaken, 0.01f);
                    if (Random.Range(0f, 1f) < staggerChance)
                    {
                        attacker.Stagger(-attacker.transform.forward);
                    }
                }

            }
            catch (NullReferenceException)
            {
                EpicLoot.LogWarning($"[StaggerOnDamageTaken] Caught null exception. Everything is fine.");
            }
		}
	}
}