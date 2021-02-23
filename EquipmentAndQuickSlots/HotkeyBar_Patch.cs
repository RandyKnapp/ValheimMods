using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EquipmentAndQuickSlots
{
    //void UpdateIcons(Player player)
    [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons", new Type[] { typeof(Player) })]
    public static class HotkeyBar_UpdateIcons_Patch
    {
        public static bool Prefix(HotkeyBar __instance, Player player, List<HotkeyBar.ElementData> ___m_elements, List<ItemDrop.ItemData> ___m_items, int ___m_selected)
        {
            if (player == null || player.IsDead())
            {
                foreach (HotkeyBar.ElementData element in ___m_elements)
                {
                    UnityEngine.Object.Destroy((UnityEngine.Object)element.m_go);
                }
                ___m_elements.Clear();
            }
            else
            {
                player.GetInventory().GetBoundItems(___m_items);
                ___m_items.Sort((Comparison<ItemDrop.ItemData>)((a, b) => a.m_gridPos.y == b.m_gridPos.y ? a.m_gridPos.x.CompareTo(b.m_gridPos.x) : a.m_gridPos.y.CompareTo(b.m_gridPos.y)));
                int num = player.GetInventory().m_width + EquipmentAndQuickSlots.QuickUseSlotCount;
                if (___m_elements.Count != num)
                {
                    foreach (HotkeyBar.ElementData element in ___m_elements)
                    {
                        UnityEngine.Object.Destroy((UnityEngine.Object)element.m_go);
                    }
                    ___m_elements.Clear();
                    for (int index = 0; index < num; ++index)
                    {
                        var parent = __instance.transform;
                        if (index >= 8)
                        {
                            parent = __instance.transform.parent.Find("healthpanel");
                        }
                        HotkeyBar.ElementData elementData = new HotkeyBar.ElementData()
                        {
                            m_go = UnityEngine.Object.Instantiate<GameObject>(__instance.m_elementPrefab, parent)
                        };

                        if (index < 8)
                        {
                            elementData.m_go.transform.localPosition = new Vector3(index * __instance.m_elementSpace, 0.0f, 0.0f);
                            elementData.m_go.transform.Find("binding").GetComponent<Text>().text = (index + 1).ToString();
                        }
                        else
                        {
                            var offset = new Vector2(100, -150);
                            var quickSlotIndex = index - 8;
                            elementData.m_go.transform.localPosition = new Vector3(offset.x, offset.y - quickSlotIndex * __instance.m_elementSpace, 0.0f);
                            elementData.m_go.transform.localEulerAngles = new Vector3(0, 0, -90);
                            string label = EquipmentAndQuickSlots.GetBindingKeycode(quickSlotIndex).ToUpperInvariant();
                            elementData.m_go.transform.Find("binding").GetComponent<Text>().text = label;
                        }
                        elementData.m_icon = elementData.m_go.transform.transform.Find("icon").GetComponent<Image>();
                        elementData.m_durability = elementData.m_go.transform.Find("durability").GetComponent<GuiBar>();
                        elementData.m_amount = elementData.m_go.transform.Find("amount").GetComponent<Text>();
                        elementData.m_equiped = elementData.m_go.transform.Find("equiped").gameObject;
                        elementData.m_queued = elementData.m_go.transform.Find("queued").gameObject;
                        elementData.m_selection = elementData.m_go.transform.Find("selected").gameObject;
                        ___m_elements.Add(elementData);
                    }
                }

                foreach (HotkeyBar.ElementData element in ___m_elements)
                {
                    element.m_used = false;
                }

                bool isGamepadActive = ZInput.IsGamepadActive();
                for (int index = 0; index < ___m_items.Count; ++index)
                {
                    ItemDrop.ItemData itemData = ___m_items[index];
                    HotkeyBar.ElementData element = GetElementForItem(___m_elements, itemData);
                    element.m_used = true;
                    element.m_icon.gameObject.SetActive(true);
                    element.m_icon.sprite = itemData.GetIcon();
                    element.m_durability.gameObject.SetActive(itemData.m_shared.m_useDurability);
                    if (itemData.m_shared.m_useDurability)
                    {
                        if (itemData.m_durability <= 0.0f)
                        {
                            element.m_durability.SetValue(1.0f);
                            element.m_durability.SetColor(Mathf.Sin(Time.time * 10.0f) > 0.0f ? Color.red : new Color(0.0f, 0.0f, 0.0f, 0.0f));
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
                        element.m_amount.text = $"{itemData.m_stack}/{itemData.m_shared.m_maxStackSize}";
                    }
                    else
                    {
                        element.m_amount.gameObject.SetActive(false);
                    }
                }

                for (int index = 0; index < ___m_elements.Count; ++index)
                {
                    HotkeyBar.ElementData element = ___m_elements[index];
                    element.m_selection.SetActive(isGamepadActive && index == ___m_selected);
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

        private static HotkeyBar.ElementData GetElementForItem(List<HotkeyBar.ElementData> elements, ItemDrop.ItemData item)
        {
            if (item.m_gridPos.y == 0)
            {
                return elements[item.m_gridPos.x];
            }
            else
            {
                return elements[Player.m_localPlayer.GetInventory().m_width + item.m_gridPos.x - EquipmentAndQuickSlots.QuickUseSlotIndexStart];
            }
        }
    }
}
