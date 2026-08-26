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
                magicItem.HasEffect(MagicEffectType.ModifyDrawStaminaUse, includeSocketed: true))
            {
                float modifier = magicItem.GetTotalEffectValue(MagicEffectType.ModifyDrawStaminaUse, 0.01f);
                __instance.m_shared.m_attack.m_drawStaminaDrain *= 1.0f - modifier;
            }
        }

        // Finalizer, not postfix: the restore must run even when the original (or another mod's
        // patch) throws -- m_shared is the descriptor shared by every copy of the item.
        public static void Finalizer(ItemDrop.ItemData __instance, float __state)
        {
            __instance.m_shared.m_attack.m_drawStaminaDrain = __state;
        }
    }
}