using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Lightning damage done by the player restores eitr, drawn from a pool. The pool refills at the player's normal
    // eitr regen rate, but it does not stop for attacks, dodges or spending as regen does, so once it is empty a
    // caster gains no more than that rate however much lightning lands. A higher value makes the pool larger and
    // takes more from it per point of lightning damage.
    public static class Conduit {
        // Lightning damage dealt per {value} eitr.
        public const float DefaultDamagePerTrigger = 200f;

        // Eitr the pool holds per point of value; each Balance Template sets its own.
        public const float DefaultPoolPerValue = 5f;

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { DamagePerTriggerKey, DefaultDamagePerTrigger },
            { PoolPerValueKey, DefaultPoolPerValue },
        };

        private const string DamagePerTriggerKey = "DamagePerTrigger";
        private const string PoolPerValueKey = "PoolPerValue";

        // Floored at 1 because the payout divides by this: a configured 0 would turn one hit into an unbounded payout.
        private static float GetDamagePerTrigger() {
            return Mathf.Max(1f,
                EffectConfig.Get(MagicEffectType.Conduit, DamagePerTriggerKey, DefaultDamagePerTrigger));
        }

        private static float GetPoolSize(float value) {
            return value * Mathf.Max(0f, EffectConfig.Get(MagicEffectType.Conduit, PoolPerValueKey, DefaultPoolPerValue));
        }

        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Conduit,
                value => new object[] { value, GetDamagePerTrigger(), GetPoolSize(value) });
        }

        // Eitr in the pool, and when it last refilled. Time passes while the effect is off too, so swapping gear gains
        // nothing: the next refill catches up on the whole gap.
        private static float _pool;
        private static float _lastRefillTime;

        private const string IndicatorName = "EL_ConduitPool";
        private static readonly int IndicatorHash = IndicatorName.GetStableHashCode();
        private const string IconItem = "Thunderstone";
        private static StatusEffect _indicator;
        private static bool _iconMissingLogged;

        public static bool IsActive(Player player) {
            return player != null && player.GetTotalActiveMagicEffectValue(MagicEffectType.Conduit) > 0f;
        }

        // The HUD readout, in whole eitr. The pool is kept up to date by the Player.Update postfix below.
        public static string GetPoolText() {
            var player = Player.m_localPlayer;
            if (player == null) {
                return "";
            }

            var size = GetPoolSize(player.GetTotalActiveMagicEffectValue(MagicEffectType.Conduit));
            return $"{Mathf.Min(_pool, size):0}/{size:0}";
        }

        // Player.UpdateStats' own eitr regen sum, without the parts that stop or slow it for combat.
        private static float GetRefillRate(Player player) {
            float maxEitr = player.GetMaxEitr();
            float rate = player.m_eiterRegen;
            if (maxEitr > 0f) {
                rate += (1f - player.GetEitr() / maxEitr) * player.m_eiterRegen;
            }

            float multiplier = 1f;
            player.m_seman.ModifyEitrRegen(ref multiplier);
            multiplier += player.GetEquipmentEitrRegenModifier();
            return Mathf.Max(0f, rate * multiplier);
        }

        private static void RefillPool(Player player, float value) {
            float now = Time.time;
            _pool = Mathf.Min(GetPoolSize(value), _pool + GetRefillRate(player) * (now - _lastRefillTime));
            _lastRefillTime = now;
        }

        // Postfix handler invoked by SharedCharacterDamagePatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker, float healthBeforeHit) {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || hit.m_damage.m_lightning <= 0f || attacker != player
                || __instance == player || __instance.IsPlayer() || __instance.IsTamed()) {
                return;
            }

            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.Conduit);
            if (value <= 0f) {
                return;
            }

            // Never more than the player has room for, so a full eitr bar leaves the pool alone.
            var wanted = hit.m_damage.m_lightning * MagicEffectsHelper.GetCountedDamageFraction(hit, healthBeforeHit)
                * value / GetDamagePerTrigger();
            var paid = Mathf.Min(wanted, _pool, player.GetMaxEitr() - player.GetEitr());
            if (paid > 0f) {
                _pool -= paid;
                player.AddEitr(paid);
            }
        }

        // AddStatusEffect clones the prototype and no-ops on a present NameHash; the guard skips even that.
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

        // A null icon would render as an invisible HUD entry, so a missing icon logs once and shows nothing.
        private static StatusEffect GetOrCreateIndicator() {
            if (_indicator != null) {
                return _indicator;
            }

            var icon = ObjectDB.instance?.GetItemPrefab(IconItem)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null) {
                if (!_iconMissingLogged) {
                    _iconMissingLogged = true;
                    EpicLoot.LogWarning($"[Conduit] could not find the '{IconItem}' icon; the HUD readout will not display.");
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<SE_ConduitPool>();
            se.name = IndicatorName;
            se.m_name = "$mod_epicloot_me_conduit_name";
            se.m_icon = icon;
            se.m_ttl = 0f;      // never expires on its own; IsDone removes it
            _indicator = se;
            return _indicator;
        }

        // Runs for as long as the effect is worn, so the pool and its readout move between hits, and a socket change
        // or a new character needs no hook of its own. The memoized effect read bails first for everyone else.
        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Player_Update_Patch {
            [UsedImplicitly]
            private static void Postfix(Player __instance) {
                if (__instance != Player.m_localPlayer || __instance.IsDead() || !IsActive(__instance)) {
                    return;
                }

                RefillPool(__instance, __instance.GetTotalActiveMagicEffectValue(MagicEffectType.Conduit));
                ShowIndicator(__instance);
            }
        }
    }
}
