using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots {
    // Epic Loot integration through its supported provider API (EpicLoot.API, reflection — no
    // assembly reference, Epic Loot stays a soft dependency):
    //
    //  - Equipment provider: items sitting in API custom slots are reported as equipped, so their
    //    magic effects, set bonuses, tooltips and equip-effect visuals count exactly like gear in
    //    a vanilla slot. The built-in paperdoll cells need nothing — their items are vanilla-
    //    equipped and Epic Loot already sees them through Inventory.GetEquippedItems().
    //  - Effect cache invalidation whenever a custom slot's content changes (drag & drop is not an
    //    Equip/Unequip call, which is all Epic Loot watches on its own).
    //  - Sacrifice filter: nothing resting in an equipment or custom cell can be sacrificed by
    //    accident at the enchanting table.
    internal static class EpicLootCompat {
        public const string EpicLootGUID = "randyknapp.mods.epicloot";
        private const string ApiTypeName = "EpicLoot.API, EpicLoot";
        private const string ProviderId = EquipmentAndQuickSlots.PluginId;

        private static bool _initialized;
        private static MethodInfo _invalidatePlayerEffectCache;

        public static bool IsLoaded => Chainloader.PluginInfos.ContainsKey(EpicLootGUID);

        internal static void Initialize() {
            if (_initialized || !IsLoaded)
                return;

            _initialized = true;

            try {
                Type api = Type.GetType(ApiTypeName);
                if (api == null) {
                    EquipmentAndQuickSlots.LogWarning("Epic Loot is loaded but EpicLoot.API could not be resolved; slot items will not contribute magic effects");
                    return;
                }

                MethodInfo registerEquipmentProvider = api.GetMethod("RegisterEquipmentProvider", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string), typeof(Func<Player, List<ItemDrop.ItemData>>) }, null);
                MethodInfo registerSacrificeFilter = api.GetMethod("RegisterSacrificeFilter", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(string), typeof(Func<ItemDrop.ItemData, bool>) }, null);
                _invalidatePlayerEffectCache = api.GetMethod("InvalidatePlayerEffectCache", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(Player) }, null);

                if (registerEquipmentProvider == null || _invalidatePlayerEffectCache == null) {
                    EquipmentAndQuickSlots.LogWarning("Epic Loot's equipment provider API is missing (older Epic Loot?); custom slot items will not contribute magic effects");
                    return;
                }

                Func<Player, List<ItemDrop.ItemData>> getExtraEquipped = GetCustomSlotEquipment;
                registerEquipmentProvider.Invoke(null, new object[] { ProviderId, getExtraEquipped });

                if (registerSacrificeFilter != null) {
                    Func<ItemDrop.ItemData, bool> canSacrifice = CanSacrifice;
                    registerSacrificeFilter.Invoke(null, new object[] { ProviderId, canSacrifice });
                }

                // Drag & drop into or out of a custom slot is an inventory move, not an equip —
                // tell Epic Loot its memoized totals are stale.
                API.AddSlotItemChangedListener(OnSlotItemChanged);
                API.AddSlotChangedListener(_ => InvalidateEffectCache());

                EquipmentAndQuickSlots.Log("Epic Loot equipment provider registered for custom slots");
            } catch (Exception ex) {
                EquipmentAndQuickSlots.LogWarning($"Epic Loot integration failed: {ex}");
            }
        }

        private static List<ItemDrop.ItemData> GetCustomSlotEquipment(Player player) {
            // Our slots only track the local player's inventory
            if (player == null || player != Player.m_localPlayer)
                return null;

            return GetCustomSlots()
                .Where(slot => slot.IsActive)
                .Select(slot => slot.Item)
                .Where(item => item != null)
                .ToList();
        }

        private static bool CanSacrifice(ItemDrop.ItemData item) {
            return !(GetItemSlot(item) is Slot slot && (slot.IsCustomSlot || slot.IsEquipmentSlot));
        }

        private static void OnSlotItemChanged(string slotId, ItemDrop.ItemData oldItem, ItemDrop.ItemData newItem) {
            if (FindSlot(customSlotPrefix + slotId) is Slot slot && slot.IsCustomSlot)
                InvalidateEffectCache();
        }

        internal static void InvalidateEffectCache() {
            Player player = Player.m_localPlayer;
            if (player == null || _invalidatePlayerEffectCache == null)
                return;

            try {
                _invalidatePlayerEffectCache.Invoke(null, new object[] { player });
            } catch (Exception ex) {
                EquipmentAndQuickSlots.LogWarning($"Epic Loot InvalidatePlayerEffectCache failed: {ex}");
            }
        }
    }
}
