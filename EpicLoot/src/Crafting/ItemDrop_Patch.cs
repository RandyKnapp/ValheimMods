using EpicLoot.Data;
using EpicLoot.LootBeams;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Crafting
{
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
    public static class ItemDrop_Awake_Patch
    {
        public static void Postfix(ItemDrop __instance)
        {
            bool isMagic = __instance.m_itemData.IsMagicCraftingMaterial();
            bool isRunestone = __instance.m_itemData.IsRunestone();
            bool isUnidentified = __instance.m_itemData.IsUnidentifiedMaterial();
            bool isChisel = __instance.m_itemData.IsShardSlotChisel();

            if (isMagic || isRunestone || isUnidentified || isChisel)
            {
                var particleContainer = __instance.transform.Find("Particles");
                if (particleContainer != null)
                {
                    particleContainer.gameObject.AddComponent<AlwaysPointUp>();
                }

                ItemRarity rarity = isRunestone ? __instance.m_itemData.GetRunestoneRarity() :
                    __instance.m_itemData.GetCraftingMaterialRarity();
                var magicColor = EpicLoot.GetRarityColor(rarity);
                var variant = isRunestone ? 0 : EpicLoot.GetRarityIconIndex(rarity);

                // Ensure unidenfitied items are loaded back up if they somehow become non-magical
                MagicItemComponent mi = __instance.m_itemData.Data().GetOrCreate<MagicItemComponent>();
                if (isUnidentified && mi.MagicItem == null)
                {
                    mi.SetMagicItem(new MagicItem
                    {
                        Rarity = rarity,
                        IsUnidentified = true,
                    });

                    mi.Save();
                }
                // Brokkr's Gift carries a cosmetic MagicItem purely for the rarity-coloured name and
                // background; heal it here so an instance that lost its custom data still renders as
                // its tier rather than as a plain grey item.
                else if (isChisel && mi.MagicItem == null)
                {
                    mi.SetMagicItem(new MagicItem { Rarity = rarity });
                    mi.Save();
                }

                if (ColorUtility.TryParseHtmlString(magicColor, out var rgbaColor))
                {
                    __instance.gameObject.AddComponent<BeamColorSetter>().SetColor(rgbaColor);
                }

                // Both carry a single authored icon rather than the ten-icon rarity array the
                // crafting materials use, so there is no variant to select.
                if (isUnidentified || isChisel)
                {
                    variant = 0;
                }

                __instance.m_itemData.m_variant = variant;
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load))]
    public static class Inventory_Load_Patch
    {
        public static void Postfix(Inventory __instance)
        {
            foreach (var item in __instance.m_inventory)
            {
                if (item.IsMagicCraftingMaterial())
                {
                    var rarity = item.GetCraftingMaterialRarity();
                    var variant = EpicLoot.GetRarityIconIndex(rarity);
                    item.m_variant = variant;
                }
            }
        }
    }
}
