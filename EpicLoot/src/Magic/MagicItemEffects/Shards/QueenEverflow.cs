using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class QueenEverflow {
        public const int DefaultMaxStacks = 10;
        public const float DefaultBuffDuration = 10f;

        private const string MaxStacksKey = "MaxStacks";
        private const string BuffDurationKey = "BuffDuration";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { MaxStacksKey, DefaultMaxStacks },
            { BuffDurationKey, DefaultBuffDuration },
        };

        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Everflow,
                value => new object[] { value, (float)GetMaxStacks() });
        }

        private const string BuffName = "EL_QueenEverflow";
        private static readonly int BuffHash = BuffName.GetStableHashCode();
        private static SE_QueenEverflow _buffPrototype;
        private static bool _iconMissingLogged;
        private static readonly KillCredit _credit = new KillCredit();

        // Postfix handler invoked by SharedCharacterDamagePatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || __instance == player
                || attacker != player || __instance.IsPlayer() || __instance.IsTamed()) {
                return;
            }

            // Only fire on a kill, and once per kill however many hits land as the victim dies (KillCredit).
            // Against a remote-owned victim the RPC carrying this hit has not executed yet, so the post-hit
            // health is estimated; against one this client owns, it has.
            if (!_credit.TryClaim(__instance, hit)) {
                return;
            }

            // The shard value is the per-stack regen percentage.
            var regenPerStack = player.GetTotalActiveMagicEffectValue(MagicEffectType.Everflow, 0.01f);
            if (regenPerStack <= 0f) {
                return;
            }

            ApplyOrStack(player, regenPerStack);
        }

        private static void ApplyOrStack(Player player, float regenPerStack) {
            var prototype = GetOrCreatePrototype();
            if (prototype == null) {
                return;
            }

            var maxStacks = GetMaxStacks();
            var duration = GetBuffDuration();
            var seMan = player.GetSEMan();

            if (seMan.GetStatusEffect(BuffHash) is SE_QueenEverflow existing) {
                existing.Stacks = Mathf.Min(existing.Stacks + 1, maxStacks);
                existing.RegenPerStack = regenPerStack;
                existing.m_ttl = duration; // restamped so a retuned duration reaches a buff already running
                existing.ResetTime();
                return;
            }

            if (seMan.AddStatusEffect(prototype) is SE_QueenEverflow added) {
                added.Stacks = 1;
                added.RegenPerStack = regenPerStack;
                added.m_ttl = duration;
                added.ResetTime();
            }
        }

        private static int GetMaxStacks() {
            return EffectConfig.GetIntAtLeast(MagicEffectType.Everflow, MaxStacksKey, DefaultMaxStacks, 1);
        }

        // Floored just above zero: a ttl of 0 is "no timeout" to vanilla, which would make the buff permanent.
        private static float GetBuffDuration() {
            return Mathf.Max(0.1f,
                EffectConfig.Get(MagicEffectType.Everflow, BuffDurationKey, DefaultBuffDuration));
        }

        // A null icon would render as an invisible HUD entry, so a failed trophy lookup leaves the prototype null.
        private static SE_QueenEverflow GetOrCreatePrototype() {
            if (_buffPrototype != null) {
                return _buffPrototype;
            }

            var icon = ObjectDB.instance?.GetItemPrefab("TrophySeekerQueen")?
                .GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null) {
                if (!_iconMissingLogged) {
                    EpicLoot.LogWarning("QueenEverflow: could not find 'TrophySeekerQueen' icon; regen buff will not display.");
                    _iconMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<SE_QueenEverflow>();
            se.name = BuffName;
            se.m_name = "$mod_epicloot_se_queeneverflow";
            se.m_icon = icon;
            se.m_ttl = GetBuffDuration(); // restamped on every apply/refresh
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
