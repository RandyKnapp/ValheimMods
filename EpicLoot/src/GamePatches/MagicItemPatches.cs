using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
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

    [UsedImplicitly]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool successPatch1 = false;
        bool successPatch2 = false;
        List<CodeInstruction> instrs = instructions.ToList();

        int counter = 0;

        CodeInstruction LogMessage(CodeInstruction instruction)
        {
            //EpicLoot.Log($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
            return instruction;
        }

        System.Reflection.FieldInfo elementUsedField =
            AccessTools.DeclaredField(typeof(InventoryElement), nameof(InventoryElement.m_used));
        System.Reflection.FieldInfo elementQueuedField =
            AccessTools.DeclaredField(typeof(InventoryElement), nameof(InventoryElement.m_queued));

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

        for (int i = 0; i < instrs.Count; ++i)
        {
            // Replacing the stfld with the call is stack-neutral - both consume (element, bool) and
            // return nothing - so the duplicate the `dup` left for m_canBeDroppedOn survives intact.
            if (IsElementClearStore(i))
            {
                //Element Spot
                CodeInstruction callInstruction = new CodeInstruction(OpCodes.Call,
                    AccessTools.DeclaredMethod(typeof(InventoryGrid_UpdateGui_MagicItemComponent_Patch), nameof(UpdateGuiElements)));
                //Move Any Labels from the instruction position being patched to new instruction.
                if (instrs[i].labels.Count > 0)
                {
                    instrs[i].MoveLabelsTo(callInstruction);
                }

                //Get Element variable
                yield return LogMessage(callInstruction);
                counter++;

                //Skip Stfld Instruction
                i++;

                successPatch1 = true;

            }
            else if (i > 6 && instrs[i].opcode == OpCodes.Ldloc_S &&
                instrs[i + 1].opcode == OpCodes.Ldfld &&
                instrs[i + 1].operand.Equals(elementQueuedField) &&
                instrs[i + 2].opcode == OpCodes.Ldarg_1 &&
                instrs[i + 3].opcode == OpCodes.Call)
            {
                //Item Spot
                object elementOperand = instrs[i].operand;
                object itemDataOperand = instrs[i - 5].operand;

                CodeInstruction ldLocsInstruction = new CodeInstruction(OpCodes.Ldloc_S, itemDataOperand);
                //Move Any Labels from the instruction position being patched to new instruction.
                if (instrs[i].labels.Count > 0)
                {
                    instrs[i].MoveLabelsTo(ldLocsInstruction);
                }

                //Get Item variable
                yield return LogMessage(ldLocsInstruction);
                counter++;

                //Get Element variable
                yield return LogMessage(new CodeInstruction(OpCodes.Ldloc_S, elementOperand));
                counter++;

                //Patch Calling Method
                yield return LogMessage(new CodeInstruction(OpCodes.Call,
                    AccessTools.DeclaredMethod(typeof(InventoryGrid_UpdateGui_MagicItemComponent_Patch), nameof(UpdateGuiItems))));
                counter++;
                successPatch2 = true;
            }

            yield return LogMessage(instrs[i]);
            counter++;
        }

        if (!successPatch2 || !successPatch1)
        {
            EpicLoot.LogError($"InventoryGrid.UpdateGui Transpiler Failed To Patch");
            EpicLoot.LogError($"!successPatch1: {!successPatch1}");
            EpicLoot.LogError($"!successPatch2: {!successPatch2}");
            Thread.Sleep(5000);
        }
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

    [UsedImplicitly]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> instrs = instructions.ToList();
        bool successPatch1 = false;
        bool successPatch2 = false;

        int counter = 0;

        CodeInstruction LogMessage(CodeInstruction instruction)
        {
            //EpicLoot.Log($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
            return instruction;
        }

        System.Reflection.FieldInfo elementDataEquipedField =
            AccessTools.DeclaredField(typeof(HotkeyBar.ElementData), nameof(HotkeyBar.ElementData.m_equiped));
        System.Reflection.FieldInfo itemDataEquipedField =
            AccessTools.DeclaredField(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_equipped));
        System.Reflection.MethodInfo setActiveMethod =
            AccessTools.DeclaredMethod(typeof(GameObject), nameof(GameObject.SetActive));
        System.Reflection.FieldInfo elementUsedField =
            AccessTools.DeclaredField(typeof(HotkeyBar.ElementData), nameof(HotkeyBar.ElementData.m_used));

        for (int i = 0; i < instrs.Count; ++i)
        {
            if (i > 6 && instrs[i].opcode == OpCodes.Stfld &&
                instrs[i].operand.Equals(elementUsedField) &&
                instrs[i - 1].opcode == OpCodes.Ldc_I4_0 &&
                instrs[i - 2].opcode == OpCodes.Call &&
                instrs[i - 3].opcode == OpCodes.Ldloca_S)
            {
                //Element Spot
                CodeInstruction callInstruction = new CodeInstruction(OpCodes.Call,
                    AccessTools.DeclaredMethod(typeof(HotkeyBar_UpdateIcons_Patch), nameof(UpdateElements)));
                //Move Any Labels from the instruction position being patched to new instruction.
                if (instrs[i].labels.Count > 0)
                {
                    instrs[i].MoveLabelsTo(callInstruction);
                }

                //Get Element variable
                yield return LogMessage(callInstruction);
                counter++;

                //Skip Stfld Instruction
                i++;
                successPatch1 = true;
            }

            yield return LogMessage(instrs[i]);
            counter++;

            if (i > 6 && instrs[i].opcode == OpCodes.Callvirt &&
                instrs[i].operand.Equals(setActiveMethod) &&
                instrs[i - 1].opcode == OpCodes.Ldfld &&
                instrs[i - 1].operand.Equals(itemDataEquipedField) &&
                instrs[i - 2].opcode == OpCodes.Ldloc_S &&
                instrs[i - 3].opcode == OpCodes.Ldfld &&
                instrs[i - 3].operand.Equals(elementDataEquipedField) &&
                instrs[i - 4].opcode == OpCodes.Ldloc_S)
            {
                object elementOperand = instrs[i - 4].operand;
                object itemDataOperand = instrs[i - 2].operand;

                CodeInstruction ldLocInstruction = new CodeInstruction(OpCodes.Ldloc_S, elementOperand);
                //Move Any Labels from the instruction position being patched to new instruction.
                if (instrs[i].labels.Count > 0)
                    instrs[i].MoveLabelsTo(ldLocInstruction);

                //Get Element
                yield return LogMessage(ldLocInstruction);
                counter++;

                //Get Item Data
                yield return LogMessage(new CodeInstruction(OpCodes.Ldloc_S, itemDataOperand));
                counter++;

                //Patch Calling Method
                yield return LogMessage(new CodeInstruction(OpCodes.Call,
                    AccessTools.DeclaredMethod(typeof(HotkeyBar_UpdateIcons_Patch), nameof(UpdateIcons))));
                counter++;
                successPatch2 = true;
            }
        }

        if (!successPatch2 || !successPatch1)
        {
            EpicLoot.LogError($"HotkeyBar.UpdateIcons Transpiler Failed To Patch");
            EpicLoot.LogError($"!successPatch1: {!successPatch1}");
            EpicLoot.LogError($"!successPatch2: {!successPatch2}");
            Thread.Sleep(5000);
        }
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