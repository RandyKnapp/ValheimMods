using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public class AvoidDamageTaken_Character_RPC_Damage_Patch
    {
        // Called from SharedCharacterRpcDamagePatch's prefix, which returns this, so false cancels the hit.
        public static bool ShouldTakeDamage(Character __instance, HitData hit, Character attacker)
        {
            if (__instance is Player player && attacker != null && attacker != __instance)
            {
                var avoidanceChance = ModifyWithLowHealth.Sum(player, MagicEffectType.AvoidDamageTaken);

                bool avoid = Random.Range(0f, 1f) < avoidanceChance;

                if (avoid)
                {
                    DamageText.instance.ShowText(HitData.DamageModifier.VeryResistant, hit.m_point, 0, true);
                }

                return !avoid;
            }

            return true;
        }
    }
}