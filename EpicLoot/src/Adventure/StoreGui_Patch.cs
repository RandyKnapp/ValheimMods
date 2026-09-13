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
            // Show runs its body only when the trader or the visibility changed, but the postfix runs
            // either way, and Hide leaves m_trader null -- so never assume it is there.
            if (!EpicLoot.IsAdventureModeEnabled() || __instance == null || __instance.m_trader == null)
            {
                return;
            }

            if (__instance.m_trader.m_name == "$npc_hildir")
            {
                var existingTemperPanel = __instance.transform.Find(nameof(TemperPanel));
                if (existingTemperPanel != null)
                {
                    TemperPanel = existingTemperPanel.gameObject;
                }
                else
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

            // The hierarchy is the authority, not the static. They can disagree -- the StoreGui.OnDestroy
            // postfix clears the static while the panel is still parented under a StoreGui that outlived
            // it, and a panel already in place is found here on every later open. Adopting what is
            // actually there keeps the SetActive at the end off a null field, which otherwise throws out
            // of the postfix and leaves the panel closed for the rest of the session.
            var existingMerchantPanel = __instance.transform.Find(nameof(MerchantPanel));
            if (existingMerchantPanel != null)
            {
                MerchantPanel = existingMerchantPanel.gameObject;
            }
            else
            {
                if (MerchantPanel != null)
                {
                    // Orphaned under some other StoreGui: it can never be shown under this one.
                    Object.Destroy(MerchantPanel);
                }

                MerchantPanel = Object.Instantiate(EpicAssets.MerchantPanel, __instance.transform, false);
                // Named here rather than relying on MerchantPanel.Awake to do it: Awake does not run
                // until the object first goes active, and the Find above has to match before that.
                MerchantPanel.name = nameof(MerchantPanel);
                MerchantPanel.AddComponent<MerchantPanel>();
            }

            MerchantPanel.SetActive(true);
        }

        [HarmonyPatch(nameof(StoreGui.Hide))]
        [HarmonyPostfix]
        public static void Hide(StoreGui __instance)
        {
            // The merchant panel goes first. Running it after the temper panel meant anything thrown
            // over there left this one active, and an object that is already active gets no OnEnable on
            // the next open -- which is the call that refreshes every list in it.
            if (MerchantPanel != null)
            {
                MerchantPanel.SetActive(false);
            }

            if (global::EpicLoot.TemperPanel.Instance)
            {
                global::EpicLoot.TemperPanel.Instance.Hide();
            }
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
