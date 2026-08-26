using EpicLoot.Config;
using EpicLoot.Crafting;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.ShardStones {
    // The yes/no panel shown before a gift is spent. Reuses the SocketMessage prefab -- it is the same
    // layout the break confirmation uses, and only the behaviour differs.
    public sealed class ShardSlotChiselPrompt : ConfirmPrompt {
        public static ShardSlotChiselPrompt Create(Transform parent, string title, string body) {
            return Create<ShardSlotChiselPrompt>(EpicAssets.SocketMessagePrefab, parent, title, body);
        }
    }

    // Brokkr's Gift: a consumable dragged onto a magic item to give it shard slots.
    //
    // Slots are capacity (MagicItem.SocketCount), which until now was only ever rolled at loot time or
    // copied between items while crafting. The gift is the one player-driven way to raise it, and it is
    // bounded by what that rarity could have rolled anyway -- so it cannot take an item past a shape the
    // loot tables already allow.
    public static class ShardSlotChisel {
        public const string LegendaryPrefab = "ShardSlotChiselLegendary";
        public const string MythicPrefab = "ShardSlotChiselMythic";

        // Only two tiers exist. Anything else (a config-patched prefab, say) falls back to the Legendary
        // numbers rather than granting nothing at all.
        private static int SlotsAddedBy(ItemRarity chiselRarity) {
            return chiselRarity == ItemRarity.Mythic
                ? ELConfig.MythicGiftSlotsAdded.Value
                : ELConfig.LegendaryGiftSlotsAdded.Value;
        }

        private static float SuccessChanceFor(ItemRarity chiselRarity) {
            return chiselRarity == ItemRarity.Mythic
                ? ELConfig.MythicGiftSuccessChance.Value
                : ELConfig.LegendaryGiftSuccessChance.Value;
        }

        /// <summary>
        /// Whether the chisel may be spent on the target, and how many slots it would actually add. A
        /// partial fit counts as success: slotsGranted is clamped to the room left under the target
        /// rarity cap, so the only failure here is having no room at all.
        /// </summary>
        public static bool CanApply(ItemDrop.ItemData chisel, ItemDrop.ItemData target,
            out int slotsGranted, out string reason) {
            slotsGranted = 0;
            reason = null;

            if (chisel == null || target == null) {
                reason = "$mod_epicloot_slotchisel_notmagic";
                return false;
            }

            // CanBeMagicItem is what keeps shardstones, unidentified items and crafting materials out:
            // all of them carry a MagicItem, so IsMagic alone is true for them too.
            if (!EpicLoot.CanBeMagicItem(target) || !target.IsMagic(out var magicItem)) {
                reason = "$mod_epicloot_slotchisel_notmagic";
                return false;
            }

            var chiselRarity = chisel.GetShardSlotChiselRarity();
            if (chiselRarity < magicItem.Rarity) {
                reason = "$mod_epicloot_slotchisel_raritytoolow";
                return false;
            }

            // Keyed off capacity, matching MagicItem.HasSockets(): an item whose slots are all empty is
            // still an item that already has slots.
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

        // Asks the player to confirm. Nothing is consumed or changed until they accept.
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
                ShowMessage("$mod_epicloot_socket_unavailable");
            }
        }

        // Spends the gift. Everything is re-checked first: the prompt is asynchronous, so the item could
        // have moved, been consumed or gained slots between showing it and accepting.
        private static void Apply(InventoryGui invGui, Inventory sourceInv,
            ItemDrop.ItemData chisel, ItemDrop.ItemData target) {
            var player = Player.m_localPlayer;
            if (player == null || invGui == null || sourceInv == null || chisel == null || target == null) {
                return;
            }

            if (!sourceInv.ContainsItem(chisel) || !player.GetInventory().ContainsItem(target)) {
                ShowMessage("$mod_epicloot_socket_unavailable");
                return;
            }

            if (!CanApply(chisel, target, out var slotsGranted, out var reason)) {
                ShowMessage(reason);
                return;
            }

            var chiselRarity = chisel.GetShardSlotChiselRarity();

            // The socket overlay sizes its row from SocketCount when it opens, so raising the count while
            // it is up would leave the grid a slot short. Closing it first reconciles the sockets safely.
            if (SocketsUI.OpenEquipment == target) {
                invGui.CloseContainer();
            }

            // Consumed whichever way the roll goes.
            sourceInv.RemoveItem(chisel, 1);
            invGui.m_moveItemEffects.Create(invGui.transform.position, Quaternion.identity);

            if (Random.Range(0f, 100f) >= SuccessChanceFor(chiselRarity)) {
                ShowMessage("$mod_epicloot_slotchisel_failed");
                return;
            }

            // Re-read after CloseContainer: reconciling the sockets replaced the stored payload.
            var magicItem = target.GetMagicItem();
            magicItem.SocketCount += slotsGranted;
            API.WithChangeReason(API.ChangeReason.AddSocket, () => target.SaveMagicItem(magicItem));

            player.Message(MessageHud.MessageType.Center, string.Format(
                Localization.instance.Localize("$mod_epicloot_slotchisel_success"),
                Localization.instance.Localize(target.m_shared.m_name), slotsGranted));
        }

        private static void ShowMessage(string reason) {
            if (Player.m_localPlayer != null && !string.IsNullOrEmpty(reason)) {
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize(reason));
            }
        }

        // Claims "drop a gift onto a magic item". Patched here rather than on InventoryGrid.DropItem
        // because OnSelectedItem unequips both the dragged item and the drop target before it ever calls
        // DropItem -- intercepting downstream would unequip the player's weapon just to put it back. The
        // prefix also lets us clear the cursor before the confirmation opens.
        //
        // SocketsUI has its own prefix on this method; the two cannot collide, because that one returns
        // true immediately whenever a drag is in flight, which is exactly the case claimed here.
        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        public static class InventoryGui_OnSelectedItem_Patch {
            [UsedImplicitly]
            private static bool Prefix(InventoryGui __instance, InventoryGrid grid, Vector2i pos) {
                // While a confirmation is up it owns the whole window: the prefab input blocker only
                // stops pointer events, so a gamepad A press would still reach the grid underneath.
                if (InventoryPromptHost.IsOpen) {
                    return false;
                }

                // No drag in flight means this is a pickup, not a drop.
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

                // Both ends must be the local player inventory. Dropping a gift into or out of a
                // container is an ordinary transfer and stays vanilla business.
                var playerInv = player.GetInventory();
                if (grid.GetInventory() != playerInv || __instance.m_dragInventory != playerInv) {
                    return true;
                }

                var target = playerInv.GetItemAt(pos.x, pos.y);
                if (target == null || target == chisel) {
                    return true;
                }

                // Dropped on anything that is not eligible equipment -- an empty slot, a plain sword, a
                // shardstone -- and the gift stays an ordinary movable item.
                if (!EpicLoot.CanBeMagicItem(target)) {
                    return true;
                }

                // We own this gesture from here. The gift comes off the cursor either way: a swap would
                // be a confusing second meaning for the same drop, so a target that cannot be chiselled
                // reports why instead.
                __instance.SetupDragItem(null, null, 1);

                if (!CanApply(chisel, target, out var slotsGranted, out var reason)) {
                    ShowMessage(reason);
                    return false;
                }

                OpenPrompt(__instance, playerInv, chisel, target, slotsGranted);
                return false;
            }
        }
    }
}
