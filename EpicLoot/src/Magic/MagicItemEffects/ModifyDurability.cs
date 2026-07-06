using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetMaxDurability), typeof(int))]
    public static class ModifyDurability_ItemData_GetMaxDurability_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.IsMagic(out var magicItem) && magicItem.HasEffect(MagicEffectType.ModifyDurability))
            {
                var totalDurabilityMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyDurability, 0.01f);
                __result *= 1.0f + totalDurabilityMod;
            }
        }
    }
}
