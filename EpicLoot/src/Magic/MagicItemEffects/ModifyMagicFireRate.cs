using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public class ModifyMagicFireRate
{
    [HarmonyPatch(typeof(Attack), nameof(Attack.UpdateProjectile))]
    public static class ModifyFireRate_Attack_UpdateProjectile_Patch
    {
        private static float originalBurstValue;
        private static bool set = false;
        
        public static void Prefix(Attack __instance)
        {
            if (__instance.m_character == null || __instance.m_character.IsPlayer() == false)
            {
                return;
            }
            Player player = __instance.m_character as Player;

            // Only one of the two modify fire rate types can apply to a particular item, the other one will be skipped.
            if (set == false) {
                if (player.HasActiveMagicEffect(MagicEffectType.ModifyFireRate, out float effect, 0.01f))
                {
                    set = true;
                    originalBurstValue = __instance.m_burstInterval;
                    __instance.m_burstInterval *= 1 - effect;
                }
            }
            if (set == false)
            {
                if (player.HasActiveMagicEffect(MagicEffectType.ModifyMagicFireRate, out float effect, 0.01f))
                {
                    set = true;
                    originalBurstValue = __instance.m_burstInterval;
                    __instance.m_burstInterval *= 1 - effect;
                }
            }
        }
        
        public static void Postfix(Attack __instance)
        {
            if (set == false || __instance.m_character == null || __instance.m_character.IsPlayer() == false)
            {
                return;
            }

            set = false;
            __instance.m_burstInterval = originalBurstValue;
        }
    }
}