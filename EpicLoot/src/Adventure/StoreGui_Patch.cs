using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [HarmonyPatch(typeof(StoreGui))]
    public static class StoreGui_Patch
    {
        public static GameObject MerchantPanel;
        public static GameObject TemperPanel;

        [HarmonyPatch(nameof(StoreGui.Show))]
        [HarmonyPostfix]
        public static void Show_Postfix(StoreGui __instance)
        {
            if (!EpicLoot.IsAdventureModeEnabled() || __instance == null)
            {
                return;
            }

            if (__instance.m_trader.m_name == "$npc_hildir")
            {
                if (__instance.transform.Find(nameof(TemperPanel)) == null)
                {
                    if (TemperPanel != null)
                    {
                        Object.Destroy(TemperPanel);
                    }
                    global::EpicLoot.TemperPanel.LoadFonts();
                    TemperPanel = Object.Instantiate(EpicAssets.TemperPanel, __instance.transform, false);
                    TemperPanel.name = nameof(TemperPanel);
                    TemperPanel.AddComponent<TemperPanel>();
                }

                TemperPanel.GetComponent<TemperPanel>().Show(Player.m_localPlayer);
                return;
            }

            if (__instance.m_trader.m_name != "$npc_haldor")
            {
                //Adds compatibility for other mods that may add other trader NPC's that are not Haldor.
                return;
            }

            if (__instance.transform.Find(nameof(MerchantPanel)) == null)
            {
                if (MerchantPanel != null)
                {
                    Object.Destroy(MerchantPanel);
                }

                MerchantPanel = Object.Instantiate(EpicAssets.MerchantPanel, __instance.transform, false);
                MerchantPanel.AddComponent<MerchantPanel>();
            }

            MerchantPanel.gameObject.SetActive(true);
        }

        [HarmonyPatch(nameof(StoreGui.Hide))]
        [HarmonyPostfix]
        public static void Hide(StoreGui __instance)
        {
            if (global::EpicLoot.TemperPanel.Instance)
            {
                global::EpicLoot.TemperPanel.Instance.Hide();
            }
            
            if (MerchantPanel == null)
            {
                return;
            }

            MerchantPanel.SetActive(false);
        }

        [HarmonyPatch(nameof(StoreGui.OnDestroy))]
        [HarmonyPostfix]
        public static void OnDestroy(StoreGui __instance)
        {
            MerchantPanel = null;
            TemperPanel = null;
        }
    }
}
