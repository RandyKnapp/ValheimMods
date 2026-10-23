using EpicLoot.Config;
using EpicLoot.General;
using System;
using System.Collections.Generic;
using UnityEngine;

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

        /// <summary>
        /// The enchantments that only act at critical health: every ...LowHealth variant (which includes the
        /// ModifyLowHealth threshold) and the two meads that fire there. The suffix is matched rather than a
        /// list kept, so a variant added later, or one another mod registers, is covered too.
        /// </summary>
        public static bool IsHealthCriticalEffect(string effectType)
        {
            return effectType != null &&
                (effectType.EndsWith("LowHealth", StringComparison.Ordinal) ||
                 effectType == MagicEffectType.InstantMead || effectType == MagicEffectType.AutoMead);
        }

        /// <summary>False for a health critical effect while the Health Critical Enchantments config is off.</summary>
        public static bool MayRoll(string effectType)
        {
            return ELConfig.HealthCriticalEffectsEnabled == null || ELConfig.HealthCriticalEffectsEnabled.Value ||
                !IsHealthCriticalEffect(effectType);
        }

        public static bool PlayerHasLowHealth(Player player)
        {
            return player != null && player.GetHealth() / player.GetMaxHealth() <= Mathf.Min(GetLowHealthPercentage(player), 1.0f);
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

        public static float GetLowHealthDisplayPercent(Player player)
        {
            return Mathf.Round(Mathf.Clamp01(GetLowHealthPercentage(player)) * 1000f) / 10f;
        }

        public static bool PlayerHasHealthCriticalEffects(Player player)
        {
            return player != null && player.GetAllActiveMagicEffects().Exists(x =>
                x.EffectType != MagicEffectType.ModifyLowHealth && IsHealthCriticalEffect(x.EffectType));
        }

        // Expects the hit as it stands after the block, resistances and armor (see
        // SharedPlayerPostArmorDamagePatch), so it only applies what vanilla still does to it before the damage
        // lands: fire, poison and spirit are split off into damage over time, the rest is scaled by the world's
        // damage-taken rate, and a creature's world-level bonus is added flat (HitData.GetTotalDamage).
        public static bool PlayerWillBecomeHealthCritical(Player player, HitData hit)
        {
            if (PlayerHasLowHealth(player))
            {
                return true;
            }

            float lowHealthPercentage = Mathf.Min(ModifyWithLowHealth.GetLowHealthPercentage(player), 1.0f) * player.GetMaxHealth();
            float currentHealth = player.GetHealth();

            HitData.DamageTypes damage = hit.m_damage;
            float typedDamage = damage.GetTotalDamage();
            float instantDamage = (typedDamage - damage.m_fire - damage.m_poison - damage.m_spirit) * Game.m_localDamgeTakenRate +
                (hit.GetTotalDamage() - typedDamage);

            return currentHealth - instantDamage < lowHealthPercentage;
        }

        // A damage-over-time tick (burning, poison, smoke) as Character.ApplyDamage is handed it: unlike a hit it
        // lands whole, fire and poison included, scaled only by the world's damage-taken rate.
        public static bool PlayerWillBecomeHealthCriticalFromTick(Player player, HitData hit)
        {
            if (PlayerHasLowHealth(player))
            {
                return true;
            }

            float lowHealthPercentage = Mathf.Min(ModifyWithLowHealth.GetLowHealthPercentage(player), 1.0f) * player.GetMaxHealth();
            float typedDamage = hit.m_damage.GetTotalDamage();
            float damage = typedDamage * Game.m_localDamgeTakenRate + (hit.GetTotalDamage() - typedDamage);

            return player.GetHealth() - damage < lowHealthPercentage;
        }
    }
}
