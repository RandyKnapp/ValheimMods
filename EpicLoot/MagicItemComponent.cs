using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Common;
using EpicLoot.Crafting;
using EpicLoot.Data;
using EpicLoot.LegendarySystem;
using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
// ReSharper disable RedundantAssignment

namespace EpicLoot
{
    public class MagicItemComponent : ItemData
    {
        public const string TypeID = "rkel";

        public MagicItem MagicItem;


        public void SetMagicItem(MagicItem magicItem)
        {
            MagicItem = magicItem;
            Value = Serialize();
            Save();
            if (Item.m_equiped && Player.m_localPlayer.IsItemEquiped(Item))
                Multiplayer_Player_Patch.UpdatePlayerZDOForEquipment(Player.m_localPlayer, Item, MagicItem != null);
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(MagicItem, Formatting.None);
        }

        public void Deserialize()
        {
            try
            {
                if (string.IsNullOrEmpty(Value))
                    return;

                MagicItem = JsonConvert.DeserializeObject<MagicItem>(Value);
            }
            catch (Exception)
            {
                EpicLoot.LogError($"[{nameof(MagicItemComponent)}] Could not deserialize MagicItem json data! ({Item?.m_shared?.m_name})"); 
                throw;
            }
        }

        public ItemData Clone()
        {
            return MemberwiseClone() as ItemData;
        }

        public override void FirstLoad()
        {
            if (Item.m_shared.m_name == "$item_helmet_dverger")
            {
                var magicItem = new MagicItem();
                magicItem.Rarity = ItemRarity.Rare;
                magicItem.Effects.Add(new MagicItemEffect(MagicEffectType.DvergerCirclet));
                magicItem.TypeNameOverride = "$mod_epicloot_circlet";

                MagicItem = magicItem;
            }
            else if (Item.m_shared.m_name == "$item_beltstrength")
            {
                var magicItem = new MagicItem();
                magicItem.Rarity = ItemRarity.Rare;
                magicItem.Effects.Add(new MagicItemEffect(MagicEffectType.Megingjord));
                magicItem.TypeNameOverride = "$mod_epicloot_belt";

                MagicItem = magicItem;
            }
            else if (Item.m_shared.m_name == "$item_wishbone")
            {
                var magicItem = new MagicItem();
                magicItem.Rarity = ItemRarity.Epic;
                magicItem.Effects.Add(new MagicItemEffect(MagicEffectType.Wishbone));
                magicItem.TypeNameOverride = "$mod_epicloot_remains";

                MagicItem = magicItem;
            }
            else
            {
                CheckForExtendedItemDataAndConvert();
            }
        }

        public override void Load()
        {
            if (!string.IsNullOrEmpty(Value))
                Deserialize();

            CheckForExtendedItemDataAndConvert();
        }

        public void Save(MagicItem magicItem)
        {
            MagicItem = magicItem;
            Serialize();
        }

        private void CheckForExtendedItemDataAndConvert()
        {
            if (!Item.IsLegacyEIDFItem() || !Item.IsLegacyMagicItem() || MagicItem != null) return;

            Value = EIDFLegacy.GetMagicItemFromCrafterName(Item);

            if (string.IsNullOrEmpty(Value))
                return;

            Deserialize();
        }
    }

    public static class ItemDataExtensions
    {
        public static bool IsMagic(this ItemDrop.ItemData itemData)
        {
            var magicData = itemData.Data().Get<MagicItemComponent>();
            return magicData != null && magicData.MagicItem != null;
        }

        public static bool IsMagic(this ItemDrop.ItemData itemData, out MagicItem magicItem)
        {
            magicItem = itemData.GetMagicItem();
            return magicItem != null;
        }

        public static bool UseMagicBackground(this ItemDrop.ItemData itemData)
        {
            return itemData.IsMagic() || itemData.IsRunestone();
        }

