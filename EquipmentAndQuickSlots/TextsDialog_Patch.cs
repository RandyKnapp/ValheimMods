using HarmonyLib;

namespace EquipmentAndQuickSlots
{
    //public void UpdateTextsList()
    [HarmonyPatch(typeof(TextsDialog), "UpdateTextsList")]
    public static class TextsDialog_UpdateTextsList_Patch
    {
        public static void Postfix(TextsDialog __instance)
        {
            if (!EquipmentAndQuickSlots.ViewDebugSaveData.Value)
            {
                __instance.m_texts.RemoveAll(x => x.m_topic.StartsWith(ExtendedPlayerData.Sentinel));
            }
        }
    }
}
