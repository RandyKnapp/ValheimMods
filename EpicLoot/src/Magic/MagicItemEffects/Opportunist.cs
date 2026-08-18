using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public class Opportunist_Character_RPC_Damage_Patch
    {
        // Prefix handler invoked by CharacterRpcDamageDispatch (attacker-side: bonus damage vs a
        // staggering victim).
        public static void ModifyIncoming(Character __instance, HitData hit, Character attacker)
        {
            if (attacker is Player player &&
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