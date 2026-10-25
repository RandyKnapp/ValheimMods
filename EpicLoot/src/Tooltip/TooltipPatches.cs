using System;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot;

/// <summary>
/// Where Epic Loot's tooltip meets vanilla's. ItemData.GetTooltip draws any item's text (the whole tooltip of an
/// item with magic data, Epic Loot's additions on the rest); InventoryGrid.CreateItemTooltip adds the title, the
/// key hints and the Ctrl comparison. While another mod overrides the inventory tooltip, CreateItemTooltip runs
/// as vanilla and the GetTooltip calls inside it get vanilla's text; GetTooltip everywhere else is unaffected.
/// </summary>
public static class TooltipPatches
{
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip),
        typeof(ItemDrop.ItemData), typeof(UITooltip))]
    public static class InventoryGrid_CreateItemTooltip_Patch
    {
        [UsedImplicitly]
        [HarmonyAfter(new[] { "kg.ValheimEnchantmentSystem" })]
        public static bool Prefix(ItemDrop.ItemData item, UITooltip tooltip, out bool __state)
        {
            // Overridden: vanilla's tooltip, so the overriding mod gets vanilla's text wherever it reads it.
            __state = ItemTooltip.IsOverridden;
            if (__state)
            {
                TooltipBoxPatches.ClearComparison();
                ItemTooltip.EnterWithoutEpicLoot();
                return true;
            }

            // ZInput.GetKey before HasEquipmentOfType: this prefix runs every frame the cursor rests on a
            // slot, and HasEquipmentOfType walks the player's equipment through GetMagicEquipment, which
            // allocates two lists and runs the registered equipment providers. Only the comparison
            // tooltip needs that answer, and only while Ctrl is actually held.
            if (item.IsEquipable() && !item.m_equipped && Player.m_localPlayer != null &&
                ZInput.GetKey(KeyCode.LeftControl) && Player.m_localPlayer.HasEquipmentOfType(item.m_shared.m_itemType))
            {
                // Shown side by side with the hovered item's tooltip.
                ItemDrop.ItemData otherItem = Player.m_localPlayer.GetEquipmentOfType(item.m_shared.m_itemType);
                TooltipBoxPatches.SetComparison($"<color=#AAA><i>$mod_epicloot_currentlyequipped:</i></color>" +
                    otherItem.GetDecoratedName(), otherItem.GetTooltip());
            }
            else
            {
                TooltipBoxPatches.ClearComparison();
            }

            string text = ItemTooltip.GetInventoryText(item, out string title);
            tooltip.Set(title, text);
            return false;
        }

        [UsedImplicitly]
        public static void Finalizer(bool __state)
        {
            if (__state)
            {
                ItemTooltip.ExitWithoutEpicLoot();
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
    public static class ItemData_GetTooltip_Patch
    {
        // The data is gathered here for every item, magic or not, and handed to the postfix: the postfix adds
        // the material line and the set section of items Epic Loot does not draw itself, crafting previews
        // included.
        [UsedImplicitly]
        private static bool Prefix(ref string __result, ItemDrop.ItemData item, int qualityLevel, bool crafting,
            out ItemTooltipData __state)
        {
            __state = null;
            if (!ItemTooltip.DrawsText || item == null)
            {
                return true;
            }

            // A gather that fails is caught, and cached as a miss, in GetData; only the drawing is guarded here.
            __state = ItemTooltip.GetData(item, qualityLevel, MagicItem.ShowEffectDetails);
            if (ItemTooltip.Disabled || crafting || __state == null || !__state.IsMagic)
            {
                return true;
            }

            try
            {
                __result = TooltipRenderer.RenderMagicItem(item, __state);
                return false;
            }
            catch (Exception e)
            {
                // Vanilla's text rather than an exception out of InventoryGui.Update every frame. The renderer
                // draws whatever an editor shaped, so a bad edit lands here.
                ItemTooltip.LogFailure(item, e);
                __state = null;
                return true;
            }
        }

        [UsedImplicitly]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(ref string __result, ItemDrop.ItemData item, ItemTooltipData __state)
        {
            if (!ItemTooltip.DrawsText || item == null || __result == null)
            {
                return;
            }

            try
            {
                __result = TooltipRenderer.Finish(item, __result, __state);
            }
            catch (Exception e)
            {
                ItemTooltip.LogFailure(item, e);
            }
        }
    }
}
