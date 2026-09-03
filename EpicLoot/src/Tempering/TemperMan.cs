using System.Collections.Generic;
using EpicLoot.Adventure;
using EpicLoot.Crafting;

namespace EpicLoot;

public static class TemperMan
{
    // The requirements panel is a fixed-height area with no ScrollRect, so only this many rows render.
    public const int MaxRequirementRows = 4;

    private static readonly Dictionary<ItemRarity, TemperRequirement[]> DefaultCostMap =
        new Dictionary<ItemRarity, TemperRequirement[]>()
        {
            [ItemRarity.Magic] = [
                // new TemperRequirement("ShardMagic", 10), 
                new TemperRequirement("Coins", 10),
                new TemperRequirement("EssenceMagic", 10),
                new TemperRequirement("ReagentMagic", 10),
                new TemperRequirement("DustMagic", 10)
            ],
            [ItemRarity.Rare] = [
                // new TemperRequirement("ShardRare", 10), 
                new TemperRequirement("ForestToken", 1), 
                new TemperRequirement("EssenceRare", 10),
                new TemperRequirement("ReagentRare", 10),
                new TemperRequirement("DustRare", 10)
            ],
            [ItemRarity.Epic] = [
                // new TemperRequirement("ShardEpic", 10),  
                new TemperRequirement("IronBountyToken", 1),  
                new TemperRequirement("EssenceEpic", 10),
                new TemperRequirement("ReagentEpic", 10),
                new TemperRequirement("DustEpic", 10)
            ],
            [ItemRarity.Legendary] = [
                // new TemperRequirement("ShardLegendary", 10),   
                new TemperRequirement("GoldBountyToken", 1),   
                new TemperRequirement("EssenceLegendary", 10),
                new TemperRequirement("ReagentLegendary", 10),
                new TemperRequirement("DustLegendary", 10)
            ],
            [ItemRarity.Mythic] = [
                // new TemperRequirement("ShardMythic", 10),  
                new TemperRequirement("GoldBountyToken", 2),   
                new TemperRequirement("EssenceMythic", 10),
                new TemperRequirement("ReagentMythic", 10),
                new TemperRequirement("DustMythic", 10)
            ],
        };

    public static Dictionary<ItemRarity, TemperRequirement[]> costMap =
        new Dictionary<ItemRarity, TemperRequirement[]>(DefaultCostMap);

    // Prefabs only resolve once ObjectDB exists, long after the config loads, so an unknown prefab
    // can only be detected on the UI/affordability path - which runs on every selection change.
    // Warn once per rarity+prefab so a bad config doesn't flood the log.
    private static readonly HashSet<string> WarnedInvalidPrefabs = new HashSet<string>();

    /// <summary>
    /// Rebuilds the temper cost table from adventuredata.json's Tempering block. A missing block, or a
    /// rarity absent from it, keeps that rarity's hardcoded default. A rarity present with an empty
    /// list is taken literally: tempering it is free.
    /// </summary>
    public static void ApplyConfig(TemperingConfig config)
    {
        WarnedInvalidPrefabs.Clear();
        costMap = new Dictionary<ItemRarity, TemperRequirement[]>(DefaultCostMap);

        if (config?.CostsByRarity == null || config.CostsByRarity.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<ItemRarity, List<ItemAmountConfig>> entry in config.CostsByRarity)
        {
            ItemRarity rarity = entry.Key;
            if (entry.Value == null)
            {
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

    public static TemperRequirement[] GetRequirements(ItemRarity rarity)
    {
        if (costMap.TryGetValue(rarity, out TemperRequirement[] requirements))
        {
            return requirements;
        }
        return [
            new  TemperRequirement("Coins", 10)
        ];
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
