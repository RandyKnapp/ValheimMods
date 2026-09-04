using EpicLoot.CraftingV2;

namespace EpicLoot.ShardStones {
    // Folds config/shardstoneconversions.json into the live material-conversion set, so the ShardStone
    // rarity ladder shows up in the enchanting table's Convert Materials tab -- in its own Upgrade Shardstones
    // mode, keyed by MaterialConversionType.ShardUpgrade, rather than mixed into the generic material upgrades.
    // The shard-to-material sinks this file used to declare now live in config/enchantcosts.json as the
    // shardstone sacrifice yield; see Normalize.
    //
    // The recipes are plain MaterialConversion entries -- one per (color, step) -- written out in full rather
    // than generated from a compact per-category cost model. That costs a large file, and buys a config a
    // player or patch author can read, JSONPath-target and reason about one shard at a time, and a
    // loot/crafting graph in which every name is a real prefab.
    //
    // They live in their own file purely so materialconversions.json stays hand-readable; the two are one
    // set as far as the rest of the mod is concerned.
    public static class ShardStoneConversions {
        // Every shipped recipe name starts with this. Stripping by prefix before appending is what makes
        // Merge idempotent, which it has to be: both configs can reload independently and each reload
        // re-runs it.
        private const string NamePrefix = "ShardStone";

        // The two recipe families this file has shipped, distinguished by name.
        private const string UpgradePrefix = "ShardStoneUpgrade";
        private const string ConvertPrefix = "ShardStoneConvert";

        public static MaterialConversionsConfig Config;

        // Config setup hook (SychronizeConfig<MaterialConversionsConfig>).
        public static void Initialize(MaterialConversionsConfig config) {
            Config = config ?? new MaterialConversionsConfig();
            Normalize();

            // OnSetupMaterialConversions only fires when materialconversions.json (re)loads, so merging
            // here too is what makes a live edit of this file, a config push from a dedicated server, or
            // an out-of-order RPC arrival actually take effect. No-ops until material conversions have
            // loaded, which covers the first-launch case where this config is read first.
            Merge();
        }

        public static MaterialConversionsConfig GetCFG() {
            return Config;
        }

        // Appends this file's recipes to the material conversions and rebuilds the lookup they are read
        // through. Wired to MaterialConversions.OnSetupMaterialConversions as well as to Initialize.
        public static void Merge() {
            var target = MaterialConversions.Config;
            if (target == null || Config?.MaterialConversions == null) {
                return;
            }

            target.MaterialConversions.RemoveAll(IsShardStoneRecipe);
            target.MaterialConversions.AddRange(Config.MaterialConversions);

            // MaterialConversions.Initialize fires the event that lands here *before* it builds its own
            // lookup, so on that path this is redundant. On the Initialize-driven path -- a reload of this
            // file alone -- it is the only thing that makes the merge visible.
            MaterialConversions.Conversions.Clear();
            foreach (var entry in target.MaterialConversions) {
                MaterialConversions.Conversions.Add(entry.Type, entry);
            }
        }

        // True for a recipe this class owns. Also used to keep the merged entries out of the
        // materialconversions network payload -- every client merges them from its own copy of this file.
        // Brings an older copy of this file in line with what this build expects. The embedded config is only
        // written to disk when the player has no copy yet, so a returning player -- or a dedicated server
        // pushing its own copy -- can still hand us upgrades typed as a plain Upgrade, and the retired
        // shard-to-material sinks. Left alone, the former would land back in the generic Upgrade mode this
        // change exists to unclutter, and the latter would sit in the Convert mode duplicating what
        // sacrificing the same stone now gives. Runs on Config itself, so GetCFG serves normalized data too.
        private static void Normalize() {
            Config.MaterialConversions.RemoveAll(x => x?.Name != null && x.Name.StartsWith(ConvertPrefix));

            foreach (MaterialConversion entry in Config.MaterialConversions) {
                if (entry?.Name != null && entry.Name.StartsWith(UpgradePrefix) &&
                    entry.Type == MaterialConversionType.Upgrade) {
                    entry.Type = MaterialConversionType.ShardUpgrade;
                }
            }
        }

        public static bool IsShardStoneRecipe(MaterialConversion conversion) {
            return conversion?.Name != null && conversion.Name.StartsWith(NamePrefix);
        }
    }
}
