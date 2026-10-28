using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

[HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui), typeof(Player), typeof(ItemDrop.ItemData))]
public static class InventoryGrid_UpdateGui_MagicItemComponent_Patch
{
    public static void UpdateGuiElements(InventoryElement element, bool used)
    {
        element.m_used = used;
        Transform magicItemTransform = element.gameObject.transform.Find("magicItem");
        if (magicItemTransform != null)
        {
            Image magicItem = magicItemTransform.GetComponent<Image>();
            if (magicItem != null)
            {
                magicItem.enabled = false;
            }
        }

        Transform setItemTransform = element.gameObject.transform.Find("setItem");
        if (setItemTransform != null)
        {
            Image setItem = setItemTransform.GetComponent<Image>();
            if (setItem != null)
            {
                setItem.enabled = false;
            }
        }
    }

    public static void UpdateGuiItems(ItemDrop.ItemData itemData, InventoryElement element)
    {
        API.ApplyMagicItemBackground(element.gameObject, element.m_equiped.gameObject, itemData, true);
    }

    // Both sites are found first and the method is only changed when both are there: one without the other either
    // hides every magic background or leaves them on emptied cells. Nothing throws, so a reshaped method costs the
    // backgrounds and nothing else.
    [UsedImplicitly]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> instrs = instructions.ToList();

        System.Reflection.FieldInfo elementUsedField =
            AccessTools.DeclaredField(typeof(InventoryElement), nameof(InventoryElement.m_used));
        System.Reflection.FieldInfo elementQueuedField =
            AccessTools.DeclaredField(typeof(InventoryElement), nameof(InventoryElement.m_queued));
        System.Reflection.FieldInfo itemEquippedField =
            AccessTools.DeclaredField(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_equipped));

        // Vanilla clears every cell at the top of UpdateGui:
        //     foreach (InventoryElement element in m_elements) { element.m_used = false; ... }
        // The Sept 2026 update added a second store (m_canBeDroppedOn = false) to that loop body,
        // which made the compiler emit a `dup` between `get_Current` and the m_used write. The
        // element-producing pair is therefore no longer at a fixed offset, so walk back over the
        // optional `dup` rather than hard-coding i-2/i-3 as before. Ldc_I4_0 is what separates this
        // clearing store from the `m_used = true` one later in the method.
        bool IsElementClearStore(int i)
        {
            if (i < 4 || instrs[i].opcode != OpCodes.Stfld || !elementUsedField.Equals(instrs[i].operand))
            {
                return false;
            }

            if (instrs[i - 1].opcode != OpCodes.Ldc_I4_0)
            {
                return false;
            }

            int j = instrs[i - 2].opcode == OpCodes.Dup ? i - 3 : i - 2;
            return instrs[j].opcode == OpCodes.Call && instrs[j - 1].opcode == OpCodes.Ldloca_S;
        }

        // `element.m_queued.enabled = (bool)player && ...` loads the cell but not the item. The item is the local
        // the line above reads m_equipped from (`element.m_equiped.enabled = (bool)player && item.m_equipped`), so
        // it is looked up by that field and must be an ItemData local, rather than taken from a fixed offset.
        bool IsItemSite(int i, out object itemLocal)
        {
            itemLocal = null;
            if (i < 2 || i + 3 >= instrs.Count || instrs[i].opcode != OpCodes.Ldloc_S ||
                instrs[i + 1].opcode != OpCodes.Ldfld || !elementQueuedField.Equals(instrs[i + 1].operand) ||
                instrs[i + 2].opcode != OpCodes.Ldarg_1 || instrs[i + 3].opcode != OpCodes.Call)
            {
                return false;
            }

            for (int k = i - 1; k >= 1 && k >= i - 8; k--)
            {
                if (instrs[k].opcode == OpCodes.Ldfld && itemEquippedField.Equals(instrs[k].operand))
                {
                    if (instrs[k - 1].opcode == OpCodes.Ldloc_S && instrs[k - 1].operand is LocalBuilder local &&
                        local.LocalType == typeof(ItemDrop.ItemData))
                    {
                        itemLocal = local;
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        HashSet<int> clearStores = new HashSet<int>();
        Dictionary<int, object> itemSites = new Dictionary<int, object>();
        for (int i = 0; i < instrs.Count; ++i)
        {
            if (IsElementClearStore(i))
            {
                clearStores.Add(i);
            }
            else if (IsItemSite(i, out object itemLocal))
            {
                itemSites.Add(i, itemLocal);
            }
        }

        if (clearStores.Count == 0 || itemSites.Count == 0)
        {
            EpicLoot.LogWarningForce("Could not find where InventoryGrid.UpdateGui " +
                (clearStores.Count == 0 ? "clears its cells" : "draws each item") +
                ", so magic items in the inventory will not show their rarity background. " +
                "Another mod may have changed that method.");
            return instrs;
        }

        List<CodeInstruction> patched = new List<CodeInstruction>(instrs.Count + 3 * itemSites.Count);
        for (int i = 0; i < instrs.Count; ++i)
        {
            // Replacing the stfld with the call is stack-neutral - both consume (element, bool) and
            // return nothing - so the duplicate the `dup` left for m_canBeDroppedOn survives intact.
            if (clearStores.Contains(i))
            {
                CodeInstruction callInstruction = new CodeInstruction(OpCodes.Call,
                    AccessTools.DeclaredMethod(typeof(InventoryGrid_UpdateGui_MagicItemComponent_Patch), nameof(UpdateGuiElements)));
                instrs[i].MoveLabelsTo(callInstruction);
                patched.Add(callInstruction);
                continue;
            }

            if (itemSites.TryGetValue(i, out object itemLocal))
            {
                // UpdateGuiItems(item, element), ahead of the m_queued line; that line's labels move to its start.
                CodeInstruction loadItem = new CodeInstruction(OpCodes.Ldloc_S, itemLocal);
                instrs[i].MoveLabelsTo(loadItem);
                patched.Add(loadItem);
                patched.Add(new CodeInstruction(OpCodes.Ldloc_S, instrs[i].operand));
                patched.Add(new CodeInstruction(OpCodes.Call,
                    AccessTools.DeclaredMethod(typeof(InventoryGrid_UpdateGui_MagicItemComponent_Patch), nameof(UpdateGuiItems))));
            }

            patched.Add(instrs[i]);
        }

        return patched;
    }
}

[HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons), typeof(Player))]
public static class HotkeyBar_UpdateIcons_Patch
{
    public static void UpdateElements(HotkeyBar.ElementData element, bool used)
    {
        element.m_used = used;
        Transform magicItemTransform = element.m_go.transform.Find("magicItem");
        if (magicItemTransform != null)
        {
            Image magicItem = magicItemTransform.GetComponent<Image>();
            if (magicItem != null)
            {
                magicItem.enabled = false;
            }
        }
    }

