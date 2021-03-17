using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace EquipmentAndQuickSlots
{
    //public void UpdateTextsList()
    [HarmonyPatch(typeof(TextsDialog), "UpdateTextsList")]
    public static class TextsDialog_UpdateTextsList_Patch
    {
        public static void Postfix(TextsDialog __instance)
        {
            __instance.m_texts.RemoveAll(x => x.m_topic.StartsWith(ExtendedPlayerData.Sentinel));
        }
    }
}