        public static bool HasRarity(this ItemDrop.ItemData itemData)
        {
            return itemData.IsMagic() || itemData.IsMagicCraftingMaterial() || itemData.IsRunestone();
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

        public static bool HasMagicEffect(this ItemDrop.ItemData itemData, string effectType)
        {
            return itemData.GetMagicItem()?.HasEffect(effectType) ?? false;
        }

        public static void SaveMagicItem(this ItemDrop.ItemData itemData, MagicItem magicItem)
        {

            itemData.Data().Get<MagicItemComponent>()?.Save(magicItem);
            return;
        }

        public static bool IsExtended(this ItemDrop.ItemData itemData)
        {
            return itemData.Data().Get<MagicItemComponent>() != null;
        }

        public static ItemDrop.ItemData Extended(this ItemDrop.ItemData itemData)
        {
            var value = itemData.Data().Get<MagicItemComponent>();
            return value?.Item;
        }

        public static MagicItem GetMagicItem(this ItemDrop.ItemData itemData)
        {
            return itemData.Data().Get<MagicItemComponent>()?.MagicItem;
        }

        public static string GetDisplayName(this ItemDrop.ItemData itemData)
        {
            var name = itemData.m_shared.m_name;

            if (itemData.IsMagic(out var magicItem) && !string.IsNullOrEmpty(magicItem.DisplayName))
            {
                name = magicItem.DisplayName;
            }

            return name;
        }

        public static string GetDecoratedName(this ItemDrop.ItemData itemData, string colorOverride = null)
        {
            var color = "white";
            var name = GetDisplayName(itemData);

            if (!string.IsNullOrEmpty(colorOverride))
            {
                color = colorOverride;
            }
            else if (itemData.IsMagic(out var magicItem))
            {
                color = magicItem.GetColorString();
            }
            else if (itemData.IsMagicCraftingMaterial() || itemData.IsRunestone())
            {
                color = itemData.GetCraftingMaterialRarityColor();
            }

            return $"<color={color}>{name}</color>";
        }

        public static string GetDescription(this ItemDrop.ItemData itemData)
        {
            if (itemData.IsMagic())
            {
                var magicItem = itemData.GetMagicItem();
                if (magicItem.IsUniqueLegendary() && UniqueLegendaryHelper.TryGetLegendaryInfo(magicItem.LegendaryID, out var legendaryInfo))
                {
                    return legendaryInfo.Description;
                }
            }
            return itemData.m_shared.m_description;
        }

        public static bool IsPartOfSet(this ItemDrop.ItemData itemData, string setName)
        {
            return itemData.GetSetID() == setName;
        }

        public static bool CanBeAugmented(this ItemDrop.ItemData itemData)
        {
            if (!itemData.IsMagic())
            {
                return false;
            }

            return itemData.GetMagicItem().Effects.Select(effect => MagicItemEffectDefinitions.Get(effect.EffectType)).Any(effectDef => effectDef.CanBeAugmented);
        }

        public static string GetSetID(this ItemDrop.ItemData itemData, out bool isMundane)
        {
            isMundane = true;
            if (itemData.IsMagic(out var magicItem) && !string.IsNullOrEmpty(magicItem.SetID))
            {
                isMundane = false;
                return magicItem.SetID;
            }

            if (!string.IsNullOrEmpty(itemData.m_shared.m_setName))
            {
                return itemData.m_shared.m_setName;
            }

            return null;
        }

        public static string GetSetID(this ItemDrop.ItemData itemData)
        {
            return GetSetID(itemData, out _);
        }

        public static LegendarySetInfo GetLegendarySetInfo(this ItemDrop.ItemData itemData)
        {
            UniqueLegendaryHelper.TryGetLegendarySetInfo(itemData.GetSetID(), out var setInfo);
            return setInfo;
        }

        public static bool IsSetItem(this ItemDrop.ItemData itemData)
        {
            return !string.IsNullOrEmpty(itemData.GetSetID());
        }

        public static bool IsLegendarySetItem(this ItemDrop.ItemData itemData)
        {
            return itemData.IsMagic(out var magicItem) && !string.IsNullOrEmpty(magicItem.SetID);
        }

        public static bool IsMundaneSetItem(this ItemDrop.ItemData itemData)
        {
            return !string.IsNullOrEmpty(itemData.m_shared.m_setName);
        }

        public static int GetSetSize(this ItemDrop.ItemData itemData)
        {
            var setID = itemData.GetSetID(out var isMundane);
            if (!string.IsNullOrEmpty(setID))
            {
                if (isMundane)
                {
                    return itemData.m_shared.m_setSize;
                }
                else if (UniqueLegendaryHelper.TryGetLegendarySetInfo(setID, out var setInfo))
                {
                    return setInfo.LegendaryIDs.Count;
                }
            }

            return 0;
        }

        public static List<string> GetSetPieces(string setName)
        {
            if (UniqueLegendaryHelper.TryGetLegendarySetInfo(setName, out var setInfo))
            {
                return setInfo.LegendaryIDs;
            }

            return GetMundaneSetPieces(ObjectDB.instance, setName);
        }

        public static List<string> GetMundaneSetPieces(ObjectDB objectDB, string setName)
        {
            var results = new List<string>();
            foreach (var itemPrefab in objectDB.m_items)
            {
                if (itemPrefab == null)
                {
                    EpicLoot.LogError("Null Item left in ObjectDB! (This means that a prefab was deleted and not an instance)");
                    continue;
                }

                var itemDrop = itemPrefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    EpicLoot.LogError($"Item in ObjectDB missing ItemDrop: ({itemPrefab.name})");
                    continue;
                }

                if (itemDrop.m_itemData.m_shared.m_setName == setName)
                {
                    results.Add(itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
                }
            }

            return results;
        }

        public static GameObject InitializeCustomData(this ItemDrop.ItemData itemData)
        {
            var prefab = itemData.m_dropPrefab;
            if (prefab != null)
            {
                var itemDropPrefab = prefab.GetComponent<ItemDrop>();
                if ((itemData.IsLegacyEIDFItem() || itemDropPrefab.m_itemData.IsExtended()) && !itemData.IsExtended())
                {
                    var instanceData = itemData.Data().Add<MagicItemComponent>();

                    if (itemDropPrefab.m_itemData.IsExtended())
                    {
                        var prefabData = itemDropPrefab.m_itemData.Data().Get<MagicItemComponent>();

                        if (instanceData != null && prefabData != null)
                        {
                            instanceData.Save(prefabData.MagicItem);
                        }
                    }
                    return itemDropPrefab.gameObject;
                }
            }

            return null;
        }

        public static string GetSetTooltip(this ItemDrop.ItemData item)
        {
            var text = new StringBuilder();
            var setID = item.GetSetID(out var isMundane);
            var setSize = item.GetSetSize();

            var setPieces = GetSetPieces(setID);
            var currentSetEquipped = Player.m_localPlayer.GetEquippedSetPieces(setID);

            var setDisplayName = GetSetDisplayName(item, isMundane);
            text.Append($"\n\n<color={EpicLoot.GetSetItemColor()}> $mod_epicloot_set: {setDisplayName} ({currentSetEquipped.Count}/{setSize}):</color>");

            foreach (var setItemName in setPieces)
            {
                var isEquipped = IsSetItemEquipped(currentSetEquipped, setItemName, isMundane);
                var color = isEquipped ? "white" : "grey";
                var displayName = GetSetItemDisplayName(setItemName, isMundane);
                text.Append($"\n  <color={color}>{displayName}</color>");
            }

            if (isMundane)
            {
                var setEffectColor = currentSetEquipped.Count == setSize ? EpicLoot.GetSetItemColor() : "grey";
                var skillLevel = Player.m_localPlayer.GetSkillLevel(item.m_shared.m_skillType);
                text.Append($"\n<color={setEffectColor}>({setSize}) ‣ {item.GetSetStatusEffectTooltip(item.m_quality, skillLevel).Replace("\n", " ")}</color>");
            }
            else
            {
                var setInfo = item.GetLegendarySetInfo();
                foreach (var setBonusInfo in setInfo.SetBonuses.OrderBy(x => x.Count))
                {
                    var hasEquipped = currentSetEquipped.Count >= setBonusInfo.Count;
                    var effectDef = MagicItemEffectDefinitions.Get(setBonusInfo.Effect.Type);
                    if (effectDef == null)
                    {
                        EpicLoot.LogError($"Set Tooltip: Could not find effect ({setBonusInfo.Effect.Type}) for set ({setInfo.ID}) bonus ({setBonusInfo.Count})!");
                        continue;
                    }

                    var display = MagicItem.GetEffectText(effectDef, setBonusInfo.Effect.Values?.MinValue ?? 0);
                    text.Append($"\n<color={(hasEquipped ? EpicLoot.GetSetItemColor() : "grey")}>({setBonusInfo.Count}) ‣ {display}</color>");
                }
            }

            return text.ToString();
        }

        private static string GetSetItemDisplayName(string setItemName, bool isMundane)
        {
            if (isMundane)
            {
                return setItemName;
            }
            else if (UniqueLegendaryHelper.TryGetLegendaryInfo(setItemName, out var legendaryInfo))
            {
                return legendaryInfo.Name;
            }

            return setItemName;
        }

        public static string GetSetDisplayName(ItemDrop.ItemData item, bool isMundane)
        {
            if (isMundane)
            {
                var textInfo = new CultureInfo("en-US", false).TextInfo;
                return textInfo.ToTitleCase(item.m_shared.m_setName);
            }

            var setInfo = item.GetLegendarySetInfo();
            if (setInfo != null)
            {
                return Localization.instance.Localize(setInfo.Name);
            }
            else
            {
                return $"<unknown set:{item.GetSetID()}>";
            }
        }

        public static bool IsSetItemEquipped(List<ItemDrop.ItemData> currentSetEquipped, string setItemName, bool isMundane)
        {
            if (isMundane)
            {
                return currentSetEquipped.Find(x => x.m_shared.m_name == setItemName) != null;
            }
            else
            {
                return currentSetEquipped.Find(x => x.IsMagic(out var magicItem) && magicItem.LegendaryID == setItemName) != null;
            }
        }
    }

