using System;
using System.Collections.Generic;
using System.Reflection;

namespace EquipmentAndQuickSlotsAPI
{
    /// <summary>
    /// Reflection transport to the mod's <c>EquipmentAndQuickSlots.API</c> facade. Type and
    /// method resolution happen once and are cached; a missing endpoint (older mod version, or
    /// the mod absent) resolves to null and every Invoke becomes a warn-and-no-op.
    /// </summary>
    internal class Method
    {
        private const string Namespace = "EquipmentAndQuickSlots";
        private const string ClassName = "API";
        private const string Assembly = "EquipmentAndQuickSlots";
        internal const string API_LOCATION = Namespace + "." + ClassName + ", " + Assembly;

        private static readonly Dictionary<string, Type> CachedTypes = new Dictionary<string, Type>();
        private readonly MethodInfo info;

        public bool IsResolved => info != null;

        /// <summary>
        /// Invokes the cached static method. Returns an array whose first entry is the return
        /// value followed by the (possibly mutated) arguments, so ref parameters can be read
        /// back. Null when the endpoint is unresolved.
        /// </summary>
        public object[] Invoke(params object[] args)
        {
            if (info == null)
                return null;

            object result = info.Invoke(null, args);
            object[] output = new object[args.Length + 1];
            output[0] = result;
            Array.Copy(args, 0, output, 1, args.Length);
            return output;
        }

        public Method(string methodName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static)
        {
            if (!TryGetType(API_LOCATION, out Type type) || type == null)
                return;

            info = type.GetMethod(methodName, bindingFlags);
            if (info == null)
            {
                EAQS.logger.LogWarning(
                    $"Failed to find public static method '{methodName}' in type '{type.FullName}'. " +
                    "The installed EquipmentAndQuickSlots version may be older than this shim; gate on EAQS.HasEndpoint().");
            }
        }

        private static bool TryGetType(string typeNameWithAssembly, out Type type)
        {
            if (CachedTypes.TryGetValue(typeNameWithAssembly, out type))
                return type != null;

            type = Type.GetType(typeNameWithAssembly);
            CachedTypes[typeNameWithAssembly] = type;

            if (type == null)
                EAQS.logger.LogWarning($"Failed to resolve type: '{typeNameWithAssembly}'. Is EquipmentAndQuickSlots installed?");

            return type != null;
        }

        internal static bool ApiTypeExists() => Type.GetType(API_LOCATION) != null;
    }
}
