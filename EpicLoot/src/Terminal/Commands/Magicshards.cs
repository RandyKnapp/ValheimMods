using EpicLoot.ShardStones;
using Jotunn.Managers;
using System;
using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager {
    private static void SpawnMagicShards(Terminal.ConsoleEventArgs args) {
        if (Player.m_localPlayer == null) {
            return;
        }

        // No rarity arg means "roll one per color"; an explicit rarity is honored but snapped to each
        // color's declared set, since only the (color, rarity) pairs in shardstones.json have prefabs.
        ItemRarity? requested = null;
        if (args.Length >= 2) {
            if (!Enum.TryParse(args[1], true, out ItemRarity parsed)) {
                args.Context.AddString($"> Unknown rarity '{args[1]}'. Use Magic/Rare/Epic/Legendary/Mythic.");
                return;
            }
            requested = parsed;
        }

        Transform transform = Player.m_localPlayer.transform;
        var spawned = 0;

        foreach (ShardType color in Enum.GetValues(typeof(ShardType))) {
            if (color == ShardType.None) {
                continue;
            }

            ItemRarity rarity = requested.HasValue
                ? Shards.ClampToRaritySet(color, requested.Value)
                : Shards.RandomRarityFromSet(color);

            string assetName = $"{color}_{rarity}_ShardStone";
            GameObject itemPrefab = PrefabManager.Instance.GetPrefab(assetName);
            ItemDrop itemDrop = itemPrefab != null
                ? UnityEngine.Object.Instantiate(itemPrefab,
                    transform.position + transform.forward * 2f + Vector3.up,
                    Quaternion.identity).GetComponent<ItemDrop>()
                : null;
            if (itemDrop == null) {
                args.Context.AddString($"> Failed to get shard prefab '{assetName}'.");
                continue;
            }

            spawned++;
        }

        args.Context.AddString($"> Spawned {spawned} shard(s).");
    }
}
