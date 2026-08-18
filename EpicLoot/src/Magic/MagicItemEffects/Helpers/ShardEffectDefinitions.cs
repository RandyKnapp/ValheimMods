using EpicLoot.MagicItemEffects.Shards;
using System.Collections.Generic;

namespace EpicLoot.Magic.MagicItemEffects.Helpers {
    // Registers a MagicItemEffectDefinition for every shard effect type that the loaded overhaul config does
    // not already define -- i.e. the new Shardstone-only effects declared in MagicEffectType_Shards.cs.
    // Without a definition, MagicItemEffectDefinitions.Get() synthesizes a blank-named fallback and logs a
    // "definition missing" warning, so socketed shards render with empty tooltip names. The socketed value
    // itself comes from the shard (ShardSocketManager.ResolveSocketedEffect); these definitions supply the
    // display text/requirements and the value ranges used by the loose-shard preview and compendium.
    //
    // Wired to OnSetupMagicItemEffectDefinitions (see EpicLoot.RegisterMagicEffectEvents) so it re-runs
    // after every config (re)load, which clears and rebuilds AllDefinitions. ShardStones types are
    // fully qualified because this namespace ends in ".Shards", which would otherwise shadow the
    // EpicLoot.ShardStones.Shards class.
    public static class ShardEffectDefinitions {
        // Per-effect Config blocks for shard effects that read tunables from MagicItemEffectDefinition.Config
        // (via MagicItemEffectDefinitions.GetEffectConfig). This mirrors the "Config" attribute a
        // magiceffects.json entry would carry; it is supplied here because shard effects are defined in code
        // rather than the overhaul config. Keys also surface in the detailed (Shift) tooltip -- see
        // MagicItem.GetEffectText / MagicItemEffectDefinition.GetConfigLabel.
        private static readonly Dictionary<string, Dictionary<string, float>> EffectConfigs =
            new Dictionary<string, Dictionary<string, float>>
            {
                // Queen's Everflow (QueenEverflow): how many times the regen buff may stack.
                { MagicEffectType.Everflow, new Dictionary<string, float> { { "MaxStacks", QueenEverflow.DefaultMaxStacks } } },
                // Dodge Momentum (PerfectDodge): how many times the damage buff may stack.
                { MagicEffectType.PerfectDodge, new Dictionary<string, float> { { "MaxStacks", PerfectDodge.DefaultMaxStacks } } },
                // Lucky Fishing (LuckWhileFishing): the bonus-treasure table (prefab -> value threshold,
                // same semantic as the Riches config) plus the triple-catch sub-roll chance.
                { MagicEffectType.LuckWhileFishing, LuckWhileFishing.DefaultConfig },
                // The three coin-economy effects below are no longer assigned to any shard slot (Golden was
                // re-themed from coins to luck). BuildDefinition is only reached for effects the grid
                // actually uses, so these entries are dormant rather than harmful -- and keeping them is
                // what makes any of the three revivable with a single config edit.
                //
                // Mercenary (was Golden weapons): the per-hit coin cost, the flat damage bonus, and the
                // soft-cap curve applied to the converted coins.
                { MagicEffectType.Mercenary, Mercenary.DefaultConfig },
                // Coinplated (was Golden chest): what share of the purse is committed to absorbing each hit.
                { MagicEffectType.Coinplated, Coinplated.DefaultConfig },
                // Wager (was Golden head): how much damage each wagered coin buys.
                { MagicEffectType.Wager, Wager.DefaultConfig },
                // Inspiration (Golden head): the percent chance that any single skill-XP gain inspires.
                { MagicEffectType.Inspiration, Inspiration.DefaultConfig },
                // Lucky Loot (Golden chest): the range of extra magic-item table rolls a proc earns.
                { MagicEffectType.LuckyLoot, LuckyLoot.DefaultConfig },
                // Bloodrage (DarkRed chest): how many times the damage buff may stack.
                { MagicEffectType.Bloodrage, new Dictionary<string, float> { { "MaxStacks", Bloodrage.DefaultMaxStacks } } },
                // Adrenaline Surge (AdrenalineIncreasesHealthRegen): seconds of buff granted per point of
                // shard value, which is what turns the single rarity ramp into both a regen % and a duration.
                { MagicEffectType.AdrenalineIncreasesHealthRegen,
                    new Dictionary<string, float>
                        { { "SecondsPerPercent", AdrenalineIncreasesHealthRegen.DefaultSecondsPerPercent } } },
            };

