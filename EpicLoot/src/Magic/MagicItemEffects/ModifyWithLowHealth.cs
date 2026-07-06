using EpicLoot.General;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Mono.Security.X509.X520;

namespace EpicLoot.MagicItemEffects
{
    public static class ModifyWithLowHealth
    {
        public static float LowHealthDefaultThreshold = 0.3f;

        public static void Apply(Player player, string name, Action<string> action)
        {
            action(name);
            if (PlayerHasLowHealth(player))
            {
                action(EffectNameWithLowHealth(name));
            }
        }

        public static void ApplyOnlyForLowHealth(Player player, string name, Action<string> action)
        {
            if (PlayerHasLowHealth(player))
            {
                action(EffectNameWithLowHealth(name));
            }
        }

        public static string EffectNameWithLowHealth(string name)
        {
            return name + "LowHealth";
        }

        public static bool PlayerHasLowHealth(Player player)
        {
            return player != null && player.GetHealth() / player.GetMaxHealth() < Mathf.Min(GetLowHealthPercentage(player), 1.0f);
        }

        public static float GetLowHealthPercentage(Player player)
        {
            float lowHealthThreshold = LowHealthDefaultThreshold;

            if (player == null)
            {
                return lowHealthThreshold;
            }
            
            if (player.HasActiveMagicEffect(MagicEffectType.ModifyLowHealth, out float effectValue, 0.01f))
            {
                lowHealthThreshold += effectValue;
                Dictionary<string, float> lowhealthcfg = MagicItemEffectDefinitions.AllDefinitions[MagicEffectType.ModifyLowHealth].Config;
                if (lowhealthcfg != null && lowhealthcfg.ContainsKey("Max") && lowhealthcfg["Max"] < lowHealthThreshold)
                {
                    lowHealthThreshold = lowhealthcfg["Max"];
                }
            }
            
            return lowHealthThreshold;
        }

        public static bool PlayerWillBecomeHealthCritical(Player player, HitData hit)
        {
            if (PlayerHasLowHealth(player))
            {
                return true;
            }

            float lowHealthPercentage = Mathf.Min(ModifyWithLowHealth.GetLowHealthPercentage(player), 1.0f) * player.GetMaxHealth();
            float currentHealth = player.GetHealth();
            float hitTotalDamage = hit.m_damage.EpicLootGetTotalDamageAgainstPlayer();

            float armorValue = player.GetBodyArmor();
            hitTotalDamage = HitData.DamageTypes.ApplyArmor(hitTotalDamage, armorValue);

            if ((currentHealth - hitTotalDamage) < lowHealthPercentage)
            {
                return true;
            }

            return false;
        }
    }
}
