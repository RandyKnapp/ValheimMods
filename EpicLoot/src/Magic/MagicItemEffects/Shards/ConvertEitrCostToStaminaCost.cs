using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class ConvertEitrCostToStaminaCost {
        [HarmonyPatch(typeof(Player), nameof(Player.UseEitr))]
        private static class UseEitr_Patch {
            [UsedImplicitly]
            private static void Postfix(Player __instance, float v) {
                if (v <= 0f || __instance != Player.m_localPlayer) {
                    return;
                }

                var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.ConvertEitrCostToStaminaCost, 0.01f);
                if (fraction <= 0f) {
                    return;
                }

                // Only what the stamina on hand can pay moves across. UseStamina stops at zero without
                // complaint, so refunding the full share on empty stamina would hand out free eitr. It
                // scales what it charges by the world's stamina rate, so the limit has to as well.
                var converted = v * fraction;
                if (Game.m_staminaRate > 0f) {
                    converted = Mathf.Min(converted, __instance.GetStamina() / Game.m_staminaRate);
                }

                if (converted > 0f) {
                    __instance.AddEitr(converted);     // refund the converted portion of the eitr cost...
                    __instance.UseStamina(converted);  // ...and pay it from stamina instead.
                }
            }
        }
    }
}
