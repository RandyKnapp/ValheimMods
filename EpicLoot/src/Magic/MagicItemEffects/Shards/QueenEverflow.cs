using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // The Queen boss shard ("Queen's Everflow"): every creature the local player kills grants a stacking buff
    // (SE_QueenEverflow) that boosts health, stamina AND eitr regeneration. Stacks build up to a configurable
    // cap and each kill refreshes the buff's duration
    //
    // Kill detection reads the target's health after the hit, which is exact when the player owns the enemy
    // (single-player, or enemies close to the host); against remote-owned enemies it is best-effort.
    public static class QueenEverflow {
        // How many kills the buff may stack to, and how long a stack survives without a refresh. Tunable as
        // "MaxStacks" and "BuffDuration" in this effect's Config block in config/shardstones.json.
        public const int DefaultMaxStacks = 10;
        public const float DefaultBuffDuration = 10f;

        private const string MaxStacksKey = "MaxStacks";
        private const string BuffDurationKey = "BuffDuration";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { MaxStacksKey, DefaultMaxStacks },
            { BuffDurationKey, DefaultBuffDuration },
        };

        private const string BuffName = "EL_QueenEverflow";
        private static readonly int BuffHash = BuffName.GetStableHashCode();
        private static SE_QueenEverflow _buffPrototype;
        private static bool _iconMissingLogged;

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || __instance == player
                || attacker != player || __instance.IsPlayer() || __instance.IsTamed()) {
                return;
            }

            // Only fire on a kill. Predicted lethality: against a remote-owned victim the RPC
            // carrying this hit has not executed yet, so GetHealth() still reads pre-hit health and
            // a plain <= 0 check never fired in multiplayer.
            if (__instance.GetHealth() - hit.GetTotalDamage() > 0f) {
                return;
            }

            // Shard value doubles as the per-stack regen percentage (10 -> +10% per stack at Legendary).
            var regenPerStack = player.GetTotalActiveMagicEffectValue(MagicEffectType.Everflow, 0.01f);
            if (regenPerStack <= 0f) {
                return;
            }

            ApplyOrStack(player, regenPerStack);
        }

        // Adds the buff on the first kill, or bumps its stack count (capped at MaxStacks) and refreshes its
        // duration on subsequent kills. The regen-per-stack fraction is stamped on the live instance so the
        // Modify*Regen overrides (re-queried every frame) always reflect the current rarity and stack count.
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
                existing.MaxStacks = maxStacks;
                existing.RegenPerStack = regenPerStack;
                existing.m_ttl = duration; // restamped so a retuned duration reaches a buff already running
                existing.ResetTime(); // refresh back to the standard duration
                return;
            }

            if (seMan.AddStatusEffect(prototype) is SE_QueenEverflow added) {
                added.Stacks = 1;
                added.MaxStacks = maxStacks;
                added.RegenPerStack = regenPerStack;
                added.m_ttl = duration;
                added.ResetTime();
            }
        }

        // Clamped to at least 1 so a misconfiguration can't disable the buff outright.
        private static int GetMaxStacks() {
            return EffectConfig.GetIntAtLeast(MagicEffectType.Everflow, MaxStacksKey, DefaultMaxStacks, 1);
        }

        // Floored just above zero: a ttl of 0 is "no timeout" to vanilla, which would make the buff permanent.
        private static float GetBuffDuration() {
            return Mathf.Max(0.1f,
                EffectConfig.Get(MagicEffectType.Everflow, BuffDurationKey, DefaultBuffDuration));
        }

        // Lazily builds the buff prototype. Runs on a kill, so ObjectDB is loaded and the Queen (Seeker Queen)
        // trophy icon is available. A null icon would render as an invisible HUD entry (SEMan only surfaces
        // effects with an icon), so if the trophy lookup fails we log once and leave the prototype null.
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
            se.m_ttl = GetBuffDuration(); // restamped on every apply/refresh by ApplyOrStack
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
