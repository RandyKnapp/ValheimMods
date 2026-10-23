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

        // Tooltip: "Corpse Rot: {0} Poison" -- the same value * PoisonPerTier the burst deals, so the
        // shown number rises with the shard's rarity instead of the effect reading as a flat name.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.CorpseRot,
                value => new object[] {
                    value * EffectConfig.Get(MagicEffectType.CorpseRot,
                        PoisonPerTierKey, DefaultPoisonPerTier),
                });
        }

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

        // Player-ZDO key holding the local player's Corpse Rot value, mirrored by
        // Multiplayer_Player_Patch.UpdateRichesAndLuck on equip/unequip, so the victim's owner can tell whether a
        // kill needs reporting without an RPC for every death.
        public const string ZdoValueKey = "el-crot";

        private const string BurstRpc = "EL_CorpseRotBurst";

        // Who made the kill is only known where the victim dies: OnDeath runs on the victim's owner -- another
        // client or a dedicated server as often as the killer -- and m_lastHit is only set there. So the owner
        // tells the killer's own client, which bursts with its own shard value and cooldown. Testing for the
        // local player here, as this used to, only ever worked when the killer owned the victim. A prefix:
        // OnDeath ends by resetting the victim's ZDO, and it also runs from the death animation on every
        // client, where only the owner's call counts.
        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class OnDeath_Patch {
            [UsedImplicitly]
            private static void Prefix(Character __instance) {
                if (__instance.m_nview == null || !__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) {
                    return;
                }

                if (__instance.m_lastHit?.GetAttacker() is not Player killer || killer == __instance ||
                    killer.m_nview == null || !killer.m_nview.IsValid() ||
                    killer.m_nview.GetZDO().GetFloat(ZdoValueKey) <= 0f) {
                    return;
                }

                killer.m_nview.InvokeRPC(BurstRpc, __instance.GetCenterPoint(), __instance.transform.position);
            }
        }

        // Every player carries the RPC; only the local player's copy acts on it.
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch {
            [UsedImplicitly]
            private static void Postfix(Player __instance) {
                __instance.m_nview?.Register<Vector3, Vector3>(BurstRpc,
                    (sender, center, position) => Burst(__instance, center, position));
            }
        }

        private static void Burst(Player player, Vector3 center, Vector3 position) {
            if (player == null || player != Player.m_localPlayer) {
                return;
            }

            if (!player.HasActiveMagicEffect(MagicEffectType.CorpseRot, out var value) || value <= 0f) {
                return;
            }

            // Rate limit: the cooldown indicator doubles as the gate (mirrors ElderForestsAid).
            if (player.GetSEMan().HaveStatusEffect(CooldownHash)) {
                return;
            }

            SpawnExplosionFx(position);
            DamageInRadius.DamageEnemiesInRadius(player, center,
                EffectConfig.Get(MagicEffectType.CorpseRot, RadiusKey, DefaultRadius),
                new HitData.DamageTypes {
                    m_poison = value * EffectConfig.Get(MagicEffectType.CorpseRot,
                        PoisonPerTierKey, DefaultPoisonPerTier)
                });
            ShowCooldown(player);
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
