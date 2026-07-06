using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public static class Warmth
    {
        public static bool AddingStatusFromEnv;

        [HarmonyPatch(typeof(Player), nameof(Player.UpdateEnvStatusEffects))]
        public static class Warmth_Player_UpdateEnvStatusEffects_Patch
        {
            public static void Prefix()
            {
                AddingStatusFromEnv = true;
            }

            public static void Postfix(Player __instance)
            {
                AddingStatusFromEnv = false;
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), typeof(int), typeof(bool), typeof(int), typeof(float))]
        public static class Warmth_SEMan_AddStatusEffect_Patch
        {
            public static bool Prefix(SEMan __instance, int nameHash)
            {
                if (AddingStatusFromEnv && __instance.m_character is Player player &&
                    (nameHash == "Freezing".GetHashCode() || nameHash == "Cold".GetHashCode()))
                {
                    if (player.HasActiveMagicEffect(MagicEffectType.Warmth, out float effectValue))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
    
}
