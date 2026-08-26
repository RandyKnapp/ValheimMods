using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EquipmentAndQuickSlots.src {
    public static class MiscPatches {
        [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.LateUpdate))]
        public static class UIToolTip_LateUpdate_Patch {
            public static void Postfix(UITooltip __instance) {
                if (!ValConfig.EquipmentSlotsEnabled.Value && !ValConfig.QuickSlotsEnabled.Value) return;

                if (ZInput.IsGamepadActive() && !ZInput.IsMouseActive()) {
                    if (!(UITooltip.m_current == __instance) || !(UITooltip.m_tooltip != null))
                        return;

                    if (__instance.m_anchor != null) {
                        UITooltip.m_tooltip.transform.localPosition = new Vector3(__instance.m_fixedPosition.x + 200, __instance.m_fixedPosition.y, 0);
                    }
                }
            }
        }

        // Pre-Mistlands versions stored slot inventories in the player's known texts, prefixed with
        // this sentinel. Until a character migrates, those entries would clutter the compendium.
        [HarmonyPatch(typeof(TextsDialog), "UpdateTextsList")]
        public static class TextsDialog_UpdateTextsList_Patch {
            public const string LegacySentinel = "<|>";

            public static void Postfix(TextsDialog __instance) {
                if (!ValConfig.ViewDebugSaveData.Value) {
                    __instance.m_texts.RemoveAll(x => x.m_topic.StartsWith(LegacySentinel));
                }
            }
        }
    }

}
