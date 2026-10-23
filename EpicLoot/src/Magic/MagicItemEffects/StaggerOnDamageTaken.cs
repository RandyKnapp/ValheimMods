namespace EpicLoot.MagicItemEffects;

// Postfix handler invoked by CharacterDamageDispatch (on-hit reaction): the local player's hits have a
// chance to stagger the (PvP-enabled) victim.
public static class StaggerOnDamageTaken_Character_Damage_Patch
{
    public static void OnDamageDealt(Character __instance, HitData hit, Character attacker)
    {
        // IsValid() (not just a null check), as in Slow and Paralyze: a creature that died is destroyed from its
        // own FixedUpdate, and ZNetScene.Destroy nulls the ZDO at once while its colliders linger to the end of
        // the frame, so an Aoe resolving later in that frame still hits it and Stagger's InvokeRPC would NRE.
        // A dead target is skipped the way vanilla's own stagger in RPC_Damage skips it.
        if (hit == null || __instance == null || __instance.m_nview == null || !__instance.m_nview.IsValid()
            || __instance.IsDead())
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