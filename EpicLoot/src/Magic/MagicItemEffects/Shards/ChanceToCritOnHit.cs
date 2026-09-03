using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a chance to crit on hit, applying a damage multiplier on crit.
    public static class ChanceToCritOnHit {
        // Damage multiplier applied on a successful crit; 2 = double damage. This is distinct in intent from
        // ChanceDoubleDamage (a Fortune-shard proc) even though the default matches, so the two carry
        // separate Config blocks and can be retuned apart. Tunable as "DamageMultiplier" in this effect's
        // Config block in config/shardstones.json.
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
        // Tooltip: "{0}% Chance to Crit for {1}x Damage" -- {1} is the configured multiplier, so the shown
        // number follows a retune instead of staying at the baked-in default.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.ChanceToCritOnHit,
                value => new object[] { value, GetDamageMultiplier() });
        }

        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
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
