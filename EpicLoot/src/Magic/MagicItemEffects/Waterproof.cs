using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public static class Waterproof
    {
        public static int AddingStatusFromEnv;

        [HarmonyPatch(typeof(Player), nameof(Player.UpdateEnvStatusEffects))]
        public static class Waterproof_Player_UpdateEnvStatusEffects_Patch
        {
            public static void Prefix()
            {
                AddingStatusFromEnv++;
            }

            // A finalizer, so a throw in the original cannot leave the count up and block Wet for good.
            public static void Finalizer()
            {
                AddingStatusFromEnv--;
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), typeof(int), typeof(bool), typeof(int), typeof(float), typeof(short))]
        public static class Waterproof_SEMan_AddStatusEffect_Patch
        {
            public static bool Prefix(SEMan __instance, int nameHash)
            {
                if (AddingStatusFromEnv > 0 && __instance.m_character.IsPlayer() && nameHash == SEMan.s_statusEffectWet)
                {
                    var player = (Player) __instance.m_character;
                    var hasWaterproofEquipment = player.HasActiveMagicEffect(MagicEffectType.Waterproof, out float _);
                    if (hasWaterproofEquipment)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
    
}
