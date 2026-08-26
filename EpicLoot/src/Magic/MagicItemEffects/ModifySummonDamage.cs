using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

// Summon damage bonus. The summoner's client stamps the bonus on the summon's ZDO at spawn (an
// owner-side write); the actual scaling happens per outgoing hit in SharedCharacterDamagePatch,
// which runs on the attacker-owner's client. The old implementation instead added the bonus onto
// the minion weapon's m_shared.m_attack.m_damageMultiplier -- shared data, so the buff applied to
// EVERY humanoid using that weapon prefab (any Greydwarf that spawned while the effect was worn,
// not just summons), stacked again on every zone reload once the ZDO value persisted, and was
// written to ZDOs this client did not own.
public static class ModifySummonDamage
{
    public const string ZdoKey = "el-msd";

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Start))]
    public static class SetupSummonDamagePatch
    {
        public static void Postfix(Humanoid __instance)
        {
            if (__instance.IsPlayer() || Player.m_localPlayer == null)
            {
                return;
            }

            if (__instance.m_nview == null || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner())
            {
                return;
            }

            // Only actual summons: tamed, and levelled by a magic skill (mirrors ModifySummonHealth).
            if (!__instance.IsTamed())
            {
                return;
            }

            Tameable tameable = __instance.GetComponent<Tameable>();
            if (tameable == null ||
                (tameable.m_levelUpOwnerSkill != Skills.SkillType.BloodMagic &&
                 tameable.m_levelUpOwnerSkill != Skills.SkillType.ElementalMagic))
            {
                return;
            }

            // Already stamped (zone reload): keep the bonus from when it was summoned.
            if (__instance.m_nview.GetZDO().GetFloat(ZdoKey, 0f) > 0f)
            {
                return;
            }

            if (Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ModifySummonDamage, out float effectValue, 0.01f))
            {
                __instance.m_nview.GetZDO().Set(ZdoKey, effectValue);
            }
        }
    }

    // Called from SharedCharacterDamagePatch (attacker side): scale a summon's outgoing hit by its
    // stamped bonus. Per-hit scaling means nothing shared is mutated and nothing accumulates.
    public static void ModifyOutgoingHit(HitData hit, Character attacker)
    {
        if (attacker == null || attacker.IsPlayer())
        {
            return;
        }

        if (attacker.m_nview == null || !attacker.m_nview.IsValid())
        {
            return;
        }

        float bonus = attacker.m_nview.GetZDO().GetFloat(ZdoKey, 0f);
        if (bonus > 0f)
        {
            hit.ApplyModifier(1f + bonus);
        }
    }
}