        // Effects that need an adrenaline pool but whose type name does not contain "Adrenaline", which is
        // how BuildDefinition infers the ItemHasAdrenaline requirement for the rest of them.
        private static readonly HashSet<string> AdrenalinePoolEffects = new HashSet<string>
        {
            MagicEffectType.StormFury,
        };

        public static void RegisterShardEffectDefinitions() {
            foreach (var pair in CollectShardEffects()) {
                if (MagicItemEffectDefinitions.AllDefinitions.ContainsKey(pair.Key)) {
                    continue; // already defined by the overhaul config or another source
                }

                MagicItemEffectDefinitions.Add(BuildDefinition(pair.Key, pair.Value));
            }
        }

        // Every effect type used by any shard, mapped to its per-rarity value ramp. Effects are globally
        // unique across shards, so first occurrence wins.
        private static Dictionary<string, Dictionary<ItemRarity, float>> CollectShardEffects() {
            var result = new Dictionary<string, Dictionary<ItemRarity, float>>();

            void Consider(ShardStones.ShardEffectDefinition effect) {
                if (effect != null && !string.IsNullOrEmpty(effect.EffectType) &&
                    !result.ContainsKey(effect.EffectType)) {
                    result[effect.EffectType] = effect.ValuesPerRarity;
                }
            }

            foreach (var shard in global::EpicLoot.ShardStones.Shards.ShardDefinitions.ShardEffects.Values) {
                Consider(shard.UniformEffect);
                if (shard.TypeEffects != null) {
                    foreach (var effect in shard.TypeEffects.Values) {
                        Consider(effect);
                    }
                }
            }

            return result;
        }

        private static MagicItemEffectDefinition BuildDefinition(string type,
            Dictionary<ItemRarity, float> valuesPerRarity) {
            var lower = type.ToLowerInvariant();
            var requirements = new MagicItemEffectRequirements { NoRoll = true };

            // Adrenaline effects only function alongside an adrenaline pool, so keep them legal only on
            // items that supply one (m_maxAdrenaline > 0, i.e. adrenaline trinkets) -- the shard grid
            // already assigns them only to the trinket slot.
            if (type.Contains("Adrenaline") || AdrenalinePoolEffects.Contains(type)) {
                requirements.ItemHasAdrenaline = true;
            }

            return new MagicItemEffectDefinition {
                Type = type,
                DisplayText = $"$mod_epicloot_me_{lower}_display",
                Description = $"$mod_epicloot_me_{lower}_desc",
                Requirements = requirements,
                ValuesPerRarity = BuildValues(valuesPerRarity),
                Config = EffectConfigs.TryGetValue(type, out var config)
                    ? new Dictionary<string, float>(config)
                    : new Dictionary<string, float>(),
                CanBeAugmented = false,
                CanBeDisenchanted = false,
                CanBeRunified = false,
            };
        }

        private static MagicItemEffectDefinition.ValuesPerRarityDef BuildValues(
            Dictionary<ItemRarity, float> valuesPerRarity) {
            return new MagicItemEffectDefinition.ValuesPerRarityDef {
                Magic = Value(valuesPerRarity, ItemRarity.Magic),
                Rare = Value(valuesPerRarity, ItemRarity.Rare),
                Epic = Value(valuesPerRarity, ItemRarity.Epic),
                Legendary = Value(valuesPerRarity, ItemRarity.Legendary),
                Mythic = Value(valuesPerRarity, ItemRarity.Mythic),
            };
        }

        private static MagicItemEffectDefinition.ValueDef Value(Dictionary<ItemRarity, float> values,
            ItemRarity rarity) {
            return values != null && values.TryGetValue(rarity, out var v)
                ? new MagicItemEffectDefinition.ValueDef { MinValue = v, MaxValue = v, Increment = 1 }
                : null;
        }
    }
}
