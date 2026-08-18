using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // DarkRed chest: taking a hit sends the player into a rage, granting a stacking bonus to all outgoing
    // damage. Each hit taken adds a stack up to MaxStacks and refreshes the countdown, so a sustained
    // fight ramps the bonus and holds it -- turning damage taken into damage dealt, which is the DarkRed
    // (blood) fantasy the whole shard is built around. The shard value is the bonus *per stack*, so a
    // Mythic (5) tops out at +25% damage. The bonus is applied by SE_Bloodrage's own ModifyAttack
    // override, so vanilla drives it off the live stack count on every swing.
    public static class Bloodrage {
        public const int DefaultMaxStacks = 5;
        private const float BuffDuration = 10f; // seconds the buff lasts / is refreshed to on each hit taken

        private const string BuffName = "EL_Bloodrage";
        private static readonly int BuffHash = BuffName.GetStableHashCode();
        private static SE_Bloodrage _buffPrototype;
        private static bool _iconMissingLogged;

        // Tooltip: "+{0}% Damage per Stack (max {1})" -- {1} surfaces the configured stack cap. Pure, as
        // the provider contract requires (MagicItem.RegisterDisplayValues): it only reads the effect config.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Bloodrage,
                value => new object[] { value, (float)GetMaxStacks() });
        }

        // Postfix handler invoked by SharedCharacterRpcDamagePatch (on-damage-taken reaction).
        //
        // Victim-side, so it must hang off Character.RPC_Damage rather than Character.Damage: the latter
        // runs on the *attacker's* client, and for the common case of a server- or peer-owned creature
        // hitting us that code never executes here at all. RPC_Damage runs on the victim's owner, which
        // for our own player is us.
        public static void OnDamageTaken(Character __instance, HitData hit) {
            if (hit == null || __instance != Player.m_localPlayer) {
                return;
            }

            // Fully-mitigated hits don't rage.
            if (hit.m_damage.EpicLootGetTotalDamageAgainstPlayer() <= 0f) {
                return;
            }

            var perStack = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.Bloodrage, 0.01f);
            if (perStack <= 0f) {
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (prototype == null) {
                return;
            }

            var maxStacks = GetMaxStacks();
            var seMan = Player.m_localPlayer.GetSEMan();

            // Re-proc while the buff is still up: add a stack (capped), restamp the per-stack bonus (the
            // shard set may have changed) and refresh the countdown rather than letting the old timer run out.
            if (seMan.GetStatusEffect(BuffHash) is SE_Bloodrage existing) {
                existing.Stacks = Mathf.Min(existing.Stacks + 1, maxStacks);
                existing.MaxStacks = maxStacks;
                existing.DamagePerStack = perStack;
                existing.ResetTime();
                return;
            }

            if (seMan.AddStatusEffect(prototype) is SE_Bloodrage added) {
                added.Stacks = 1;
                added.MaxStacks = maxStacks;
                added.DamagePerStack = perStack;
                added.m_ttl = BuffDuration;
                added.ResetTime();
            }
        }

        // Max stacks come from the Bloodrage magic effect's Config block ("MaxStacks", see
        // ShardEffectDefinitions), defaulting to DefaultMaxStacks when unset. Clamped to at least 1 so a
        // misconfiguration can't disable the buff.
        private static int GetMaxStacks() {
            var cfg = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.Bloodrage);
            if (cfg != null && cfg.TryGetValue("MaxStacks", out var raw)) {
                return Mathf.Max(1, Mathf.RoundToInt(raw));
            }
            return DefaultMaxStacks;
        }

        // Lazily builds the buff prototype. Runs on a hit taken, so the asset bundle is loaded. A null
        // icon would render as an invisible HUD entry (SEMan only surfaces effects with an icon), so if
        // the sprite lookup fails we log once and leave the prototype null.
        private static SE_Bloodrage GetOrCreatePrototype() {
            if (_buffPrototype != null) {
                return _buffPrototype;
            }

            // The DarkRed shardstone's own gem icon -- same sprite the shard items use (see Shards.cs).
            var icon = EpicAssets.AssetBundle?.LoadAsset<Sprite>("Assets/EpicLoot/Sprites/Shardstones/DarkRed.png");
            if (icon == null) {
                if (!_iconMissingLogged) {
                    EpicLoot.LogWarning("Bloodrage: could not load the DarkRed shardstone sprite; Bloodrage will not display.");
                    _iconMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<SE_Bloodrage>();
            se.name = BuffName;
            se.m_name = "$mod_epicloot_se_bloodrage";
            se.m_icon = icon;
            se.m_ttl = BuffDuration;
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
