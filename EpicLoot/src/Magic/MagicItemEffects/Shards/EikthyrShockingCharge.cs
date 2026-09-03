using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a charge that builds on combat hits and discharges as a forward lightning shockwave when full.
    public static class EikthyrShockingCharge {
        // All tunable in this effect's Config block in config/shardstones.json, under these key names.
        //
        // Hits required to trigger a discharge, and the portion of the banked combat damage the shockwave
        // delivers. Cone geometry: reach straight ahead, and the full width of the cone at that reach --
        // the half-width grows linearly from 0 at the player to (ConeMaxWidth / 2) at ConeLength. The
        // ignore window is how long after a discharge the shockwave's own hits are kept from rebuilding
        // the charge.
        public const int DefaultMaxCharges = 15;
        public const float DefaultDamageFraction = 0.3f;
        public const float DefaultConeLength = 4f;
        public const float DefaultConeMaxWidth = 4f;
        public const float DefaultDischargeIgnoreWindow = 0.3f;

        private const string MaxChargesKey = "MaxCharges";
        private const string DamageFractionKey = "DamageFraction";
        private const string ConeLengthKey = "ConeLength";
        private const string ConeMaxWidthKey = "ConeMaxWidth";
        private const string DischargeIgnoreWindowKey = "DischargeIgnoreWindow";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { MaxChargesKey, DefaultMaxCharges },
            { DamageFractionKey, DefaultDamageFraction },
            { ConeLengthKey, DefaultConeLength },
            { ConeMaxWidthKey, DefaultConeMaxWidth },
            { DischargeIgnoreWindowKey, DefaultDischargeIgnoreWindow },
        };

        // Clamped to at least 1 so a misconfiguration can't discharge on every hit through a zero threshold.
        private static int GetMaxCharges() {
            return EffectConfig.GetIntAtLeast(MagicEffectType.ShockingCharge,
                MaxChargesKey, DefaultMaxCharges, 1);
        }

        // Floored above zero: the cone test divides by the reach.
        private static float GetConeLength() {
            return Mathf.Max(0.1f,
                EffectConfig.Get(MagicEffectType.ShockingCharge, ConeLengthKey, DefaultConeLength));
        }

        private const string ShockwaveFx = "fx_eikthyr_forwardshockwave";

        private static int _charges;
        // Total non-pickaxe/chop damage banked from the hits that built the current charge; spent (and reset)
        // by the shockwave and cleared if the shard is unequipped.
        private static float _bankedDamage;
        private static float _ignoreUntil;
        private static bool _fxMissingLogged;
        private const string IndicatorName = "EL_ShockingChargeIndicator";
        private static readonly int IndicatorHash = IndicatorName.GetStableHashCode();
        private static StatusEffect _indicator;
        private static bool _indicatorMissingLogged;
        // Live charge state, read by SE_ShockingChargeIndicator for its icon text and removal check.
        public static int CurrentCharges => _charges;
        public static int MaxChargeCount => GetMaxCharges();

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            if (hit == null || attacker != Player.m_localPlayer || Time.time < _ignoreUntil) {
                return;
            }

            // Only hits on hostile targets build a charge -- the same friendly filter the discharge cone
            // applies, so whacking your own boar (or a player) can't wind up the shockwave.
            if (__instance == null || __instance.IsPlayer() || __instance.IsTamed()) {
                return;
            }

            var player = Player.m_localPlayer;
            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.ShockingCharge, 1f);
            if (value <= 0f) {
                // indicator self-removes via SE_ShockingChargeIndicator.IsDone
                _charges = 0;
                _bankedDamage = 0f;
                return;
            }

            // Only real combat damage builds a charge; pickaxe/chop damage (mining, chopping, and the
            // pick/chop portion of a mixed weapon hit) is excluded from both the trigger and the bank.
            var damage = hit.m_damage;
            var contributed = damage.GetTotalDamage() - damage.m_chop - damage.m_pickaxe;
            if (contributed <= 0f) {
                return;
            }

            _bankedDamage += contributed;

            if (++_charges < GetMaxCharges()) {
                ShowIndicator(player);
                return;
            }

            // Discharge: fire along this (final) attack's direction, spend the bank, and open the ignore
            // window before the shockwave's hits route back through this postfix.
            _charges = 0;
            _ignoreUntil = Time.time + EffectConfig.Get(MagicEffectType.ShockingCharge,
                DischargeIgnoreWindowKey, DefaultDischargeIgnoreWindow);
            var shotDamage = _bankedDamage * EffectConfig.Get(MagicEffectType.ShockingCharge,
                DamageFractionKey, DefaultDamageFraction);
            _bankedDamage = 0f;
            FireShockwave(player, hit.m_dir, shotDamage);
        }

        // Spawns the shockwave FX and applies the banked damage as a forward cone, both aligned to the final
        // attack's (horizontal) direction. Falls back to the player's facing if the attack direction is flat.
        private static void FireShockwave(Player player, Vector3 attackDir, float lightningDamage) {
            var dir = new Vector3(attackDir.x, 0f, attackDir.z);
            if (dir.sqrMagnitude < 1e-4f) {
                var forward = player.transform.forward;
                dir = new Vector3(forward.x, 0f, forward.z);
            }
            dir.Normalize();

            var origin = player.transform.position;
            SpawnShockwaveFx(origin, dir);
            DamageEnemiesInCone(player, origin, dir, lightningDamage);
        }

        private static void SpawnShockwaveFx(Vector3 origin, Vector3 dir) {
            var prefab = ZNetScene.instance?.GetPrefab(ShockwaveFx);
            if (prefab == null) {
                if (!_fxMissingLogged) {
                    EpicLoot.LogWarning($"EikthyrShockingCharge: could not find '{ShockwaveFx}' prefab; shockwave visual will not display.");
                    _fxMissingLogged = true;
                }
                return;
            }

            Object.Instantiate(prefab, origin, Quaternion.LookRotation(dir));
        }

        // Applies `lightningDamage` to every hostile character inside a cone that starts at `origin`, points
        // along `dir`, reaches ConeLength ahead and widens to ConeMaxWidth at that reach. The test is done in
        // the horizontal plane so the ground shockwave catches enemies regardless of small height offsets.
        private static void DamageEnemiesInCone(Player player, Vector3 origin, Vector3 dir, float lightningDamage) {
            if (lightningDamage <= 0f) {
                return;
            }

            var coneLength = GetConeLength();
            var halfMaxWidth = EffectConfig.Get(MagicEffectType.ShockingCharge,
                ConeMaxWidthKey, DefaultConeMaxWidth) * 0.5f;
            foreach (var character in Character.GetAllCharacters()) {
                if (character == null || character.IsPlayer() || character.IsTamed() || character.IsDead()) {
                    continue;
                }

                if (character.m_nview == null || !character.m_nview.IsValid()) {
                    continue;
                }

                var toTarget = character.transform.position - origin;
                toTarget.y = 0f;

                var along = Vector3.Dot(toTarget, dir);
                if (along <= 0f || along > coneLength) {
                    continue;
                }

                var perpendicular = toTarget - dir * along;
                var halfWidth = (along / coneLength) * halfMaxWidth;
                if (perpendicular.sqrMagnitude > halfWidth * halfWidth) {
                    continue;
                }

                var hit = new HitData {
                    m_point = character.GetCenterPoint(),
                    m_dir = dir,
                    m_ranged = true,
                };
                hit.m_damage.m_lightning = lightningDamage;
                hit.SetAttacker(player);
                character.Damage(hit);
            }
        }

        // Adds the charge HUD indicator to the player if it isn't already showing. AddStatusEffect clones
        // the prototype and no-ops when an effect with the same NameHash is present, so the HaveStatusEffect
        // guard just skips building the prototype on repeat hits.
        private static void ShowIndicator(Player player) {
            var seMan = player.GetSEMan();
            if (seMan.HaveStatusEffect(IndicatorHash)) {
                return;
            }

            var indicator = GetOrCreateIndicator();
            if (indicator != null) {
                seMan.AddStatusEffect(indicator);
            }
        }

        // Lazily builds the indicator prototype. Runs on a hit, so ObjectDB is loaded and the Eikthyr
        // trophy icon is available. A null icon would render as an invisible HUD entry (SEMan only surfaces
        // effects with an icon), so if the trophy lookup fails we log once and leave _indicator null.
        private static StatusEffect GetOrCreateIndicator() {
            if (_indicator != null) {
                return _indicator;
            }

            var icon = ObjectDB.instance?.GetItemPrefab("TrophyEikthyr")?
                .GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null) {
                if (!_indicatorMissingLogged) {
                    EpicLoot.LogWarning("EikthyrShockingCharge: could not find 'TrophyEikthyr' icon; charge indicator will not display.");
                    _indicatorMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<SE_ShockingChargeIndicator>();
            se.name = IndicatorName;
            se.m_name = "$mod_epicloot_se_shockingcharge";
            se.m_icon = icon;
            se.m_ttl = 0f;
            _indicator = se;
            return _indicator;
        }
    }
}
