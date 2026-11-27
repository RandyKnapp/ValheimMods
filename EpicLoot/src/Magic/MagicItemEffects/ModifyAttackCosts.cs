using EpicLoot.MagicItemEffects.Shards;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public static class ModifyAttackCosts
{
    // The share of its cost an attack, dodge or jump always pays, however many reductions stack. Without a floor they
    // add past 100%: an attack cost turns negative and hands back the resource it should spend, and a dodge or jump
    // goes free. It covers all of them, so it lives in the Global block of shardstones.json ("CostFloorPercent"),
    // resolved once per config load by EffectConfig.ApplyGlobalConfig.
    public const float DefaultCostFloorPercent = 10f;
    public static float CostFloor { get; private set; } = DefaultCostFloorPercent * 0.01f;

    public static void RefreshGlobalConfig()
    {
        CostFloor = Mathf.Clamp01(EffectConfig.Global("CostFloorPercent", DefaultCostFloorPercent) * 0.01f);
    }

    [HarmonyPatch]
    private static class ModifyAttackCost_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackHealth))]
        private static void GetAttackHealth_Postfix(Attack __instance, ref float __result)
        {
            // UseHealth takes the banked kill discount off this figure afterwards.
            DoPostfix(__instance.m_character, __instance.m_weapon, MagicEffectType.ModifyAttackHealthUse,
                KillsReduceNextBloodCost.GetNextCostShare(__instance.m_character), ref __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
        private static void GetAttackStamina_Postfix(Attack __instance, ref float __result)
        {
            DoPostfix(__instance.m_character, __instance.m_weapon, MagicEffectType.ModifyAttackStaminaUse, 1f, ref __result);
        }

        // GetAttackEitr() is a thin wrapper over GetAttackEitr(Character, ItemData), so the name alone is
        // ambiguous; its siblings above have one overload each.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), typeof(Character), typeof(ItemDrop.ItemData))]
        private static void GetAttackEitr_Postfix(Character character, ItemDrop.ItemData weapon, ref float __result)
        {
            // UseEitr takes ReduceEitrCost off this figure afterwards.
            DoPostfix(character, weapon, MagicEffectType.ModifyAttackEitrUse, ReduceEitrCost.GetCostShare(character), ref __result);
        }
    }

    // Vanilla works a dodge or jump out as base * (1 - movement modifier + this modifier), so the movement modifier
    // decides the least this one can be. Last, because several effects lower the dodge modifier. The grappling hook's
    // pull reads the jump modifier with no status effect step after it, so this is the floor it gets.
    [HarmonyPatch]
    private static class MovementCostFloor_Patches
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentDodgeStaminaModifier))]
        private static void GetEquipmentDodgeStaminaModifier_Postfix(Player __instance, ref float __result)
        {
            __result = Mathf.Max(__result, CostFloor - 1f + __instance.GetEquipmentMovementModifier());
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentJumpStaminaModifier))]
        private static void GetEquipmentJumpStaminaModifier_Postfix(Player __instance, ref float __result)
        {
            __result = Mathf.Max(__result, CostFloor - 1f + __instance.GetEquipmentMovementModifier());
        }

        // Status effects come off after the modifiers above. minZero is false only for the tooltip's total, which
        // passes a base of 1 rather than a real cost.
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyDodgeStaminaUsage))]
        private static void ModifyDodgeStaminaUsage_Postfix(SEMan __instance, ref float staminaUse, bool minZero)
        {
            if (minZero && __instance.m_character is Player player)
            {
                staminaUse = Mathf.Max(staminaUse, CostFloor * player.m_dodgeStaminaUsage);
            }
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyJumpStaminaUsage))]
        private static void ModifyJumpStaminaUsage_Postfix(SEMan __instance, ref float staminaUse, bool minZero)
        {
            if (minZero && __instance.m_character is Player player)
            {
                staminaUse = Mathf.Max(staminaUse, CostFloor * player.m_jumpStaminaUsage);
            }
        }
    }

    // laterShare is what a reduction applied after this figure leaves of it, so the floor holds for what is paid.
    private static void DoPostfix(Character character, ItemDrop.ItemData weapon, string magicEffect, float laterShare,
        ref float __result)
    {
        if (__result == 0f || character != Player.m_localPlayer)
        {
            return;
        }

        float share = GetModifyAttackValue(Player.m_localPlayer, weapon, magicEffect);
        __result *= laterShare > 0f ? Mathf.Max(share, CostFloor / laterShare) : share;
    }

    public static float GetModifyAttackValue(Player player, ItemDrop.ItemData item, string magicEffect)
    {
        return GetEffectPercentage(MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
            player, item, magicEffect, 0.01f));
    }

    public static float GetEffectPercentage(float effectValue)
    {
        return Mathf.Max(CostFloor, 1.0f - effectValue);
    }
}
