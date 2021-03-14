using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.Crafting;
using ExtendedItemDataFramework;
using fastJSON;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot
{
    public class MagicItemComponent : BaseExtendedItemComponent
    {
        public MagicItem MagicItem;

        public MagicItemComponent(ExtendedItemData parent) 
            : base(typeof(MagicItemComponent).AssemblyQualifiedName, parent)
        {
        }

        public void SetMagicItem(MagicItem magicItem)
        {
            MagicItem = magicItem;
            Save();
        }

        public override string Serialize()
        {
            return JSON.ToJSON(MagicItem);
        }

        public override void Deserialize(string data)
        {
            MagicItem = JSON.ToObject<MagicItem>(data);
        }

        public override BaseExtendedItemComponent Clone()
        {
            return MemberwiseClone() as BaseExtendedItemComponent;
        }

        public static void OnNewExtendedItemData(ExtendedItemData itemdata)
        {
            if (itemdata.m_shared.m_name == "$item_helmet_dverger")
            {
                var magicItem = new MagicItem();
                magicItem.Rarity = ItemRarity.Rare;
                magicItem.Effects.Add(new MagicItemEffect() { EffectType = MagicEffectType.DvergerCirclet });
                magicItem.DisplayNameOverride = "circlet";

                itemdata.ReplaceComponent<MagicItemComponent>().MagicItem = magicItem;
            }
            else if (itemdata.m_shared.m_name == "$item_beltstrength")
            {
                var magicItem = new MagicItem();
                magicItem.Rarity = ItemRarity.Rare;
                magicItem.Effects.Add(new MagicItemEffect() { EffectType = MagicEffectType.Megingjord });
                magicItem.DisplayNameOverride = "belt";

                itemdata.ReplaceComponent<MagicItemComponent>().MagicItem = magicItem;
            }
            else if (itemdata.m_shared.m_name == "$item_wishbone")
            {
                var magicItem = new MagicItem();
                magicItem.Rarity = ItemRarity.Rare;
                magicItem.Effects.Add(new MagicItemEffect() { EffectType = MagicEffectType.Wishbone });
                magicItem.DisplayNameOverride = "remains";

                itemdata.ReplaceComponent<MagicItemComponent>().MagicItem = magicItem;
            }
        }
    }

    public static partial class ItemDataExtensions
    {
        public static bool IsMagic(this ItemDrop.ItemData itemData)
        {
            return itemData.Extended()?.GetComponent<MagicItemComponent>() != null;
        }

        public static bool UseMagicBackground(this ItemDrop.ItemData itemData)
        {
            return itemData.IsMagic() || itemData.IsRunestone();
        }

        public static ItemRarity GetRarity(this ItemDrop.ItemData itemData)
        {
            if (itemData.IsMagic())
            {
                return itemData.GetMagicItem().Rarity;
            }
            else if (itemData.IsMagicCraftingMaterial())
            {
                return itemData.GetCraftingMaterialRarity();
            }
            else if (itemData.IsRunestone())
            {
                return itemData.GetRunestoneRarity();
            }

            throw new ArgumentException("itemData is not magic item, magic crafting material, or runestone");
        }

        public static Color GetRarityColor(this ItemDrop.ItemData itemData)
        {
            var colorString = "white";
            if (itemData.IsMagic())
            {
                colorString = itemData.GetMagicItem().GetColorString();
            }
            else if (itemData.IsMagicCraftingMaterial())
            {
                colorString = itemData.GetCraftingMaterialRarityColor();
            }
            else if (itemData.IsRunestone())
            {
                colorString = itemData.GetRunestoneRarityColor();
            }

            return ColorUtility.TryParseHtmlString(colorString, out var color) ? color : Color.white;
        }

        public static bool HasMagicEffect(this ItemDrop.ItemData itemData, MagicEffectType effectType)
        {
            return itemData.GetMagicItem()?.HasEffect(effectType) ?? false;
        }

        public static MagicItem GetMagicItem(this ItemDrop.ItemData itemData)
        {
            return itemData.Extended()?.GetComponent<MagicItemComponent>()?.MagicItem;
        }

        public static string GetDecoratedName(this ItemDrop.ItemData itemData, string colorOverride = null)
        {
            var color = "white";
            if (!string.IsNullOrEmpty(colorOverride))
            {
                color = colorOverride;
            }
            else if (itemData.IsMagic())
            {
                color = itemData.GetMagicItem().GetColorString();
            }
            else if (itemData.IsMagicCraftingMaterial() || itemData.IsRunestone())
            {
                color = itemData.GetCraftingMaterialRarityColor();
            }

            return $"<color={color}>{itemData.m_shared.m_name}</color>";
        }

        public static bool IsPartOfSet(this ItemDrop.ItemData itemData, string setName)
        {
            return itemData.m_shared.m_setName == setName;
        }
    }

    public static partial class PlayerExtensions
    {
        public static List<ItemDrop.ItemData> GetEquipment(this Player player)
        {
            var results = new List<ItemDrop.ItemData>();
            if (player.m_rightItem != null)
                results.Add(player.m_rightItem);
            if (player.m_leftItem != null)
                results.Add(player.m_leftItem);
            if (player.m_chestItem != null)
                results.Add(player.m_chestItem);
            if (player.m_legItem != null)
                results.Add(player.m_legItem);
            if (player.m_helmetItem != null)
                results.Add(player.m_helmetItem);
            if (player.m_shoulderItem != null)
                results.Add(player.m_shoulderItem);
            if (player.m_utilityItem != null)
                results.Add(player.m_utilityItem);
            return results;
        }

        public static List<ItemDrop.ItemData> GetMagicEquipmentWithEffect(this Player player, MagicEffectType effectType)
        {
            return player.GetEquipment().Where(x => x.HasMagicEffect(effectType)).ToList();
        }

        public static bool HasMagicEquipmentWithEffect(this Player player, MagicEffectType effectType)
        {
            return GetMagicEquipmentWithEffect(player, effectType).Count > 0;
        }

        public static List<ItemDrop.ItemData> GetEquippedSetPieces(this Player player, string setName)
        {
            return player.GetEquipment().Where(x => x.IsPartOfSet(setName)).ToList();
        }

        public static int GetEquippedSetItemCount(this Player player, string setName)
        {
            return player.GetEquippedSetPieces(setName).Count;
        }
    }

    public static class ObjectDBExtensions
    {
        public static List<ItemDrop.ItemData> GetSetPieces(this ObjectDB objectDB, string setName)
        {
            return objectDB.m_items
                .Where(x => x.GetComponent<ItemDrop>().m_itemData.m_shared.m_setName == setName)
                .Select(x => x.GetComponent<ItemDrop>().m_itemData)
                .ToList();
        }
    }

    public static class ItemBackgroundHelper
    {
        public static Image CreateAndGetMagicItemBackgroundImage(GameObject elementGo, GameObject equipped, bool addSetItem)
        {
            var magicItemTransform = elementGo.transform.Find("magicItem");
            if (magicItemTransform == null)
            {
                var magicItemObject = Object.Instantiate(equipped, equipped.transform.parent);
                magicItemObject.transform.SetSiblingIndex(equipped.transform.GetSiblingIndex() + 1);
                magicItemObject.name = "magicItem";
                magicItemObject.SetActive(true);
                magicItemTransform = magicItemObject.transform;
                var magicItemInit = magicItemTransform.GetComponent<Image>();
                magicItemInit.color = Color.white;
                magicItemInit.raycastTarget = false;
            }

            // Also add set item marker
            if (addSetItem)
            {
                var setItemTransform = elementGo.transform.Find("setItem");
                if (setItemTransform == null)
                {
                    var setItemObject = Object.Instantiate(equipped, equipped.transform.parent);
                    setItemObject.transform.SetAsLastSibling();
                    setItemObject.name = "setItem";
                    setItemObject.SetActive(true);
                    setItemTransform = setItemObject.transform;
                    var setItemInit = setItemTransform.GetComponent<Image>();
                    setItemInit.raycastTarget = false;
                    setItemInit.sprite = EpicLoot.Assets.GenericSetItemSprite;
                    setItemInit.color = ColorUtility.TryParseHtmlString(EpicLoot.GetSetItemColor(), out var color) ? color : Color.white;
                }
            }

            // Also change equipped image
            var equippedImage = equipped.GetComponent<Image>();
            if (equippedImage != null)
            {
                equippedImage.sprite = EpicLoot.Assets.EquippedSprite;
                equippedImage.color = Color.white;
                equippedImage.raycastTarget = false;
                var rectTransform = equipped.transform as RectTransform;
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, equippedImage.sprite.texture.width);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, equippedImage.sprite.texture.height);
            }

            return magicItemTransform.GetComponent<Image>();
        }
    }

    //public void UpdateGui(Player player, ItemDrop.ItemData dragItem)
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    public static class InventoryGrid_UpdateGui_MagicItemComponent_Patch
    {
        public static void Postfix(InventoryGrid __instance, Player player, ItemDrop.ItemData dragItem)
        {
            foreach (var element in __instance.m_elements)
            {
                var magicItemTransform = element.m_go.transform.Find("magicItem");
                if (magicItemTransform != null)
                {
                    var magicItem = magicItemTransform.GetComponent<Image>();
                    if (magicItem != null)
                    {
                        magicItem.enabled = false;
                    }
                }

                var setItemTransform = element.m_go.transform.Find("setItem");
                if (setItemTransform != null)
                {
                    var setItem = setItemTransform.GetComponent<Image>();
                    if (setItem != null)
                    {
                        setItem.enabled = false;
                    }
                }
            }

            foreach (var item in __instance.m_inventory.GetAllItems())
            {
                var element = __instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, __instance.m_inventory.GetWidth());

                var magicItem = ItemBackgroundHelper.CreateAndGetMagicItemBackgroundImage(element.m_go, element.m_equiped.gameObject, true);
                if (item.UseMagicBackground())
                {
                    magicItem.enabled = true;
                    magicItem.sprite = EpicLoot.GetMagicItemBgSprite();
                    magicItem.color = item.GetRarityColor();
                }

                var setItem = element.m_go.transform.Find("setItem").GetComponent<Image>();
                if (setItem != null && !string.IsNullOrEmpty(item.m_shared.m_setName))
                {
                    setItem.enabled = true;
                }
            }
        }
    }

    //void UpdateIcons(Player player)
    [HarmonyPatch(typeof(HotkeyBar), "UpdateIcons", typeof(Player))]
    public static class HotkeyBar_UpdateIcons_Patch
    {
        public static void Postfix(HotkeyBar __instance, List<HotkeyBar.ElementData> ___m_elements, List<ItemDrop.ItemData> ___m_items, Player player)
        {
            if (player == null || player.IsDead())
            {
                return;
            }

            for (var index = 0; index < ___m_elements.Count; index++)
            {
                var element = ___m_elements[index];
                var magicItem = ItemBackgroundHelper.CreateAndGetMagicItemBackgroundImage(element.m_go, element.m_equiped, false);
                magicItem.enabled = false;
            }

            for (var index = 0; index < ___m_items.Count; ++index)
            {
                var itemData = ___m_items[index];
                var element = GetElementForItem(___m_elements, itemData);
                if (element == null)
                {
                    Debug.LogWarning($"Tried to get element for {itemData.m_shared.m_name} at {itemData.m_gridPos}, but element was null (total elements = {___m_elements.Count})");
                    continue;
                }

                var magicItem = ItemBackgroundHelper.CreateAndGetMagicItemBackgroundImage(element.m_go, element.m_equiped, false);
                if (itemData.UseMagicBackground())
                {
                    magicItem.enabled = true;
                    magicItem.sprite = EpicLoot.GetMagicItemBgSprite();
                    magicItem.color = itemData.GetRarityColor();
                }
            }
        }

        private static HotkeyBar.ElementData GetElementForItem(List<HotkeyBar.ElementData> elements, ItemDrop.ItemData item)
        {
            var index = item.m_gridPos.y == 0 
                ? item.m_gridPos.x 
                : Player.m_localPlayer.GetInventory().m_width + item.m_gridPos.x - 5;

            return index >= 0 && index < elements.Count ? elements[index] : null;
        }
    }
}