    public static void UpdateIcons(HotkeyBar.ElementData element, ItemDrop.ItemData itemData)
    {
        API.ApplyMagicItemBackground(element.m_go, element.m_equiped, itemData, false);
    }

    // As the inventory grid's: both sites are found first, and the method is only changed when both are there.
    [UsedImplicitly]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> instrs = instructions.ToList();

        System.Reflection.FieldInfo elementDataEquipedField =
            AccessTools.DeclaredField(typeof(HotkeyBar.ElementData), nameof(HotkeyBar.ElementData.m_equiped));
        System.Reflection.FieldInfo itemDataEquipedField =
            AccessTools.DeclaredField(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_equipped));
        System.Reflection.MethodInfo setActiveMethod =
            AccessTools.DeclaredMethod(typeof(GameObject), nameof(GameObject.SetActive));
        System.Reflection.FieldInfo elementUsedField =
            AccessTools.DeclaredField(typeof(HotkeyBar.ElementData), nameof(HotkeyBar.ElementData.m_used));

        // `foreach (ElementData element in m_elements) element.m_used = false;`
        bool IsElementClearStore(int i)
        {
            return i >= 3 && instrs[i].opcode == OpCodes.Stfld && elementUsedField.Equals(instrs[i].operand) &&
                instrs[i - 1].opcode == OpCodes.Ldc_I4_0 &&
                instrs[i - 2].opcode == OpCodes.Call &&
                instrs[i - 3].opcode == OpCodes.Ldloca_S;
        }

        // `element.m_equiped.SetActive(item.m_equipped);` -- the element and item locals are the two it loads.
        bool IsEquippedSite(int i)
        {
            return i >= 4 && instrs[i].opcode == OpCodes.Callvirt && setActiveMethod.Equals(instrs[i].operand) &&
                instrs[i - 1].opcode == OpCodes.Ldfld && itemDataEquipedField.Equals(instrs[i - 1].operand) &&
                instrs[i - 2].opcode == OpCodes.Ldloc_S &&
                instrs[i - 3].opcode == OpCodes.Ldfld && elementDataEquipedField.Equals(instrs[i - 3].operand) &&
                instrs[i - 4].opcode == OpCodes.Ldloc_S;
        }

        HashSet<int> clearStores = new HashSet<int>();
        HashSet<int> equippedSites = new HashSet<int>();
        for (int i = 0; i < instrs.Count; ++i)
        {
            if (IsElementClearStore(i))
            {
                clearStores.Add(i);
            }
            else if (IsEquippedSite(i))
            {
                equippedSites.Add(i);
            }
        }

        if (clearStores.Count == 0 || equippedSites.Count == 0)
        {
            EpicLoot.LogWarningForce("Could not find where HotkeyBar.UpdateIcons " +
                (clearStores.Count == 0 ? "clears its slots" : "marks equipped items") +
                ", so magic items on the hotbar will not show their rarity background. " +
                "Another mod may have changed that method.");
            return instrs;
        }

        List<CodeInstruction> patched = new List<CodeInstruction>(instrs.Count + 3 * equippedSites.Count);
        for (int i = 0; i < instrs.Count; ++i)
        {
            // Stack-neutral like the inventory grid's: the call consumes the (element, bool) the stfld did.
            if (clearStores.Contains(i))
            {
                CodeInstruction callInstruction = new CodeInstruction(OpCodes.Call,
                    AccessTools.DeclaredMethod(typeof(HotkeyBar_UpdateIcons_Patch), nameof(UpdateElements)));
                instrs[i].MoveLabelsTo(callInstruction);
                patched.Add(callInstruction);
                continue;
            }

            patched.Add(instrs[i]);

            if (equippedSites.Contains(i))
            {
                // UpdateIcons(element, item), right after the SetActive.
                patched.Add(new CodeInstruction(OpCodes.Ldloc_S, instrs[i - 4].operand));
                patched.Add(new CodeInstruction(OpCodes.Ldloc_S, instrs[i - 2].operand));
                patched.Add(new CodeInstruction(OpCodes.Call,
                    AccessTools.DeclaredMethod(typeof(HotkeyBar_UpdateIcons_Patch), nameof(UpdateIcons))));
            }
        }

        return patched;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.GetActionProgress),
    new Type[] { typeof(string), typeof(float) }, new ArgumentType[] { ArgumentType.Out, ArgumentType.Out })]
