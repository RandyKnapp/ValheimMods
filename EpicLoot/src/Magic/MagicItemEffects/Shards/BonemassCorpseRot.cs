using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Detonates a poison cloud on enemies the local player kills
    public static class BonemassCorpseRot {
        // Seconds between detonations, the burst radius, and the poison dealt per point of shard value
        // (20 -> 5..25 becomes 100..500). All tunable in this effect's Config block in
        // config/shardstones.json, under these key names.
        public const float DefaultCooldown = 5f;
        public const float DefaultRadius = 2f;
        public const float DefaultPoisonPerTier = 20f;

        private const string CooldownKey = "Cooldown";
        private const string RadiusKey = "Radius";
        private const string PoisonPerTierKey = "PoisonPerTier";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { CooldownKey, DefaultCooldown },
            { RadiusKey, DefaultRadius },
            { PoisonPerTierKey, DefaultPoisonPerTier },
        };

        // Floored just above zero: a ttl of 0 is "no timeout" to vanilla, which would gate the shard
        // permanently rather than removing the cooldown.
        private static float GetCooldown() {
            return Mathf.Max(0.1f,
                EffectConfig.Get(MagicEffectType.CorpseRot, CooldownKey, DefaultCooldown));
        }

        private const string ExplosionFx = "vfx_BombBlob_explode_poison";

        private static bool _fxMissingLogged;

        // Cooldown indicator
        private const string CooldownName = "EL_BonemassPoisonCooldown";
        private static readonly int CooldownHash = CooldownName.GetStableHashCode();
        private static StatusEffect _cooldownIndicator;
        private static bool _cooldownMissingLogged;

        // Detonates a poison cloud on enemies the local player kills. Mirrors vanilla's own last-hit attribution
        // (Character.OnDeath, m_lastHit.GetAttacker() == local player).
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class OnDeath_Patch {
            [UsedImplicitly]
            private static void Postfix(Character __instance) {
                var player = Player.m_localPlayer;
                if (player == null || __instance == player
                    || __instance.m_lastHit?.GetAttacker() != player) {
                    return;
                }

                if (!player.HasActiveMagicEffect(MagicEffectType.CorpseRot, out var value) || value <= 0f) {
                    return;
                }

                // Rate limit: the cooldown indicator doubles as the gate (mirrors ElderForestsAid).
                // It was wired up but never engaged, so every kill detonated with no cooldown.
                if (player.GetSEMan().HaveStatusEffect(CooldownHash)) {
                    return;
                }

                var center = __instance.GetCenterPoint();
                SpawnExplosionFx(__instance.transform.position);
                DamageInRadius.DamageEnemiesInRadius(player, center,
                    EffectConfig.Get(MagicEffectType.CorpseRot, RadiusKey, DefaultRadius),
                    new HitData.DamageTypes {
                        m_poison = value * EffectConfig.Get(MagicEffectType.CorpseRot,
                            PoisonPerTierKey, DefaultPoisonPerTier)
                    });
                ShowCooldown(player);
            }
        }

        private static void SpawnExplosionFx(Vector3 position) {
            var prefab = ZNetScene.instance?.GetPrefab(ExplosionFx);
            if (prefab == null) {
                if (!_fxMissingLogged) {
                    EpicLoot.LogWarning($"BonemassPoison: could not find '{ExplosionFx}' prefab; poison burst visual will not display.");
                    _fxMissingLogged = true;
                }
                return;
            }

            Object.Instantiate(prefab, position, Quaternion.identity);
        }

        // The ttl is stamped here rather than at construction because the prototype is built once and
        // cached, so a retuned cooldown would otherwise not take hold until the next game session.
        private static void ShowCooldown(Player player) {
            var indicator = GetOrCreateCooldownIndicator();
            if (indicator != null) {
                indicator.m_ttl = GetCooldown();
                player.GetSEMan().AddStatusEffect(indicator, true);
            }
        }

        private static StatusEffect GetOrCreateCooldownIndicator() {
            if (_cooldownIndicator != null) {
                return _cooldownIndicator;
            }

            var icon = ObjectDB.instance?.GetItemPrefab("TrophyBonemass")?
                .GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null) {
                if (!_cooldownMissingLogged) {
                    EpicLoot.LogWarning("BonemassPoison: could not find 'TrophyBonemass' icon; cooldown indicator will not display.");
                    _cooldownMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<StatusEffect>();
            se.name = CooldownName;
            se.m_name = "$mod_epicloot_se_corpserot";
            se.m_icon = icon;
            se.m_ttl = GetCooldown(); // restamped on every proc by ShowCooldown
            se.m_cooldownIcon = true;
            _cooldownIndicator = se;
            return _cooldownIndicator;
        }
    }
}
