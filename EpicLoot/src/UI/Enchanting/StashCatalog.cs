using System.Collections.Generic;
using EpicLoot;
using EpicLoot.Crafting;
using EpicLoot.ShardStones;
using UnityEngine;

namespace EpicLoot_UnityLib
{
    public enum StashKind
    {
        Material,
        Runestone,
        Shardstone,
        Chisel,
        Token
    }

    public class StashItemKind
    {
        public string PrefabName;
        public string SharedName;
        public GameObject Prefab;
        public StashKind Kind;
        public string Family;
        public ItemRarity Rarity;
        public ShardType Color;

        // Built as loot is, so the item carries this world's level: a bare prefab clone keeps the prefab's,
        // and in a New Game+ world that never stacks with the same material already carried.
        public ItemDrop.ItemData CreateItem(int stack)
        {
            ItemDrop.ItemData item = LootRoller.CreateLootItemData(Prefab);
            item.m_stack = stack;
            return item;
        }
    }

    /// <summary>
    /// Every item kind the enchanting table's storage accepts: the fungible materials, keyed by prefab. Built
    /// from ObjectDB on first use and again whenever its item list changes.
    /// </summary>
    public static class StashCatalog
    {
        public static readonly string[] MaterialFamilies = { "Dust", "Essence", "Reagent", "Shard", "Runestone" };
        private const string OtherFamily = "Other";

        private static readonly Dictionary<string, StashItemKind> ByPrefab = new Dictionary<string, StashItemKind>();
        private static readonly Dictionary<string, StashItemKind> ByShared = new Dictionary<string, StashItemKind>();
        private static readonly List<StashItemKind> MaterialGridKinds = new List<StashItemKind>();
        private static readonly List<StashItemKind> ShardstoneGridKinds = new List<StashItemKind>();
        private static ObjectDB _builtFor;
        private static int _builtItemCount;

        public static IReadOnlyList<StashItemKind> MaterialGrid
        {
            get
            {
                Ensure();
                return MaterialGridKinds;
            }
        }

        public static IReadOnlyList<StashItemKind> ShardstoneGrid
        {
            get
            {
                Ensure();
                return ShardstoneGridKinds;
            }
        }

        public static bool TryGetByPrefab(string prefabName, out StashItemKind kind)
        {
            Ensure();
            kind = null;
            return prefabName != null && ByPrefab.TryGetValue(prefabName, out kind);
        }

        public static bool TryGetBySharedName(string sharedName, out StashItemKind kind)
        {
            Ensure();
            kind = null;
            return sharedName != null && ByShared.TryGetValue(sharedName, out kind);
        }

        public static string GetPrefabName(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                return null;
            }

            if (item.m_dropPrefab != null)
            {
                return item.m_dropPrefab.name;
            }

            return TryGetBySharedName(item.m_shared.m_name, out StashItemKind kind) ? kind.PrefabName : null;
        }

        public static bool IsStashable(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null || item.m_quality > 1 || !TryGetByPrefab(GetPrefabName(item), out StashItemKind kind))
            {
                return false;
            }

            if (kind.Kind != StashKind.Runestone)
            {
                return true;
            }

