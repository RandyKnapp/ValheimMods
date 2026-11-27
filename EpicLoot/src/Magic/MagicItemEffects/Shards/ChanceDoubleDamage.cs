using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class ChanceDoubleDamage {
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

        static GameObject effect = null;
        public static void ModifyOutgoingHit(HitData hit, Character attacker) {
            if (!(attacker is Player player) || player != Player.m_localPlayer) {
                return;
            }

            // The shard is socketed into the weapon, so only that weapon procs. GetActiveWeapon prefers the
            // weapon of the attack in flight; GetCurrentWeapon alone returns the right hand first, so an
            // off-hand shard would never fire.
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
