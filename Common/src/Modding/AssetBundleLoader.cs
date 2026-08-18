using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Common {
    /// <summary>
    /// Loads an asset bundle embedded in a mod assembly as a manifest resource.
    ///
    /// Replaces the copy of this method that lived in EpicLoot, Jam, AdvancedPortals and
    /// EquipmentAndQuickSlots. Those used Assembly.GetCallingAssembly(); that is unsafe here, because the
    /// caller can be another type in this shared project rather than the mod, so the assembly is explicit.
    /// </summary>
    public static class AssetBundleLoader {
        /// <summary>
        /// Loads the bundle embedded as "&lt;assembly name&gt;.&lt;filename&gt;". Returns null (and logs) on failure.
        /// </summary>
        public static AssetBundle LoadFromResources(string filename, Assembly assembly) {
            if (assembly == null) { throw new ArgumentNullException(nameof(assembly)); }

            string resourceName = $"{assembly.GetName().Name}.{filename}";
            try {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName)) {
                    if (stream == null) {
                        ModLogger.LogError($"Embedded asset bundle '{resourceName}' not found in {assembly.GetName().Name}.");
                        return null;
                    }
                    return AssetBundle.LoadFromStream(stream);
                }
            } catch (Exception e) {
                ModLogger.LogError($"Failed to load embedded asset bundle '{resourceName}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads the bundle and assigns it to <see cref="ModContext.AssetBundle"/> so the shared loaders
        /// can find it without being handed one.
        /// </summary>
        public static AssetBundle LoadIntoContext(string filename, Assembly assembly) {
            AssetBundle bundle = LoadFromResources(filename, assembly);
            if (bundle != null) { ModContext.AssetBundle = bundle; }
            return bundle;
        }
    }
}
