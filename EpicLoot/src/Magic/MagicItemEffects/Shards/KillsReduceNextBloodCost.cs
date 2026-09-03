using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a status effect on kill which reduces the health cost of the next blood attack. The reduction is banked and can be stacked up to a cap, and expires after a duration.
    public static class KillsReduceNextBloodCost {
        // Cap on the banked reduction (1 = kills can bank up to a fully-free next blood attack), and how
        // many seconds the bank lasts before lapsing -- refreshed on every kill. Tunable as "MaxReduction"
        // and "BuffDuration" in this effect's Config block in config/shardstones.json.
        public const float DefaultMaxReduction = 1f;
        public const float DefaultBuffDuration = 30f;

        private const string MaxReductionKey = "MaxReduction";
        private const string BuffDurationKey = "BuffDuration";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { MaxReductionKey, DefaultMaxReduction },
            { BuffDurationKey, DefaultBuffDuration },
        };

        // The bank is a fraction of the cost, so anything above 1 is meaningless and anything below 0
        // would turn kills into a surcharge.
        private static float GetMaxReduction() {
            return EffectConfig.GetClamped(MagicEffectType.KillsReduceNextBloodCost,
                MaxReductionKey, DefaultMaxReduction, 0f, 1f);
        }

        // Floored just above zero: a ttl of 0 is "no timeout" to vanilla, which would make the bank permanent.
        private static float GetBuffDuration() {
            return Mathf.Max(0.1f, EffectConfig.Get(MagicEffectType.KillsReduceNextBloodCost,
                BuffDurationKey, DefaultBuffDuration));
        }

        private const string BuffName = "EL_BloodRite";
        private static readonly int BuffHash = BuffName.GetStableHashCode();
        private static SE_BloodRite _buffPrototype;
        private static bool _iconMissingLogged;

        // Banked fraction (0-1) to shave off the local player's next attack health cost.
        private static float _bankedReduction;

        // Read live by SE_BloodRite for its icon text and removal check.
        public static float BankedReduction => _bankedReduction;

        public static void ClearBank() => _bankedReduction = 0f;

        [HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
        private static class OnDeath_Patch {
            [UsedImplicitly]
            private static void Postfix(Character __instance) {
                if (Player.m_localPlayer == null || __instance == Player.m_localPlayer
                    || __instance.m_lastHit?.GetAttacker() != Player.m_localPlayer) {
                    return;
                }

                var perKill = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.KillsReduceNextBloodCost, 0.01f);
                if (perKill > 0f) {
                    _bankedReduction = Mathf.Min(GetMaxReduction(), _bankedReduction + perKill);
                    ApplyOrRefreshBuff(Player.m_localPlayer);
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.UseHealth))]
        private static class UseHealth_Patch {
            [UsedImplicitly]
            private static void Prefix(Character __instance, ref float hp) {
                if (__instance != Player.m_localPlayer || hp <= 0f || _bankedReduction <= 0f) {
                    return;
                }

                hp *= 1f - Mathf.Clamp01(_bankedReduction);
                ClearBank();
                // The buff notices the empty bank on the next SEMan tick (SE_BloodRite.IsDone) and removes
                // itself; nothing to do here.
            }
        }

        // Shows the buff on the first banked kill, or just refreshes its countdown while it is already up.
        // The discount itself is read live from the bank, so there is no per-instance state to restamp.
        private static void ApplyOrRefreshBuff(Player player) {
            var duration = GetBuffDuration();
            var seMan = player.GetSEMan();
            if (seMan.GetStatusEffect(BuffHash) is SE_BloodRite existing) {
                // Stamped on the live instance too, so a retune shortens or extends a bank already running.
                existing.m_ttl = duration;
                existing.ResetTime();
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (prototype != null) {
                // The prototype is built once and cached, so the ttl is restamped here rather than at
                // construction -- otherwise a retuned duration would not take hold until the next session.
                prototype.m_ttl = duration;
                seMan.AddStatusEffect(prototype);
            }
        }

        // Lazily builds the buff prototype. Runs on a kill, so the asset bundle is loaded. A null icon would
        // render as an invisible HUD entry (SEMan only surfaces effects with an icon), so if the sprite
        // lookup fails we log once and leave the prototype null -- the discount still works, it just has no
        // icon, and with it the expiry (owned by the buff) simply doesn't apply.
        private static SE_BloodRite GetOrCreatePrototype() {
            if (_buffPrototype != null) {
                return _buffPrototype;
            }

            // The DarkPurple (Blood Magic) shardstone's own icon -- same sprite the shard items use
            // (see Shards.cs).
            var icon = EpicAssets.AssetBundle?.LoadAsset<Sprite>("Assets/EpicLoot/Sprites/Shardstones/DarkPurple.png");
            if (icon == null) {
                if (!_iconMissingLogged) {
                    EpicLoot.LogWarning("KillsReduceNextBloodCost: could not load the DarkPurple shardstone sprite; Blood Rite will not display.");
                    _iconMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<SE_BloodRite>();
            se.name = BuffName;
            se.m_name = "$mod_epicloot_se_bloodrite";
            se.m_icon = icon;
            se.m_ttl = GetBuffDuration(); // restamped on every apply/refresh by ApplyOrRefreshBuff
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
