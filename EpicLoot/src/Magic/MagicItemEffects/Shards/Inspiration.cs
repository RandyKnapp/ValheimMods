using System.Collections.Generic;
using System.Linq;
using EpicLoot.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using SkillType = Skills.SkillType;
using Random = UnityEngine.Random;

namespace EpicLoot.MagicItemEffects.Shards {
    // Golden head: every scrap of skill XP the player earns carries a small chance to "inspire" them,
    // dumping a lump of bonus XP into a randomly chosen skill weighted toward the ones they are worst at.
    //
    // *** VALUE CONVENTION EXCEPTION -- READ BEFORE TOUCHING ***
    // Almost every shard effect authors its value as a whole-number percent and reads it back with the
    // 0.01f scale. Inspiration does NOT: its grid value (10/15/20/25/30) is a count of RAW SKILL
    // ACCUMULATOR POINTS, read with no scale at all. "Fixing" this for consistency would nerf the effect
    // by 100x. The only percent here is the proc chance, which lives in the Config block.
    public static class Inspiration {
        // Percent chance to proc on any single skill-XP gain. Deliberately tiny: Skills.RaiseSkill fires
        // roughly once a second while sprinting or swimming, plus once per connecting swing, block, jump
        // and dodge -- on the order of 1000-2500 calls in an hour of ordinary play. 0.25% lands at a
        // handful of procs an hour, which is what makes each one feel like an event rather than a passive
        // XP faucet. Exposed as Config so it can be retuned without a rebuild.
        public const float DefaultProcChance = 0.25f;

        private const string ProcChanceKey = "ProcChance";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float>
        {
            { ProcChanceKey, DefaultProcChance },
        };

        // Skills below this level are never chosen. Level 0-1 skills are usually ones the player has
        // barely touched by accident, and vanilla's curve makes the first two levels nearly free anyway.
        private const float MinSkillLevel = 2f;
        // Weight = (100 - level)^2, so level 2 weighs 9604 and level 99 weighs 1. Squared rather than
        // linear because a linear ramp still hands ~10% of procs to skills where the grant is a rounding
        // error; the point of the effect is to pull up whatever is lagging.
        private const float LevelWeightExponent = 2f;
        // Vanilla's Skills.c_MaxSkillLevel (private const there).
        private const float MaxSkillLevel = 100f;
        // Hard stop on the level-walk loop. Nothing should get near this -- the only way to spin is a
        // degenerate zero-cost level requirement.
        private const int MaxLevelUpsPerProc = 20;
        // The needed/perFactor round trip can land an ULP short of the boundary and silently fail to
        // level. The overshoot is discarded by vanilla anyway, so padding it costs nothing.
        private const float LevelEpsilon = 0.001f;

