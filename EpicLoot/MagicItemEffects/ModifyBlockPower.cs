using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public float GetBaseBlockPower(int quality) => this.m_shared.m_blockPower + (float) Mathf.Max(0, quality - 1) * this.m_shared.m_blockPowerPerLevel;
    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetBaseBlockPower", typeof(int))]
    public static class ModifyBlockPower_ItemData_GetBaseBlockPower_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.IsMagic() && __instance.GetMagicItem().HasEffect(MagicEffectType.ModifyBlockPower))
            {
                var totalBlockPowerMod = __instance.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyBlockPower, 0.01f);
                __result *= 1.0f + totalBlockPowerMod;
            }
        }
    }
}