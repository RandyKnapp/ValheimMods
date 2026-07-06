using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace EpicLoot.MagicItemEffects;

public static class ModifyPlayerRegen
{
    [HarmonyPatch]
    private static class SEMan_ModifyRegen_Patches
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
        private static IEnumerable<CodeInstruction> ModifyHealthRegen_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions);
                codeMatcher.MatchStartForward(
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Mul),
                        new CodeMatch(OpCodes.Stloc_S),
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Ldc_I4_1),
                        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Character), nameof(Character.Heal))))
                    .ThrowIfNotMatch("Unable to patch vanilla pattern of Player.UpdateFood for AddHealthRegen effect. " +
                        "Trying alternative pattern match...")
                    .Advance(1)
                    .InsertAndAdvance(Transpilers.EmitDelegate(AddHealthTicks));

                    return codeMatcher.Instructions();
            }
            catch (Exception e)
            {
                EpicLoot.Log(e.Message);
            }

            // Steady Regeneration mod support
            try
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions);
                codeMatcher.MatchStartForward(
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Mul),
                        new CodeMatch(OpCodes.Stloc_S),
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(OpCodes.Ldc_R4),
                        new CodeMatch(OpCodes.Mul),
                        new CodeMatch(OpCodes.Ldc_I4_0),
                        new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Character), nameof(Character.Heal))))
                    .ThrowIfNotMatch("SteadyRegeneration mod support patch: Unable to patch Player.UpdateFood for AddHealthRegen effect.")
                    .Advance(1)
                    .InsertAndAdvance(Transpilers.EmitDelegate(AddHealthTicks));

                return codeMatcher.Instructions();
            }
            catch (Exception e)
            {
                EpicLoot.Log(e.Message);
            }

            EpicLoot.LogErrorForce("Mod conflict detected! Modify Health Regen effects WILL NOT WORK!");

            return instructions;
        }

        private static float AddHealthTicks(float value)
        {
            return value + Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.AddHealthRegen);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyHealthRegen))]
        private static void ModifyHealthRegen_Postfix(SEMan __instance, ref float regenMultiplier)
        {
            DoPostfix(__instance, MagicEffectType.ModifyHealthRegen, ref regenMultiplier);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyStaminaRegen))]
        private static void ModifyStaminaRegen_Postfix(SEMan __instance, ref float staminaMultiplier)
        {
            DoPostfix(__instance, MagicEffectType.ModifyStaminaRegen, ref staminaMultiplier);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyEitrRegen))]
        private static void ModifyEitrRegen_Postfix(SEMan __instance, ref float eitrMultiplier)
        {
            DoPostfix(__instance, MagicEffectType.ModifyEitrRegen, ref eitrMultiplier);
        }
    }

    private static void DoPostfix(SEMan __instance, string magicEffect, ref float __result)
    {
        if (__instance.m_character != Player.m_localPlayer)
        {
            return;
        }

        __result += GetModifyRegenValue(Player.m_localPlayer, magicEffect);
    }

    public static float GetModifyRegenValue(Player player, string magicEffect)
    {
        var regenValue = 0f;
        ModifyWithLowHealth.Apply(player, magicEffect, effect =>
        {
            regenValue = player.GetTotalActiveMagicEffectValue(effect, 0.01f);
        });

        return regenValue;
    }

    /// <summary>
    /// Helper function primarily for the tooltip.
    /// </summary>
    public static float GetModifiedRegenValue(ItemDrop.ItemData item, string magicEffect, float originalValue)
    {
        if (item.HasMagicEffect(magicEffect))
        {
            return originalValue + GetModifyRegenValue(Player.m_localPlayer, magicEffect);
        }

        return originalValue;
    }
}