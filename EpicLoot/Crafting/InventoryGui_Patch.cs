using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Crafting
{
    [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
    public static class InventoryGui_UpdateRecipe_Patch
    {
        public static void Postfix(InventoryGui __instance)
        {
            if (__instance.InCraftTab() && __instance.m_selectedRecipe.Key != null 
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

            if (__instance.InUpradeTab() && __instance.m_selectedRecipe.Value != null)
            {
                var item = __instance.m_selectedRecipe.Value;

                Image bgImage = null;

                var magicItemBG = __instance.m_recipeIcon.transform.parent.Find("MagicItemBG");
                if (magicItemBG != null)
                {
                    bgImage = magicItemBG.GetComponent<Image>();
                    bgImage.color = item.GetRarityColor();
                }
                else
                {
                    bgImage = Object.Instantiate(__instance.m_recipeIcon, __instance.m_recipeIcon.transform.parent, true);
                    bgImage.name = "MagicItemBG";
                    bgImage.transform.SetSiblingIndex(__instance.m_recipeIcon.transform.GetSiblingIndex());
                    bgImage.sprite = EpicLoot.GetMagicItemBgSprite();
                    bgImage.color = item.GetRarityColor();
                }

                bgImage.enabled = item.UseMagicBackground();
            }
            else if (__instance.InCraftTab())
            {
                var magicItemBG = __instance.m_recipeIcon.transform.parent.Find("MagicItemBG");
                if (magicItemBG != null)
                {
                    magicItemBG.GetComponent<Image>().enabled = false;
                }
            }
        }
    }

    //SetupRequirementList
    [HarmonyPatch(typeof(InventoryGui), "SetupRequirementList")]
    public static class InventoryGui_SetupRequirementList_Patch
    {
        public static void Postfix(InventoryGui __instance)
        {
            if (__instance.InCraftTab() || __instance.InUpradeTab())
            {
                foreach (var requirementObject in __instance.m_recipeRequirementList)
                {
                    var bgIconTransform = requirementObject.transform.Find("bgIcon");
                    if (bgIconTransform != null)
                    {
                        bgIconTransform.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), "AddRecipeToList")]
    public static class InventoryGui_AddRecipeToList_Patch
    {
        public static void Postfix(InventoryGui __instance, Player player, Recipe recipe, ItemDrop.ItemData item, bool canCraft)
        {
            if (__instance.InUpradeTab() && item != null)
            {
                if (item.UseMagicBackground())
                {
                    var element = __instance.m_recipeList.LastOrDefault();
                    if (element != null)
                    {
                        var image = element.transform.Find("icon").GetComponent<Image>();
                        var bgImage = Object.Instantiate(image, image.transform.parent, true);
                        bgImage.name = "MagicItemBG";
                        bgImage.transform.SetSiblingIndex(image.transform.GetSiblingIndex());
                        bgImage.sprite = EpicLoot.Assets.GenericItemBgSprite;
                        bgImage.color = item.GetRarityColor();

                        var nameText = element.transform.Find("name").GetComponent<Text>();
                        nameText.text = Localization.instance.Localize(item.GetDecoratedName());
                    }
                }
            }
        }
    }
}
