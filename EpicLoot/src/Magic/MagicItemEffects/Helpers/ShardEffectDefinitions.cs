using EpicLoot.MagicItemEffects.Shards;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.Magic.MagicItemEffects.Helpers {
    // Registers a MagicItemEffectDefinition for every shard effect type that the loaded overhaul config does
    // not already define -- i.e. the new Shardstone-only effects declared in MagicEffectType_Shards.cs.
    // Without a definition, MagicItemEffectDefinitions.Get() synthesizes a blank-named fallback and logs a
    // "definition missing" warning, so socketed shards render with empty tooltip names. The socketed value
    // itself comes from the shard (ShardSocketManager.ResolveSocketedEffect); these definitions supply the
    // display text/requirements and the value ranges used by the loose-shard preview and compendium.
    //
    // Runs from two places, because either config file can reload independently: the
    // OnSetupMagicItemEffectDefinitions event (magiceffects.json) and Shards.InitializeShardDefinitions
    // (shardstones.json). It retracts its own previous output first, so re-running is safe -- and that is
    // what makes a live edit or a server push of the shard grid take effect without a restart. ShardStones
    // types are written with global:: because inside this namespace the name EpicLoot resolves to the
    // EpicLoot class.
    public static class ShardEffectDefinitions {
        // Code-side defaults for the per-effect tunables, which the grid entry's "Config" block overlays per
        // key (BuildConfig).
        //
        // Keeping a code default for every key is not optional: a player's existing on-disk
        // shardstones.json keeps winning until they accept the ConfigVersionManager rewrite prompt, so a
        // key that only exists in the embedded config is simply absent for them until then.
        //
        // Keys also surface in the detailed (Shift) tooltip -- see MagicItem.GetEffectDetailBlock and
        // MagicItemEffectDefinition.GetConfigLabel -- so prefer a key name that already has a shared
        // mod_epicloot_config_<key> label token over inventing a per-effect one.
        private static readonly Dictionary<string, Dictionary<string, float>> EffectConfigs =
            new Dictionary<string, Dictionary<string, float>>
            {
                { MagicEffectType.Everflow, QueenEverflow.DefaultConfig },
                { MagicEffectType.PerfectDodge, PerfectDodge.DefaultConfig },
                { MagicEffectType.PerfectDodgeGivesSpeed, PerfectDodgeGivesSpeed.DefaultConfig },
                { MagicEffectType.LuckWhileFishing, LuckWhileFishing.DefaultConfig },
                // Mercenary, Coinplated, Wager and ChanceToCritOnHit are on no grid slot, so BuildDefinition never
                // reaches them; their defaults stay so a shardstones.json edit can bring any of them back.
                { MagicEffectType.Mercenary, Mercenary.DefaultConfig },
                { MagicEffectType.Coinplated, Coinplated.DefaultConfig },
                { MagicEffectType.Wager, Wager.DefaultConfig },
                { MagicEffectType.Inspiration, Inspiration.DefaultConfig },
                { MagicEffectType.LuckyLoot, LuckyLoot.DefaultConfig },
                { MagicEffectType.Bloodrage, Bloodrage.DefaultConfig },
                { MagicEffectType.AdrenalineIncreasesHealthRegen, AdrenalineIncreasesHealthRegen.DefaultConfig },
                { MagicEffectType.RestingHealthRegen, RestingHealthRegen.DefaultConfig },
                { MagicEffectType.MeteorSummoner, MeteorSummoner.DefaultConfig },
                { MagicEffectType.ShockingCharge, EikthyrShockingCharge.DefaultConfig },
                { MagicEffectType.IcyRetribution, ModerIcyRetribution.DefaultConfig },
                { MagicEffectType.CorpseRot, BonemassCorpseRot.DefaultConfig },
                { MagicEffectType.ForestsAid, ElderForestsAid.DefaultConfig },
                { MagicEffectType.Trailblazer, Trailblazer.DefaultConfig },
                { MagicEffectType.AdrenalineFrostWave, AdrenalineFrostWave.DefaultConfig },
                { MagicEffectType.SummonBatWhenActivatingAdrenaline, SummonBatWhenActivatingAdrenaline.DefaultConfig },
                { MagicEffectType.StrikeCausesLightning, StrikeCausesLightning.DefaultConfig },
                { MagicEffectType.Conduit, Conduit.DefaultConfig },
                { MagicEffectType.HealthGainPerXDamageDone, HealthGainPerXDamageDone.DefaultConfig },
                { MagicEffectType.HealthOnEitrUse, HealthOnEitrUse.DefaultConfig },
                { MagicEffectType.Kindling, Kindling.DefaultConfig },
                { MagicEffectType.KillsReduceNextBloodCost, KillsReduceNextBloodCost.DefaultConfig },
                { MagicEffectType.GainAdrenalineWhenApplyingPoison, GainAdrenalineWhenApplyingPoison.DefaultConfig },
                { MagicEffectType.StormFury, StormFury.DefaultConfig },
                { MagicEffectType.RunningOnEmpty, RunningOnEmpty.DefaultConfig },
                { MagicEffectType.ChanceDoubleDamage, ChanceDoubleDamage.DefaultConfig },
                { MagicEffectType.ChanceToCritOnHit, ChanceToCritOnHit.DefaultConfig },
                { MagicEffectType.BloodDrinker, BloodDrinker.DefaultConfig },
                { MagicEffectType.TravelLight, TravelLight.DefaultConfig },
                { MagicEffectType.BurdenedBlock, BurdenedBlock.DefaultConfig },
                { MagicEffectType.EitrImbueAttack, EitrImbueAttack.DefaultConfig },
            };

        // Effects that need an adrenaline pool but whose type name does not contain "Adrenaline", which is
        // how BuildDefinition infers the ItemHasAdrenaline requirement for the rest of them.
        private static readonly HashSet<string> AdrenalinePoolEffects = new HashSet<string>
        {
            MagicEffectType.StormFury,
        };

        // Definitions this class created, so a rebuild can retract its own previous output. Reference
        // equality is what makes retraction safe: MagicItemEffectDefinitions.Initialize clears
        // AllDefinitions wholesale, and API.AddMagicEffect may have replaced an entry since, so a type we
        // registered last time is only ours to remove if the object under it is still the one we added.
        private static readonly Dictionary<string, MagicItemEffectDefinition> Synthesized =
            new Dictionary<string, MagicItemEffectDefinition>();

        // Definitions owned by someone else (the overhaul config, API.AddMagicEffect) whose Config we
        // overlaid grid keys onto, paired with the Config they carried before we touched it, so the
        // overlay can be lifted and reapplied rather than accumulating across reloads.
        private static readonly List<OverlayRecord> Overlays = new List<OverlayRecord>();

        private class OverlayRecord {
            public MagicItemEffectDefinition Definition;
            public Dictionary<string, float> OriginalConfig;
        }

        public static void RegisterShardEffectDefinitions() {
            RetractPreviousOutput();

            foreach (var pair in CollectShardEffects()) {
                if (MagicItemEffectDefinitions.AllDefinitions.TryGetValue(pair.Key, out var existing) &&
                    existing != null) {
                    // Already defined by the overhaul config or another source, so that definition wins --
                    // but the grid entry's Config still has to mean something, or authoring one on a shared
                    // effect like LifeGainOnHit would silently do nothing. Overlay just those keys.
                    ApplyConfigOverlay(existing, pair.Value);
                    continue;
                }

                var definition = BuildDefinition(pair.Key, pair.Value);
                MagicItemEffectDefinitions.Add(definition);
                Synthesized[pair.Key] = definition;
            }

            // After the merge rather than in BuildDefinition: on a client this definition arrives with the
            // server's magiceffects.json, so the grid only reaches it as an overlay and its text would stay
            // as the server last sent it.
            RestingHealthRegen.RefreshDisplayText();
        }

        // Undoes the last run so this one rebuilds from the current grid instead of no-opping on the
        // "already defined" check. Anything that is no longer ours is left alone.
        private static void RetractPreviousOutput() {
            foreach (var pair in Synthesized) {
                if (MagicItemEffectDefinitions.AllDefinitions.TryGetValue(pair.Key, out var current) &&
                    ReferenceEquals(current, pair.Value)) {
                    MagicItemEffectDefinitions.AllDefinitions.Remove(pair.Key);
                }
            }
            Synthesized.Clear();

            foreach (var overlay in Overlays) {
                // If magiceffects.json reloaded in between, this definition object is gone and its
                // replacement already carries a pristine Config -- nothing to restore.
                if (MagicItemEffectDefinitions.AllDefinitions.TryGetValue(overlay.Definition.Type, out var current) &&
                    ReferenceEquals(current, overlay.Definition)) {
                    overlay.Definition.Config = overlay.OriginalConfig;
                }
            }
            Overlays.Clear();
        }

        private static void ApplyConfigOverlay(MagicItemEffectDefinition definition,
            global::EpicLoot.ShardStones.ShardEffectDefinition shardEffect) {
            if (shardEffect.Config == null || shardEffect.Config.Count == 0) {
                return;
            }

            Overlays.Add(new OverlayRecord {
                Definition = definition,
                OriginalConfig = definition.Config,
            });

            var merged = definition.Config != null
                ? new Dictionary<string, float>(definition.Config)
                : new Dictionary<string, float>();
            foreach (var entry in shardEffect.Config) {
                merged[entry.Key] = entry.Value;
            }
            definition.Config = merged;

            EpicLoot.Log($"Shard grid overlaid {shardEffect.Config.Count} Config key(s) onto the existing " +
                $"'{definition.Type}' enchantment definition.");
        }

        // Every effect type used by any shard, mapped to the grid entry that declares it. Effects are
        // globally unique across shards, so first occurrence wins -- but an effect assigned to several
        // slots is authored once per slot, so a later copy carrying a different Config is a silent
        // divergence worth naming.
        private static Dictionary<string, global::EpicLoot.ShardStones.ShardEffectDefinition> CollectShardEffects() {
            var result = new Dictionary<string, global::EpicLoot.ShardStones.ShardEffectDefinition>();
            var sources = new Dictionary<string, string>();

            void Consider(global::EpicLoot.ShardStones.ShardEffectDefinition effect, string source) {
                if (effect == null || string.IsNullOrEmpty(effect.EffectType)) {
                    return;
                }

                if (!result.ContainsKey(effect.EffectType)) {
                    result[effect.EffectType] = effect;
                    sources[effect.EffectType] = source;
                    return;
                }

                if (!SameConfig(result[effect.EffectType].Config, effect.Config)) {
                    EpicLoot.LogWarning($"Shard effect '{effect.EffectType}' is declared with conflicting " +
                        $"Config blocks: {sources[effect.EffectType]} wins, {source} is ignored. Give every " +
                        "slot that declares this effect the same Config, or leave the duplicates empty.");
                }
            }

            foreach (var shardPair in global::EpicLoot.ShardStones.Shards.ShardDefinitions.ShardEffects) {
                var shard = shardPair.Value;
                if (shard == null) {
                    continue;
                }

                Consider(shard.UniformEffect, $"{shardPair.Key}/Uniform");
                if (shard.TypeEffects != null) {
                    foreach (var slotPair in shard.TypeEffects) {
                        Consider(slotPair.Value, $"{shardPair.Key}/{slotPair.Key}");
                    }
                }
            }

            return result;
        }

        private static bool SameConfig(Dictionary<string, float> a, Dictionary<string, float> b) {
            var countA = a?.Count ?? 0;
            var countB = b?.Count ?? 0;
            if (countA != countB) {
                return false;
            }
            if (countA == 0) {
                return true;
            }

            foreach (var entry in a) {
                if (!b.TryGetValue(entry.Key, out var other) || !Mathf.Approximately(entry.Value, other)) {
                    return false;
                }
            }
            return true;
        }

        private static MagicItemEffectDefinition BuildDefinition(string type,
            global::EpicLoot.ShardStones.ShardEffectDefinition shardEffect) {
            var lower = type.ToLowerInvariant();
            var requirements = new MagicItemEffectRequirements { NoRoll = true };

            // Adrenaline effects only function alongside an adrenaline pool (m_maxAdrenaline > 0).
            if (type.Contains("Adrenaline") || AdrenalinePoolEffects.Contains(type)) {
                requirements.ItemHasAdrenaline = true;
            }

            return new MagicItemEffectDefinition {
                Type = type,
                DisplayText = $"$mod_epicloot_me_{lower}_display",
                Description = $"$mod_epicloot_me_{lower}_desc",
                Requirements = requirements,
                ValuesPerRarity = BuildValues(shardEffect.ValuesPerRarity),
                Config = BuildConfig(type, shardEffect.Config),
                CanBeAugmented = false,
                CanBeDisenchanted = false,
                CanBeRunified = false,
            };
        }

        /// <summary>
        /// The Config a shard effect has with the given grid block (the shipped one, for Quick Configure's
        /// page reset): the code defaults with the authored keys overlaid, exactly as a load builds it.
        /// </summary>
        internal static Dictionary<string, float> ConfigWith(string type, Dictionary<string, float> authored) {
            return BuildConfig(type, authored);
        }

        // Code defaults first, the grid entry's authored keys overlaid on top. Merging rather than
        // replacing is what lets a partial "Config" block retune one knob without blanking the rest --
        // and it is why LuckWhileFishing's treasure table survives someone overriding only TripleChance.
        private static Dictionary<string, float> BuildConfig(string type, Dictionary<string, float> authored) {
            var merged = EffectConfigs.TryGetValue(type, out var defaults)
                ? new Dictionary<string, float>(defaults)
                : new Dictionary<string, float>();

            if (authored != null) {
                foreach (var entry in authored) {
                    merged[entry.Key] = entry.Value;
                }
            }

            return merged;
        }

        private static MagicItemEffectDefinition.ValuesPerRarityDef BuildValues(
            Dictionary<ItemRarity, float> valuesPerRarity) {
            return new MagicItemEffectDefinition.ValuesPerRarityDef {
                Magic = Value(valuesPerRarity, ItemRarity.Magic),
                Rare = Value(valuesPerRarity, ItemRarity.Rare),
                Epic = Value(valuesPerRarity, ItemRarity.Epic),
                Legendary = Value(valuesPerRarity, ItemRarity.Legendary),
                Mythic = Value(valuesPerRarity, ItemRarity.Mythic),
                Ancient = Value(valuesPerRarity, ItemRarity.Ancient),
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
