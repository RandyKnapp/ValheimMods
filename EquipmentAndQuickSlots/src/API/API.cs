using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots
{
    // The public integration surface for other mods. Callable via pure reflection
    // (Type.GetType("EquipmentAndQuickSlots.API, EquipmentAndQuickSlots")) or through the typed
    // EquipmentAndQuickSlotsAPI.dll shim.
    //
    // The signature rule: nothing but primitives, string, vanilla/Unity types and System.Func/
    // Action built from those crosses this boundary — no EAQS type in any signature. Structured
    // data travels as JSON strings; slots are identified by caller-supplied namespaced string
    // ids. `ref` rather than `out`, so a reflection transport can read mutated arguments back
    // out of the object[].
    public static partial class API
    {
        public const int ApiVersion = 2;

        private static readonly List<Action<string>> slotChangedListeners = new List<Action<string>>();
        private static readonly List<Action<string, ItemDrop.ItemData, ItemDrop.ItemData>> slotItemChangedListeners = new List<Action<string, ItemDrop.ItemData, ItemDrop.ItemData>>();
        private static readonly Dictionary<string, ItemDrop.ItemData> lastKnownSlotItems = new Dictionary<string, ItemDrop.ItemData>();

        // ---------------------------------------------------------------------------------------
        // Versioning / diagnostics

        public static int GetApiVersion() => ApiVersion;

        public static string GetPluginVersion() => EquipmentAndQuickSlots.Version;

        public static string GetPluginId() => EquipmentAndQuickSlots.PluginId;

        public static bool HasEndpoint(string name) =>
            typeof(API).GetMethods(BindingFlags.Public | BindingFlags.Static).Any(m => m.Name == name);

        public static List<string> GetEndpointNames() =>
            typeof(API).GetMethods(BindingFlags.Public | BindingFlags.Static).Select(m => m.Name).Distinct().OrderBy(n => n).ToList();

        // ---------------------------------------------------------------------------------------
        // Custom slots

        /// <summary>
        /// Registers a custom slot in one of the reserved cells. slotId must be unique per
        /// consumer (namespace it with your plugin name); ownerPluginGuid is recorded for
        /// diagnostics. Returns false when the id is taken or capacity is exhausted.
        /// </summary>
        public static bool AddSlot(string slotId, string ownerPluginGuid, string nameToken, Func<ItemDrop.ItemData, bool> isValid, Func<bool> isActive)
        {
            if (string.IsNullOrEmpty(slotId))
                return false;

            bool added = TryAddCustomSlot(slotId, ownerPluginGuid,
                () => nameToken ?? "",
                item => GuardedPredicate(ownerPluginGuid, isValid, item),
                () => GuardedActive(ownerPluginGuid, isActive));

            if (added)
                NotifySlotChanged(slotId);

            return added;
        }

        /// <summary>
        /// Removes a previously registered custom slot. A resident item is relocated to the
        /// inventory (or dropped as a last resort), never destroyed.
        /// </summary>
        public static bool RemoveSlot(string slotId)
        {
            bool removed = TryRemoveCustomSlot(slotId);
            if (removed)
            {
                lastKnownSlotItems.Remove(customSlotPrefix + slotId);
                NotifySlotChanged(slotId);
            }

            return removed;
        }

        private static bool GuardedPredicate(string owner, Func<ItemDrop.ItemData, bool> isValid, ItemDrop.ItemData item)
        {
            if (isValid == null)
                return true;

            try
            {
                return isValid(item);
            }
            catch (Exception ex)
            {
                // Always logged — a throwing consumer delegate is their bug and must be visible
                UnityEngine.Debug.LogWarning($"[EAQS API] isValid delegate of '{owner}' threw: {ex}");
                return false;
            }
        }

        private static bool GuardedActive(string owner, Func<bool> isActive)
        {
            if (isActive == null)
                return true;

            try
            {
                return isActive();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[EAQS API] isActive delegate of '{owner}' threw: {ex}");
                return false;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Queries

        public static string GetSlotIdsJson()
        {
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (Slot slot in slots)
            {
                if (slot.IsEmptySlot)
                    continue;

                if (!first)
                    sb.Append(',');
                first = false;
                sb.Append('"').Append(Escape(PublicSlotId(slot))).Append('"');
            }

            return sb.Append(']').ToString();
        }

        public static string GetSlotInfoJson(string slotId)
        {
            Slot slot = FindPublicSlot(slotId);
            if (slot == null)
                return null;

            return "{"
                   + $"\"id\":\"{Escape(PublicSlotId(slot))}\","
                   + $"\"index\":{slot.Index},"
                   + $"\"nameToken\":\"{Escape(slot.Name)}\","
                   + $"\"active\":{(slot.IsActive ? "true" : "false")},"
                   + $"\"gridX\":{slot.GridPosition.x},"
                   + $"\"gridY\":{slot.GridPosition.y},"
                   + $"\"isQuickSlot\":{(slot.IsQuickSlot ? "true" : "false")},"
                   + $"\"isEquipmentSlot\":{(slot.IsEquipmentSlot ? "true" : "false")},"
                   + $"\"isCustomSlot\":{(slot.IsCustomSlot ? "true" : "false")},"
                   + $"\"ownerPluginGuid\":\"{Escape(slot.OwnerGuid ?? "")}\","
                   + $"\"occupied\":{(slot.Item != null ? "true" : "false")}"
                   + "}";
        }

        public static bool TryGetSlotItem(string slotId, ref ItemDrop.ItemData item)
        {
            Slot slot = FindPublicSlot(slotId);
            if (slot == null)
                return false;

            item = slot.Item;
            return item != null;
        }

        public static bool IsSlotCell(int x, int y, ref string slotId)
        {
            Slot slot = GetSlotInGrid(new Vector2i(x, y));
            if (slot == null || slot.IsEmptySlot)
                return false;

            slotId = PublicSlotId(slot);
            return true;
        }

        public static List<ItemDrop.ItemData> GetQuickSlotItems() =>
            GetQuickSlots().Where(slot => slot.IsActive).Select(slot => slot.Item).Where(item => item != null).ToList();

        public static List<ItemDrop.ItemData> GetEquipmentSlotItems() =>
            GetEquipmentSlots().Select(slot => slot.Item).Where(item => item != null).ToList();

        public static int GetVisibleRows() => VisibleRows;

        public static int GetFullHeight() => FullHeight;

        // ---------------------------------------------------------------------------------------
        // Listeners — plain method calls so a reflection-only consumer can subscribe without
        // binding to an event field.

        public static void AddSlotChangedListener(Action<string> listener)
        {
            if (listener != null && !slotChangedListeners.Contains(listener))
                slotChangedListeners.Add(listener);
        }

        public static void RemoveSlotChangedListener(Action<string> listener)
        {
            slotChangedListeners.Remove(listener);
        }

        public static void AddSlotItemChangedListener(Action<string, ItemDrop.ItemData, ItemDrop.ItemData> listener)
        {
            if (listener != null && !slotItemChangedListeners.Contains(listener))
                slotItemChangedListeners.Add(listener);
        }

        public static void RemoveSlotItemChangedListener(Action<string, ItemDrop.ItemData, ItemDrop.ItemData> listener)
        {
            slotItemChangedListeners.Remove(listener);
        }

        // ---------------------------------------------------------------------------------------
        // Internals

        private static string PublicSlotId(Slot slot) =>
            slot.IsCustomSlot ? slot.ID.Substring(customSlotPrefix.Length) : slot.ID;

        private static Slot FindPublicSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
                return null;

            Slot slot = FindSlot(slotId) ?? FindSlot(customSlotPrefix + slotId);
            return slot != null && !slot.IsEmptySlot ? slot : null;
        }

        private static string Escape(string value) => value?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

        private static void NotifySlotChanged(string slotId)
        {
            foreach (var listener in slotChangedListeners.ToList())
            {
                try
                {
                    listener(slotId);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[EAQS API] slot-changed listener threw: {ex}");
                }
            }
        }

        // Drained once per frame from the plugin's LateUpdate, after the validation sweep, so
        // listeners observe settled state.
        internal static void DetectSlotItemChanges()
        {
            if (slotItemChangedListeners.Count == 0 || Player.m_localPlayer == null)
                return;

            foreach (Slot slot in slots)
            {
                if (slot.IsEmptySlot)
                    continue;

                string id = slot.ID;
                ItemDrop.ItemData current = slot.Item;
                lastKnownSlotItems.TryGetValue(id, out ItemDrop.ItemData previous);

                if (ReferenceEquals(current, previous))
                    continue;

                lastKnownSlotItems[id] = current;

                string publicId = PublicSlotId(slot);
                foreach (var listener in slotItemChangedListeners.ToList())
                {
                    try
                    {
                        listener(publicId, previous, current);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"[EAQS API] slot-item-changed listener threw: {ex}");
                    }
                }
            }
        }
    }
}
