using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // The shard value is the damage bonus per stack; SE_Bloodrage's ModifyAttack applies it.
    public static class Bloodrage {
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

        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Bloodrage,
                value => new object[] { value, (float)GetMaxStacks() });
        }

        // Postfix handler invoked by SharedCharacterRpcDamagePatch (on-damage-taken reaction).
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

            // Restamp the per-stack bonus: the shard set may have changed.
            if (seMan.GetStatusEffect(BuffHash) is SE_Bloodrage existing) {
                existing.Stacks = Mathf.Min(existing.Stacks + 1, maxStacks);
                existing.DamagePerStack = perStack;
                existing.m_ttl = duration; // restamped so a retuned duration reaches a buff already running
                existing.ResetTime();
                return;
            }

            if (seMan.AddStatusEffect(prototype) is SE_Bloodrage added) {
                added.Stacks = 1;
                added.DamagePerStack = perStack;
                added.m_ttl = duration;
                added.ResetTime();
            }
        }

        private static int GetMaxStacks() {
            return EffectConfig.GetIntAtLeast(MagicEffectType.Bloodrage, MaxStacksKey, DefaultMaxStacks, 1);
        }

        // Floored just above zero: a ttl of 0 is "no timeout" to vanilla, which would make the rage permanent.
        private static float GetBuffDuration() {
            return Mathf.Max(0.1f,
                EffectConfig.Get(MagicEffectType.Bloodrage, BuffDurationKey, DefaultBuffDuration));
        }

        // A null icon would render as an invisible HUD entry, so a failed sprite lookup leaves the prototype null.
        private static SE_Bloodrage GetOrCreatePrototype() {
            if (_buffPrototype != null) {
                return _buffPrototype;
            }

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
            se.m_ttl = GetBuffDuration(); // restamped on every proc
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
