using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;

namespace EpicLoot.MagicItemEffects.Shards {
    // Counts vanilla's "Resting" status (by a fire, sitting or sheltered), the "Rested" buff it leaves behind, or
    // either, per the WhileResting and WhileRested keys. The bonus applies once when both hold.
    public static class RestingHealthRegen {
        public const float DefaultWhileResting = 1f;
        public const float DefaultWhileRested = 0f;

        private const string WhileRestingKey = "WhileResting";
        private const string WhileRestedKey = "WhileRested";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { WhileRestingKey, DefaultWhileResting },
            { WhileRestedKey, DefaultWhileRested },
        };

        public static void Apply(SEMan seman, ref float regenMultiplier) {
            var player = Player.m_localPlayer;
            if (seman.m_character != player) {
                return;
            }

            var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.RestingHealthRegen, 0.01f);
            if (bonus == 0f) {
                return;
            }

            if (IsActive(seman, WhileRestingKey, DefaultWhileResting, SEMan.s_statusEffectResting) ||
                IsActive(seman, WhileRestedKey, DefaultWhileRested, SEMan.s_statusEffectRested)) {
                regenMultiplier += bonus;
            }
        }

        private static bool IsActive(SEMan seman, string key, float fallback, int statusEffect) {
            return EffectConfig.Get(MagicEffectType.RestingHealthRegen, key, fallback) > 0f &&
                seman.HaveStatusEffect(statusEffect);
        }

        // Points the definition's text at the tokens for its Config, so the tooltip names the states that
        // count. The default pair also stands for both off, where the Shift detail shows the two zeros.
        public static void RefreshDisplayText() {
            if (!MagicItemEffectDefinitions.AllDefinitions.TryGetValue(MagicEffectType.RestingHealthRegen, out var definition) ||
                definition?.Config == null) {
                return;
            }

            var config = definition.Config;
            var tokens = MagicEffectType.RestingHealthRegen.ToLowerInvariant();
            if (config.TryGetValue(WhileRestedKey, out var rested) && rested > 0f) {
                tokens += config.TryGetValue(WhileRestingKey, out var resting) && resting > 0f ? "_restingorrested" : "_rested";
            }

            definition.DisplayText = $"$mod_epicloot_me_{tokens}_display";
            definition.Description = $"$mod_epicloot_me_{tokens}_desc";
        }
    }
}
