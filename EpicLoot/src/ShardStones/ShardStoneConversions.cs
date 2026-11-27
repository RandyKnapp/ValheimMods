using EpicLoot.CraftingV2;

namespace EpicLoot.ShardStones {
    // Folds config/shardstoneconversions.json into the live material-conversion set.
    public static class ShardStoneConversions {
        // Stripping by prefix before appending is what makes Merge idempotent, which it has to be: both
        // configs can reload independently and each reload re-runs it.
        private const string NamePrefix = "ShardStone";

        private const string UpgradePrefix = "ShardStoneUpgrade";
        private const string ConvertPrefix = "ShardStoneConvert";

        public static MaterialConversionsConfig Config;

        public static void Initialize(MaterialConversionsConfig config) {
            Config = config ?? new MaterialConversionsConfig();
            Normalize();

            // OnSetupMaterialConversions only fires when materialconversions.json loads, so merging here too
            // is what makes a live edit or server push of this file take effect. No-ops until material
            // conversions have loaded.
            Merge();
        }

        public static MaterialConversionsConfig GetCFG() {
            return Config;
        }

        public static void Merge() {
            var target = MaterialConversions.Config;
            if (target == null || Config?.MaterialConversions == null) {
                return;
            }

            target.MaterialConversions.RemoveAll(IsShardStoneRecipe);
            target.MaterialConversions.AddRange(Config.MaterialConversions);

            // MaterialConversions.Initialize fires the event that lands here before it builds its own
            // lookup, so on that path this is redundant; on a reload of this file alone it is what makes
            // the merge visible.
            MaterialConversions.Conversions.Clear();
            foreach (var entry in target.MaterialConversions) {
                MaterialConversions.Conversions.Add(entry.Type, entry);
            }
        }

        // An on-disk or server-pushed copy can predate the ShardUpgrade type and still carry the
        // ShardStoneConvert sinks that sacrificing now replaces. Runs on Config itself, so GetCFG serves
        // normalized data too.
        private static void Normalize() {
            Config.MaterialConversions.RemoveAll(x => x?.Name != null && x.Name.StartsWith(ConvertPrefix));

            foreach (MaterialConversion entry in Config.MaterialConversions) {
                if (entry?.Name != null && entry.Name.StartsWith(UpgradePrefix) &&
                    entry.Type == MaterialConversionType.Upgrade) {
                    entry.Type = MaterialConversionType.ShardUpgrade;
                }
            }
        }

        // Also keeps the merged entries out of the materialconversions network payload; every client
        // merges them from its own copy of this file.
        public static bool IsShardStoneRecipe(MaterialConversion conversion) {
            return conversion?.Name != null && conversion.Name.StartsWith(NamePrefix);
        }
    }
}
