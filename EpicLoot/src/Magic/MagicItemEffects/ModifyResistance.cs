using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public static class ModifyResistance
{
    // Prefix handler invoked by CharacterRpcDamageDispatch (victim-side incoming modifier).
    public static void ModifyIncoming(Character __instance, HitData hit)
    {
        if (__instance != Player.m_localPlayer)
        {
            return;
        }

        float elementalResistance = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
            MagicEffectType.AddElementalResistancePercentage, 0.01f);
        float physicalResistance = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
            MagicEffectType.AddPhysicalResistancePercentage, 0.01f);

        // elemental resistances
        hit.m_damage.m_fire *= GetCappedResistanceValue(Player.m_localPlayer,
            MagicEffectType.AddFireResistancePercentage, elementalResistance);
        hit.m_damage.m_frost *= GetCappedResistanceValue(Player.m_localPlayer,
            MagicEffectType.AddFrostResistancePercentage, elementalResistance);
        hit.m_damage.m_lightning *= GetCappedResistanceValue(Player.m_localPlayer,
            MagicEffectType.AddLightningResistancePercentage, elementalResistance);
        hit.m_damage.m_poison *= GetCappedResistanceValue(Player.m_localPlayer,
            MagicEffectType.AddPoisonResistancePercentage, elementalResistance);
        hit.m_damage.m_spirit *= GetCappedResistanceValue(Player.m_localPlayer,
            MagicEffectType.AddSpiritResistancePercentage, elementalResistance);

        // physical resistances
        hit.m_damage.m_blunt *= GetCappedResistanceValue(Player.m_localPlayer,
            MagicEffectType.AddBluntResistancePercentage, physicalResistance);
        hit.m_damage.m_slash *= GetCappedResistanceValue(Player.m_localPlayer,
            MagicEffectType.AddSlashingResistancePercentage, physicalResistance);
        hit.m_damage.m_pierce *= GetCappedResistanceValue(Player.m_localPlayer,
            MagicEffectType.AddPiercingResistancePercentage, physicalResistance);
        hit.m_damage.m_chop *= GetCappedResistanceValue(Player.m_localPlayer,
            MagicEffectType.AddChoppingResistancePercentage, physicalResistance);
    }

    private static float GetCappedResistanceValue(Player player, string effect, float additionalResistance = 0f)
    {
        return 1f - GetResistanceFraction(player, effect, additionalResistance);
    }

    public static string GetResistanceEffect(HitData.DamageType type)
    {
        switch (type)
        {
            case HitData.DamageType.Blunt: return MagicEffectType.AddBluntResistancePercentage;
            case HitData.DamageType.Slash: return MagicEffectType.AddSlashingResistancePercentage;
            case HitData.DamageType.Pierce: return MagicEffectType.AddPiercingResistancePercentage;
            case HitData.DamageType.Chop: return MagicEffectType.AddChoppingResistancePercentage;
            case HitData.DamageType.Fire: return MagicEffectType.AddFireResistancePercentage;
            case HitData.DamageType.Frost: return MagicEffectType.AddFrostResistancePercentage;
            case HitData.DamageType.Lightning: return MagicEffectType.AddLightningResistancePercentage;
            case HitData.DamageType.Poison: return MagicEffectType.AddPoisonResistancePercentage;
            case HitData.DamageType.Spirit: return MagicEffectType.AddSpiritResistancePercentage;
            default: return null;
        }
    }

    public static string GetGroupResistanceEffect(HitData.DamageType type)
    {
        switch (type)
        {
            case HitData.DamageType.Blunt:
            case HitData.DamageType.Slash:
            case HitData.DamageType.Pierce:
            case HitData.DamageType.Chop:
                return MagicEffectType.AddPhysicalResistancePercentage;
            case HitData.DamageType.Fire:
            case HitData.DamageType.Frost:
            case HitData.DamageType.Lightning:
            case HitData.DamageType.Poison:
            case HitData.DamageType.Spirit:
                return MagicEffectType.AddElementalResistancePercentage;
            default:
                return null;
        }
    }

    public static float GetResistanceCap(string effect)
    {
        Dictionary<string, float> cfg = MagicItemEffectDefinitions.GetEffectConfig(effect);
        if (cfg == null || !cfg.ContainsKey("MaxResistance"))
        {
            return 1f;
        }

        float resistanceCap = Mathf.Clamp01(cfg["MaxResistance"] / 100f);
        if (resistanceCap > 1f)
        {
            EpicLoot.LogWarning($"Resistance calculated to 100%, player immune. " +
                $"Suggested to reduce max resistance below 100 in your configuration.");
        }

        return resistanceCap;
    }

    public static float GetUncappedResistanceFraction(Player player, string effect, float additionalResistance = 0f)
    {
        return player.GetTotalActiveMagicEffectValue(effect, 0.01f) + additionalResistance;
    }

    public static float GetResistanceFraction(Player player, string effect, float additionalResistance = 0f)
    {
        return Mathf.Clamp(GetUncappedResistanceFraction(player, effect, additionalResistance), 0f, GetResistanceCap(effect));
    }
}
