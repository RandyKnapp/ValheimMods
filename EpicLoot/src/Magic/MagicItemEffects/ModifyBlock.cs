using EpicLoot.General;
using HarmonyLib;
using System;

namespace EpicLoot.MagicItemEffects;

public static class ModifyBlock
{
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDeflectionForce), typeof(int))]
    private static class ModifyParry_ItemData_GetDeflectionForce_Patch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (!RunModifyBlockPatchs(__instance))
            {
                return;
            }

            float modifiedValue = GetDeflectionForceValue(Player.m_localPlayer, __instance, __result, ModifyParryWindow.IsParry());
            __result = modifiedValue;
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBaseBlockPower), typeof(int))]
    private static class ModifyParry_ItemDrop_ItemData_GetBaseBlockPower_Patch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (!RunModifyBlockPatchs(__instance))
            {
                return;
            }

            float modifiedValue = GetBlockPowerValue(Player.m_localPlayer, __instance, __result, ModifyParryWindow.IsParry());
            __result = modifiedValue;
        }
    }

    /// <summary>
    /// Helper method to determine calculation for tooltip.
    /// </summary>
    public static bool RunModifyBlockPatchs(ItemDrop.ItemData item)
    {
        if (Player.m_localPlayer == null || Player.m_localPlayer.GetCurrentBlocker() != item)
        {
            return false;
        }

        return true;
    }

    public static float GetDeflectionForceValue(Player player, ItemDrop.ItemData item, float originalValue, bool didParry)
    {
        float multipliers = GetMultiplier(player, MagicEffectType.ModifyBlockForce, item);

        if (didParry)
        {
            multipliers += GetMultiplier(player, MagicEffectType.ModifyParry, item);
        }

        return originalValue +
            (originalValue * multipliers) +
            GetDuelistDeflectionBonus(player, item);
    }

    public static float GetBlockPowerValue(Player player, ItemDrop.ItemData item, float originalValue, bool didParry)
    {
        float multipliers = GetMultiplier(player, MagicEffectType.ModifyBlockPower, item);

        if (didParry)
        {
            multipliers += GetMultiplier(player, MagicEffectType.ModifyParry, item);
        }

        return originalValue +
            (originalValue * multipliers) +
            GetDuelistBlockBonus(player, item);
    }

    public static float GetMultiplier(Player player, string effectName, ItemDrop.ItemData item)
    {
        float multiplier = 0f;
        ModifyWithLowHealth.Apply(player, effectName, effect =>
        {
            multiplier += MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, item, effect, 0.01f);
        });

        return multiplier;
    }

    public static float GetDuelistDeflectionBonus(Player player, ItemDrop.ItemData item)
    {
        // TODO: check validity when using two handed weapons
        float duelistBonus = 0f;
        if ((player == null || player.m_leftItem == null) &&
            MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, item, MagicEffectType.Duelist, out float duelistValue, 0.01f))
        {
            HitData.DamageTypes damage = ModifyDamage.GetDamageWithMagicEffects(item);
            duelistBonus = damage.EpicLootGetTotalDamage() / 2 * duelistValue;
        }

        return duelistBonus;
    }

    public static float GetDuelistBlockBonus(Player player, ItemDrop.ItemData item)
    {
        // TODO: check validity when using two handed weapons
        float duelistBonus = 0f;
        if ((player == null || player.m_leftItem == null) &&
            MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, item, MagicEffectType.Duelist, out float duelistValue, 0.01f))
        {
            HitData.DamageTypes damage = ModifyDamage.GetDamageWithMagicEffects(item);
            duelistBonus = damage.EpicLootGetTotalDamage() * duelistValue;
        }

        return duelistBonus;
    }
}