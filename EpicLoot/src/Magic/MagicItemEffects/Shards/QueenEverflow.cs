using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // The Queen boss shard ("Queen's Everflow"): every creature the local player kills grants a stacking buff
    // (SE_QueenEverflow) that boosts health, stamina AND eitr regeneration. Stacks build up to a configurable
    // cap and each kill refreshes the buff's duration
    //
    // Kill detection reads the target's health after the hit, which is exact when the player owns the enemy
    // (single-player, or enemies close to the host); against remote-owned enemies it is best-effort.
    public static class QueenEverflow {
        public const int DefaultMaxStacks = 10;
        private const float BuffDuration = 10f; // standard duration the buff lasts / is refreshed to on each kill

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

            // Only fire on a kill -- the hit must have dropped the target to (or below) zero health.
            if (__instance.GetHealth() > 0f) {
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
            var seMan = player.GetSEMan();

            if (seMan.GetStatusEffect(BuffHash) is SE_QueenEverflow existing) {
                existing.Stacks = Mathf.Min(existing.Stacks + 1, maxStacks);
                existing.MaxStacks = maxStacks;
                existing.RegenPerStack = regenPerStack;
                existing.ResetTime(); // refresh back to the standard duration
                return;
            }

            if (seMan.AddStatusEffect(prototype) is SE_QueenEverflow added) {
                added.Stacks = 1;
                added.MaxStacks = maxStacks;
                added.RegenPerStack = regenPerStack;
                added.m_ttl = BuffDuration;
                added.ResetTime();
            }
        }

        // Max stacks come from the Everflow magic effect's Config block ("MaxStacks"), defaulting to
        // DefaultMaxStacks when unset. Clamped to at least 1 so a misconfiguration can't disable the buff.
        private static int GetMaxStacks() {
            var cfg = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.Everflow);
            if (cfg != null && cfg.TryGetValue("MaxStacks", out var raw)) {
                return Mathf.Max(1, Mathf.RoundToInt(raw));
            }
            return DefaultMaxStacks;
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
            se.m_ttl = BuffDuration;
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
