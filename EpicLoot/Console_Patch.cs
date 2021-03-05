using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Console), "InputText")]
    public static class Console_Patch
    {
        private static readonly System.Random _random = new System.Random();

        public static bool Prefix(Console __instance)
        {
            var input = __instance.m_input.text;
            var args = input.Split(' ');
            if (args.Length == 0)
            {
                return true;
            }

            var command = args[0];
            if (command.Equals("magicitem", StringComparison.InvariantCultureIgnoreCase) ||
                command.Equals("mi", StringComparison.InvariantCultureIgnoreCase))
            {
                MagicItem(__instance, args);
                return false;
            }
            else if (command.Equals("checkstackquality", StringComparison.InvariantCultureIgnoreCase))
            {
                CheckStackQuality(__instance);
                return false;
            }

            return true;
        }

        public static void MagicItem(Console __instance, string[] args)
        {
            var rarityArg = args.Length >= 2 ? args[1] : "random";
            var itemArg = args.Length >= 3 ? args[2] : "random";
            var count = args.Length >= 4 ? int.Parse(args[3]) : 1;

            __instance.AddString($"magicitem - rarity:{rarityArg}, item:{itemArg}, count:{count}");

            var items = new List<GameObject>();
            var allItemNames = ObjectDB.instance.m_items
                .Where(x => EpicLoot.CanBeMagicItem(x.GetComponent<ItemDrop>().m_itemData))
                .Select(x => x.name)
                .ToList();

            if (Player.m_localPlayer == null)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                var rarityTable = new[] { 1, 1, 1, 1 };
                if (rarityArg != "random" && !Enum.TryParse(rarityArg, out ItemRarity rarity))
                {
                    __instance.AddString($"> Could not parse rarity ({rarityArg}) using random instead");
                    switch (rarity)
                    {
                        case ItemRarity.Magic: rarityTable = new[] { 1, 0, 0, 0, }; break;
                        case ItemRarity.Rare: rarityTable = new[] { 0, 1, 0, 0, }; break;
                        case ItemRarity.Epic: rarityTable = new[] { 0, 0, 1, 0, }; break;
                        case ItemRarity.Legendary: rarityTable = new[] { 0, 0, 0, 1, }; break;
                    }
                }

                var item = itemArg;
                if (item == "random")
                {
                    var weightedRandomTable = new WeightedRandomCollection<string>(_random, allItemNames, x => 1);
                    item = weightedRandomTable.Roll();
                }

                if (ObjectDB.instance.GetItemPrefab(item) == null)
                {
                    __instance.AddString($"> Could not find item: {item}");
                    break;
                }

                __instance.AddString($"  {i + 1} - rarity: [{string.Join(", ", rarityTable)}], item: {item}");

                var loot = new LootTable()
                {
                    Object = "Console",
                    Drops = new[] { new[] { 1, 1 } },
                    Loot = new[]
                    {
                            new LootDrop()
                            {
                                Item = item,
                                Rarity = rarityTable
                            }
                        }
                };

                var randomOffset = UnityEngine.Random.insideUnitSphere;
                var dropPoint = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 3 + Vector3.up * 1.5f + randomOffset;
                items.AddRange(EpicLoot.RollLootTableAndSpawnObjects(loot, loot.Object, dropPoint));
            }
        }

        public static void CheckStackQuality(Console __instance)
        {
            __instance.AddString("CheckStackQuality");
            if (ObjectDB.instance == null)
            {
                __instance.AddString(" - ObjectDB is null");
                return;
            }

            var count = 0;
            foreach (var itemObject in ObjectDB.instance.m_items)
            {
                var itemDrop = itemObject.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    continue;
                }

                var itemData = itemDrop.m_itemData;

                if (itemData.m_shared.m_maxStackSize > 1 && itemData.m_shared.m_maxQuality > 1)
                {
                    count++;
                    __instance.AddString($" - {itemDrop.name}");
                }
            }

            if (count == 0)
            {
                __instance.AddString(" (none)");
            }
        }
    }
}
