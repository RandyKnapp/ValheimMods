using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.ShardStones {
    // Enforces the "one exclusive-category shard across worn gear" rule at equip time. Socketing a
    // unique shard into an unequipped item is always allowed (ShardSocketManager only gates the
    // cross-equipped case for items that are already worn); this guard closes the loop by refusing to
    // equip an item that would put a second shard of an exclusive category onto the player at once.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    public static class UniqueShardGuard {
        [UsedImplicitly]
        private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result) {
            // Local player only, mirroring the inventory-scoped socket UI. During early load the item
            // is equipped before m_localPlayer is assigned, so we simply let those through.
            if (__instance == null || __instance != Player.m_localPlayer ||
                item == null || !item.IsMagic(out var magicItem)) {
                return true;
            }

            foreach (var socket in magicItem.Sockets) {
                if (socket == null || socket.ShardType == ShardType.None) {
                    continue;
                }

                var category = Shards.GetCategory(socket.ShardType);
                if (!Shards.IsExclusive(category)) {
                    continue;
                }

                // The item being equipped is not yet worn, so this scans the currently-worn set.
                if (ShardSocketManager.IsExclusiveCategoryEquipped(Player.m_localPlayer, category, item)) {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                        Localization.instance.Localize(
                            $"$mod_epicloot_equip_{Shards.ExclusiveCategorySlug(category)}limit"));
                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }
}
