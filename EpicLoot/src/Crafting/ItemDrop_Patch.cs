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
            if (!InitializeItemData(__instance.m_itemData, out var rarity))
            {
                return;
            }

            var particleContainer = __instance.transform.Find("Particles");
            if (particleContainer != null)
            {
                particleContainer.gameObject.AddComponent<AlwaysPointUp>();
            }

            if (ColorUtility.TryParseHtmlString(EpicLoot.GetRarityColor(rarity), out var rgbaColor))
            {
                __instance.gameObject.AddComponent<BeamColorSetter>().SetColor(rgbaColor);
            }
        }

        /// <summary>
        /// The ItemData half of the postfix above: the icon variant, and the MagicItem an unidentified
        /// item or Brokkr's Gift must carry. LootRoller's data-only drops never run Awake, so they call
        /// this directly. Returns false, touching nothing, for anything but those materials.
        /// </summary>
        public static bool InitializeItemData(ItemDrop.ItemData itemData, out ItemRarity rarity)
        {
            rarity = ItemRarity.Magic;
            bool isMagic = itemData.IsMagicCraftingMaterial();
            bool isRunestone = itemData.IsRunestone();
            bool isUnidentified = itemData.IsUnidentifiedMaterial();
            bool isChisel = itemData.IsShardSlotChisel();

            if (!isMagic && !isRunestone && !isUnidentified && !isChisel)
            {
                return false;
            }

            rarity = isRunestone ? itemData.GetRunestoneRarity() : itemData.GetCraftingMaterialRarity();
            var variant = isRunestone ? 0 : EpicLoot.GetRarityIconIndex(rarity);

            // Ensure unidenfitied items are loaded back up if they somehow become non-magical
            MagicItemComponent mi = itemData.Data().GetOrCreate<MagicItemComponent>();
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

            // Both carry a single authored icon rather than the ten-icon rarity array the
            // crafting materials use, so there is no variant to select.
            if (isUnidentified || isChisel)
            {
                variant = 0;
            }

            itemData.m_variant = variant;
            return true;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load), typeof(ZPackage))]
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
