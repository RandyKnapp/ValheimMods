using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        }
    }
}
