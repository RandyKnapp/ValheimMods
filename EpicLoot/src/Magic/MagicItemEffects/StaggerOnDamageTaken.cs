namespace EpicLoot.MagicItemEffects;

// Postfix handler invoked by CharacterDamageDispatch (on-hit reaction): the local player's hits have a
// chance to stagger the (PvP-enabled) victim.
public static class StaggerOnDamageTaken_Character_Damage_Patch
{
    public static void OnDamageDealt(Character __instance, HitData hit, Character attacker)
    {
        if (hit == null || __instance == null)
        {
            return;
        }

        if (attacker == Player.m_localPlayer &&
            __instance != null && attacker != __instance &&
            Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.StaggerOnDamageTaken, out float effectValue, 0.01f))
        {
            // Don't stagger friendly players, only PvP enabled ones
            if (__instance is Player && __instance.IsPVPEnabled() == false)
            {
                return;
            }

            if (UnityEngine.Random.Range(0f, 1f) <= effectValue)
            {
                __instance.Stagger(-__instance.transform.forward);
            }
        }
    }
}