        // Tooltip: "Inspiration: {1}% Chance of +{0} Skill XP" -- {1} surfaces the configured proc chance.
        // Pure, as the provider contract requires (MagicItem.RegisterDisplayValues): it only reads config.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Inspiration,
                value => new object[] { value, GetProcChance() });
        }

        // Postfix rather than prefix: a prefix would grant before vanilla's own raise landed, so a proc
        // that happened to pick the same skill would append to an accumulator vanilla is about to touch,
        // and the level-up messages would come out in the wrong order. Running after means vanilla has
        // fully settled and the skill's live state is exactly what we read.
        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class RaiseSkill_Patch {
            [HarmonyPostfix]
            [UsedImplicitly]
            private static void Postfix(Skills __instance, SkillType skillType) {
                // Our own grant calls re-enter RaiseSkill; without this the effect feeds on itself.
                if (SkillXpGrant.InProgress || skillType == SkillType.None) {
                    return;
                }

                var player = __instance.m_player;
                if (player == null || player != Player.m_localPlayer) {
                    return;
                }

                // NOTE: no 0.01f -- see the value convention warning at the top of this file.
                var points = player.GetTotalActiveMagicEffectValue(MagicEffectType.Inspiration);
                if (points <= 0f) {
                    return;
                }

                if (Random.value >= GetProcChance() * 0.01f) {
                    return;
                }

                var target = PickTargetSkill(__instance);
                if (target == null) {
                    return;
                }

                player.Message(MessageHud.MessageType.TopLeft, "$mod_epicloot_msg_inspiration", 0,
                    target.m_info.m_icon);
                Grant(__instance, target, points);
            }
        }

        // GetSkillList() hands back the live Skill objects out of the private m_skillData dictionary, so
        // no reflection is needed and mutations through them are real. It only contains skills the player
        // has actually trained, which is the same set the >= level 2 filter wants anyway.
        private static Skills.Skill PickTargetSkill(Skills skills) {
            var candidates = skills.GetSkillList()
                .Where(s => s?.m_info != null
                            && s.m_info.m_skill != SkillType.None
                            && s.m_level >= MinSkillLevel
                            && s.m_level < MaxSkillLevel)
                .ToList();

            if (candidates.Count == 0) {
                return null;
            }

            return new WeightedRandomCollection<Skills.Skill>(candidates,
                s => Mathf.Pow(Mathf.Max(0.01f, MaxSkillLevel - s.m_level), LevelWeightExponent)).Roll();
        }

        // Hands the skill `points` raw accumulator points, spread over as many RaiseSkill calls as it
        // takes to consume them.
        //
        // This is the whole reason the effect needs code rather than a single RaiseSkill call:
        // Skills.Skill.Raise zeroes m_accumulator when it levels, discarding whatever was left over. One
        // call with a big factor therefore grants exactly ONE level no matter how large the grant is.
        // Walking to each boundary in turn, charging only what the boundary actually cost, is what lets a
        // Mythic proc carry a low skill through several levels at once -- and routing through RaiseSkill
        // (rather than writing m_level) is what makes vanilla fire OnSkillLevelup and the "$msg_skillup"
        // message for each one, so the player sees the whole run.
        private static void Grant(Skills skills, Skills.Skill skill, float points) {
            // Raise() adds m_increseStep * factor * m_skillGainRate, so this is the accumulator points a
            // factor of 1.0 buys for this particular skill.
            var perFactor = skill.m_info.m_increseStep * Game.m_skillGainRate;
            if (perFactor <= 0f) {
                return; // a server with SkillGainRate 0, or a SkillDef with no increase step
            }

            var type = skill.m_info.m_skill;
            var remaining = points;

            SkillXpGrant.InProgress = true;
            try {
                var guard = 0;
                // skill is a live reference, so m_level / m_accumulator are re-read fresh each pass.
                while (remaining > 0f && skill.m_level < MaxSkillLevel && guard++ < MaxLevelUpsPerProc) {
                    var needed = Mathf.Max(0f, NextLevelRequirement(skill.m_level) - skill.m_accumulator);
                    if (needed > remaining) {
                        break; // can't reach the next boundary; fall through to the top-up below
                    }

                    skills.RaiseSkill(type, (needed + LevelEpsilon) / perFactor);
                    remaining -= needed; // the epsilon's overshoot is discarded, so don't charge for it
                }

                if (remaining > 0f && skill.m_level < MaxSkillLevel) {
                    skills.RaiseSkill(type, remaining / perFactor);
                }
            }
            finally {
                SkillXpGrant.InProgress = false;
            }
        }

        // Mirrors Skills.Skill.GetNextLevelRequirement, which is private.
        private static float NextLevelRequirement(float level) {
            return Mathf.Pow(Mathf.Floor(level + 1f), 1.5f) * 0.5f + 0.5f;
        }

        private static float GetProcChance() {
            var cfg = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.Inspiration);
            if (cfg != null && cfg.TryGetValue(ProcChanceKey, out var raw)) {
                return Mathf.Max(0f, raw);
            }
            return DefaultProcChance;
        }
    }
}
