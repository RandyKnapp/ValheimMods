using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a chance to crit on hit, applying a damage multiplier on crit.
    public static class ChanceToCritOnHit {
        // Damage multiplier applied on a successful crit. Tunable; 2 = double damage on crit. This is distinct
        // in intent from ChanceDoubleDamage (a Fortune-shard proc) even though the default multiplier matches.
        private const float CritDamageMultiplier = 2f;
        static GameObject effect;
        // Tooltip: "{0}% Chance to Crit for {1}x Damage" -- {1} surfaces the crit multiplier from the const.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.ChanceToCritOnHit,
                value => new object[] { value, CritDamageMultiplier });
        }

        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        public static void ModifyOutgoingHit(HitData hit, Character attacker) {
            if (hit == null || attacker != Player.m_localPlayer) {
                return;
            }

            var chance = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.ChanceToCritOnHit, 0.01f);
            if (chance > 0f && Random.value < chance) {
                hit.m_damage.Modify(CritDamageMultiplier);
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
