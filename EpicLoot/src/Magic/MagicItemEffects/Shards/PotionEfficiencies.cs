using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicLoot.src.Magic.MagicItemEffects.Shards {
    internal class PotionEfficiencies {

        // Provides a bonus to potion duration
        public static class PotionEfficacy {
            [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
            private static class Player_ConsumeItem_Patch {
                [UsedImplicitly]
                private static void Postfix(Player __instance, ItemDrop.ItemData item, bool __result) {
                    if (!__result || __instance != Player.m_localPlayer
                        || item?.m_shared?.m_consumeStatusEffect == null) {
                        return;
                    }

                    var fraction = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.PotionEfficacy, 0.01f);
                    if (fraction <= 0f) {
                        return;
                    }

                    var se = __instance.GetSEMan().GetStatusEffect(item.m_shared.m_consumeStatusEffect.NameHash());
                    if (se != null && se.m_ttl > 0f) {
                        se.m_ttl *= 1f + fraction;
                    }
                }
            }
        }
    }
}
