using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.ShardStones {
    // Enforces the "one exclusive-category shard across worn gear" rule at equip time. Socketing a
    // unique shard into an unequipped item is always allowed (ShardSocketManager only gates the
    // cross-equipped case for items that are already worn); this guard closes the loop by refusing to
    // equip an item that would put a second shard of an exclusive category onto the player at once.
    //
    // It measures the loadout the equip will *produce*, not the one it starts from: vanilla unequips
    // the gear this item displaces from inside EquipItem, after every prefix has run, so swapping one
    // boss-shard sword for another has to be allowed even though both are worn when we look.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    public static class UniqueShardGuard {
        // Last, so EquipDisplacement reads the item type every other prefix has settled on.
        // EquipmentAndQuickSlots' MultiUtility rewrites m_shared.m_itemType from its own EquipItem
        // prefix to reroute a utility item into a slot it owns; seeing the original type there would
        // have us report vanilla's utility slot as displaced when nothing is. Harmony still runs every
        // postfix and finalizer when a prefix returns false, so that mod's type restore is unaffected.
        [UsedImplicitly]
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result) {
            // Local player only, mirroring the inventory-scoped socket UI. During early load the item
            // is equipped before m_localPlayer is assigned, so we simply let those through.
            if (__instance == null || __instance != Player.m_localPlayer ||
                item == null || !item.IsMagic(out var magicItem)) {
                return true;
            }

            // Vanilla refuses a re-equip of what is already worn, and it changes no loadout either
            // way. Bailing here also keeps us from reading the item's own slot occupant as displaced.
            if (__instance.IsItemEquiped(item)) {
                return true;
            }

            var displaced = EquipDisplacement.Predict(__instance, item);

            // Scanning per category rather than per socket: IsExclusiveCategoryEquipped walks
            // GetMagicEquipment(), which allocates and runs every registered equipment provider, and
            // there are only ever two exclusive categories to check.
            foreach (var category in Shards.ExclusiveCategories) {
                if (!ShardSocketManager.ItemHasCategory(magicItem, category)) {
                    continue;
                }

                if (ShardSocketManager.IsExclusiveCategoryEquipped(Player.m_localPlayer, category, item, displaced)) {
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
