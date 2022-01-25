using Common;
using System;
using Auga;
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
        public GameObject GamepadHint;
        public Auga.WorkbenchTabData AugaTabData;

        protected bool _hasInventoryListener;

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
            if (TabButton != null)
            {
                Object.Destroy(TabButton.gameObject);
            }
            TabButton = null;
        }

        public virtual void TryInitialize(InventoryGui inventoryGui, int tabIndex, Action<TabController> onTabPressed)
        {
            if (EpicLoot.HasAuga)
            {
                var exists = Auga.API.Workbench_HasWorkbenchTab(GetTabButtonId());
                if (IsCustomTab && !exists)
                {
                    var buttonSprite = EpicLoot.LoadAsset<Sprite>($"{GetTabButtonId().ToLower()}_tabicon");
                    AugaTabData = Auga.API.Workbench_AddWorkbenchTab(GetTabButtonId(), buttonSprite, GetTabButtonText(), (index) => onTabPressed(this));
                    var tabButtonGameObject = AugaTabData.TabButtonGO;
                    TabButton = tabButtonGameObject.GetComponent<Button>();
                }
            }
            else
            {
                var tabContainer = GetTabContainer(inventoryGui);
                var existingButton = tabContainer.Find(GetTabButtonId());
                if (IsCustomTab && existingButton == null)
                {
                    var go = Object.Instantiate(GetTabPrefab(inventoryGui), tabContainer, true).gameObject;
                    go.name = GetTabButtonId();
                    TabButton = go.GetComponent<Button>();

                    var label = go.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        label.text = GetTabButtonText();
                    }
                    go.RectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GetTabWidth());
                    TabButton.gameObject.RectTransform().anchoredPosition = TabButton.gameObject.RectTransform().anchoredPosition + new Vector2(107 * (tabIndex + 1), 0);

                    go.transform.SetSiblingIndex(tabContainer.childCount - 2);
                    TabButton.onClick = new Button.ButtonClickedEvent();
                    TabButton.onClick.AddListener(TabButton.GetComponent<ButtonSfx>().OnClick);
                    TabButton.onClick.AddListener(() => onTabPressed(this));
                }
            }

            if (TabButton != null)
            {
                var gp = TabButton.GetComponent<UIGamePad>();
                if (gp != null)
                {
                    GamepadHint = gp.m_hint;
                    Object.Destroy(gp);
                }
            }
        }

        public Transform GetTabContainer(InventoryGui inventoryGui)
        {
            return inventoryGui.m_tabCraft.transform.parent;
        }

        public Button GetTabPrefab(InventoryGui inventoryGui)
        {
            return inventoryGui.m_tabUpgrade;
        }

        public virtual string GetTabButtonId() => "TabId";
        public virtual string GetTabButtonText() => "TabButton";
        public virtual float GetTabWidth() => 100;

        public virtual bool IsAllowedAtThisStation(CraftingStation station)
        {
            return true;
        }

        public virtual bool IsActive()
        {
            if (TabButton == null || !TabButton.isActiveAndEnabled)
            {
                return false;
            }

            return EpicLoot.HasAuga ? Auga.API.Workbench_IsTabActive(TabButton.gameObject) : !TabButton.interactable;
        }

        public virtual void SetActive(bool active)
        {
            if (TabButton != null && (!EpicLoot.HasAuga || !IsCustomTab))
            {
                TabButton.interactable = !active;
            }

            SelectedRecipe = -1;
            if (active)
            {
                if (!_hasInventoryListener && Player.m_localPlayer != null)
                {
                    Player.m_localPlayer.GetInventory().m_onChanged += OnInventoryChanged;
                    _hasInventoryListener = true;
                }
                GenerateRecipes();
            }
            else
            {
                if (_hasInventoryListener && Player.m_localPlayer != null)
                {
                    Player.m_localPlayer.GetInventory().m_onChanged -= OnInventoryChanged;
                    _hasInventoryListener = false;
                }
            }
        }

        public virtual void OnInventoryChanged()
        {
            if (IsActive())
            {
                InventoryGui.instance.OnCraftCancelPressed();
                UpdateCraftingPanel(InventoryGui.instance, false);
            }
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

        public static bool SetupRequirement(
            InventoryGui __instance,
            Transform elementRoot,
            ItemDrop item,
            int amount,
            Player player,
            bool showOutOfMaterials,
            out bool haveMaterials)
        {
            haveMaterials = false;
            var icon = elementRoot.transform.Find("res_icon").GetComponent<Image>();
            var nameText = elementRoot.transform.Find("res_name").GetComponent<Text>();
            var amountText = elementRoot.transform.Find("res_amount").GetComponent<Text>();
            var tooltip = elementRoot.GetComponent<UITooltip>();
            if (item != null)
            {
                icon.gameObject.SetActive(true);
                nameText.gameObject.SetActive(true);
                amountText.gameObject.SetActive(true);
                if (item.m_itemData.IsMagicCraftingMaterial())
                {
                    var rarity = item.m_itemData.GetCraftingMaterialRarity();
                    icon.sprite = item.m_itemData.m_shared.m_icons[EpicLoot.GetRarityIconIndex(rarity)];
                }
                else
                {
                    icon.sprite = item.m_itemData.GetIcon();
                }
                icon.color = Color.white;

                var bgIconTransform = (RectTransform)icon.transform.parent.Find("bgIcon");
                if (item.m_itemData.UseMagicBackground())
                {
                    if (bgIconTransform == null)
                    {
                        bgIconTransform = (RectTransform)Object.Instantiate(icon, icon.transform.parent, true).transform;
                        bgIconTransform.name = "bgIcon";
                        bgIconTransform.SetSiblingIndex(icon.transform.GetSiblingIndex());
                        bgIconTransform.anchorMin = Vector2.zero;
                        bgIconTransform.anchorMax = new Vector2(1, 1);
                        bgIconTransform.sizeDelta = Vector2.zero;
                        bgIconTransform.pivot = new Vector2(0.5f, 0.5f);
                        bgIconTransform.anchoredPosition = Vector2.zero;
                    }

                    bgIconTransform.gameObject.SetActive(true);
                    var bgIcon = bgIconTransform.GetComponent<Image>();
                    bgIcon.sprite = EpicLoot.GetMagicItemBgSprite();
                    bgIcon.color = item.m_itemData.GetRarityColor();
                }
                else if (bgIconTransform != null)
                {
                    bgIconTransform.gameObject.SetActive(false);
                }

                tooltip.m_text = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
                nameText.text = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
                if (amount <= 0)
                {
                    InventoryGui.HideRequirement(elementRoot);
                    return false;
                }
                amountText.text = amount.ToString();

                haveMaterials = player.HaveRequirements(new []{ new Piece.Requirement() { m_resItem = item, m_amount = amount } }, false, 1);
                if (showOutOfMaterials && !haveMaterials)
                {
                    amountText.color = Mathf.Sin(Time.time * 10.0f) > 0.0f ? Color.red : Color.white;
                }
                else
                {
                    amountText.color = Color.white;
                }
            }
            else
            {
                var bgIconTransform = icon.transform.parent.Find("bgIcon");
                if (bgIconTransform != null)
                {
                    bgIconTransform.gameObject.SetActive(false);
                }
            }
            return true;
        }

        public virtual bool DisallowInventoryHiding()
        {
            return false;
        }

        public virtual void OnCraftCanceled()
        {
        }
    }
}
