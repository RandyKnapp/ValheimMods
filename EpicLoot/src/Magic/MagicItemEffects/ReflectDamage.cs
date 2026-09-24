using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects
{
    public class ReflectiveDamage_Character_Damage_Patch
    {
        private static bool _isApplyingReflectiveDmg;

        // Prefix handler invoked by SharedCharacterRpcDamagePatch (victim-side, runs last in that prefix so
        // it reflects the hit after the conversion, resistance and night reductions). The reflected hit is
        // a bonus hit: it gets none of the local player's weapon-strike effects.
        public static void OnIncomingHit(Character __instance, HitData hit)
        {
            var attacker = hit.GetAttacker();
            if (__instance is Player player &&
                attacker != null && attacker != __instance && !_isApplyingReflectiveDmg &&
                player.HasActiveMagicEffect(MagicEffectType.ReflectDamage, out float effectValue, 0.01f))
            {
                if (effectValue > 0)
                {
                    var hitData = new HitData()
                    {
                        m_attacker = __instance.GetZDOID(),
                        m_dir = hit.m_dir * -1,
                        m_point = attacker.transform.localPosition,
                        m_damage = { m_pierce = 
                            (hit.GetTotalPhysicalDamage() + hit.GetTotalElementalDamage()) * effectValue }
                    };
                    try
                    {
                        _isApplyingReflectiveDmg = true;
                        HitSource.DealBonusDamage(attacker, hitData);
                    }
                    finally
                    {
                        _isApplyingReflectiveDmg = false;
                    }
                }
            }
        }
    }
}