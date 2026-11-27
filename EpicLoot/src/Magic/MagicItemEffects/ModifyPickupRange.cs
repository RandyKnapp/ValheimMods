using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public class ModifyPickupRange
{
    [HarmonyPatch(typeof(Player), nameof(Player.AutoPickup))]
    public static class ModifyPickupRange_Patch
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

            float effectValue = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyPickupRange, 0.01f);
            if (effectValue > 0f)
            {
                __state = __instance.m_autoPickupRange;
                __instance.m_autoPickupRange *= 1 + effectValue;
            }
        }

        public static void Finalizer(Player __instance, float? __state)
        {
            if (__state.HasValue)
            {
                __instance.m_autoPickupRange = __state.Value;
            }
        }
    }
}
