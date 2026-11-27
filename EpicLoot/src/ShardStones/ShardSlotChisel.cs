using EpicLoot.Config;
using EpicLoot.Crafting;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.ShardStones {
    public sealed class ShardSlotChiselPrompt : ConfirmPrompt {
        public static ShardSlotChiselPrompt Create(Transform parent, string title, string body) {
            return Create<ShardSlotChiselPrompt>(EpicAssets.SocketMessagePrefab, parent, title, body);
        }
    }

    // Brokkr's Gift. Bounded by what the item's rarity could have rolled, so it cannot take an item past
    // a shape the loot tables already allow.
    public static class ShardSlotChisel {
        public const string LegendaryPrefab = "ShardSlotChiselLegendary";
        public const string MythicPrefab = "ShardSlotChiselMythic";
        public const string AncientPrefab = "ShardSlotChiselAncient";

        // Anything but the three tiers (a config-patched prefab, say) falls back to the Legendary numbers.
        private static int SlotsAddedBy(ItemRarity chiselRarity) {
            switch (chiselRarity) {
                case ItemRarity.Ancient: return ELConfig.AncientGiftSlotsAdded.Value;
                case ItemRarity.Mythic: return ELConfig.MythicGiftSlotsAdded.Value;
                default: return ELConfig.LegendaryGiftSlotsAdded.Value;
            }
        }

        private static float SuccessChanceFor(ItemRarity chiselRarity) {
            switch (chiselRarity) {
                case ItemRarity.Ancient: return ELConfig.AncientGiftSuccessChance.Value;
                case ItemRarity.Mythic: return ELConfig.MythicGiftSuccessChance.Value;
                default: return ELConfig.LegendaryGiftSuccessChance.Value;
            }
        }

        // A partial fit counts as success: slotsGranted is clamped to the room left under the target's
        // rarity cap, so the only failure on that count is having no room at all.
        public static bool CanApply(ItemDrop.ItemData chisel, ItemDrop.ItemData target,
            out int slotsGranted, out string reason) {
            slotsGranted = 0;
            reason = null;

            if (chisel == null || target == null) {
                reason = "$mod_epicloot_slotchisel_notmagic";
                return false;
            }

            if (!GatedItemType.GatedItemTypeHelper.IsSocketable(target)) {
                reason = "$mod_epicloot_socket_unavailable";
                return false;
            }

            // Shardstones, unidentified items and crafting materials all carry a MagicItem, so IsMagic
            // alone is true for them too.
            if (!EpicLoot.CanBeMagicItem(target) || !target.IsMagic(out var magicItem)) {
                reason = "$mod_epicloot_slotchisel_notmagic";
                return false;
            }

            var chiselRarity = chisel.GetShardSlotChiselRarity();
            if (chiselRarity < magicItem.Rarity) {
                reason = "$mod_epicloot_slotchisel_raritytoolow";
                return false;
            }

            // Capacity, not occupancy: an item whose slots are all empty already has slots.
            if (!ELConfig.AllowGiftOnItemsWithSlots.Value && magicItem.SocketCount > 0) {
                reason = "$mod_epicloot_slotchisel_hasslots";
                return false;
            }

            var cap = LootRoller.GetMaxSocketCountForRarity(magicItem.Rarity);
            slotsGranted = Mathf.Min(SlotsAddedBy(chiselRarity), cap - magicItem.SocketCount);
            if (slotsGranted <= 0) {
                slotsGranted = 0;
                reason = "$mod_epicloot_slotchisel_atmax";
                return false;
            }

            return true;
        }

        private static void OpenPrompt(InventoryGui invGui, Inventory sourceInv,
            ItemDrop.ItemData chisel, ItemDrop.ItemData target, int slotsGranted) {
            var magicItem = target.GetMagicItem();
            var body = string.Format(
                Localization.instance.Localize("$mod_epicloot_slotchisel_confirm_body"),
                Localization.instance.Localize(target.m_shared.m_name),
                magicItem.SocketCount,
                magicItem.SocketCount + slotsGranted,
                SuccessChanceFor(chisel.GetShardSlotChiselRarity()).ToString("0.##"));

            var prompt = ShardSlotChiselPrompt.Create(invGui.transform,
                Localization.instance.Localize("$mod_epicloot_slotchisel_confirm_title"), body);

            // No prefab to confirm with -- refuse rather than spending the gift unconfirmed.
            if (!InventoryPromptHost.Open(prompt, () => Apply(invGui, sourceInv, chisel, target))) {
                ShardUI.ShowSocketMessage("$mod_epicloot_socket_unavailable");
            }
        }

        // Everything is re-checked: the item could have moved, been consumed or gained slots while the
        // prompt was up.
        private static void Apply(InventoryGui invGui, Inventory sourceInv,
            ItemDrop.ItemData chisel, ItemDrop.ItemData target) {
            var player = Player.m_localPlayer;
            if (player == null || invGui == null || sourceInv == null || chisel == null || target == null) {
                return;
            }

            if (!sourceInv.ContainsItem(chisel) || !player.GetInventory().ContainsItem(target)) {
                ShardUI.ShowSocketMessage("$mod_epicloot_socket_unavailable");
                return;
            }

            if (!CanApply(chisel, target, out var slotsGranted, out var reason)) {
                ShardUI.ShowSocketMessage(reason);
                return;
            }

            var chiselRarity = chisel.GetShardSlotChiselRarity();

            // The slot panel sizes its row from SocketCount when it opens, so raising the count while it
            // is up would leave the grid a slot short. Closing it first reconciles the sockets safely.
            if (ShardUI.IsOpenFor(target)) {
                ShardUI.Close();
            }

            // Consumed whichever way the roll goes.
            sourceInv.RemoveItem(chisel, 1);
            invGui.m_moveItemEffects.Create(invGui.transform.position, Quaternion.identity);

            if (Random.Range(0f, 100f) >= SuccessChanceFor(chiselRarity)) {
                ShardUI.ShowSocketMessage("$mod_epicloot_slotchisel_failed");
                return;
            }

            // Re-read after ShardUI.Close: reconciling the sockets replaced the stored payload.
            var magicItem = target.GetMagicItem();
            magicItem.SocketCount += slotsGranted;
            API.WithChangeReason(API.ChangeReason.AddSocket, () => target.SaveMagicItem(magicItem));

            player.Message(MessageHud.MessageType.Center, string.Format(
                Localization.instance.Localize("$mod_epicloot_slotchisel_success"),
                Localization.instance.Localize(target.m_shared.m_name), slotsGranted));
        }

        // Not InventoryGrid.DropItem: OnSelectedItem unequips both the dragged item and the drop target
        // before it calls DropItem. ShardUI's prefix on this method cannot collide with this one, because
        // it returns true whenever a drag is in flight, which is exactly the case claimed here.
        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        public static class InventoryGui_OnSelectedItem_Patch {
            [UsedImplicitly]
            private static bool Prefix(InventoryGui __instance, InventoryGrid grid, Vector2i pos) {
                // The prefab's input blocker only stops pointer events, so a gamepad press would still
                // reach the grid underneath.
                if (InventoryPromptHost.IsOpen) {
                    return false;
                }

                if (__instance.m_dragGo == null || grid == null) {
                    return true;
                }

                var chisel = __instance.m_dragItem;
                if (chisel == null || !chisel.IsShardSlotChisel()) {
                    return true;
                }

                var player = Player.m_localPlayer;
                if (player == null) {
                    return true;
                }

                var playerInv = player.GetInventory();
                if (grid.GetInventory() != playerInv || __instance.m_dragInventory != playerInv) {
                    return true;
                }

                var target = playerInv.GetItemAt(pos.x, pos.y);
                if (target == null || target == chisel) {
                    return true;
                }

                if (!EpicLoot.CanBeMagicItem(target)) {
                    return true;
                }

                // The gift comes off the cursor either way: a swap would be a confusing second meaning
                // for the same drop, so a target that cannot take it reports why instead.
                __instance.SetupDragItem(null, null, 1);

                if (!CanApply(chisel, target, out var slotsGranted, out var reason)) {
                    ShardUI.ShowSocketMessage(reason);
                    return false;
                }

                OpenPrompt(__instance, playerInv, chisel, target, slotsGranted);
                return false;
            }
        }
    }
}
