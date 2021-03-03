using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public float GetWeight() => this.m_shared.m_weight * (float) this.m_stack;
    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetWeight")]
    public static class ReduceWeight_ItemData_GetWeight_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.HasMagicEffect(MagicEffectType.Weightless))
            {
                __result = 0;
            }
            else if (__instance.HasMagicEffect(MagicEffectType.ReduceWeight))
            {
                var totalWeightReduction = __instance.GetMagicItem().GetTotalEffectValue(MagicEffectType.ReduceWeight, 0.01f);
                __result *= 1.0f - totalWeightReduction;
            }
        }
    }
}