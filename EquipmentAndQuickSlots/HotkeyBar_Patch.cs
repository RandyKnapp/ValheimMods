using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
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
            public static bool Prefix(HotkeyBar __instance, Player player)
            {
                if (__instance.name != "QuickSlotsHotkeyBar")
                {
                    return true;
                }

                if (player == null || player.IsDead())
                {
                    foreach (var element in __instance.m_elements)
                    {
                        Object.Destroy(element.m_go);
                    }

                    __instance.m_elements.Clear();
                }
                else
                {
                    player.GetQuickSlotInventory().GetBoundItems(__instance.m_items);
                    __instance.m_items.Sort((x, y) => x.m_gridPos.x.CompareTo(y.m_gridPos.x));
                    const int showElementCount = EquipmentAndQuickSlots.QuickSlotCount;
                    if (__instance.m_elements.Count != showElementCount)
                    {
                        foreach (var element in __instance.m_elements)
                        {
                            Object.Destroy(element.m_go);
                        }

                        __instance.m_elements.Clear();
                        for (var index = 0; index < showElementCount; ++index)
                        {
                            var elementData = new HotkeyBar.ElementData()
                            {
                                m_go = Object.Instantiate(__instance.m_elementPrefab, __instance.transform)
                            };
                            elementData.m_go.transform.localPosition = new Vector3(index * __instance.m_elementSpace, 0.0f, 0.0f);
                            elementData.m_icon = elementData.m_go.transform.transform.Find("icon").GetComponent<Image>();
                            elementData.m_durability = elementData.m_go.transform.Find("durability").GetComponent<GuiBar>();
                            elementData.m_amount = elementData.m_go.transform.Find("amount").GetComponent<Text>();
                            elementData.m_equiped = elementData.m_go.transform.Find("equiped").gameObject;
                            elementData.m_queued = elementData.m_go.transform.Find("queued").gameObject;
                            elementData.m_selection = elementData.m_go.transform.Find("selected").gameObject;

                            var bindingText = elementData.m_go.transform.Find("binding").GetComponent<Text>();
                            bindingText.enabled = true;
                            bindingText.horizontalOverflow = HorizontalWrapMode.Overflow;
                            bindingText.text = EquipmentAndQuickSlots.GetBindingLabel(index);

                            __instance.m_elements.Add(elementData);
                        }
                    }

                    foreach (var element in __instance.m_elements)
                    {
                        element.m_used = false;
                    }

                    var isGamepadActive = ZInput.IsGamepadActive();
                    foreach (var itemData in __instance.m_items)
                    {
                        var element = __instance.m_elements[itemData.m_gridPos.x];
                        element.m_used = true;
                        element.m_icon.gameObject.SetActive(true);
                        element.m_icon.sprite = itemData.GetIcon();
                        element.m_durability.gameObject.SetActive(itemData.m_shared.m_useDurability);
                        if (itemData.m_shared.m_useDurability)
                        {
                            if (itemData.m_durability <= 0.0)
                            {
                                element.m_durability.SetValue(1f);
                                element.m_durability.SetColor((double) Mathf.Sin(Time.time * 10f) > 0.0 ? Color.red : new Color(0.0f, 0.0f, 0.0f, 0.0f));
                            }
                            else
                            {
                                element.m_durability.SetValue(itemData.GetDurabilityPercentage());
                                element.m_durability.ResetColor();
                            }
                        }

                        element.m_equiped.SetActive(itemData.m_equiped);
                        element.m_queued.SetActive(player.IsItemQueued(itemData));
                        if (itemData.m_shared.m_maxStackSize > 1)
                        {
                            element.m_amount.gameObject.SetActive(true);
                            element.m_amount.text = itemData.m_stack.ToString() + "/" + itemData.m_shared.m_maxStackSize.ToString();
                        }
                        else
                        {
                            element.m_amount.gameObject.SetActive(false);
                        }
                    }

                    for (var index = 0; index < __instance.m_elements.Count; ++index)
                    {
                        var element = __instance.m_elements[index];
                        element.m_selection.SetActive(isGamepadActive && index == __instance.m_selected);
                        if (!element.m_used)
                        {
                            element.m_icon.gameObject.SetActive(false);
                            element.m_durability.gameObject.SetActive(false);
                            element.m_equiped.SetActive(false);
                            element.m_queued.SetActive(false);
                            element.m_amount.gameObject.SetActive(false);
                        }
                    }
                }

                return false;
            }
        }
    }

    [HarmonyPatch(typeof(Hud), "Awake")]
    public static class Hud_Awake_Patch
    {
        public static void Postfix(Hud __instance)
        {
            var hotkeyBar = __instance.GetComponentInChildren<HotkeyBar>();

            if (hotkeyBar.transform.parent.Find("QuickSlotsHotkeyBar") == null)
            {
                var quickslotsHotkeyBar = Object.Instantiate(hotkeyBar.gameObject, __instance.m_healthBarRoot, true);
                quickslotsHotkeyBar.name = "QuickSlotsHotkeyBar";
                (quickslotsHotkeyBar.transform as RectTransform).anchoredPosition = new Vector2(55, -120);
                quickslotsHotkeyBar.transform.localEulerAngles = new Vector3(0.0f, 0.0f, -90f);
                quickslotsHotkeyBar.GetComponent<HotkeyBar>().m_selected = -1;
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
