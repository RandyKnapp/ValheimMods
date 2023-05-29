using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public static class Waterproof
    {
        public static int AddingStatusFromEnv;

        //public void UpdateEnvStatusEffects(float dt)
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateEnvStatusEffects))]
        public static class Waterproof_Player_UpdateEnvStatusEffects_Patch
        {
            public static bool Prefix()
            {
                AddingStatusFromEnv++;
                return true;
            }

            public static void Postfix(Player __instance)
            {
                AddingStatusFromEnv--;
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.AddStatusEffect), typeof(int), typeof(bool), typeof(int), typeof(float))]
        public static class Waterproof_SEMan_AddStatusEffect_Patch
        {
            public static bool Prefix(SEMan __instance, int name)
            {
                if (AddingStatusFromEnv > 0 && __instance.m_character.IsPlayer() && name == "Wet".GetHashCode())
                {
                    var player = (Player) __instance.m_character;
                    var hasWaterproofEquipment = player.HasActiveMagicEffect(MagicEffectType.Waterproof);
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
