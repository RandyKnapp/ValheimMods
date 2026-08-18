using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public class AvoidDamageTaken_Character_RPC_Damage_Patch
    {
        // Prefix handler invoked by CharacterRpcDamageDispatch. Returns false to cancel the incoming hit
        // (the dispatcher propagates that as the Harmony prefix return value).
        public static bool ShouldTakeDamage(Character __instance, HitData hit, Character attacker)
        {

            if (__instance is Player player && attacker != null && attacker != __instance)
            {
                var avoidanceChance = 0f;
                ModifyWithLowHealth.Apply(player, MagicEffectType.AvoidDamageTaken, effect =>
                {
                    avoidanceChance += player.GetTotalActiveMagicEffectValue(effect, 0.01f);
                });

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