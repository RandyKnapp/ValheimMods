using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public class ModifyProjectileSpeed
{
    [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
    public static class ModifyProjectileSpeed_Attack_FireProjectileBurst_Patch
    {
        private static float originalVelocity;

        public static void Prefix(Attack __instance)
        {
            Player player = __instance.m_character as Player;

            if (player == Player.m_localPlayer)
            {
                originalVelocity = __instance.m_projectileVel;

                float effectValue =
                    player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyProjectileSpeed, 0.01f);
                if (effectValue > 0f)
                {
                    __instance.m_projectileVel *= 1 + effectValue;
                }
            }
        }

        public static void Postfix(Attack __instance)
        {
            Player player = __instance.m_character as Player;

            if (player == Player.m_localPlayer)
            {
                __instance.m_projectileVel = originalVelocity;
            }
        }
    }
}