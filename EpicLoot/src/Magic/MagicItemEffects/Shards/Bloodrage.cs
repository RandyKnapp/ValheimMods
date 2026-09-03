using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // DarkRed chest: taking a hit sends the player into a rage, granting a stacking bonus to all outgoing
    // damage. Each hit taken adds a stack up to MaxStacks and refreshes the countdown, so a sustained
    // fight ramps the bonus and holds it -- turning damage taken into damage dealt, which is the DarkRed
    // (blood) fantasy the whole shard is built around. The shard value is the bonus *per stack*, so a
    // Mythic (5) tops out at +25% damage. The bonus is applied by SE_Bloodrage's own ModifyAttack
    // override, so vanilla drives it off the live stack count on every swing.
    public static class Bloodrage {
        // How many hits taken the rage may stack to, and how long a stack survives without a refresh.
        // Tunable as "MaxStacks" and "BuffDuration" in this effect's Config block in config/shardstones.json.
        public const int DefaultMaxStacks = 5;
        public const float DefaultBuffDuration = 10f;

        private const string MaxStacksKey = "MaxStacks";
        private const string BuffDurationKey = "BuffDuration";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { MaxStacksKey, DefaultMaxStacks },
            { BuffDurationKey, DefaultBuffDuration },
        };

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
            var duration = GetBuffDuration();
            var seMan = Player.m_localPlayer.GetSEMan();

            // Re-proc while the buff is still up: add a stack (capped), restamp the per-stack bonus (the
            // shard set may have changed) and refresh the countdown rather than letting the old timer run out.
            if (seMan.GetStatusEffect(BuffHash) is SE_Bloodrage existing) {
                existing.Stacks = Mathf.Min(existing.Stacks + 1, maxStacks);
                existing.MaxStacks = maxStacks;
                existing.DamagePerStack = perStack;
                existing.m_ttl = duration; // restamped so a retuned duration reaches a buff already running
                existing.ResetTime();
                return;
            }

            if (seMan.AddStatusEffect(prototype) is SE_Bloodrage added) {
                added.Stacks = 1;
                added.MaxStacks = maxStacks;
                added.DamagePerStack = perStack;
                added.m_ttl = duration;
                added.ResetTime();
            }
        }

        // Clamped to at least 1 so a misconfiguration can't disable the buff outright.
        private static int GetMaxStacks() {
            return EffectConfig.GetIntAtLeast(MagicEffectType.Bloodrage, MaxStacksKey, DefaultMaxStacks, 1);
        }

        // Floored just above zero: a ttl of 0 is "no timeout" to vanilla, which would make the rage permanent.
        private static float GetBuffDuration() {
            return Mathf.Max(0.1f,
                EffectConfig.Get(MagicEffectType.Bloodrage, BuffDurationKey, DefaultBuffDuration));
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
            se.m_ttl = GetBuffDuration(); // restamped on every proc by ApplyOrStack
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
