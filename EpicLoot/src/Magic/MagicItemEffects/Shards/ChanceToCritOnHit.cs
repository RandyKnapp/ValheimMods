using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class ChanceToCritOnHit {
        // Kept apart from ChanceDoubleDamage's multiplier despite the matching default, so the two can be
        // retuned separately.
        public const float DefaultDamageMultiplier = 2f;

        private const string DamageMultiplierKey = "DamageMultiplier";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { DamageMultiplierKey, DefaultDamageMultiplier },
        };

        // Floored at 1: a multiplier below 1 would make a crit weaker than an ordinary hit.
        private static float GetDamageMultiplier() {
            return Mathf.Max(1f, EffectConfig.Get(MagicEffectType.ChanceToCritOnHit,
                DamageMultiplierKey, DefaultDamageMultiplier));
        }

        static GameObject effect;
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.ChanceToCritOnHit,
                value => new object[] { value, GetDamageMultiplier() });
        }

        // Prefix handler invoked by SharedCharacterDamagePatch (attacker-side outgoing modifier).
        public static void ModifyOutgoingHit(HitData hit, Character attacker) {
            if (hit == null || attacker != Player.m_localPlayer) {
                return;
            }

            var chance = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.ChanceToCritOnHit, 0.01f);
            if (chance > 0f && Random.value < chance) {
                hit.m_damage.Modify(GetDamageMultiplier());
                if (effect == null) {
                    effect = PrefabManager.Instance.GetPrefab("sfx_stonegolem_hurt");
                }
                if (effect != null) {
                    GameObject.Instantiate(effect, Player.m_localPlayer.transform.position, Quaternion.identity);
                }

            }
        }
    }
}
