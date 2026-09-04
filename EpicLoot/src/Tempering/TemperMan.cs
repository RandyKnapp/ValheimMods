using System.Collections.Generic;
using EpicLoot.Adventure;
using EpicLoot.Crafting;

namespace EpicLoot;

public static class TemperMan
{
    // The requirements panel is a fixed-height area with no ScrollRect, so only this many rows render.
    public const int MaxRequirementRows = 4;

    // No hardcoded defaults: adventuredata.json's Tempering block is the only source of temper costs,
    // so a rarity a config leaves out is simply not temperable.
    public static Dictionary<ItemRarity, TemperRequirement[]> costMap =
        new Dictionary<ItemRarity, TemperRequirement[]>();

    // Prefabs only resolve once ObjectDB exists, long after the config loads, so an unknown prefab
    // can only be detected on the UI/affordability path - which runs on every selection change.
    // Warn once per rarity+prefab so a bad config doesn't flood the log.
    private static readonly HashSet<string> WarnedInvalidPrefabs = new HashSet<string>();

    /// <summary>
    /// Rebuilds the temper cost table from adventuredata.json's Tempering block, which is the only
    /// source of temper costs. A rarity absent from the block is not temperable at all - that is how a
    /// config turns tempering off for a rarity. A rarity present with an empty list is taken literally:
    /// it is temperable and costs nothing.
    /// </summary>
    public static void ApplyConfig(TemperingConfig config)
    {
        WarnedInvalidPrefabs.Clear();
        costMap = new Dictionary<ItemRarity, TemperRequirement[]>();

        if (config?.CostsByRarity == null || config.CostsByRarity.Count == 0)
        {
            EpicLoot.LogWarning("Tempering: adventuredata.json has no Tempering.CostsByRarity entries, " +
                "so nothing can be tempered. An on-disk adventuredata.json predating the Tempering " +
                "block will do this - add the block or delete the file to pick the shipped one back up.");
            return;
        }

        foreach (KeyValuePair<ItemRarity, List<ItemAmountConfig>> entry in config.CostsByRarity)
        {
            ItemRarity rarity = entry.Key;
            // A null cost list is malformed rather than empty, so it is treated like an omitted rarity
            // (not temperable) instead of like "[]" (temperable, free) - but say so, since the two
            // look nearly identical in the json.
            if (entry.Value == null)
            {
                EpicLoot.LogWarning($"Tempering: rarity {rarity} has a null cost list, treating it as " +
                    "not temperable. Use [] for a temperable rarity that costs nothing.");
                continue;
            }

            List<TemperRequirement> requirements = new List<TemperRequirement>();
            foreach (ItemAmountConfig cost in entry.Value)
            {
                if (cost == null || string.IsNullOrWhiteSpace(cost.Item))
                {
                    EpicLoot.LogWarning($"Tempering: skipping cost entry with no Item name for rarity {rarity}.");
                    continue;
                }
                if (cost.Amount <= 0)
                {
                    EpicLoot.LogWarning($"Tempering: skipping cost entry '{cost.Item}' for rarity {rarity}, Amount must be greater than 0.");
                    continue;
                }
                requirements.Add(new TemperRequirement(cost.Item, cost.Amount));
            }

            if (requirements.Count > MaxRequirementRows)
            {
                EpicLoot.LogWarning($"Tempering: rarity {rarity} has {requirements.Count} cost entries, " +
                    $"but the temper panel only displays {MaxRequirementRows}. All of them are still required and consumed.");
            }

            costMap[rarity] = requirements.ToArray();
        }
    }

    /// <summary>
    /// Whether tempering applies to this rarity at all. Purely a question of whether the config listed
    /// the rarity: an omitted rarity has no costs to charge and its items are kept out of the temper
    /// panel entirely, rather than being tempered for free.
    /// </summary>
    public static bool IsTemperableRarity(ItemRarity rarity) => costMap.ContainsKey(rarity);

    public static TemperRequirement[] GetRequirements(ItemRarity rarity)
    {
        return costMap.TryGetValue(rarity, out TemperRequirement[] requirements) ? requirements : [];
    }

    /// <summary>
    /// The requirements that actually apply right now: everything from <see cref="GetRequirements"/>
    /// whose prefab resolves against ObjectDB. An unresolvable prefab is warned about and skipped, so a
    /// single bad name costs that one ingredient rather than blocking tempering entirely. Every caller
    /// must use this, or the affordability check, the consume call and the displayed list can disagree.
    /// </summary>
    public static TemperRequirement[] GetResolvedRequirements(ItemRarity rarity)
    {
        TemperRequirement[] requirements = GetRequirements(rarity);
        List<TemperRequirement> resolved = new List<TemperRequirement>(requirements.Length);

        foreach (TemperRequirement requirement in requirements)
        {
            if (requirement.isValid)
            {
                resolved.Add(requirement);
                continue;
            }

            if (WarnedInvalidPrefabs.Add($"{rarity}:{requirement.prefab}"))
            {
                EpicLoot.LogWarning($"Tempering: cost item '{requirement.prefab}' for rarity {rarity} " +
                    "could not be found, skipping that requirement.");
            }
        }

        return resolved.ToArray();
    }
}
