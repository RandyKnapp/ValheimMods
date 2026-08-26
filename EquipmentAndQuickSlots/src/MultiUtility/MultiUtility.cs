using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots.src.MultiUtility {
    // Wearing more than one utility item.
    //
    // Vanilla keeps exactly one reference, Humanoid.m_utilityItem, and EquipItem's type chain
    // unequips whatever is in it before storing the new item. So the first utility item is always
    // vanilla's; anything beyond it is held here, and every vanilla method that reads
    // m_utilityItem needs a matching postfix (see the patches at the bottom of this file).
    //
    // The linchpin is Humanoid.IsItemEquiped: EAQS equipment cells are equipped-only
    // (Slot.ItemBelongs), so an extra item that does not report as equipped is swept out of its
    // cell within a frame, and vanilla's own UnequipItem would refuse to touch it.
    //
    // State lives here rather than in the cells because at EquipItem time the item has not been
    // moved into a cell yet — the validation sweep does that afterwards. Which cell an item ends
    // up in is not meaningful: the registry, not the grid, decides what is worn.
    internal static class MultiUtility {
        // Matches no branch of vanilla's EquipItem type chain, which is the whole point: with this
        // set, EquipItem skips the Utility branch (and its unequip of m_utilityItem) and falls
        // through to the tail. Restored in the postfix.
        private const ItemDrop.ItemData.ItemType passthroughType = (ItemDrop.ItemData.ItemType)8127;

        private static readonly ItemDrop.ItemData[] extras = new ItemDrop.ItemData[MaxUtilitySlots - 1];
        private static Player owner;

        private static readonly HashSet<StatusEffect> pendingEffects = new HashSet<StatusEffect>();

        internal static void Reset() {
            for (int i = 0; i < extras.Length; i++)
                extras[i] = null;

            owner = null;
        }

        private static void EnsureOwner(Player player) {
            if (owner == player)
                return;

            Reset();
            owner = player;
        }

        // Index-based rather than a list, and deliberately so: Humanoid.HaveSetEffect calls
        // GetSetCount, whose postfix enumerates the extras again from inside a loop that is
        // already enumerating them. Nothing shared, nothing allocated, re-entrant by construction.
        //
        // Clipped to the active slot count, so lowering the count stops the bonuses immediately.
        // Entries may be null: unequipping the second of three leaves a hole.
        internal static int GetExtraCount(Humanoid humanoid) =>
            humanoid != null && owner == humanoid ? Mathf.Min(ExtraWearableUtilityItems, extras.Length) : 0;

        internal static ItemDrop.ItemData GetExtra(Humanoid humanoid, int index) =>
            index >= 0 && index < GetExtraCount(humanoid) ? extras[index] : null;

        internal static bool IsExtraItem(Humanoid humanoid, ItemDrop.ItemData item) => GetExtraIndex(humanoid, item) != -1;

        private static int GetExtraIndex(Humanoid humanoid, ItemDrop.ItemData item) {
            if (item == null || owner == null || owner != humanoid)
                return -1;

            for (int i = 0; i < extras.Length; i++)
                if (ReferenceEquals(extras[i], item))
                    return i;

            return -1;
        }

        private static void SetExtra(int index, ItemDrop.ItemData item) {
            if (index < 0 || index >= extras.Length || ReferenceEquals(extras[index], item))
                return;

            extras[index] = item;
            EpicLootCompat.InvalidateEffectCache();
        }

        // Which extra slot should take this item, or -1 to leave it to vanilla's own utility slot.
        // The order of these rules is what enforces "never two copies of the same item worn":
        // replacing a worn duplicate has to be considered before filling any empty slot, including
        // vanilla's own. Otherwise unequipping the first belt and then re-equipping a copy of the
        // second would leave both worn.
        private static int GetTargetExtraIndex(Humanoid humanoid, ItemDrop.ItemData item) {
            if (item == null)
                return -1;

            int active = Mathf.Min(ExtraWearableUtilityItems, extras.Length);

            // A copy of something in an extra slot replaces it there.
            for (int i = 0; i < active; i++)
                if (extras[i] != null && IsSameItem(extras[i], item))
                    return i;

            // A copy of what vanilla is wearing, or an empty vanilla slot: vanilla's own branch
            // does the right thing in both cases, and filling its slot first is what keeps a
            // single utility item behaving exactly as it always has.
            if (humanoid.m_utilityItem == null || IsSameItem(humanoid.m_utilityItem, item))
                return -1;

            for (int i = 0; i < active; i++)
                if (extras[i] == null)
                    return i;

            // No room. Vanilla swaps its own slot rather than refusing the equip.
            return -1;
        }

        // Two distinct stacks of the same thing. The shared descriptor is what identifies an item
        // here, so an upgraded and a plain copy of the same belt still count as the same item.
        private static bool IsSameItem(ItemDrop.ItemData a, ItemDrop.ItemData b) =>
            !ReferenceEquals(a, b) && a.m_shared.m_name == b.m_shared.m_name;

        // Drag-to-equip gate, called from Slots.WouldFitEquipmentSlot. Dropping a second copy of a
        // worn item onto an extra utility cell is refused outright, so the cell tints red and says
        // so, rather than silently swapping it into the base cell. The base cell is left alone:
        // dropping a duplicate there is the swap it has always been.
        //
        // An item already worn may always go back to a utility cell — that is a move, not an equip.
        internal static bool CanEquipIntoUtilityCell(Slot slot, ItemDrop.ItemData item) {
            if (!IsExtraUtilitySlot(slot))
                return true;

            Player player = CurrentPlayer;
            if (player == null || item == null || player.IsItemEquiped(item))
                return true;

            if (player.m_utilityItem != null && IsSameItem(player.m_utilityItem, item))
                return false;

            for (int i = 0; i < extras.Length; i++)
                if (extras[i] != null && IsSameItem(extras[i], item))
                    return false;

            return true;
        }

        // The count changed (config edit, or the server's value arriving on join). Anything past
        // the new limit is unequipped outright: the validation sweep would move it out of its cell
        // but leave it worn, and a lowered server limit has to actually take the bonus away.
        internal static void OnUtilitySlotCountChanged() {
            Player player = Player.m_localPlayer;
            if (player == null || owner != player) {
                EpicLootCompat.InvalidateEffectCache();
                return;
            }

            for (int i = ExtraWearableUtilityItems; i < extras.Length; i++)
                if (extras[i] is ItemDrop.ItemData item)
                    player.UnequipItem(item, triggerEquipEffects: false);

            EpicLootCompat.InvalidateEffectCache();
            player.SetupEquipment();
            SlotValidation.ValidateItems();
            SlotValidation.ValidateSlots();
        }

        // ---------------------------------------------------------------------------------------
        // Equip / unequip

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        private static class Humanoid_EquipItem_RouteExtraUtility {
            private static void Prefix(Humanoid __instance, ItemDrop.ItemData item, ref int __state) {
                __state = -1;

                if (!IsValidPlayer(__instance) || item == null || __instance is not Player player)
                    return;

                EnsureOwner(player);

                if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Utility)
                    return;

                // An item already worn here is a no-op that vanilla's own IsItemEquiped guard
                // rejects before we ever get here.
                if (IsExtraItem(player, item))
                    return;

                // New game plus: vanilla rejects an under-levelled utility item before it reaches
                // the type chain, and only for Utility/Trinket. Leaving the type alone here is
                // what keeps that check applying to the extra slots too.
                if (Game.m_worldLevel > 0 && item.m_worldLevel < Game.m_worldLevel)
                    return;

                if ((__state = GetTargetExtraIndex(player, item)) == -1)
                    return;

                item.m_shared.m_itemType = passthroughType;
            }

            // First, so the borrowed item type is back to Utility before any other mod's postfix
            // observes it — m_shared is the descriptor shared by every copy of the item.
            [HarmonyPriority(Priority.First)]
            private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects, int __state, bool __result) {
                if (__state == -1 || item == null || item.m_shared.m_itemType != passthroughType)
                    return;

                item.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Utility;

                // Vanilla's guards sit above the type chain and can still refuse the equip:
                // mid-attack, swimming, a broken item, an item no longer in the inventory. Nothing
                // is worn in that case, so restoring the type is all there was to do.
                if (!__result)
                    return;

                // Replayed here rather than in the prefix, so a refused equip stays silent: this is
                // the effect the skipped Utility branch would have spawned.
                if (__instance.m_visEquipment && __instance.m_visEquipment.m_isPlayer)
                    item.m_shared.m_equipEffect.Create(__instance.transform.position + Vector3.up, __instance.transform.rotation);

                if (extras[__state] is ItemDrop.ItemData previous)
                    __instance.UnequipItem(previous, triggerEquipEffects);

                SetExtra(__state, item);

                // Vanilla's tail ran while the item was still unknown to us, so its own
                // "if (IsItemEquiped(item)) m_equipped = true" did not fire.
                item.m_equipped = true;
                __instance.SetupEquipment();
            }

            // The borrowed shared type must be restored even when EquipItem (or another mod's patch
            // on it) throws and the postfix never runs -- m_shared is the descriptor for every copy
            // of this item, and a stranded passthrough type would leave the item unequippable (and
            // misrouted everywhere) for the rest of the session.
            private static void Finalizer(ItemDrop.ItemData item, int __state) {
                if (__state != -1 && item != null && item.m_shared.m_itemType == passthroughType)
                    item.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Utility;
            }
        }

        // No prefix needed: the IsItemEquiped postfix gets vanilla past its early-out, no branch of
        // its type chain matches an extra item, and its tail already clears m_equipped and fires
        // the unequip effect for us.
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        private static class Humanoid_UnequipItem_ClearExtraUtility {
            [HarmonyPriority(Priority.First)]
            private static void Postfix(Humanoid __instance, ItemDrop.ItemData item) {
                int index = GetExtraIndex(__instance, item);
                if (index == -1)
                    return;

                SetExtra(index, null);
                // Vanilla's SetupEquipment ran while the item was still registered.
                __instance.SetupEquipment();
            }
        }

        // Load-bearing. Without this the equipment cells reject the extra items (Slot.ItemBelongs),
        // UnequipItem refuses to touch them, and EquipItem happily equips the same one twice.
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsItemEquiped))]
        private static class Humanoid_IsItemEquiped_IncludeExtraUtility {
            private static void Postfix(Humanoid __instance, ItemDrop.ItemData item, ref bool __result) {
                if (!__result)
                    __result = IsExtraItem(__instance, item);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipAllItems))]
        private static class Humanoid_UnequipAllItems_IncludeExtraUtility {
            [HarmonyPriority(Priority.First)]
            private static void Postfix(Humanoid __instance) {
                if (owner != __instance)
                    return;

                // Every entry, not just the active ones: a count lowered while the player was
                // elsewhere can leave a worn item in a slot that no longer grants anything.
                for (int i = 0; i < extras.Length; i++)
                    if (extras[i] is ItemDrop.ItemData item)
                        __instance.UnequipItem(item, triggerEquipEffects: false);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UnequipDeathDropItems))]
        private static class Player_UnequipDeathDropItems_IncludeExtraUtility {
            private static void Prefix(Player __instance) {
                if (!IsValidPlayer(__instance) || owner != __instance)
                    return;

                // A real unequip, the same treatment vanilla gives m_utilityItem two lines further
                // down, so the status effects come off with the item rather than lingering.
                for (int i = 0; i < extras.Length; i++)
                    if (extras[i] is ItemDrop.ItemData item)
                        __instance.UnequipItem(item, triggerEquipEffects: false);
            }
        }

        // ---------------------------------------------------------------------------------------
        // Everything vanilla does for m_utilityItem and would otherwise skip for the extras.
        // Deliberately absent: Player.ApplyArmorDamageMods and Player.GetBodyArmor. Neither
        // includes the vanilla utility item, so adding the extras there would make the second and
        // third slots stronger than the first.

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetEquipmentWeight))]
        private static class Humanoid_GetEquipmentWeight_IncludeExtraUtility {
            private static void Postfix(Humanoid __instance, ref float __result) {
                int count = GetExtraCount(__instance);
                for (int i = 0; i < count; i++)
                    if (extras[i] is ItemDrop.ItemData item)
                        __result += item.m_shared.m_weight;
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipment))]
        private static class Humanoid_UpdateEquipment_DrainExtraUtility {
            private static void Postfix(Humanoid __instance, float dt) {
                int count = GetExtraCount(__instance);
                for (int i = 0; i < count; i++)
                    if (extras[i] is ItemDrop.ItemData item && item.m_shared.m_useDurability)
                        __instance.DrainEquipedItemDurability(item, dt);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetSetCount))]
        private static class Humanoid_GetSetCount_IncludeExtraUtility {
            private static void Postfix(Humanoid __instance, string setName, ref int __result) {
                int count = GetExtraCount(__instance);
                for (int i = 0; i < count; i++)
                    if (extras[i] is ItemDrop.ItemData item && item.m_shared.m_setName == setName)
                        __result++;
            }
        }

        // Vanilla rebuilds m_equipmentStatusEffects from its own nine slots and removes anything
        // that is no longer granted. The prefix records what the extras grant, the SEMan patch
        // below stops those being torn off mid-rebuild, and the postfix adds them back in. The
        // finalizer clears the set even if the body throws — a stuck set would leave the player
        // unable to lose those effects at all.
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects))]
        private static class Humanoid_UpdateEquipmentStatusEffects_IncludeExtraUtility {
            private static void Prefix(Humanoid __instance) {
                pendingEffects.Clear();

                int count = GetExtraCount(__instance);
                for (int i = 0; i < count; i++) {
                    if (extras[i] is not ItemDrop.ItemData item)
                        continue;

                    if (item.m_shared.m_equipStatusEffect)
                        pendingEffects.Add(item.m_shared.m_equipStatusEffect);

                    // Re-enters GetSetCount, which is why the extras are read by index here.
                    if (__instance.HaveSetEffect(item))
                        pendingEffects.Add(item.m_shared.m_setStatusEffect);
                }
            }

            private static void Postfix(Humanoid __instance) {
                if (pendingEffects.Count == 0)
                    return;

                foreach (StatusEffect effect in pendingEffects)
                    if (!__instance.m_equipmentStatusEffects.Contains(effect))
                        __instance.m_seman.AddStatusEffect(effect);

                __instance.m_equipmentStatusEffects.UnionWith(pendingEffects);
            }

            private static void Finalizer() {
                pendingEffects.Clear();
            }
        }

        [HarmonyPatch(typeof(SEMan), nameof(SEMan.RemoveStatusEffect), typeof(int), typeof(bool))]
        private static class SEMan_RemoveStatusEffect_KeepExtraUtilityEffects {
            private static void Prefix(SEMan __instance, ref int nameHash) {
                // pendingEffects is only populated while a status-effect rebuild is in flight.
                if (pendingEffects.Count == 0 || __instance != CurrentPlayer?.GetSEMan())
                    return;

                foreach (StatusEffect effect in pendingEffects) {
                    if (effect.NameHash() == nameHash) {
                        nameHash = 0;
                        return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentEitrRegenModifier))]
        private static class Player_GetEquipmentEitrRegenModifier_IncludeExtraUtility {
            private static void Postfix(Player __instance, ref float __result) {
                int count = GetExtraCount(__instance);
                for (int i = 0; i < count; i++)
                    if (extras[i] is ItemDrop.ItemData item)
                        __result += item.m_shared.m_eitrRegenModifier;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.UpdateModifiers))]
        private static class Player_UpdateModifiers_IncludeExtraUtility {
            private static void Postfix(Player __instance) {
                if (Player.s_equipmentModifierSourceFields == null || __instance.m_equipmentModifierValues == null)
                    return;

                int count = GetExtraCount(__instance);
                for (int i = 0; i < __instance.m_equipmentModifierValues.Length; i++)
                    for (int e = 0; e < count; e++)
                        if (extras[e] is ItemDrop.ItemData item)
                            __instance.m_equipmentModifierValues[i] += (float)Player.s_equipmentModifierSourceFields[i].GetValue(item.m_shared);
            }
        }

        // The registry holds references into the player inventory; anything that left it (dropped,
        // stored, destroyed) is no longer worn.
        [HarmonyPatch(typeof(Player), nameof(Player.OnInventoryChanged))]
        private static class Player_OnInventoryChanged_DropStaleExtraUtility {
            private static void Postfix(Player __instance) {
                if (!IsValidPlayer(__instance) || __instance.m_isLoading || owner != __instance)
                    return;

                Inventory inventory = __instance.GetInventory();
                if (inventory == null)
                    return;

                for (int i = 0; i < extras.Length; i++)
                    if (extras[i] is ItemDrop.ItemData item && !inventory.ContainsItem(item))
                        SetExtra(i, null);
            }
        }

        // A fresh local player (respawn, character switch) starts with an empty registry.
        [HarmonyPatch(typeof(Player), nameof(Player.OnDestroy))]
        private static class Player_OnDestroy_ResetExtraUtility {
            private static void Postfix(Player __instance) {
                if (owner == __instance)
                    Reset();
            }
        }
    }
}
