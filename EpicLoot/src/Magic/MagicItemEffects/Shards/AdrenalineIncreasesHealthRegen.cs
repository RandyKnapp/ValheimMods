using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a temporary health regeneration buff on adrenaline activation
    public static class AdrenalineIncreasesHealthRegen {
        // Seconds of buff granted per 1 point of shard value. Overridable via the effect's Config block
        // ("SecondsPerPercent", see ShardEffectDefinitions).
        public const float DefaultSecondsPerPercent = 0.5f;

        private const string BuffName = "EL_AdrenalineSurge";
        private static readonly int BuffHash = BuffName.GetStableHashCode();
        private static SE_AdrenalineSurge _buffPrototype;
        private static bool _iconMissingLogged;

        // Tooltip: "Adrenaline Surge: +{0}% Health Regen for {1}s" -- {1} surfaces the derived duration. Pure,
        // as the provider contract requires (MagicItem.RegisterDisplayValues): it only reads the effect config.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.AdrenalineIncreasesHealthRegen,
                value => new object[] { value, value * GetSecondsPerPercent() });
        }

        // Called by SharedPlayerAddAdrenalinePatch, which owns the Player.AddAdrenaline patch and the
        // fill/pop detection (including the local-player and no-adrenaline-source guards).
        public static void OnAdrenalineActivated(Player player) {
            var percent = player.GetTotalActiveMagicEffectValue(MagicEffectType.AdrenalineIncreasesHealthRegen);
            if (percent <= 0f) {
                return;
            }

            var bonus = percent * 0.01f;
            var duration = percent * GetSecondsPerPercent();

            var seMan = player.GetSEMan();

            if (seMan.GetStatusEffect(BuffHash) is SE_AdrenalineSurge existing) {
                existing.RegenBonus = bonus;
                existing.m_ttl = duration;
                existing.ResetTime();
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (prototype == null) {
                return;
            }

            if (seMan.AddStatusEffect(prototype) is SE_AdrenalineSurge added) {
                added.RegenBonus = bonus;
                added.m_ttl = duration;
                added.ResetTime();
            }
        }

        private static float GetSecondsPerPercent() {
            var cfg = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.AdrenalineIncreasesHealthRegen);
            if (cfg != null && cfg.TryGetValue("SecondsPerPercent", out var raw) && raw > 0f) {
                return raw;
            }
            return DefaultSecondsPerPercent;
        }

        private static SE_AdrenalineSurge GetOrCreatePrototype() {
            if (_buffPrototype != null) {
                return _buffPrototype;
            }

            // The LightGreen shardstone's own icon -- same sprite the shard items use (see Shards.cs).
            var icon = EpicAssets.AssetBundle?.LoadAsset<Sprite>("Assets/EpicLoot/Sprites/Shardstones/LightGreen.png");
            if (icon == null) {
                if (!_iconMissingLogged) {
                    EpicLoot.LogWarning("AdrenalineIncreasesHealthRegen: could not load the LightGreen shardstone sprite; Adrenaline Surge will not display.");
                    _iconMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<SE_AdrenalineSurge>();
            se.name = BuffName;
            se.m_name = "$mod_epicloot_se_adrenalinesurge";
            se.m_icon = icon;
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
