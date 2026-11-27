using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

// Summon damage and health bonuses. The summoner's client stamps both on the summon's ZDO the first time it
// starts (it owns a summon it just spawned); damage is scaled per outgoing hit in SharedCharacterDamagePatch and
// health in ModifySummonHealth's SetMaxHealth prefix, both from the stamp, so a summon keeps its summoner's bonus
// whichever client owns it later. Scaling shared weapon data instead buffed every humanoid using that prefab.
public static class ModifySummonDamage
{
    public const string ZdoKey = "el-msd";
    public const string HealthZdoKey = "el-msh";
    private const string StampedZdoKey = "el-mss";

    internal static bool IsMagicSummon(Character character)
    {
        if (character == null || character.IsPlayer() || !character.IsTamed())
        {
            return false;
        }

        Tameable tameable = character.GetComponent<Tameable>();
        return tameable != null &&
            (tameable.m_levelUpOwnerSkill == Skills.SkillType.BloodMagic ||
             tameable.m_levelUpOwnerSkill == Skills.SkillType.ElementalMagic);
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Start))]
    public static class SetupSummonDamagePatch
    {
        public static void Postfix(Humanoid __instance)
        {
            if (Player.m_localPlayer == null || __instance.m_nview == null || !__instance.m_nview.IsValid() ||
                !__instance.m_nview.IsOwner() || !IsMagicSummon(__instance))
            {
                return;
            }

            ZDO zdo = __instance.m_nview.GetZDO();
            // A summon stamped before the flag existed carries a damage value instead.
            if (zdo.GetBool(StampedZdoKey) || zdo.GetFloat(ZdoKey, 0f) > 0f)
            {
                return;
            }

            zdo.Set(StampedZdoKey, true);
            if (Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ModifySummonDamage, out float damage, 0.01f))
            {
                zdo.Set(ZdoKey, damage);
            }

            if (Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ModifySummonHealth, out float health, 0.01f))
            {
                zdo.Set(HealthZdoKey, health);
                // Awake set the max health before the stamp existed; an unhurt summon gets it again with the bonus.
                if (Mathf.Approximately(__instance.GetHealth(), __instance.GetMaxHealth()))
                {
                    __instance.SetMaxHealth(__instance.GetMaxHealthBase() * __instance.GetLevel());
                }
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
