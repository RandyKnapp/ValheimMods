using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDrawStaminaDrain))]
    public static class ModifyDrawStamina_ItemData_GetDrawStaminaDrain_Patch
    {
        public static void Prefix(ItemDrop.ItemData __instance, ref float __state)
        {
            __state = __instance.m_shared.m_attack.m_drawStaminaDrain;

            if (__instance.IsMagic(out var magicItem) &&
                magicItem.HasEffect(MagicEffectType.ModifyDrawStaminaUse))
            {
                float modifier = magicItem.GetTotalEffectValue(MagicEffectType.ModifyDrawStaminaUse, 0.01f);
                __instance.m_shared.m_attack.m_drawStaminaDrain *= 1.0f - modifier;
            }
        }

        public static void Postfix(ItemDrop.ItemData __instance, ref float __state)
        {
            __instance.m_shared.m_attack.m_drawStaminaDrain = __state;
        }
    }
}