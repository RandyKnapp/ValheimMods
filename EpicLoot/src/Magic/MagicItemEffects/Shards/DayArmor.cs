using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to armor during the day.
    public static class DayArmor {
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int), typeof(float))]
        private static class GetArmor_Patch {
            [UsedImplicitly]
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result) {
                var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);
                if (player == null || !EnvMan.IsDay()) {
                    return;
                }

                var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.DayArmor, 0.01f);
                if (bonus != 0f) {
                    __result *= 1f + bonus;
                }
            }
        }
    }
}
