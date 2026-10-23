using EpicLoot.Config;
using EpicLoot_UnityLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EpicLoot.QuickConfig;

/// <summary>
/// Edits to the baseconfig JSON files. Each edit parses the file to a JObject, changes only the tokens
/// the panel staged (<see cref="ApplyStaged"/>), writes it back indented and re-reads it through
/// <see cref="ELConfig.ReloadBaseConfigsFromDisk"/> (which also pushes it to connected peers). The host
/// edits its own files here; a server admin runs the same <see cref="ApplyStaged"/> over the server's
/// copy and sends the result to the server (<see cref="QuickConfigServerSave"/>).
///
/// These writes NEVER call <see cref="ConfigVersionManager.RecordWrittenContent"/>: what the panel
/// writes is the player's own configuration. Stamping it as the mod's output would tell the next
/// launch's version check the file was untouched, and a mod update would then silently replace it.
/// </summary>
internal static class JsonConfigEdits {
    internal static bool Edit(string fileName, Action<JObject> mutate, out string message) {
        message = "";
        if (QuickConfigBindings.IsHost() == false) {
            message = $"{fileName} can only be changed by the host.";
            return false;
        }

        string path;
        try {
            path = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), fileName);
            if (File.Exists(path) == false) {
                message = $"{fileName} was not found in the baseconfig folder.";
                return false;
            }
            JObject root = JObject.Parse(File.ReadAllText(path));
            mutate(root);
            File.WriteAllText(path, root.ToString(Formatting.Indented));
        } catch (Exception e) {
            EpicLoot.LogWarningForce($"Quick Configure could not write {fileName}: {e}");
            message = $"{fileName} was not saved: {e.Message}";
            return false;
        }

        ELConfig.ReloadBaseConfigsFromDisk(new[] { fileName });
        message = $"{fileName} updated.";
        return true;
    }

    /// <summary>
    /// Writes every baseconfig file with a staged change, in apply order (magiceffects.json last).
    /// Failures are collected per file; the other files are still written, and listed in <paramref name="written"/>.
    /// </summary>
    internal static void ApplyDirty(StagedConfig staged, StagedConfig baseline, List<string> failures, List<string> notes,
        HashSet<string> written) {
        foreach (string file in staged.DirtyJsonFiles(baseline)) {
            int changed = 0;
            bool ok = Edit(file, root => changed = ApplyStaged(root, file, staged, baseline), out string message);
            if (ok) {
                EpicLoot.Log($"Quick Configure wrote {changed} value(s) to {file}.");
                notes.Add(message);
                written.Add(file);
                string patchedNote = PatchedNote(file);
                if (patchedNote != null) {
                    EpicLoot.LogWarningForce($"Quick Configure: {patchedNote}");
                    notes.Add(patchedNote);
                }
            } else {
                failures.Add(message);
            }
        }
    }

    /// <summary>
    /// Applies every staged change to one file onto its parsed JSON: only the values that differ from
    /// the baseline, each through its slot's writer. Returns how many slots were written.
    /// </summary>
    internal static int ApplyStaged(JObject root, string file, StagedConfig staged, StagedConfig baseline) {
        List<JsonSlot> dirty = staged.DirtyJsonSlots(file, baseline);
        foreach (JsonSlot slot in dirty) {
            slot.Apply(root, staged.GetRaw(slot.Key), baseline?.GetRaw(slot.Key));
        }
        return dirty.Count;
    }

    /// <summary>
    /// The note for an edit to a file that patch files target, or null. FilePatching rebuilds such a file
    /// from the embedded default plus its patches on every launch, which replaces this edit just as it
    /// would a hand edit. Asked of the machine that wrote the file, since the patches are its own.
    /// </summary>
    internal static string PatchedNote(string file) {
        if (BalancePreset.IsPatched(Path.GetFileNameWithoutExtension(file)) == false) { return null; }
        return $"{file} is rebuilt from its patch files at every launch, so this change lasts until the game restarts.";
    }

    // --- JObject helpers -------------------------------------------------------------------------

    /// <summary>Sets root.a.b.c = value, creating the objects along the way.</summary>
    internal static void SetPath(JObject root, string dottedPath, JToken value) {
        string[] parts = dottedPath.Split('.');
        JObject current = root;
        for (int i = 0; i < parts.Length - 1; i++) {
            if (current[parts[i]] is JObject next == false) {
                next = new JObject();
                current[parts[i]] = next;
            }
            current = next;
        }
        current[parts[parts.Length - 1]] = value;
    }

    /// <summary>A [count, weight] table as JSON; whole weights are written as integers, like the shipped file.</summary>
    internal static JArray TableToJArray(float[][] rows) {
        JArray table = new JArray();
        foreach (float[] row in rows) {
            table.Add(new JArray(Number(row[0]), Number(row[1])));
        }
        return table;
    }

    /// <summary>The MagicItemEffects element with this Type, or null.</summary>
    internal static JObject FindEffect(JObject root, string effectType) {
        if (root["MagicItemEffects"] is JArray effects == false) { return null; }
        foreach (JToken token in effects) {
            if (token is JObject effect && (string)effect["Type"] == effectType) { return effect; }
        }
        return null;
    }

    /// <summary>Replaces (or creates) the Config object of the magiceffects.json element with this Type, keys in staged order.</summary>
    internal static void ReplaceEffectConfig(JObject root, string effectType, List<EffectConfigEntry> entries) {
        JObject effect = FindEffect(root, effectType)
            ?? throw new InvalidDataException($"magiceffects.json has no effect of type {effectType}");
        effect["Config"] = ConfigObject(entries);
    }

    /// <summary>
    /// Writes the staged Config into every shardstones.json slot entry (TypeEffects.* and UniformEffect
    /// of every colour) whose EffectType matches and which already carries a Config. Entries without
    /// one are left alone: the code default applies there and a partial block would retune one slot.
    /// </summary>
    internal static int SetShardEffectConfigs(JObject root, string effectType, List<EffectConfigEntry> entries) {
        int written = 0;
        foreach (JObject entry in EffectConfigTables.ShardSlotEntries(root)) {
            if ((string)entry["EffectType"] != effectType || entry["Config"] is JObject == false) { continue; }
            entry["Config"] = ConfigObject(entries);
            written++;
        }
        return written;
    }

    private static JObject ConfigObject(List<EffectConfigEntry> entries) {
        JObject config = new JObject();
        foreach (EffectConfigEntry entry in entries) {
            config[entry.Key.Trim()] = Number(entry.Value);
        }
        return config;
    }

    /// <summary>
    /// Rebuilds Bounties.Targets: the file's biomes in their existing order, each with its staged
    /// entries (an edited entry keeps its Adds and any other property), then biomes new to the file.
    /// Nothing else under Bounties changes.
    /// </summary>
    internal static void SetBountyTargets(JObject root, BountiesValue value) {
        if (root["Bounties"] is JObject bounties == false) {
            throw new InvalidDataException("adventuredata.json has no Bounties object");
        }
        JArray targets = new JArray();
        foreach (string biome in value.FileBiomes) { AddBountyTargets(targets, biome, value.Get(biome)); }
        foreach (string biome in value.BiomeOrder) {
            if (value.FileBiomes.Contains(biome) == false) { AddBountyTargets(targets, biome, value.Get(biome)); }
        }
        bounties["Targets"] = targets;
    }

    private static void AddBountyTargets(JArray targets, string biome, List<BountyEntry> entries) {
        if (entries == null) { return; }
        foreach (BountyEntry entry in entries) {
            JObject target = new JObject {
                ["Biome"] = biome,
                ["TargetID"] = entry.TargetID.Trim(),
                ["RewardGold"] = entry.RewardGold,
                ["RewardIron"] = entry.RewardIron,
                ["RewardCoins"] = entry.RewardCoins
            };
            if (entry.Extra != null) {
                foreach (JProperty property in entry.Extra.Properties()) { target[property.Name] = property.Value.DeepClone(); }
            }
            targets.Add(target);
        }
    }

    /// <summary>
    /// Sets Cost and ForestTokens on each TreasureMap.BiomeInfo element whose Biome is in the staged
    /// list. Every other property (radii, tokens the panel does not show) and any biome not staged
    /// stay exactly as the file has them.
    /// </summary>
    internal static void SetBiomeCosts(JObject root, List<BiomeCostEntry> entries) {
        if (root["TreasureMap"]?["BiomeInfo"] is JArray infos == false) {
            throw new InvalidDataException("adventuredata.json has no TreasureMap.BiomeInfo array");
        }
        foreach (BiomeCostEntry entry in entries) {
            foreach (JToken token in infos) {
                if (token is JObject info && string.Equals((string)info["Biome"], entry.Biome, StringComparison.OrdinalIgnoreCase)) {
                    info["Cost"] = entry.Cost;
                    info["ForestTokens"] = entry.ForestTokens;
                }
            }
        }
    }

    /// <summary>
    /// Rebuilds one iteminfo.json category's ItemsByBoss from its staged rows: the file's existing
    /// boss keys keep their order (an emptied key stays as an empty array), new keys are appended in
    /// the order they first appear. Type, Fallback, ItemFallback, IgnoredItems and the obsolete Items
    /// list are not touched.
    /// </summary>
    internal static void SetItemsByBoss(JObject root, string category, List<ItemByBossEntry> entries) {
        if (root["ItemInfo"] is JArray infos == false) {
            throw new InvalidDataException("iteminfo.json has no ItemInfo array");
        }
        JObject element = null;
        foreach (JToken token in infos) {
            if (token is JObject info && (string)info["Type"] == category) {
                element = info;
                break;
            }
        }
        if (element == null) {
            throw new InvalidDataException($"iteminfo.json has no category named {category}");
        }

        JObject existing = element["ItemsByBoss"] as JObject ?? new JObject();
        JObject rebuilt = new JObject();
        foreach (JProperty property in existing.Properties()) {
            rebuilt[property.Name] = ItemsFor(entries, property.Name);
        }
        foreach (ItemByBossEntry entry in entries) {
            if (rebuilt.ContainsKey(entry.Boss) == false) {
                rebuilt[entry.Boss] = ItemsFor(entries, entry.Boss);
            }
        }
        element["ItemsByBoss"] = rebuilt;
    }

    private static JArray ItemsFor(List<ItemByBossEntry> entries, string boss) {
        JArray items = new JArray();
        foreach (ItemByBossEntry entry in entries) {
            if (entry.Boss == boss) { items.Add(entry.Item.Trim()); }
        }
        return items;
    }

    /// <summary>
    /// Writes one drop target of loottables.json: the Drops table of the N-th LootTables element named
    /// row.Object (its LeveledLoot element with row.Level when the row is leveled), and the Rarity
    /// weights onto the Loot entries the row covers (BiomeDropTables.RarityTargets): those already
    /// carrying a Rarity array, less the rarity-keyed ones whose Rarity picks the item. Entries without
    /// one and everything else in the table are left alone. The caller passes the tables already
    /// rebalanced; a null one is not written.
    /// </summary>
    internal static void SetBiomeDrops(JObject root, BiomeDropRow row, List<WeightEntry> amount, List<WeightEntry> rarity) {
        if (root["LootTables"] is JArray tables == false) {
            throw new InvalidDataException("loottables.json has no LootTables array");
        }
        // The live config is always LeveledLoot, so the rows address levels; a file still holding a flat
        // table (a player's edit since the last load) is brought into the same shape first.
        LootTableMigration.NormalizeJson(root);
        JObject table = null;
        int seen = 0;
        foreach (JToken token in tables) {
            if (token is JObject candidate && (string)candidate["Object"] == row.Object && seen++ == row.Occurrence) {
                table = candidate;
                break;
            }
        }
        if (table == null) {
            throw new InvalidDataException($"loottables.json has no table for {row.Object}");
        }

        JObject target = table;
        if (row.Level.HasValue) {
            target = (table["LeveledLoot"] as JArray)?.OfType<JObject>().FirstOrDefault(def => (int?)def["Level"] == row.Level)
                ?? throw new InvalidDataException($"loottables.json: {row.Object} has no LeveledLoot entry for level {row.Level}");
        }

        if (amount != null) {
            target["Drops"] = TableToJArray(WeightTable.ToRows(amount));
        }
        if (rarity != null && target["Loot"] is JArray loot) {
            float[] weights = WeightTable.ToWeights(rarity, Rarities.Count);
            int length = BiomeDropTables.WriteLength(weights, row.RarityLength);
            HashSet<string> keyedSets = BiomeDropTables.RarityKeyedSets(root["ItemSets"] as JArray);
            List<JObject> targets = BiomeDropTables.RarityTargets(loot.OfType<JObject>(), entry => entry["Rarity"] is JArray,
                entry => BiomeDropTables.IsRarityKeyed((string)entry["Item"], BiomeDropTables.HasRarityItems(entry), keyedSets));
            foreach (JObject entry in targets) {
                entry["Rarity"] = new JArray(weights.Take(length).Select(Number));
            }
        }
    }

    /// <summary>
    /// Grows UpgradeCosts.{feature} and UpgradeValues.{feature} in enchantingupgrades.json to at least
    /// <paramref name="count"/> levels, each new level a copy of the last: the table reads both for every
    /// level up to the max, and a missing value shows as NaN. Never shrinks either list, so levels above
    /// a lowered max stay in the file.
    /// </summary>
    internal static void EnsureUpgradeLevels(JObject root, EnchantingFeature feature, int count) {
        PadWithLast(root["UpgradeCosts"]?[feature.ToString()] as JArray, count, new JArray());
        // A value list the file lacks or leaves empty has nothing to repeat; that is the file's problem.
        if (root["UpgradeValues"]?[feature.ToString()] is JArray values && values.Count > 0) {
            PadWithLast(values, count, null);
        }
    }

    private static void PadWithLast(JArray levels, int count, JToken whenEmpty) {
        if (levels == null) { return; }
        while (levels.Count < count) {
            JToken last = levels.Count > 0 ? levels[levels.Count - 1] : null;
            levels.Add(last != null && last.Type != JTokenType.Null ? last.DeepClone() : whenEmpty?.DeepClone() ?? JValue.CreateNull());
        }
    }

    /// <summary>
    /// Replaces one level's cost list of UpgradeCosts.{feature} in enchantingupgrades.json. The level
    /// must exist: <see cref="EnsureUpgradeLevels"/> adds it, with its UpgradeValues entry.
    /// </summary>
    internal static void SetUpgradeCost(JObject root, EnchantingFeature feature, int level, List<CostEntry> cost) {
        if (root["UpgradeCosts"]?[feature.ToString()] is JArray levels == false) {
            throw new InvalidDataException($"enchantingupgrades.json has no UpgradeCosts.{feature} array");
        }
        if (level < 0 || level >= levels.Count) {
            throw new InvalidDataException($"enchantingupgrades.json: UpgradeCosts.{feature} has no level {level}");
        }
        JArray items = new JArray();
        foreach (CostEntry entry in cost) {
            items.Add(new JObject { ["Item"] = entry.Item.Trim(), ["Amount"] = entry.Amount });
        }
        levels[level] = items;
    }

    private static JToken Number(float value) {
        return Math.Abs(value - Math.Round(value)) < 0.00001f ? new JValue((int)Math.Round(value)) : new JValue(value);
    }
}
