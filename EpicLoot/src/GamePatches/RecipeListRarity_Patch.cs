using EpicLoot.Config;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot;

/// <summary>
/// Shows rarity behind item icons in the crafting panel. Vanilla draws the recipe list and the detail
/// panel itself, so neither is covered by the inventory grid transpilers in
/// <see cref="MagicItemPatches"/>. Only the Upgrade tab can ever show a background: Craft tab rows are
/// recipes rather than owned items, so they pass a null item and cost nothing.
/// </summary>
public static class RecipeListRarity_Patch
{
    // Read fresh rather than cached, so toggling the option applies without a restart.
    private static bool Enabled => ELConfig.ShowRarityInRecipeList.Value;

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.AddRecipeToList))]
    public static class InventoryGui_AddRecipeToList_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance, ItemDrop.ItemData item)
        {
            if (!Enabled || __instance.m_availableRecipes.Count == 0)
            {
                return;
            }

            // The row is a local in the patched method; it is appended to m_availableRecipes as the last
            // statement, so by postfix time the entry we want is the final one.
            GameObject row = __instance.m_availableRecipes[__instance.m_availableRecipes.Count - 1].InterfaceElement;
            if (row == null)
            {
                return;
            }

            Transform icon = row.transform.Find("icon");
            if (icon != null)
            {
                API.ApplyMagicItemBackgroundToIcon(icon.gameObject, item);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
    public static class InventoryGui_UpdateRecipe_Patch
    {
        [UsedImplicitly]
        private static void Postfix(InventoryGui __instance)
        {
            if (!Enabled || __instance.m_recipeIcon == null)
            {
                return;
            }

            API.ApplyMagicItemBackgroundToIcon(__instance.m_recipeIcon.gameObject,
                __instance.m_selectedRecipe.ItemData);
        }
    }
}
