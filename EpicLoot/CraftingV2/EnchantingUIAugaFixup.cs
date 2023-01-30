using System.Collections.Generic;
using EpicLoot_UnityLib;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.CraftingV2
{
    public static class EnchantingUIAugaFixup
    {
        private static readonly HashSet<GameObject> _hasBeenFixedUp = new HashSet<GameObject>();

        public static void AugaFixup(EnchantingTableUI ui)
        {
            if (!EpicLoot.HasAuga)
                return;

            if (_hasBeenFixedUp.Contains(ui.gameObject))
                return;

            var root = ui.Root;
            EpicLootAuga.ReplaceBackground(root, true);

            foreach (var tabData in ui.TabHandler.m_tabs)
            {
                var newButton = EpicLootAuga.ReplaceVerticalLargeTab(tabData.m_button);
                tabData.m_button = newButton;
            }

            foreach (var panelBase in ui.Panels)
            {
                EpicLootAuga.FixFonts(panelBase.gameObject);

                var dividerParts = Auga.API.Divider_CreateLarge(panelBase.transform, "TitleDivider");
                var dividerRT = (RectTransform)dividerParts.Item1.transform;
                dividerRT.SetSiblingIndex(0);
                dividerRT.anchoredPosition = new Vector2(0, 295);
                dividerRT.sizeDelta = new Vector2(910, 40);
                Object.Destroy(dividerParts.Item2.GetComponent<ContentSizeFitter>());
                var contentRT = (RectTransform)dividerParts.Item2.transform;
                contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);

                panelBase.MainButton = EpicLootAuga.ReplaceButtonFancy(panelBase.MainButton, false, true);
                
                if (panelBase.AvailableItems != null)
                    AugaFixupMultiselectPrefab(panelBase.AvailableItems.ElementPrefab.gameObject);

                var existingListElements = panelBase.GetComponentsInChildren<MultiSelectItemListElement>();
                foreach (var multiSelectItemListElement in existingListElements)
                {
                    AugaFixupMultiselectPrefab(multiSelectItemListElement.gameObject);
                }

                var bottomRowHints = (RectTransform)panelBase.transform.Find("GamepadHints/BottomRow");
                if (bottomRowHints)
                    bottomRowHints.anchoredPosition = new Vector2(bottomRowHints.anchoredPosition.x, 8);

                foreach (var scrollbar in panelBase.GetComponentsInChildren<Scrollbar>())
                {
                    EpicLootAuga.FixupScrollbar(scrollbar);
                }

                if (panelBase is SacrificeUI sacrificeUI)
                {
                    AugaFixupMultiselectPrefab(sacrificeUI.SacrificeProducts.ElementPrefab.gameObject);
                }
                else if (panelBase is ConvertUI convertUI)
                {
                    AugaFixupMultiselectPrefab(convertUI.Products.ElementPrefab.gameObject);
                    AugaFixupMultiselectPrefab(convertUI.CostList.ElementPrefab.gameObject);

                    for (var index = 0; index < convertUI.ModeButtons.Count; index++)
                    {
                        var modeButton = convertUI.ModeButtons[index];
                        AugaFixupModeSelectButton(modeButton);
                    }

                    var modeButtonContainer = (RectTransform)convertUI.ModeButtons[0].transform.parent;
                    modeButtonContainer.anchoredPosition = new Vector2(-20, modeButtonContainer.anchoredPosition.y);
                }
                else if (panelBase is EnchantUI enchantUI)
                {
                    AugaFixupMultiselectPrefab(enchantUI.CostList.ElementPrefab.gameObject);

                    foreach (var rarityButton in enchantUI.RarityButtons)
                    {
                        AugaFixupRaritySelectButton(rarityButton);
                    }
                }
                else if (panelBase is AugmentUI augmentUI)
                {
                    AugaFixupMultiselectPrefab(augmentUI.CostList.ElementPrefab.gameObject);
                }
            }

            _hasBeenFixedUp.Add(ui.gameObject);
        }

        public static void AugaFixupMultiselectPrefab(GameObject prefab)
        {
            if (!EpicLoot.HasAuga || _hasBeenFixedUp.Contains(prefab))
                return;

            EpicLootAuga.FixItemBG(prefab);
            EpicLootAuga.FixListElementColors(prefab);
            EpicLootAuga.FixFonts(prefab);

            _hasBeenFixedUp.Add(prefab);
        }

        public static void AugaFixupModeSelectButton(Toggle modeButton)
        {
            Object.Destroy(modeButton.GetComponent<Image>());
            var toggle = modeButton.GetComponent<Toggle>();
            toggle.toggleTransition = Toggle.ToggleTransition.None;
            var oldText = modeButton.transform.Find("Text").GetComponent<Text>();
            var newButton = Auga.API.MediumButton_Create(modeButton.transform, modeButton.name, oldText.text);
            newButton.transform.SetSiblingIndex(0);
            Object.Destroy(oldText.gameObject);
            var rt = (RectTransform)newButton.transform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(34, 0);
            rt.sizeDelta = new Vector2(0, -10);

            newButton.onClick = new Button.ButtonClickedEvent();
            newButton.onClick.AddListener(() => toggle.OnSubmit(null));

            Object.Destroy(newButton.GetComponent<ButtonSfx>());
            Object.Destroy(newButton.GetComponent<UITooltip>());
        }

        public static void AugaFixupRaritySelectButton(Toggle rarityButton)
        {
            Object.Destroy(rarityButton.GetComponent<Image>());
            var toggle = rarityButton.GetComponent<Toggle>();
            toggle.toggleTransition = Toggle.ToggleTransition.None;
            var oldText = rarityButton.transform.Find("Text").GetComponent<Text>();
            var newButton = Auga.API.MediumButton_Create(rarityButton.transform, rarityButton.name, oldText.text);
            newButton.transform.SetSiblingIndex(0);
            Object.Destroy(oldText.gameObject);
            var rt = (RectTransform)newButton.transform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(0, 0);

            var rarityColor = toggle.GetComponent<SetRarityColor>();
            rarityColor.Graphics[0] = newButton.GetComponentInChildren<Text>();
            rarityColor.Refresh();

            var border = toggle.transform.Find("Border").GetComponent<Image>();
            var augaBorderAsset = EpicLoot.LoadAsset<GameObject>("ButtonFocusAuga");
            var augaBorderImage = augaBorderAsset.GetComponent<Image>();
            border.raycastTarget = false;
            border.sprite = augaBorderImage.sprite;
            border.pixelsPerUnitMultiplier = augaBorderImage.pixelsPerUnitMultiplier;

            newButton.onClick = new Button.ButtonClickedEvent();
            newButton.onClick.AddListener(() => toggle.OnSubmit(null));

            Object.Destroy(newButton.GetComponent<ButtonSfx>());
            Object.Destroy(newButton.GetComponent<UITooltip>());
        }
    }
}
