using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public float GetDeflectionForce(int quality)
    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetDeflectionForce", typeof(int))]
    public static class ModifyParry_ItemData_GetDeflectionForce_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
	        var totalParryMod = 0f;
	        ModifyWithLowHealth.Apply(Player.m_localPlayer, MagicEffectType.ModifyParry, effect =>
	        {
		        if (__instance.IsMagic() && __instance.GetMagicItem().HasEffect(effect))
		        {
			        totalParryMod += __instance.GetMagicItem().GetTotalEffectValue(effect, 0.01f);
		        }
	        });
            __result *= 1.0f + totalParryMod;
        }
    }

    //public override bool BlockAttack(HitData hit, Character attacker)
    [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
    public static class ModifyParry_Humanoid_BlockAttack_Patch
    {
        public static bool Override;
        public static float OriginalValue;

        public static bool Prefix(Humanoid __instance, HitData hit, Character attacker)
        {
            Override = false;
            OriginalValue = -1;

            ItemDrop.ItemData currentBlocker = __instance.GetCurrentBlocker();
            if (currentBlocker == null || !(__instance is Player player))
            {
                return true;
            }

            var totalParryBonusMod = 0f;
			ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyParry, effect =>
            {
	            if (currentBlocker.IsMagic() && currentBlocker.GetMagicItem().HasEffect(effect))
	            {
		            if (!Override)
		            {
			            Override = true;
			            OriginalValue = currentBlocker.m_shared.m_timedBlockBonus;
		            }

		            totalParryBonusMod += currentBlocker.GetMagicItem().GetTotalEffectValue(effect, 0.01f);
	            }
            });
		    currentBlocker.m_shared.m_timedBlockBonus *= 1.0f + totalParryBonusMod;

            return true;
        }

        public static void Postfix(Humanoid __instance, HitData hit, Character attacker)
        {
            ItemDrop.ItemData currentBlocker = __instance.GetCurrentBlocker();
            if (currentBlocker != null && Override)
            {
                currentBlocker.m_shared.m_timedBlockBonus = OriginalValue;
            }

            Override = false;
            OriginalValue = -1;
        }
    }
}