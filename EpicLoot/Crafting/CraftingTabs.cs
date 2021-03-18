using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Crafting
{
    public enum CraftingTabType
    {
        Crafting,
        Upgrade,
        Disenchant,
        Enchant
    }

    public static class CraftingTabs
    {
        public static List<TabController> TabControllers = new List<TabController>();
        public static Text TabTitle;

        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        public static class InventoryGui_Awake_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                TabControllers.Clear();
                TabControllers.Add(new TabController(CraftingTabType.Crafting, false, __instance.m_tabCraft));
                TabControllers.Add(new TabController(CraftingTabType.Upgrade, false, __instance.m_tabUpgrade));
                TabControllers.Add(new DisenchantTabController());
                TabControllers.Add(new EnchantTabController());

                foreach (var tabController in CustomTabs())
                {
                    tabController.Awake();
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "OnDestroy")]
        public static class InventoryGui_OnDestroy_Patch
        {
            public static void Postfix()
            {
                foreach (var tabController in CustomTabs())
                {
                    tabController.OnDestroy();
                }
                TabControllers.Clear();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Hide")]
        public static class InventoryGui_Hide_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                if (!__instance.m_animator.GetBool("visible"))
                {
                    return true;
                }

                foreach (var tabController in TabControllers)
                {
                    tabController.SetActive(false);
                }
                TabControllers.Find(x => x.Tab == CraftingTabType.Crafting)?.SetActive(true);

                return true;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateCraftingPanel")]
        public static class InventoryGui_UpdateCraftingPanel_Patch
        {
            public static bool Prefix(InventoryGui __instance, bool focusView)
            {
                var index = 0;
                foreach (var tabController in CustomTabs())
                {
                    tabController.TryInitialize(__instance, index++, OnTabPressed);
                }

                var player = Player.m_localPlayer;
                var station = player.GetCurrentCraftingStation();
                foreach (var tabController in CustomTabs())
                {
                    var isAllowedAtThisStation = tabController.IsAllowedAtThisStation(station);
                    tabController.TabButton.gameObject.SetActive(isAllowedAtThisStation);
                }

                var activeTab = GetActiveTabController();
                if (activeTab != null)
                {
                    activeTab.UpdateCraftingPanel(__instance, focusView);
                    return false;
                }

                return true;
            }

            /*public static void Postfix(InventoryGui __instance)
            {
                var index = 0;
                var start = new Vector2(-560, -300);
                var spacing = -38;
                foreach (var tabController in TabControllers)
                {
                    var tabButton = tabController.TabButton;
                    if (tabButton != null && tabButton.gameObject.activeSelf)
                    {
                        var rt = tabButton.transform as RectTransform;
                        rt.anchoredPosition = start + new Vector2(0, index * spacing);

                        index++;
                    }
                }

                if (TabTitle == null)
                {
                    __instance.m_tabCraft.transform.parent.SetSiblingIndex(__instance.m_repairPanel.GetSiblingIndex() + 1);

                    TabTitle = Object.Instantiate(__instance.m_craftingStationName, __instance.m_craftingStationName.transform.parent, false);
                    TabTitle.color = Color.white;
                    TabTitle.fontSize = 26;
                    TabTitle.rectTransform.anchoredPosition += new Vector2(0, -38);
                }

                var activeTab = GetActiveTabController();
                if (__instance.InCraftTab())
                {
                    TabTitle.text = Localization.instance.Localize("$inventory_craftbutton");
                }
                else if (__instance.InUpradeTab())
                {
                    TabTitle.text = Localization.instance.Localize("$inventory_upgradebutton");
                }
                else if (activeTab != null)
                {
                    TabTitle.text = activeTab.GetTabButtonText();
                }
            }*/
        }

        //public void UpdateRecipe(Player player, float dt)
        [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
        public static class InventoryGui_UpdateRecipe_Patch
        {
            public static void Postfix(InventoryGui __instance, Player player, float dt)
            {
                var activeTab = GetActiveTabController();
                if (activeTab != null)
                {
                    var magicItemBG = __instance.m_recipeIcon.transform.parent.Find("MagicItemBG");
                    Image bgImage;
                    if (magicItemBG == null)
                    {
                        bgImage = Object.Instantiate(__instance.m_recipeIcon, __instance.m_recipeIcon.transform.parent, true);
                        bgImage.name = "MagicItemBG";
                        bgImage.transform.SetSiblingIndex(__instance.m_recipeIcon.transform.GetSiblingIndex());
                        bgImage.sprite = EpicLoot.GetMagicItemBgSprite();
                        bgImage.color = Color.white;
                    }
                    else
                    {
                        bgImage = magicItemBG.GetComponent<Image>();
                    }

                    activeTab.UpdateRecipe(__instance, player, dt, bgImage);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed")]
        public static class InventoryGui_OnCraftPressed_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                var activeTab = GetActiveTabController();
                if (activeTab != null)
                {
                    activeTab.OnCraftPressed(__instance);
                    return false;
                }

                return true;
            }
        }

        //public void DoCrafting(Player player)
        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        public static class InventoryGui_DoCrafting_Patch
        {
            public static bool Prefix(InventoryGui __instance, Player player)
            {
                var activeTab = GetActiveTabController();
                if (activeTab != null)
                {
                    activeTab.DoCrafting(__instance, player);
                    return false;
                }

                return true;
            }
        }

        private static IEnumerable<TabController> CustomTabs()
        {
            return TabControllers.Where(x => x.IsCustomTab);
        }

        private static TabController GetActiveTabController()
        {
            return TabControllers.FirstOrDefault(tabController => tabController.IsCustomTab && tabController.IsActive());
        }

        private static void OnTabPressed(TabController tab)
        {
            InventoryGui.instance.OnCraftCancelPressed();

            foreach (var tabController in TabControllers)
            {
                tabController.SetActive(tabController.Tab == tab.Tab);
            }

            InventoryGui.instance.UpdateCraftingPanel();
        }

        //public void OnTabCraftPressed()
        [HarmonyPatch(typeof(InventoryGui), "OnTabCraftPressed")]
        public static class InventoryGui_OnTabCraftPressed_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                OnTabPressed(TabControllers.FirstOrDefault(x => x.Tab == CraftingTabType.Crafting));
                return false;
            }
        }

        //public void OnTabUpgradePressed()
        [HarmonyPatch(typeof(InventoryGui), "OnTabUpgradePressed")]
        public static class InventoryGui_OnTabUpgradePressed_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                OnTabPressed(TabControllers.FirstOrDefault(x => x.Tab == CraftingTabType.Upgrade));
                return false ;
            }
        }
    }
}
