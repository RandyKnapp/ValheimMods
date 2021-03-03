using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public float GetMaxDurability(int quality) => this.m_shared.m_maxDurability + (float) Mathf.Max(0, quality - 1) * this.m_shared.m_durabilityPerLevel;
    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetMaxDurability", typeof(int))]
    public static class ModifyDurability_ItemData_GetMaxDurability_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.IsMagic() && __instance.GetMagicItem().HasEffect(MagicEffectType.ModifyDurability))
            {
                var totalDurabilityMod = __instance.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyDurability, 0.01f);
                __result *= 1.0f + totalDurabilityMod;
            }
        }
    }
}
