using EpicLoot.LootBeams;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Crafting
{
    [HarmonyPatch(typeof(ItemDrop), "Awake")]
    public static class ItemDrop_Awake_Patch
    {
        public static void Postfix(ItemDrop __instance)
        {
            if (__instance.m_itemData.IsMagicCraftingMaterial())
            {
                var particleContainer = __instance.transform.Find("Particles");
                if (particleContainer != null)
                {
                    particleContainer.gameObject.AddComponent<AlwaysPointUp>();
                }

                var rarity = __instance.m_itemData.GetCraftingMaterialRarity();
                var magicColor = EpicLoot.GetRarityColor(rarity);
                var variant = EpicLoot.GetRarityIconIndex(rarity);

                if (ColorUtility.TryParseHtmlString(magicColor, out var rgbaColor))
                {
                    __instance.gameObject.AddComponent<BeamColorSetter>().SetColor(rgbaColor);
                }

                __instance.m_itemData.m_variant = variant;
            }
        }
    }
}
