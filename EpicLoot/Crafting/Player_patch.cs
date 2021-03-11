using HarmonyLib;

namespace EpicLoot.Crafting
{
    //public void AddKnownItem(ItemDrop.ItemData item)
    [HarmonyPatch(typeof(Player), "AddKnownItem")]
    public static class Player_Patch
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
