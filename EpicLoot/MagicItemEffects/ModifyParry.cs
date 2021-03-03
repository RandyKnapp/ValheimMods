using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public float GetDeflectionForce(int quality)
    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetDeflectionForce", typeof(int))]
    public static class ModifyParry_ItemData_GetDeflectionForce_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (__instance.IsMagic() && __instance.GetMagicItem().HasEffect(MagicEffectType.ModifyParry))
            {
                var totalParryMod = __instance.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyParry, 0.01f);
                __result *= 1.0f + totalParryMod;
            }
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
            if (currentBlocker == null)
            {
                return true;
            }

            if (currentBlocker.IsMagic() && currentBlocker.GetMagicItem().HasEffect(MagicEffectType.ModifyParry))
            {
                Override = true;
                OriginalValue = currentBlocker.m_shared.m_timedBlockBonus;

                var totalParryBonusMod = currentBlocker.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyParry, 0.01f);
                currentBlocker.m_shared.m_timedBlockBonus *= 1.0f + totalParryBonusMod;
            }

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