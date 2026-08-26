using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Detonates a poison cloud on enemies the local player kills
    public static class BonemassCorpseRot {
        private const float Cooldown = 5f;
        private const float CorpseRadius = 2f;
        private const float PoisonPerTier = 20f; // 20 poison damage per point of shard value (5..25 -> 100..500)
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
                DamageInRadius.DamageEnemiesInRadius(player, center, CorpseRadius,
                    new HitData.DamageTypes { m_poison = value * PoisonPerTier });
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

        private static void ShowCooldown(Player player) {
            var indicator = GetOrCreateCooldownIndicator();
            if (indicator != null) {
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
            se.m_ttl = Cooldown;
            se.m_cooldownIcon = true;
            _cooldownIndicator = se;
            return _cooldownIndicator;
        }
    }
}
