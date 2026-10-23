using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDrawStaminaDrain))]
    public static class ModifyDrawStamina_ItemData_GetDrawStaminaDrain_Patch
    {
        public static void Prefix(ItemDrop.ItemData __instance, ref float __state)
        {
            __state = __instance.m_shared.m_attack.m_drawStaminaDrain;

            float modifier = 0f;
            if (__instance.IsMagic(out var magicItem) &&
                magicItem.HasEffect(MagicEffectType.ModifyDrawStaminaUse, includeSocketed: true))
            {
                modifier = magicItem.GetTotalEffectValue(MagicEffectType.ModifyDrawStaminaUse, 0.01f);
            }

            // A set bonus lives on no single item, so it applies to whichever bow the local player holds.
            Player player = Player.m_localPlayer;
            if (player != null && player.IsItemEquiped(__instance))
            {
                modifier += MagicEffectsHelper.GetTotalActiveSetEffectValue(player, MagicEffectType.ModifyDrawStaminaUse, 0.01f);
            }

            if (modifier > 0f)
            {
                __instance.m_shared.m_attack.m_drawStaminaDrain *= 1.0f - Mathf.Min(1f, modifier);
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