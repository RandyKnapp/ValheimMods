using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public class ModifyBuildDistance
{
    [HarmonyPatch(typeof(Player), nameof(Player.PieceRayTest))]
    public static class ModifyBuildDistance_Patch
    {
        // __state holds the unscaled value only when this call scaled it; the finalizer puts it back even if the
        // original throws, so the bonus can never compound.
        public static void Prefix(Player __instance, out float? __state)
        {
            __state = null;
            Player player = __instance;
            if (player == null)
            {
                return;
            }

            float effectValue = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyBuildDistance, 0.01f);
            if (effectValue > 0f)
            {
                __state = __instance.m_maxPlaceDistance;
                __instance.m_maxPlaceDistance *= 1 + effectValue;
            }
        }

        public static void Finalizer(Player __instance, float? __state)
        {
            if (__state.HasValue)
            {
                __instance.m_maxPlaceDistance = __state.Value;
            }
        }
    }
}
