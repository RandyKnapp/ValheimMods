using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Grants adrenaline every few seconds for each nearby enemy that the local player has poisoned.
    public static class GainAdrenalineWhenApplyingPoison {
        // Seconds between adrenaline pulses, and how far a poisoned foe can be and still count. Tunable as
        // "TickInterval" and "Radius" in this effect's Config block in config/shardstones.json;
        // PoisonAdrenalinePulse re-arms its InvokeRepeating when the interval changes.
        public const float DefaultTickInterval = 3f;
        public const float DefaultRadius = 30f;

        private const string TickIntervalKey = "TickInterval";
        private const string RadiusKey = "Radius";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { TickIntervalKey, DefaultTickInterval },
            { RadiusKey, DefaultRadius },
        };

        // Floored well above zero: InvokeRepeating with a tiny period would pulse every frame.
        internal static float GetTickInterval() {
            return Mathf.Max(0.5f, EffectConfig.Get(MagicEffectType.GainAdrenalineWhenApplyingPoison,
                TickIntervalKey, DefaultTickInterval));
        }

        private static float GetRadius() {
            return Mathf.Max(0f, EffectConfig.Get(MagicEffectType.GainAdrenalineWhenApplyingPoison,
                RadiusKey, DefaultRadius));
        }

        // Vanilla SE_Poison TTL defaults, used only when the prototype cannot be read from the ObjectDB.
        private const float DefaultBaseTtl = 2f;
        private const float DefaultTtlPerDamage = 2f;
        private const float DefaultTtlPower = 0.5f;

        // Enemies the local player has poisoned, mapped to the Time.time at which that poison runs out.
        // Pruned by the pulse, which walks the whole dictionary anyway.
        private static readonly Dictionary<Character, float> _poisonedUntil = new Dictionary<Character, float>();
        private static readonly List<Character> _stale = new List<Character>();

        // Tooltip: "Gain {0} Adrenaline every {1}s per Poisoned Foe Nearby" -- {1}/{2} are the configured
        // interval and radius, so the shown numbers follow a retune instead of the baked-in defaults.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.GainAdrenalineWhenApplyingPoison,
                value => new object[] { value, GetTickInterval(), GetRadius() });
        }

        internal static void ClearTracking() {
            _poisonedUntil.Clear();
        }

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction). Records the victim rather
        // than granting adrenaline; the payout happens on the pulse.
        public static void OnDamageDealt(Character victim, HitData hit, Character attacker) {
            if (victim == null || hit == null || hit.m_damage.m_poison <= 0f || attacker != Player.m_localPlayer) {
                return;
            }

            if (victim.IsPlayer() || victim.IsTamed()) {
                return;
            }

            if (!Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.GainAdrenalineWhenApplyingPoison)) {
                return;
            }

            // Extend the window, never shorten it -- vanilla's SE_Poison.AddDamage ignores a weaker
            // application while a stronger one is still running.
            var until = Time.time + GetPoisonDuration(victim, hit.m_damage.m_poison);
            if (!_poisonedUntil.TryGetValue(victim, out var existing) || until > existing) {
                _poisonedUntil[victim] = until;
            }
        }

        // One payout tick. Called by PoisonAdrenalinePulse only after it has confirmed the local player has
        // the effect, so value is the gating call's out-value and is not re-read here.
        internal static void Pulse(Player player, float value) {
            if (player.IsDead() || player.GetMaxAdrenaline() <= 0f) {
                return; // no adrenaline pool -> AddAdrenaline is inert (matches the other adrenaline shards)
            }

            var now = Time.time;
            var radius = GetRadius();
            var radiusSqr = radius * radius;
            var origin = player.transform.position;
            var count = 0;

            _stale.Clear();
            foreach (var pair in _poisonedUntil) {
                var character = pair.Key;
                if (character == null || character.IsDead() || now >= pair.Value) {
                    _stale.Add(character);
                    continue;
                }

                // Self-correct on cleanse/immunity when this client owns the victim; a remote-owned victim
                // has no local SEMan state, so there we fall back to the tracked window.
                if (character.m_nview != null && character.m_nview.IsValid() && character.m_nview.IsOwner() &&
                    !character.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectPoison)) {
                    _stale.Add(character);
                    continue;
                }

                if ((character.transform.position - origin).sqrMagnitude > radiusSqr) {
                    continue; // out of range for this pulse, but still poisoned -- keep tracking it
                }

                count++;
            }

            foreach (var key in _stale) {
                _poisonedUntil.Remove(key);
            }
            _stale.Clear();

            if (count > 0) {
                player.AddAdrenaline(value * Mathf.Sqrt(count));
            }
        }

        // How long the victim will keep taking poison damage. The live DoT is readable whenever this client
        // owns the victim (Character.Damage routes RPC_Damage inline for self-owned targets, so AddPoisonDamage
        // has already run by the time this postfix fires) and is exact, including damage from other sources.
        // For a remote-owned victim there is no local SE_Poison, so mirror vanilla's TTL formula instead.
        private static float GetPoisonDuration(Character victim, float poison) {
            if (victim.GetSEMan().GetStatusEffect(SEMan.s_statusEffectPoison) is SE_Poison live) {
                return live.GetRemaningTime();
            }

            var prototype = ObjectDB.instance != null
                ? ObjectDB.instance.GetStatusEffect(SEMan.s_statusEffectPoison) as SE_Poison
                : null;
            var baseTtl = prototype != null ? prototype.m_baseTTL : DefaultBaseTtl;
            var ttlPerDamage = prototype != null ? prototype.m_TTLPerDamage : DefaultTtlPerDamage;
            var ttlPower = prototype != null ? prototype.m_TTLPower : DefaultTtlPower;

            return baseTtl + Mathf.Pow(poison * ttlPerDamage, ttlPower);
        }
    }

    // Drives the adrenaline pulse from its own DontDestroyOnLoad object, so it survives scene loads, needs no
    // player to exist yet, and costs one scheduled call every few seconds instead of a per-frame patch.
    // Created once from the plugin Awake.
    internal class PoisonAdrenalinePulse : MonoBehaviour {
        internal static PoisonAdrenalinePulse instance;

        // Tracked so a new Player object (respawn, logout, world change) drops victims poisoned by the
        // previous one instead of paying out for them.
        private Player _trackedPlayer;

        internal static void Create() {
            if (instance != null) {
                return;
            }

            var go = new GameObject("EL_PoisonAdrenalinePulse");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<PoisonAdrenalinePulse>();
        }

        // The period InvokeRepeating was last armed with. InvokeRepeating fixes its period at scheduling
        // time, so a retuned TickInterval only takes hold once the invoke is cancelled and re-armed.
        private float _scheduledInterval;

        [UsedImplicitly]
        private void Awake() {
            instance = this;
            Reschedule();
        }

        private void Reschedule() {
            _scheduledInterval = GainAdrenalineWhenApplyingPoison.GetTickInterval();
            CancelInvoke(nameof(Pulse));
            InvokeRepeating(nameof(Pulse), _scheduledInterval, _scheduledInterval);
        }

        [UsedImplicitly]
        private void Pulse() {
            // Cheapest place guaranteed to run after a config reload, and it costs one float compare on a
            // call that already only happens every few seconds.
            if (!Mathf.Approximately(_scheduledInterval, GainAdrenalineWhenApplyingPoison.GetTickInterval())) {
                Reschedule();
            }

            var player = Player.m_localPlayer;
            if (player == null || _trackedPlayer != player) {
                _trackedPlayer = player;
                GainAdrenalineWhenApplyingPoison.ClearTracking();
                return;
            }

            // Gate on the effect before doing any work: without the shard socketed this pulse is a couple of
            // checks and a return.
            if (!player.HasActiveMagicEffect(MagicEffectType.GainAdrenalineWhenApplyingPoison, out var value) ||
                value <= 0f) {
                GainAdrenalineWhenApplyingPoison.ClearTracking();
                return;
            }

            GainAdrenalineWhenApplyingPoison.Pulse(player, value);
        }
    }
}
