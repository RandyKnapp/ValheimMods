using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots {
    // Change detector for the controlled hotkey bars. HotkeyBar.UpdateIcons is expensive once
    // EpicLoot / MyLittleUI decorate it, so the Hud.Update controller only calls it when the
    // fingerprint of everything it renders has changed. Item membership and positions are
    // covered by a revision counter bumped on inventory/equip events; m_durability and m_stack
    // are mutated in place without Inventory.Changed(), so they are sampled each frame from the
    // item refs cached at the last refresh. A heartbeat catches what the fingerprint can't see
    // (m_shared mutations, icon swaps, rebind labels, other mods' dirty channels).
    internal class HotkeyBarRefreshGate {
        private const float HeartbeatInterval = 1f;

        private static int _inventoryRevision;

        private ItemDrop.ItemData[] _items = new ItemDrop.ItemData[8];
        private int[] _stacks = new int[8];
        private float[] _durabilities = new float[8];
        private bool[] _equipped = new bool[8];
        private int[] _qualities = new int[8];
        private int[] _variants = new int[8];
        private int[] _gridX = new int[8];

        private int _itemCount;
        private int _elementCount = -1; // sentinel: never sampled, so the first check refreshes
        private int _selected;
        private int _revision;
        private int _actionQueueCount;
        private bool _gamepadActive;
        private bool _playerAlive;
        private float _lastRefreshTime;

        internal bool ShouldRefresh(HotkeyBar bar, Player player) {
            if (_elementCount == -1
                || !player.IsDead() != _playerAlive
                || bar.m_elements.Count != _elementCount // catches MyLittleUI's UpdateIcons(null) element wipe
                || bar.m_selected != _selected
                || _revision != _inventoryRevision
                || ZInput.IsGamepadActive() != _gamepadActive
                || player.GetActionQueueCount() != _actionQueueCount
                || Time.unscaledTime - _lastRefreshTime > HeartbeatInterval)
                return true;

            for (int i = 0; i < _itemCount; i++) {
                ItemDrop.ItemData item = _items[i];
                if (item.m_stack != _stacks[i]
                    || item.m_durability != _durabilities[i]
                    || item.m_equipped != _equipped[i]
                    || item.m_quality != _qualities[i]
                    || item.m_variant != _variants[i]
                    || item.m_gridPos.x != _gridX[i])
                    return true;

                // A broken item blinks red at 10 Hz inside UpdateIcons -- keep feeding it frames.
                if (item.m_shared.m_useDurability && item.m_durability <= 0f)
                    return true;
            }

            return false;
        }

        // Called right after an actual bar.UpdateIcons(player); bar.m_items still holds exactly
        // the items that call rendered (including the quick slot items GetBoundItems swapped in).
        internal void Resample(HotkeyBar bar, Player player) {
            _playerAlive = !player.IsDead();
            _itemCount = _playerAlive ? bar.m_items.Count : 0; // the dead branch never reads m_items

            if (_itemCount > _items.Length)
                Grow(_itemCount);

            for (int i = 0; i < _itemCount; i++) {
                ItemDrop.ItemData item = bar.m_items[i];
                _items[i] = item;
                _stacks[i] = item.m_stack;
                _durabilities[i] = item.m_durability;
                _equipped[i] = item.m_equipped;
                _qualities[i] = item.m_quality;
                _variants[i] = item.m_variant;
                _gridX[i] = item.m_gridPos.x;
            }

            for (int i = _itemCount; i < _items.Length; i++)
                _items[i] = null; // don't keep dropped items alive

            _elementCount = bar.m_elements.Count;
            _selected = bar.m_selected;
            _revision = _inventoryRevision;
            _actionQueueCount = player.GetActionQueueCount();
            _gamepadActive = ZInput.IsGamepadActive();
            _lastRefreshTime = Time.unscaledTime;
        }

        private void Grow(int size) {
            _items = new ItemDrop.ItemData[size];
            _stacks = new int[size];
            _durabilities = new float[size];
            _equipped = new bool[size];
            _qualities = new int[size];
            _variants = new int[size];
            _gridX = new int[size];
        }

        // Item movement raises Player.OnInventoryChanged, but equip state (and the ammo a bow
        // will draw, which MyLittleUI renders on the bar) changes without it, so equip and
        // unequip bump too. No loading guard here: loading-time changes must still invalidate.
        [HarmonyPatch]
        private static class InventoryEvents_BumpRevision {
            private static IEnumerable<MethodBase> TargetMethods() {
                yield return AccessTools.Method(typeof(Player), nameof(Player.OnInventoryChanged));
                yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.EquipItem));
                yield return AccessTools.Method(typeof(Humanoid), nameof(Humanoid.UnequipItem));
            }

            private static void Postfix(Humanoid __instance) {
                if (__instance == Player.m_localPlayer)
                    _inventoryRevision++;
            }
        }
    }
}
