using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Crafting
{
    public static class GameObjectExtensions
    {
        public static RectTransform RectTransform(this GameObject go)
        {
            return go.transform as RectTransform;
        }
    }

    public class DisenchantRecipe
    {
        
    }

    public static class Disenchant_Patches
    {
        public static Button TabDisenchant;
        public static List<DisenchantRecipe> Recipes;
        public static int SelectedRecipe;

        [HarmonyPatch(typeof(InventoryGui), "OnDestroy")]
        public static class InventoryGui_OnDestroy_Patch
        {
            public static void Postfix()
            {
                TabDisenchant = null;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateCraftingPanel")]
        public static class InventoryGui_UpdateCraftingPanel_Patch
        {
            public static bool Prefix(InventoryGui __instance, bool focusView)
            {
                var player = Player.m_localPlayer;
                var station = player.GetCurrentCraftingStation();
                if (station == null)
                {
                    return true;
                }

                if (TabDisenchant == null)
                {
                    var go = Object.Instantiate(__instance.m_tabUpgrade, __instance.m_tabUpgrade.transform.parent, true).gameObject;
                    go.name = "Disenchant";
                    go.GetComponentInChildren<Text>().text = "SACRIFICE";
                    go.transform.SetSiblingIndex(__instance.m_tabUpgrade.transform.GetSiblingIndex() + 1);
                    go.RectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100);
                    TabDisenchant = go.GetComponent<Button>();
                    TabDisenchant.gameObject.RectTransform().anchoredPosition = TabDisenchant.gameObject.RectTransform().anchoredPosition + new Vector2(107, 0);
                    TabDisenchant.onClick.RemoveAllListeners();
                    TabDisenchant.onClick.AddListener(TabDisenchant.GetComponent<ButtonSfx>().OnClick);
                    TabDisenchant.onClick.AddListener(OnDisenchatTabPressed);
                }

                TabDisenchant.gameObject.SetActive(station.m_name == "$piece_artisanstation");
                if (station.m_name != "$piece_artisanstation")
                {
                    return true;
                }

                if (__instance.InCraftTab() || __instance.InUpradeTab())
                {
                    return true;
                }

                var available = new List<Recipe>();
                //localPlayer.GetAvailableRecipes(ref available);
                //this.UpdateRecipeList(available);
                {
                    __instance.m_availableRecipes.Clear();
                    foreach (var recipe in __instance.m_recipeList)
                    {
                        Object.Destroy(recipe);
                    }
                    __instance.m_recipeList.Clear();
                }
                /*if (this.m_availableRecipes.Count > 0)
                {
                  if ((Object) this.m_selectedRecipe.Key != (Object) null)
                    this.SetRecipe(this.GetSelectedRecipeIndex(), focusView);
                  else
                    this.SetRecipe(0, focusView);
                }
                else*/
                __instance.SetRecipe(-1, focusView);

                return false;
            }
        }

        //public void OnTabCraftPressed()
        [HarmonyPatch(typeof(InventoryGui), "OnTabCraftPressed")]
        public static class InventoryGui_OnTabCraftPressed_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                TabDisenchant.interactable = true;
            }
        }

        //public void OnTabUpgradePressed()
        [HarmonyPatch(typeof(InventoryGui), "OnTabUpgradePressed")]
        public static class InventoryGui_OnTabUpgradePressed_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                TabDisenchant.interactable = true;
            }
        }

        //this.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(this.m_recipeListBaseSize, (float) this.m_recipeList.Count * this.m_recipeListSpace));
        //public void UpdateRecipeList(List<Recipe> recipes)
        [HarmonyPatch(typeof(InventoryGui), "UpdateRecipeList")]
        public static class InventoryGui_UpdateRecipeList_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                if (InDisenchantTab())
                {
                    Player localPlayer = Player.m_localPlayer;
                    __instance.m_availableRecipes.Clear();
                    foreach (var recipe in __instance.m_recipeList)
                    {
                        UnityEngine.Object.Destroy(recipe);
                    }
                    __instance.m_recipeList.Clear();


                    return false;
                }
                return true;
            }
        }

        //public void SetRecipe(int index, bool center)
        [HarmonyPatch(typeof(InventoryGui), "SetRecipe")]
        public static class InventoryGui_SetRecipe_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                if (InDisenchantTab())
                {
                    
                    return false;
                }
                return true;
            }
        }

        private static bool InDisenchantTab()
        {
            return TabDisenchant != null && !TabDisenchant.interactable;
        }

        public static void OnDisenchatTabPressed()
        {
            var instance = InventoryGui.instance;
            instance.m_tabCraft.interactable = true;
            instance.m_tabUpgrade.interactable = true;
            TabDisenchant.interactable = false;

            GenerateDisenchantRecipes();
            instance.UpdateCraftingPanel();
        }

        public static void GenerateDisenchantRecipes()
        {
            Recipes = new List<DisenchantRecipe>();
            if (Player.m_localPlayer != null)
            {
                foreach (var item in Player.m_localPlayer.GetInventory().GetAllItems())
                {
                    if (item.IsMagic())
                    {
                        Recipes.Add(GenerateDisenchantRecipe(item));
                    }
                }
            }
        }

        private static DisenchantRecipe GenerateDisenchantRecipe(ItemDrop.ItemData item)
        {
            return new DisenchantRecipe();
        }
    }
}
