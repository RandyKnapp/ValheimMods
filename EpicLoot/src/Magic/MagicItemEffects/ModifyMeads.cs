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
                    // resetTime / itemLevel / skillLevel of the SEMan.AddStatusEffect call below.
                    // Player.ConsumeItem loads m_consumeStatusEffect three times; only this one is
                    // followed by these literals, so they identify the site on their own. Matching
                    // the callvirt's exact overload as well is what broke when the Sept 2026 update
                    // appended a `short variant` parameter, so it is deliberately not matched here.
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Ldc_I4_0),
                    new CodeMatch(OpCodes.Ldc_R4));
            if (codeMatcher.IsInvalid)
            {
                EpicLoot.LogWarningForce("Mead effects: could not find where Player.ConsumeItem adds a mead's status " +
                    "effect, so Instant Mead and Meads Cooldown will not work. Another mod may have changed that method.");
                return codeMatcher.Instructions();
            }

            codeMatcher.Advance(1)
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
                (ModifyWithLowHealth.PlayerHasLowHealth(Player.m_localPlayer) || AutoMeads.DrinkingForCriticalHit))
            {
                returnEffect = TryCreateInstantMead(returnEffect);
            }

            // Read off the base mead, not returnEffect: an Instant Mead copy has moved its restoration up front,
            // so it no longer looks like a mead whose duration is a cooldown, and the cut used to be skipped.
            if (HasOverTimeValues(seStats) &&
                Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.DecreaseMeadCooldown, out float effectValue, 0.01f))
            {
                // Never stamp the shared ObjectDB copy; the Instant Mead copy is already this drink's own.
                if (returnEffect == seStats)
                {
                    returnEffect = (SE_Stats)seStats.Clone();
                }
                returnEffect.m_ttl = seStats.m_ttl * Mathf.Clamp01(1f - effectValue);
            }

            return returnEffect;
        }

        /// <summary>
        /// How long a drunk mead's effect lasts before Potion Efficacy: its base duration, cut by Meads Cooldown
        /// for a mead that restores over time, whose duration is its cooldown. The same cut ModifyMead applies.
        /// </summary>
        public static float GetDrinkDuration(Player player, StatusEffect consumeEffect)
        {
            StatusEffect dbEffect = ObjectDB.instance?.GetStatusEffect(consumeEffect.NameHash()) ?? consumeEffect;
            float duration = dbEffect.m_ttl;
            if (dbEffect is SE_Stats seStats && HasOverTimeValues(seStats) &&
                player.HasActiveMagicEffect(MagicEffectType.DecreaseMeadCooldown, out float effectValue, 0.01f))
            {
                duration *= Mathf.Clamp01(1f - effectValue);
            }

            return duration;
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

        private static bool HasOverTimeValues(SE_Stats se)
        {
            bool hasHealth = se.m_healthOverTime > 0f;
            bool hasStamina = se.m_staminaOverTime > 0f;
            bool hasEitr = se.m_eitrOverTime > 0f;

            return hasHealth || hasStamina || hasEitr;
        }
    }
}
