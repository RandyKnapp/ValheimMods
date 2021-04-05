using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Attack), "Start")]
    class ModifyAttackSpeed_Attack_Start_Patch
    {
        public static void Postfix(Attack __instance, ref bool __result)
        {
            if (__result && __instance.m_weapon != null && __instance.m_weapon.IsMagic() && __instance.m_weapon.HasMagicEffect(MagicEffectType.ModifyAttackSpeed))
            {
                var effect = __instance.m_weapon.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyAttackSpeed, 0.01f);
                EpicLoot.Log($"Attack speed increased {effect:0.###}");
                __instance.m_zanim.SetSpeed(1.0f + effect);
            }
            else
            {
                __instance.m_zanim.SetSpeed(1.0f);
            }
        }
    }
}
