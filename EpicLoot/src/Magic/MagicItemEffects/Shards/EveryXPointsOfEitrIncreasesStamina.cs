using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class EveryXPointsOfEitrIncreasesStamina {
        [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
        private static class GetTotalFoodValue_Patch {
            [HarmonyPriority(Priority.Low)]
            [UsedImplicitly]
            private static void Postfix(Player __instance, ref float stamina, ref float eitr) {
                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.EveryXPointsOfEitrIncreasesStamina, 0.01f);
                if (fraction > 0f) {
                    stamina += eitr * fraction;
                }
            }
        }
    }
}
