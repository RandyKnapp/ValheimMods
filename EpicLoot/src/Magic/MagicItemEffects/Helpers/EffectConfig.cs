using EpicLoot.ShardStones;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Reads a shard effect's per-effect tunables -- the "Config" block authored on its grid entry in
    // config/shardstones.json, merged over the code-side defaults registered in
    // ShardEffectDefinitions.EffectConfigs. This is the shard-side equivalent of the "Config" block a
    // magiceffects.json entry carries, and it resolves to the same place: the synthesized
    // MagicItemEffectDefinition.Config, which is also what the Shift-detail tooltip renders.
    //
    // Every key needs a code default at its call site. A player's existing on-disk shardstones.json
    // keeps winning until they accept the ConfigVersionManager rewrite prompt, so a key added to the
    // embedded config is simply absent for them until then -- the fallback argument is what they run on.
    //
    // Hot-path rule (see CLAUDE.md, "Read the memoized effect value first"): Get costs two dictionary
    // lookups. That is fine after the GetTotalActiveMagicEffectValue == 0 bail every effect already
    // does, and not before it. Globals are the exception -- they feed 50Hz vanilla methods, so they are
    // resolved once per config load into plain static fields rather than looked up per read.
    public static class EffectConfig {
        private static Dictionary<string, float> _global = new Dictionary<string, float>();

        public static float Get(string effectType, string key, float fallback) {
            var config = MagicItemEffectDefinitions.GetEffectConfig(effectType);
            return config != null && config.TryGetValue(key, out var value) ? value : fallback;
        }

        // Rounds rather than truncates, so an author writing 2.5 for a count gets 3 instead of 2.
        public static int GetInt(string effectType, string key, int fallback) {
            return Mathf.RoundToInt(Get(effectType, key, fallback));
        }

        // For counts where zero or negative would disable the effect outright rather than tune it --
        // stack caps, charge thresholds. A misconfiguration should weaken an effect, never delete it.
        public static int GetIntAtLeast(string effectType, string key, int fallback, int min) {
            return Mathf.Max(min, GetInt(effectType, key, fallback));
        }

        public static float GetClamped(string effectType, string key, float fallback, float min, float max) {
            return Mathf.Clamp(Get(effectType, key, fallback), min, max);
        }

        // Cross-effect tunables from the "Global" block. Prefer a static field refreshed by
        // ApplyGlobalConfig over calling this per read; this overload exists for cold paths.
        public static float Global(string key, float fallback) {
            return _global.TryGetValue(key, out var value) ? value : fallback;
        }

        // Called from Shards.InitializeShardDefinitions, so it runs on every load path the config file
        // has: first load, embedded-default fallback, file-watcher hot reload, and the server->client
        // RPC. Pushes the resolved values into the classes that own them, so their hot paths read a
        // plain static field.
        public static void ApplyGlobalConfig(ShardGlobalConfig config) {
            _global = config?.Values ?? new Dictionary<string, float>();
            PenaltyScaling.RefreshGlobalConfig();
            BloodBlockSelfDamage.RefreshGlobalConfig();
        }
    }
}
