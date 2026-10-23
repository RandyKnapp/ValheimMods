using EpicLoot.Adventure;
using EpicLoot.Biomes;
using EpicLoot.Config;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EpicLoot.QuickConfig;

/// <summary>One adventuredata.json Bounties.Targets element. Extra keeps every property the panel does not edit (Adds...).</summary>
internal sealed class BountyEntry {
    internal string TargetID = "";
    internal int RewardIron;
    internal int RewardGold;
    internal int RewardCoins;
    internal JObject Extra;

    internal BountyEntry Clone() => new BountyEntry {
        TargetID = TargetID, RewardIron = RewardIron, RewardGold = RewardGold, RewardCoins = RewardCoins,
        Extra = Extra?.DeepClone() as JObject
    };

    internal static bool Same(BountyEntry a, BountyEntry b) {
        return a.TargetID == b.TargetID && a.RewardIron == b.RewardIron && a.RewardGold == b.RewardGold
            && a.RewardCoins == b.RewardCoins && JToken.DeepEquals(a.Extra, b.Extra);
    }
}

/// <summary>Bounty targets per biome: the file's biomes in their order, then registry biomes the file does not name yet.</summary>
internal sealed class BountiesValue {
    /// <summary>Biome strings as the file spells them, in first-appearance order.</summary>
    internal readonly List<string> FileBiomes = new List<string>();
    /// <summary>FileBiomes followed by every registry biome not already covered, so a bounty can be added to an empty biome.</summary>
    internal readonly List<string> BiomeOrder = new List<string>();
    internal readonly Dictionary<string, List<BountyEntry>> Entries = new Dictionary<string, List<BountyEntry>>(StringComparer.Ordinal);

    internal List<BountyEntry> Get(string biome) {
        return biome != null && Entries.TryGetValue(biome, out List<BountyEntry> list) ? list : null;
    }

    internal BountiesValue Clone() {
        BountiesValue clone = new BountiesValue();
        clone.FileBiomes.AddRange(FileBiomes);
        clone.BiomeOrder.AddRange(BiomeOrder);
        foreach (KeyValuePair<string, List<BountyEntry>> pair in Entries) {
            clone.Entries[pair.Key] = pair.Value.Select(entry => entry.Clone()).ToList();
        }
        return clone;
    }

    internal static bool Same(BountiesValue a, BountiesValue b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a == null || b == null || a.BiomeOrder.Count != b.BiomeOrder.Count) { return false; }
        for (int i = 0; i < a.BiomeOrder.Count; i++) {
            string biome = a.BiomeOrder[i];
            if (biome != b.BiomeOrder[i]) { return false; }
            List<BountyEntry> listA = a.Get(biome) ?? new List<BountyEntry>();
            List<BountyEntry> listB = b.Get(biome) ?? new List<BountyEntry>();
            if (listA.Count != listB.Count) { return false; }
            for (int j = 0; j < listA.Count; j++) {
                if (BountyEntry.Same(listA[j], listB[j]) == false) { return false; }
            }
        }
        return true;
    }
}

/// <summary>Reads adventuredata.json's Bounties.Targets as the panel edits it, and the monster list the picker offers.</summary>
internal static class BountyTables {
    private static readonly string[] EditedProperties = { "Biome", "TargetID", "RewardGold", "RewardIron", "RewardCoins" };

    /// <summary>The on-disk file's targets (what the write rebuilds), or null when the file has no Bounties.Targets.</summary>
    internal static BountiesValue Read() {
        JObject root;
        try {
            string path = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), "adventuredata.json");
            if (File.Exists(path) == false) { return null; }
            root = JObject.Parse(File.ReadAllText(path));
        } catch (Exception e) {
            EpicLoot.LogWarning($"Quick Configure could not read adventuredata.json for the bounty editor: {e.Message}");
            return null;
        }
        return Read(root);
    }

    /// <summary>The targets of any adventuredata.json root (the shipped default, for a page reset).</summary>
    internal static BountiesValue Read(JObject root) {
        if (root?["Bounties"]?["Targets"] is JArray targets == false) { return null; }

        BountiesValue value = new BountiesValue();
        foreach (JToken token in targets) {
            if (token is JObject target == false) { continue; }
            string biome = (string)target["Biome"] ?? "";
            if (value.Entries.TryGetValue(biome, out List<BountyEntry> list) == false) {
                list = new List<BountyEntry>();
                value.Entries[biome] = list;
                value.FileBiomes.Add(biome);
            }
            JObject extra = (JObject)target.DeepClone();
            foreach (string property in EditedProperties) { extra.Remove(property); }
            list.Add(new BountyEntry {
                TargetID = (string)target["TargetID"] ?? "",
                RewardIron = (int?)target["RewardIron"] ?? 0,
                RewardGold = (int?)target["RewardGold"] ?? 0,
                RewardCoins = (int?)target["RewardCoins"] ?? 0,
                Extra = extra.HasValues ? extra : null
            });
        }

        value.BiomeOrder.AddRange(value.FileBiomes);
        try {
            foreach (BiomeDefinition definition in BiomeDataManager.BiomesInOrder) {
                if (string.IsNullOrEmpty(definition?.Name)) { continue; }
                bool covered = value.FileBiomes.Any(name => string.Equals(name, definition.Name, StringComparison.OrdinalIgnoreCase)
                    || (BiomeDataManager.TryResolve(name, out Heightmap.Biome resolved) && resolved == definition.Biome));
                if (covered) { continue; }
                value.BiomeOrder.Add(definition.Name);
                value.Entries[definition.Name] = new List<BountyEntry>();
            }
        } catch (Exception) {
            // Registry not loaded: the file's own biomes are still editable.
        }
        return value;
    }

    /// <summary>
    /// Creature prefab names: in a world, every ZNetScene prefab with a Character and no Player; on
    /// the main menu the union of the current bounty targets and the loot tables' creature objects.
    /// Null when nothing is known, so the picker button hides.
    /// </summary>
    internal static IList<string> MonsterNames() {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        try {
            if (ZNetScene.instance != null && ZNetScene.instance.m_prefabs != null) {
                foreach (GameObject prefab in ZNetScene.instance.m_prefabs) {
                    if (prefab == null || prefab.GetComponent<Character>() == null || prefab.GetComponent<Player>() != null) { continue; }
                    names.Add(prefab.name);
                }
            } else {
                List<BountyTargetConfig> targets = AdventureDataManager.Config?.Bounties?.Targets;
                if (targets != null) {
                    foreach (BountyTargetConfig target in targets) {
                        if (string.IsNullOrEmpty(target?.TargetID) == false) { names.Add(target.TargetID); }
                    }
                }
                LootTable[] tables = LootRoller.Config?.LootTables;
                if (tables != null) {
                    foreach (LootTable table in tables) {
                        if (table != null && string.IsNullOrEmpty(table.RefObject) == false && string.IsNullOrEmpty(table.Object) == false) { names.Add(table.Object); }
                    }
                }
            }
        } catch (Exception e) {
            EpicLoot.LogWarning($"Quick Configure could not list monsters for the bounty picker: {e.Message}");
        }
        return names.Count > 0 ? names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList() : null;
    }
}
