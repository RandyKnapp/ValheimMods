using EpicLoot_UnityLib;
using System.Collections.Generic;
using TMPro;
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
                if (dividerParts != null)
                {
                    var dividerRT = (RectTransform)dividerParts.Item1.transform;
                    dividerRT.SetSiblingIndex(0);
                    dividerRT.anchoredPosition = new Vector2(0, 295);
                    dividerRT.sizeDelta = new Vector2(910, 40);
                    Object.Destroy(dividerParts.Item2.GetComponent<ContentSizeFitter>());
                    var contentRT = (RectTransform)dividerParts.Item2.transform;
                    contentRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);
                }

                panelBase.ReplaceMainButton(EpicLootAuga.ReplaceButtonFancy(panelBase.MainButton, false, true));
                
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

                    foreach (var modeButton in convertUI.ModeButtons)
                    {
                        AugaFixupModeSelectButton(modeButton);
                    }

                    var modeButtonContainer = (RectTransform)convertUI.ModeButtons[0].transform.parent;
                    modeButtonContainer.anchoredPosition = new Vector2(-20, modeButtonContainer.anchoredPosition.y);
                }
                else if (panelBase is EnchantUI enchantUI)
                {
                    AugaFixupMultiselectPrefab(enchantUI.CostList.ElementPrefab.gameObject);

                    // Any rarity toggle the prefab lacks is cloned first: the forwarding click listener
                    // added below is not copied by Instantiate, so a toggle cloned later would not click.
                    enchantUI.EnsureRarityButtons();
                    foreach (var rarityButton in enchantUI.RarityButtons)
                    {
                        AugaFixupRaritySelectButton(rarityButton);
                    }
                }
                else if (panelBase is AugmentUI augmentUI)
                {
                    AugaFixupMultiselectPrefab(augmentUI.CostList.ElementPrefab.gameObject);
                }
                else if (panelBase is StashUI stashUI)
                {
                    AugaFixupMultiselectPrefab(stashUI.MaterialsList.ElementPrefab.gameObject);
                    AugaFixupMultiselectPrefab(stashUI.ShardstonesList.ElementPrefab.gameObject);
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
            var newButton = ReplaceToggleWithAugaButton(modeButton);
            if (newButton == null)
                return;

            var rt = (RectTransform)newButton.transform;
            rt.anchoredPosition = new Vector2(34, 0);
            rt.sizeDelta = new Vector2(0, -10);
        }

        public static void AugaFixupRaritySelectButton(Toggle rarityButton)
        {
            var oldLabel = rarityButton.transform.Find("Text")?.GetComponent<Graphic>();
            var newButton = ReplaceToggleWithAugaButton(rarityButton);
            if (newButton == null)
                return;

            var rt = (RectTransform)newButton.transform;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            // The rarity colour now goes onto the Auga button's label, which must then stop taking the
            // button's per-state text colours.
            var rarityColor = rarityButton.GetComponent<SetRarityColor>();
            var newLabel = newButton.GetComponentInChildren<TMP_Text>();
            if (rarityColor != null && newLabel != null)
            {
                Auga.API.Button_OverrideTextColor(newButton, newLabel.color);
                rarityColor.ReplaceGraphic(oldLabel, newLabel);
            }

            var border = rarityButton.transform.Find("Border")?.GetComponent<Image>();
            var augaBorderAsset = EpicLoot.LoadAsset<GameObject>("ButtonFocusAuga");
            var augaBorderImage = augaBorderAsset != null ? augaBorderAsset.GetComponent<Image>() : null;
            if (border != null && augaBorderImage != null)
            {
                border.raycastTarget = false;
                border.sprite = augaBorderImage.sprite;
                border.pixelsPerUnitMultiplier = augaBorderImage.pixelsPerUnitMultiplier;
            }
        }

        /// <summary>
        /// Puts an Auga medium button, stretched over the toggle, in place of the toggle's own background and
        /// label. The toggle keeps its state and group; the button only forwards the click to it.
        /// </summary>
        private static Button ReplaceToggleWithAugaButton(Toggle toggle)
        {
            var oldLabel = toggle.transform.Find("Text");
            var labelText = oldLabel != null ? EpicLootAuga.GetLabel(oldLabel) : string.Empty;
            var newButton = Auga.API.MediumButton_Create(toggle.transform, toggle.name, labelText);
            if (newButton == null)
                return null;

            var background = toggle.GetComponent<Image>();
            if (background != null)
            {
                Object.Destroy(background);
            }

            toggle.toggleTransition = Toggle.ToggleTransition.None;
            newButton.transform.SetSiblingIndex(0);
            if (oldLabel != null)
            {
                Object.Destroy(oldLabel.gameObject);
            }

            var rt = (RectTransform)newButton.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;

            newButton.onClick = new Button.ButtonClickedEvent();
            newButton.onClick.AddListener(() => toggle.OnSubmit(null));

            Object.Destroy(newButton.GetComponent<ButtonSfx>());
            Object.Destroy(newButton.GetComponent<UITooltip>());
            return newButton;
        }
    }
}
