using System;
using System.Reflection;
using HarmonyLib;

namespace EquipmentAndQuickSlots {
    // Shared plumbing for the compatibility shims. Everything here resolves the other mod by name
    // through reflection, so no shim ever forces an assembly reference and a missing or renamed
    // member degrades to a log line instead of a TypeLoadException.
    internal static class CompatibilityHelper {
        /// <summary>
        /// Removes one Harmony patch another mod applied to a vanilla method. Every lookup failure
        /// is logged and swallowed: the other mod is free to rename or drop the patch in a later
        /// version, and that must not take this mod down with it.
        /// </summary>
        internal static bool RemoveHarmonyPatch(Harmony harmony, Assembly assembly, Type patchedType, string patchedMethod, string patcherClassName, string patcherClassMethod, string reason) {
            if (harmony == null || assembly == null)
                return false;

            Type patcherType = assembly.GetType(patcherClassName);
            if (patcherType == null) {
                EquipmentAndQuickSlots.LogInfo($"Compatibility: {patcherClassName} not found; nothing to unpatch");
                return false;
            }

            if (AccessTools.Method(patchedType, patchedMethod) is not MethodInfo method) {
                EquipmentAndQuickSlots.LogInfo($"Compatibility: {patchedType.Name}.{patchedMethod} not found; nothing to unpatch");
                return false;
            }

            if (AccessTools.Method(patcherType, patcherClassMethod) is not MethodInfo patch) {
                EquipmentAndQuickSlots.LogInfo($"Compatibility: {patcherClassName}.{patcherClassMethod} not found; nothing to unpatch");
                return false;
            }

            try {
                harmony.Unpatch(method, patch);
            } catch (Exception ex) {
                EquipmentAndQuickSlots.LogWarning($"Compatibility: failed to unpatch {patcherClassName}.{patcherClassMethod}: {ex.Message}");
                return false;
            }

            EquipmentAndQuickSlots.LogInfo($"Compatibility: unpatched {patcherClassName}.{patcherClassMethod} from {patchedType.Name}.{patchedMethod} to {reason}");
            return true;
        }

        /// <summary>
        /// Binds a static method of another mod to a delegate once, so the call site pays no
        /// reflection cost. Null when the method is missing or its signature has changed.
        /// </summary>
        internal static TDelegate BindStatic<TDelegate>(Type type, string methodName) where TDelegate : class {
            if (type == null)
                return null;

            if (AccessTools.Method(type, methodName) is not MethodInfo method || !method.IsStatic)
                return null;

            try {
                return Delegate.CreateDelegate(typeof(TDelegate), method, throwOnBindFailure: false) as TDelegate;
            } catch (Exception) {
                return null;
            }
        }
    }
}
