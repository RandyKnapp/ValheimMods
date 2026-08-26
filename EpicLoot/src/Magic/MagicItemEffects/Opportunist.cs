using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public class Opportunist_Character_RPC_Damage_Patch
    {
        // Prefix handler invoked by SharedCharacterDamagePatch -- ATTACKER side (Character.Damage
        // serializes the modified hit to the victim's owner). It used to run on the RPC_Damage
        // (victim-owner) dispatcher, where a remote attacking player's magic data reads as empty,
        // so the bonus never applied in multiplayer.
        public static void ModifyIncoming(Character __instance, HitData hit, Character attacker)
        {
            if (attacker is Player player && player == Player.m_localPlayer &&
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