using System;
using Common;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Crafting
{
    public class TabController
    {
        public CraftingTabType Tab;
        public bool IsCustomTab;
        public Button TabButton;
        public int SelectedRecipe = -1;

        public TabController(CraftingTabType tabType, bool isCustomTab, Button tabButton = null)
        {
            Tab = tabType;
            IsCustomTab = isCustomTab;
            TabButton = tabButton;
        }

        public virtual void Awake()
        {
        }

        public virtual void OnDestroy()
        {
            TabButton = null;
        }

        public virtual void OnHide()
        {
            if (TabButton != null)
            {
                TabButton.interactable = true;
            }

            SelectedRecipe = -1;
        }

        public virtual void TryInitialize(InventoryGui inventoryGui, int tabIndex, Action<TabController> onTabPressed)
        {
            if (IsCustomTab && TabButton == null)
            {
                var go = Object.Instantiate(inventoryGui.m_tabUpgrade, inventoryGui.m_tabUpgrade.transform.parent, true).gameObject;
                go.name = GetTabButtonId();
                go.GetComponentInChildren<Text>().text = GetTabButtonText();
                go.transform.SetSiblingIndex(inventoryGui.m_tabUpgrade.transform.GetSiblingIndex() + 1);
                go.RectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GetTabWidth());
                TabButton = go.GetComponent<Button>();
                TabButton.gameObject.RectTransform().anchoredPosition = TabButton.gameObject.RectTransform().anchoredPosition + new Vector2(107 * (tabIndex + 1), 0);
                TabButton.onClick.RemoveAllListeners();
                TabButton.onClick.AddListener(TabButton.GetComponent<ButtonSfx>().OnClick);
                TabButton.onClick.AddListener(() => onTabPressed(this));
            }
        }

        protected virtual string GetTabButtonId() => "TabId";
        protected virtual string GetTabButtonText() => "TabButton";
        protected virtual float GetTabWidth() => 100;

        public virtual bool IsAllowedAtThisStation(CraftingStation station)
        {
            return true;
        }

        public virtual bool IsActive()
        {
            return TabButton != null && TabButton.isActiveAndEnabled && !TabButton.interactable;
        }

        public virtual void SetActive(bool active)
        {
            if (TabButton != null)
            {
                TabButton.interactable = !active;
            }

            SelectedRecipe = -1;
            if (active)
            {
                Player.m_localPlayer.GetInventory().m_onChanged += OnInventoryChanged;
                GenerateRecipes();
            }
            else
            {
                Player.m_localPlayer.GetInventory().m_onChanged -= OnInventoryChanged;
            }
        }

        public virtual void OnInventoryChanged()
        {
            InventoryGui.instance.OnCraftCancelPressed();
            UpdateCraftingPanel(InventoryGui.instance, false);
        }

        public virtual void UpdateCraftingPanel(InventoryGui instance, bool focusView)
        {
        }

        public virtual void GenerateRecipes()
        {
        }

        public virtual void UpdateRecipe(InventoryGui instance, Player player, float dt, Image bgImage)
        {
        }

        public virtual void OnCraftPressed(InventoryGui instance)
        {
        }

        public virtual void DoCrafting(InventoryGui instance, Player player)
        {
        }
    }
}
