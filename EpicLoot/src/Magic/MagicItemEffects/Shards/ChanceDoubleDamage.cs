using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a chance to double the damage of an attack.
    public static class ChanceDoubleDamage {
        // Multiplier applied on a successful proc; 2 = double damage. Kept separate from
        // ChanceToCritOnHit's multiplier despite the matching default -- they are different shards and are
        // meant to be tunable apart. Tunable as "DamageMultiplier" in this effect's Config block in
        // config/shardstones.json.
        public const float DefaultDamageMultiplier = 2f;

        private const string DamageMultiplierKey = "DamageMultiplier";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { DamageMultiplierKey, DefaultDamageMultiplier },
        };

        // Floored at 1: a multiplier below 1 would make the proc a penalty.
        private static float GetDamageMultiplier() {
            return Mathf.Max(1f, EffectConfig.Get(MagicEffectType.ChanceDoubleDamage,
                DamageMultiplierKey, DefaultDamageMultiplier));
        }

        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        static GameObject effect = null;
        public static void ModifyOutgoingHit(HitData hit, Character attacker) {
            if (!(attacker is Player player) || player != Player.m_localPlayer) {
                return;
            }

            // The shard is socketed into the weapon, so only that weapon procs. GetActiveWeapon prefers
            // the weapon of the attack actually in flight and only falls back to GetCurrentWeapon, which
            // returns the right hand first -- without it a shard in the off-hand weapon never fires.
            var magicItem = MagicEffectsHelper.GetActiveWeapon(player)?.GetMagicItem();
            if (magicItem == null ||
                !magicItem.HasEffect(MagicEffectType.ChanceDoubleDamage, includeSocketed: true)) {
                return;
            }

            float chance = magicItem.GetTotalEffectValue(MagicEffectType.ChanceDoubleDamage, 0.01f);
            if (chance > 0f && Random.value < chance) {
                if (effect == null) {
                    effect = PrefabManager.Instance.GetPrefab("sfx_archery_target_hit");
                }
                if (effect != null) {
                    GameObject.Instantiate(effect, hit.m_point, Quaternion.identity);
                }
                DamageText.instance.ShowText(DamageText.TextType.Bonus, hit.m_point, $"+{Mathf.RoundToInt(hit.m_damage.EpicLootGetTotalDamage())}", true);
                hit.m_damage.Modify(GetDamageMultiplier());
            }
        }
    }
}
