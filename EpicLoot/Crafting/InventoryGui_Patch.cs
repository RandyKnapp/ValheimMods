using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Crafting
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    public static class InventoryGui_UpdateRecipe_Patch
    {
        public static void Postfix(InventoryGui __instance)
        {
            var recipeDesc = __instance.m_recipeDecription;
            if (EpicLoot.UseScrollingCraftDescription.Value && recipeDesc.GetComponent<ContentSizeFitter>() == null)
            {
                var contentSizeFitter = recipeDesc.gameObject.AddComponent<ContentSizeFitter>();
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                recipeDesc.resizeTextForBestFit = false;
                recipeDesc.fontSize = 18;
                recipeDesc.rectTransform.anchorMin = new Vector2(0, 1);
                recipeDesc.rectTransform.anchorMax = new Vector2(1, 1); // pin top, stretch horiz
                recipeDesc.rectTransform.pivot = new Vector2(0, 1);
                recipeDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
                recipeDesc.rectTransform.anchoredPosition = new Vector2(4, 4);
                recipeDesc.raycastTarget = false;

                var scrollRectGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
                scrollRectGO.transform.SetParent(__instance.m_recipeDecription.transform.parent, false);
                scrollRectGO.transform.SetSiblingIndex(0);
                var rt = scrollRectGO.transform as RectTransform;
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(11, -74);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 330);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300);
                scrollRectGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);

                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
                viewport.transform.SetParent(scrollRectGO.transform, false);
                var vrt = viewport.transform as RectTransform;
                vrt.anchorMin = new Vector2(0, 0);
                vrt.anchorMax = new Vector2(1, 1);
                vrt.sizeDelta = new Vector2(0, 0);
                recipeDesc.transform.SetParent(vrt, false);

                var scrollRect = scrollRectGO.GetComponent<ScrollRect>();
                scrollRect.viewport = vrt;
                scrollRect.content = recipeDesc.rectTransform;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                scrollRect.scrollSensitivity = 30;
                scrollRect.inertia = false;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.onValueChanged.RemoveAllListeners();

                var newScrollbar = Object.Instantiate(__instance.m_recipeListScroll, scrollRectGO.transform);
                newScrollbar.size = 0.4f;
                scrollRect.onValueChanged.AddListener((_) => newScrollbar.size = 0.4f); 
                scrollRect.verticalScrollbar = newScrollbar;
            }

            if (__instance.InCraftTab() 
                && __instance.m_selectedRecipe.Key != null 
                && __instance.m_selectedRecipe.Key.m_item.m_itemData.IsMagicCraftingMaterial())
            {
                var localizedName = Localization.instance.Localize(__instance.m_selectedRecipe.Key.m_item.m_itemData.GetDecoratedName());
                if (__instance.m_selectedRecipe.Key.m_amount > 1)
                {
                    localizedName = localizedName + " x" + __instance.m_selectedRecipe.Key.m_amount;
                }
                __instance.m_recipeName.text = localizedName;

                __instance.m_recipeIcon.sprite = __instance.m_selectedRecipe.Key.m_item.m_itemData.GetIcon();
                __instance.m_variantButton.gameObject.SetActive(false);
            }

            if (__instance.InCraftTab() || __instance.InUpradeTab())
            {
                Image bgImage = null;

                var magicItemBG = __instance.m_recipeIcon.transform.parent.Find("MagicItemBG");
                if (magicItemBG != null)
                {
                    bgImage = magicItemBG.GetComponent<Image>();
                }
                else
                {
                    bgImage = Object.Instantiate(__instance.m_recipeIcon, __instance.m_recipeIcon.transform.parent, true);
                    bgImage.name = "MagicItemBG";
                    bgImage.transform.SetSiblingIndex(__instance.m_recipeIcon.transform.GetSiblingIndex());
                    bgImage.sprite = EpicLoot.GetMagicItemBgSprite();
                }

                var item = __instance.InCraftTab() ? __instance.m_selectedRecipe.Key?.m_item?.m_itemData : __instance.m_selectedRecipe.Value;
                if (item != null && item.UseMagicBackground())
                {
                    bgImage.enabled = item.UseMagicBackground();
                    bgImage.color = item.GetRarityColor();

                    __instance.m_recipeName.text = Localization.instance.Localize(item.GetDecoratedName());
                }
                else
                {
                    bgImage.enabled = false;
                }
            }

            if (__instance.InUpradeTab() && __instance.m_selectedRecipe.Value != null)
            {
                var newQuality = Mathf.Min(__instance.m_selectedRecipe.Value.m_quality + 1, __instance.m_selectedRecipe.Value.m_shared.m_maxQuality);
                var tooltip = ItemDrop.ItemData.GetTooltip(__instance.m_selectedRecipe.Value, newQuality, true);
                __instance.m_recipeDecription.text = Localization.instance.Localize(tooltip);
            }
        }
    }

    //SetupRequirement
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    public static class InventoryGui_SetupRequirement_Patch
    {
        public static void Postfix(Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality)
        {
            var instance = InventoryGui.instance;
            if ((instance.InCraftTab() || instance.InUpradeTab()) && req?.m_resItem != null)
            {
                var item = req.m_resItem;
                var icon = elementRoot.transform.Find("res_icon").GetComponent<Image>();

                var bgIconTransform = elementRoot.Find("bgIcon");
                if (item.m_itemData.UseMagicBackground())
                {
                    if (bgIconTransform == null)
                    {
                        bgIconTransform = Object.Instantiate(icon, elementRoot, true).transform;
                        bgIconTransform.name = "bgIcon";
                        bgIconTransform.SetSiblingIndex(icon.transform.GetSiblingIndex());
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
            }
        }
    }

    //HideRequirement
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.HideRequirement))]
    public static class InventoryGui_HideRequirement_Patch
    {
        public static void Postfix(Transform elementRoot)
        {
            var bgIconTransform = elementRoot.Find("bgIcon");
            if (bgIconTransform != null)
            {
                bgIconTransform.gameObject.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.AddRecipeToList))]
    public static class InventoryGui_AddRecipeToList_Patch
    {
        public static void Postfix(InventoryGui __instance, Player player, Recipe recipe, ItemDrop.ItemData item, bool canCraft)
        {
            var selectedCraftingItem = __instance.InCraftTab() && recipe.m_item != null;
            var selectedUpgradeItem = __instance.InUpradeTab() && item != null;
            if (selectedCraftingItem || selectedUpgradeItem)
            {
                var thisItem = selectedCraftingItem ? recipe.m_item.m_itemData : item;
                if (thisItem.UseMagicBackground())
                {
                    var element = __instance.m_recipeList.LastOrDefault();
                    if (element != null)
                    {
                        var image = element.transform.Find("icon").GetComponent<Image>();
                        var bgImage = Object.Instantiate(image, image.transform.parent, true);
                        bgImage.name = "MagicItemBG";
                        bgImage.transform.SetSiblingIndex(image.transform.GetSiblingIndex());
                        bgImage.sprite = EpicLoot.GetMagicItemBgSprite();
                        bgImage.color = thisItem.GetRarityColor();
                        if (!canCraft)
                        {
                            bgImage.color -= new Color(0, 0, 0, 0.66f);
                        }

                        var nameText = element.transform.Find("name").GetComponent<Text>();
                        nameText.color = canCraft ? thisItem.GetRarityColor() : new Color(0.66f, 0.66f, 0.66f, 1f);
                    }
                }
            }
        }
    }
}
