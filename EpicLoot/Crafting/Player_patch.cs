using HarmonyLib;

namespace EpicLoot.Crafting
{
    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
    public static class Player_AddKnownItem_Patch
    {
        public static bool Prefix(ItemDrop.ItemData item)
        {
            if (item.IsMagicCraftingMaterial())
            {
                var variant = EpicLoot.GetRarityIconIndex(item.GetCraftingMaterialRarity());
                item.m_variant = variant;
            }
            return true;
        }
    }
}
