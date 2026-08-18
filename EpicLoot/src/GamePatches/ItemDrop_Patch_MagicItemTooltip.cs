using System;
using System.Text;
using EpicLoot.Crafting;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot;

public static class MagicTooltipPatches
{
    public static bool TooltipDisable = false;

    // Tells the player how to open an item's shard slots. Appended here, on the inventory grid's own
    // tooltip, rather than inside MagicItem.GetTooltip: the enchanting panels, tempering panel, craft
    // dialogs and trader lists all call GetTooltip() directly, and the hint means nothing there.
    private static string GetSocketOpenHint(ItemDrop.ItemData item)
    {
        if (item == null || !item.IsMagic(out MagicItem magicItem) || !magicItem.HasSockets())
        {
            return "";
        }

        // The $KEY_ tokens must sit in this source string, never inside a translation value:
        // Localization.Localize is single-pass and never re-scans what Translate() emitted, so a nested
        // $KEY_Use would reach the screen literally. $KEY_RTrigger / $KEY_ButtonX only resolve to pad
        // glyphs while a gamepad is active (Localization looks up "Joy" + the binding name), which is
        // exactly when this branch is taken.
        var keys = ZInput.IsGamepadActive() ? "$KEY_RTrigger + $KEY_ButtonX" : "$KEY_Use";
        return $"\n[<color=yellow><b>{keys}</b></color>] $mod_epicloot_press_socket";
    }

    // Set the topic of the tooltip with the decorated name
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip),
        typeof(ItemDrop.ItemData), typeof(UITooltip))]
    public static class InventoryGrid_CreateItemTooltip_MagicItemComponent_Patch
    {
        [HarmonyAfter(new[] { "kg.ValheimEnchantmentSystem" })]
        public static bool Prefix(ItemDrop.ItemData item, UITooltip tooltip, out string __state)
        {
            __state = null;
            string tooltipText;
            if (item.IsEquipable() && !item.m_equipped && Player.m_localPlayer != null &&
                Player.m_localPlayer.HasEquipmentOfType(item.m_shared.m_itemType) && ZInput.GetKey(KeyCode.LeftControl))
            {
                ItemDrop.ItemData otherItem = Player.m_localPlayer.GetEquipmentOfType(item.m_shared.m_itemType);
                tooltipText = item.GetTooltip();
                // Set the comparision tooltip to be shown side-by-side with our original tooltip
                PatchOnHoverFix.ComparisonTitleString = $"<color=#AAA><i>$mod_epicloot_currentlyequipped:" +
                    $"</i></color>" + otherItem.GetDecoratedName();
                PatchOnHoverFix.ComparisonTooltipString = otherItem.GetTooltip();
            }
            else
            {
                PatchOnHoverFix.ComparisonTooltipString = "";
                PatchOnHoverFix.ComparisonAdded = false;
                tooltipText = item.GetTooltip();
            }
            tooltip.Set(item.GetDecoratedName(), tooltipText + GetSocketOpenHint(item));
            return false;
        }
    }

    // Set the content of the tooltip
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
        typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int))]
    public static class MagicItemTooltip_ItemDrop_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(ref string __result, ItemDrop.ItemData item, int qualityLevel, bool crafting)
        {
            if (TooltipDisable == true || item == null || crafting)
            {
                return true;
            }

            MagicItem magicItem = item.GetMagicItem();

            if (magicItem == null)
            {
                return true;
            }

            __result = new MagicTooltip(item, magicItem, qualityLevel).GetTooltip();
            return false;
        }

        [UsedImplicitly]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref string __result, ItemDrop.ItemData item)
        {
            if (item == null)
                return;

            if (item.IsMagicCraftingMaterial() || item.IsRunestone())
            {
                string rarityDisplay = EpicLoot.GetRarityDisplayName(item.GetCraftingMaterialRarity());
                __result = $"<color={item.GetCraftingMaterialRarityColor()}>{rarityDisplay} " +
                    $"$mod_epicloot_craftingmaterial\n</color>" + __result;
            }

            if (!item.IsMagic())
            {
                StringBuilder text = new StringBuilder();

                // Set stuff
                if (item.IsSetItem())
                {
                    // Remove old set stuff
                    int index = __result.IndexOf("\n\n$item_seteffect", StringComparison.InvariantCulture);
                    if (index >= 0)
                    {
                        __result = __result.Remove(index);
                    }

                    // Create new
                    text.Append(item.GetSetTooltip());
                }

                __result += text.ToString();
            }

            __result = __result.Replace("<color=orange>", "<color=#add8e6ff>");
            __result = __result.Replace("<color=yellow>", "<color=#add8e6ff>");
            __result = __result.Replace("\n\n\n", "\n\n");
        }
    }
}