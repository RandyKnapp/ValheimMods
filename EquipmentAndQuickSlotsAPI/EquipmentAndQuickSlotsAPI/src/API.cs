using System;
using System.Collections.Generic;

namespace EquipmentAndQuickSlotsAPI
{
    /// <summary>
    /// Typed wrappers over EquipmentAndQuickSlots' <c>EquipmentAndQuickSlots.API</c> facade.
    /// Embed this assembly into your plugin (ILRepack) and declare a soft dependency:
    /// <c>[BepInDependency("randyknapp.mods.equipmentandquickslots", BepInDependency.DependencyFlags.SoftDependency)]</c>.
    /// Gate every call path on <see cref="IsLoaded"/> — when the mod is absent, calls warn and
    /// no-op rather than throw.
    /// </summary>
    public static class EAQS
    {
        public static readonly Logger logger = new Logger();

        private static Method API_GetApiVersion;
        private static Method API_GetPluginVersion;
        private static Method API_HasEndpoint;
        private static Method API_AddSlot;
        private static Method API_RemoveSlot;
        private static Method API_GetSlotIdsJson;
        private static Method API_GetSlotInfoJson;
        private static Method API_TryGetSlotItem;
        private static Method API_IsSlotCell;
        private static Method API_GetQuickSlotItems;
        private static Method API_GetEquipmentSlotItems;
        private static Method API_GetVisibleRows;
        private static Method API_GetFullHeight;
        private static Method API_AddSlotChangedListener;
        private static Method API_RemoveSlotChangedListener;
        private static Method API_AddSlotItemChangedListener;
        private static Method API_RemoveSlotItemChangedListener;

        private static Method Resolve(ref Method cache, string name) => cache ?? (cache = new Method(name));

        /// <summary>True when EquipmentAndQuickSlots is installed and its API type is loadable.</summary>
        public static bool IsLoaded() => Method.ApiTypeExists();

        public static int GetApiVersion()
        {
            object[] result = Resolve(ref API_GetApiVersion, "GetApiVersion").Invoke();
            return (int)(result?[0] ?? 0);
        }

        public static string GetPluginVersion()
        {
            object[] result = Resolve(ref API_GetPluginVersion, "GetPluginVersion").Invoke();
            return (string)(result?[0] ?? "");
        }

        public static bool HasEndpoint(string name)
        {
            object[] result = Resolve(ref API_HasEndpoint, "HasEndpoint").Invoke(name);
            return (bool)(result?[0] ?? false);
        }

        /// <summary>
        /// Registers a custom slot. Namespace slotId with your plugin name; pass your plugin GUID
        /// as ownerPluginGuid. nameToken may be a localization token or plain text.
        /// </summary>
        public static bool AddSlot(string slotId, string ownerPluginGuid, string nameToken, Func<ItemDrop.ItemData, bool> isValid, Func<bool> isActive)
        {
            object[] result = Resolve(ref API_AddSlot, "AddSlot").Invoke(slotId, ownerPluginGuid, nameToken, isValid, isActive);
            return (bool)(result?[0] ?? false);
        }

        public static bool RemoveSlot(string slotId)
        {
            object[] result = Resolve(ref API_RemoveSlot, "RemoveSlot").Invoke(slotId);
            return (bool)(result?[0] ?? false);
        }

        /// <summary>Raw JSON array of all slot ids.</summary>
        public static string GetSlotIdsJson()
        {
            object[] result = Resolve(ref API_GetSlotIdsJson, "GetSlotIdsJson").Invoke();
            return (string)(result?[0] ?? "[]");
        }

        /// <summary>Parsed convenience over <see cref="GetSlotIdsJson"/>.</summary>
        public static List<string> GetSlotIds()
        {
            var ids = new List<string>();
            string json = GetSlotIdsJson();

            foreach (string part in json.Trim('[', ']').Split(','))
            {
                string id = part.Trim().Trim('"');
                if (id.Length > 0)
                    ids.Add(id.Replace("\\\"", "\"").Replace("\\\\", "\\"));
            }

            return ids;
        }

        /// <summary>Raw JSON object describing a slot (index, name, active, grid position, owner), or null.</summary>
        public static string GetSlotInfoJson(string slotId)
        {
            object[] result = Resolve(ref API_GetSlotInfoJson, "GetSlotInfoJson").Invoke(slotId);
            return (string)result?[0];
        }

        public static bool TryGetSlotItem(string slotId, out ItemDrop.ItemData item)
        {
            item = null;
            object[] result = Resolve(ref API_TryGetSlotItem, "TryGetSlotItem").Invoke(slotId, item);
            if (result == null)
                return false;

            item = (ItemDrop.ItemData)result[2];
            return (bool)(result[0] ?? false);
        }

        public static bool IsSlotCell(int x, int y, out string slotId)
        {
            slotId = null;
            object[] result = Resolve(ref API_IsSlotCell, "IsSlotCell").Invoke(x, y, slotId);
            if (result == null)
                return false;

            slotId = (string)result[3];
            return (bool)(result[0] ?? false);
        }

        public static List<ItemDrop.ItemData> GetQuickSlotItems()
        {
            object[] result = Resolve(ref API_GetQuickSlotItems, "GetQuickSlotItems").Invoke();
            return (List<ItemDrop.ItemData>)(result?[0] ?? new List<ItemDrop.ItemData>());
        }

        public static List<ItemDrop.ItemData> GetEquipmentSlotItems()
        {
            object[] result = Resolve(ref API_GetEquipmentSlotItems, "GetEquipmentSlotItems").Invoke();
            return (List<ItemDrop.ItemData>)(result?[0] ?? new List<ItemDrop.ItemData>());
        }

        public static int GetVisibleRows()
        {
            object[] result = Resolve(ref API_GetVisibleRows, "GetVisibleRows").Invoke();
            return (int)(result?[0] ?? 4);
        }

        public static int GetFullHeight()
        {
            object[] result = Resolve(ref API_GetFullHeight, "GetFullHeight").Invoke();
            return (int)(result?[0] ?? 4);
        }

        /// <summary>Fires when a custom slot is added or removed; payload is the slot id.</summary>
        public static void AddSlotChangedListener(Action<string> listener) =>
            Resolve(ref API_AddSlotChangedListener, "AddSlotChangedListener").Invoke(listener);

        public static void RemoveSlotChangedListener(Action<string> listener) =>
            Resolve(ref API_RemoveSlotChangedListener, "RemoveSlotChangedListener").Invoke(listener);

        /// <summary>
        /// Fires when the item in any slot changes; payload is (slotId, oldItem, newItem), with
        /// null for empty. Raised once per frame after EAQS's validation has settled.
        /// </summary>
        public static void AddSlotItemChangedListener(Action<string, ItemDrop.ItemData, ItemDrop.ItemData> listener) =>
            Resolve(ref API_AddSlotItemChangedListener, "AddSlotItemChangedListener").Invoke(listener);

        public static void RemoveSlotItemChangedListener(Action<string, ItemDrop.ItemData, ItemDrop.ItemData> listener) =>
            Resolve(ref API_RemoveSlotItemChangedListener, "RemoveSlotItemChangedListener").Invoke(listener);
    }
}
