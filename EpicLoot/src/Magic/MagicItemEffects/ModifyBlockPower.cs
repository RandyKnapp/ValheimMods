using System;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public float GetBaseBlockPower(int quality) => this.m_shared.m_blockPower + (float) Mathf.Max(0, quality - 1) * this.m_shared.m_blockPowerPerLevel;
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBaseBlockPower), typeof(int))]
    public static class ModifyBlockPower_ItemData_GetBaseBlockPower_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);
            var totalBlockPowerMod = 0f;
            ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyBlockPower, effect =>
            {
                totalBlockPowerMod += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, __instance, effect, 0.01f);
            });

            __result *= 1.0f + totalBlockPowerMod;

            if (player != null && player.m_leftItem == null && player.HasActiveMagicEffect(MagicEffectType.Duelist))
            {
                __result += __instance.GetDamage().GetTotalDamage() * player.GetTotalActiveMagicEffectValue(MagicEffectType.Duelist, 0.01f);
            }

            __result = (float)Math.Round(__result, 1);
        }
    }
}