using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // Death handling for the single enlarged tombstone.
    //
    // Keep-on-death: kept slot items are pulled out of m_inventory in a Character.CheckDeath
    // prefix at Priority.First — before vanilla or any other mod touches the inventory — so
    // neither the tombstone transfer nor the DeathDeleteItems/DeathDeleteUnequipped world
    // modifiers can reach them. Finalizers on CheckDeath and Player.OnDeath put them back.
    // Kept equipment is re-marked m_equipped afterwards, so the death-save stores it worn and
    // the respawn load re-equips it natively via EquipInventoryItems.
    //
    // Everything else runs pure vanilla: MoveInventoryToGrave copies the full-height inventory
    // with grid positions intact, so dropped slot items return to their exact cells on take-all.
    public static class DeathPatches {
        private class KeptItem {
            public ItemDrop.ItemData item;
            public bool wasEquipped;
        }

        private static readonly List<KeptItem> itemsToKeep = new List<KeptItem>();
        private static readonly HashSet<Slot> takenSlots = new HashSet<Slot>();

        public static readonly int AfterdeathGhost = "Afterdeath Ghost".GetStableHashCode();

        // Legacy 2.x tombstone markers, honored once for graves created before the rewrite
        private const string legacyEquipmentKey = "eaqs-e";
        private const string legacyQuickSlotKey = "eaqs-qs";

        public static void OnDeathPrefix(Player player) {
            if (itemsToKeep.Count != 0)
                return;

            SaveLastEquippedSlotsToItems();
            SaveLastEquippedWeaponShieldToItems(player);

            foreach (Slot slot in slots) {
                if (!IsSlotToKeep(slot))
                    continue;

                ItemDrop.ItemData item = slot.Item;
                itemsToKeep.Add(new KeptItem { item = item, wasEquipped = item.m_equipped || player.IsItemEquiped(item) });
                player.GetInventory().m_inventory.Remove(item);
            }

            ClearCachedItems();
        }

        private static bool IsSlotToKeep(Slot slot) {
            if (slot.Item == null)
                return false;

            // API custom slots hold equipment-like accessories; they follow the equipment setting
            return (slot.IsEquipmentSlot || slot.IsCustomSlot) && ValConfig.DontDropEquipmentOnDeath.Value
                   || slot.IsQuickSlot && ValConfig.DontDropQuickslotsOnDeath.Value;
        }

        public static void OnDeathPostfix(Player player) {
            if (itemsToKeep.Count == 0)
                return;

            foreach (KeptItem kept in itemsToKeep) {
                // Vanilla unequipped the gear through the humanoid's equip references while the
                // item was outside the inventory; re-mark it so the death-save stores it worn.
                kept.item.m_equipped = kept.wasEquipped;
                player.GetInventory().m_inventory.Add(kept.item);
            }

            itemsToKeep.Clear();
            ClearCachedItems();
        }

        private static void SaveLastEquippedWeaponShieldToItems(Player player) {
            long playerID = Game.instance.GetPlayerProfile().GetPlayerID();

            if (player.LeftItem != null)
                player.LeftItem.m_customData[customKeyWeaponShield] = playerID.ToString();

            if (player.RightItem != null)
                player.RightItem.m_customData[customKeyWeaponShield] = playerID.ToString();
        }

        [HarmonyPatch(typeof(Character), nameof(Character.CheckDeath))]
        private static class Character_CheckDeath_OnDeathWrapping {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Character __instance) {
                if (!IsValidPlayer(__instance))
                    return;

                if (!__instance.IsDead() && __instance.GetHealth() <= 0f)
                    OnDeathPrefix((Player)__instance);
            }

            [HarmonyFinalizer]
            [HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Character __instance, Exception __exception) {
                if (IsValidPlayer(__instance))
                    OnDeathPostfix((Player)__instance);

                return __exception;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
        private static class Player_OnDeath_RestoreKeptItems {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Player __instance) {
                if (IsValidPlayer(__instance))
                    OnDeathPostfix(__instance);
            }

            [HarmonyFinalizer]
            [HarmonyPriority(Priority.Last)]
            private static Exception Finalizer(Player __instance, Exception __exception) {
                if (IsValidPlayer(__instance))
                    OnDeathPostfix(__instance);

                return __exception;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Tombstone size: the grave container must fit the full-height inventory.

        private static int TargetGraveHeight(int width) => (InventorySizeFull - 1) / Mathf.Max(1, width) + 1;

        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        private static class Container_Awake_TombstoneHeightAdjustment {
            private static void Prefix(Container __instance) {
                if (__instance.m_name != "Grave" && !__instance.GetComponentInParent<TombStone>())
                    return;

                int targetHeight = TargetGraveHeight(__instance.m_width);
                if (targetHeight > __instance.m_height)
                    __instance.m_height = targetHeight;
            }

            private static void Postfix(Container __instance) {
                // Persist the enlarged height on the grave's ZDO so it reloads at the right size
                // (graves inside dungeons re-create their container from ZDO fields).
                if (__instance.m_nview?.IsValid() == true && __instance.m_nview.IsOwner() && __instance.GetComponent<TombStone>() != null && __instance.m_height > VanillaInventoryHeight) {
                    string typeName = __instance.GetType().Name;
                    __instance.m_nview.GetZDO().Set(ZNetView.CustomFieldsStr, true);
                    __instance.m_nview.GetZDO().Set((ZNetView.CustomFieldsStr + typeName).GetStableHashCode(), true);
                    __instance.m_nview.GetZDO().Set(typeName + ".m_height", __instance.m_height);
                }
            }
        }

        // Runs right after CreateTombStone's MoveInventoryToGrave (Setup is the next call), with the
        // grave's real inventory dimensions in place. Freeze them onto the Container fields and ZDO:
        // reload builds the container inventory from those fields, so without this a lowered
        // 'Extra Inventory Rows' (or a width-changing mod being removed) between death and pickup
        // would rebuild the grave smaller and Inventory.Load would silently drop everything beyond
        // the new bounds.
        [HarmonyPatch(typeof(TombStone), nameof(TombStone.Setup))]
        private static class TombStone_Setup_PersistGraveDimensions {
            private static void Postfix(TombStone __instance) {
                Container container = __instance.m_container != null
                    ? __instance.m_container
                    : __instance.GetComponent<Container>();
                if (container == null || container.m_inventory == null)
                    return;

                container.m_width = Mathf.Max(container.m_width, container.m_inventory.m_width);
                container.m_height = Mathf.Max(container.m_height, container.m_inventory.m_height);

                if (container.m_nview?.IsValid() == true && container.m_nview.IsOwner()) {
                    string typeName = container.GetType().Name;
                    ZDO zdo = container.m_nview.GetZDO();
                    zdo.Set(ZNetView.CustomFieldsStr, true);
                    zdo.Set((ZNetView.CustomFieldsStr + typeName).GetStableHashCode(), true);
                    zdo.Set(typeName + ".m_width", container.m_width);
                    zdo.Set(typeName + ".m_height", container.m_height);
                }
            }
        }

        [HarmonyPatch(typeof(TombStone), nameof(TombStone.Interact))]
        private static class TombStone_Interact_AdjustHeight {
            [HarmonyPriority(Priority.First)]
            private static void Prefix(TombStone __instance, bool hold) {
                if (hold)
                    return;

                int targetHeight = TargetGraveHeight(__instance.m_container.m_width);
                if (targetHeight > __instance.m_container.m_height) {
                    __instance.m_container.m_height = targetHeight;
                    __instance.m_container.m_inventory.m_height = targetHeight;

                    __instance.m_container.m_lastRevision = 0;
                    __instance.m_container.m_lastDataString = "";
                    __instance.m_container.Load();
                }
            }
        }

        // ---------------------------------------------------------------------------------------
        // "Easy fit" check: credit the carry weight of belts still in the grave (they will be
        // auto-equipped on pickup) and don't count items that will land in their own slot cells.

        [HarmonyPatch(typeof(TombStone), nameof(TombStone.EasyFitInInventory))]
        private static class TombStone_EasyFitInInventory_SlotAwareCounting {
            private static float GetDynamicWeightChange(StatusEffect se) {
                float limit = 0f;
                se.ModifyMaxCarryWeight(0f, ref limit);
                return limit;
            }

            private static void Prefix(TombStone __instance, Player player, ref float __state) {
                if (!IsValidPlayer(player))
                    return;

                __state = (__instance.m_lootStatusEffect as SE_Stats)?.m_addMaxCarryWeight ?? 0f;

                if (ValConfig.AutoEquipCarryWeightItems.Value || ValConfig.InstantlyReequipArmorOnPickup.Value) {
                    __state += __instance.m_container.GetInventory().GetAllItems()
                        .Where(item => item != null && item.m_shared.m_equipStatusEffect != null && GetSlotInGrid(item.m_gridPos) is Slot slot && slot.IsEquipmentSlot)
                        .Sum(item => GetDynamicWeightChange(item.m_shared.m_equipStatusEffect));
                }

                Player.m_localPlayer.m_maxCarryWeight += __state;
            }

            private static void Postfix(TombStone __instance, Player player, float __state, ref bool __result) {
                if (!IsValidPlayer(player))
                    return;

                Player.m_localPlayer.m_maxCarryWeight -= __state;
                if (__result)
                    return;

                if (__instance.m_container.GetInventory().NrOfItems() > InventorySizeActive)
                    return;

                int nrOfItems = 0;
                takenSlots.Clear();
                foreach (ItemDrop.ItemData item in __instance.m_container.GetInventory().GetAllItemsInGridOrder()) {
                    Slot slot = GetSlotInGrid(item.m_gridPos);
                    if (slot == null || takenSlots.Contains(slot)) {
                        nrOfItems++;
                        continue;
                    }

                    takenSlots.Add(slot);

                    // A grave item sitting at a free dedicated cell of its own type transfers
                    // straight into that cell and doesn't consume a regular inventory slot.
                    if (slot.IsEquipmentSlot && slot.IsFree && WouldFitEquipmentSlot(slot, item))
                        continue;

                    nrOfItems++;
                }

                __result = nrOfItems <= PlayerInventory.GetEmptySlots()
                           && player.GetInventory().GetTotalWeight() + __instance.m_container.GetInventory().GetTotalWeight() < player.GetMaxCarryWeight() + __state;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Pickup: return items to remembered slots and auto-equip per config.

        // Runs synchronously in the take-all frame — before the validation sweep prunes the
        // slot-memory tags and before it could evict unequipped armor from its cell. Two states:
        // with the auto-equip options on, the matching items are equipped instantly (no animation
        // — this is your own gear coming back); with them off, armor that landed in its cell is
        // parked there unequipped for the player to decide.
        [HarmonyPatch(typeof(TombStone), nameof(TombStone.OnTakeAllSuccess))]
        private static class TombStone_OnTakeAllSuccess_AutoEquip {
            private static void Postfix() {
                AutoEquipAndParkReturnedItems();
            }
        }

        // OnTakeAllSuccess only fires on the easy-fit auto-loot path (TombStone.Interact ->
        // Container.TakeAll -> RPC_TakeAllRespons). A grave that does not easy-fit is opened as a
        // container, and its Take All button goes InventoryGui.OnTakeAll -> Inventory.MoveAll --
        // which never raises m_onTakeAllSuccess. Cover that path too, or auto-equip/parking is
        // silently dead exactly when the player died carrying the most.
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnTakeAll))]
        private static class InventoryGui_OnTakeAll_AutoEquip {
            private static void Postfix(InventoryGui __instance) {
                if (__instance.m_currentContainer != null
                    && __instance.m_currentContainer.GetComponent<TombStone>() != null)
                    AutoEquipAndParkReturnedItems();
            }
        }

        private static void AutoEquipAndParkReturnedItems() {
            Player player = CurrentPlayer;
            if (player == null || PlayerInventory == null || player.IsDead())
                return;

            string playerID = Game.instance.GetPlayerProfile().GetPlayerID().ToString();

            foreach (ItemDrop.ItemData item in PlayerInventory.GetAllItems().ToList()) {
                if (item == null)
                    continue;

                // An equipped flag without the matching equip reference is stale (it rode
                // along with the item through the grave). Clear it so the item is either
                // auto-equipped below or parked/evicted by the sweep, instead of masquerading
                // as worn.
                if (item.m_equipped && !player.IsItemEquiped(item))
                    item.m_equipped = false;

                HandleLegacyMarkers(player, item);

                if (IsItemToEquip(player, item, playerID)) {
                    PruneWeaponShieldKey(item);
                    if (!player.IsItemEquiped(item))
                        player.EquipItem(item, triggerEquipEffects: false);
                    continue;
                }

                // Not auto-equipped: armor that came back into its own equipment cell stays
                // there unequipped until the player equips it or moves it.
                if (!player.IsItemEquiped(item) && GetItemSlot(item) is Slot slot && slot.IsEquipmentSlot && WouldFitEquipmentSlot(slot, item))
                    slot.Park(item);
            }
        }

        private static bool IsItemToEquip(Player player, ItemDrop.ItemData item, string playerID) {
            // Belts and other carry-weight gear: equipping them is what makes the loot carryable
            if (ValConfig.AutoEquipCarryWeightItems.Value && item.m_shared.m_equipStatusEffect is SE_Stats se && se.m_addMaxCarryWeight > 0)
                return true;

            // Weapon/shield the player died holding
            if (ValConfig.AutoEquipWeaponShield.Value
                && item.m_customData.TryGetValue(customKeyWeaponShield, out string weaponOwner) && weaponOwner == playerID)
                return true;

            // Armor that was in an equipment cell when the player died
            if (ValConfig.InstantlyReequipArmorOnPickup.Value
                && item.m_customData.TryGetValue(customKeyPlayerID, out string owner) && owner == playerID
                && item.m_customData.TryGetValue(customKeySlotID, out string slotID)
                && FindSlot(slotID) is Slot slot && slot.IsEquipmentSlot)
                return true;

            return false;
        }

        private static void PruneWeaponShieldKey(ItemDrop.ItemData item) {
            item.m_customData.Remove(customKeyWeaponShield);
        }

        private static void HandleLegacyMarkers(Player player, ItemDrop.ItemData item) {
            if (item.m_customData.Remove(legacyEquipmentKey) && ValConfig.InstantlyReequipArmorOnPickup.Value && !player.IsItemEquiped(item))
                player.EquipItem(item, triggerEquipEffects: false);

            if (item.m_customData.TryGetValue(legacyQuickSlotKey, out string posText)) {
                item.m_customData.Remove(legacyQuickSlotKey);

                string[] parts = posText.Split(',');
                if (parts.Length >= 1 && int.TryParse(parts[0], out int quickIndex)
                    && quickIndex >= 0 && quickIndex < ValConfig.MaxQuickSlots
                    && slots[quickIndex] is Slot quickSlot && quickSlot.IsFreeQuickSlot()) {
                    item.m_gridPos = quickSlot.GridPosition;
                    quickSlot.ClearItemCache();
                    player.GetInventory().Changed();
                }
            }
        }
    }
}
