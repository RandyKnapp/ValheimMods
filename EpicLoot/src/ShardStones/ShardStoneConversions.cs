using EpicLoot.CraftingV2;

namespace EpicLoot.ShardStones {
    // Folds config/shardstoneconversions.json into the live material-conversion set, so the ShardStone
    // rarity ladder and the shard-to-material sinks show up in the enchanting table's Convert Materials
    // tab alongside everything materialconversions.json declares.
    //
    // The recipes are plain MaterialConversion entries -- one per (color, step) and per (color, rarity,
    // product) -- written out in full rather than generated from a compact per-category cost model. That
    // costs a large file, and buys a config a player or patch author can read, JSONPath-target and reason
    // about one shard at a time, and a loot/crafting graph in which every name is a real prefab.
    //
    // They live in their own file purely so materialconversions.json stays hand-readable; the two are one
    // set as far as the rest of the mod is concerned.
    public static class ShardStoneConversions {
        // Every shipped recipe name starts with this. Stripping by prefix before appending is what makes
        // Merge idempotent, which it has to be: both configs can reload independently and each reload
        // re-runs it.
        private const string NamePrefix = "ShardStone";

        public static MaterialConversionsConfig Config;

        // Config setup hook (SychronizeConfig<MaterialConversionsConfig>).
        public static void Initialize(MaterialConversionsConfig config) {
            Config = config ?? new MaterialConversionsConfig();

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
        public static bool IsShardStoneRecipe(MaterialConversion conversion) {
            return conversion?.Name != null && conversion.Name.StartsWith(NamePrefix);
        }
    }
}
