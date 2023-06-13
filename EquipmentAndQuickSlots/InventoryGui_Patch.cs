using System;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EquipmentAndQuickSlots
{
    public static class InventoryGui_Patch
    {
        public static InventoryGrid QuickSlotGrid;
        public static InventoryGrid EquipmentSlotGrid;

        public static GameObject EAQSPanel;

        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        public static class InventoryGui_Awake_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (EquipmentAndQuickSlots.QuickSlotsEnabled.Value)
                {
                    BuildQuickSlotGrid(__instance);
                }
                if (EquipmentAndQuickSlots.EquipmentSlotsEnabled.Value)
                {
                    BuildEquipmentSlotGrid(__instance);
                }
            }

            private static void BuildQuickSlotGrid(InventoryGui inventoryGui)
            {
                var pos = EquipmentAndQuickSlots.HasAuga ? new Vector2(3, -108) : new Vector2(3, -160);
                BuildInventoryGrid(ref QuickSlotGrid, "QuickSlotGrid", pos, new Vector2((74 * EquipmentAndQuickSlots.QuickSlotCount) + 10, 90), inventoryGui);
            }

            private static void BuildEquipmentSlotGrid(InventoryGui inventoryGui)
            {
                var pos = EquipmentAndQuickSlots.HasAuga ? new Vector2(3, 40) : new Vector2(3, 20);
                BuildInventoryGrid(ref EquipmentSlotGrid, "EquipmentSlotGrid", pos, new Vector2(210, 270), inventoryGui);
            }

            private static void BuildInventoryGrid(ref InventoryGrid grid, string name, Vector2 position, Vector2 size, InventoryGui inventoryGui)
            {
                if (grid != null)
                {
                    Object.Destroy(grid);
                    grid = null;
                }

                if (EAQSPanel == null)
                {
                    if (EquipmentAndQuickSlots.HasAuga)
                    {
                        EAQSPanel = Auga.API.Panel_Create(inventoryGui.m_player, new Vector2(255, 352), "EAQS", false);
                        var rt = (RectTransform)EAQSPanel.transform;
                        rt.anchorMin = new Vector2(0, 1);
                        rt.anchorMax = new Vector2(0, 1);
                        rt.anchoredPosition = new Vector2(752, -166);
                        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 255);
                        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 352);

                        var paperdolls = Object.Instantiate(EquipmentAndQuickSlots.Paperdolls, EAQSPanel.transform, false);
                        paperdolls.name = "Paperdolls";

                        var divider = Auga.API.Divider_CreateSmall(EAQSPanel.transform, "Divider", 255 - 40);
                        rt = (RectTransform)divider.transform;
                        rt.anchoredPosition = new Vector2(0, -157);
                    }
                    else
                    {
                        EAQSPanel = new GameObject("EAQS", typeof(RectTransform));
                        EAQSPanel.transform.SetParent(inventoryGui.m_player);
                        var rt = (RectTransform)EAQSPanel.transform;
                        rt.anchorMin = new Vector2(0, 1);
                        rt.anchorMax = new Vector2(0, 1);
                        rt.anchoredPosition = new Vector2(752, -166);
                        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 255);
                        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 352);
                    }
                }

                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(EAQSPanel.transform, false);

                grid = go.AddComponent<InventoryGrid>();
                var root = new GameObject("Root", typeof(RectTransform));
                root.transform.SetParent(go.transform, false);

                var rect = (RectTransform)go.transform;
                rect.anchoredPosition = position;

                grid.m_elementPrefab = inventoryGui.m_playerGrid.m_elementPrefab;
                grid.m_gridRoot = root.transform as RectTransform;
                grid.m_elementSpace = inventoryGui.m_playerGrid.m_elementSpace;
                grid.ResetView();

                if (name == "EquipmentSlotGrid")
                {
                    grid.m_onSelected += OnEquipmentSelected(inventoryGui);
                    grid.m_onRightClick += OnEquipmentRightClicked(inventoryGui);
                }
                else
                {
                    grid.m_onSelected += OnSelected(inventoryGui);
                    grid.m_onRightClick += OnRightClicked(inventoryGui);
                }

                grid.m_uiGroup = grid.gameObject.AddComponent<UIGroupHandler>();
                grid.m_uiGroup.m_groupPriority = 1;
                grid.m_uiGroup.m_active = true;

                if (!EquipmentAndQuickSlots.HasAuga)
                {
                    var highlight = new GameObject("SelectedFrame", typeof(RectTransform));
                    highlight.transform.SetParent(go.transform, false);
                    highlight.AddComponent<Image>().color = Color.yellow;
                    var highlightRT = (RectTransform)highlight.transform;
                    highlightRT.SetAsFirstSibling();
                    highlightRT.anchoredPosition = new Vector2(0, 0);
                    highlightRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + 2);
                    highlightRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y + 2);
                    highlightRT.localScale = new Vector3(1, 1, 1);
                    grid.m_uiGroup.m_enableWhenActiveAndGamepad = highlight;

                    var bkg = inventoryGui.m_player.Find("Bkg").gameObject;
                    var background = Object.Instantiate(bkg, go.transform);
                    background.name = name + "Bkg";
                    var backgroundRT = (RectTransform)background.transform;
                    backgroundRT.SetSiblingIndex(1);
                    backgroundRT.anchoredPosition = new Vector2(0, 0);
                    backgroundRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                    backgroundRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                    backgroundRT.localScale = new Vector3(1, 1, 1);
                }

                var list = inventoryGui.m_uiGroups.ToList();
                list.Insert(2, grid.m_uiGroup);
                inventoryGui.m_uiGroups = list.ToArray();
            }

            private static Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier> OnSelected(InventoryGui inventoryGui)
            {
                return (InventoryGrid inventoryGrid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod) =>
                {
                    EquipmentAndQuickSlots.Log($"OnSelected: inventoryGrid={inventoryGrid}, item={item?.m_shared.m_name}, pos={pos}, mod={mod}");
                    inventoryGui.OnSelectedItem(inventoryGrid, item, pos, mod);
                };
            }

            private static Action<InventoryGrid, ItemDrop.ItemData, Vector2i> OnRightClicked(InventoryGui inventoryGui)
            {
                return (InventoryGrid inventoryGrid, ItemDrop.ItemData item, Vector2i pos) =>
                {
                    EquipmentAndQuickSlots.Log($"OnRightClicked: inventoryGrid={inventoryGrid}, item={item?.m_shared.m_name}, pos={pos}");
                    if (item == null || Player.m_localPlayer == null)
                    {
                        return;
                    }
                    Player.m_localPlayer.UseItem(Player.m_localPlayer.m_inventory.Extended(), item, true);
                };
            }

            private static Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier> OnEquipmentSelected(InventoryGui inventoryGui)
            {
                return (InventoryGrid inventoryGrid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod) =>
                {
                    var player = Player.m_localPlayer;
                    EquipmentAndQuickSlots.Log($"OnEquipmentSelected: inventoryGrid={inventoryGrid}, item={item?.m_shared.m_name}, pos={pos}, mod={mod}");

                    if (player != null 
                        && inventoryGui.m_dragItem != null 
                        && EquipmentAndQuickSlots.IsSlotEquippable(inventoryGui.m_dragItem)
                        && pos == EquipmentAndQuickSlots.GetEquipmentSlotForType(inventoryGui.m_dragItem.m_shared.m_itemType))
                    {
                        player.QueueEquipAction(inventoryGui.m_dragItem);
                        inventoryGui.SetupDragItem(null, null, 0);
                    }
                };
            }

            private static Action<InventoryGrid, ItemDrop.ItemData, Vector2i> OnEquipmentRightClicked(InventoryGui inventoryGui)
            {
                return (InventoryGrid inventoryGrid, ItemDrop.ItemData item, Vector2i pos) =>
                {
                    var player = Player.m_localPlayer;
                    EquipmentAndQuickSlots.Log($"OnEquipmentRightClicked: inventoryGrid={inventoryGrid}, item={item?.m_shared.m_name}, pos={pos}");
                    if (item != null 
                        && player != null 
                        && item.m_equipped 
                        && player.IsItemEquiped(item)
                        && inventoryGui.m_dragItem == null)
                    {
                        Player.m_localPlayer.QueueUnequipAction(item);
                    }
                };
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateInventory")]
        public static class InventoryGui_UpdateInventory_Patch
        {
            public static bool Prefix(InventoryGui __instance, Player player)
            {
                player.m_inventory.Extended().CallBase = true;
                __instance.m_playerGrid.UpdateInventory(player.m_inventory, player, __instance.m_dragItem);
                player.m_inventory.Extended().CallBase = false;

                if (QuickSlotGrid != null)
                {
                    var quickSlotInventory = player.GetQuickSlotInventory();
                    if (quickSlotInventory != null)
                    {
                        QuickSlotGrid.UpdateInventory(quickSlotInventory, player, __instance.m_dragItem);
                    }
                }

                if (EquipmentSlotGrid != null)
                {
                    var equipmentSlotInventory = player.GetEquipmentSlotInventory();
                    if (equipmentSlotInventory != null)
                    {
                        EquipmentSlotGrid.UpdateInventory(equipmentSlotInventory, player, __instance.m_dragItem);
                    }
                }

                if (EquipmentAndQuickSlots.HasAuga && EAQSPanel != null)
                {
                    var paperdolls = EAQSPanel.transform.Find("Paperdolls");
                    if (player.m_visEquipment.GetModelIndex() == 1)
                    {
                        paperdolls.transform.Find("Male").gameObject.SetActive(false);
                    }
                    else
                    {
                        paperdolls.transform.Find("Female").gameObject.SetActive(false);
                    }
                }

                return false;
            }
        }

        
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateGamepad))]
        public static class InventoryGui_UpdateGamepad_Patch
        {
            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
            {
                var activeGroupField = AccessTools.DeclaredField(typeof(InventoryGui), "m_activeGroup" );
                
                var instrs = instructions.ToList();

                var counter = 0;
                var skipLines = 0;

                for (int i = 0; i < instrs.Count; ++i)
                {
                    if (i > 5 && instrs[i].opcode == OpCodes.Ldfld 
                        && instrs[i].operand.Equals(activeGroupField) && instrs[i + 1].opcode == OpCodes.Ldc_I4_3 
                        && instrs[i + 2].opcode == OpCodes.Bne_Un)
                    {
                        //Replace Field with Call
                        yield return EquipmentAndQuickSlots.LogMessage(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(InventoryGui_UpdateGamepad_Patch), nameof(GetMaxUiGroups))),counter);
                        counter++;
                        
                        //Remove ldc_i4.3
                        skipLines++;
                        
                        //Create new BrTrue
                        yield return EquipmentAndQuickSlots.LogMessage(new CodeInstruction(OpCodes.Brtrue, instrs[i +2].operand), counter);
                        
                        //Remove Bne
                        skipLines++;

                        i += skipLines;
                    }
                    else
                    {
                        yield return EquipmentAndQuickSlots.LogMessage(instrs[i], counter);
                        counter++;
                    }
                }
            }

            public static bool GetMaxUiGroups(InventoryGui instance)
            {
                return instance.m_activeGroup != instance.m_uiGroups.Length - 1;
            }
        }
    }

    // Bugfix: Upgrading doesn't properly check if you have an empty space, so no more unequipping
    //         Also, this upgrades in-place instead of making a new item!
    // public void DoCrafting(Player player)
    [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
    public static class InventoryGui_DoCrafting_Patch
    {
        public static bool Prefix(InventoryGui __instance, Player player, bool __runOriginal)
        {
            if (!__runOriginal)
                return false;

            if (__instance.m_craftRecipe == null || __instance.m_craftRecipe.m_requireOnlyOneIngredient)
                return true;

            var newQuality = __instance.m_craftUpgradeItem?.m_quality + 1 ?? 1;
            if (newQuality > __instance.m_craftRecipe.m_item.m_itemData.m_shared.m_maxQuality
                || !player.HaveRequirements(__instance.m_craftRecipe, false, newQuality) && !player.NoCostCheat()
                || (__instance.m_craftUpgradeItem != null
                    && !player.GetInventory().ContainsItem(__instance.m_craftUpgradeItem)
                    || __instance.m_craftUpgradeItem == null
                    && !player.GetInventory().HaveEmptySlot()))
            {
                return false;
            }

            if (__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(__instance.m_craftRecipe.m_item.m_itemData.m_shared.m_dlc))
            {
                return false;
            }

            var upgradeItem = __instance.m_craftUpgradeItem;
            if (upgradeItem != null)
            {
                upgradeItem.m_quality = newQuality;
                upgradeItem.m_durability = upgradeItem.GetMaxDurability();

                if (!player.NoCostCheat())
                {
                    player.ConsumeResources(__instance.m_craftRecipe.m_resources, newQuality);
                }
                __instance.UpdateCraftingPanel();

                var currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
                if (currentCraftingStation != null)
                {
                    currentCraftingStation.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                }
                else
                {
                    __instance.m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                }

                ++Game.instance.GetPlayerProfile().m_playerStats.m_crafts;
                Gogan.LogEvent("Game", "Crafted", __instance.m_craftRecipe.m_item.m_itemData.m_shared.m_name, (long)newQuality);

                return false;
            }

            return true;
        }
    }

    [HarmonyPatch]
    public static class InventoryGui_UpdateContainer_Patch
    {
        public static int InUpdateContainerCall = 0;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
        [HarmonyPrefix]
        public static void InventoryGui_UpdateContainer_Prefix()
        {
            InUpdateContainerCall++;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateContainer))]
        [HarmonyPostfix]
        public static void InventoryGui_UpdateContainer_Postfix()
        {
            InUpdateContainerCall--;
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupDragItem))]
        [HarmonyPrefix]
        public static bool InventoryGui_SetupDragItem_Prefix(InventoryGui __instance, ItemDrop.ItemData item)
        {
            if (InUpdateContainerCall > 0 && item == null && __instance.m_dragInventory != null && IsOneOfPlayersInventory(__instance.m_dragInventory))
                return false;
            return true;
        }

        public static bool IsOneOfPlayersInventory(Inventory inventory)
        {
            foreach (var playerInventory in Player.m_localPlayer.GetAllInventories())
            {
                if (inventory == playerInventory)
                    return true;
            }

            return false;
        }
    }
}
