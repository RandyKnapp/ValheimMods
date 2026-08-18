using System.Collections.Generic;
using System.Linq;
using EpicLoot.GatedItemType;
using EpicLoot.LegendarySystem;
using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void SpawnMagicItem(Terminal.ConsoleEventArgs args)
    {
        if (Player.m_localPlayer == null) return;

        var rarityArg = args.GetString(1, "random");
        var itemArg = args.GetString(2, "random");
        var count = args.TryParameterInt(3, 1);
        var effectCount = args.TryParameterInt(4, -1);

        args.Context.PrintInfo($"magicitem - rarity:{rarityArg}, item:{itemArg}, count:{count}");

        var allItemNames = GetValidMagicItemNames();

        LootRoller.CheatEffectCount = effectCount;
        for (var i = 0; i < count; i++)
        {
            var rarityTable = GetRarityTable(rarityArg);

            var item = itemArg;
            if (item == "random")
            {
                var weightedRandomTable =
                    new WeightedRandomCollection<string>(allItemNames, x => 1);
                item = weightedRandomTable.Roll();
            }

            if (ObjectDB.instance.GetItemPrefab(item) == null)
            {
                args.Context.PrintWarning($"> Could not find item: {item}");
                break;
            }

            args.Context.PrintInfo($">  {i + 1} - rarity: [{string.Join(", ", rarityTable)}], item: {item}");

            var loot = new LootTable
            {
                Object = "Console",
                Drops = [[1, 1]],
                Loot =
                [
                    new LootDrop { Item = item, Rarity = rarityTable, Weight = 1 }
                ]
            };

            var randomOffset = Random.insideUnitSphere;
            var dropPoint = Player.m_localPlayer.transform.position +
                            Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
            LootRoller.CheatRollingItem = true;
            LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);
            LootRoller.CheatRollingItem = false;
        }

        LootRoller.CheatEffectCount = -1;
    }

    private static List<string> GetSpawnMagicItemOptions(string[] args)
    {
        return args.Length switch
        {
            2 => ["magic", "rare", "epic", "legendary", "mythic"],
            3 => GetValidMagicItemNames(),
            _ => []
        };
    }

    private static List<string> GetValidMagicItemNamesWithRequirements(string effectType)
    {
        List<string> result = [];
        var definition = MagicItemEffectDefinitions.Get(effectType);
        if (definition == null)
        {
            return result;
        }

        for (int i = 0; i < ObjectDB.instance.m_items.Count; ++i)
        {
            var itemPrefab = ObjectDB.instance.m_items[i];
            var itemData = itemPrefab.GetComponent<ItemDrop>().m_itemData.Clone();
            itemData.m_dropPrefab = itemPrefab;
            MagicItem dummyMagicItem = new MagicItem { Rarity = definition.Requirements.AllowedRarities.Count == 0 ? ItemRarity.Magic : definition.Requirements.AllowedRarities.First() };
            if (definition.Requirements.CheckRequirements(itemData, dummyMagicItem))
            {
                result.Add(itemPrefab.name);
            }
        }

        return result;
    }

    private static void SpawnMagicItemWithEffect(Terminal.ConsoleEventArgs args)
    {
        if (args.Length < 3)
        {
            args.Context.PrintWarning("> Specify effect and item name");
            return;
        }

        if (Player.m_localPlayer == null) return;

        string effectArg = args.GetString(1);
        string itemPrefabNameArg = args.GetString(2);
        args.Context.PrintInfo($"magicitem - {itemPrefabNameArg} with effect: {effectArg}");

        MagicItemEffectDefinition magicItemEffectDef = MagicItemEffectDefinitions.Get(effectArg);
        if (magicItemEffectDef == null)
        {
            args.Context.PrintWarning($"> Could not find effect: {effectArg}");
            return;
        }

        GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemPrefabNameArg);
        if (itemPrefab == null)
        {
            args.Context.PrintWarning($"> Could not find item: {itemPrefabNameArg}");
            return;
        }

        ItemDrop.ItemData fromItemData = itemPrefab.GetComponent<ItemDrop>().m_itemData;
        if (!EpicLoot.CanBeMagicItem(fromItemData))
        {
            args.Context.PrintWarning($"> Can't be magic item: {itemPrefabNameArg}");
            return;
        }

        MagicItemEffectRequirements effectRequirements = magicItemEffectDef.Requirements;
        ItemRarity itemRarity = effectRequirements.AllowedRarities.Count == 0 ? ItemRarity.Magic :
            effectRequirements.AllowedRarities.First();
        float[] rarityTable = GetRarityTable(itemRarity.ToString());
        LootTable loot = new LootTable
        {
            Object = "Console",
            Drops = [[1, 1]],
            Loot =
            [
                new LootDrop
                {
                    Item = itemPrefab.name,
                    Rarity = rarityTable
                }
            ]
        };

        Vector3 randomOffset = UnityEngine.Random.insideUnitSphere;
        Vector3 dropPoint = Player.m_localPlayer.transform.position +
            Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
        LootRoller.CheatRollingItem = true;
        LootRoller.CheatForceMagicEffect = true;
        LootRoller.ForcedMagicEffect = effectArg;
        LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);
        LootRoller.CheatForceMagicEffect = false;
        LootRoller.ForcedMagicEffect = string.Empty;
        LootRoller.CheatRollingItem = false;
    }

    private static List<string> GetSpawnMagicItemWithEffectOptions(string[] args)
    {
        return args.Length switch
        {
            2 => MagicItemEffectDefinitions.AllDefinitions.Keys.ToList(),
            3 => GetValidMagicItemNames(),
            _ => []
        };
    }

    private static void SpawnMythicMagicItem(Terminal.ConsoleEventArgs args) =>
        SpawnLegendary(args, ItemRarity.Mythic);

    private static void SpawnLegendaryMagicItem(Terminal.ConsoleEventArgs args) =>
        SpawnLegendary(args, ItemRarity.Legendary);
    
    private static void SpawnLegendary(Terminal.ConsoleEventArgs args, ItemRarity rarity)
    {
        if (args.Length < 2)
        {
            args.Context.PrintWarning("> Specify legendaryID, itemID (optional)");
            return;
        }

        string legendaryID = args.GetString(1);
        string itemType = args.GetString(2);

        if (rarity == ItemRarity.Legendary)
        {
            args.Context.PrintInfo($"magicitemlegendary - legendaryID:{legendaryID}");
        }
        else
        {
            args.Context.PrintInfo($"magicitemmythic - legendaryID:{legendaryID}");
        }
        
        SpawnLegendaryHelper(args, legendaryID, rarity, itemType);
    }

    private static void SpawnLegendaryHelper(Terminal.ConsoleEventArgs args, string legendaryID, ItemRarity rarity, string itemId = "")
    {
        if (!UniqueLegendaryHelper.TryGetLegendaryInfo(legendaryID, out LegendaryInfo itemInfo))
        {
            args.Context.PrintWarning($"> Could not find legendary/mythic info for legendaryID: ({legendaryID})");
            return;
        }

        if (string.IsNullOrEmpty(itemId))
        {
            MagicItem dummyMagicItem = new MagicItem { Rarity = rarity };
            List<ItemDrop> allowedItems = new List<ItemDrop>();
            foreach (string itemName in GatedItemTypeHelper.AllItemsWithDetails.Keys)
            {
                GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
                if (itemPrefab == null)
                {
                    continue;
                }

                ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    continue;
                }

                ItemDrop.ItemData itemData = itemDrop.m_itemData;
                itemData.m_dropPrefab = itemPrefab;
                bool checkRequirements = itemInfo.Requirements.CheckRequirements(itemData, dummyMagicItem);

                if (checkRequirements)
                {
                    allowedItems.Add(itemDrop);
                }
            }

            if (allowedItems.Count == 0)
            {
                args.Context.PrintWarning($"> Could not find suitable items with parameter ({itemId}) for legendaryID: ({legendaryID})");
                return;
            }

            int selected = UnityEngine.Random.Range(0, allowedItems.Count);
            itemId = allowedItems.ElementAt(selected).name;
        }

        if (string.IsNullOrEmpty(itemId))
        {
            args.Context.PrintWarning($"> Could not find suitable item for legendaryID: ({legendaryID})");
            return;
        }

        LootTable loot = new LootTable
        {
            Object = "Console",
            Drops = [[1, 1]],
            Loot =
            [
                new LootDrop
                {
                    Item = itemId,
                    Rarity = GetRarityTable(rarity.ToString())
                }
            ]
        };

        if (rarity == ItemRarity.Legendary)
        {
            LootRoller.CheatForceLegendary = legendaryID;
        }
        else
        {
            LootRoller.CheatForceMythic = legendaryID;
        }

        bool previousDisableGatingState = LootRoller.CheatDisableGating;
        LootRoller.CheatDisableGating = true;

        Vector3 randomOffset = UnityEngine.Random.insideUnitSphere;
        Vector3 dropPoint = Player.m_localPlayer.transform.position +
            Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
        LootRoller.CheatRollingItem = true;
        LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);
        LootRoller.CheatRollingItem = false;
        LootRoller.CheatForceLegendary = null;
        LootRoller.CheatForceMythic = null;
        LootRoller.CheatDisableGating = previousDisableGatingState;
    }

    private static List<string> GetLegendaryOptions(string[] args)
    {
        return args.Length switch
        {
            2 => UniqueLegendaryHelper.LegendaryInfo.Keys.ToList(),
            3 => GetValidLegendaryItemNames(args.GetString(1), ItemRarity.Legendary),
            _ => []
        };
    }

    private static List<string> GetMythicOptions(string[] args)
    {
        return args.Length switch
        {
            2 => UniqueLegendaryHelper.MythicInfo.Keys.ToList(),
            3 => GetValidLegendaryItemNames(args.GetString(1), ItemRarity.Mythic),
            _ => []
        };
    }

    private static List<string> GetValidLegendaryItemNames(string legendaryID, ItemRarity rarity)
    {
        List<string> result = [];
        if (UniqueLegendaryHelper.TryGetLegendaryInfo(legendaryID, out LegendaryInfo itemInfo))
        {
            MagicItem dummyMagicItem = new MagicItem { Rarity = rarity };

            for (int i = 0; i < ObjectDB.instance.m_items.Count; ++i)
            {
                var itemPrefab = ObjectDB.instance.m_items[i];
                var itemData = itemPrefab.GetComponent<ItemDrop>().m_itemData.Clone();
                if (!EpicLoot.CanBeMagicItem(itemData)) continue;
                itemData.m_dropPrefab = itemPrefab;
                if (itemInfo.Requirements.CheckRequirements(itemData, dummyMagicItem))
                {
                    if (result.Contains(itemPrefab.name)) continue;
                    result.Add(itemPrefab.name);
                }
            }
        }

        return result;
    }
}