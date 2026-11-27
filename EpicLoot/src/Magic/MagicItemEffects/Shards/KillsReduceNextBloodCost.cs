using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class KillsReduceNextBloodCost {
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

        private static readonly KillCredit _credit = new KillCredit();

        // Banked fraction (0-1) to shave off the next attack health cost of the player who banked it.
        private static float _bankedReduction;
        private static Player _bankOwner;

        // Read live by SE_BloodRite for its icon text and removal check.
        public static float BankedReduction => _bankOwner == Player.m_localPlayer ? _bankedReduction : 0f;

        public static void ClearBank() => _bankedReduction = 0f;

        // Credited from the attacker's own hit (the shared Character.Damage postfix): a creature's death, and the
        // m_lastHit that names its killer, only happen on the client that owns it, often someone else.
        public static void OnDamageDealt(Character victim, HitData hit, Character attacker) {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || attacker != player || victim == player
                || victim.IsPlayer() || victim.IsTamed()) {
                return;
            }

            var perKill = player.GetTotalActiveMagicEffectValue(MagicEffectType.KillsReduceNextBloodCost, 0.01f);
            if (perKill <= 0f || !_credit.TryClaim(victim, hit)) {
                return;
            }

            if (_bankOwner != player) {
                _bankOwner = player;
                _bankedReduction = 0f;
            }

            _bankedReduction = Mathf.Min(GetMaxReduction(), _bankedReduction + perKill);
            ApplyOrRefreshBuff(player);
        }

        // What the banked discount leaves of the next blood cost. Other characters have no bank.
        public static float GetNextCostShare(Character character) {
            return character == Player.m_localPlayer
                ? ModifyAttackCosts.GetEffectPercentage(BankedReduction)
                : 1f;
        }

        [HarmonyPatch(typeof(Character), nameof(Character.UseHealth))]
        private static class UseHealth_Patch {
            [UsedImplicitly]
            private static void Prefix(Character __instance, ref float hp) {
                if (__instance != Player.m_localPlayer || hp <= 0f || BankedReduction <= 0f) {
                    return;
                }

                hp *= GetNextCostShare(__instance);
                ClearBank();
            }
        }

        private static void ApplyOrRefreshBuff(Player player) {
            var duration = GetBuffDuration();
            var seMan = player.GetSEMan();
            if (seMan.GetStatusEffect(BuffHash) is SE_BloodRite existing) {
                existing.m_ttl = duration;
                existing.ResetTime();
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (prototype != null) {
                // Restamped because the cached prototype would not follow a retune.
                prototype.m_ttl = duration;
                seMan.AddStatusEffect(prototype);
            }
        }

        // A null icon would render as an invisible HUD entry, so a failed sprite lookup leaves the prototype
        // null: the discount still works, but without the buff it never expires.
        private static SE_BloodRite GetOrCreatePrototype() {
            if (_buffPrototype != null) {
                return _buffPrototype;
            }

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
            se.m_ttl = GetBuffDuration(); // restamped on every apply/refresh
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
