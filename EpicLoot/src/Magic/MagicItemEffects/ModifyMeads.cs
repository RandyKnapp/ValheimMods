using EpicLoot.MagicItemEffects;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace EpicLoot.Magic.MagicItemEffects
{
    [HarmonyPatch]
    public static class ModifyMeads
    {
        /// <summary>
        /// Changes the stack value of the status effect to be passed into the AddStatusEffect method
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions);
            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ItemDrop.ItemData.SharedData),
                        nameof(ItemDrop.ItemData.m_shared.m_consumeStatusEffect))),
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(OpCodes.Ldc_R4),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(SEMan), nameof(SEMan.AddStatusEffect), new System.Type[]
                    {
                        typeof(StatusEffect), typeof(bool), typeof(int), typeof(float)
                    })))
                .ThrowIfNotMatch("Unable to patch Player.ConsumeItem for Mead Effects.")
                .Advance(1)
                .InsertAndAdvance(Transpilers.EmitDelegate(ModifyMead));
            return codeMatcher.Instructions();
        }

        private static StatusEffect ModifyMead(StatusEffect original)
        {
            StatusEffect dbEffect = ObjectDB.instance.GetStatusEffect(original.NameHash());
            if (Player.m_localPlayer == null ||
                dbEffect is not SE_Stats seStats ||
                !seStats.CanAdd(Player.m_localPlayer))
            {
                return original;
            }

            SE_Stats returnEffect = seStats;

            if (Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.InstantMead) &&
                ModifyWithLowHealth.PlayerHasLowHealth(Player.m_localPlayer))
            {
                returnEffect = TryCreateInstantMead(returnEffect);
            }

            if (Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.DecreaseMeadCooldown, out float effectValue, 0.01f))
            {
                returnEffect = TryCreateDecreasedCooldownMead(returnEffect, effectValue);
            }

            return returnEffect;
        }

        private static SE_Stats TryCreateInstantMead(SE_Stats effect)
        {
            if (!HasOverTimeValues(effect))
            {
                return effect;
            }

            SE_Stats newStatusEffect = (SE_Stats)effect.Clone();
            newStatusEffect.m_healthUpFront += newStatusEffect.m_healthOverTime;
            newStatusEffect.m_staminaUpFront += newStatusEffect.m_staminaOverTime;
            newStatusEffect.m_eitrUpFront += newStatusEffect.m_eitrOverTime;
            newStatusEffect.m_healthOverTime = 0f;
            newStatusEffect.m_staminaOverTime = 0f;
            newStatusEffect.m_eitrOverTime = 0f;

            return newStatusEffect;
        }

        private static SE_Stats TryCreateDecreasedCooldownMead(SE_Stats effect, float value)
        {
            if (!HasOverTimeValues(effect))
            {
                return effect;
            }

            SE_Stats newStatusEffect = (SE_Stats)effect.Clone();
            newStatusEffect.m_ttl *= Mathf.Clamp01(1f - value);

            return newStatusEffect;
        }

        private static bool HasOverTimeValues(SE_Stats se)
        {
            bool hasHealth = se.m_healthOverTime > 0f;
            bool hasStamina = se.m_staminaOverTime > 0f;
            bool hasEitr = se.m_eitrOverTime > 0f;

            return hasHealth || hasStamina || hasEitr;
        }
    }
}
