using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public float GetBaseBlockPower(int quality) => this.m_shared.m_blockPower + (float) Mathf.Max(0, quality - 1) * this.m_shared.m_blockPowerPerLevel;
    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetBaseBlockPower", typeof(int))]
    public static class ModifyBlockPower_ItemData_GetBaseBlockPower_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
	        var totalBlockPowerMod = 0f;
	        Duelist.Apply(Player.m_localPlayer, MagicEffectType.ModifyBlockPower, effect =>
	        {
		        if (__instance.IsMagic() && __instance.GetMagicItem().HasEffect(effect))
		        {
			        totalBlockPowerMod += __instance.GetMagicItem().GetTotalEffectValue(effect, 0.01f);
		        }
	        });
            __result *= 1.0f + totalBlockPowerMod;
        }
    }
}