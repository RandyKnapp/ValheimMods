using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common;
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
        Enchant,
        Augment
    }

    public enum CraftingTabStyle
    {
        Horizontal,
        HorizontalSquish,
        Vertical,
        Angled
    }

    public static class CraftingTabs
    {
        public static readonly List<TabController> TabControllers = new List<TabController>();
        public static readonly List<Button> OtherTabs = new List<Button>();
        public static Text TabTitle;
        public static Coroutine UpdateTabPositionCoroutine;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class InventoryGui_Awake_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                TabControllers.Clear();
                TabControllers.Add(new TabController(CraftingTabType.Crafting, false, __instance.m_tabCraft));
                TabControllers.Add(new TabController(CraftingTabType.Upgrade, false, __instance.m_tabUpgrade));
                TabControllers.Add(new DisenchantTabController());
                //TabControllers.Add(new EnchantTabController());
                TabControllers.Add(new AugmentTabController());

                foreach (var tabController in CustomTabs())
                {
                    tabController.Awake();
                }

                if (!EpicLoot.HasAuga)
                {
                    UpdateTabPositionCoroutine = __instance.StartCoroutine(UpdateTabPositions());
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnDestroy))]
        public static class InventoryGui_OnDestroy_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                foreach (var tabController in CustomTabs())
                {
                    tabController.OnDestroy();
                }
                TabControllers.Clear();
                OtherTabs.Clear();

                if (UpdateTabPositionCoroutine != null)
                {
                    __instance.StopCoroutine(UpdateTabPositionCoroutine);
                }
                UpdateTabPositionCoroutine = null;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        public static class InventoryGui_Hide_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                if (!__instance.m_animator.GetBool("visible"))
                {
                    return true;
                }

                var activeTab = GetActiveTabController();
                if (activeTab != null && activeTab.DisallowInventoryHiding())
                {
                    return false;
                }

                foreach (var tabController in TabControllers)
                {
                    tabController.SetActive(false);
                }
                TabControllers.Find(x => x.Tab == CraftingTabType.Crafting)?.SetActive(true);

                return true;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCraftingPanel))]
        public static class InventoryGui_UpdateCraftingPanel_Patch
        {
            public static bool Prefix(InventoryGui __instance, bool focusView)
            {
                var index = 0;
                foreach (var tabController in TabControllers)
                {
                    tabController.TryInitialize(__instance, index, OnTabPressed);
                    if (tabController.IsCustomTab)
                    {
                        index++;
                    }
                }

                var player = Player.m_localPlayer;
                if (player == null)
                {
                    return true;
                }

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
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftCancelPressed))]
        public static class InventoryGui_OnCraftCancelPressed_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                foreach (var tabController in CustomTabs())
                {
                    tabController.OnCraftCanceled();
                }
            }
        }

        public static IEnumerator UpdateTabPositions()
        {
            while (true)
            {
                var inventoryGui = InventoryGui.instance;
                if (inventoryGui == null)
                {
                    yield return null;
                    continue;
                }

                var allTabs = GetAllTabs();
                foreach (var tabButton in allTabs)
                {
                    if (TabControllers.All(x => x.TabButton != tabButton) && !OtherTabs.Contains(tabButton))
                    {
                        OtherTabs.Add(tabButton);
                        tabButton.onClick.AddListener(() => OnOtherTabPressed(tabButton));
                    }
                }
                OtherTabs.RemoveAll(x => !allTabs.Contains(x));

                var index = 0;
                var start = GetStart();
                var spacing = GetSpacing(allTabs);
                var angle = EpicLoot.CraftingTabStyle.Value == CraftingTabStyle.Angled ? 60 : 0;
                var squishedWidth = Mathf.Min(100, 500 / allTabs.Count);

                foreach (var tabButton in allTabs)
                {
                    if (tabButton != null && tabButton.gameObject.activeSelf)
                    {
                        var rt = (RectTransform)tabButton.transform;
                        rt.anchoredPosition = start + (index * spacing);
                        rt.localEulerAngles = new Vector3(0, 0, angle);

                        if (EpicLoot.CraftingTabStyle.Value == CraftingTabStyle.HorizontalSquish)
                        {
                            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, squishedWidth);
                        }

                        index++;
                    }
                }

                if (EpicLoot.CraftingTabStyle.Value == CraftingTabStyle.Vertical)
                {
                    if (TabTitle == null)
                    {
                        inventoryGui.m_tabCraft.transform.parent.SetSiblingIndex(inventoryGui.m_repairPanel.GetSiblingIndex() + 1);

                        TabTitle = Object.Instantiate(inventoryGui.m_craftingStationName, inventoryGui.m_craftingStationName.transform.parent, false);
                        TabTitle.color = Color.white;
                        TabTitle.fontSize = 26;
                        TabTitle.rectTransform.anchoredPosition += new Vector2(0, -38);
                    }

                    if (allTabs.TryFind(tab => !tab.interactable, out var activeTab))
                    {
                        var activeTabText = activeTab.GetComponentInChildren<Text>();
                        if (activeTabText != null)
                        {
                            TabTitle.text = activeTabText.text;
                        }
                    }
                }
                else if (EpicLoot.CraftingTabStyle.Value == CraftingTabStyle.Angled)
                {
                    inventoryGui.m_craftingStationName.transform.localPosition = new Vector3(-424, inventoryGui.m_craftingStationName.transform.localPosition.y);
                }

                yield return null;
            }
        }

        public static Vector2 GetStart()
        {
            switch (EpicLoot.CraftingTabStyle.Value)
            {
                default:
                case CraftingTabStyle.Horizontal:
                case CraftingTabStyle.HorizontalSquish:
                    return new Vector2(-448, -144);
                case CraftingTabStyle.Vertical:
                    return new Vector2(-560, -300);
                case CraftingTabStyle.Angled:
                    return new Vector2(-250, -110);
            }
        }

        public static Vector2 GetSpacing(List<Button> allTabs)
        {
            switch (EpicLoot.CraftingTabStyle.Value)
            {
                default:
                case CraftingTabStyle.Horizontal:
                    return new Vector2(107, 0);
                case CraftingTabStyle.HorizontalSquish:
                    return new Vector2(allTabs.Count <= 5 ? 107 : ((498 / allTabs.Count) + 7), 0);
                case CraftingTabStyle.Vertical:
                    return new Vector2(0, -38);
                case CraftingTabStyle.Angled:
                    return new Vector2(38, 0);
            }
        }

        //public void UpdateRecipe(Player player, float dt)
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
        public static class InventoryGui_UpdateRecipe_Patch
        {
            public static void Postfix(InventoryGui __instance, Player player, float dt)
            {
                var activeTab = GetActiveTabController();
                if (activeTab != null)
                {
                    var icon = EpicLoot.HasAuga ? Auga.API.RequirementsPanel_GetIcon(activeTab.AugaTabData.RequirementsPanelGO) : __instance.m_recipeIcon;
                    var magicItemBG = icon.transform.parent.Find("MagicItemBG");
                    Image bgImage;
                    if (magicItemBG == null)
                    {
                        bgImage = Object.Instantiate(icon, icon.transform.parent, true);
                        bgImage.name = "MagicItemBG";
                        bgImage.transform.SetSiblingIndex(icon.transform.GetSiblingIndex());
                        bgImage.sprite = EpicLoot.GetMagicItemBgSprite();
                        bgImage.color = Color.white;
                        bgImage.rectTransform.anchorMin = new Vector2(0, 0);
                        bgImage.rectTransform.anchorMax = new Vector2(1, 1);
                        bgImage.rectTransform.sizeDelta = new Vector2(0, 0);
                        bgImage.rectTransform.anchoredPosition = new Vector2(0, 0);
                    }
                    else
                    {
                        bgImage = magicItemBG.GetComponent<Image>();
                    }

                    activeTab.UpdateRecipe(__instance, player, dt, bgImage);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
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
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
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

        public static List<Button> GetAllTabs()
        {
            return InventoryGui.instance.m_tabCraft.transform.parent.GetComponentsInChildren<Button>().ToList();
        }

        private static void OnTabPressed(TabController tab)
        {
            InventoryGui.instance.OnCraftCancelPressed();

            foreach (var tabController in TabControllers)
            {
                tabController.SetActive(tabController.Tab == tab.Tab);
            }

            foreach (var otherTab in OtherTabs)
            {
                otherTab.interactable = true;
            }

            InventoryGui.instance.UpdateCraftingPanel();
        }

        private static void OnOtherTabPressed(Button tabButton)
        {
            InventoryGui.instance.OnCraftCancelPressed();

            foreach (var tabController in TabControllers)
            {
                tabController.SetActive(false);
            }

            foreach (var otherTab in OtherTabs)
            {
                otherTab.interactable = otherTab != tabButton;
            }

            InventoryGui.instance.UpdateCraftingPanel();
        }

        //public void OnTabCraftPressed()
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabCraftPressed))]
        public static class InventoryGui_OnTabCraftPressed_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                OnTabPressed(TabControllers.FirstOrDefault(x => x.Tab == CraftingTabType.Crafting));
                return false;
            }
        }

        //public void OnTabUpgradePressed()
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTabUpgradePressed))]
        public static class InventoryGui_OnTabUpgradePressed_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                OnTabPressed(TabControllers.FirstOrDefault(x => x.Tab == CraftingTabType.Upgrade));
                return false ;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeGamepadInput))]
        public static class InventoryGui_UpdateRecipeGamepadInput_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                var gamepadActive = ZInput.IsGamepadActive();
                var activeIndex = 0;
                for (int i = 0; i < TabControllers.Count; i++)
                {
                    var tabController = TabControllers[i];
                    if (tabController.GamepadHint == null)
                    {
                        continue;
                    }

                    tabController.GamepadHint.SetActive(gamepadActive && (i == 0 || i == TabControllers.Count - 1));
                    if (tabController.IsActive())
                    {
                        activeIndex = i;
                    }
                }

                if (ZInput.GetButtonDown("JoyLStickLeft"))
                {
                    activeIndex--;
                    if (activeIndex < 0)
                    {
                        activeIndex = TabControllers.Count - 1;
                    }
                    OnTabPressed(TabControllers[activeIndex]);
                }
                else if (ZInput.GetButtonDown("JoyLStickRight"))
                {
                    activeIndex++;
                    if (activeIndex >= TabControllers.Count)
                    {
                        activeIndex = 0;
                    }
                    OnTabPressed(TabControllers[activeIndex]);
                }
            }
        }
    }
}