public static class Player_GetActionProgress_Patch
{
    public static void Postfix(Player __instance, ref string name)
    {
        if (__instance.m_actionQueue.Count > 0)
        {
            Player.MinorActionData equip = __instance.m_actionQueue[0];
            if (equip.m_type != Player.MinorActionData.ActionType.Reload)
            {
                if (equip.m_duration > 0.5f)
                {
                    name = equip.m_type == Player.MinorActionData.ActionType.Unequip ?
                        "$hud_unequipping " + equip.m_item.GetDecoratedName() :
                        "$hud_equipping " + equip.m_item.GetDecoratedName();
                }
            }
        }
    }
}

[HarmonyPatch(typeof(ItemDrop))]
public static class ItemDrop_Patches
{
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
    [HarmonyPrefix]
    public static bool GetHoverText_Prefix(ItemDrop __instance, ref string __result)
    {
        string str = __instance.m_itemData.GetDecoratedName();
        if (__instance.m_itemData.m_quality > 1)
        {
            str = $"{str}[{__instance.m_itemData.m_quality}] ";
        }
        else if (__instance.m_itemData.m_stack > 1)
        {
            str = $"{str} x{__instance.m_itemData.m_stack}";
        }
        __result = Localization.instance.Localize($"{str}\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup");
        return false;
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverName))]
    [HarmonyPrefix]
    public static bool GetHoverName_Prefix(ItemDrop __instance, ref string __result)
    {
        __result = __instance.m_itemData.GetDecoratedName();
        return false;
    }
}