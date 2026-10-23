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

            var loot = LootTable.Simple("Console", [[1, 1]],
            [
                new LootDrop { Item = item, Rarity = rarityTable, Weight = 1 }
            ]);

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
            2 => System.Enum.GetNames(typeof(ItemRarity)).Select(x => x.ToLowerInvariant()).ToList(),
            3 => GetValidMagicItemNames(),
            _ => []
        };
    }

    private static List<string> GetValidMagicItemNamesWithRequirements(string effectType)
    {
        List<string> result = [];
        if (!MagicItemEffectDefinitions.TryGet(effectType, out var definition))
        {
            return result;
        }

        for (int i = 0; i < ObjectDB.instance.m_items.Count; ++i)
        {
            var itemPrefab = ObjectDB.instance.m_items[i];
            if (!itemPrefab.TryGetComponent(out ItemDrop itemDrop))
            {
                continue;
            }

            var itemData = itemDrop.m_itemData.Clone();
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

        if (!MagicItemEffectDefinitions.TryGet(effectArg, out MagicItemEffectDefinition magicItemEffectDef))
        {
            args.Context.PrintWarning($"> Could not find effect: {effectArg}");
            return;
        }

        GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemPrefabNameArg);
        if (itemPrefab == null || !itemPrefab.TryGetComponent(out ItemDrop itemDrop))
        {
            args.Context.PrintWarning($"> Could not find item: {itemPrefabNameArg}");
            return;
        }

        ItemDrop.ItemData fromItemData = itemDrop.m_itemData;
        if (!EpicLoot.CanBeMagicItem(fromItemData))
        {
            args.Context.PrintWarning($"> Can't be magic item: {itemPrefabNameArg}");
            return;
        }

        MagicItemEffectRequirements effectRequirements = magicItemEffectDef.Requirements;
        ItemRarity itemRarity = effectRequirements.AllowedRarities.Count == 0 ? ItemRarity.Magic :
            effectRequirements.AllowedRarities.First();
        float[] rarityTable = GetRarityTable(itemRarity.ToString());
        LootTable loot = LootTable.Simple("Console", [[1, 1]],
        [
            new LootDrop
            {
                Item = itemPrefab.name,
                Rarity = rarityTable
            }
        ]);

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

    // magicsetitem <id> [item|random] [rarity|random]: any unique or set piece, at any rarity.
    private static void SpawnSetItem(Terminal.ConsoleEventArgs args)
    {
        if (args.Length < 2)
        {
            args.Context.PrintWarning("> Specify uniqueID, item (optional or random), rarity (optional or random)");
            return;
        }

        string legendaryID = args.GetString(1);
        string itemArg = args.GetString(2);
        string rarityArg = args.GetString(3);

        if (!UniqueLegendaryHelper.TryGetLegendaryInfo(legendaryID, out LegendaryInfo itemInfo))
        {
            args.Context.PrintWarning($"> Could not find unique or set piece: ({legendaryID})");
            return;
        }

        List<ItemRarity> enabled = UniqueLegendaryHelper.GetRarities(itemInfo);
        if (!TryResolveSpawnRarity(args, legendaryID, enabled, rarityArg, out ItemRarity rarity))
        {
            return;
        }

        args.Context.PrintInfo($"magicsetitem - id:{legendaryID} rarity:{rarity}");
        SpawnLegendaryHelper(args, legendaryID, rarity, IsRandomArg(itemArg) ? "" : itemArg);
    }

    private static bool IsRandomArg(string arg)
    {
        return string.IsNullOrEmpty(arg) || arg.Equals("random", System.StringComparison.OrdinalIgnoreCase);
    }

    // Omitted or "random" picks one of the rarities the unique or set is enabled at. An explicit rarity it
    // is not enabled at still spawns, with a warning, so any tier can be tested.
    private static bool TryResolveSpawnRarity(Terminal.ConsoleEventArgs args, string id, List<ItemRarity> enabled,
        string rarityArg, out ItemRarity rarity)
    {
        if (IsRandomArg(rarityArg))
        {
            rarity = enabled[Random.Range(0, enabled.Count)];
            return true;
        }

        if (!System.Enum.TryParse(rarityArg, true, out rarity) || !System.Enum.IsDefined(typeof(ItemRarity), rarity))
        {
            args.Context.PrintWarning($"> Unknown rarity: ({rarityArg})");
            return false;
        }

        if (!enabled.Contains(rarity))
        {
            args.Context.PrintWarning($"> {id} does not roll at {rarity} (it rolls at {string.Join(", ", enabled)}); " +
                $"spawning it anyway.");
        }

        return true;
    }

    private static void SpawnLegendaryHelper(Terminal.ConsoleEventArgs args, string legendaryID, ItemRarity rarity, string itemId = "")
    {
        if (!UniqueLegendaryHelper.TryGetLegendaryInfo(legendaryID, out LegendaryInfo itemInfo))
        {
            args.Context.PrintWarning($"> Could not find unique or set piece: ({legendaryID})");
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
                args.Context.PrintWarning($"> Could not find suitable items for: ({legendaryID}) at {rarity}");
                return;
            }

            int selected = UnityEngine.Random.Range(0, allowedItems.Count);
            itemId = allowedItems.ElementAt(selected).name;
        }

        if (string.IsNullOrEmpty(itemId))
        {
            args.Context.PrintWarning($"> Could not find suitable item for: ({legendaryID})");
            return;
        }

        LootTable loot = LootTable.Simple("Console", [[1, 1]],
        [
            new LootDrop
            {
                Item = itemId,
                Rarity = GetRarityTable(rarity.ToString())
            }
        ]);

        LootRoller.CheatForceUniqueID = legendaryID;
        LootRoller.CheatForceUniqueRarity = rarity;

        bool previousDisableGatingState = LootRoller.CheatDisableGating;
        LootRoller.CheatDisableGating = true;

        Vector3 randomOffset = UnityEngine.Random.insideUnitSphere;
        Vector3 dropPoint = Player.m_localPlayer.transform.position +
            Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
        LootRoller.CheatRollingItem = true;
        LootRoller.RollLootTableAndSpawnObjects(loot, 1, loot.Object, dropPoint);
        LootRoller.CheatRollingItem = false;
        LootRoller.CheatForceUniqueID = null;
        LootRoller.CheatForceUniqueRarity = null;
        LootRoller.CheatDisableGating = previousDisableGatingState;
    }

    private static List<string> GetSetItemOptions(string[] args)
    {
        return args.Length switch
        {
            2 => UniqueLegendaryHelper.AllUniques.Keys.OrderBy(x => x).ToList(),
            3 => ["random", .. GetValidLegendaryItemNames(args.GetString(1), GetFirstRarity(args.GetString(1)))],
            4 => GetRarityOptions(UniqueLegendaryHelper.TryGetLegendaryInfo(args.GetString(1), out LegendaryInfo info)
                ? UniqueLegendaryHelper.GetRarities(info)
                : null),
            _ => []
        };
    }

    private static ItemRarity GetFirstRarity(string legendaryID)
    {
        return UniqueLegendaryHelper.TryGetLegendaryInfo(legendaryID, out LegendaryInfo info)
            ? UniqueLegendaryHelper.GetRarities(info)[0]
            : ItemRarity.Legendary;
    }

    // "random" plus the rarities an ID is enabled at, or every rarity when the ID is unknown.
    private static List<string> GetRarityOptions(List<ItemRarity> enabled)
    {
        List<string> result = ["random"];
        result.AddRange((enabled ?? Rarities.All.ToList()).Select(x => x.ToString()));
        return result;
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
                if (!itemPrefab.TryGetComponent(out ItemDrop itemDrop))
                {
                    continue;
                }

                var itemData = itemDrop.m_itemData.Clone();
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