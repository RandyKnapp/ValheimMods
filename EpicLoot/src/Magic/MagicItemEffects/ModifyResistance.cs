using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public static class ModifyResistance
{
    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    private static class Character_RPC_Damage_Patch
    {
        private static void Prefix(Character __instance, HitData hit)
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
    }

    private static float GetCappedResistanceValue(Player player, string effect, float additionalResistance = 0f)
    {
        float resistance = player.GetTotalActiveMagicEffectValue(effect, 0.01f) + additionalResistance;
        float resistanceCap;

        Dictionary<string, float> cfg = MagicItemEffectDefinitions.GetEffectConfig(effect);
        if (cfg == null || !cfg.ContainsKey("MaxResistance"))
        {
            resistanceCap = 1f;
        }
        else
        {
            resistanceCap = Mathf.Clamp01(cfg["MaxResistance"] / 100f);
            if (resistanceCap > 1f)
            {
                EpicLoot.LogWarning($"Resistance calculated to 100%, player immune. " +
                    $"Suggested to reduce max resistance below 100 in your configuration.");
            }
        }

        resistance = Mathf.Clamp(resistance, 0f, resistanceCap);

        return 1f - resistance;
    }
}
