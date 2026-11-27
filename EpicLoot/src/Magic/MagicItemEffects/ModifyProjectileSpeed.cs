using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public class ModifyProjectileSpeed
{
    [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
    public static class ModifyProjectileSpeed_Patch
    {
        // __state holds the unscaled value only when this call scaled it; the finalizer puts it back even if the
        // original throws, so the bonus can never compound.
        public static void Prefix(Attack __instance, out float? __state)
        {
            __state = null;
            if (!(__instance.m_character is Player player) || player != Player.m_localPlayer)
            {
                return;
            }

            float effectValue = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyProjectileSpeed, 0.01f);
            if (effectValue > 0f)
            {
                __state = __instance.m_projectileVel;
                __instance.m_projectileVel *= 1 + effectValue;
            }
        }

        public static void Finalizer(Attack __instance, float? __state)
        {
            if (__state.HasValue)
            {
                __instance.m_projectileVel = __state.Value;
            }
        }
    }
}
