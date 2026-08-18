using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
    public static class ModifyDurability_ItemData_GetMaxDurability_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            // Hot path: GetMaxDurability is called from UI/HUD render loops for every item, every frame.
            // Durability mods come only from this item, and a magic/extended item always has at least one
            // m_customData entry, so mundane items can bail before touching the data manager.
            if (__instance.m_customData.Count == 0)
            {
                return;
            }

            if (__instance.IsMagic(out var magicItem) && magicItem.HasEffect(MagicEffectType.ModifyDurability, includeSocketed: true))
            {
                var totalDurabilityMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyDurability, 0.01f);
                __result *= 1.0f + totalDurabilityMod;
            }
        }
    }
}
