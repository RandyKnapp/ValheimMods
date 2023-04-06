using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Common;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EquipmentAndQuickSlots
{
    public static class CustomHotkeyBar
    {
        [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons")]
        public static class HotkeyBar_UpdateIcons_Patch
        {
            public static void UpdateIcons(HotkeyBar instance, Player player, List<ItemDrop.ItemData> m_items)
            {
                if (instance.name != "QuickSlotsHotkeyBar")
                {
                    return;
                }
                
                player.GetQuickSlotInventory().GetBoundItems(m_items);
            }
            public static int UpdateIconCount(int defaultCount, HotkeyBar instance)
            {
                if (instance.name != "QuickSlotsHotkeyBar")
                {
                    return defaultCount;
                }
                
                return EquipmentAndQuickSlots.QuickSlotCount;
            }
            public static void UpdateIconBinding(HotkeyBar instance, int index, HotkeyBar.ElementData elementData)
            {
                if (instance.name != "QuickSlotsHotkeyBar")
                {
                    return;
                }
                
                var bindingText = elementData.m_go.transform.Find("binding").GetComponent<Text>();
                bindingText.enabled = true;
                bindingText.horizontalOverflow = HorizontalWrapMode.Overflow;
                bindingText.text = EquipmentAndQuickSlots.GetBindingLabel(index);
            }
            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instrs = instructions.ToList();

                var counter = 0;

                CodeInstruction LogMessage(CodeInstruction instruction)
                {
                    //EquipmentAndQuickSlots.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                    return instruction;
                }

                var boundItemsMethod = AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.GetBoundItems));
                var itemsListField = AccessTools.DeclaredField(typeof(HotkeyBar), nameof(HotkeyBar.m_items));
                var setTextProperty = AccessTools.PropertySetter(typeof(Text), "text");

                for (int i = 0; i < instrs.Count; ++i)
                {

                    yield return LogMessage(instrs[i]);
                    counter++;
                    
                    if (i > 6 && instrs[i].opcode == OpCodes.Callvirt && instrs[i].operand.Equals(boundItemsMethod))
                    {
                        //Load LdArg0
                        var ldArgInstruction = new CodeInstruction(OpCodes.Ldarg_0);
                        //Move Any Labels from the instruction position being patched to new instruction.
                        if (instrs[i].labels.Count > 0)
                        {
                            instrs[i].MoveLabelsTo(ldArgInstruction);
                        }

                        //LdArg0
                        yield return LogMessage(ldArgInstruction);
                        counter++;
                        
                        //Player
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_1));
                        counter++;
                        
                        //this
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                        counter++;
                        
                        //this.m_items
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldfld, itemsListField));
                        counter++;
                        
                        //Call Method
                        yield return LogMessage(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(HotkeyBar_UpdateIcons_Patch), nameof(UpdateIcons))));
                        counter++;
                    } 
                    else if (i > 6 && instrs[i].opcode == OpCodes.Ldc_I4_0 && instrs[i+1].opcode == OpCodes.Stloc_0)
                    {
                        //this
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                        counter++;
                        
                        //Call Method
                        yield return LogMessage(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(HotkeyBar_UpdateIcons_Patch), nameof(UpdateIconCount))));
                        counter++;
                    }
                    else if (i > 6 && instrs[i].opcode == OpCodes.Callvirt && instrs[i].operand.Equals(setTextProperty) 
                             && instrs[i-1].opcode == OpCodes.Call && instrs[i-9].opcode == OpCodes.Ldstr && instrs[i-9].operand.Equals("binding"))
                    {
                        var indexOperand = instrs[i-6].opcode == OpCodes.Ldloc_S ? instrs[i-6].operand : null;
                        var elementDataOperand = instrs[i+1].opcode == OpCodes.Ldloc_S ? instrs[i+1].operand : null;

                        if (indexOperand != null)
                        {
                            //this
                            yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                            counter++;
                        
                            //index
                            yield return LogMessage(new CodeInstruction(OpCodes.Ldloc_S, indexOperand));
                            counter++;
                        
                            //elementData
                            yield return LogMessage(new CodeInstruction(OpCodes.Ldloc_S, elementDataOperand));
                            counter++;
                        
                            //Call Method
                            yield return LogMessage(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(HotkeyBar_UpdateIcons_Patch), nameof(UpdateIconBinding))));
                            counter++;
                        }
                        else
                        {
                            EquipmentAndQuickSlots.LogError($"Can't Find Index Operand !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Hud), "Awake")]
    public static class Hud_Awake_Patch
    {
        public static void Postfix(Hud __instance)
        {
            var hotkeyBar = __instance.GetComponentInChildren<HotkeyBar>();

            if (EquipmentAndQuickSlots.QuickSlotsEnabled.Value && hotkeyBar.transform.parent.Find("QuickSlotsHotkeyBar") == null)
            {
                var quickslotsHotkeyBar = Object.Instantiate(hotkeyBar.gameObject, __instance.m_rootObject.transform, true);
                quickslotsHotkeyBar.name = "QuickSlotsHotkeyBar";
                quickslotsHotkeyBar.GetComponent<HotkeyBar>().m_selected = -1;

                var configPositionedElement = quickslotsHotkeyBar.AddComponent<ConfigPositionedElement>();
                configPositionedElement.PositionConfig = EquipmentAndQuickSlots.QuickSlotsPosition;
                configPositionedElement.AnchorConfig = EquipmentAndQuickSlots.QuickSlotsAnchor;
                configPositionedElement.EnsureCorrectPosition();
            }
        }
    }

    public static class HotkeyBarController
    {
        public static List<HotkeyBar> HotkeyBars;
        public static int SelectedHotkeyBarIndex = -1;

        [HarmonyPatch(typeof(Hud), "Update")]
        public static class Hud_Update_Patch
        {
            public static void Postfix(Hud __instance)
            {
                var player = Player.m_localPlayer;
                if (HotkeyBars == null)
                {
                    HotkeyBars = __instance.transform.parent.GetComponentsInChildren<HotkeyBar>().ToList();
                }

                if (player != null)
                {
                    if (SelectedHotkeyBarIndex >= 0 && SelectedHotkeyBarIndex < HotkeyBars.Count)
                    {
                        var currentHotKeyBar = HotkeyBars[SelectedHotkeyBarIndex];
                        UpdateHotkeyBarInput(currentHotKeyBar);
                    }
                    else
                    {
                        UpdateInitialHotkeyBarInput();
                    }
                }

                foreach (var hotkeyBar in HotkeyBars)
                {
                    if (hotkeyBar.m_selected > hotkeyBar.m_elements.Count - 1)
                    {
                        hotkeyBar.m_selected = Mathf.Max(0, hotkeyBar.m_elements.Count - 1);
                    }

                    hotkeyBar.UpdateIcons(player);
                }
            }

            private static void UpdateInitialHotkeyBarInput()
            {
                if (ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyDPadRight"))
                {
                    SelectHotkeyBar(0, false);
                }
            }

            public static void UpdateHotkeyBarInput(HotkeyBar hotkeyBar)
            {
                var player = Player.m_localPlayer;
                if (hotkeyBar.m_selected >= 0 && player != null && !InventoryGui.IsVisible() && !Menu.IsVisible() && !GameCamera.InFreeFly())
                {
                    if (ZInput.GetButtonDown("JoyDPadLeft"))
                    {
                        if (hotkeyBar.m_selected == 0)
                        {
                            GotoHotkeyBar(SelectedHotkeyBarIndex - 1);
                        }
                        else
                        {
                            hotkeyBar.m_selected = Mathf.Max(0, hotkeyBar.m_selected - 1);
                        }
                    }
                    else if (ZInput.GetButtonDown("JoyDPadRight"))
                    {
                        if (hotkeyBar.m_selected == hotkeyBar.m_elements.Count - 1)
                        {
                            GotoHotkeyBar(SelectedHotkeyBarIndex + 1);
                        }
                        else
                        {
                            hotkeyBar.m_selected = Mathf.Min(hotkeyBar.m_elements.Count - 1, hotkeyBar.m_selected + 1);
                        }
                    }

                    if (ZInput.GetButtonDown("JoyDPadUp"))
                    {
                        if (hotkeyBar.name == "QuickSlotsHotkeyBar")
                        {
                            var quickSlotInventory = player.GetQuickSlotInventory();
                            var item = quickSlotInventory.GetItemAt(hotkeyBar.m_selected, 0);
                            player.UseItem(null, item, false);
                        }
                        else
                        {
                            player.UseHotbarItem(hotkeyBar.m_selected + 1);
                        }
                    }
                }

                if (hotkeyBar.m_selected > hotkeyBar.m_elements.Count - 1)
                {
                    hotkeyBar.m_selected = Mathf.Max(0, hotkeyBar.m_elements.Count - 1);
                }
            }

            public static void GotoHotkeyBar(int newIndex)
            {
                if (newIndex < 0 || newIndex >= HotkeyBars.Count)
                {
                    return;
                }

                var fromRight = newIndex < SelectedHotkeyBarIndex;
                SelectHotkeyBar(newIndex, fromRight);
            }

            public static void SelectHotkeyBar(int index, bool fromRight)
            {
                if (index < 0 || index >= HotkeyBars.Count)
                {
                    return;
                }

                SelectedHotkeyBarIndex = index;
                for (var i = 0; i < HotkeyBars.Count; i++)
                {
                    var hotkeyBar = HotkeyBars[i];
                    if (i == index)
                    {
                        hotkeyBar.m_selected = fromRight ? hotkeyBar.m_elements.Count - 1 : 0;
                    }
                    else
                    {
                        hotkeyBar.m_selected = -1;
                    }
                }
            }

            public static void DeselectHotkeyBar()
            {
                SelectedHotkeyBarIndex = -1;
                foreach (var hotkeyBar in HotkeyBars)
                {
                    hotkeyBar.m_selected = -1;
                }
            }
        }

        [HarmonyPatch(typeof(Hud), "OnDestroy")]
        public static class Hud_OnDestroy_Patch
        {
            public static void Postfix(Hud __instance)
            {
                HotkeyBars = null;
                SelectedHotkeyBarIndex = -1;
            }
        }
    }

    [HarmonyPatch(typeof(HotkeyBar), "Update")]
    public static class HotkeyBar_Update_Patch
    {
        public static bool Prefix(HotkeyBar __instance)
        {
            // Everything controlled in above update
            return false;
        }
    }
}