            return !item.IsMagic(out MagicItem magicItem) ||
                ((magicItem.Effects == null || magicItem.Effects.Count == 0) && string.IsNullOrEmpty(magicItem.SetID));
        }

        private static void Ensure()
        {
            ObjectDB db = ObjectDB.instance;
            if (db == null || (_builtFor == db && _builtItemCount == db.m_items.Count))
            {
                return;
            }

            _builtFor = db;
            _builtItemCount = db.m_items.Count;
            ByPrefab.Clear();
            ByShared.Clear();
            MaterialGridKinds.Clear();
            ShardstoneGridKinds.Clear();

            List<StashItemKind> kinds = new List<StashItemKind>();
            foreach (GameObject prefab in db.m_items)
            {
                if (prefab == null)
                {
                    continue;
                }

                ItemDrop drop = prefab.GetComponent<ItemDrop>();
                if (drop == null || drop.m_itemData?.m_shared == null)
                {
                    continue;
                }

                StashItemKind kind = Classify(prefab, drop.m_itemData);
                if (kind == null || ByPrefab.ContainsKey(kind.PrefabName))
                {
                    continue;
                }

                ByPrefab[kind.PrefabName] = kind;
                if (!ByShared.ContainsKey(kind.SharedName))
                {
                    ByShared[kind.SharedName] = kind;
                }

                kinds.Add(kind);
            }

            BuildMaterialGrid(kinds);
            BuildShardstoneGrid(kinds);
        }

        private static StashItemKind Classify(GameObject prefab, ItemDrop.ItemData data)
        {
            string prefabName = prefab.name;
            StashItemKind kind = new StashItemKind
            {
                PrefabName = prefabName,
                SharedName = data.m_shared.m_name,
                Prefab = prefab
            };

            if (Shards.IsShard(data))
            {
                ShardType color = Shards.GetShardColor(data);
                if (color == ShardType.None)
                {
                    return null;
                }

                kind.Kind = StashKind.Shardstone;
                kind.Color = color;
                kind.Rarity = Shards.GetShardRarity(data);
                kind.Family = "Shardstone";
                return kind;
            }

            if (data.IsShardSlotChisel())
            {
                kind.Kind = StashKind.Chisel;
                kind.Rarity = data.IsMagic(out MagicItem chiselMagic) ? chiselMagic.Rarity : ItemRarity.Magic;
                kind.Family = "Chisel";
                return kind;
            }

            if (data.IsRunestone())
            {
                if (prefabName.StartsWith("Etched"))
                {
                    return null;
                }

                kind.Kind = StashKind.Runestone;
                kind.Rarity = data.GetRunestoneRarity();
                kind.Family = "Runestone";
                return kind;
            }

            if (data.IsMagicCraftingMaterial())
            {
                kind.Kind = StashKind.Material;
                kind.Rarity = data.GetCraftingMaterialRarity();
                kind.Family = OtherFamily;
                foreach (string family in MaterialFamilies)
                {
                    if (prefabName.StartsWith(family))
                    {
                        kind.Family = family;
                        break;
                    }
                }

                return kind;
            }

            if (System.Array.IndexOf(EpicLoot.EpicLoot.ItemNames, prefabName) >= 0)
            {
                kind.Kind = StashKind.Token;
                kind.Family = "Token";
                return kind;
            }

            return null;
        }

        // Row per family, column per rarity, then one row of chisels and tokens.
        private static void BuildMaterialGrid(List<StashItemKind> kinds)
        {
            foreach (string family in MaterialFamilies)
            {
                foreach (ItemRarity rarity in Rarities.All)
                {
                    StashItemKind kind = kinds.Find(x =>
                        (x.Kind == StashKind.Material || x.Kind == StashKind.Runestone) && x.Family == family && x.Rarity == rarity);
                    if (kind != null)
                    {
                        MaterialGridKinds.Add(kind);
                    }
                }
            }

            List<StashItemKind> rest = kinds.FindAll(x => x.Kind == StashKind.Chisel);
            rest.Sort((a, b) => a.Rarity.CompareTo(b.Rarity));
            foreach (string token in EpicLoot.EpicLoot.ItemNames)
            {
                StashItemKind kind = kinds.Find(x => x.Kind == StashKind.Token && x.PrefabName == token);
                if (kind != null)
                {
                    rest.Add(kind);
                }
            }

            rest.AddRange(kinds.FindAll(x => x.Kind == StashKind.Material && x.Family == OtherFamily));
            MaterialGridKinds.AddRange(rest);
        }

        // Row per color in config order, column per rarity.
        private static void BuildShardstoneGrid(List<StashItemKind> kinds)
        {
            List<ShardType> colors = new List<ShardType>(Shards.ShardDefinitions.ShardEffects.Keys);
            foreach (StashItemKind kind in kinds)
            {
                if (kind.Kind == StashKind.Shardstone && !colors.Contains(kind.Color))
                {
                    colors.Add(kind.Color);
                }
            }

            foreach (ShardType color in colors)
            {
                List<StashItemKind> row = kinds.FindAll(x => x.Kind == StashKind.Shardstone && x.Color == color);
                row.Sort((a, b) => a.Rarity.CompareTo(b.Rarity));
                ShardstoneGridKinds.AddRange(row);
            }
        }
    }
}