    public static class EquipmentEffectCache
    {
        public static ConditionalWeakTable<Player, Dictionary<string, float?>> Values = new ConditionalWeakTable<Player, Dictionary<string, float?>>();

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        public static class EquipmentEffectCache_Humanoid_UnequipItem_Patch
        {
            [UsedImplicitly]
            public static void Prefix(Humanoid __instance, ItemDrop.ItemData item)
            {
                if (__instance is Player player)
                {
                    Reset(player);
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        public static class EquipmentEffectCache_Humanoid_EquipItem_Patch
        {
            [UsedImplicitly]
            public static void Prefix(Humanoid __instance, ItemDrop.ItemData item)
            {
                if (__instance is Player player)
                {
                    Reset(player);
                }
            }
        }

        public static void Reset(Player player)
        {
            Values.Remove(player);
        }

        public static float? Get(Player player, string effect, Func<float?> calculate)
        {
            var values = Values.GetOrCreateValue(player);
            if (values.TryGetValue(effect, out float? value))
            {
                return value;
            }

            return values[effect] = calculate();
        }
    }

    public static class PlayerExtensions
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

        private static List<ItemDrop.ItemData> GetMagicEquipmentWithEffect(this Player player, string effectType)
        {
            return player.GetEquipment().Where(x => x.HasMagicEffect(effectType)).ToList();
        }

        public static List<MagicItemEffect> GetAllActiveMagicEffects(this Player player, string effectType = null)
        {
            var equipEffects = player.GetEquipment().Where(x => x.IsMagic())
                .SelectMany(x => x.GetMagicItem().GetEffects(effectType));
            var setEffects = player.GetAllActiveSetMagicEffects(effectType);
            return equipEffects.Concat(setEffects).ToList();
        }

        public static List<MagicItemEffect> GetAllActiveSetMagicEffects(this Player player, string effectType = null)
        {
            var activeSetEffects = new List<MagicItemEffect>();
            var equippedSets = player.GetEquippedSets();
            foreach (var setInfo in equippedSets)
            {
                var count = player.GetEquippedSetPieces(setInfo.ID).Count;
                foreach (var setBonusInfo in setInfo.SetBonuses)
                {
                    if (count >= setBonusInfo.Count && (effectType == null || setBonusInfo.Effect.Type == effectType))
                    {
                        var effect = new MagicItemEffect(setBonusInfo.Effect.Type, setBonusInfo.Effect.Values?.MinValue ?? 0);
                        activeSetEffects.Add(effect);
                    }
                }
            }

            return activeSetEffects;
        }

        public static HashSet<LegendarySetInfo> GetEquippedSets(this Player player)
        {
            var sets = new HashSet<LegendarySetInfo>();
            foreach (var itemData in player.GetEquipment())
            {
                if (itemData.IsMagic(out var magicItem) && magicItem.IsLegendarySetItem())
                {
                    if (UniqueLegendaryHelper.TryGetLegendarySetInfo(magicItem.SetID, out var setInfo))
                    {
                        sets.Add(setInfo);
                    }
                }
            }

            return sets;
        }

        public static float GetTotalActiveMagicEffectValue(this Player player, string effectType, float scale = 1.0f)
        {
            return scale * (EquipmentEffectCache.Get(player, effectType, () =>
            {
                List<MagicItemEffect> allEffects = player.GetAllActiveMagicEffects(effectType);
                return allEffects.Count > 0 ? (float?)allEffects.Select(x => x.EffectValue).Sum() : null;
            }) ?? 0);
        }

        public static bool HasActiveMagicEffect(this Player player, string effectType)
        {
            return null != EquipmentEffectCache.Get(player, effectType, () =>
            {
                List<MagicItemEffect> allEffects = player.GetAllActiveMagicEffects(effectType);
                return allEffects.Count > 0 ? (float?)allEffects.Select(x => x.EffectValue).Sum() : null;
            });
        }

        public static List<ItemDrop.ItemData> GetEquippedSetPieces(this Player player, string setName)
        {
            return player.GetEquipment().Where(x => x.IsPartOfSet(setName)).ToList();
        }

        public static bool HasEquipmentOfType(this Player player, ItemDrop.ItemData.ItemType type)
        {
            return player.GetEquipment().Exists(x => x != null && x.m_shared.m_itemType == type);
        }

        public static ItemDrop.ItemData GetEquipmentOfType(this Player player, ItemDrop.ItemData.ItemType type)
        {
            return player.GetEquipment().FirstOrDefault(x => x != null && x.m_shared.m_itemType == type);
        }

        public static Player GetPlayerWithEquippedItem(ItemDrop.ItemData itemData)
        {
            return Player.m_players.FirstOrDefault(player => player.IsItemEquiped(itemData));
        }
    }

    public static class ItemBackgroundHelper
    {
        public static Image CreateAndGetMagicItemBackgroundImage(GameObject elementGo, GameObject equipped, bool isInventoryGrid)
        {
            var magicItemTransform = (RectTransform)elementGo.transform.Find("magicItem");
            if (magicItemTransform == null)
            {
                var magicItemObject = Object.Instantiate(equipped, equipped.transform.parent);
                magicItemObject.transform.SetSiblingIndex(EpicLoot.HasAuga ? equipped.transform.GetSiblingIndex() : equipped.transform.GetSiblingIndex() + 1);
                magicItemObject.name = "magicItem";
                magicItemObject.SetActive(true);
                magicItemTransform = (RectTransform)magicItemObject.transform;
                magicItemTransform.anchorMin = magicItemTransform.anchorMax = new Vector2(0.5f, 0.5f);
                magicItemTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64);
                magicItemTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);
                magicItemTransform.pivot = new Vector2(0.5f, 0.5f);
                magicItemTransform.anchoredPosition = Vector2.zero;
                var magicItemInit = magicItemTransform.GetComponent<Image>();
                magicItemInit.color = Color.white;
                magicItemInit.raycastTarget = false;
            }

            // Also add set item marker
            if (isInventoryGrid)
            {
                var setItemTransform = (RectTransform)elementGo.transform.Find("setItem");
                if (setItemTransform == null)
                {
                    var setItemObject = Object.Instantiate(equipped, equipped.transform.parent);
                    setItemObject.transform.SetAsLastSibling();
                    setItemObject.name = "setItem";
                    setItemObject.SetActive(true);
                    setItemTransform = (RectTransform)setItemObject.transform;
                    setItemTransform.anchorMin = setItemTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    setItemTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64);
                    setItemTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);
                    setItemTransform.pivot = new Vector2(0.5f, 0.5f);
                    setItemTransform.anchoredPosition = Vector2.zero;
                    var setItemInit = setItemTransform.GetComponent<Image>();
                    setItemInit.raycastTarget = false;
                    setItemInit.sprite = EpicLoot.GetSetItemSprite();
                    setItemInit.color = ColorUtility.TryParseHtmlString(EpicLoot.GetSetItemColor(), out var color) ? color : Color.white;
                }
            }

