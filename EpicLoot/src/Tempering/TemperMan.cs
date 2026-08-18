using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace EpicLoot;

public static class TemperMan
{
    public static Dictionary<ItemRarity, TemperRequirement[]> costMap =
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
}