            // Also change equipped image
            var equippedImage = equipped.GetComponent<Image>();
            if (equippedImage != null && (!isInventoryGrid || !EpicLoot.HasAuga))
            {
                equippedImage.sprite = EpicLoot.GetEquippedSprite();
                equippedImage.color = Color.white;
                equippedImage.raycastTarget = false;
                var rectTransform = equipped.RectTransform();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, equippedImage.sprite.texture.width);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, equippedImage.sprite.texture.height);
            }

            return magicItemTransform.GetComponent<Image>();
        }
    }

    //public void UpdateGui(Player player, ItemDrop.ItemData dragItem)
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
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

            foreach (var item in __instance.m_inventory.m_inventory)
            {
                var element = __instance.GetElement(item.m_gridPos.x, item.m_gridPos.y, __instance.m_inventory.GetWidth());
                if (element == null)
                {
                    EpicLoot.LogError($"Could not find element for item ({item.m_shared.m_name}: {item.m_gridPos}) in inventory: {__instance.m_inventory.m_name}");
                    continue;
                }

                var magicItem = ItemBackgroundHelper.CreateAndGetMagicItemBackgroundImage(element.m_go, element.m_equiped.gameObject, true);
                if (item.UseMagicBackground())
                {
                    magicItem.enabled = true;
                    magicItem.sprite = EpicLoot.GetMagicItemBgSprite();
                    magicItem.color = item.GetRarityColor();
                }

                var setItemTransform = element.m_go.transform.Find("setItem");
                if (setItemTransform != null)
                {
                    var setItem = setItemTransform.GetComponent<Image>();
                    if (setItem != null)
                    {
                        setItem.enabled = item.IsSetItem();
                    }
                }
            }
        }
    }

    //void UpdateIcons(Player player)
    [HarmonyPatch(typeof(HotkeyBar), nameof(HotkeyBar.UpdateIcons), typeof(Player))]
    public static class HotkeyBar_UpdateIcons_Patch
    {
        public static void Postfix(HotkeyBar __instance, List<HotkeyBar.ElementData> ___m_elements, List<ItemDrop.ItemData> ___m_items, Player player)
        {

            if (!__instance.name.Equals("HotKeyBar") || player == null || player.IsDead()) return;

            for (var index = 0; index < Player.m_localPlayer.GetInventory().m_width; index++)
            {
                var itemData = __instance.m_items.FirstOrDefault(x => x.m_gridPos.x.Equals(index));

                if (index < 0 || index >= __instance.m_elements.Count) continue;

                var element = __instance.m_elements[index];

                var magicItem =
                    ItemBackgroundHelper.CreateAndGetMagicItemBackgroundImage(element.m_go, element.m_equiped, false);
                if (itemData != null && itemData.UseMagicBackground())
                {
                    magicItem.enabled = true;
                    magicItem.sprite = EpicLoot.GetMagicItemBgSprite();
                    magicItem.color = itemData.GetRarityColor();
                }
                else
                {
                    magicItem.enabled = false;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetActionProgress))]
    public static class Player_GetActionProgress_Patch
    {
        public static void Postfix(Player __instance, ref string name)
        {
            if (__instance.m_actionQueue.Count > 0)
            {
                var equip = __instance.m_actionQueue[0];
                if (equip.m_type != Player.MinorActionData.ActionType.Reload)
                {
                    if (equip.m_duration > 0.5f)
                    {
                        name = equip.m_type == Player.MinorActionData.ActionType.Unequip ? "$hud_unequipping " + equip.m_item.GetDecoratedName() : "$hud_equipping " + equip.m_item.GetDecoratedName();
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
            var str = __instance.m_itemData.GetDecoratedName();
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
}
